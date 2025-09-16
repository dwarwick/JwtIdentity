namespace JwtIdentity.Client.Pages.Admin
{
    public class PlaywrightLogsModel : BlazorBase
    {
        protected List<PlaywrightLogViewModel> PlaywrightLogs { get; set; } = new();
        protected bool IsLoading { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadLogsAsync();
        }

        protected Color GetStatusColor(string status)
        {
            return status?.Equals("Passed", StringComparison.OrdinalIgnoreCase) == true
                ? Color.Success
                : Color.Error;
        }

        private async Task LoadLogsAsync()
        {
            try
            {
                IsLoading = true;
                var logs = await ApiService.GetAllAsync<PlaywrightLogViewModel>(ApiEndpoints.PlaywrightLog);
                PlaywrightLogs = logs?.OrderByDescending(log => log.ExecutedAt).ToList() ?? new List<PlaywrightLogViewModel>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load Playwright logs");
                Snackbar.Add("Unable to load Playwright test results.", Severity.Error);
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
