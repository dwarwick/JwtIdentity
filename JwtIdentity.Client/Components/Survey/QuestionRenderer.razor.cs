using JwtIdentity.Client.Helpers;
using JwtIdentity.Client.Pages;

namespace JwtIdentity.Client.Components.Survey
{
    public class QuestionRendererModel : BlazorBase
    {
        [Parameter]
        public QuestionViewModel Question { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public Func<AnswerViewModel, object, Task> OnAnswerChanged { get; set; }

        [Parameter]
        public Func<SelectAllThatApplyAnswerViewModel, int, bool, int, Task> OnSelectAllChanged { get; set; }

        protected Type RendererType
        {
            get
            {
                if (Question == null)
                {
                    throw new InvalidOperationException("Question parameter is required.");
                }

                return QuestionComponentRegistry.GetRendererComponent(Question.QuestionType);
            }
        }

        protected IDictionary<string, object> RendererParameters => new Dictionary<string, object>
        {
            [nameof(Question)] = Question ?? throw new InvalidOperationException("Question parameter is required."),
            [nameof(Disabled)] = Disabled,
            [nameof(OnAnswerChanged)] = OnAnswerChanged,
            [nameof(OnSelectAllChanged)] = OnSelectAllChanged
        };
    }
}
