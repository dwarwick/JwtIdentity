using JwtIdentity.Client.Pages;

namespace JwtIdentity.Client.Components.Survey
{
    public class TrueFalseQuestionAnswerModel : BlazorBase
    {
        [Parameter]
        public QuestionViewModel Question { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public Func<AnswerViewModel, object, Task> OnAnswerChanged { get; set; }

        [Parameter]
        public Func<SelectAllThatApplyAnswerViewModel, int, bool, int, Task> OnSelectAllChanged { get; set; }

        protected TrueFalseAnswerViewModel TrueFalseAnswer => (TrueFalseAnswerViewModel)Question.Answers.First();

        protected Task OnValueChanged(bool? value)
        {
            TrueFalseAnswer.Value = value;
            return OnAnswerChanged != null
                ? OnAnswerChanged(TrueFalseAnswer, value ?? false)
                : Task.CompletedTask;
        }
    }
}
