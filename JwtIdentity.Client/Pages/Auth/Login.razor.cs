namespace JwtIdentity.Client.Pages.Auth
{
    public class LoginModel : BlazorBase
    {
        protected ApplicationUserViewModel applicationUserViewModel { get; set; } = new();

        protected Dictionary<string, object> InputAttributes { get; set; } =
        new Dictionary<string, object>()
            {
               { "autocomplete", "current-password" },
            };

        protected async Task HandleLogin()
        {
            Response<ApplicationUserViewModel> response = await AuthService.Login(applicationUserViewModel);
            if (!response.Success)
            {
                // Handle login failure
            }
            else
            {
                NavigationManager.NavigateTo("/");
            }
        }
    }
}
