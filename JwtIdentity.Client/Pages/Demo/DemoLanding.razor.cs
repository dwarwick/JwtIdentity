namespace JwtIdentity.Client.Pages.Demo
{
    public class DemoLandingModel : BlazorBase
    {
        protected bool IsStartingLinearDemo { get; set; }
        protected bool IsStartingBranchingDemo { get; set; }

        protected AppSettings AppSettings { get; set; } = new();

        protected bool HasYoutubeEmbed => !string.IsNullOrWhiteSpace(AppSettings.Youtube?.HomePageCode);

        protected string YoutubeEmbedCode => AppSettings.Youtube?.HomePageCode ?? string.Empty;

        protected bool HasThirdPartyConsent { get; set; }
        protected bool InlinePlaybackEnabled { get; set; }

        protected override async Task OnInitializedAsync()
        {
            AppSettings = await ApiService.GetPublicAsync<AppSettings>("/api/appsettings");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            try
            {
                HasThirdPartyConsent = await JSRuntime.InvokeAsync<bool>("userHasThirdPartyConsent");
#if !DEBUG
                // In non-DEBUG (production) automatically enable inline playback once consent exists
                if (HasThirdPartyConsent)
                {
                    InlinePlaybackEnabled = true;
                }
#endif
                await InvokeAsync(StateHasChanged);
            }
            catch
            {
                // ignore JS interop failures during prerender or debug
            }
        }

        protected async Task EnableVideoAsync()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("setThirdPartyCookieConsent", "AllCookies");
                HasThirdPartyConsent = true;
#if !DEBUG
                // Auto-enable inline playback in production immediately after consent
                InlinePlaybackEnabled = true;
#endif
                await InvokeAsync(StateHasChanged);
            }
            catch { }
        }

#if DEBUG
        protected Task EnableInlinePlaybackAsync()
        {
            InlinePlaybackEnabled = true;
            return InvokeAsync(StateHasChanged);
        }
#endif

        protected async Task BeginLinearDemo()
        {
            if (IsStartingLinearDemo)
            {
                return;
            }

            IsStartingLinearDemo = true;

            try
            {
                Response<ApplicationUserViewModel> loginResponse = await AuthService.StartDemo();

                if (loginResponse.Success)
                {
                    Navigation.NavigateTo("/survey/create?DemoType=linear");
                }
                else
                {
                    _ = Snackbar.Add("Unable to start the demo right now. Please try again.", MudBlazor.Severity.Error);
                }
            }
            finally
            {
                IsStartingLinearDemo = false;
            }
        }

        protected async Task BeginBranchingDemo()
        {
            if (IsStartingBranchingDemo)
            {
                return;
            }

            IsStartingBranchingDemo = true;

            try
            {
                Response<ApplicationUserViewModel> loginResponse = await AuthService.StartDemo();

                if (loginResponse.Success)
                {
                    Navigation.NavigateTo("/survey/create?DemoType=branching");
                }
                else
                {
                    _ = Snackbar.Add("Unable to start the demo right now. Please try again.", MudBlazor.Severity.Error);
                }
            }
            finally
            {
                IsStartingBranchingDemo = false;
            }
        }
    }
}
