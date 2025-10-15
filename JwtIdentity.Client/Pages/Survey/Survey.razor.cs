using System.Net.Http.Json;
using System.Security.Claims;

namespace JwtIdentity.Client.Pages.Survey
{
    public class SurveyModel : BlazorBase, IAsyncDisposable
    {
        private int _previousDemoStep = -1;

        [Parameter]
        public Guid SurveyId { get; set; }

        protected SurveyViewModel Survey { get; set; }

        protected List<AnswerViewModel> Answers { get; set; } = new List<AnswerViewModel>();

        protected int SelectedOptionId { get; set; }

        protected string Url => $"{NavigationManager.BaseUri}survey/{Survey?.Guid ?? ""}";

        protected bool isCaptchaVerified { get; set; } = false;

        protected bool Preview { get; set; }

        protected bool ViewAnswers { get; set; }

        private DotNetObjectReference<SurveyModel> objRef;

        private bool disposed = false;

        protected bool Loading { get; set; } = true;

        protected bool IsAnonymousUser { get; set; }

        protected bool AgreedToTerms { get; set; }

        protected bool IsDemoUser { get; set; }
        protected int DemoStep { get; set; }

        protected bool ShowDemoStep(int step) => IsDemoUser && DemoStep == step;

        // Branching-related properties
        protected bool HasBranching => Survey?.QuestionGroups?.Any(g => g.GroupNumber > 0) ?? false;
        protected int CurrentQuestionIndex { get; set; } = 0;
        protected List<QuestionViewModel> QuestionsToShow { get; set; } = new();
        protected QuestionViewModel CurrentQuestion => QuestionsToShow.ElementAtOrDefault(CurrentQuestionIndex);
        protected bool IsLastQuestion
        {
            get
            {
                if (!HasBranching)
                {
                    return CurrentQuestionIndex >= QuestionsToShow.Count - 1;
                }

                // In branching mode, we're at the last question only if:
                // 1. We're at the last question in QuestionsToShow, AND
                // 2. There are no more groups to visit
                return CurrentQuestionIndex >= QuestionsToShow.Count - 1 && 
                       GetNextGroupToVisit() == null;
            }
        }
        protected bool IsFirstQuestion => CurrentQuestionIndex == 0;
        protected int TotalQuestionsShown => CurrentQuestionIndex + 1;

        // Track which groups need to be visited based on branching logic
        private HashSet<int> _groupsToVisit = new();
        private HashSet<int> _visitedGroups = new();
        private int _currentGroupId = 0;

        // Calculate total question count based on groups to visit
        protected int CalculateTotalQuestions()
        {
            if (!HasBranching)
            {
                return Survey?.Questions?.Count ?? 0;
            }

            // Count questions that are either already shown or will be shown
            // This includes visited groups and groups scheduled to visit
            var allGroupsToConsider = _visitedGroups.Union(_groupsToVisit).ToHashSet();
            return Survey?.Questions?.Count(q => allGroupsToConsider.Contains(q.GroupId)) ?? 0;
        }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userName = authState.User.Identity?.Name ?? string.Empty;
            IsDemoUser = userName.StartsWith("DemoUser") && userName.EndsWith("@surveyshark.site");

            isCaptchaVerified = IsDemoUser;

            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var queryParams = QueryHelpers.ParseQuery(uri.Query);

            if (queryParams.TryGetValue("Preview", out var preview))
            {
                Preview = bool.Parse(preview);
            }
            else if (queryParams.TryGetValue("ViewAnswers", out var viewAnswers))
            {
                ViewAnswers = bool.Parse(viewAnswers);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (OperatingSystem.IsBrowser())
                {
                    await HandleLoggingInUser();
                    await LoadData();

                    if (Survey != null && Survey.Id > 0)
                    {
                        await JSRuntime.InvokeVoidAsync("registerCaptchaCallback", objRef);
                        await JSRuntime.InvokeVoidAsync("renderReCaptcha", "captcha-container", Configuration["ReCaptcha:SiteKey"]);
                    }

                    Loading = false;
                    StateHasChanged();
                }
                else
                {
                    Logger?.LogWarning("OperatingSystem.IsBrowser() returned false; captcha not rendered.");
                }
            }

