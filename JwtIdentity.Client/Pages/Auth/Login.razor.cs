namespace JwtIdentity.Client.Pages.Auth
{
    public class LoginModel : BlazorBase
    {
        protected ApplicationUserViewModel applicationUserViewModel { get; set; } = new();

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
