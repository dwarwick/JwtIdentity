using JwtIdentity.Client.Pages.Common;
using Syncfusion.Blazor.Data;

namespace JwtIdentity.Client.Pages.Survey
{
    public class EditSurveyModel : BlazorBase
    {
        private bool ResetQuestions;

        [Parameter]
        public string SurveyId { get; set; }

        protected SurveyViewModel Survey { get; set; } = new();

        protected MultipleChoiceQuestionViewModel MultipleChoiceQuestion { get; set; } = new MultipleChoiceQuestionViewModel();

        protected Query RemoteDataQuery { get; set; } = new Query().Take(6);

        private QuestionViewModel _selectedQuestion;
        protected QuestionViewModel SelectedQuestion
        {
            get => _selectedQuestion;
            set
            {
                // If user "re-selects" the same question, unselect
                if (value == _selectedQuestion)
                    _selectedQuestion = null;
                else
                    _selectedQuestion = value;
            }
        }

        protected BaseQuestionDto SelectedExistingQuestion { get; set; }

        private readonly DialogOptions _topCenter = new() { Position = DialogPosition.TopCenter, CloseButton = false, CloseOnEscapeKey = false };

        protected static string[] QuestionTypes => Enum.GetNames(typeof(QuestionType));

        protected string SelectedQuestionType { get; set; } = Enum.GetName(QuestionType.Text) ?? "Text";
        protected bool IsRequired { get; set; } = true;

        protected string QuestionText { get; set; }

        protected string NewChoiceOptionText { get; set; }

        protected bool AddQuestionToSurveyDisabled =>
            Survey.Published ||
            string.IsNullOrWhiteSpace(QuestionText) ||
            ((SelectedQuestionType.Replace(" ", "") == Enum.GetName(QuestionType.MultipleChoice) || 
              SelectedQuestionType.Replace(" ", "") == Enum.GetName(QuestionType.SelectAllThatApply)) && 
              MultipleChoiceQuestion.Options.Count == 0);

        protected MudExpansionPanel ExistingQuestionPanel { get; set; }

        protected MudExpansionPanel ManualQuestionPanel { get; set; }

        protected bool ExistingQuestionPanelExpanded { get; set; } = false;
        protected bool ManualQuestionPanelExpanded { get; set; } = true;


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
            if (SelectedQuestionType.Replace(" ", "") == Enum.GetName(typeof(QuestionType), QuestionType.MultipleChoice) ||
                SelectedQuestionType.Replace(" ", "") == Enum.GetName(typeof(QuestionType), QuestionType.SelectAllThatApply))
            {
                MultipleChoiceQuestion.Options.Add(new ChoiceOptionViewModel
                {
                    OptionText = NewChoiceOptionText,
                    Order = MultipleChoiceQuestion.Options.Count,
                });
                NewChoiceOptionText = null;
            }
        }

        protected async Task DeleteQuestion(QuestionViewModel question)
        {
            DialogParameters keyValuePairs = new()
                {
                    { "Message", "Are you sure you want to delete this question?" },
                    { "OkText" , "Yes" },
                    { "CancelText" , "No" }
                };

            IDialogReference response = await MudDialog.ShowAsync<ConfirmDialog>("Delete Question?", keyValuePairs, _topCenter);
            var result = await response.Result;
            if (!result.Canceled)
            {
                // User pressed OK
                bool success = await ApiService.DeleteAsync($"{ApiEndpoints.Question}/{question.Id}");

                if (success)
                {
                    QuestionText = null;
                    MultipleChoiceQuestion.Options.Clear();
                    IsRequired = true;

                    await LoadData();
                    _ = Snackbar.Add("Question Deleted", MudBlazor.Severity.Success);
                }
                else
                {
                    _ = Snackbar.Add("Question Not Deleted", MudBlazor.Severity.Error);
                }
            }
        }

