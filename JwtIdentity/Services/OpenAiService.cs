using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace JwtIdentity.Services
{
    public class OpenAiService : IOpenAi
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAiService> _logger;
        private readonly ApplicationDbContext _context;

        public OpenAiService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiService> logger, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public async Task<SurveyViewModel> GenerateSurveyAsync(string description, string aiInstructions = "")
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(180);
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key is not configured.");
                return null;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var prompt = _configuration["OpenAi:Prompt"];
            if (string.IsNullOrWhiteSpace(prompt))
            {
                _logger.LogWarning("OpenAI prompt is not configured.");
                return null;
            }

            var model = _configuration["OpenAi:Model"];
            if (string.IsNullOrWhiteSpace(model))
            {
                _logger.LogWarning("OpenAI model is not configured.");
                return null;
            }

            var body = new
            {
                model = model,
                messages = new object[]
                {
                    new { role = "system", content = "You are a helpful assistant for creating surveys." },
                    new { role = "user", content = prompt.Replace("{SURVEY_DESCRIPTION}", description).Replace("{AI_INSTRUCTIONS}", aiInstructions) }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("OpenAI response was empty.");
                return null;
            }

            var match = Regex.Match(message, "{[\\s\\S]*}", RegexOptions.Singleline);
            if (!match.Success)
            {
                _logger.LogWarning("No JSON found in OpenAI response: {Message}", message);
                return null;
            }

            var json = match.Value;

            // Normalize question type property name to match converter expectations
            try
            {
                var node = JsonNode.Parse(json);
                if (node?["questions"] is JsonArray questions)
                {
                    foreach (var qNode in questions)
                    {
                        if (qNode is JsonObject q && q["QuestionType"] is JsonNode type && q["questionType"] is null)
                        {
                            q["questionType"] = type;
                            q.Remove("QuestionType");
                        }
                    }
                }
                json = node?.ToJsonString() ?? json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to normalize OpenAI JSON: {Json}", json);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            options.Converters.Add(new QuestionViewModelConverter());

            try
            {
                var survey = JsonSerializer.Deserialize<SurveyViewModel>(json, options);
                return survey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize OpenAI response: {Json}", json);
                return null;
            }
        }

        public async Task<SurveyAnalysisResponse> AnalyzeSurveyAsync(int surveyId)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key is not configured.");
                return null;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var prompt = _configuration["OpenAi:AnalysisPrompt"];
            if (string.IsNullOrWhiteSpace(prompt))
            {
                _logger.LogWarning("OpenAI analysis prompt is not configured.");
                return null;
            }

            var model = _configuration["OpenAi:Model"];
            if (string.IsNullOrWhiteSpace(model))
            {
                _logger.LogWarning("OpenAI model is not configured.");
                return null;
            }

            // Load survey with questions and answers
            var survey = await _context.Surveys
                .Include(s => s.Questions.OrderBy(q => q.QuestionNumber))
                .FirstOrDefaultAsync(s => s.Id == surveyId);

            if (survey == null)
            {
                _logger.LogWarning("Survey with ID {SurveyId} not found", surveyId);
                return null;
            }

            // Load all answers for the survey questions
            var questionIds = survey.Questions.Select(q => q.Id).ToList();
            var answers = await _context.Answers
                .Where(a => questionIds.Contains(a.QuestionId) && a.Complete)
                .ToListAsync();

            // Build survey data JSON for the prompt
            var surveyData = new
            {
                title = survey.Title,
                description = survey.Description,
                aiInstructions = survey.AiInstructions ?? "",
                questions = await BuildQuestionDataAsync(survey.Questions, answers)
            };

            var surveyDataJson = JsonSerializer.Serialize(surveyData);

            // Replace placeholders in prompt
            var finalPrompt = prompt
                .Replace("{SURVEY_TITLE}", survey.Title ?? "")
                .Replace("{SURVEY_DESCRIPTION}", survey.Description ?? "")
                .Replace("{AI_INSTRUCTIONS}", survey.AiInstructions ?? "")
                .Replace("{SURVEY_DATA}", surveyDataJson);

#if DEBUG
            Console.WriteLine($"Final OpenAI analysis prompt: {finalPrompt}");
#endif

            var body = new
            {
                model = model,
                messages = new object[]
                {
                    new { role = "system", content = "You are an expert survey analyst." },
                    new { role = "user", content = finalPrompt }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

#if DEBUG
            Console.WriteLine($"OpenAI analysis response: {responseString}");
#endif

            using var doc = JsonDocument.Parse(responseString);
            var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("OpenAI response was empty.");
                return null;
            }

            // Extract JSON from response (might be wrapped in markdown code blocks)
            var match = Regex.Match(message, @"\{[\s\S]*\}", RegexOptions.Singleline);
            if (!match.Success)
            {
                _logger.LogWarning("No JSON found in OpenAI response: {Message}", message);
                return null;
            }

            var analysisJson = match.Value;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            try
            {
                var analysis = JsonSerializer.Deserialize<SurveyAnalysisResponse>(analysisJson, options);
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize OpenAI analysis response: {Json}", analysisJson);
                return null;
            }
        }

        private async Task<List<object>> BuildQuestionDataAsync(List<Question> questions, List<Answer> answers)
        {
            var questionData = new List<object>();

            foreach (var question in questions)
            {
                var questionAnswers = answers.Where(a => a.QuestionId == question.Id).ToList();

                object questionObj = question.QuestionType switch
                {
                    QuestionType.Text => BuildTextQuestionData(question, questionAnswers),
                    QuestionType.TrueFalse => BuildTrueFalseQuestionData(question, questionAnswers),
                    QuestionType.MultipleChoice => await BuildMultipleChoiceQuestionDataAsync(question, questionAnswers),
                    QuestionType.Rating1To10 => BuildRatingQuestionData(question, questionAnswers),
                    QuestionType.SelectAllThatApply => await BuildSelectAllQuestionDataAsync(question, questionAnswers),
                    _ => new { questionNumber = question.QuestionNumber, text = question.Text, questionType = (int)question.QuestionType, responses = new List<object>() }
                };

                questionData.Add(questionObj);
            }

            return questionData;
        }

        private object BuildTextQuestionData(Question question, List<Answer> answers)
        {
            var textAnswers = answers.OfType<TextAnswer>().Select(a => a.Text).ToList();
            return new
            {
                questionNumber = question.QuestionNumber,
                text = question.Text,
                questionType = (int)QuestionType.Text,
                responses = textAnswers
            };
        }

        private object BuildTrueFalseQuestionData(Question question, List<Answer> answers)
        {
            var tfAnswers = answers.OfType<TrueFalseAnswer>().ToList();
            var trueCount = tfAnswers.Count(a => a.Value.HasValue && a.Value.Value);
            var falseCount = tfAnswers.Count(a => a.Value.HasValue && !a.Value.Value);

            return new
            {
                questionNumber = question.QuestionNumber,
                text = question.Text,
                questionType = (int)QuestionType.TrueFalse,
                responses = new Dictionary<string, int>
                {
                    { "True", trueCount },
                    { "False", falseCount }
                }
            };
        }

        private async Task<object> BuildMultipleChoiceQuestionDataAsync(Question question, List<Answer> answers)
        {
            var mcQuestion = question as MultipleChoiceQuestion;
            if (mcQuestion?.Options == null)
            {
                // Load options if not already loaded
                await _context.Entry(mcQuestion).Collection(q => q.Options).LoadAsync();
            }

            var multipleChoiceAnswers = answers.OfType<MultipleChoiceAnswer>().Select(a => a.SelectedOptionId).ToList();
            var singleChoiceAnswers = answers.OfType<SingleChoiceAnswer>().Select(a => a.SelectedOptionId).ToList();
            var selectedOptionIds = multipleChoiceAnswers.Concat(singleChoiceAnswers).ToList();

            // Group by option and count
            var optionCounts = selectedOptionIds
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            // Build responses dictionary with option text and count
            var responses = mcQuestion?.Options?
                .OrderBy(o => o.Order)
                .ToDictionary(
                    o => o.OptionText,
                    o => optionCounts.ContainsKey(o.Id) ? optionCounts[o.Id] : 0
                ) ?? new Dictionary<string, int>();

            return new
            {
                questionNumber = question.QuestionNumber,
                text = question.Text,
                questionType = (int)QuestionType.MultipleChoice,
                options = mcQuestion?.Options?.OrderBy(o => o.Order).Select(o => o.OptionText).ToList() ?? new List<string>(),
                responses = responses
            };
        }

        private object BuildRatingQuestionData(Question question, List<Answer> answers)
        {
            var ratingAnswers = answers.OfType<Rating1To10Answer>()
                .Where(a => a.SelectedOptionId >= 1 && a.SelectedOptionId <= 10)
                .ToList();

            // Group by rating value and count
            var ratingCounts = ratingAnswers
                .GroupBy(a => a.SelectedOptionId)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            // Ensure all ratings 1-10 are represented
            var responses = Enumerable.Range(1, 10)
                .ToDictionary(
                    i => i.ToString(),
                    i => ratingCounts.ContainsKey(i.ToString()) ? ratingCounts[i.ToString()] : 0
                );

            return new
            {
                questionNumber = question.QuestionNumber,
                text = question.Text,
                questionType = (int)QuestionType.Rating1To10,
                responses = responses
            };
        }

        private async Task<object> BuildSelectAllQuestionDataAsync(Question question, List<Answer> answers)
        {
            var satQuestion = question as SelectAllThatApplyQuestion;
            if (satQuestion?.Options == null)
            {
                // Load options if not already loaded
                await _context.Entry(satQuestion).Collection(q => q.Options).LoadAsync();
            }

            // Count how many times each option was selected
            var optionCounts = new Dictionary<int, int>();

            foreach (var answer in answers.OfType<SelectAllThatApplyAnswer>())
            {
                if (!string.IsNullOrEmpty(answer.SelectedOptionIds))
                {
                    var optionIds = answer.SelectedOptionIds.Split(',')
                        .Select(id => int.TryParse(id, out var val) ? val : 0)
                        .Where(id => id > 0)
                        .ToList();

                    foreach (var optionId in optionIds)
                    {
                        if (optionCounts.ContainsKey(optionId))
                            optionCounts[optionId]++;
                        else
                            optionCounts[optionId] = 1;
                    }
                }
            }

            // Build responses dictionary with option text and count
            var responses = satQuestion?.Options?
                .OrderBy(o => o.Order)
                .ToDictionary(
                    o => o.OptionText,
                    o => optionCounts.ContainsKey(o.Id) ? optionCounts[o.Id] : 0
                ) ?? new Dictionary<string, int>();

            return new
            {
                questionNumber = question.QuestionNumber,
                text = question.Text,
                questionType = (int)QuestionType.SelectAllThatApply,
                options = satQuestion?.Options?.OrderBy(o => o.Order).Select(o => o.OptionText).ToList() ?? new List<string>(),
                responses = responses
            };
        }
    }
}
