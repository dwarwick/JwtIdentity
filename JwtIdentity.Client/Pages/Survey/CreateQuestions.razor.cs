using Microsoft.AspNetCore.Components.Web;
using Syncfusion.Blazor.DropDowns;

namespace JwtIdentity.Client.Pages.Survey
{
    public class CreateQuestionsModel : BlazorBase
    {
        private bool ResetQuestions;

        [Parameter]
        public string SurveyId { get; set; }

        protected SurveyViewModel Survey { get; set; } = new();

        protected MultipleChoiceQuestionViewModel MultipleChoiceQuestion { get; set; } = new MultipleChoiceQuestionViewModel();

        protected static string[] QuestionTypes => Enum.GetNames(typeof(QuestionType));

        protected string SelectedQuestionType { get; set; } = Enum.GetName(typeof(QuestionType), QuestionType.Text) ?? "Text";

        protected string QuestionText { get; set; }

        protected string NewChoiceOptionText { get; set; }

        protected int DragIndex { get; set; }

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
            ResetQuestions = true;

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

            if (await UpdateSurvey())
            {
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

        protected void QuestionSelected(ListBoxChangeEventArgs<int, QuestionViewModel> args)
        {
            QuestionViewModel selectedQuestion = Survey.Questions.FirstOrDefault(x => x.Id == args.Value);

            if (selectedQuestion != null)
            {
                SelectedQuestionType = Enum.GetName(typeof(QuestionType), selectedQuestion.QuestionType);
                QuestionText = selectedQuestion.Text;

                if (selectedQuestion.QuestionType == QuestionType.MultipleChoice)
                {
                    MultipleChoiceQuestion = selectedQuestion as MultipleChoiceQuestionViewModel;
                }
            }
        }

        protected void OnDragStart(DragEventArgs args, int index)
        {
            DragIndex = index;
            ResetQuestions = false;
        }

        protected void OnDragOver(DragEventArgs args, int index)
        {
            // this method is required to allow the drop event to fire
        }

        // PSEUDOCODE:
        // 1) Find the MultipleChoiceQuestion in Survey.Questions with the same Id as MultipleChoiceQuestion.Id
        // 2) Replace its Options list with the newly reordered MultipleChoiceQuestion.Options
        // 3) Proceed with the existing 'await UpdateSurvey()' call

        // CODE:
        protected async Task OnDrop(DragEventArgs args, int index)
        {
            if (DragIndex == index)
            {
                return;
            }

            // reorder MultipleChoiceQuestion.Options according to the drag and drop
            var movedItem = MultipleChoiceQuestion.Options[DragIndex];
            MultipleChoiceQuestion.Options.RemoveAt(DragIndex);
            MultipleChoiceQuestion.Options.Insert(index, movedItem);

            // update Order property for each item according to its new order
            for (int i = 0; i < MultipleChoiceQuestion.Options.Count; i++)
            {
                MultipleChoiceQuestion.Options[i].Order = i;
            }

            // update Survey.Questions with the new order in MultipleChoiceQuestion.Options
            var questionToUpdate = Survey.Questions.FirstOrDefault(x => x.Id == MultipleChoiceQuestion.Id) as MultipleChoiceQuestionViewModel;
            if (questionToUpdate != null)
            {
                questionToUpdate.Options = MultipleChoiceQuestion.Options.ToList();


            }

            if (await UpdateSurvey())
            {
                _ = Snackbar.Add("Options order updated", MudBlazor.Severity.Success);
            }
            else
            {
                _ = Snackbar.Add("Options order Not Updated", MudBlazor.Severity.Error);
            }
        }

        private async Task<bool> UpdateSurvey()
        {
            var response = await ApiService.PostAsync(ApiEndpoints.Survey, Survey);
            if (response != null && response.Id > 0)
            {
                await LoadData();

                if (ResetQuestions)
                {
                    QuestionText = null;
                    MultipleChoiceQuestion.Options.Clear();
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
