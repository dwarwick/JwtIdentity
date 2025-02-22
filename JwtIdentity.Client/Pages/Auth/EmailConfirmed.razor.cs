namespace JwtIdentity.Client.Pages.Auth
{
    public class EmailConfirmedModel : BlazorBase
    {
        [SupplyParameterFromQuery]
        public string email { get; set; } = string.Empty;

        protected override void OnInitialized()
        {
            _ = AuthService.Logout();
        }
    }
}
