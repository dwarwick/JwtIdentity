namespace JwtIdentity.Client.Pages.Auth
{
    public class RegisterModel : BlazorBase
    {
        [Parameter]
        public string id { get; set; }

        protected RegisterViewModel registerModel { get; set; } = new RegisterViewModel();

        protected private bool AgreedToTerms { get; set; }

        protected async Task HandleRegister()
        {
            if (!AgreedToTerms)
            {
                _ = Snackbar.Add("You must agree to the terms and conditions", Severity.Warning);
                return;
            }
            if (string.IsNullOrEmpty(registerModel.Email) || string.IsNullOrEmpty(registerModel.Password))
            {
                _ = Snackbar.Add("Email and password are required", Severity.Warning);
                return;
            }

            if (registerModel.Password != registerModel.ConfirmPassword)
            {
                _ = Snackbar.Add("Passwords do not match", Severity.Warning);
                return;
            }            

            var result = await ApiService.PostAsync("api/auth/register", registerModel);
            if (result != null && result.Response == "User created successfully")
            {
                if (!string.IsNullOrEmpty(id))
                {
                    await LocalStorage.SetItemAsStringAsync("survey id", id);
                }
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
