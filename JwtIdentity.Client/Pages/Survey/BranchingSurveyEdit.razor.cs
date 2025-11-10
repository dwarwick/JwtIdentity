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
        protected DiagramConstraints Constraints { get; set; } = DiagramConstraints.Default | DiagramConstraints.Bridging;
        protected ConnectorConstraints ConnectorConstraints { get; set; } = ConnectorConstraints.Default | ConnectorConstraints.Bridging;

        protected SfDiagramComponent diagram { get; set; }
        protected double ZoomLevel { get; set; } = 1.0;
        protected LayoutType DiagramLayoutType { get; set; } = LayoutType.None; // Manual positioning like Azure example

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            BuildSyncfusionDiagram();
        }

        protected void DiagramCreated()
        {
            FitOptions options = new FitOptions() { Mode = FitMode.Both, Region = DiagramRegion.Content };

            if (diagram != null)
            {
                diagram.FitToPage(options);
            }

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
                    await RefreshDiagram();
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
                // Check if this group is used in any branching rules
                var branchingRulesUsingGroup = new List<string>();

                // Check Multiple Choice and Select All That Apply questions
                foreach (var question in Survey.Questions)
                {
                    if (question.QuestionType == QuestionType.MultipleChoice)
                    {
                        var mcQuestion = question as MultipleChoiceQuestionViewModel;
                        if (mcQuestion?.Options != null)
                        {
                            foreach (var option in mcQuestion.Options)
                            {
                                if (option.BranchToGroupId == group.GroupNumber)
                                {
                                    branchingRulesUsingGroup.Add($"Q{question.QuestionNumber}: {question.Text} - Option: {option.OptionText}");
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
                                if (option.BranchToGroupId == group.GroupNumber)
                                {
                                    branchingRulesUsingGroup.Add($"Q{question.QuestionNumber}: {question.Text} - Option: {option.OptionText}");
                                }
                            }
                        }
                    }
                    else if (question.QuestionType == QuestionType.TrueFalse)
                    {
                        var tfQuestion = question as TrueFalseQuestionViewModel;
                        if (tfQuestion != null)
                        {
                            if (tfQuestion.BranchToGroupIdOnTrue == group.GroupNumber)
                            {
                                branchingRulesUsingGroup.Add($"Q{question.QuestionNumber}: {question.Text} - True branch");
                            }
                            if (tfQuestion.BranchToGroupIdOnFalse == group.GroupNumber)
                            {
                                branchingRulesUsingGroup.Add($"Q{question.QuestionNumber}: {question.Text} - False branch");
                            }
                        }
                    }

                    // Check if this group contains questions that branch TO other groups
                    if (question.GroupId == group.GroupNumber)
                    {
                        if (question.QuestionType == QuestionType.MultipleChoice)
                        {
                            var mcQuestion = question as MultipleChoiceQuestionViewModel;
                            if (mcQuestion?.Options != null && mcQuestion.Options.Any(o => o.BranchToGroupId.HasValue))
                            {
                                branchingRulesUsingGroup.Add($"Q{question.QuestionNumber} in this group has branching rules");
                            }
                        }
                        else if (question.QuestionType == QuestionType.SelectAllThatApply)
                        {
                            var saQuestion = question as SelectAllThatApplyQuestionViewModel;
                            if (saQuestion?.Options != null && saQuestion.Options.Any(o => o.BranchToGroupId.HasValue))
                            {
                                branchingRulesUsingGroup.Add($"Q{question.QuestionNumber} in this group has branching rules");
                            }
                        }
                        else if (question.QuestionType == QuestionType.TrueFalse)
                        {
                            var tfQuestion = question as TrueFalseQuestionViewModel;
                            if (tfQuestion != null && (tfQuestion.BranchToGroupIdOnTrue.HasValue || tfQuestion.BranchToGroupIdOnFalse.HasValue))
                            {
                                branchingRulesUsingGroup.Add($"Q{question.QuestionNumber} in this group has branching rules");
                            }
                        }
                    }
                }

                if (branchingRulesUsingGroup.Any())
                {
                    _ = Snackbar.Add($"Cannot delete Group {group.GroupNumber}. Remove it from all branching rules first.", Severity.Error);
                    return;
                }

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
                        await RefreshDiagram();
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
                    await RefreshDiagram();
                    return;
                }

                var response = await ApiService.UpdateAsync(ApiEndpoints.QuestionGroup, group);
                if (response != null)
                {
                    _ = Snackbar.Add("Group updated", Severity.Success);
                    await RefreshDiagram();
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
                    await RefreshDiagram();
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
                    await RefreshDiagram();
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
                    await RefreshDiagram();
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
            if (diagram != null)
            {
                // Clear existing diagram elements
                diagram.Clear();
            }

            // Build new nodes and connectors
            BuildSyncfusionDiagram();

            // Add elements to the diagram using the async method
            if (diagram != null && (Nodes.Any() || Connectors.Any()))
            {
                // Combine nodes and connectors into a single collection
                var diagramElements = new DiagramObjectCollection<NodeBase>();
                foreach (var node in Nodes)
                {
                    diagramElements.Add(node);
                }
                foreach (var connector in Connectors)
                {
                    diagramElements.Add(connector);
                }

                await diagram.AddDiagramElementsAsync(diagramElements);

                // Allow UI to update before triggering layout
                await Task.Delay(100);

                // Apply layout
                await diagram.DoLayoutAsync();
            }

            StateHasChanged();
        }

        protected void OnZoomChanged(double newZoom)
        {
            ZoomLevel = newZoom;
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

            double xPosition = 300;
            const double groupSpacing = 600;
            const double nodeSpacing = 80;
            const double questionNodeHeight = 60;
            const double optionNodeHeight = 50;

            // First pass: Create all nodes
            foreach (var group in QuestionGroups.OrderBy(g => g.GroupNumber))
            {
                var groupName = string.IsNullOrWhiteSpace(group.GroupName) ? $"Group {group.GroupNumber}" : group.GroupName;
                var groupQuestions = Survey.Questions
                    .Where(q => q.GroupId == group.GroupNumber)
                    .OrderBy(q => q.QuestionNumber)
                    .ToList();

                double yPosition = 150;
                var childrenIds = new List<string>();

                if (!groupQuestions.Any())
                {
                    // Empty group placeholder
                    var placeholderNode = new Node()
                    {
                        ID = $"Group{group.GroupNumber}_Placeholder",
                        Width = 300,
                        Height = 80,
                        OffsetX = xPosition,
                        OffsetY = yPosition,
                        Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                        {
                            new ShapeAnnotation()
                            {
                                Content = $"{groupName}\n(No questions)",
                                Style = new TextStyle() { Color = "white", Bold = true, FontSize = 12 }
                            }
                        },
                        Style = new ShapeStyle()
                        {
                            Fill = GetGroupColor(group.GroupNumber),
                            StrokeWidth = 2,
                            StrokeColor = "Black"
                        }
                    };
                    Nodes.Add(placeholderNode);
                    childrenIds.Add(placeholderNode.ID);
                }
                else
                {
                    // Create nodes for each question and its options
                    foreach (var question in groupQuestions)
                    {
                        var questionNodeId = $"Question_{question.Id}";
                        var questionNode = new Node()
                        {
                            ID = questionNodeId,
                            Width = 400,
                            Height = questionNodeHeight,
                            OffsetX = xPosition,
                            OffsetY = yPosition,
                            Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                            {
                                new ShapeAnnotation()
                                {
                                    Content = TruncateText($"Q{question.QuestionNumber}: {question.Text}", 300),
                                    Style = new TextStyle() { Color = "black", FontSize = 11, Bold = true }
                                }
                            },
                            Style = new ShapeStyle()
                            {
                                Fill = "#e3f2fd",
                                StrokeWidth = 2,
                                StrokeColor = GetGroupColor(group.GroupNumber)
                            }
                        };
                        Nodes.Add(questionNode);
                        childrenIds.Add(questionNodeId);
                        yPosition += nodeSpacing;

                        // Create option nodes with branching
                        var options = GetBranchingOptions(question);
                        foreach (var (optionText, branchToGroupId, optionId) in options)
                        {
                            var optionNodeId = $"Option_Q{question.Id}_O{optionId}";
                            var targetGroupColor = GetGroupColor(branchToGroupId);

                            var optionNode = new Node()
                            {
                                ID = optionNodeId,
                                Width = 380,
                                Height = optionNodeHeight,
                                OffsetX = xPosition,
                                OffsetY = yPosition,
                                Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                                {
                                    new ShapeAnnotation()
                                    {
                                        Content = TruncateText(optionText, 45),
                                        Style = new TextStyle() { Color = "white", FontSize = 10 }
                                    }
                                },
                                Style = new ShapeStyle()
                                {
                                    Fill = targetGroupColor,
                                    StrokeWidth = 2,
                                    StrokeColor = "Black"
                                },
                                Ports = new DiagramObjectCollection<PointPort>()
                                {
                                    new PointPort()
                                    {
                                        ID = "leftPort",
                                        Offset = new DiagramPoint() { X = 0, Y = 0.5 },
                                        Visibility = PortVisibility.Hidden
                                    },
                                    new PointPort()
                                    {
                                        ID = "rightPort",
                                        Offset = new DiagramPoint() { X = 1, Y = 0.5 },
                                        Visibility = PortVisibility.Hidden
                                    }
                                }
                            };
                            Nodes.Add(optionNode);
                            childrenIds.Add(optionNodeId);
                            yPosition += nodeSpacing;
                        }
                    }
                }

                // Create NodeGroup for this group
                var nodeGroup = new NodeGroup()
                {
                    ID = $"GroupContainer{group.GroupNumber}",
                    Children = childrenIds.ToArray(),
                    Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                    {
                        new ShapeAnnotation()
                        {
                            Content = groupName,
                            Style = new TextStyle() { Color = "white", Bold = true, FontSize = 14 },
                            Offset = new DiagramPoint() { X = 0.5, Y = 0 },
                            Margin = new DiagramThickness() { Top = 5, Left = 0, Right = 0, Bottom = 0 },
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = Syncfusion.Blazor.Diagram.HorizontalAlignment.Center
                        }
                    },
                    Style = new ShapeStyle()
                    {
                        Fill = GetGroupColor(group.GroupNumber),
                        StrokeWidth = 3,
                        StrokeColor = "Black",
                        Opacity = 0.3
                    },
                    Padding = new DiagramThickness() { Left = 10, Right = 10, Top = 40, Bottom = 10 }
                };
                Nodes.Add(nodeGroup);

                xPosition += groupSpacing;
            }

            // Second pass: Create connectors
            foreach (var group in QuestionGroups.OrderBy(g => g.GroupNumber))
            {
                var groupQuestions = Survey.Questions
                    .Where(q => q.GroupId == group.GroupNumber)
                    .OrderBy(q => q.QuestionNumber)
                    .ToList();

                foreach (var question in groupQuestions)
                {
                    var options = GetBranchingOptions(question);
                    foreach (var (optionText, branchToGroupId, optionId) in options)
                    {
                        var optionNodeId = $"Option_Q{question.Id}_O{optionId}";
                        var targetGroupId = $"GroupContainer{branchToGroupId}";
                        var targetGroupColor = GetGroupColor(branchToGroupId);

                        var sourcePortId = group.GroupNumber < branchToGroupId ? "rightPort" : "leftPort";

                        var connector = new Connector()
                        {
                            ID = $"Connector_Q{question.Id}_O{optionId}_To_Group{branchToGroupId}",
                            SourceID = optionNodeId,
                            SourcePortID = sourcePortId,
                            TargetID = targetGroupId,
                            Type = ConnectorSegmentType.Orthogonal,
                            Constraints = ConnectorConstraints,
                            Style = new ShapeStyle() { StrokeColor = targetGroupColor, StrokeWidth = 2 },
                            TargetDecorator = new DecoratorSettings()
                            {
                                Shape = DecoratorShape.Arrow,
                                Style = new ShapeStyle() { Fill = targetGroupColor, StrokeColor = targetGroupColor }
                            }
                        };
                        Connectors.Add(connector);
                    }
                }
            }
        }
        private List<(string optionText, int branchToGroupId, int optionId)> GetBranchingOptions(QuestionViewModel question)
        {
            var result = new List<(string, int, int)>();

            if (question.QuestionType == QuestionType.MultipleChoice)
            {
                var mcQuestion = question as MultipleChoiceQuestionViewModel;
                if (mcQuestion?.Options != null)
                {
                    foreach (var option in mcQuestion.Options.Where(o => o.BranchToGroupId.HasValue))
                    {
                        result.Add((option.OptionText, option.BranchToGroupId.Value, option.Id));
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
                        result.Add((option.OptionText, option.BranchToGroupId.Value, option.Id));
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
                        result.Add(("True", tfQuestion.BranchToGroupIdOnTrue.Value, question.Id * 1000 + 1));
                    }
                    if (tfQuestion.BranchToGroupIdOnFalse.HasValue)
                    {
                        result.Add(("False", tfQuestion.BranchToGroupIdOnFalse.Value, question.Id * 1000 + 2));
                    }
                }
            }

            return result;
        }

        private string GetFirstQuestionContainerInGroup(int groupNumber)
        {
            // Find the first question container in the target group
            var firstQuestion = Survey.Questions
                .Where(q => q.GroupId == groupNumber &&
                    (q.QuestionType == QuestionType.MultipleChoice ||
                     q.QuestionType == QuestionType.SelectAllThatApply ||
                     q.QuestionType == QuestionType.TrueFalse))
                .OrderBy(q => q.QuestionNumber)
                .FirstOrDefault();

            if (firstQuestion != null)
            {
                var hasOptions = GetBranchingOptions(firstQuestion).Count > 0;
                if (hasOptions)
                {
                    return $"QuestionContainer{firstQuestion.Id}";
                }
            }

            return null;
        }

        private string GetGroupColor(int groupNumber)
        {
            // Colors match the group-badge-{n} CSS classes for consistency
            var colors = new[]
            {
                "#9e9e9e", "#1976D2", "#7B1FA2", "#C62828", "#F57C00",
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

        /// <summary>
        /// Determines which port (left or right) to use for the connector source based on target group position.
        /// </summary>
        /// <param name="sourceGroupNumber">The source group number</param>
        /// <param name="targetGroupNumber">The target group number</param>
        /// <returns>Port ID ("leftPort" or "rightPort")</returns>
        private string DetermineSourcePort(int sourceGroupNumber, int targetGroupNumber)
        {
            // If target group is before source group (lower number), use left port
            // If target group is after source group (higher number), use right port
            return targetGroupNumber < sourceGroupNumber ? "leftPort" : "rightPort";
        }

        /// <summary>
        /// Determines which port (left or right) to use for the connector target based on source group position.
        /// </summary>
        /// <param name="sourceGroupNumber">The source group number</param>
        /// <param name="targetGroupNumber">The target group number</param>
        /// <returns>Port ID ("leftPort" or "rightPort")</returns>
        private string DetermineTargetPort(int sourceGroupNumber, int targetGroupNumber)
        {
            // If source group is before target group (lower number), use left port on target
            // If source group is after target group (higher number), use right port on target
            return sourceGroupNumber < targetGroupNumber ? "leftPort" : "rightPort";
        }

        /// <summary>
        /// Determines if branching from one group to another is allowed.
        /// Users cannot branch back to the default group (Group 0) from non-default groups.
        /// </summary>
        /// <param name="fromGroupNumber">The source group number</param>
        /// <param name="toGroupNumber">The target group number</param>
        /// <returns>True if branching is allowed, false otherwise</returns>
        protected bool CanBranchToGroup(int fromGroupNumber, int toGroupNumber)
        {
            // Can't branch to the same group
            if (fromGroupNumber == toGroupNumber)
                return false;

            // From Group 0 (default), can branch to any other group
            if (fromGroupNumber == 0)
                return true;

            // From non-default groups, cannot branch back to Group 0
            return toGroupNumber != 0;
        }
    }
}
