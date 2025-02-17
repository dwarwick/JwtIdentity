namespace JwtIdentity.Common.ViewModels
{
    public abstract class QuestionViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; }
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
        public List<ChoiceOptionViewModel> Options { get; set; }
    }
}
