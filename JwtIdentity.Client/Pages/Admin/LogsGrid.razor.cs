using JwtIdentity.Client.Pages.Admin.Dialogs;

namespace JwtIdentity.Client.Pages.Admin
{
    public class LogsGridModel : BlazorBase
    {
        protected List<LogEntryViewModel> LogEntries { get; set; } = new();
        protected bool _loading = false;
        
        // Syncfusion Grid properties
        protected int FrozenColumns { get; set; } = 1;

        protected async Task OpenLogDetailDialog(LogEntryViewModel log)
        {
            var parameters = new DialogParameters
            {
                ["Log"] = log
            };

            var options = new DialogOptions
            { 
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            var dialog = await MudDialog.ShowAsync<LogDetailDialog>("Log Details", parameters, options);
            await dialog.Result;
        }

        protected Color GetColorForLogLevel(string level)
        {
            return level?.ToLower() switch
            {
                "error" => Color.Error,
                "warning" => Color.Warning,
                "information" => Color.Info,
                "debug" => Color.Default,
                "trace" => Color.Dark,
                "critical" => Color.Error,
                _ => Color.Default
            };
        }
    }
}