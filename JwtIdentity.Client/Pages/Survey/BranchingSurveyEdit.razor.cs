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

        protected SfDiagramComponent diagram;
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

            diagram.FitToPage(options);
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
            BuildSyncfusionDiagram();
            StateHasChanged();

            // Allow UI to update before triggering layout
            await Task.Delay(100);

            if (diagram != null)
            {
                await diagram.DoLayoutAsync();
            }
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

            double xPosition = 300; // Starting X position
            double yPosition = 150; // Starting Y position
            const double groupSpacing = 600; // Horizontal spacing between groups

            // Dictionary to store group center X positions for routing calculations
            var groupPositions = new Dictionary<int, double>();
            
            // Dictionary to store option node positions (X, Y) for routing calculations
            var optionNodePositions = new Dictionary<string, (double x, double y)>();

            // Create nested containers with manual positioning (like Azure example)
            foreach (var group in QuestionGroups.OrderBy(g => g.GroupNumber))
            {
                var groupName = string.IsNullOrWhiteSpace(group.GroupName) ? $"Group {group.GroupNumber}" : group.GroupName;

                // Get all questions in this group (not just those with branching rules)
                var groupQuestions = Survey.Questions
                    .Where(q => q.GroupId == group.GroupNumber)
                    .OrderBy(q => q.QuestionNumber)
                    .ToList();

                // Skip groups with no questions at all
                if (!groupQuestions.Any())
                {
                    // Create simple placeholder for empty groups
                    var placeholderPorts = new DiagramObjectCollection<PointPort>()
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
                    };

                    var placeholderNode = new Node()
                    {
                        ID = $"GroupContainer{group.GroupNumber}",
                        Width = 300,
                        Height = 80,
                        OffsetX = xPosition,
                        OffsetY = yPosition,
                        Ports = placeholderPorts,
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
                            StrokeWidth = 3,
                            StrokeColor = "Black"
                        }
                    };
                    Nodes.Add(placeholderNode);
                    xPosition += groupSpacing;
                    continue;
                }

                // Calculate total height needed for all question containers
                var totalQuestionHeight = 0.0;
                foreach (var question in groupQuestions)
                {
                    var options = GetBranchingOptions(question);
                    // Each question needs space: header + options (if any) + spacing
                    var questionHeight = 80; // base height for question header
                    if (options.Count > 0)
                    {
                        questionHeight += options.Count * 60; // space for each option
                    }
                    totalQuestionHeight += questionHeight + 20; // add spacing between questions
                }

                var groupContainerWidth = 480.0;
                var groupContainerHeight = totalQuestionHeight + 60; // Add space for header
                var groupCenterX = xPosition;
                var groupCenterY = yPosition + (groupContainerHeight / 2);

                var groupChildrenIds = new List<string>();
                var questionYOffset = yPosition + 50; // Start below group header

                // Create question containers and option nodes
                foreach (var question in groupQuestions)
                {
                    var options = GetBranchingOptions(question);

                    var questionContainerId = $"QuestionContainer{question.Id}";
                    var optionChildrenIds = new List<string>();
                    // Calculate container height based on whether there are options
                    var questionContainerHeight = options.Count > 0 ? 80 + (options.Count * 60) : 80;
                    var questionCenterY = questionYOffset + (questionContainerHeight / 2);

                    // Create option nodes only if there are branching options
                    if (options.Count > 0)
                    {
                        var optionYOffset = questionYOffset + 60; // Start below question header
                        foreach (var (optionText, branchToGroupId, optionId) in options)
                        {
                            var targetGroupColor = GetGroupColor(branchToGroupId);
                            var optionNodeId = $"Option_Q{question.Id}_O{optionId}";

                            // Create ports for left and right sides of the option node
                            var ports = new DiagramObjectCollection<PointPort>()
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
                            };

                            var optionNode = new Node()
                            {
                                ID = optionNodeId,
                                Width = 400,
                                Height = 50,
                                OffsetX = groupCenterX,
                                OffsetY = optionYOffset + 25,
                                Ports = ports,
                                Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                                {
                                    new ShapeAnnotation()
                                    {
                                        Content = optionText,
                                        Style = new TextStyle()
                                        {
                                            Color = "black",
                                            Bold = false,
                                            FontSize = 11,
                                            TextWrapping = Syncfusion.Blazor.Diagram.TextWrap.Wrap
                                        }
                                    }
                                },
                                Style = new ShapeStyle()
                                {
                                    Fill = "white",
                                    StrokeWidth = 2,
                                    StrokeColor = targetGroupColor
                                }
                            };

                            Nodes.Add(optionNode);
                            optionChildrenIds.Add(optionNodeId);
                            
                            // Store option node position for routing calculations
                            optionNodePositions[optionNodeId] = (groupCenterX, optionYOffset + 25);
                            
                            optionYOffset += 60;
                            
                            // Connectors will be created in second pass
                        }
                    }

                    // Create question container
                    var questionContainer = new Container()
                    {
                        ID = questionContainerId,
                        Width = 440,
                        Height = questionContainerHeight,
                        OffsetX = groupCenterX,
                        OffsetY = questionCenterY,
                        Header = new ContainerHeader()
                        {
                            ID = $"QHeader{question.Id}",
                            Height = 50,
                            Annotation = new ShapeAnnotation()
                            {
                                Content = $"Q{question.QuestionNumber}: {question.Text}",
                                Style = new TextStyle()
                                {
                                    Color = "black",
                                    Bold = true,
                                    FontSize = 11,
                                    TextWrapping = Syncfusion.Blazor.Diagram.TextWrap.Wrap
                                }
                            },
                            Style = new TextStyle()
                            {
                                Fill = "#E8E8E8",
                                StrokeColor = "#999999",
                            }
                        },
                        Style = new ShapeStyle()
                        {
                            Fill = "#F5F5F5",
                            StrokeWidth = 1,
                            StrokeColor = "#CCCCCC"
                        },
                        Children = optionChildrenIds.ToArray()
                    };

                    Nodes.Add(questionContainer);
                    groupChildrenIds.Add(questionContainerId);
                    questionYOffset += questionContainerHeight + 20;
                }

                // Create ports for the group container
                var groupPorts = new DiagramObjectCollection<PointPort>()
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
                };

                // Create group container
                var groupContainer = new Container()
                {
                    ID = $"GroupContainer{group.GroupNumber}",
                    Width = groupContainerWidth,
                    Height = groupContainerHeight,
                    OffsetX = groupCenterX,
                    OffsetY = groupCenterY,
                    Ports = groupPorts,
                    Header = new ContainerHeader()
                    {
                        ID = $"GHeader{group.GroupNumber}",
                        Height = 40,
                        Annotation = new ShapeAnnotation()
                        {
                            Content = groupName,
                            Style = new TextStyle()
                            {
                                Color = "white",
                                Bold = true,
                                FontSize = 14
                            }
                        },
                        Style = new TextStyle()
                        {
                            Fill = GetGroupColor(group.GroupNumber),
                            StrokeColor = GetGroupColor(group.GroupNumber),
                        }
                    },
                    Style = new ShapeStyle()
                    {
                        Fill = GetGroupColor(group.GroupNumber),
                        Opacity = 0.15,
                        StrokeWidth = 2,
                        StrokeColor = GetGroupColor(group.GroupNumber)
                    },
                    Children = groupChildrenIds.ToArray()
                };

                Nodes.Add(groupContainer);
                
                // Store group position for connector routing calculations
                groupPositions[group.GroupNumber] = groupCenterX;
                
                xPosition += groupSpacing; // Move X for next group horizontally
            }
            
            // Second pass: Create connectors with calculated waypoints
            CreateConnectorsWithWaypoints(groupPositions, optionNodePositions);
        }

        private void CreateConnectorsWithWaypoints(Dictionary<int, double> groupPositions, Dictionary<string, (double x, double y)> optionNodePositions)
        {
            // Track connectors going to each target group
            var connectorsToTarget = new Dictionary<int, int>();
            
            foreach (var question in Survey.Questions.OrderBy(q => q.QuestionNumber))
            {
                var options = GetBranchingOptions(question);
                if (options.Count == 0) continue;

                var sourceGroupNumber = question.GroupId;

                foreach (var (optionText, branchToGroupId, optionId) in options)
                {
                    var targetGroupColor = GetGroupColor(branchToGroupId);
                    var optionNodeId = $"Option_Q{question.Id}_O{optionId}";

                    // Get positions
                    if (!optionNodePositions.ContainsKey(optionNodeId)) continue;

                    // Determine source and target ports
                    var sourcePortId = DetermineSourcePort(sourceGroupNumber, branchToGroupId);
                    var targetPortId = DetermineTargetPort(sourceGroupNumber, branchToGroupId);

                    // Track connector count for this target
                    if (!connectorsToTarget.ContainsKey(branchToGroupId))
                    {
                        connectorsToTarget[branchToGroupId] = 0;
                    }
                    var connectorIndex = connectorsToTarget[branchToGroupId];
                    connectorsToTarget[branchToGroupId]++;

                    // Create connector with orthogonal routing
                    // Don't specify segments - let Syncfusion calculate the path
                    // Ports guide the routing direction
                    var connector = new Connector()
                    {
                        ID = $"Connector_Q{question.Id}_O{optionId}_To_Group{branchToGroupId}",
                        SourceID = optionNodeId,
                        SourcePortID = sourcePortId,
                        TargetID = $"GroupContainer{branchToGroupId}",
                        TargetPortID = targetPortId,
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