        protected async Task DeleteChoiceOption(ChoiceOptionViewModel choice)
        {
            DialogParameters keyValuePairs = new()
                {
                    { "Message", "Are you sure you want to delete this choice?" },
                    { "OkText" , "Yes" },
                    { "CancelText" , "No" }
                };

            IDialogReference response = await MudDialog.ShowAsync<ConfirmDialog>("Delete Choice Option?", keyValuePairs, _topCenter);
            var result = await response.Result;
            if (!result.Canceled)
            {
                if (choice.Id == 0)
                {
                    _ = MultipleChoiceQuestion.Options.Remove(choice);
                    _ = Snackbar.Add("Choice Deleted", MudBlazor.Severity.Success);

                    return;
                }

                // User pressed OK
                bool success = await ApiService.DeleteAsync($"{ApiEndpoints.ChoiceOption}/{choice.Id}");

                if (success)
                {
                    await LoadData();
                    QuestionSelected(Survey.Questions.FirstOrDefault(x => x.Id == choice.MultipleChoiceQuestionId));
                    _ = Snackbar.Add("Choice Deleted", MudBlazor.Severity.Success);
                }
                else
                {
                    _ = Snackbar.Add("Choice Not Deleted", MudBlazor.Severity.Error);
                }
            }
        }

        protected async Task AddQuestionToSurvey()
        {
            ResetQuestions = true;

            switch (SelectedQuestionType.Replace(" ", ""))
            {
                case "Text":
                    if ((SelectedQuestion?.Id ?? 0) == 0)
                    {
                        Survey.Questions.Add(new TextQuestionViewModel { Text = QuestionText, QuestionType = QuestionType.Text, QuestionNumber = Survey.Questions.Count + 1, IsRequired = IsRequired });
                    }
                    else
                    {
                        var questionToUpdate = Survey.Questions.FirstOrDefault(x => x.Id == SelectedQuestion.Id) as TextQuestionViewModel;
                        if (questionToUpdate != null)
                        {
                            SelectedQuestion.Text = QuestionText;
                            SelectedQuestion.IsRequired = IsRequired;
                            _ = Survey.Questions.Remove(questionToUpdate);
                            Survey.Questions.Add(SelectedQuestion);
                        }
                    }
                    break;
                case "TrueFalse":
                    if ((SelectedQuestion?.Id ?? 0) == 0)
                    {
                        Survey.Questions.Add(new TrueFalseQuestionViewModel { Text = QuestionText, QuestionType = QuestionType.TrueFalse, QuestionNumber = Survey.Questions.Count + 1, IsRequired = IsRequired });
                    }
                    else
                    {
                        var questionToUpdate = Survey.Questions.FirstOrDefault(x => x.Id == SelectedQuestion.Id) as TrueFalseQuestionViewModel;
                        if (questionToUpdate != null)
                        {
                            SelectedQuestion.Text = QuestionText;
                            SelectedQuestion.IsRequired = IsRequired;
                            _ = Survey.Questions.Remove(questionToUpdate);
                            Survey.Questions.Add(SelectedQuestion);
                        }
                    }
                    break;
                case "Rating1To10":
                    if ((SelectedQuestion?.Id ?? 0) == 0)
                    {
                        Survey.Questions.Add(new Rating1To10QuestionViewModel { Text = QuestionText, QuestionType = QuestionType.Rating1To10, QuestionNumber = Survey.Questions.Count + 1, IsRequired = IsRequired });
                    }
                    else
                    {
                        var questionToUpdate = Survey.Questions.FirstOrDefault(x => x.Id == SelectedQuestion.Id) as Rating1To10QuestionViewModel;
                        if (questionToUpdate != null)
                        {
                            SelectedQuestion.Text = QuestionText;
                            SelectedQuestion.IsRequired = IsRequired;
                            _ = Survey.Questions.Remove(questionToUpdate);
                            Survey.Questions.Add(SelectedQuestion);
                        }
                    }
                    break;
                case "MultipleChoice":
                    if ((SelectedQuestion?.Id ?? 0) == 0)
                    {
                        Survey.Questions.Add(new MultipleChoiceQuestionViewModel { Text = QuestionText, QuestionType = QuestionType.MultipleChoice, QuestionNumber = Survey.Questions.Count + 1, Options = MultipleChoiceQuestion.Options, IsRequired = IsRequired });
                    }
                    else
                    {
                        var questionToUpdate = Survey.Questions.FirstOrDefault(x => x.Id == SelectedQuestion.Id) as MultipleChoiceQuestionViewModel;
                        if (questionToUpdate != null)
                        {
                            SelectedQuestion.Text = QuestionText;
                            SelectedQuestion.IsRequired = IsRequired;

                            _ = Survey.Questions.Remove(questionToUpdate);
                            Survey.Questions.Add(SelectedQuestion);
                        }
                    }

                    break;
                    
                case "SelectAllThatApply":
                    if ((SelectedQuestion?.Id ?? 0) == 0)
                    {
                        Survey.Questions.Add(new SelectAllThatApplyQuestionViewModel { Text = QuestionText, QuestionType = QuestionType.SelectAllThatApply, QuestionNumber = Survey.Questions.Count + 1, Options = MultipleChoiceQuestion.Options, IsRequired = IsRequired });
                    }
                    else
                    {
                        var questionToUpdate = Survey.Questions.FirstOrDefault(x => x.Id == SelectedQuestion.Id) as SelectAllThatApplyQuestionViewModel;
                        if (questionToUpdate != null)
                        {
                            SelectedQuestion.Text = QuestionText;
                            SelectedQuestion.IsRequired = IsRequired;
                            _ = Survey.Questions.Remove(questionToUpdate);
                            Survey.Questions.Add(SelectedQuestion);
                        }
                    }
                    break;
            }

            SelectedQuestion = null;
            SelectedExistingQuestion = null;

            if (await UpdateSurvey())
            {
                _ = Snackbar.Add("Question Added / Updated", MudBlazor.Severity.Success);
            }
            else
            {
                _ = Snackbar.Add("Question Not Added", MudBlazor.Severity.Error);
            }
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
                            return;
                        }
                        break;
                    case QuestionType.SelectAllThatApply:
                        if (((SelectAllThatApplyQuestionViewModel)question).Options.Count == 0)
                        {
                            _ = Snackbar.Add("A 'select all that apply' question must have at least 1 option", Severity.Error);
                            return;
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

        protected void HandleSelectedQuestionType(string questionType)
        {
            SelectedQuestionType = questionType;
            QuestionText = null;
            MultipleChoiceQuestion = new MultipleChoiceQuestionViewModel();
            ResetQuestions = true;
        }

        protected void QuestionSelected(QuestionViewModel input)
        {
            if (input == null)
            {
                SelectedQuestion = null;
                SelectedQuestionType = Enum.GetName(QuestionType.Text) ?? "Text";
                QuestionText = null;
                MultipleChoiceQuestion = null;
                IsRequired = true;

                return;
            }

            if (SelectedExistingQuestion == null)
            {
                SelectedQuestion = Survey.Questions.FirstOrDefault(x => x.Id == input.Id);                
            }

            if (SelectedQuestion != null)
            {
                SelectedQuestionType = Enum.GetName(typeof(QuestionType), SelectedQuestion.QuestionType);
                QuestionText = SelectedQuestion.Text;
                IsRequired = SelectedQuestion.IsRequired;

                if (SelectedQuestion.QuestionType == QuestionType.MultipleChoice)
                {
                    MultipleChoiceQuestion = SelectedQuestion as MultipleChoiceQuestionViewModel;
                }
                else if (SelectedQuestion.QuestionType == QuestionType.SelectAllThatApply)
                {
                    // When a SelectAllThatApply question is selected, use its options for the MultipleChoiceQuestion property
                    // so they can be displayed and edited in the UI
                    var selectAllQuestion = SelectedQuestion as SelectAllThatApplyQuestionViewModel;
                    MultipleChoiceQuestion = new MultipleChoiceQuestionViewModel
                    {
                        Options = selectAllQuestion.Options
                    };
                }
            }

            ManualQuestionPanelExpanded = true;
            ExistingQuestionPanelExpanded = false;
        }

        protected async Task DroppedChoiceOption(List<ChoiceOptionViewModel> choices)
        {
            ResetQuestions = false;

            // update Order property for each item according to its new order
            for (int i = 0; i < choices.Count; i++)
            {
                choices[i].Order = i;
            }

            // update Survey.Questions with the new order in MultipleChoiceQuestion.Options
            var questionToUpdate = Survey.Questions.FirstOrDefault(x => x.Id == MultipleChoiceQuestion.Id) as MultipleChoiceQuestionViewModel;
            if (questionToUpdate != null)
            {
                questionToUpdate.Options = choices.ToList();
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

        protected async Task DroppedQuestion(List<QuestionViewModel> questions)
        {
            // update QuestionNumber property for each item according to its new order
            for (int i = 0; i < questions.Count; i++)
            {
                questions[i].QuestionNumber = i + 1;
            }
            // update Survey.Questions with the new order
            Survey.Questions = questions.ToList();
            if (await UpdateSurvey())
            {
                _ = Snackbar.Add("Questions order updated", MudBlazor.Severity.Success);
            }
            else
            {
                _ = Snackbar.Add("Questions order Not Updated", MudBlazor.Severity.Error);
            }
        }

        private async Task<bool> UpdateSurvey()
        {
            try
            {
                // To avoid potential circular references and excessive payload size,
                // consider using a simplified DTO for update requests
                var simplifiedSurvey = new SurveyViewModel
                {
                    Id = Survey.Id,
                    Title = Survey.Title,
                    Description = Survey.Description,
                    Published = Survey.Published,
                    CreatedById = Survey.CreatedById,
                    CreatedDate = Survey.CreatedDate,
                    Questions = Survey.Questions
                };

                var response = await ApiService.PostAsync(ApiEndpoints.Survey, simplifiedSurvey);
                if (response != null && response.Id > 0)
                {
                    await LoadData();

                    if (ResetQuestions)
                    {
                        QuestionText = null;
                        MultipleChoiceQuestion = new MultipleChoiceQuestionViewModel();
                    }

                    return true;
                }
                else
                {
                    Console.Error.WriteLine("API returned null or invalid response");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error updating survey: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        protected async Task HandleSelectedExistingQuestion(BaseQuestionDto question)
        {
            SelectedExistingQuestion = question;

            if (question != null)
            {
                SelectedQuestion = await ApiService.GetAsync<QuestionViewModel>($"{ApiEndpoints.Question}/QuestionAndOptions/{question.Id}");

                SelectedQuestion.Id = 0;

                if (SelectedQuestion.QuestionType == QuestionType.MultipleChoice)
                {
                    foreach (var option in ((MultipleChoiceQuestionViewModel)SelectedQuestion).Options)
                    {
                        option.Id = 0;
                    }
                }
                else if (SelectedQuestion.QuestionType == QuestionType.SelectAllThatApply)
                {
                    foreach (var option in ((SelectAllThatApplyQuestionViewModel)SelectedQuestion).Options)
                    {
                        option.Id = 0;
                    }
                }
            }
            else
            {
                SelectedQuestion = null;
            }

            QuestionSelected(SelectedQuestion);

            ExistingQuestionPanelExpanded = false;
            ManualQuestionPanelExpanded = true;

        }

        protected async Task UpdateTitleDescription(string value, string type)
        {
            if(string.IsNullOrWhiteSpace(value) || value.Length < 5)
            {
                // Show error message if title or description is empty or too short
                _ = Snackbar.Add("Title/Description must be at least 5 characters long", MudBlazor.Severity.Error);
                return;
            }

            if (type == "title")
            {
                Survey.Title = value;
            }
            else if (type == "description")
            {
                Survey.Description = value;
            }
        
        
            if (await UpdateSurvey())
            {
                _ = Snackbar.Add("Update Successful", MudBlazor.Severity.Success);
            }
            else
            {
                _ = Snackbar.Add("Problem Updating Survey", MudBlazor.Severity.Error);
            }
        }
    }
}