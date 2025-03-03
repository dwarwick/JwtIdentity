using MudBlazor.Utilities;

namespace JwtIdentity.Client.Pages.Survey
{
    public class CreateQuestionsModel : BlazorBase
    {
        [Parameter]
        public string SurveyId { get; set; }

        protected SurveyViewModel Survey { get; set; } = new();

        protected MultipleChoiceQuestionViewModel MultipleChoiceQuestion { get; set; } = new MultipleChoiceQuestionViewModel();

        protected MudDropContainer<QuestionViewModel> _container { get; set; }
        protected static string[] QuestionTypes => Enum.GetNames(typeof(QuestionType));

        protected string SelectedQuestionType { get; set; } = Enum.GetName(typeof(QuestionType), QuestionType.Text) ?? "Text";

        protected string QuestionText { get; set; }

        protected string NewChoiceOptionText { get; set; }

        protected bool AddQuestionToSurveyDisabled => string.IsNullOrWhiteSpace(QuestionText) ||
            (SelectedQuestionType.Replace(" ", "") == Enum.GetName(QuestionType.MultipleChoice) && MultipleChoiceQuestion.Options.Count == 0);

        protected override async Task OnInitializedAsync()
        {
            // get the survey based on the SurveyId
            await LoadData();

            if (await AuthService.GetUserId() != Survey.CreatedById)
            {
                Navigation.NavigateTo("/");

                _ = Snackbar.Add("You are not authorized to edit this survey.", MudBlazor.Severity.Error);
            }
        }

        private async Task LoadData()
        {
            // get the survey based on the SurveyId
            Survey = await ApiService.GetAsync<SurveyViewModel>($"{ApiEndpoints.Survey}/{SurveyId}");

            RefreshContainer();
        }

        protected void AddChoiceOption()
        {
            if (SelectedQuestionType.Replace(" ", "") == Enum.GetName(typeof(QuestionType), QuestionType.MultipleChoice))
            {
                MultipleChoiceQuestion.Options.Add(new ChoiceOptionViewModel { OptionText = NewChoiceOptionText });
                NewChoiceOptionText = null;
            }
        }

        protected async Task AddQuestionToSurvey()
        {
            switch (SelectedQuestionType.Replace(" ", ""))
            {
                case "Text":
                    Survey.Questions.Add(new TextQuestionViewModel { Text = QuestionText, QuestionType = QuestionType.Text });
                    break;
                case "TrueFalse":
                    Survey.Questions.Add(new TrueFalseQuestionViewModel { Text = QuestionText, QuestionType = QuestionType.TrueFalse });
                    break;
                case "MultipleChoice":
                    Survey.Questions.Add(new MultipleChoiceQuestionViewModel { Text = QuestionText, QuestionType = QuestionType.MultipleChoice, Options = MultipleChoiceQuestion.Options });
                    break;
            }

            var response = await ApiService.PostAsync(ApiEndpoints.Survey, Survey);
            if (response != null && response.Id > 0)
            {
                await LoadData();

                QuestionText = null;
                MultipleChoiceQuestion.Options.Clear();

                _ = Snackbar.Add("Question Added", MudBlazor.Severity.Success);
            }
            else
            {
                _ = Snackbar.Add("Question Not Added", MudBlazor.Severity.Error);
            }
        }

        protected async Task ItemUpdated(MudItemDropInfo<QuestionViewModel> dropItem)
        {
            //var indexOffset = dropItem.DropzoneIdentifier switch
            //{
            //    "2" => Survey.Questions.Count(x => x.Selector == "1"),
            //    _ => 0
            //};

            Survey.Questions.UpdateOrder(dropItem, item => item.QuestionNumber, dropItem.IndexInZone);

            _ = await ApiService.UpdateAsync($"{ApiEndpoints.Question}/UpdateQuestionNumbers", Survey.Questions);
        }

        protected void RefreshContainer()
        {
            //update the binding to the container
            StateHasChanged();

            //the container refreshes the internal state
            _container.Refresh();
        }
    }
}
