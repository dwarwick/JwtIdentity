namespace JwtIdentity.Client.Pages.Survey.Charts
{
    public class BarChartModel : BlazorBase
    {
        [CascadingParameter(Name = "Theme")]
        public string Theme { get; set; }

        [Parameter]
        public string SurveyId { get; set; }

        protected List<SurveyDataViewModel> SurveyData { get; set; } = new();

        protected List<ChartData> SurveyChartData { get; set; } = new List<ChartData>();

        protected QuestionViewModel SelectedQuestion { get; set; }

        protected Theme CurrentTheme => Theme == "dark" ? Syncfusion.Blazor.Theme.Tailwind3Dark : Syncfusion.Blazor.Theme.Material3;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            SurveyData = await ApiService.GetAsync<List<SurveyDataViewModel>>($"api/answer/getanswersforsurveyforCharts/{SurveyId}");

            if (SurveyData != null && SurveyData.Count > 0)
            {
                SelectedQuestion = SurveyData[0].Question;
                HandleSelectQuestion(SelectedQuestion);
            }
        }

        protected void HandleSelectQuestion(QuestionViewModel question)
        {
            SelectedQuestion = question;

            SurveyChartData = SurveyData.Where(x => x.Question == question).Select(x => x.SurveyData).FirstOrDefault();
        }
    }
}
