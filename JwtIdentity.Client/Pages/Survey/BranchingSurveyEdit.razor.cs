using Syncfusion.Blazor.Diagram;

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

        // Syncfusion Diagram
        protected SfDiagramComponent DiagramRef { get; set; }
        protected DiagramObjectCollection<Node> DiagramNodes { get; set; }
        protected DiagramObjectCollection<Connector> DiagramConnectors { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            BuildDiagram();
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
            BuildDiagram();
            StateHasChanged();
            await Task.CompletedTask;
        }

        private void BuildDiagram()
        {
            DiagramNodes = new DiagramObjectCollection<Node>();
            DiagramConnectors = new DiagramObjectCollection<Connector>();

            if (Survey == null || QuestionGroups == null || !QuestionGroups.Any())
                return;

            var verticalSpacing = 120;
            var horizontalPosition = 400;
            var startY = 80;

            // Create nodes for each group
            foreach (var group in QuestionGroups.OrderBy(g => g.GroupNumber))
            {
                var groupColor = GetGroupColorForDiagram(group.GroupNumber);
                var questionCount = Survey.Questions.Count(q => q.GroupId == group.GroupNumber);
                var groupName = string.IsNullOrWhiteSpace(group.GroupName) ? $"Group {group.GroupNumber}" : group.GroupName;
                
                var node = new Node
                {
                    ID = $"Group{group.GroupNumber}",
                    OffsetX = horizontalPosition,
                    OffsetY = startY + (group.GroupNumber * verticalSpacing),
                    Width = 250,
                    Height = 90,
                    Shape = new BasicShape { Type = NodeShapes.Basic, Shape = NodeBasicShapes.Rectangle, CornerRadius = 8 },
                    Style = new ShapeStyle 
                    { 
                        Fill = groupColor,
                        StrokeColor = GetGroupBorderColorForDiagram(group.GroupNumber),
                        StrokeWidth = 3
                    },
                    Annotations = new DiagramObjectCollection<ShapeAnnotation>
                    {
                        new ShapeAnnotation
                        {
                            Content = $"{groupName}\n({questionCount} question{(questionCount != 1 ? "s" : "")})",
                            Style = new TextStyle { Color = "#000000", Bold = true, FontSize = 13 }
                        }
                    }
                };

                DiagramNodes.Add(node);
            }

            // Create connectors based on branching rules
            var processedConnections = new HashSet<string>();

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
                                var connectionKey = $"Group{question.GroupId}-Group{option.BranchToGroupId.Value}";
                                if (!processedConnections.Contains(connectionKey))
                                {
                                    CreateConnector(question.GroupId, option.BranchToGroupId.Value, option.OptionText);
                                    processedConnections.Add(connectionKey);
                                }
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
                                var connectionKey = $"Group{question.GroupId}-Group{option.BranchToGroupId.Value}";
                                if (!processedConnections.Contains(connectionKey))
                                {
                                    CreateConnector(question.GroupId, option.BranchToGroupId.Value, option.OptionText);
                                    processedConnections.Add(connectionKey);
                                }
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
                            var connectionKey = $"Group{question.GroupId}-Group{tfQuestion.BranchToGroupIdOnTrue.Value}-True";
                            if (!processedConnections.Contains(connectionKey))
                            {
                                CreateConnector(question.GroupId, tfQuestion.BranchToGroupIdOnTrue.Value, "True");
                                processedConnections.Add(connectionKey);
                            }
                        }
                        if (tfQuestion.BranchToGroupIdOnFalse.HasValue)
                        {
                            var connectionKey = $"Group{question.GroupId}-Group{tfQuestion.BranchToGroupIdOnFalse.Value}-False";
                            if (!processedConnections.Contains(connectionKey))
                            {
                                CreateConnector(question.GroupId, tfQuestion.BranchToGroupIdOnFalse.Value, "False");
                                processedConnections.Add(connectionKey);
                            }
                        }
                    }
                }
            }

            // Add default sequential flow connectors for groups without explicit branching
            for (int i = 0; i < QuestionGroups.Count - 1; i++)
            {
                var currentGroup = QuestionGroups.OrderBy(g => g.GroupNumber).ElementAt(i);
                var nextGroup = QuestionGroups.OrderBy(g => g.GroupNumber).ElementAt(i + 1);
                var connectionKey = $"Group{currentGroup.GroupNumber}-Group{nextGroup.GroupNumber}";
                
                if (!processedConnections.Contains(connectionKey))
                {
                    CreateConnector(currentGroup.GroupNumber, nextGroup.GroupNumber, "Sequential", isDashed: true);
                }
            }
        }

        private void CreateConnector(int fromGroupNumber, int toGroupNumber, string label, bool isDashed = false)
        {
            var connector = new Connector
            {
                ID = $"Connector{fromGroupNumber}to{toGroupNumber}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                SourceID = $"Group{fromGroupNumber}",
                TargetID = $"Group{toGroupNumber}",
                Type = ConnectorSegmentType.Orthogonal,
                Style = new ShapeStyle
                {
                    StrokeColor = isDashed ? "#999999" : "#1976d2",
                    StrokeWidth = isDashed ? 2 : 3,
                    StrokeDashArray = isDashed ? "5,5" : null
                },
                TargetDecorator = new DecoratorSettings
                {
                    Shape = DecoratorShape.Arrow,
                    Style = new ShapeStyle { Fill = isDashed ? "#999999" : "#1976d2", StrokeColor = isDashed ? "#999999" : "#1976d2" },
                    Width = 12,
                    Height = 12
                },
                Annotations = new DiagramObjectCollection<PathAnnotation>
                {
                    new PathAnnotation
                    {
                        Content = label,
                        Style = new TextStyle { Color = isDashed ? "#666666" : "#1976d2", FontSize = 11, Bold = !isDashed }
                    }
                }
            };

            DiagramConnectors.Add(connector);
        }

        private string GetGroupColorForDiagram(int groupNumber)
        {
            var colors = new[]
            {
                "rgba(96, 125, 139, 0.25)",   // Blue Gray
                "rgba(103, 58, 183, 0.25)",   // Deep Purple
                "rgba(0, 150, 136, 0.25)",    // Teal
                "rgba(255, 87, 34, 0.25)",    // Deep Orange
                "rgba(3, 169, 244, 0.25)",    // Light Blue
                "rgba(76, 175, 80, 0.25)",    // Green
                "rgba(233, 30, 99, 0.25)",    // Pink
                "rgba(255, 152, 0, 0.25)",    // Orange
                "rgba(121, 85, 72, 0.25)",    // Brown
                "rgba(158, 158, 158, 0.25)"   // Gray
            };
            return colors[groupNumber % colors.Length];
        }

        private string GetGroupBorderColorForDiagram(int groupNumber)
        {
            var colors = new[]
            {
                "rgb(96, 125, 139)",   // Blue Gray
                "rgb(103, 58, 183)",   // Deep Purple
                "rgb(0, 150, 136)",    // Teal
                "rgb(255, 87, 34)",    // Deep Orange
                "rgb(3, 169, 244)",    // Light Blue
                "rgb(76, 175, 80)",    // Green
                "rgb(233, 30, 99)",    // Pink
                "rgb(255, 152, 0)",    // Orange
                "rgb(121, 85, 72)",    // Brown
                "rgb(158, 158, 158)"   // Gray
            };
            return colors[groupNumber % colors.Length];
        }
    }
}
