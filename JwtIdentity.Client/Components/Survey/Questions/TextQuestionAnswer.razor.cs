using JwtIdentity.Client.Pages;

namespace JwtIdentity.Client.Components.Survey
{
    public class TextQuestionAnswerModel : BlazorBase
    {
        [Parameter]
        public QuestionViewModel Question { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public Func<AnswerViewModel, object, Task> OnAnswerChanged { get; set; }

        [Parameter]
        public Func<SelectAllThatApplyAnswerViewModel, int, bool, int, Task> OnSelectAllChanged { get; set; }

        protected TextAnswerViewModel TextAnswer => (TextAnswerViewModel)Question.Answers.First();

        protected Task OnValueChanged(string value)
        {
            TextAnswer.Text = value;
            return OnAnswerChanged != null
                ? OnAnswerChanged(TextAnswer, value)
                : Task.CompletedTask;
        }
    }
}
