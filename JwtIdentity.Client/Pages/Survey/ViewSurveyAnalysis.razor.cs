namespace JwtIdentity.Client.Pages.Survey
{
    public class ViewSurveyAnalysisModel : BlazorBase
    {
        [Parameter]
        public int SurveyId { get; set; }

        [SupplyParameterFromQuery]
        public int DemoStep { get; set; }

        protected List<SurveyAnalysisViewModel> Analyses { get; set; } = new();
        protected SurveyAnalysisViewModel SelectedAnalysis { get; set; }
        protected SurveyViewModel Survey { get; set; }
        protected bool IsLoading { get; set; } = true;
        protected bool IsDemoUser { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadAnalysesAsync();

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userName = authState.User.Identity?.Name ?? string.Empty;
            IsDemoUser = userName.StartsWith("DemoUser") && userName.EndsWith("@surveyshark.site");
        }

        protected void NextDemoStep()
        {
            if (!IsDemoUser) return;
            NavigationManager.NavigateTo("/register");
        }

        protected async Task LoadAnalysesAsync()
        {
            try
            {
                IsLoading = true;

                // Load the survey details
                var surveys = await ApiService.GetAsync<List<SurveyViewModel>>($"{ApiEndpoints.Survey}/surveysicreated");
                Survey = surveys?.FirstOrDefault(s => s.Id == SurveyId);

                if (Survey == null)
                {
                    _ = Snackbar.Add("Survey not found", Severity.Error);
                    NavigationManager.NavigateTo("/mysurveys/surveysicreated");
                    return;
                }

                // Load all analyses for this survey
                Analyses = await ApiService.GetAsync<List<SurveyAnalysisViewModel>>($"{ApiEndpoints.SurveyAnalysis}/list/{SurveyId}");

                if (Analyses != null && Analyses.Any())
                {
                    // Select the most recent analysis by default
                    SelectedAnalysis = Analyses.First();
                }
                else
                {
                    _ = Snackbar.Add("No analyses found for this survey", Severity.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading survey analyses");
                _ = Snackbar.Add("Error loading analyses", Severity.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task OnAnalysisSelected(int analysisId)
        {
            SelectedAnalysis = Analyses.FirstOrDefault(a => a.Id == analysisId);
            await InvokeAsync(StateHasChanged);
        }

        protected string FormatDate(DateTime date)
        {
            // Convert UTC to local time and format as MM/DD/YYYY
            return date.ToLocalTime().ToString("MM/dd/yyyy");
        }

        protected string FormatDateTime(DateTime date)
        {
            // Convert UTC to local time and format with time
            return date.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt");
        }
    }
}
