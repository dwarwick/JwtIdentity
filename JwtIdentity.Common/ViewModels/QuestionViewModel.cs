using System.Text.Json;
using System.Text.Json.Serialization;

namespace JwtIdentity.Common.ViewModels
{
    //[JsonPolymorphic(TypeDiscriminatorPropertyName = nameof(QuestionType))]
    //[JsonDerivedType(typeof(TextQuestionViewModel), (int)QuestionType.Text)]
    //[JsonDerivedType(typeof(TrueFalseQuestionViewModel), (int)QuestionType.TrueFalse)]
    //[JsonDerivedType(typeof(MultipleChoiceQuestionViewModel), (int)QuestionType.MultipleChoice)]
    public abstract class QuestionViewModel : BaseViewModel
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Text { get; set; }
        [JsonPropertyName("questionType")]
        public QuestionType QuestionType { get; set; }
        public List<AnswerViewModel> Answers { get; set; }
    }

    public class TextQuestionViewModel : QuestionViewModel
    {
        public int MaxLength { get; set; }
    }

    public class TrueFalseQuestionViewModel : QuestionViewModel
    {
        // No additional fields for TrueFalseQuestion
    }

    public class MultipleChoiceQuestionViewModel : QuestionViewModel
    {
        public List<ChoiceOptionViewModel> Options { get; set; } = new List<ChoiceOptionViewModel>();
    }

    public class QuestionViewModelConverter : JsonConverter<QuestionViewModel>
    {
        public override QuestionViewModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (doc.RootElement.TryGetProperty("questionType", out var answerTypeEl))
            {
                var questionType = (QuestionType)answerTypeEl.GetInt32();
                return questionType switch
                {
                    QuestionType.Text => JsonSerializer.Deserialize<TextQuestionViewModel>(doc.RootElement.GetRawText(), options),
                    QuestionType.TrueFalse => JsonSerializer.Deserialize<TrueFalseQuestionViewModel>(doc.RootElement.GetRawText(), options),
                    QuestionType.MultipleChoice => JsonSerializer.Deserialize<MultipleChoiceQuestionViewModel>(doc.RootElement.GetRawText(), options),
                    _ => throw new NotSupportedException($"Unsupported QuestionType: {questionType}")
                };
            }
            throw new JsonException("Missing 'questionType' property.");
        }

        public override void Write(Utf8JsonWriter writer, QuestionViewModel value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, options);
        }
    }
}
