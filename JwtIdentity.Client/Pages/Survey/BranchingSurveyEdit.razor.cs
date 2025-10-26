namespace JwtIdentity.Client.Pages.Survey
{
    public class BranchingSurveyEditModel : BlazorBase
    {
        [Parameter]
        public string SurveyId { get; set; }

        protected SurveyViewModel Survey { get; set; }
        protected List<QuestionGroupViewModel> QuestionGroups { get; set; } = new();
        protected bool Loading { get; set; } = true;

        // Track True/False branching separately since TrueFalse doesn't have options
        protected Dictionary<int, int?> TrueBranch { get; set; } = new();
        protected Dictionary<int, int?> FalseBranch { get; set; } = new();

        // Flow diagram data
        protected List<FlowNode> FlowNodes { get; set; } = new();
        protected List<FlowConnection> FlowConnections { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            BuildFlowDiagram();
        }

        private async Task LoadData()
        {
            Loading = true;
            StateHasChanged();

            try
            {
                // Load survey with questions
                Survey = await ApiService.GetAsync<SurveyViewModel>($"{ApiEndpoints.Survey}/{SurveyId}");

                if (Survey != null)
                {
                    // Load or initialize question groups
                    var groups = await ApiService.GetAsync<List<QuestionGroupViewModel>>($"{ApiEndpoints.QuestionGroup}/Survey/{Survey.Id}");

                    // Always ensure Group 0 exists (it's implicit and may not be in the database)
                    QuestionGroups = new List<QuestionGroupViewModel>();
                    
                    // Add Group 0 if it doesn't exist in the loaded groups
                    if (groups == null || !groups.Any(g => g.GroupNumber == 0))
                    {
                        QuestionGroups.Add(new QuestionGroupViewModel
                        {
                            SurveyId = Survey.Id,
                            GroupNumber = 0,
                            GroupName = "Default Group",
                            SubmitAfterGroup = false // Default to false so it can flow to other groups
                        });
                    }
                    
                    // Add all other groups from the database
                    if (groups != null && groups.Any())
                    {
                        QuestionGroups.AddRange(groups);
                    }

                    // Load all questions with their options for branching configuration
                    // Create a copy of the list to avoid "Collection was modified" exception
                    var questionsToLoad = Survey.Questions.ToList();
                    foreach (var question in questionsToLoad)
                    {
                        if (question.QuestionType == QuestionType.MultipleChoice)
                        {
                            var mcQuestion = await ApiService.GetAsync<MultipleChoiceQuestionViewModel>(
                                $"{ApiEndpoints.Question}/QuestionAndOptions/{question.Id}");
                            if (mcQuestion != null)
                            {
                                var index = Survey.Questions.FindIndex(q => q.Id == question.Id);
                                if (index >= 0)
                                {
                                    Survey.Questions[index] = mcQuestion;
                                }
                            }
                        }
                        else if (question.QuestionType == QuestionType.SelectAllThatApply)
                        {
                            var saQuestion = await ApiService.GetAsync<SelectAllThatApplyQuestionViewModel>(
                                $"{ApiEndpoints.Question}/QuestionAndOptions/{question.Id}");
                            if (saQuestion != null)
                            {
                                var index = Survey.Questions.FindIndex(q => q.Id == question.Id);
                                if (index >= 0)
                                {
                                    Survey.Questions[index] = saQuestion;
                                }
                            }
                        }
                        else if (question.QuestionType == QuestionType.TrueFalse)
                        {
                            // Load True/False question with branching data
                            var tfQuestion = await ApiService.GetAsync<TrueFalseQuestionViewModel>(
                                $"{ApiEndpoints.Question}/QuestionAndOptions/{question.Id}");
                            if (tfQuestion != null)
                            {
                                var index = Survey.Questions.FindIndex(q => q.Id == question.Id);
                                if (index >= 0)
                                {
                                    Survey.Questions[index] = tfQuestion;
                                }

                                // Initialize True/False branching dictionaries with values from database
                                TrueBranch[tfQuestion.Id] = tfQuestion.BranchToGroupIdOnTrue;
                                FalseBranch[tfQuestion.Id] = tfQuestion.BranchToGroupIdOnFalse;
                            }
                        }
                    }
                }
                else
                {
                    Navigation.NavigateTo("/surveys/created");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading survey branching data");
                _ = Snackbar.Add("Error loading survey data", Severity.Error);
            }
            finally
            {
                Loading = false;
                StateHasChanged();
            }
        }

        protected async Task AddQuestionGroup()
        {
            try
            {
                var maxGroupNumber = QuestionGroups.Any() ? QuestionGroups.Max(g => g.GroupNumber) : 0;
                var newGroup = new QuestionGroupViewModel
                {
                    SurveyId = Survey.Id,
                    GroupNumber = maxGroupNumber + 1,
                    GroupName = $"Group {maxGroupNumber + 1}",
                    SubmitAfterGroup = true
                };

                var response = await ApiService.PostAsync(ApiEndpoints.QuestionGroup, newGroup);
                if (response != null)
                {
                    QuestionGroups.Add(response);
                    _ = Snackbar.Add($"Added Group {newGroup.GroupNumber}", Severity.Success);
                    StateHasChanged();
                }
                else
                {
                    _ = Snackbar.Add("Error creating question group", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error adding question group");
                _ = Snackbar.Add("Error creating question group", Severity.Error);
            }
        }

        protected async Task DeleteQuestionGroup(QuestionGroupViewModel group)
        {
            try
            {
                bool? confirm = await MudDialog.ShowMessageBox(
                    "Confirm Delete",
                    $"Delete Group {group.GroupNumber}? All questions in this group will be moved to Group 0.",
                    yesText: "Delete", cancelText: "Cancel");

                if (confirm == true)
                {
                    var response = await ApiService.DeleteAsync($"{ApiEndpoints.QuestionGroup}/{group.Id}");
                    if (response)
                    {
                        QuestionGroups.Remove(group);

                        // Move questions back to group 0
                        foreach (var question in Survey.Questions.Where(q => q.GroupId == group.GroupNumber))
                        {
                            question.GroupId = 0;
                        }

                        _ = Snackbar.Add($"Deleted Group {group.GroupNumber}", Severity.Success);
                        StateHasChanged();
                    }
                    else
                    {
                        _ = Snackbar.Add("Error deleting question group", Severity.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error deleting question group");
                _ = Snackbar.Add("Error deleting question group", Severity.Error);
            }
        }

        protected async Task UpdateQuestionGroup(QuestionGroupViewModel group)
        {
            try
            {
                if (group.Id == 0)
                {
                    // Group 0 doesn't need to be saved to database as it's implicit
                    return;
                }

                var response = await ApiService.UpdateAsync(ApiEndpoints.QuestionGroup, group);
                if (response != null)
                {
                    _ = Snackbar.Add("Group updated", Severity.Success);
                }
                else
                {
                    _ = Snackbar.Add("Error updating group", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error updating question group");
                _ = Snackbar.Add("Error updating group", Severity.Error);
            }
        }

        protected async Task MoveQuestionToGroup(QuestionViewModel question, int targetGroupId)
        {
            try
            {
                var oldGroupId = question.GroupId;
                question.GroupId = targetGroupId;

                // Update question via API - use PostAsync with proper typing
                var response = await ApiService.PostAsync<object, object>($"{ApiEndpoints.Question}/UpdateGroup", new
                {
                    QuestionId = question.Id,
                    GroupId = targetGroupId
                });

                if (response != null)
                {
                    _ = Snackbar.Add($"Moved question to Group {targetGroupId}", Severity.Success);
                    StateHasChanged();
                }
                else
                {
                    _ = Snackbar.Add("Error moving question", Severity.Error);
                    // Revert on error
                    question.GroupId = oldGroupId;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error moving question to group");
                _ = Snackbar.Add("Error moving question", Severity.Error);
                // Reload data to ensure consistency
                await LoadData();
            }
        }

        protected async Task UpdateChoiceOptionBranch(ChoiceOptionViewModel option)
        {
            try
            {
                var response = await ApiService.UpdateAsync(ApiEndpoints.ChoiceOption, option);
                if (response != null)
                {
                    _ = Snackbar.Add("Branching updated", Severity.Success);
                }
                else
                {
                    _ = Snackbar.Add("Error updating branching", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error updating choice option branch");
                _ = Snackbar.Add("Error updating branching", Severity.Error);
            }
        }

        protected async Task UpdateTrueFalseBranch(TrueFalseQuestionViewModel question, int? branchToGroupId, bool isTrue)
        {
            try
            {
                // Update the question's branching properties
                if (isTrue)
                {
                    question.BranchToGroupIdOnTrue = branchToGroupId;
                }
                else
                {
                    question.BranchToGroupIdOnFalse = branchToGroupId;
                }

                // Call the API to persist the changes
                var response = await ApiService.PostAsync<object, object>($"{ApiEndpoints.Question}/UpdateTrueFalseBranching", new
                {
                    QuestionId = question.Id,
                    BranchToGroupIdOnTrue = question.BranchToGroupIdOnTrue,
                    BranchToGroupIdOnFalse = question.BranchToGroupIdOnFalse
                });

                if (response != null)
                {
                    _ = Snackbar.Add("Branching updated", Severity.Success);
                }
                else
                {
                    _ = Snackbar.Add("Error updating branching", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error updating True/False branching");
                _ = Snackbar.Add("Error updating branching", Severity.Error);
            }
        }

        protected async Task RefreshDiagram()
        {
            BuildFlowDiagram();
            await Task.CompletedTask;
            StateHasChanged();
        }

        private void BuildFlowDiagram()
        {
            FlowNodes = new List<FlowNode>();
            FlowConnections = new List<FlowConnection>();

            if (Survey == null || QuestionGroups == null || !QuestionGroups.Any())
                return;

            // Create nodes for each group
            foreach (var group in QuestionGroups.OrderBy(g => g.GroupNumber))
            {
                var questionCount = Survey.Questions.Count(q => q.GroupId == group.GroupNumber);
                var groupName = string.IsNullOrWhiteSpace(group.GroupName) ? $"Group {group.GroupNumber}" : group.GroupName;
                
                FlowNodes.Add(new FlowNode
                {
                    GroupNumber = group.GroupNumber,
                    GroupName = groupName,
                    QuestionCount = questionCount
                });
            }

            // Create connections based on branching rules
            // Note: We no longer deduplicate conditional connections because we want to show ALL branching rules
            var processedSequentialConnections = new HashSet<string>();

            foreach (var question in Survey.Questions.OrderBy(q => q.QuestionNumber))
            {
                if (question.QuestionType == QuestionType.MultipleChoice)
                {
                    var mcQuestion = question as MultipleChoiceQuestionViewModel;
                    if (mcQuestion?.Options != null)
                    {
                        foreach (var option in mcQuestion.Options)
                        {
                            if (option.BranchToGroupId.HasValue)
                            {
                                // Include question text with option text for better context
                                var label = $"Q{question.QuestionNumber}: {TruncateText(question.Text, 35)} → {TruncateText(option.OptionText, 45)}";
                                FlowConnections.Add(new FlowConnection
                                {
                                    FromGroup = question.GroupId,
                                    ToGroup = option.BranchToGroupId.Value,
                                    Label = label,
                                    IsConditional = true
                                });
                            }
                        }
                    }
                }
                else if (question.QuestionType == QuestionType.SelectAllThatApply)
                {
                    var saQuestion = question as SelectAllThatApplyQuestionViewModel;
                    if (saQuestion?.Options != null)
                    {
                        foreach (var option in saQuestion.Options)
                        {
                            if (option.BranchToGroupId.HasValue)
                            {
                                var label = $"Q{question.QuestionNumber}: {TruncateText(question.Text, 35)} → {TruncateText(option.OptionText, 45)}";
                                FlowConnections.Add(new FlowConnection
                                {
                                    FromGroup = question.GroupId,
                                    ToGroup = option.BranchToGroupId.Value,
                                    Label = label,
                                    IsConditional = true
                                });
                            }
                        }
                    }
                }
                else if (question.QuestionType == QuestionType.TrueFalse)
                {
                    var tfQuestion = question as TrueFalseQuestionViewModel;
                    if (tfQuestion != null)
                    {
                        if (tfQuestion.BranchToGroupIdOnTrue.HasValue)
                        {
                            var label = $"Q{question.QuestionNumber}: {TruncateText(question.Text, 35)} → True";
                            FlowConnections.Add(new FlowConnection
                            {
                                FromGroup = question.GroupId,
                                ToGroup = tfQuestion.BranchToGroupIdOnTrue.Value,
                                Label = label,
                                IsConditional = true
                            });
                        }
                        if (tfQuestion.BranchToGroupIdOnFalse.HasValue)
                        {
                            var label = $"Q{question.QuestionNumber}: {TruncateText(question.Text, 35)} → False";
                            FlowConnections.Add(new FlowConnection
                            {
                                FromGroup = question.GroupId,
                                ToGroup = tfQuestion.BranchToGroupIdOnFalse.Value,
                                Label = label,
                                IsConditional = true
                            });
                        }
                    }
                }
            }

            // Add default sequential flow connectors for groups without explicit branching
            for (int i = 0; i < QuestionGroups.Count - 1; i++)
            {
                var currentGroup = QuestionGroups.OrderBy(g => g.GroupNumber).ElementAt(i);
                var nextGroup = QuestionGroups.OrderBy(g => g.GroupNumber).ElementAt(i + 1);
                var connectionKey = $"{currentGroup.GroupNumber}-{nextGroup.GroupNumber}";
                
                // Only add sequential connection if there's no conditional branching between these groups
                if (!processedSequentialConnections.Contains(connectionKey))
                {
                    FlowConnections.Add(new FlowConnection
                    {
                        FromGroup = currentGroup.GroupNumber,
                        ToGroup = nextGroup.GroupNumber,
                        Label = "Sequential",
                        IsConditional = false
                    });
                    processedSequentialConnections.Add(connectionKey);
                }
            }
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            if (text.Length <= maxLength)
                return text;
            
            return text.Substring(0, maxLength - 3) + "...";
        }

        protected class FlowNode
        {
            public int GroupNumber { get; set; }
            public string GroupName { get; set; }
            public int QuestionCount { get; set; }
        }

        protected class FlowConnection
        {
            public int FromGroup { get; set; }
            public int ToGroup { get; set; }
            public string Label { get; set; }
            public bool IsConditional { get; set; }
        }
    }
}
