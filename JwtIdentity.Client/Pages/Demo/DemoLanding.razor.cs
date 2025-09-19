namespace JwtIdentity.Client.Pages.Demo
{
    public class DemoLandingModel : BlazorBase
    {
        protected bool IsStartingDemo { get; set; }

        protected async Task BeginDemo()
        {
            if (IsStartingDemo)
            {
                return;
            }

            IsStartingDemo = true;

            try
            {
                Response<ApplicationUserViewModel> loginResponse = await AuthService.StartDemo();

                if (loginResponse.Success)
                {
                    Navigation.NavigateTo("/survey/create");
                }
                else
                {
                    _ = Snackbar.Add("Unable to start the demo right now. Please try again.", MudBlazor.Severity.Error);
                }
            }
            finally
            {
                IsStartingDemo = false;
            }
        }
    }
}
