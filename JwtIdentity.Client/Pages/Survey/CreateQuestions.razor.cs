using Syncfusion.Blazor.DropDowns;

namespace JwtIdentity.Client.Pages.Survey
{
    public class CreateQuestionsModel : BlazorBase
    {
        [Parameter]
        public string SurveyId { get; set; }

        protected SurveyViewModel Survey { get; set; } = new();

        protected MultipleChoiceQuestionViewModel MultipleChoiceQuestion { get; set; } = new MultipleChoiceQuestionViewModel();

        protected static string[] QuestionTypes => Enum.GetNames(typeof(QuestionType));

        protected string SelectedQuestionType { get; set; } = Enum.GetName(typeof(QuestionType), QuestionType.Text) ?? "Text";

        protected string QuestionText { get; set; }

        protected string NewChoiceOptionText { get; set; }

        protected bool AddQuestionToSurveyDisabled =>
            Survey.Published ||
            string.IsNullOrWhiteSpace(QuestionText) ||
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

        protected async Task ItemUpdated(DropEventArgs<QuestionViewModel> args)
        {
            // Ensure we have valid indices from the event args.
            int fromIndex = args.Items.ToList()[0].QuestionNumber - 1;
            int toIndex = args.DropIndex;

            if (fromIndex != toIndex && fromIndex >= 0 && toIndex >= 0)
            {
                // Remove the dragged item from its original position
                var movedItem = Survey.Questions[fromIndex];
                Survey.Questions.RemoveAt(fromIndex);

                // Insert the dragged item into the new position
                Survey.Questions.Insert(toIndex, movedItem);

                // Update QuestionNumber property for each item according to its new order
                for (int i = 0; i < Survey.Questions.Count; i++)
                {
                    Survey.Questions[i].QuestionNumber = i + 1;
                }
            }

            // Now call your API to persist the new order.
            _ = await ApiService.UpdateAsync($"{ApiEndpoints.Question}/UpdateQuestionNumbers", Survey.Questions);

            await LoadData();
        }

        protected async Task PublishSurvey()
        {
            if (Survey.Questions.Count == 0)
            {
                _ = Snackbar.Add("A survey must have at least 1 question", Severity.Error);
                return;
            }

            foreach (var question in Survey.Questions)
            {
                switch (question.QuestionType)
                {
                    case QuestionType.MultipleChoice:
                        if (((MultipleChoiceQuestionViewModel)question).Options.Count == 0)
                        {
                            _ = Snackbar.Add("A multiple choice question must have at least 1 option", Severity.Error);
                        }

                        break;
                }
            }

            Survey.Published = true;

            SurveyViewModel publishedSurvey = await ApiService.UpdateAsync(ApiEndpoints.Survey, Survey);

            if (publishedSurvey.Published)
            {
                _ = Snackbar.Add("Survey Published", Severity.Success);
                Navigation.NavigateTo("/");
            }
            else
            {
                _ = Snackbar.Add("Unable to publish survey", Severity.Error);
            }
        }
    }
}
