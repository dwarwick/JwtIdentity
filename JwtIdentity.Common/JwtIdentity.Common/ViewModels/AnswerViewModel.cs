namespace JwtIdentity.Common.ViewModels
{
    public abstract class AnswerViewModel
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public AnswerType AnswerType { get; set; }
    }

    public class TextAnswerViewModel : AnswerViewModel
    {
        public string Text { get; set; }
    }

    public class TrueFalseAnswerViewModel : AnswerViewModel
    {
        public bool Value { get; set; }
    }

    public class MultipleChoiceAnswerViewModel : AnswerViewModel
    {
        public int SelectedOptionId { get; set; }
    }

    public class SingleChoiceAnswerViewModel : AnswerViewModel
    {
        public int SelectedOptionId { get; set; }
    }
}
