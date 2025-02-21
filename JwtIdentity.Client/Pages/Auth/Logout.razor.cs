namespace JwtIdentity.Client.Pages.Auth
{
    public class LogoutModel : BlazorBase
    {
        protected override async Task OnInitializedAsync()
        {
            await AuthService.Logout();
            NavigationManager.NavigateTo("/login");
        }
    }
}
