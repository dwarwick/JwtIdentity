using System.Text.Json.Serialization;

namespace JwtIdentity.Common.ViewModels
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = nameof(QuestionType))]
    [JsonDerivedType(typeof(TextQuestionViewModel), (int)QuestionType.Text)]
    [JsonDerivedType(typeof(TrueFalseQuestionViewModel), (int)QuestionType.TrueFalse)]
    [JsonDerivedType(typeof(MultipleChoiceQuestionViewModel), (int)QuestionType.MultipleChoice)]
    public abstract class QuestionViewModel
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Text { get; set; }
        public QuestionType QuestionType { get; set; }
        public List<AnswerViewModel> Answers { get; set; }
    }

    public class QuestionsViewModel
    {
        public List<TextQuestionViewModel> TextQuestions { get; set; } = new List<TextQuestionViewModel>();
        public List<TrueFalseQuestionViewModel> TrueFalseQuestions { get; set; } = new List<TrueFalseQuestionViewModel>();
        public List<MultipleChoiceQuestionViewModel> MultipleChoiceQuestions { get; set; } = new List<MultipleChoiceQuestionViewModel>();
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
}
