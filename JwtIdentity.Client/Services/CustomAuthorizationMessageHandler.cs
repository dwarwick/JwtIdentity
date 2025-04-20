namespace JwtIdentity.Client.Services
{
    public class CustomAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly NavigationManager _navigationManager;
        private readonly ISnackbar snackbar;

        public event Action OnUnauthorized;

        public CustomAuthorizationMessageHandler(NavigationManager navigationManager, ISnackbar snackbar)
        {
            _navigationManager = navigationManager;
            this.snackbar = snackbar;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                OnUnauthorized?.Invoke();
                _navigationManager.NavigateTo("not-authorized");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _navigationManager.NavigateTo("/");
                _ = snackbar.Add("The page does not exist.", Severity.Error);
            }

            return response;
        }
    }
}