            if (IsDemoUser && DemoStep != _previousDemoStep && Loading == false)
            {
                await ScrollToCurrentDemoStep();
                _previousDemoStep = DemoStep;
            }
        }

        private async Task HandleLoggingInUser()
        {
            objRef = DotNetObjectReference.Create(this);

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            ClaimsPrincipal user = authState.User;

            IsAnonymousUser = !user.Identity.IsAuthenticated;

            if (IsAnonymousUser)
            {
                Response<ApplicationUserViewModel> loginResponse = await AuthService.Login(new ApplicationUserViewModel() { UserName = "logmeinanonymoususer", Password = "123" });
                if (!loginResponse.Success)
                {
                    Navigation.NavigateTo("/");
                }
            }
        }

        private async Task LoadData()
        {
            // get the survey based on the SurveyId
            Survey = await ApiService.GetAsync<SurveyViewModel>($"{ApiEndpoints.Answer}/getanswersforsurveyforloggedinuser/{SurveyId}?Preview={Preview || ViewAnswers}");

            if (Survey != null && Survey.Id > 0)
            {
                foreach (var question in Survey.Questions)
                {

                    if (question.QuestionType == QuestionType.MultipleChoice)
                    {
                        if (question.Answers.Count == 0)
                        {
                            MultipleChoiceAnswerViewModel answer = new()
                            {
                                AnswerType = AnswerType.MultipleChoice,
                                QuestionId = question.Id
                            };

                            question.Answers.Add(answer);
                        }
                    }
                    else if (question.QuestionType == QuestionType.Text)
                    {
                        if (question.Answers.Count == 0)
                        {
                            TextAnswerViewModel answer = new()
                            {
                                AnswerType = AnswerType.Text,
                                QuestionId = question.Id
                            };

                            question.Answers.Add(answer);
                        }
                    }
                    else if (question.QuestionType == QuestionType.TrueFalse)
                    {
                        if (question.Answers.Count == 0)
                        {
                            TrueFalseAnswerViewModel answer = new()
                            {
                                AnswerType = AnswerType.TrueFalse,
                                QuestionId = question.Id,
                                Value = null
                            };

                            question.Answers.Add(answer);
                        }
                    }
                    else if (question.QuestionType == QuestionType.Rating1To10)
                    {
                        if (question.Answers.Count == 0)
                        {
                            Rating1To10AnswerViewModel answer = new()
                            {
                                AnswerType = AnswerType.Rating1To10,
                                QuestionId = question.Id
                            };

                            question.Answers.Add(answer);
                        }
                    }
                    else if (question.QuestionType == QuestionType.SelectAllThatApply)
                    {
                        var saQuestion = question as SelectAllThatApplyQuestionViewModel;

                        if (question.Answers.Count == 0)
                        {
                            SelectAllThatApplyAnswerViewModel answer = new()
                            {
                                AnswerType = AnswerType.SelectAllThatApply,
                                QuestionId = question.Id,
                                Options = ((SelectAllThatApplyQuestionViewModel)question).Options,
                                SelectedOptions = new List<bool>()
                            };

                            // Initialize the SelectedOptions list with false values for each option
                            for (int i = 0; i < ((SelectAllThatApplyQuestionViewModel)question).Options.Count; i++)
                            {
                                answer.SelectedOptions.Add(false);
                            }

                            question.Answers.Add(answer);
                        }
                        else
                        {
                            // Populate the SelectedOptions list with values from the database
                            if (question.QuestionType == QuestionType.SelectAllThatApply)
                            {
                                var answer = question.Answers.FirstOrDefault() as SelectAllThatApplyAnswerViewModel;

                                if (answer != null)
                                {
                                    while (answer.SelectedOptions.Count < saQuestion.Options.Count)
                                    {
                                        answer.SelectedOptions.Add(false);
                                    }

                                    var selectedOptionIds = new List<int>();
                                    if (!string.IsNullOrWhiteSpace(answer.SelectedOptionIds))
                                    {
                                        selectedOptionIds = answer.SelectedOptionIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(int.Parse).ToList();
                                    }
                                    for (int i = 0; i < saQuestion.Options.Count; i++)
                                    {
                                        answer.SelectedOptions[i] = selectedOptionIds.Contains(saQuestion.Options[i].Id);
                                    }
                                }
                            }
                        }
                    }
                }

                // Initialize branching questions after loading survey data
                InitializeBranchingQuestions();
            }
            else
            {
                NavigationManager.NavigateTo("/");
            }
        }

