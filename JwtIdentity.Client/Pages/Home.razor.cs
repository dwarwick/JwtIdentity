namespace JwtIdentity.Client.Pages
{
    public class HomeModel : BlazorBase
    {
        protected AppSettings AppSettings { get; set; } = new();
        protected override async Task OnInitializedAsync()
        {
            AppSettings = await ApiService.GetAsync<AppSettings>("/api/appsettings");
        }
    }
}
