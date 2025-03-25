using Syncfusion.Blazor.Charts;

namespace JwtIdentity.Client.Pages.Survey.Results
{
    public class BarChartModel : BlazorBase
    {
        [CascadingParameter(Name = "Theme")]
        public string Theme { get; set; }

        [Parameter]
        public string SurveyId { get; set; }

        protected SfChart chartObj { get; set; }

        protected SfAccumulationChart pieChartObj { get; set; }

        protected List<SurveyDataViewModel> SurveyData { get; set; } = new();

        protected List<ChartData> SurveyChartData { get; set; } = new List<ChartData>();

        protected List<ChartData> PieChartData { get; set; } = new List<ChartData>();

        protected QuestionViewModel SelectedQuestion { get; set; }

        protected Theme CurrentTheme => Theme == "dark" ? Syncfusion.Blazor.Theme.Tailwind3Dark : Syncfusion.Blazor.Theme.Material3;

        protected string ChartWidth { get; set; } = "100%";

        protected string ChartHeight { get; set; } = "100%";

        protected ExportType SelectedExportType { get; set; } = ExportType.PDF;

        protected string SelectedChartType { get; set; } = "Bar";

        protected override async Task OnInitializedAsync()
        {
            await LoadData();

            StateHasChanged();
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

            PieChartData = new List<ChartData>();

            foreach (ChartData data in SurveyChartData)
            {

                PieChartData.Add(new ChartData() { X = data.X, Y = data.Y / SurveyChartData.Sum(d => d.Y) * 100 });
            }
        }

        protected async Task ExportChart()
        {
            switch (SelectedChartType)
            {
                case "Bar":
                    ChartWidth = "1000";
                    ChartHeight = "650";
                    await chartObj.ExportAsync(SelectedExportType, $"Chart.{SelectedExportType}", Syncfusion.PdfExport.PdfPageOrientation.Landscape, true);
                    break;

                case "Pie":
                    ChartWidth = "1000";
                    ChartHeight = "700";

                    await Task.Delay(100);

                    await pieChartObj.ExportAsync(SelectedExportType, $"Chart.{SelectedExportType}", Syncfusion.PdfExport.PdfPageOrientation.Landscape, true);
                    break;
            }

            ChartWidth = "100%";
            ChartHeight = "100%";
        }
    }
}
