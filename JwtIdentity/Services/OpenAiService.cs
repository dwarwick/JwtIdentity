using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
                var questions = node?["questions"] as JsonArray ?? node?["Questions"] as JsonArray;
                if (questions is not null)
                {
                    foreach (var qNode in questions.OfType<JsonObject>())
                    {
                        NormalizeQuestionObject(qNode);
                    }
                }

                json = node?.ToJsonString() ?? json;

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

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = QuestionViewModelTypeInfoResolver,
            };

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

        private static readonly DefaultJsonTypeInfoResolver QuestionViewModelTypeInfoResolver = CreateQuestionViewModelResolver();

        private static DefaultJsonTypeInfoResolver CreateQuestionViewModelResolver()
        {
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(ConfigureQuestionViewModelPolymorphism);
            return resolver;
        }

        private static void ConfigureQuestionViewModelPolymorphism(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Type != typeof(QuestionViewModel))
            {
                return;
            }

            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "questionType",
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
            };

            var derivedTypes = typeInfo.PolymorphismOptions.DerivedTypes;

            derivedTypes.Add(new JsonDerivedType(typeof(TextQuestionViewModel), (int)QuestionType.Text));
            derivedTypes.Add(new JsonDerivedType(typeof(TrueFalseQuestionViewModel), (int)QuestionType.TrueFalse));
            derivedTypes.Add(new JsonDerivedType(typeof(MultipleChoiceQuestionViewModel), (int)QuestionType.MultipleChoice));
            derivedTypes.Add(new JsonDerivedType(typeof(Rating1To10QuestionViewModel), (int)QuestionType.Rating1To10));
            derivedTypes.Add(new JsonDerivedType(typeof(SelectAllThatApplyQuestionViewModel), (int)QuestionType.SelectAllThatApply));
        }
    }
}
