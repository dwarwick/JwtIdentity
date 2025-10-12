using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace JwtIdentity.Client.Services
{
    public class CustomAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly NavigationManager _navigationManager;
        private readonly IServiceProvider serviceProvider;
        private readonly ILocalStorageService localStorage;

        private ISnackbar Snackbar => serviceProvider.GetRequiredService<ISnackbar>();

        public event Action OnUnauthorized;

        public CustomAuthorizationMessageHandler(NavigationManager navigationManager, IServiceProvider serviceProvider, ILocalStorageService localStorage)
        {
            _navigationManager = navigationManager;
            this.serviceProvider = serviceProvider;
            this.localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (OperatingSystem.IsBrowser())
            {
                var token = await localStorage.GetItemAsync<string>("authToken");
                if (!string.IsNullOrWhiteSpace(token) && request.Headers.Authorization is null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                // Pass the user's timezone offset in minutes (client local time) to the server
                try
                {
                    var js = serviceProvider.GetRequiredService<IJSRuntime>();
                    int offsetMinutes = await js.InvokeAsync<int>("eval", "new Date().getTimezoneOffset()");
                    if (!request.Headers.Contains("X-Timezone-Offset"))
                    {
                        request.Headers.Add("X-Timezone-Offset", offsetMinutes.ToString());
                    }
                }
                catch
                {
                    // Best-effort only; ignore if JS not available
                }
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                OnUnauthorized?.Invoke();
                _navigationManager.NavigateTo("not-authorized");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _navigationManager.NavigateTo("/");
                if (OperatingSystem.IsBrowser())
                {
                    _ = Snackbar.Add("The page does not exist.", Severity.Error);
                }
            }

            return response;
        }
    }
}

