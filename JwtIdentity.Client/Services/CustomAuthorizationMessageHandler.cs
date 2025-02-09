using Microsoft.AspNetCore.Components;

namespace JwtIdentity.Client.Services
{
    public class CustomAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly NavigationManager _navigationManager;

        public event Action? OnUnauthorized;

        public CustomAuthorizationMessageHandler(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                OnUnauthorized?.Invoke();
                _navigationManager.NavigateTo("not-authorized");
            }

            return response;
        }
    }
}
