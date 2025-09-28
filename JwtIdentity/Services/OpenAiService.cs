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
    }
}
