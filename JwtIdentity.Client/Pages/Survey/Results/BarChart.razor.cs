using Syncfusion.Blazor.Charts;

namespace JwtIdentity.Client.Pages.Survey.Results
{
    public class BarChartModel : BlazorBase
    {
        [CascadingParameter(Name = "Theme")]
        public string Theme { get; set; }

        [Parameter]
        public string SurveyId { get; set; }

        protected SfChart chartObj;

        protected List<SurveyDataViewModel> SurveyData { get; set; } = new();

        protected List<ChartData> SurveyChartData { get; set; } = new List<ChartData>();

        protected QuestionViewModel SelectedQuestion { get; set; }

        protected Theme CurrentTheme => Theme == "dark" ? Syncfusion.Blazor.Theme.Tailwind3Dark : Syncfusion.Blazor.Theme.Material3;

        protected string ChartWidth { get; set; } = "100%";

        protected string ChartHeight { get; set; } = "100%";

        protected ExportType SelectedExportType { get; set; } = ExportType.PDF;

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

        protected async Task ExportChart()
        {
            ChartWidth = "1000";
            ChartHeight = "650";

            await chartObj.ExportAsync(SelectedExportType, $"Chart.{SelectedExportType}", Syncfusion.PdfExport.PdfPageOrientation.Landscape, true);

            ChartWidth = "100%";
            ChartHeight = "100%";
        }
    }
}
