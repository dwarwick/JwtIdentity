using Syncfusion.Blazor.Grids;
using System.Dynamic;
using Action = Syncfusion.Blazor.Grids.Action;

namespace JwtIdentity.Client.Pages.Survey.Results
{
    public class FilterModel : BlazorBase
    {
        [Parameter]
        public string SurveyId { get; set; }

        protected SfGrid<ExpandoObject> Grid { get; set; }

        protected SurveyViewModel Survey { get; set; }

        protected Dictionary<int, string> _propertyMap;
        protected List<ExpandoObject> SurveyRows { get; set; } = new();

        protected bool columnsInitialized { get; set; } = false;

        protected Dictionary<int, Dictionary<int, int>> OptionCounts { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();

        }

        protected async Task ToolbarClickHandler(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Export to Excel") //Id is combination of Grid's ID and itemname
            {
                await this.Grid.ExportToExcelAsync();
            }
        }

        // 1) Hook DataBound
        protected Task OnGridDataBound()
            => RefreshCountsAsync();

        // 2) Hook OnActionComplete
        protected Task OnActionCompleteHandler(ActionEventArgs<ExpandoObject> args)
        {
            if (args.RequestType == Action.Filtering
             || args.RequestType == Action.Sorting
             || args.RequestType == Action.Paging)
            {
                return RefreshCountsAsync();
            }
            return Task.CompletedTask;
        }

        private async Task LoadData()
        {
            // 1) fetch survey + answers
            Survey = await ApiService.GetAsync<SurveyViewModel>($"{ApiEndpoints.Answer}/getsurveyresults/{SurveyId}");

            // 2) build the field‑name map
            _propertyMap = Survey.Questions.ToDictionary(
                q => q.Id,
                q => $"Q_{q.Id}"
            );

            // 3) flatten all answers
            var allAnswers = Survey.Questions
                                   .SelectMany(q => q.Answers)
                                   .ToList();

            // 4) build ExpandoObject rows
            SurveyRows = BuildSurveyRowsAsExpando(Survey.Questions, allAnswers).ToList();

            ComputeOptionCountsFromRows(SurveyRows);

            columnsInitialized = true;

            StateHasChanged();

            // 5) refresh grid so it picks up columns & data
            await Grid.Refresh();
        }

        private IEnumerable<ExpandoObject> BuildSurveyRowsAsExpando(
                List<QuestionViewModel> questions,
                List<AnswerViewModel> allAnswers)
        {
            // group answers by response
            var groups = allAnswers.GroupBy(a => a.CreatedById);

            foreach (var grp in groups)
            {
                dynamic expando = new ExpandoObject();
                var dict = (IDictionary<string, object?>)expando;

                // initialize *all* properties to null (so grid sees them up front)
                foreach (var q in questions)
                    dict[_propertyMap[q.Id]] = null;

                // populate actual answers
                foreach (var ans in grp)
                {
                    var propName = _propertyMap[ans.QuestionId];
                    dict[propName] = ConvertAnswerValue(ans);
                }

                yield return expando;
            }
        }

        private object? ConvertAnswerValue(AnswerViewModel ans)
        {
            return ans.AnswerType switch
            {
                AnswerType.Text => (ans as TextAnswerViewModel)?.Text,
                AnswerType.TrueFalse => (ans as TrueFalseAnswerViewModel)?.Value,
                AnswerType.Rating1To10 => (ans as Rating1To10AnswerViewModel)?.SelectedOptionId,
                AnswerType.MultipleChoice or AnswerType.SingleChoice
                    => GetOptionText(ans as MultipleChoiceAnswerViewModel),
                AnswerType.SelectAllThatApply
                    => GetSelectAllText(ans as SelectAllThatApplyAnswerViewModel),
                _ => null
            };
        }

        private string? GetOptionText(MultipleChoiceAnswerViewModel? m)
        {
            if (m == null) return null;

            // find the question in Survey.Questions, not on the answer object
            var question = Survey.Questions
                                 .OfType<MultipleChoiceQuestionViewModel>()
                                 .FirstOrDefault(q => q.Id == m.QuestionId);
            if (question == null) return null;

            var option = question.Options
                                 .FirstOrDefault(o => o.Id == m.SelectedOptionId);
            return option?.OptionText;
        }

