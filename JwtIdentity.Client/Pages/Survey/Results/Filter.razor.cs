using Syncfusion.Blazor.Grids;

namespace JwtIdentity.Client.Pages.Survey.Results
{
    public class FilterModel : BlazorBase
    {
        [Parameter]
        public string SurveyId { get; set; }

        protected SfGrid<object> Grid { get; set; }

        // Handle grid actions (filtering, sorting, paging) to recalc counts for displayed rows
        protected async Task OnActionCompleteHandler(ActionEventArgs<object> args)
        {
            if (args.RequestType == Syncfusion.Blazor.Grids.Action.Filtering
             || args.RequestType == Syncfusion.Blazor.Grids.Action.Sorting
             || args.RequestType == Syncfusion.Blazor.Grids.Action.Paging)
            {
                var viewRecords = await Grid.GetCurrentViewRecordsAsync();
                ComputeOptionCountsFromRows(viewRecords);
                StateHasChanged();
            }
        }

        protected SurveyViewModel Survey { get; set; }

        protected List<AnswerViewModel> Answers { get; set; } = new();

        protected List<SurveyResponseViewModel> SurveyResponses = new List<SurveyResponseViewModel>();

        protected Type _surveyType { get; set; }

        protected Dictionary<int, string> _propertyMap;
        protected List<object> SurveyRows { get; set; }

        // Add private flag for column initialization
        protected bool columnsInitialized = false;

        protected Dictionary<int, Dictionary<int, int>> OptionCounts { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            Survey = await ApiService.GetAsync<SurveyViewModel>($"{ApiEndpoints.Answer}/getsurveyresults/{SurveyId}");

            StateHasChanged();

            // 1) Suppose we fetch the dynamic list of questions from somewhere
            var questions = Survey.Questions;

            // 2) Generate the dynamic type
            (_surveyType, _propertyMap) = DynamicSurveyTypeBuilder.BuildSurveyType(questions);

            // 3) Get the raw answers from DB
            foreach (var question in Survey.Questions)
            {
                Answers.AddRange(question.Answers);
            }

            //  AddColumnsDynamically();

            // 3) Get the raw answers from DB

            // 4) Build row objects of the dynamic type
            SurveyRows = BuildSurveyRows(
                _surveyType,
                _propertyMap,
                Answers).ToList();

            // Compute counts for each option based on displayed rows
            ComputeOptionCountsFromRows(SurveyRows);

            columnsInitialized = true;

            await Grid.Refresh();
        }

        // Suppose we have a method to build "row objects" for each response
        private IEnumerable<object> BuildSurveyRows(Type dynamicSurveyType, Dictionary<int, string> propertyMap, List<AnswerViewModel> answersForAllResponses)
        {
            // Group answers by some "ResponseId" or "IpAddress" etc.
            var grouped = answersForAllResponses
                .GroupBy(a => a.CreatedById); // or .GroupBy(a => a.IpAddress)

            var rowObjects = new List<object>();

            foreach (var responseGroup in grouped)
            {
                // Create an instance of the dynamic type
                object rowInstance = Activator.CreateInstance(dynamicSurveyType)!;

                // For each answer in this response, find the corresponding property
                foreach (var ans in responseGroup)
                {
                    if (propertyMap.TryGetValue(ans.QuestionId, out string propName))
                    {
                        // This means "Q_29" or "Q_42", etc.

                        // We'll figure out how to convert 'ans' to the correct property value
                        object propValue = ConvertAnswerValue(ans);

                        // Finally set the property on rowInstance
                        dynamicSurveyType.GetProperty(propName)?.SetValue(rowInstance, propValue);
                    }
                }

                rowObjects.Add(rowInstance);
            }

            return rowObjects;
        }

        private object ConvertAnswerValue(AnswerViewModel ans)
        {
            // You might switch on ans.AnswerType or check the derived class
            // to return the correct .NET type (string, bool?, int?, etc.)
            switch (ans.AnswerType)
            {
                case AnswerType.Text:
                    // Assume 'TextValue' is your property on TextAnswerViewModel
                    return (ans as TextAnswerViewModel)?.Text;

                case AnswerType.TrueFalse:
                    return (ans as TrueFalseAnswerViewModel)?.Value;

                case AnswerType.Rating1To10:
                    return (ans as Rating1To10AnswerViewModel)?.SelectedOptionId;

                case AnswerType.MultipleChoice:
                case AnswerType.SingleChoice:

                    var question = Survey.Questions
                .OfType<MultipleChoiceQuestionViewModel>()
                .FirstOrDefault(q => q.Id == ans.QuestionId);

                    if (question != null)
                    {
                        var option = question.Options
                            .FirstOrDefault(o => o.Id == ans.SelectedOptionValue);

                        return option?.OptionText;
                    }
                    return null;

                case AnswerType.SelectAllThatApply:
                    var selectAllQuestion = Survey.Questions
                        .OfType<SelectAllThatApplyQuestionViewModel>()
                        .FirstOrDefault(q => q.Id == ans.QuestionId);

                    if (selectAllQuestion != null && ans is SelectAllThatApplyAnswerViewModel selectAllAns)
                    {
                        if (string.IsNullOrEmpty(selectAllAns.SelectedOptionIds))
                            return null;

                        // Get the IDs of the selected options
                        var selectedIds = selectAllAns.SelectedOptionIds
                            .Split(',')
                            .Select(int.Parse)
                            .ToHashSet();

                        // Get the text of the selected options
                        var selectedOptions = selectAllQuestion.Options
                            .Where(o => selectedIds.Contains(o.Id))
                            .Select(o => o.OptionText);

                        // Join them with commas for display
                        return string.Join(", ", selectedOptions);
                    }
                    return null;

                default:
                    return null;
            }
        }

