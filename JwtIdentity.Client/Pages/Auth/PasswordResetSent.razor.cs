namespace JwtIdentity.Client.Pages.Auth
{
    public class PasswordResetSentModel : BlazorBase
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public string Email { get; set; } = string.Empty;
    }
}