        protected async Task HandleAnswerQuestion(AnswerViewModel answer, object selectedAnswer)
        {
            AnswerViewModel response = null;

            if (answer is MultipleChoiceAnswerViewModel multipleChoiceAnswer)
            {
                multipleChoiceAnswer.SelectedOptionId = (int)selectedAnswer;
            }
            else if (answer is Rating1To10AnswerViewModel rating1To10Answer)
            {
                rating1To10Answer.SelectedOptionId = (int)selectedAnswer;
            }
            else if (answer is TextAnswerViewModel textAnswerViewModel)
            {
                textAnswerViewModel.Text = selectedAnswer.ToString();
            }
            else if (answer is TrueFalseAnswerViewModel trueFalseAnswerViewModel)
            {
                trueFalseAnswerViewModel.Value = (bool)selectedAnswer;
            }

            if (!Preview)
            {
                response = await ApiService.PostAsync(ApiEndpoints.Answer, answer);
            }

            if (response != null || Preview)
            {
                // copy answer to Survey.Questions.Answers
                foreach (var question in Survey.Questions)
                {
                    if (question.Id == answer.QuestionId)
                    {
                        for (int i = 0; i < question.Answers.Count; i++)
                        {
                            if (question.Answers[i].Id == answer.Id)
                            {
                                question.Answers[i] = !Preview ? response : answer;
                            }
                        }
                    }
                }

                // Process branching logic after answer is saved
                await OnQuestionAnswered();
            }
        }

