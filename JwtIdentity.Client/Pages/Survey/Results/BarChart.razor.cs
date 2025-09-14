using Syncfusion.Blazor.Charts;

#nullable enable

namespace JwtIdentity.Client.Pages.Survey.Results
{
    public class BarChartModel : BlazorBase, IAsyncDisposable
    {
        private int _previousDemoStep = -1;

        [CascadingParameter(Name = "Theme")]
        public string Theme { get; set; } = string.Empty;

        [Parameter]
        public string SurveyId { get; set; } = string.Empty;

        protected SfChart? chartObj { get; set; }

        protected SfAccumulationChart? pieChartObj { get; set; }

        protected List<SfChart> BarCharts { get; set; } = new();

        protected List<SfAccumulationChart> PieCharts { get; set; } = new();

        protected List<SurveyDataViewModel> SurveyData { get; set; } = new();

        [Inject]
        private SurveyHubClient SurveyHubClient { get; set; } = default!;

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

        protected ElementReference SingleChartElement { get; set; }
        protected ElementReference AllChartsElement { get; set; }

        protected bool IsDemoUser { get; set; }
        protected int DemoStep { get; set; }

        protected bool ShowDemoStep(int step) => IsDemoUser && DemoStep == step;

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            await base.OnInitializedAsync();

            // Load your survey data here
            await LoadData();

            SurveyHubClient.SurveyUpdated += HandleSurveyUpdated;
            await SurveyHubClient.JoinSurveyGroup(SurveyId);

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userName = authState.User.Identity?.Name ?? string.Empty;
            IsDemoUser = userName.StartsWith("DemoUser") && userName.EndsWith("@surveyshark.site");

            IsLoading = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (IsDemoUser && DemoStep != _previousDemoStep && IsLoading == false)
            {
                await ScrollToCurrentDemoStep();
                _previousDemoStep = DemoStep;
            }
        }

        private async Task ScrollToCurrentDemoStep()
        {
            var id = DemoStep switch
            {
                0 => "",
                1 => "",
                3 => "ChartType_dropdown",
                5 => "ExportChart_button",
                _ => null
            };

            if (!string.IsNullOrEmpty(id))
            {
                await JSRuntime.InvokeVoidAsync(
    "scrollToElement",
            "ChartType_dropdown",
            new { behavior = "smooth", block = "center", headerOffset = 0 } // adjust offset if you have a sticky header
                );

                await Task.Yield(); // or a tiny delay
                StateHasChanged();
            }
        }

        protected void NextDemoStep()
        {
            if (!IsDemoUser) return;
            DemoStep++;

            StateHasChanged();

            if (DemoStep == 6)
            {
                Navigation.NavigateTo("/mysurveys/surveysicreated?DemoStep=7");
            }
        }

        private async void HandleSurveyUpdated(string id)
        {
            if (id == SurveyId)
            {
                await LoadData();
                StateHasChanged();
            }
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
            if (IsDemoUser && DemoStep != 1) return;

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
                // Initialize with placeholders for chart references
                for (int i = 0; i < SurveyData.Count; i++)
                {
                    BarCharts.Add(null);
                    PieCharts.Add(null);
                }

                GetDataToPrintAllCharts();

                if (IsDemoUser && DemoStep == 1)
                {
                    NextDemoStep();
                }
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
                            if (BarCharts[i] != null)
                            {
                                await Task.Delay(100);
                                await BarCharts[i].ExportAsync(SelectedExportType, $"{SelectedChartType}_Chart_Q{i + 1}.{SelectedExportType}", Syncfusion.PdfExport.PdfPageOrientation.Landscape, true);
                            }
                        }
                        break;
                    case "Pie":
                        ChartWidth = "1000";
                        ChartHeight = "700";

                        for (int i = 0; i < PieCharts.Count; i++)
                        {
                            if (PieCharts[i] != null)
                            {
                                await Task.Delay(100);
                                await PieCharts[i].ExportAsync(SelectedExportType, $"{SelectedChartType}_Chart_Q{i + 1}.{SelectedExportType}", Syncfusion.PdfExport.PdfPageOrientation.Landscape, true);
                            }
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
            ChartWidth = "950";
            ChartHeight = "700";

            await Task.Delay(100);

            await JSRuntime.InvokeVoidAsync("printPage");

            ChartWidth = "100%";
            ChartHeight = "100%";
            StateHasChanged();
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

        public ValueTask DisposeAsync()
        {
            SurveyHubClient.SurveyUpdated -= HandleSurveyUpdated;
            return ValueTask.CompletedTask;
        }
    }
}
