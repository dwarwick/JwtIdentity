using System.Text.Json;
using System.Text.Json.Serialization;

namespace JwtIdentity.Common.ViewModels
{
    public abstract class QuestionViewModel : BaseViewModel
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Text { get; set; }
        public int QuestionNumber { get; set; }
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

    public class Rating1To10QuestionViewModel : QuestionViewModel
    {
        // No additional fields for Rating1To10Question
    }

    public class MultipleChoiceQuestionViewModel : QuestionViewModel
    {
        public List<ChoiceOptionViewModel> Options { get; set; } = new List<ChoiceOptionViewModel>();
    }
    
    public class SelectAllThatApplyQuestionViewModel : QuestionViewModel
    {
        public List<ChoiceOptionViewModel> Options { get; set; } = new List<ChoiceOptionViewModel>();
    }

    public class BaseQuestionDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Text { get; set; }
        public int QuestionNumber { get; set; }
        public QuestionType QuestionType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        // etc. � but *no* Answers or derived class fields
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
                    QuestionType.Rating1To10 => JsonSerializer.Deserialize<Rating1To10QuestionViewModel>(doc.RootElement.GetRawText(), options),
                    QuestionType.SelectAllThatApply => JsonSerializer.Deserialize<SelectAllThatApplyQuestionViewModel>(doc.RootElement.GetRawText(), options),
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
