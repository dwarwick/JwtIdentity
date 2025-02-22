namespace JwtIdentity.Client.Pages.Auth
{
    public class PleaseCheckYourEmailModel : BlazorBase
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public string Email { get; set; } = string.Empty;

        protected AppSettings AppSettings { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            AppSettings = await ApiService.GetAsync<AppSettings>("/api/appsettings");
        }
    }
}