        private void ComputeOptionCounts()
        {
            OptionCounts.Clear();
            // Initialize counts for multiple choice questions
            foreach (var mcq in Survey.Questions.OfType<MultipleChoiceQuestionViewModel>())
            {
                OptionCounts[mcq.Id] = mcq.Options.ToDictionary(o => o.Id, o => 0);
            }
            // Initialize counts for select-all-that-apply questions
            foreach (var saq in Survey.Questions.OfType<SelectAllThatApplyQuestionViewModel>())
            {
                OptionCounts[saq.Id] = saq.Options.ToDictionary(o => o.Id, o => 0);
            }
            // Tally each answer
            foreach (var ans in Answers)
            {
                if ((ans.AnswerType == AnswerType.MultipleChoice || ans.AnswerType == AnswerType.SingleChoice)
                    && ans.SelectedOptionValue.HasValue
                    && OptionCounts.TryGetValue(ans.QuestionId, out var dict))
                {
                    int optionId = ans.SelectedOptionValue.Value;
                    if (dict.ContainsKey(optionId))
                        dict[optionId]++;
                }
                else if (ans.AnswerType == AnswerType.SelectAllThatApply && ans is SelectAllThatApplyAnswerViewModel sel)
                {
                    if (!string.IsNullOrEmpty(sel.SelectedOptionIds) && OptionCounts.TryGetValue(ans.QuestionId, out var dict2))
                    {
                        var ids = sel.SelectedOptionIds.Split(',').Select(int.Parse);
                        foreach (var id in ids)
                        {
                            if (dict2.ContainsKey(id))
                                dict2[id]++;
                        }
                    }
                }
            }
        }

        // Compute counts for each option based on a set of dynamic row objects
        private void ComputeOptionCountsFromRows(IEnumerable<object> rows)
        {
            OptionCounts.Clear();
            // Initialize counts for multiple choice and select-all questions
            foreach (var mcq in Survey.Questions.OfType<MultipleChoiceQuestionViewModel>())
                OptionCounts[mcq.Id] = mcq.Options.ToDictionary(o => o.Id, o => 0);
            foreach (var saq in Survey.Questions.OfType<SelectAllThatApplyQuestionViewModel>())
                OptionCounts[saq.Id] = saq.Options.ToDictionary(o => o.Id, o => 0);
            // Initialize counts for true/false questions (true=1, false=0)
            foreach (var tf in Survey.Questions.OfType<TrueFalseQuestionViewModel>())
                OptionCounts[tf.Id] = new Dictionary<int, int> { { 1, 0 }, { 0, 0 } };
            // Initialize counts for rating questions (1-10)
            foreach (var rt in Survey.Questions.OfType<Rating1To10QuestionViewModel>())
                OptionCounts[rt.Id] = Enumerable.Range(1, 10).ToDictionary(v => v, v => 0);
            
            // Tally based on displayed rows
            foreach (var row in rows)
            {
                // multiple choice
                foreach (var mcq in Survey.Questions.OfType<MultipleChoiceQuestionViewModel>())
                {
                    var propName = _propertyMap[mcq.Id];
                    var val = _surveyType.GetProperty(propName)?.GetValue(row) as string;
                    if (!string.IsNullOrEmpty(val))
                    {
                        var option = mcq.Options.FirstOrDefault(o => o.OptionText == val);
                        if (option != null)
                            OptionCounts[mcq.Id][option.Id]++;
                    }
                }
                // select-all-that-apply
                foreach (var saq in Survey.Questions.OfType<SelectAllThatApplyQuestionViewModel>())
                {
                    var propName = _propertyMap[saq.Id];
                    var val = _surveyType.GetProperty(propName)?.GetValue(row) as string;
                    if (!string.IsNullOrEmpty(val))
                    {
                        var texts = val.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var text in texts)
                        {
                            var option = saq.Options.FirstOrDefault(o => o.OptionText == text);
                            if (option != null)
                                OptionCounts[saq.Id][option.Id]++;
                        }
                    }
                }
                // true/false
                foreach (var tf in Survey.Questions.OfType<TrueFalseQuestionViewModel>())
                {
                    var propName = _propertyMap[tf.Id];
                    var val = _surveyType.GetProperty(propName)?.GetValue(row) as bool?;
                    if (val.HasValue)
                    {
                        int key = val.Value ? 1 : 0;
                        OptionCounts[tf.Id][key]++;
                    }
                }
                // rating 1-10
                foreach (var rt in Survey.Questions.OfType<Rating1To10QuestionViewModel>())
                {
                    var propName = _propertyMap[rt.Id];
                    var val = _surveyType.GetProperty(propName)?.GetValue(row) as int?;
                    if (val.HasValue && OptionCounts[rt.Id].ContainsKey(val.Value))
                        OptionCounts[rt.Id][val.Value]++;
                }
            }
        }

        protected async Task ToolbarClickHandler(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Export to Excel") //Id is combination of Grid's ID and itemname
            {
                await this.Grid.ExportToExcelAsync();
            }
        }

        private RenderFragment AddContent(string context) => builder =>
        {
            builder.AddContent(1, context);
        };
    }
}
