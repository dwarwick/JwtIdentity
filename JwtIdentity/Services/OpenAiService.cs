using System.Globalization;
using System.Linq;
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

        public OpenAiService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SurveyViewModel> GenerateSurveyAsync(string description, string aiInstructions = "")
        {
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
            JsonObject? normalizedRoot = null;
            try
            {
                normalizedRoot = JsonNode.Parse(json) as JsonObject;
                var questions = normalizedRoot?["questions"] as JsonArray ?? normalizedRoot?["Questions"] as JsonArray;
                if (questions is not null)
                {
                    foreach (var qNode in questions.OfType<JsonObject>())
                    {
                        NormalizeQuestionObject(qNode);
                    }
                }

                json = normalizedRoot?.ToJsonString() ?? json;

                void NormalizeQuestionObject(JsonObject question)
                {
                    if (!TryGetQuestionTypeNode(question, out var typeNode, out var sourceKey))
                    {
                        _logger.LogWarning("Question is missing a questionType discriminator. Defaulting to Text. Question: {Question}", question.ToJsonString());
                        question["questionType"] = JsonValue.Create((int)QuestionType.Text);
                        return;
                    }

                    if (!TryConvertQuestionType(typeNode, out var discriminator))
                    {
                        _logger.LogWarning("Question has an unknown questionType value {TypeValue}. Defaulting to Text. Question: {Question}", typeNode?.ToJsonString(), question.ToJsonString());
                        discriminator = (int)QuestionType.Text;
                    }

                    question["questionType"] = JsonValue.Create(discriminator);

                    if (!string.Equals(sourceKey, "questionType", StringComparison.Ordinal))
                    {
                        question.Remove(sourceKey!);
                    }
                }

                bool TryGetQuestionTypeNode(JsonObject question, out JsonNode? typeNode, out string? sourceKey)
                {
                    if (question.TryGetPropertyValue("questionType", out var existing))
                    {
                        typeNode = existing;
                        sourceKey = "questionType";
                        return true;
                    }

                    foreach (var property in question.ToList())
                    {
                        if (IsQuestionTypeProperty(property.Key))
                        {
                            typeNode = property.Value;
                            sourceKey = property.Key;
                            return true;
                        }
                    }

                    typeNode = null;
                    sourceKey = null;
                    return false;
                }

                bool TryConvertQuestionType(JsonNode? typeNode, out int discriminator)
                {
                    discriminator = default;

                    if (typeNode is null)
                    {
                        return false;
                    }

                    if (typeNode is JsonValue value)
                    {
                        if (value.TryGetValue<int>(out var intValue))
                        {
                            discriminator = intValue;
                            return true;
                        }

                        if (value.TryGetValue<string>(out var stringValue) && TryConvertQuestionTypeFromString(stringValue, out discriminator))
                        {
                            return true;
                        }
                    }

                    if (typeNode is JsonArray array && array.Count == 1)
                    {
                        return TryConvertQuestionType(array[0], out discriminator);
                    }

                    return false;
                }

                bool TryConvertQuestionTypeFromString(string? raw, out int discriminator)
                {
                    discriminator = default;

                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        return false;
                    }

                    var trimmed = raw.Trim();

                    if (Enum.TryParse<QuestionType>(trimmed, true, out var directMatch))
                    {
                        discriminator = (int)directMatch;
                        return true;
                    }

                    if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericValue))
                    {
                        discriminator = numericValue;
                        return true;
                    }

                    var sanitized = new string(trimmed.Where(char.IsLetterOrDigit).ToArray());

                    foreach (var questionType in Enum.GetValues<QuestionType>())
                    {
                        var enumSanitized = new string(questionType.ToString().Where(char.IsLetterOrDigit).ToArray());
                        if (enumSanitized.Equals(sanitized, StringComparison.OrdinalIgnoreCase))
                        {
                            discriminator = (int)questionType;
                            return true;
                        }
                    }

                    return false;
                }

                bool IsQuestionTypeProperty(string propertyName)
                {
                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        return false;
                    }

                    var normalized = new string(propertyName.Where(char.IsLetterOrDigit).ToArray());

                    return normalized.Equals("questiontype", StringComparison.OrdinalIgnoreCase)
                        || normalized.Equals("type", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to normalize OpenAI JSON: {Json}", json);
            }

            var survey = ConvertToSurveyViewModel(normalizedRoot, json);
            if (survey is null)
            {
                try
                {
                    var rootFromJson = JsonNode.Parse(json) as JsonObject;
                    survey = ConvertToSurveyViewModel(rootFromJson, json);
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "Failed to parse OpenAI JSON while creating survey view model: {Json}", json);
                }
            }

            return survey;
        }

        private SurveyViewModel? ConvertToSurveyViewModel(JsonObject? root, string rawJson)
        {
            if (root is null)
            {
                _logger.LogWarning("OpenAI JSON was not an object: {Json}", rawJson);
                return null;
            }

            try
            {
                var survey = new SurveyViewModel
                {
                    Title = GetString(root, "title") ?? string.Empty,
                    Description = GetString(root, "description") ?? string.Empty,
                    Questions = new List<QuestionViewModel>(),
                };

                var questions = root["questions"] as JsonArray ?? new JsonArray();
                var questionIndex = 0;

                foreach (var questionNode in questions.OfType<JsonObject>())
                {
                    var question = CreateQuestionViewModel(questionNode);
                    if (question is null)
                    {
                        _logger.LogWarning("Skipping question due to invalid questionType: {Question}", questionNode.ToJsonString());
                        continue;
                    }

                    questionIndex++;
                    if (!TryGetInt(questionNode, "questionNumber", out var questionNumber))
                    {
                        questionNumber = questionIndex;
                    }

                    PopulateCommonQuestionFields(questionNode, question, questionNumber);
                    survey.Questions.Add(question);
                }

                return survey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert OpenAI JSON to SurveyViewModel: {Json}", rawJson);
                return null;
            }
        }

        private static void PopulateCommonQuestionFields(JsonObject questionNode, QuestionViewModel question, int questionNumber)
        {
            question.Text = GetString(questionNode, "text") ?? string.Empty;
            question.QuestionNumber = questionNumber;

            if (TryGetBool(questionNode, "isRequired", out var isRequired))
            {
                question.IsRequired = isRequired;
            }
        }

        private QuestionViewModel? CreateQuestionViewModel(JsonObject questionNode)
        {
            if (!TryGetInt(questionNode, "questionType", out var typeValue))
            {
                typeValue = (int)QuestionType.Text;
            }

            var questionType = Enum.IsDefined(typeof(QuestionType), typeValue)
                ? (QuestionType)typeValue
                : QuestionType.Text;

            return questionType switch
            {
                QuestionType.Text => CreateTextQuestion(questionNode, questionType),
                QuestionType.TrueFalse => CreateTrueFalseQuestion(questionType),
                QuestionType.MultipleChoice => CreateMultipleChoiceQuestion(questionNode, questionType),
                QuestionType.Rating1To10 => CreateRatingQuestion(questionType),
                QuestionType.SelectAllThatApply => CreateSelectAllThatApplyQuestion(questionNode, questionType),
                _ => null,
            };
        }

        private static QuestionViewModel CreateTextQuestion(JsonObject questionNode, QuestionType questionType)
        {
            var question = new TextQuestionViewModel
            {
                QuestionType = questionType,
            };

            if (TryGetInt(questionNode, "maxLength", out var maxLength))
            {
                question.MaxLength = maxLength;
            }

            return question;
        }

        private static QuestionViewModel CreateTrueFalseQuestion(QuestionType questionType) => new TrueFalseQuestionViewModel
        {
            QuestionType = questionType,
        };

        private static QuestionViewModel CreateRatingQuestion(QuestionType questionType) => new Rating1To10QuestionViewModel
        {
            QuestionType = questionType,
        };

        private static QuestionViewModel CreateMultipleChoiceQuestion(JsonObject questionNode, QuestionType questionType)
        {
            var question = new MultipleChoiceQuestionViewModel
            {
                QuestionType = questionType,
            };

            PopulateChoiceOptions(question.Options, questionNode);
            return question;
        }

        private static QuestionViewModel CreateSelectAllThatApplyQuestion(JsonObject questionNode, QuestionType questionType)
        {
            var question = new SelectAllThatApplyQuestionViewModel
            {
                QuestionType = questionType,
            };

            PopulateChoiceOptions(question.Options, questionNode);
            return question;
        }

        private static void PopulateChoiceOptions(List<ChoiceOptionViewModel> target, JsonObject questionNode)
        {
            if (!questionNode.TryGetPropertyValue("options", out var optionsNode) || optionsNode is not JsonArray optionsArray)
            {
                return;
            }

            var orderFallback = 0;
            foreach (var optionNode in optionsArray.OfType<JsonObject>())
            {
                var optionText = GetString(optionNode, "optionText");
                if (string.IsNullOrWhiteSpace(optionText))
                {
                    continue;
                }

                if (!TryGetInt(optionNode, "order", out var order))
                {
                    order = ++orderFallback;
                }

                target.Add(new ChoiceOptionViewModel
                {
                    OptionText = optionText.Trim(),
                    Order = order,
                });
            }
        }

        private static string? GetString(JsonObject obj, string propertyName)
        {
            if (!obj.TryGetPropertyValue(propertyName, out var node))
            {
                return null;
            }

            if (node is JsonValue value)
            {
                if (value.TryGetValue<string>(out var stringValue))
                {
                    return stringValue?.Trim();
                }

                if (value.TryGetValue<int>(out var intValue))
                {
                    return intValue.ToString(CultureInfo.InvariantCulture);
                }

                if (value.TryGetValue<double>(out var doubleValue))
                {
                    return doubleValue.ToString(CultureInfo.InvariantCulture);
                }
            }

            return node?.ToJsonString();
        }

        private static bool TryGetInt(JsonObject obj, string propertyName, out int value)
        {
            value = default;

            if (!obj.TryGetPropertyValue(propertyName, out var node))
            {
                return false;
            }

            return TryConvertToInt(node, out value);
        }

        private static bool TryConvertToInt(JsonNode? node, out int value)
        {
            value = default;

            if (node is null)
            {
                return false;
            }

            if (node is JsonValue valueNode)
            {
                if (valueNode.TryGetValue<int>(out var intValue))
                {
                    value = intValue;
                    return true;
                }

                if (valueNode.TryGetValue<long>(out var longValue))
                {
                    value = (int)longValue;
                    return true;
                }

                if (valueNode.TryGetValue<double>(out var doubleValue))
                {
                    value = (int)Math.Round(doubleValue);
                    return true;
                }

                if (valueNode.TryGetValue<string>(out var stringValue)
                    && int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    value = parsed;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetBool(JsonObject obj, string propertyName, out bool value)
        {
            value = default;

            if (!obj.TryGetPropertyValue(propertyName, out var node))
            {
                return false;
            }

            return TryConvertToBool(node, out value);
        }

        private static bool TryConvertToBool(JsonNode? node, out bool value)
        {
            value = default;

            if (node is null)
            {
                return false;
            }

            if (node is JsonValue valueNode)
            {
                if (valueNode.TryGetValue<bool>(out var boolValue))
                {
                    value = boolValue;
                    return true;
                }

                if (valueNode.TryGetValue<string>(out var stringValue)
                    && bool.TryParse(stringValue, out var parsedBool))
                {
                    value = parsedBool;
                    return true;
                }

                if (valueNode.TryGetValue<int>(out var intValue))
                {
                    value = intValue != 0;
                    return true;
                }
            }

            return false;
        }
    }
}