        [JSInvokable("ReceiveCaptchaToken")]
        // This method is called by JavaScript once the captcha is solved.
        // [JSInvokable("ReceiveCaptchaToken")]
        public async Task ReceiveCaptchaToken(string token)
        {
            // Call the server-side API endpoint to verify the token using the client-side HttpClient.
            var response = await Client.PostAsJsonAsync("api/recaptcha/validate", new { Token = token });
            var result = await response.Content.ReadFromJsonAsync<RecaptchaValidationResult>();
            if (result != null && result.Success)
            {
                isCaptchaVerified = true;
                StateHasChanged();
            }
            else
            {
                // Optionally, alert the user or reset the captcha
                _ = Snackbar.Add("Captcha verification failed", Severity.Error);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    objRef?.Dispose();

                    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                    ClaimsPrincipal user = authState.User;

                    // get the users username and email
                    if (user.Identity.IsAuthenticated)
                    {
                        var username = user.FindFirst(ClaimTypes.Name)?.Value;

                        if (username == "anonymous")
                        {
                            // log the user out
                            await ((CustomAuthStateProvider)AuthStateProvider).LoggedOut();

                        }
                    }
                }

                // Dispose unmanaged resources

                disposed = true;
            }
        }

        ~SurveyModel()
        {
            DisposeAsync(false).AsTask().GetAwaiter().GetResult();
        }

        protected async Task SubmitSurvey()
        {
            if (AllQuestionsAnswered())
            {
                // Mark all answers as complete when submitting the survey
                foreach (var question in Survey.Questions)
                {
                    // First, mark all answers as complete
                    foreach (var answer in question.Answers.ToList()) // Create a copy to avoid modifying the collection while enumerating
                    {
                        answer.Complete = true;

                        // Submit the updated answer
                        var updatedAnswer = await ApiService.PostAsync(ApiEndpoints.Answer, answer);

                        // Update local answer reference with the response
                        if (updatedAnswer != null)
                        {
                            var questionRef = Survey.Questions.FirstOrDefault(q => q.Id == updatedAnswer.QuestionId);
                            if (questionRef != null)
                            {
                                var answerIndex = questionRef.Answers.FindIndex(a => a.Id == updatedAnswer.Id);
                                if (answerIndex >= 0)
                                {
                                    questionRef.Answers[answerIndex] = updatedAnswer;
                                }
                            }
                        }
                    }
                }

                _ = Snackbar.Add("Survey submitted successfully", Severity.Success);

                if (IsDemoUser)
                {
                    Navigation.NavigateTo("/mysurveys/surveysicreated?DemoStep=3");
                }
                else
                {
                    Navigation.NavigateTo("/");
                }
            }
            else
            {
                _ = Snackbar.Add("Please answer all Required questions before submitting. Required questions are marked with a red asterisk.", Severity.Warning);
            }
        }

        private bool AllQuestionsAnswered()
        {
            // check if all questions have been answered
            foreach (var question in Survey.Questions)
            {
                if (question.IsRequired == false)
                {
                    continue; // Skip non-required questions
                }

                switch (question.QuestionType)
                {
                    case QuestionType.Text:
                        if (string.IsNullOrEmpty(((TextAnswerViewModel)question.Answers[0]).Text))
                        {
                            return false;
                        }
                        break;
                    case QuestionType.TrueFalse:
                        if (((TrueFalseAnswerViewModel)question.Answers[0]).Value == null)
                        {
                            return false;
                        }
                        break;
                    case QuestionType.Rating1To10:
                        if (((Rating1To10AnswerViewModel)question.Answers[0]).SelectedOptionId == 0)
                        {
                            return false;
                        }
                        break;
                    case QuestionType.MultipleChoice:
                        if (((MultipleChoiceAnswerViewModel)question.Answers[0]).SelectedOptionId == 0)
                        {
                            return false;
                        }
                        break;
                    case QuestionType.SelectAllThatApply:
                        // Check if at least one option is selected for SelectAllThatApply questions
                        var selectAllAnswer = (SelectAllThatApplyAnswerViewModel)question.Answers[0];
                        if (string.IsNullOrEmpty(selectAllAnswer.SelectedOptionIds) ||
                            !selectAllAnswer.SelectedOptions.Any(opt => opt))
                        {
                            return false;
                        }
                        break;
                    default:
                        break;
                }
            }
            return true;
        }

        protected async Task HandleSelectAllThatApplyOption(SelectAllThatApplyAnswerViewModel answer, int index, bool isChecked, int optionId)
        {
            // Update the selected option in the SelectedOptions list
            answer.SelectedOptions[index] = isChecked;

            // Update the SelectedOptionIds string (comma-separated values)
            var selectedIds = new List<int>();
            var question = (SelectAllThatApplyQuestionViewModel)Survey.Questions.First(q => q.Id == answer.QuestionId);

            for (int i = 0; i < answer.SelectedOptions.Count && i < question.Options.Count; i++)
            {
                if (answer.SelectedOptions[i])
                {
                    selectedIds.Add(question.Options[i].Id);
                }
            }

            answer.SelectedOptionIds = string.Join(",", selectedIds);

            // Save the answer
            if (!Preview)
            {
                var response = await ApiService.PostAsync(ApiEndpoints.Answer, answer);
                if (response != null)
                {
                    // Only reload data for non-branching surveys
                    // For branching surveys, we handle this in navigation
                    if (!HasBranching)
                    {
                        await LoadData();
                    }
                    else
                    {
                        // Process branching logic after answer is saved
                        await OnQuestionAnswered();
                    }
                }
            }
            else if (HasBranching)
            {
                // In preview mode, still process branching for the UI
                await OnQuestionAnswered();
            }

            StateHasChanged();
        }

        protected void NextDemoStep()
        {
            if (!IsDemoUser) return;
            DemoStep++;

            if (DemoStep == 3 && Preview)
            {
                Navigation.NavigateTo("/mysurveys/surveysicreated?DemoStep=1");
            }
        }

        private async Task ScrollToCurrentDemoStep()
        {
            var id = DemoStep switch
            {
                0 => "2",
                1 => "survey-submit-btn",
                _ => null
            };

            if (!string.IsNullOrEmpty(id))
            {
                await JSRuntime.InvokeVoidAsync("scrollToElement", id);

                // Ensure any demo popover tied to the element renders after the scroll
                StateHasChanged();
            }
        }

        // Branching navigation methods
        protected void InitializeBranchingQuestions()
        {
            if (!HasBranching)
            {
                // No branching - show all questions
                QuestionsToShow = Survey.Questions.OrderBy(q => q.QuestionNumber).ToList();
            }
            else
            {
                // Start with group 0 questions ONLY if there are questions in group 0
                _currentGroupId = 0;
                _groupsToVisit.Clear();
                _visitedGroups.Clear();
                QuestionsToShow.Clear();

                // Only add group 0 if it has questions
                var group0Questions = Survey.Questions.Where(q => q.GroupId == 0).ToList();
                if (group0Questions.Any())
                {
                    _groupsToVisit.Add(0); // Start with group 0 if it has questions
                    LoadQuestionsForCurrentGroup();
                }
                
                // Check all questions in the survey for existing answers that trigger branching
                // This is important when resuming a partially completed survey
                CalculateInitialGroupsFromExistingAnswers();
                
                // Load any groups that were identified from existing answers
                // This ensures that when resuming a survey, all groups that should be visited
                // based on existing answers are loaded and visible
                LoadGroupsFromExistingAnswers();
                
                // If there are no Group 0 questions, the survey can't start
                // This shouldn't happen in a properly configured survey
            }
            CurrentQuestionIndex = 0;
        }

        private void LoadQuestionsForCurrentGroup()
        {
            var groupQuestions = Survey.Questions
                .Where(q => q.GroupId == _currentGroupId)
                .OrderBy(q => q.QuestionNumber)
                .ToList();

            // Initialize SelectedOptions for SelectAllThatApply questions in this group
            foreach (var question in groupQuestions)
            {
                if (question.QuestionType == QuestionType.SelectAllThatApply)
                {
                    var saQuestion = question as SelectAllThatApplyQuestionViewModel;
                    var answer = question.Answers.FirstOrDefault() as SelectAllThatApplyAnswerViewModel;
                    
                    if (answer != null && saQuestion != null)
                    {
                        // Ensure SelectedOptions list is properly sized
                        if (answer.SelectedOptions == null)
                        {
                            answer.SelectedOptions = new List<bool>();
                        }
                        
                        while (answer.SelectedOptions.Count < saQuestion.Options.Count)
                        {
                            answer.SelectedOptions.Add(false);
                        }
                        
                        // Populate based on saved SelectedOptionIds
                        if (!string.IsNullOrWhiteSpace(answer.SelectedOptionIds))
                        {
                            var selectedOptionIds = answer.SelectedOptionIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(int.Parse).ToList();
                            
                            for (int i = 0; i < saQuestion.Options.Count && i < answer.SelectedOptions.Count; i++)
                            {
                                answer.SelectedOptions[i] = selectedOptionIds.Contains(saQuestion.Options[i].Id);
                            }
                        }
                    }
                }
            }

            QuestionsToShow.AddRange(groupQuestions);
            _visitedGroups.Add(_currentGroupId);
        }

        protected void GoToNextQuestion()
        {
            if (!HasBranching)
            {
                // Simple linear progression
                if (CurrentQuestionIndex < QuestionsToShow.Count - 1)
                {
                    CurrentQuestionIndex++;
                }
                StateHasChanged();
                return;
            }

            // Branching mode - check if we need to add more groups
            if (CurrentQuestionIndex < QuestionsToShow.Count - 1)
            {
                // Still questions in current list
                CurrentQuestionIndex++;
            }
            else
            {
                // Check if we need to load more groups
                var nextGroup = GetNextGroupToVisit();
                if (nextGroup.HasValue)
                {
                    _currentGroupId = nextGroup.Value;
                    LoadQuestionsForCurrentGroup();
                    CurrentQuestionIndex++;
                }
            }
            StateHasChanged();
        }

        protected void GoToPreviousQuestion()
        {
            if (CurrentQuestionIndex > 0)
            {
                CurrentQuestionIndex--;
                StateHasChanged();
            }
        }

        private int? GetNextGroupToVisit()
        {
            // Find groups that should be visited but haven't been yet
            var unvisitedGroups = _groupsToVisit.Except(_visitedGroups).OrderBy(g => g).ToList();
            return unvisitedGroups.FirstOrDefault() as int?;
        }

        private void ProcessBranchingForCurrentQuestion()
        {
            if (!HasBranching) return;

            // Recalculate all groups to visit from scratch based on all answered questions
            // This handles the case where a user goes back and changes an answer
            RecalculateGroupsToVisit();
        }

        private void RecalculateGroupsToVisit()
        {
            if (!HasBranching) return;

            // Start fresh - only keep visited groups (we can't unvisit them)
            // but recalculate which groups we need to visit based on current answers
            var newGroupsToVisit = new HashSet<int>();

            // Always start with group 0 if it has questions
            var group0Questions = Survey.Questions.Where(q => q.GroupId == 0).ToList();
            if (group0Questions.Any())
            {
                newGroupsToVisit.Add(0);
            }

            // Go through all questions that have been shown and check their branching
            foreach (var question in QuestionsToShow)
            {
                var answer = question.Answers.FirstOrDefault();
                if (answer == null) continue;

                // Check for branching based on answer type
                if (question.QuestionType == QuestionType.MultipleChoice)
                {
                    var mcAnswer = answer as MultipleChoiceAnswerViewModel;
                    if (mcAnswer?.SelectedOptionId > 0)
                    {
                        var mcQuestion = question as MultipleChoiceQuestionViewModel;
                        var selectedOption = mcQuestion?.Options.FirstOrDefault(o => o.Id == mcAnswer.SelectedOptionId);
                        if (selectedOption?.BranchToGroupId.HasValue == true)
                        {
                            newGroupsToVisit.Add(selectedOption.BranchToGroupId.Value);
                        }
                    }
                }
                else if (question.QuestionType == QuestionType.SelectAllThatApply)
                {
                    var saAnswer = answer as SelectAllThatApplyAnswerViewModel;
                    var saQuestion = question as SelectAllThatApplyQuestionViewModel;
                    if (saQuestion != null && !string.IsNullOrEmpty(saAnswer?.SelectedOptionIds))
                    {
                        var selectedIds = saAnswer.SelectedOptionIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                        foreach (var optionId in selectedIds)
                        {
                            var option = saQuestion.Options.FirstOrDefault(o => o.Id == optionId);
                            if (option?.BranchToGroupId.HasValue == true)
                            {
                                newGroupsToVisit.Add(option.BranchToGroupId.Value);
                            }
                        }
                    }
                }
                else if (question.QuestionType == QuestionType.TrueFalse)
                {
                    var tfAnswer = answer as TrueFalseAnswerViewModel;
                    var tfQuestion = question as TrueFalseQuestionViewModel;
                    if (tfQuestion != null && tfAnswer?.Value.HasValue == true)
                    {
                        // Check if there's branching configured for this True/False answer
                        if (tfAnswer.Value.Value == true && tfQuestion.BranchToGroupIdOnTrue.HasValue)
                        {
                            newGroupsToVisit.Add(tfQuestion.BranchToGroupIdOnTrue.Value);
                        }
                        else if (tfAnswer.Value.Value == false && tfQuestion.BranchToGroupIdOnFalse.HasValue)
                        {
                            newGroupsToVisit.Add(tfQuestion.BranchToGroupIdOnFalse.Value);
                        }
                    }
                }
            }

            // Update the groups to visit with the recalculated set
            _groupsToVisit = newGroupsToVisit;
        }

        private void CalculateInitialGroupsFromExistingAnswers()
        {
            if (!HasBranching || Survey?.Questions == null) return;

            // Check all questions in the survey for existing answers that trigger branching
            // This is important when resuming a partially completed survey
            foreach (var question in Survey.Questions)
            {
                var answer = question.Answers.FirstOrDefault();
                if (answer == null) continue;

                // Check for branching based on answer type
                if (question.QuestionType == QuestionType.MultipleChoice)
                {
                    var mcAnswer = answer as MultipleChoiceAnswerViewModel;
                    if (mcAnswer?.SelectedOptionId > 0)
                    {
                        var mcQuestion = question as MultipleChoiceQuestionViewModel;
                        var selectedOption = mcQuestion?.Options.FirstOrDefault(o => o.Id == mcAnswer.SelectedOptionId);
                        if (selectedOption?.BranchToGroupId.HasValue == true && !_visitedGroups.Contains(selectedOption.BranchToGroupId.Value))
                        {
                            _groupsToVisit.Add(selectedOption.BranchToGroupId.Value);
                        }
                    }
                }
                else if (question.QuestionType == QuestionType.SelectAllThatApply)
                {
                    var saAnswer = answer as SelectAllThatApplyAnswerViewModel;
                    var saQuestion = question as SelectAllThatApplyQuestionViewModel;
                    if (saQuestion != null && !string.IsNullOrEmpty(saAnswer?.SelectedOptionIds))
                    {
                        var selectedIds = saAnswer.SelectedOptionIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                        foreach (var optionId in selectedIds)
                        {
                            var option = saQuestion.Options.FirstOrDefault(o => o.Id == optionId);
                            if (option?.BranchToGroupId.HasValue == true && !_visitedGroups.Contains(option.BranchToGroupId.Value))
                            {
                                _groupsToVisit.Add(option.BranchToGroupId.Value);
                            }
                        }
                    }
                }
                else if (question.QuestionType == QuestionType.TrueFalse)
                {
                    var tfAnswer = answer as TrueFalseAnswerViewModel;
                    var tfQuestion = question as TrueFalseQuestionViewModel;
                    if (tfQuestion != null && tfAnswer?.Value.HasValue == true)
                    {
                        // Check if there's branching configured for this True/False answer
                        if (tfAnswer.Value.Value == true && tfQuestion.BranchToGroupIdOnTrue.HasValue && !_visitedGroups.Contains(tfQuestion.BranchToGroupIdOnTrue.Value))
                        {
                            _groupsToVisit.Add(tfQuestion.BranchToGroupIdOnTrue.Value);
                        }
                        else if (tfAnswer.Value.Value == false && tfQuestion.BranchToGroupIdOnFalse.HasValue && !_visitedGroups.Contains(tfQuestion.BranchToGroupIdOnFalse.Value))
                        {
                            _groupsToVisit.Add(tfQuestion.BranchToGroupIdOnFalse.Value);
                        }
                    }
                }
            }
        }

        private void LoadGroupsFromExistingAnswers()
        {
            if (!HasBranching) return;

            // Get groups that should be loaded based on existing answers
            // (groups in _groupsToVisit that haven't been visited yet)
            var groupsToLoad = _groupsToVisit.Except(_visitedGroups).OrderBy(g => g).ToList();

            foreach (var groupId in groupsToLoad)
            {
                _currentGroupId = groupId;
                LoadQuestionsForCurrentGroup();
            }
            
            // Reset current group to 0 for proper navigation
            _currentGroupId = 0;
        }

        protected bool IsCurrentQuestionAnswered()
        {
            if (CurrentQuestion == null) return false;

            switch (CurrentQuestion.QuestionType)
            {
                case QuestionType.Text:
                    return !string.IsNullOrEmpty(((TextAnswerViewModel)CurrentQuestion.Answers[0]).Text);
                case QuestionType.TrueFalse:
                    return ((TrueFalseAnswerViewModel)CurrentQuestion.Answers[0]).Value != null;
                case QuestionType.Rating1To10:
                    return ((Rating1To10AnswerViewModel)CurrentQuestion.Answers[0]).SelectedOptionId != 0;
                case QuestionType.MultipleChoice:
                    return ((MultipleChoiceAnswerViewModel)CurrentQuestion.Answers[0]).SelectedOptionId != 0;
                case QuestionType.SelectAllThatApply:
                    var selectAllAnswer = (SelectAllThatApplyAnswerViewModel)CurrentQuestion.Answers[0];
                    return !string.IsNullOrEmpty(selectAllAnswer.SelectedOptionIds) &&
                           selectAllAnswer.SelectedOptions.Any(opt => opt);
                default:
                    return false;
            }
        }

        protected Task OnQuestionAnswered()
        {
            // Process branching logic when a question is answered
            ProcessBranchingForCurrentQuestion();
            StateHasChanged();
            return Task.CompletedTask;
        }
    }
}
