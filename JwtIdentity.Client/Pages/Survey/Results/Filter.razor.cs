using Syncfusion.Blazor.Grids;

namespace JwtIdentity.Client.Pages.Survey.Results
{
    public class FilterModel : BlazorBase
    {
        [Parameter]
        public string SurveyId { get; set; }

        protected SfGrid<object> Grid { get; set; }

        protected SurveyViewModel Survey { get; set; }

        protected List<AnswerViewModel> Answers { get; set; } = new();

        protected List<SurveyResponseViewModel> SurveyResponses = new List<SurveyResponseViewModel>();

        protected Type _surveyType { get; set; }


        private Dictionary<int, string> _propertyMap;
        protected List<object> SurveyRows { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            Survey = await ApiService.GetAsync<SurveyViewModel>($"api/answer/getsurveyresults/{SurveyId}");

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

            AddColumnsDynamically();

            // 3) Get the raw answers from DB


            // 4) Build row objects of the dynamic type
            SurveyRows = BuildSurveyRows(
                _surveyType,
                _propertyMap,
                Answers).ToList();
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

        private void AddColumnsDynamically()
        {
            foreach (var q in Survey.Questions)
            {
                // For question ID 29, property name is "Q_29"
                string propertyName = _propertyMap[q.Id];

#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
                var gridColumn = new GridColumn
                {
                    Field = propertyName,
                    HeaderText = q.Text
                };


                // Decide the column type based on the question type
                switch (q.QuestionType)
                {
                    case QuestionType.Text:
                    case QuestionType.MultipleChoice:
                    case QuestionType.SelectAllThatApply:
                        gridColumn.Type = ColumnType.String;
                        break;

                    case QuestionType.TrueFalse:
                        gridColumn.Type = ColumnType.Boolean;
                        break;

                    case QuestionType.Rating1To10:
                        gridColumn.Type = ColumnType.Integer;
                        break;
                }
#pragma warning restore BL0005 // Component parameter should not be set outside of its component.
                Grid.Columns.Add(gridColumn);
            }
        }

        protected async Task ToolbarClickHandler(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Export to Excel") //Id is combination of Grid's ID and itemname
            {
                await this.Grid.ExportToExcelAsync();
            }
        }
    }
}
