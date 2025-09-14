using JwtIdentity.Client.Pages.Common;
using Syncfusion.Blazor.Data;

namespace JwtIdentity.Client.Pages.Survey
{
    public class EditSurveyModel : BlazorBase
    {
        private bool ResetQuestions;

        protected bool IsDemoUser { get; set; }
        protected int DemoStep { get; set; }
        private int _previousDemoStep = -1;
        protected Origin AnchorOrigin { get; set; } = Origin.BottomRight;
        protected Origin TransformOrigin { get; set; } = Origin.TopLeft;
        protected bool QuestionsPanelExpanded { get; set; }
        protected bool ShowDemoStep(int step) => IsDemoUser && DemoStep == step;

        [Parameter]
        public string SurveyId { get; set; }

        protected SurveyViewModel Survey { get; set; } = new();

        protected MultipleChoiceQuestionViewModel MultipleChoiceQuestion { get; set; } = new MultipleChoiceQuestionViewModel();

        protected Query RemoteDataQuery { get; set; } = new Query().Take(6);

        protected bool CanEditQuestions => string.IsNullOrWhiteSpace(Survey.AiInstructions) || Survey.AiQuestionsApproved || Survey.AiRetryCount >= 2;
        protected bool RequiresReview => !CanEditQuestions && !string.IsNullOrWhiteSpace(Survey.AiInstructions);

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

        private string tempQuestionText;

        protected string NewChoiceOptionText { get; set; }

        private string _selectedPresetChoice = string.Empty;
        protected string SelectedPresetChoice
        {
            get => _selectedPresetChoice;
            set
            {
                if (_selectedPresetChoice != value)
                {
                    _selectedPresetChoice = value;
                    var preset = PresetChoices.FirstOrDefault(x => x.Key == value);
                    if (preset.Key != null && preset.Options != null)
                    {
                        MultipleChoiceQuestion.Options.Clear();
                        int i = 0;
                        foreach (var option in preset.Options)
                        {
                            MultipleChoiceQuestion.Options.Add(new ChoiceOptionViewModel { OptionText = option, Order = i++ });
                        }
                    }

                    if (IsDemoUser && DemoStep == 7 && value == "Yes No Partially")
                    {
                        DemoStep = 8;
                    }
                }
            }
        }
        protected List<(string Key, List<string> Options)> PresetChoices => ChoiceOptionHelper.PresetChoices;

        protected bool AddQuestionToSurveyDisabled =>
            !CanEditQuestions ||
            Survey.Published ||
            string.IsNullOrWhiteSpace(QuestionText) ||
            ((SelectedQuestionType.Replace(" ", "") == Enum.GetName(QuestionType.MultipleChoice) ||
              SelectedQuestionType.Replace(" ", "") == Enum.GetName(QuestionType.SelectAllThatApply)) &&
              MultipleChoiceQuestion.Options.Count == 0);

        protected MudExpansionPanel ExistingQuestionPanel { get; set; }

        protected MudExpansionPanel ManualQuestionPanel { get; set; }

        protected bool ExistingQuestionPanelExpanded { get; set; } = false;
        protected bool ManualQuestionPanelExpanded { get; set; } = true;

        protected bool RegeneratingQuestions { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadData();

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userName = authState.User.Identity?.Name ?? string.Empty;
            IsDemoUser = userName.StartsWith("DemoUser") && userName.EndsWith("@surveyshark.site");

            if (await AuthService.GetUserId() != Survey.CreatedById)
            {
                Navigation.NavigateTo("/");

                _ = Snackbar.Add("You are not authorized to edit this survey.", MudBlazor.Severity.Error);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var isMobile = await JSRuntime.InvokeAsync<bool>("isMobile");
                if (isMobile)
                {
                    AnchorOrigin = Origin.BottomCenter;
                    TransformOrigin = Origin.TopCenter;
                }
                StateHasChanged();
            }

            if (IsDemoUser && DemoStep != _previousDemoStep)
            {
                await ScrollToCurrentDemoStep();
                _previousDemoStep = DemoStep;
            }
        }

        private async Task ScrollToCurrentDemoStep()
        {
            var id = DemoStep switch
            {
                0 => "QuestionsPanel",
                1 or 2 or 4 => "QuestionList",
                3 or 6 => "Text",
                5 => "QuestionTypeSelect",
                7 => "PresetChoices",
                8 => "SaveQuestionBtn",
                9 => "PublishSurveyBtn",
                10 => "RegenerateQuestionsBtn",
                11 => "AcceptQuestionsBtn",
                _ => null
            };

            if (!string.IsNullOrEmpty(id))
            {
                await JSRuntime.InvokeVoidAsync("scrollToElement", id);

                // Ensure any demo popover tied to the element renders after the scroll
                StateHasChanged();
            }
        }

