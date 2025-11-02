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
        protected double ZoomLevel { get; set; } = 1.0;

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

            // Create nodes for each group - let the layout algorithm position them
            foreach (var group in QuestionGroups.OrderBy(g => g.GroupNumber))
            {
                var questionCount = Survey.Questions.Count(q => q.GroupId == group.GroupNumber);
                var groupName = string.IsNullOrWhiteSpace(group.GroupName) ? $"Group {group.GroupNumber}" : group.GroupName;

                // Get all questions with branching rules in this group
                var groupQuestions = Survey.Questions
                    .Where(q => q.GroupId == group.GroupNumber &&
                        (q.QuestionType == QuestionType.MultipleChoice ||
                         q.QuestionType == QuestionType.SelectAllThatApply ||
                         q.QuestionType == QuestionType.TrueFalse))
                    .OrderBy(q => q.QuestionNumber)
                    .ToList();

                // Build annotation content for the node with question and options
                var annotationContent = BuildGroupNodeContent(group, groupName, questionCount, groupQuestions);

                // Calculate height based on content
                var nodeHeight = CalculateNodeHeight(groupQuestions);

                // Create primary node for the group
                var groupNode = new Node()
                {
                    ID = $"Group{group.GroupNumber}",
                    Width = 300,
                    Height = nodeHeight,
                    Annotations = new DiagramObjectCollection<ShapeAnnotation>()
                    {
                        new ShapeAnnotation()
                        {
                            Content = annotationContent,
                            Style = new TextStyle() { Color = "white", Bold = false, TextWrapping = Syncfusion.Blazor.Diagram.TextWrap.Wrap }
                        }
                    },
                    Style = new ShapeStyle()
                    {
                        Fill = GetGroupColor(group.GroupNumber),
                        StrokeWidth = 3,
                        StrokeColor = "Black"
                    },
                    Ports = CreatePortsForOptions(group, groupQuestions)
                };

                Nodes.Add(groupNode);

                // Create connectors from option ports to destination groups
                CreateConnectorsForOptions(group, groupQuestions);
            }
        }

        private string BuildGroupNodeContent(QuestionGroupViewModel group, string groupName, int questionCount, List<QuestionViewModel> groupQuestions)
        {
            var content = new System.Text.StringBuilder();
            
            // Group header with bold styling
            content.AppendLine($"═══ {groupName} ═══");
            content.AppendLine($"({questionCount} question{(questionCount != 1 ? "s" : "")})");
            content.AppendLine();
            
            // Questions with branching rules
            if (groupQuestions.Any())
            {
                foreach (var question in groupQuestions)
                {
                    // Question text - NOT truncated as per requirements
                    content.AppendLine($"● Q{question.QuestionNumber}: {question.Text}");
                    content.AppendLine();
                    
                    // Options with branching - grouped under the question
                    if (question.QuestionType == QuestionType.MultipleChoice)
                    {
                        var mcQuestion = question as MultipleChoiceQuestionViewModel;
                        if (mcQuestion?.Options != null)
                        {
                            foreach (var option in mcQuestion.Options.Where(o => o.BranchToGroupId.HasValue))
                            {
                                var truncatedOption = TruncateText(option.OptionText, 35);
                                var targetGroup = QuestionGroups.FirstOrDefault(g => g.GroupNumber == option.BranchToGroupId.Value);
                                var targetGroupName = targetGroup != null && !string.IsNullOrWhiteSpace(targetGroup.GroupName) 
                                    ? targetGroup.GroupName 
                                    : $"Group {option.BranchToGroupId.Value}";
                                content.AppendLine($"   ▸ {truncatedOption} → {targetGroupName}");
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
                                var truncatedOption = TruncateText(option.OptionText, 35);
                                var targetGroup = QuestionGroups.FirstOrDefault(g => g.GroupNumber == option.BranchToGroupId.Value);
                                var targetGroupName = targetGroup != null && !string.IsNullOrWhiteSpace(targetGroup.GroupName) 
                                    ? targetGroup.GroupName 
                                    : $"Group {option.BranchToGroupId.Value}";
                                content.AppendLine($"   ▸ {truncatedOption} → {targetGroupName}");
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
                                var targetGroup = QuestionGroups.FirstOrDefault(g => g.GroupNumber == tfQuestion.BranchToGroupIdOnTrue.Value);
                                var targetGroupName = targetGroup != null && !string.IsNullOrWhiteSpace(targetGroup.GroupName) 
                                    ? targetGroup.GroupName 
                                    : $"Group {tfQuestion.BranchToGroupIdOnTrue.Value}";
                                content.AppendLine($"   ▸ True → {targetGroupName}");
                            }
                            if (tfQuestion.BranchToGroupIdOnFalse.HasValue)
                            {
                                var targetGroup = QuestionGroups.FirstOrDefault(g => g.GroupNumber == tfQuestion.BranchToGroupIdOnFalse.Value);
                                var targetGroupName = targetGroup != null && !string.IsNullOrWhiteSpace(targetGroup.GroupName) 
                                    ? targetGroup.GroupName 
                                    : $"Group {tfQuestion.BranchToGroupIdOnFalse.Value}";
                                content.AppendLine($"   ▸ False → {targetGroupName}");
                            }
                        }
                    }
                    
                    content.AppendLine();
                }
            }
            
            return content.ToString().TrimEnd();
        }

        private double CalculateNodeHeight(List<QuestionViewModel> groupQuestions)
        {
            // Base height for group header
            double height = 80;
            
            // Add height for each question with options
            foreach (var question in groupQuestions)
            {
                // Question text height
                height += 50;
                
                // Option heights
                if (question.QuestionType == QuestionType.MultipleChoice)
                {
                    var mcQuestion = question as MultipleChoiceQuestionViewModel;
                    if (mcQuestion?.Options != null)
                    {
                        height += mcQuestion.Options.Count(o => o.BranchToGroupId.HasValue) * 35;
                    }
                }
                else if (question.QuestionType == QuestionType.SelectAllThatApply)
                {
                    var saQuestion = question as SelectAllThatApplyQuestionViewModel;
                    if (saQuestion?.Options != null)
                    {
                        height += saQuestion.Options.Count(o => o.BranchToGroupId.HasValue) * 35;
                    }
                }
                else if (question.QuestionType == QuestionType.TrueFalse)
                {
                    var tfQuestion = question as TrueFalseQuestionViewModel;
                    if (tfQuestion != null)
                    {
                        if (tfQuestion.BranchToGroupIdOnTrue.HasValue) height += 35;
                        if (tfQuestion.BranchToGroupIdOnFalse.HasValue) height += 35;
                    }
                }
            }
            
            // Min and max height constraints
            return Math.Max(100, Math.Min(height, 600));
        }

        private DiagramObjectCollection<PointPort> CreatePortsForOptions(QuestionGroupViewModel group, List<QuestionViewModel> groupQuestions)
        {
            var ports = new DiagramObjectCollection<PointPort>();
            int portIndex = 0;
            
            foreach (var question in groupQuestions)
            {
                if (question.QuestionType == QuestionType.MultipleChoice)
                {
                    var mcQuestion = question as MultipleChoiceQuestionViewModel;
                    if (mcQuestion?.Options != null)
                    {
                        foreach (var option in mcQuestion.Options.Where(o => o.BranchToGroupId.HasValue))
                        {
                            ports.Add(new PointPort()
                            {
                                ID = $"port_mc_{option.Id}",
                                Offset = new DiagramPoint() { X = 1, Y = 0.2 + (portIndex * 0.15) },
                                Visibility = PortVisibility.Visible,
                                Width = 8,
                                Height = 8,
                                Style = new ShapeStyle() { Fill = GetGroupColor(option.BranchToGroupId.Value), StrokeColor = GetGroupColor(option.BranchToGroupId.Value) }
                            });
                            portIndex++;
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
                            ports.Add(new PointPort()
                            {
                                ID = $"port_sa_{option.Id}",
                                Offset = new DiagramPoint() { X = 1, Y = 0.2 + (portIndex * 0.15) },
                                Visibility = PortVisibility.Visible,
                                Width = 8,
                                Height = 8,
                                Style = new ShapeStyle() { Fill = GetGroupColor(option.BranchToGroupId.Value), StrokeColor = GetGroupColor(option.BranchToGroupId.Value) }
                            });
                            portIndex++;
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
                            ports.Add(new PointPort()
                            {
                                ID = $"port_tf_{question.Id}_true",
                                Offset = new DiagramPoint() { X = 1, Y = 0.2 + (portIndex * 0.15) },
                                Visibility = PortVisibility.Visible,
                                Width = 8,
                                Height = 8,
                                Style = new ShapeStyle() { Fill = GetGroupColor(tfQuestion.BranchToGroupIdOnTrue.Value), StrokeColor = GetGroupColor(tfQuestion.BranchToGroupIdOnTrue.Value) }
                            });
                            portIndex++;
                        }
                        if (tfQuestion.BranchToGroupIdOnFalse.HasValue)
                        {
                            ports.Add(new PointPort()
                            {
                                ID = $"port_tf_{question.Id}_false",
                                Offset = new DiagramPoint() { X = 1, Y = 0.2 + (portIndex * 0.15) },
                                Visibility = PortVisibility.Visible,
                                Width = 8,
                                Height = 8,
                                Style = new ShapeStyle() { Fill = GetGroupColor(tfQuestion.BranchToGroupIdOnFalse.Value), StrokeColor = GetGroupColor(tfQuestion.BranchToGroupIdOnFalse.Value) }
                            });
                            portIndex++;
                        }
                    }
                }
            }
            
            return ports;
        }

        private void CreateConnectorsForOptions(QuestionGroupViewModel group, List<QuestionViewModel> groupQuestions)
        {
            foreach (var question in groupQuestions)
            {
                if (question.QuestionType == QuestionType.MultipleChoice)
                {
                    var mcQuestion = question as MultipleChoiceQuestionViewModel;
                    if (mcQuestion?.Options != null)
                    {
                        foreach (var option in mcQuestion.Options.Where(o => o.BranchToGroupId.HasValue))
                        {
                            var targetGroupColor = GetGroupColor(option.BranchToGroupId.Value);
                            var connector = new Connector()
                            {
                                ID = $"Connector_MC_O{option.Id}_To_Group{option.BranchToGroupId}",
                                SourceID = $"Group{group.GroupNumber}",
                                SourcePortID = $"port_mc_{option.Id}",
                                TargetID = $"Group{option.BranchToGroupId}",
                                Type = ConnectorSegmentType.Bezier,
                                Style = new ShapeStyle() { StrokeColor = targetGroupColor, StrokeWidth = 3 },
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
                else if (question.QuestionType == QuestionType.SelectAllThatApply)
                {
                    var saQuestion = question as SelectAllThatApplyQuestionViewModel;
                    if (saQuestion?.Options != null)
                    {
                        foreach (var option in saQuestion.Options.Where(o => o.BranchToGroupId.HasValue))
                        {
                            var targetGroupColor = GetGroupColor(option.BranchToGroupId.Value);
                            var connector = new Connector()
                            {
                                ID = $"Connector_SA_O{option.Id}_To_Group{option.BranchToGroupId}",
                                SourceID = $"Group{group.GroupNumber}",
                                SourcePortID = $"port_sa_{option.Id}",
                                TargetID = $"Group{option.BranchToGroupId}",
                                Type = ConnectorSegmentType.Bezier,
                                Style = new ShapeStyle() { StrokeColor = targetGroupColor, StrokeWidth = 3 },
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
                else if (question.QuestionType == QuestionType.TrueFalse)
                {
                    var tfQuestion = question as TrueFalseQuestionViewModel;
                    if (tfQuestion != null)
                    {
                        if (tfQuestion.BranchToGroupIdOnTrue.HasValue)
                        {
                            var targetGroupColor = GetGroupColor(tfQuestion.BranchToGroupIdOnTrue.Value);
                            var connector = new Connector()
                            {
                                ID = $"Connector_TF_{question.Id}_True_To_Group{tfQuestion.BranchToGroupIdOnTrue}",
                                SourceID = $"Group{group.GroupNumber}",
                                SourcePortID = $"port_tf_{question.Id}_true",
                                TargetID = $"Group{tfQuestion.BranchToGroupIdOnTrue}",
                                Type = ConnectorSegmentType.Bezier,
                                Style = new ShapeStyle() { StrokeColor = targetGroupColor, StrokeWidth = 3 },
                                TargetDecorator = new DecoratorSettings()
                                {
                                    Shape = DecoratorShape.Arrow,
                                    Style = new ShapeStyle() { Fill = targetGroupColor, StrokeColor = targetGroupColor }
                                }
                            };
                            Connectors.Add(connector);
                        }
                        if (tfQuestion.BranchToGroupIdOnFalse.HasValue)
                        {
                            var targetGroupColor = GetGroupColor(tfQuestion.BranchToGroupIdOnFalse.Value);
                            var connector = new Connector()
                            {
                                ID = $"Connector_TF_{question.Id}_False_To_Group{tfQuestion.BranchToGroupIdOnFalse}",
                                SourceID = $"Group{group.GroupNumber}",
                                SourcePortID = $"port_tf_{question.Id}_false",
                                TargetID = $"Group{tfQuestion.BranchToGroupIdOnFalse}",
                                Type = ConnectorSegmentType.Bezier,
                                Style = new ShapeStyle() { StrokeColor = targetGroupColor, StrokeWidth = 3 },
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
    }
}
