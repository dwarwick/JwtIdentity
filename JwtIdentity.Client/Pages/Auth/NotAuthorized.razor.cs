namespace JwtIdentity.Client.Pages.Auth
{
    public class NotAuthorizedModel : BlazorBase
    {
        protected override async Task OnInitializedAsync()
        {
            // This is a workaround to force the page to re-render when the user logs in
            await ((CustomAuthStateProvider)AuthStateProvider!).LoggedOut();
        }
    }
}
