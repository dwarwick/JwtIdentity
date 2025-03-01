namespace JwtIdentity.Client.Pages.Auth
{
    public class RedirectToLoginModel : BlazorBase
    {
        [Parameter]
        public string ReturnUrl { get; set; }
        protected override void OnInitialized()
        {
            if (ReturnUrl != null)
            {
                NavigationManager.NavigateTo($"login?returnUrl={ReturnUrl}", forceLoad: true);
            }
            else NavigationManager.NavigateTo($"login", forceLoad: true);

        }
    }
}