        protected void NextDemoStep()
        {
            if (!IsDemoUser) return;

            switch (DemoStep)
            {
                case 1:
                    DemoStep = 2;
                    break;
                case 3:
                    DemoStep = 4;
                    break;
                case 6:
                    QuestionText = "Did the representative answer all of your questions?";
                    DemoStep = 7;
                    break;
            }
        }

        private async Task LoadData()
        {
            // get the survey based on the SurveyId
            Survey = await ApiService.GetAsync<SurveyViewModel>($"{ApiEndpoints.Survey}/{SurveyId}");
        }

        protected void AddChoiceOption()
        {
            if (!CanEditQuestions) return;
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
            if (!CanEditQuestions) return;
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
            if (!CanEditQuestions) return;
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
                    tempQuestionText = QuestionText; // in case the question text was edited, we need to restore it after loading data
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
                            SelectedPresetChoice = null; // Reset preset choice when updating a question
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
                if (IsDemoUser && DemoStep == 8)
                {
                    DemoStep = 9;
                }
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
                Navigation.NavigateTo("/mysurveys/surveysicreated");
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

            if (IsDemoUser && DemoStep == 5 && questionType.Replace(" ", "") == Enum.GetName(QuestionType.MultipleChoice))
            {
                DemoStep = 6;
            }
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
                ManualQuestionPanelExpanded = true;
                ExistingQuestionPanelExpanded = false;

                if (IsDemoUser && DemoStep == 4)
                {
                    DemoStep = 5;
                }

                return;
            }

            if (SelectedExistingQuestion == null)
            {
                SelectedQuestion = Survey.Questions.FirstOrDefault(x => x.Id == input.Id);
            }

            if (SelectedQuestion != null)
            {
                SelectedQuestionType = Enum.GetName(typeof(QuestionType), SelectedQuestion.QuestionType);
                QuestionText = !string.IsNullOrWhiteSpace(tempQuestionText) ? tempQuestionText : SelectedQuestion.Text;
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
            tempQuestionText = null;

            if (IsDemoUser && DemoStep == 2 && input.QuestionNumber == 1)
            {
                DemoStep = 3;
            }
        }

        protected async Task DroppedChoiceOption(List<ChoiceOptionViewModel> choices)
        {
            if (!CanEditQuestions) return;
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
            if (!CanEditQuestions) return;
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
                    AiInstructions = Survey.AiInstructions,
                    AiRetryCount = Survey.AiRetryCount,
                    AiQuestionsApproved = Survey.AiQuestionsApproved,
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
            if (string.IsNullOrWhiteSpace(value) || value.Length < 5)
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

        protected async Task UpdateAiInstructions(string value)
        {
            Survey.AiInstructions = value;
            if (await UpdateSurvey())
            {
                _ = Snackbar.Add("Update Successful", MudBlazor.Severity.Success);
            }
            else
            {
                _ = Snackbar.Add("Problem Updating Survey", MudBlazor.Severity.Error);
            }
        }

        protected async Task RegenerateQuestions()
        {
            if (Survey.AiRetryCount >= 2) return;

            RegeneratingQuestions = true;

            var response = await ApiService.PostAsync(ApiEndpoints.Survey + "/regenerate", Survey);
            if (response != null)
            {
                Survey = response;
                SelectedQuestion = null;
                QuestionsPanelExpanded = true;
                _ = Snackbar.Add("Questions regenerated", MudBlazor.Severity.Success);
                if (IsDemoUser && DemoStep == 10)
                {
                    DemoStep = 11;
                }
            }
            else
            {
                _ = Snackbar.Add("Problem regenerating questions", MudBlazor.Severity.Error);
            }

            RegeneratingQuestions = false;
        }

        protected async Task AcceptQuestions()
        {
            var response = await ApiService.PostAsync(ApiEndpoints.Survey + "/accept", Survey);
            if (response != null)
            {
                await LoadData();

                _ = Snackbar.Add("Questions accepted", MudBlazor.Severity.Success);
                if (IsDemoUser && DemoStep == 11)
                {
                    DemoStep = 0;
                }
            }
            else
            {
                _ = Snackbar.Add("Problem updating survey", MudBlazor.Severity.Error);
            }

            StateHasChanged();
        }

        protected void HandleQuestionsPanelExpanded(bool expanded)
        {
            QuestionsPanelExpanded = expanded;
            if (IsDemoUser && DemoStep == 0 && expanded)
            {
                DemoStep = 1;
            }
        }
    }
}