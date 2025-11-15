namespace JwtIdentity.Client.Pages.Demo
{
    public class DemoLandingModel : BlazorBase
    {
        protected bool IsStartingLinearDemo { get; set; }
        protected bool IsStartingBranchingDemo { get; set; }

        protected AppSettings AppSettings { get; set; } = new();

        protected bool HasYoutubeEmbed => !string.IsNullOrWhiteSpace(AppSettings.Youtube?.HomePageCode);

        protected string YoutubeEmbedCode => AppSettings.Youtube?.HomePageCode ?? string.Empty;

        protected override async Task OnInitializedAsync()
        {
            AppSettings = await ApiService.GetPublicAsync<AppSettings>("/api/appsettings");
        }

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
