namespace JwtIdentity.Client.Pages.Auth
{
    public class LoginModel : BlazorBase
    {
        [SupplyParameterFromQuery]
        public string returnUrl { get; set; }

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
                if (await LocalStorage.ContainKeyAsync("survey id"))
                {
                    string surveyId = await LocalStorage.GetItemAsStringAsync("survey id");
                    await LocalStorage.RemoveItemAsync("survey id");
                    NavigationManager.NavigateTo($"/survey/{surveyId}");
                }
                else
                {
                    NavigationManager.NavigateTo(returnUrl ?? "/");
                }
            }
        }
    }
}