        private string? GetSelectAllText(SelectAllThatApplyAnswerViewModel? s)
        {
            if (s == null || string.IsNullOrEmpty(s.SelectedOptionIds))
                return null;

            // find the question
            var question = Survey.Questions
                                 .OfType<SelectAllThatApplyQuestionViewModel>()
                                 .FirstOrDefault(q => q.Id == s.QuestionId);
            if (question == null) return null;

            var selectedIds = s.SelectedOptionIds
                               .Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(int.Parse)
                               .ToHashSet();

            // map back to the question’s options
            var texts = question.Options
                                .Where(o => selectedIds.Contains(o.Id))
                                .Select(o => o.OptionText);

            return string.Join(", ", texts);
        }

        // Compute counts for each option based on a set of dynamic row objects
        private void ComputeOptionCountsFromRows(IEnumerable<ExpandoObject> rows)
        {
            // 1) clear & re‑init the bins
            OptionCounts.Clear();
            foreach (var mcq in Survey.Questions.OfType<MultipleChoiceQuestionViewModel>())
                OptionCounts[mcq.Id] = mcq.Options.ToDictionary(o => o.Id, o => 0);
            foreach (var saq in Survey.Questions.OfType<SelectAllThatApplyQuestionViewModel>())
                OptionCounts[saq.Id] = saq.Options.ToDictionary(o => o.Id, o => 0);
            foreach (var tf in Survey.Questions.OfType<TrueFalseQuestionViewModel>())
                OptionCounts[tf.Id] = new Dictionary<int, int> { { 1, 0 }, { 0, 0 } };
            foreach (var rt in Survey.Questions.OfType<Rating1To10QuestionViewModel>())
                OptionCounts[rt.Id] = Enumerable.Range(1, 10).ToDictionary(i => i, _ => 0);

            // 2) tally
            foreach (var row in rows)
            {
                var dict = (IDictionary<string, object?>)row;

                // multiple‑choice
                foreach (var mcq in Survey.Questions.OfType<MultipleChoiceQuestionViewModel>())
                {
                    var key = _propertyMap[mcq.Id];
                    if (dict.TryGetValue(key, out var obj) && obj is string str && !string.IsNullOrWhiteSpace(str))
                    {
                        var opt = mcq.Options.FirstOrDefault(o => o.OptionText == str);
                        if (opt != null)
                            OptionCounts[mcq.Id][opt.Id]++;
                    }
                }

                // ── SELECT‑ALL‑THAT‑APPLY ──
                foreach (var saq in Survey.Questions.OfType<SelectAllThatApplyQuestionViewModel>())
                {
                    var key = _propertyMap[saq.Id];
                    if (dict.TryGetValue(key, out var raw)
                        && raw is string csv
                        && !string.IsNullOrWhiteSpace(csv))
                    {
                        // split on comma, trim each piece
                        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var text = part.Trim();
                            // find matching option by text
                            var opt = saq.Options.FirstOrDefault(o => o.OptionText == text);
                            if (opt != null)
                                OptionCounts[saq.Id][opt.Id]++;
                        }
                    }
                }

                // true/false
                foreach (var tf in Survey.Questions.OfType<TrueFalseQuestionViewModel>())
                {
                    var key = _propertyMap[tf.Id];
                    if (dict.TryGetValue(key, out var obj) && obj is bool b)
                        OptionCounts[tf.Id][b ? 1 : 0]++;
                }

                // rating 1–10
                foreach (var rt in Survey.Questions.OfType<Rating1To10QuestionViewModel>())
                {
                    var key = _propertyMap[rt.Id];
                    if (dict.TryGetValue(key, out var obj) && obj is int v && OptionCounts[rt.Id].ContainsKey(v))
                        OptionCounts[rt.Id][v]++;
                }
            }
        }

        // call this whenever the grid renders or finishes an action
        private async Task RefreshCountsAsync()
        {
            // Grab whatever rows are showing (empty if none)
            var view = await Grid.GetCurrentViewRecordsAsync()
                       ?? Enumerable.Empty<ExpandoObject>();

            ComputeOptionCountsFromRows(view);
            StateHasChanged();
        }
    }
}
