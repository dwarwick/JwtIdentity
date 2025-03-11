using System.Text.Json;
using System.Text.Json.Serialization;

namespace JwtIdentity.Common.ViewModels
{
    public abstract class AnswerViewModel : BaseViewModel
    {
        public int Id { get; set; }

        public int QuestionId { get; set; }

        public string IpAddress { get; set; }

        [JsonPropertyName("answerType")]
        public AnswerType AnswerType { get; set; }
    }

    public class TextAnswerViewModel : AnswerViewModel
    {
        public string Text { get; set; }
    }

    public class TrueFalseAnswerViewModel : AnswerViewModel
    {
        public bool? Value { get; set; }
    }

    public class MultipleChoiceAnswerViewModel : AnswerViewModel
    {
        public int SelectedOptionId { get; set; }
    }

    public class SingleChoiceAnswerViewModel : AnswerViewModel
    {
        public int SelectedOptionId { get; set; }
    }

    public class AnswerViewModelConverter : JsonConverter<AnswerViewModel>
    {
        public override AnswerViewModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (doc.RootElement.TryGetProperty("answerType", out var answerTypeEl))
            {
                var answerType = (AnswerType)answerTypeEl.GetInt32();
                return answerType switch
                {
                    AnswerType.Text => JsonSerializer.Deserialize<TextAnswerViewModel>(doc.RootElement.GetRawText(), options),
                    AnswerType.TrueFalse => JsonSerializer.Deserialize<TrueFalseAnswerViewModel>(doc.RootElement.GetRawText(), options),
                    AnswerType.MultipleChoice => JsonSerializer.Deserialize<MultipleChoiceAnswerViewModel>(doc.RootElement.GetRawText(), options),
                    AnswerType.SingleChoice => JsonSerializer.Deserialize<SingleChoiceAnswerViewModel>(doc.RootElement.GetRawText(), options),
                    _ => throw new NotSupportedException($"Unsupported AnswerType: {answerType}")
                };
            }
            throw new JsonException("Missing 'answerType' property.");
        }

        public override void Write(Utf8JsonWriter writer, AnswerViewModel value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, options);
        }
    }
}
