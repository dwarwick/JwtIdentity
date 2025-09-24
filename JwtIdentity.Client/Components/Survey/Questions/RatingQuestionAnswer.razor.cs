using JwtIdentity.Client.Pages;

namespace JwtIdentity.Client.Components.Survey
{
    public class RatingQuestionAnswerModel : BlazorBase
    {
        [Parameter]
        public QuestionViewModel Question { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public Func<AnswerViewModel, object, Task> OnAnswerChanged { get; set; }

        [Parameter]
        public Func<SelectAllThatApplyAnswerViewModel, int, bool, int, Task> OnSelectAllChanged { get; set; }

        protected Rating1To10AnswerViewModel RatingAnswer => (Rating1To10AnswerViewModel)Question.Answers.First();

        protected Task OnValueChanged(int value)
        {
            RatingAnswer.SelectedOptionId = value;
            return OnAnswerChanged != null
                ? OnAnswerChanged(RatingAnswer, value)
                : Task.CompletedTask;
        }
    }
}
