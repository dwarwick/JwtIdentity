using Syncfusion.Blazor.Charts;

#nullable enable

namespace JwtIdentity.Client.Pages.Survey.Results
{
    public class BarChartModel : BlazorBase
    {
        [CascadingParameter(Name = "Theme")]
        public string Theme { get; set; } = string.Empty;

        [Parameter]
        public string SurveyId { get; set; } = string.Empty;

        protected SfChart? chartObj { get; set; }

        protected SfAccumulationChart? pieChartObj { get; set; }

        protected List<SfChart> BarCharts { get; set; } = new();

        protected List<SfAccumulationChart> PieCharts { get; set; } = new();

        protected List<SurveyDataViewModel> SurveyData { get; set; } = new();

        protected List<ChartData> BarChartData { get; set; } = new();

        protected List<ChartData> PieChartData { get; set; } = new();

        protected List<List<ChartData>> BarChartDataForPrint { get; set; } = new();

        protected List<List<ChartData>> PieChartDataForPrint { get; set; } = new();

        protected QuestionViewModel? SelectedQuestion { get; set; }

        protected Theme CurrentTheme => Theme == "dark" ? Syncfusion.Blazor.Theme.Tailwind3Dark : Syncfusion.Blazor.Theme.Material3;

        protected string ChartWidth { get; set; } = "100%";

        protected string ChartHeight { get; set; } = "100%";

        protected ExportType SelectedExportType { get; set; } = ExportType.PDF;

        protected string SelectedChartType { get; set; } = "Bar";

        protected bool IsLoading { get; set; } = true;

        protected ElementReference Element { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            await base.OnInitializedAsync();

            // Load your survey data here
            await LoadData();

            IsLoading = false;
        }

        private async Task LoadData()
        {
            SurveyData = await ApiService.GetAsync<List<SurveyDataViewModel>>($"{ApiEndpoints.Answer}/getanswersforsurveyforCharts/{SurveyId}");

            if (SurveyData != null && SurveyData.Count > 0)
            {
                SelectedQuestion = SurveyData[0].Question;
                HandleSelectQuestion(SelectedQuestion);
            }
        }

        protected void HandleSelectQuestion(QuestionViewModel question)
        {
            IsLoading = true;

            SelectedQuestion = question;

            BarChartDataForPrint.Clear();
            PieChartDataForPrint.Clear();

            BarCharts.Clear();
            PieCharts.Clear();

            if (question != null)
            {
                BarChartData = SurveyData.Where(x => x.Question == question).Select(x => x.SurveyData).FirstOrDefault() ?? new List<ChartData>();

                PieChartData = new List<ChartData>();

                foreach (ChartData data in BarChartData)
                {

                    PieChartData.Add(new ChartData() { X = data.X, Y = data.Y / BarChartData.Sum(d => d.Y) * 100 });
                }
            }
            else
            {
                // Initialize with null values
                for (int i = 0; i < SurveyData.Count; i++)
                {
                    BarCharts.Add(null);
                    PieCharts.Add(null);
                }

                GetDataToPrintAllCharts();
            }

            IsLoading = false;
            StateHasChanged();
        }

        protected void GetDataToPrintAllCharts()
        {
            IsLoading = true;

            // Clear previous data to avoid duplication
            BarChartDataForPrint.Clear();
            PieChartDataForPrint.Clear();

            foreach (var question in SurveyData.Select(x => x.Question))
            {
                var barData = SurveyData.Where(x => x.Question == question).Select(x => x.SurveyData).FirstOrDefault();
                var pieData = new List<ChartData>();

                if (barData != null)
                {
                    foreach (ChartData data in barData)
                    {
                        pieData.Add(new ChartData() { X = data.X, Y = data.Y / barData.Sum(d => d.Y) * 100 });
                    }
                    BarChartDataForPrint.Add(barData);
                    PieChartDataForPrint.Add(pieData);
                }
            }

            IsLoading = false;
            StateHasChanged(); // Explicitly tell Blazor to update the UI
        }

        protected async Task ExportChart()
        {
            if (SelectedQuestion != null)
            {
                _ = Snackbar.Add("Exporting chart", Severity.Info);
                switch (SelectedChartType)
                {
                    case "Bar":
                        ChartWidth = "1000";
                        ChartHeight = "650";
                        await chartObj.ExportAsync(SelectedExportType, $"{SelectedChartType}_Chart_Q{SelectedQuestion.QuestionNumber}.{SelectedExportType}", Syncfusion.PdfExport.PdfPageOrientation.Landscape, true);
                        break;

                    case "Pie":
                        ChartWidth = "1000";
                        ChartHeight = "700";

                        await Task.Delay(100);

                        await pieChartObj.ExportAsync(SelectedExportType, $"{SelectedChartType}_Chart_Q{SelectedQuestion.QuestionNumber}.{SelectedExportType}", Syncfusion.PdfExport.PdfPageOrientation.Landscape, true);
                        break;
                }

                _ = Snackbar.Add("Export complete", Severity.Success);
            }
            else
            {
                _ = Snackbar.Add("Exporting all charts", Severity.Info);
                switch (SelectedChartType)
                {
                    case "Bar":
                        ChartWidth = "950";
                        ChartHeight = "700";

                        for (int i = 0; i < BarCharts.Count; i++)
                        {
                            await Task.Delay(100);
                            await BarCharts[i].ExportAsync(SelectedExportType, $"{SelectedChartType}_Chart_Q{i + 1}.{SelectedExportType}", Syncfusion.PdfExport.PdfPageOrientation.Landscape, true);
                        }
                        break;
                    case "Pie":
                        ChartWidth = "1000";
                        ChartHeight = "700";

                        for (int i = 0; i < BarCharts.Count; i++)
                        {
                            await Task.Delay(100);
                            await PieCharts[i].ExportAsync(SelectedExportType, $"{SelectedChartType}_Chart_Q{i + 1}.{SelectedExportType}", Syncfusion.PdfExport.PdfPageOrientation.Landscape, true);
                        }
                        break;
                }

                _ = Snackbar.Add("Export complete", Severity.Success);
            }

            ChartWidth = "100%";
            ChartHeight = "100%";
        }

        protected async Task PrintChart()
        {
            switch (SelectedChartType)
            {
                case "Bar":
                    ChartWidth = "950";
                    ChartHeight = "700";

                    await Task.Delay(100);

                    await chartObj.PrintAsync(Element);
                    break;
                case "Pie":
                    ChartWidth = "1000";
                    ChartHeight = "700";
                    await Task.Delay(100);
                    await pieChartObj.PrintAsync(Element);
                    break;
            }
            ChartWidth = "100%";
            ChartHeight = "100%";
        }

        protected Func<QuestionViewModel, string> QuestionDropdownConverter = p => p == null ? "All Questions" : $"{p.QuestionNumber}. {p.Text}";

        private string GetRootDomain()
        {
            var uri = new Uri(NavigationManager.Uri);
            return uri.Host;
        }

        private static string GetCurrentYear()
        {
            return DateTime.Now.Year.ToString();
        }

        protected string GetSubtitle()
        {
            return $"© {GetCurrentYear()} {GetRootDomain()}";
        }
    }
}
