using MudBlazor;

namespace JwtIdentity.Client.Pages.Auth
{
    public class RegisterModel : BlazorBase
    {
        protected RegisterViewModel registerModel { get; set; } = new RegisterViewModel();

        protected async Task HandleRegister()
        {
            var result = await ApiService.PostAsync("api/auth/register", registerModel);
            if (result != null && result.Response == "User created successfully")
            {
                _ = Snackbar.Add("User created successfully", Severity.Success);
                NavigationManager.NavigateTo($"/users/emailsent?email={registerModel.Email}");
            }
            else
            {
                _ = Snackbar.Add("Failed to create user", Severity.Error);
            }
        }
    }
}
