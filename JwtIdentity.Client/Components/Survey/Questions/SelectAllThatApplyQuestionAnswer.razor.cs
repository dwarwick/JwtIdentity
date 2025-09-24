using JwtIdentity.Client.Pages;

namespace JwtIdentity.Client.Components.Survey
{
    public class SelectAllThatApplyQuestionAnswerModel : BlazorBase
    {
        [Parameter]
        public QuestionViewModel Question { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public Func<AnswerViewModel, object, Task> OnAnswerChanged { get; set; }

        [Parameter]
        public Func<SelectAllThatApplyAnswerViewModel, int, bool, int, Task> OnSelectAllChanged { get; set; }

        protected SelectAllThatApplyAnswerViewModel Answer => (SelectAllThatApplyAnswerViewModel)Question.Answers.First();

        protected IList<ChoiceOptionViewModel> Options
        {
            get
            {
                if (Answer.Options == null)
                {
                    var questionOptions = ((SelectAllThatApplyQuestionViewModel)Question).Options ?? new List<ChoiceOptionViewModel>();
                    Answer.Options = questionOptions;
                }

                return Answer.Options;
            }
        }

        protected Task OnOptionChanged(int index, bool value, int optionId)
        {
            return OnSelectAllChanged != null
                ? OnSelectAllChanged(Answer, index, value, optionId)
                : Task.CompletedTask;
        }
    }
}
