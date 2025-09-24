using JwtIdentity.Client.Pages;

namespace JwtIdentity.Client.Components.Survey
{
    public class MultipleChoiceQuestionAnswerModel : BlazorBase
    {
        [Parameter]
        public QuestionViewModel Question { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public Func<AnswerViewModel, object, Task> OnAnswerChanged { get; set; }

        [Parameter]
        public Func<SelectAllThatApplyAnswerViewModel, int, bool, int, Task> OnSelectAllChanged { get; set; }

        protected MultipleChoiceAnswerViewModel Answer => (MultipleChoiceAnswerViewModel)Question.Answers.First();

        protected IEnumerable<ChoiceOptionViewModel> Options => ((MultipleChoiceQuestionViewModel)Question).Options.OrderBy(o => o.Order);

        protected Task OnValueChanged(int value)
        {
            Answer.SelectedOptionId = value;
            return OnAnswerChanged != null
                ? OnAnswerChanged(Answer, value)
                : Task.CompletedTask;
        }
    }
}
