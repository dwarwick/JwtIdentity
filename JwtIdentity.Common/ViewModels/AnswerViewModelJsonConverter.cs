using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JwtIdentity.Common.ViewModels
{
    public class AnswerViewModelJsonConverter : JsonConverter<AnswerViewModel>
    {
        public override AnswerViewModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            if (!TryGetDiscriminator(root, out var discriminator))
            {
                throw new JsonException("The JSON payload for AnswerViewModel must specify an answer type discriminator.");
            }

            var json = root.GetRawText();
            var sanitizedOptions = CreateInternalSerializerOptions(options);

            return discriminator switch
            {
                AnswerType.Text => JsonSerializer.Deserialize<TextAnswerViewModel>(json, sanitizedOptions),
                AnswerType.TrueFalse => JsonSerializer.Deserialize<TrueFalseAnswerViewModel>(json, sanitizedOptions),
                AnswerType.SingleChoice => JsonSerializer.Deserialize<SingleChoiceAnswerViewModel>(json, sanitizedOptions),
                AnswerType.MultipleChoice => JsonSerializer.Deserialize<MultipleChoiceAnswerViewModel>(json, sanitizedOptions),
                AnswerType.Rating1To10 => JsonSerializer.Deserialize<Rating1To10AnswerViewModel>(json, sanitizedOptions),
                AnswerType.SelectAllThatApply => JsonSerializer.Deserialize<SelectAllThatApplyAnswerViewModel>(json, sanitizedOptions),
                _ => throw new JsonException($"Unsupported AnswerType discriminator value: {discriminator}.")
            };
        }

        public override void Write(Utf8JsonWriter writer, AnswerViewModel value, JsonSerializerOptions options)
        {
            var sanitizedOptions = CreateInternalSerializerOptions(options);

            JsonNode node = JsonSerializer.SerializeToNode(value, value.GetType(), sanitizedOptions)
                ?? throw new JsonException("Unable to serialize AnswerViewModel to JSON node.");

            if (node is not JsonObject jsonObject)
            {
                throw new JsonException("Expected AnswerViewModel to serialize to a JSON object.");
            }

            jsonObject["$answerType"] = (int)value.AnswerType;

            jsonObject.WriteTo(writer, options);
        }

        private static bool TryGetDiscriminator(JsonElement element, out AnswerType answerType)
        {
            if (TryGetAnswerTypeValue(element, "$answerType", out var discriminatorValue) ||
                TryGetAnswerTypeValue(element, "answerType", out discriminatorValue))
            {
                answerType = (AnswerType)discriminatorValue;
                return true;
            }

            answerType = default;
            return false;
        }

        private static bool TryGetAnswerTypeValue(JsonElement element, string propertyName, out int value)
        {
            if (element.TryGetProperty(propertyName, out var typeProperty) && typeProperty.TryGetInt32(out var discriminator))
            {
                value = discriminator;
                return true;
            }

            value = default;
            return false;
        }

        private static JsonSerializerOptions CreateInternalSerializerOptions(JsonSerializerOptions options)
        {
            var sanitizedOptions = new JsonSerializerOptions(options)
            {
                ReferenceHandler = null
            };

            return sanitizedOptions;
        }
    }
}
