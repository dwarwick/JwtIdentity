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

        // Syncfusion Diagram data
        protected DiagramObjectCollection<Node> Nodes { get; set; } = new DiagramObjectCollection<Node>();
        protected DiagramObjectCollection<Connector> Connectors { get; set; } = new DiagramObjectCollection<Connector>();
        protected SfDiagramComponent diagram;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            BuildSyncfusionDiagram();
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
            BuildSyncfusionDiagram();
            await Task.CompletedTask;
            StateHasChanged();
        }

        protected void OnNodeCreating(IDiagramObject obj)
        {
            Node node = obj as Node;
            if (node == null) return;
            // Disables the selection of a node in the diagram
            node.Constraints = NodeConstraints.Default & ~NodeConstraints.Select;
        }

        private void BuildSyncfusionDiagram()
        {
            Nodes = new DiagramObjectCollection<Node>();
            Connectors = new DiagramObjectCollection<Connector>();

            if (Survey == null || QuestionGroups == null || !QuestionGroups.Any())
                return;

            // Create nodes for each group
            int yPosition = 50;
            var nodePositions = new Dictionary<int, (double x, double y)>();

            foreach (var group in QuestionGroups.OrderBy(g => g.GroupNumber))
            {
                var questionCount = Survey.Questions.Count(q => q.GroupId == group.GroupNumber);
                var groupName = string.IsNullOrWhiteSpace(group.GroupName) ? $"Group {group.GroupNumber}" : group.GroupName;
                
                // Create primary node for the group
                var groupNode = new Node()
                {
                    ID = $"Group{group.GroupNumber}",
                    OffsetX = 300,
                    OffsetY = yPosition,
                    Width = 200,
                    Height = 60,
                    Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                    {
                        new ShapeAnnotation()
                        {
                            Content = $"{groupName}\n({questionCount} question{(questionCount != 1 ? "s" : "")})",
                            Style = new TextStyle() { Color = "white", Bold = true }
                        }
                    },
                    Style = new ShapeStyle() 
                    { 
                        Fill = GetGroupColor(group.GroupNumber), 
                        StrokeWidth = 3, 
                        StrokeColor = "Black" 
                    }
                };
                
                Nodes.Add(groupNode);
                nodePositions[group.GroupNumber] = (300, yPosition);

                // Create nodes for branching rules within this group
                var groupQuestions = Survey.Questions
                    .Where(q => q.GroupId == group.GroupNumber && 
                        (q.QuestionType == QuestionType.MultipleChoice || 
                         q.QuestionType == QuestionType.SelectAllThatApply ||
                         q.QuestionType == QuestionType.TrueFalse))
                    .OrderBy(q => q.QuestionNumber)
                    .ToList();

                int branchNodeX = 550;
                int branchNodeY = yPosition;

                foreach (var question in groupQuestions)
                {
                    if (question.QuestionType == QuestionType.MultipleChoice)
                    {
                        var mcQuestion = question as MultipleChoiceQuestionViewModel;
                        if (mcQuestion?.Options != null)
                        {
                            foreach (var option in mcQuestion.Options.Where(o => o.BranchToGroupId.HasValue))
                            {
                                var branchNode = new Node()
                                {
                                    ID = $"Branch_MC_Q{question.Id}_O{option.Id}",
                                    OffsetX = branchNodeX,
                                    OffsetY = branchNodeY,
                                    Width = 220,
                                    Height = 60,
                                    Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                                    {
                                        new ShapeAnnotation()
                                        {
                                            Content = $"Q{question.QuestionNumber}: {TruncateText(option.OptionText, 30)}",
                                            Style = new TextStyle() { Color = "white", Bold = false, FontSize = 10 }
                                        }
                                    },
                                    Style = new ShapeStyle() 
                                    { 
                                        Fill = "#2196F3", 
                                        StrokeWidth = 2, 
                                        StrokeColor = "#1976D2" 
                                    }
                                };
                                Nodes.Add(branchNode);

                                // Connect group to branch node
                                var connectorToBranch = new Connector()
                                {
                                    ID = $"Connector_Group{group.GroupNumber}_To_Branch_Q{question.Id}_O{option.Id}",
                                    SourceID = $"Group{group.GroupNumber}",
                                    TargetID = branchNode.ID,
                                    Type = ConnectorSegmentType.Straight,
                                    Style = new ShapeStyle() { StrokeColor = "#757575", StrokeWidth = 1 }
                                };
                                Connectors.Add(connectorToBranch);

                                // Connect branch node to target group
                                var connectorToTarget = new Connector()
                                {
                                    ID = $"Connector_Branch_Q{question.Id}_O{option.Id}_To_Group{option.BranchToGroupId}",
                                    SourceID = branchNode.ID,
                                    TargetID = $"Group{option.BranchToGroupId}",
                                    Type = ConnectorSegmentType.Orthogonal,
                                    Style = new ShapeStyle() { StrokeColor = "#2196F3", StrokeWidth = 2 },
                                    TargetDecorator = new DecoratorSettings()
                                    {
                                        Shape = DecoratorShape.Arrow,
                                        Style = new ShapeStyle() { Fill = "#2196F3", StrokeColor = "#2196F3" }
                                    }
                                };
                                Connectors.Add(connectorToTarget);

                                branchNodeY += 80;
                            }
                        }
                    }
                    else if (question.QuestionType == QuestionType.SelectAllThatApply)
                    {
                        var saQuestion = question as SelectAllThatApplyQuestionViewModel;
                        if (saQuestion?.Options != null)
                        {
                            foreach (var option in saQuestion.Options.Where(o => o.BranchToGroupId.HasValue))
                            {
                                var branchNode = new Node()
                                {
                                    ID = $"Branch_SA_Q{question.Id}_O{option.Id}",
                                    OffsetX = branchNodeX,
                                    OffsetY = branchNodeY,
                                    Width = 220,
                                    Height = 60,
                                    Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                                    {
                                        new ShapeAnnotation()
                                        {
                                            Content = $"Q{question.QuestionNumber}: {TruncateText(option.OptionText, 30)}",
                                            Style = new TextStyle() { Color = "white", Bold = false, FontSize = 10 }
                                        }
                                    },
                                    Style = new ShapeStyle() 
                                    { 
                                        Fill = "#2196F3", 
                                        StrokeWidth = 2, 
                                        StrokeColor = "#1976D2" 
                                    }
                                };
                                Nodes.Add(branchNode);

                                // Connect group to branch node
                                var connectorToBranch = new Connector()
                                {
                                    ID = $"Connector_Group{group.GroupNumber}_To_Branch_Q{question.Id}_O{option.Id}",
                                    SourceID = $"Group{group.GroupNumber}",
                                    TargetID = branchNode.ID,
                                    Type = ConnectorSegmentType.Straight,
                                    Style = new ShapeStyle() { StrokeColor = "#757575", StrokeWidth = 1 }
                                };
                                Connectors.Add(connectorToBranch);

                                // Connect branch node to target group
                                var connectorToTarget = new Connector()
                                {
                                    ID = $"Connector_Branch_Q{question.Id}_O{option.Id}_To_Group{option.BranchToGroupId}",
                                    SourceID = branchNode.ID,
                                    TargetID = $"Group{option.BranchToGroupId}",
                                    Type = ConnectorSegmentType.Orthogonal,
                                    Style = new ShapeStyle() { StrokeColor = "#2196F3", StrokeWidth = 2 },
                                    TargetDecorator = new DecoratorSettings()
                                    {
                                        Shape = DecoratorShape.Arrow,
                                        Style = new ShapeStyle() { Fill = "#2196F3", StrokeColor = "#2196F3" }
                                    }
                                };
                                Connectors.Add(connectorToTarget);

                                branchNodeY += 80;
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
                                var branchNode = new Node()
                                {
                                    ID = $"Branch_TF_Q{question.Id}_True",
                                    OffsetX = branchNodeX,
                                    OffsetY = branchNodeY,
                                    Width = 220,
                                    Height = 60,
                                    Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                                    {
                                        new ShapeAnnotation()
                                        {
                                            Content = $"Q{question.QuestionNumber}: True",
                                            Style = new TextStyle() { Color = "white", Bold = false, FontSize = 10 }
                                        }
                                    },
                                    Style = new ShapeStyle() 
                                    { 
                                        Fill = "#2196F3", 
                                        StrokeWidth = 2, 
                                        StrokeColor = "#1976D2" 
                                    }
                                };
                                Nodes.Add(branchNode);

                                // Connect group to branch node
                                var connectorToBranch = new Connector()
                                {
                                    ID = $"Connector_Group{group.GroupNumber}_To_Branch_Q{question.Id}_True",
                                    SourceID = $"Group{group.GroupNumber}",
                                    TargetID = branchNode.ID,
                                    Type = ConnectorSegmentType.Straight,
                                    Style = new ShapeStyle() { StrokeColor = "#757575", StrokeWidth = 1 }
                                };
                                Connectors.Add(connectorToBranch);

                                // Connect branch node to target group
                                var connectorToTarget = new Connector()
                                {
                                    ID = $"Connector_Branch_Q{question.Id}_True_To_Group{tfQuestion.BranchToGroupIdOnTrue}",
                                    SourceID = branchNode.ID,
                                    TargetID = $"Group{tfQuestion.BranchToGroupIdOnTrue}",
                                    Type = ConnectorSegmentType.Orthogonal,
                                    Style = new ShapeStyle() { StrokeColor = "#2196F3", StrokeWidth = 2 },
                                    TargetDecorator = new DecoratorSettings()
                                    {
                                        Shape = DecoratorShape.Arrow,
                                        Style = new ShapeStyle() { Fill = "#2196F3", StrokeColor = "#2196F3" }
                                    }
                                };
                                Connectors.Add(connectorToTarget);

                                branchNodeY += 80;
                            }

                            if (tfQuestion.BranchToGroupIdOnFalse.HasValue)
                            {
                                var branchNode = new Node()
                                {
                                    ID = $"Branch_TF_Q{question.Id}_False",
                                    OffsetX = branchNodeX,
                                    OffsetY = branchNodeY,
                                    Width = 220,
                                    Height = 60,
                                    Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                                    {
                                        new ShapeAnnotation()
                                        {
                                            Content = $"Q{question.QuestionNumber}: False",
                                            Style = new TextStyle() { Color = "white", Bold = false, FontSize = 10 }
                                        }
                                    },
                                    Style = new ShapeStyle() 
                                    { 
                                        Fill = "#2196F3", 
                                        StrokeWidth = 2, 
                                        StrokeColor = "#1976D2" 
                                    }
                                };
                                Nodes.Add(branchNode);

                                // Connect group to branch node
                                var connectorToBranch = new Connector()
                                {
                                    ID = $"Connector_Group{group.GroupNumber}_To_Branch_Q{question.Id}_False",
                                    SourceID = $"Group{group.GroupNumber}",
                                    TargetID = branchNode.ID,
                                    Type = ConnectorSegmentType.Straight,
                                    Style = new ShapeStyle() { StrokeColor = "#757575", StrokeWidth = 1 }
                                };
                                Connectors.Add(connectorToBranch);

                                // Connect branch node to target group
                                var connectorToTarget = new Connector()
                                {
                                    ID = $"Connector_Branch_Q{question.Id}_False_To_Group{tfQuestion.BranchToGroupIdOnFalse}",
                                    SourceID = branchNode.ID,
                                    TargetID = $"Group{tfQuestion.BranchToGroupIdOnFalse}",
                                    Type = ConnectorSegmentType.Orthogonal,
                                    Style = new ShapeStyle() { StrokeColor = "#2196F3", StrokeWidth = 2 },
                                    TargetDecorator = new DecoratorSettings()
                                    {
                                        Shape = DecoratorShape.Arrow,
                                        Style = new ShapeStyle() { Fill = "#2196F3", StrokeColor = "#2196F3" }
                                    }
                                };
                                Connectors.Add(connectorToTarget);

                                branchNodeY += 80;
                            }
                        }
                    }
                }

                yPosition += Math.Max(120, branchNodeY - yPosition + 40);
            }

            // Add sequential flow connectors between groups without explicit branching
            for (int i = 0; i < QuestionGroups.OrderBy(g => g.GroupNumber).Count() - 1; i++)
            {
                var currentGroup = QuestionGroups.OrderBy(g => g.GroupNumber).ElementAt(i);
                var nextGroup = QuestionGroups.OrderBy(g => g.GroupNumber).ElementAt(i + 1);
                
                // Only add sequential connector if there's no branching from this group
                bool hasExplicitBranching = Survey.Questions.Any(q => 
                    q.GroupId == currentGroup.GroupNumber && 
                    ((q is MultipleChoiceQuestionViewModel mc && mc.Options.Any(o => o.BranchToGroupId.HasValue)) ||
                     (q is SelectAllThatApplyQuestionViewModel sa && sa.Options.Any(o => o.BranchToGroupId.HasValue)) ||
                     (q is TrueFalseQuestionViewModel tf && (tf.BranchToGroupIdOnTrue.HasValue || tf.BranchToGroupIdOnFalse.HasValue))));

                if (!hasExplicitBranching)
                {
                    var sequentialConnector = new Connector()
                    {
                        ID = $"Connector_Group{currentGroup.GroupNumber}_To_Group{nextGroup.GroupNumber}_Sequential",
                        SourceID = $"Group{currentGroup.GroupNumber}",
                        TargetID = $"Group{nextGroup.GroupNumber}",
                        Type = ConnectorSegmentType.Orthogonal,
                        Style = new ShapeStyle() { StrokeColor = "#9E9E9E", StrokeWidth = 2, StrokeDashArray = "5,5" },
                        TargetDecorator = new DecoratorSettings()
                        {
                            Shape = DecoratorShape.Arrow,
                            Style = new ShapeStyle() { Fill = "#9E9E9E", StrokeColor = "#9E9E9E" }
                        }
                    };
                    Connectors.Add(sequentialConnector);
                }
            }
        }

        private string GetGroupColor(int groupNumber)
        {
            var colors = new[] 
            { 
                "#00897B", "#1976D2", "#7B1FA2", "#C62828", "#F57C00", 
                "#558B2F", "#0277BD", "#5E35B1", "#C2185B", "#EF6C00" 
            };
            return colors[groupNumber % colors.Length];
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            if (text.Length <= maxLength)
                return text;
            
            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}
