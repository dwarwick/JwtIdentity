using Blazored.LocalStorage;
using JwtIdentity.Client.Helpers;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace JwtIdentity.Client.Pages
{
    public class BlazorBase : ComponentBase
    {
#pragma warning disable CS8618
        [Inject]
        internal NavigationManager NavigationManager { get; set; }

        [Inject]
        internal IApiService ApiService { get; set; }

        [Inject]
        internal IAuthService AuthService { get; set; }

        [Inject]
        internal IConfiguration Configuration { get; set; }

        [Inject]
        internal AuthenticationStateProvider AuthStateProvider { get; set; }

        [Inject]
        internal IHttpClientFactory HttpClientFactory { get; set; }

        [Inject]
        internal IServiceProvider ServiceProvider { get; set; }

        [Inject]
        internal IJSRuntime JSRuntime { get; set; }

        [Inject]
        internal ILocalStorageService LocalStorage { get; set; }

        [Inject]
        internal IDialogService MudDialog { get; set; }

        [Inject]
        internal IUtility Utility { get; set; }

        [Inject]
        internal ILogger<BlazorBase> Logger { get; set; }
#pragma warning restore CS8618

        private HttpClient _client;
        protected HttpClient Client => _client ??= HttpClientFactory.CreateClient("AuthorizedClient");

        protected ISnackbar Snackbar => ServiceProvider.GetRequiredService<ISnackbar>();

        protected CustomAuthorizationMessageHandler CustomAuthorizationMessageHandler => ServiceProvider.GetRequiredService<CustomAuthorizationMessageHandler>();

        protected NavigationManager Navigation => NavigationManager;

        /// <summary>
        /// Checks if the current authentication token has expired and redirects to login if necessary.
        /// Should be called in OnInitializedAsync of protected pages.
        /// </summary>
        /// <returns>True if token is valid, false if expired (and redirected to login)</returns>
        protected async Task<bool> CheckTokenExpirationAsync()
        {
            if (!OperatingSystem.IsBrowser())
            {
                return true; // Server-side rendering, skip check
            }

            try
            {
                var token = await LocalStorage.GetItemAsync<string>(AuthStorageKeys.AuthTokenStorageKey);
                if (string.IsNullOrEmpty(token))
                {
                    return true; // No token, let normal auth flow handle it
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    // Token is expired, clear it and redirect to login
                    await LocalStorage.RemoveItemAsync(AuthStorageKeys.AuthTokenStorageKey);
                    await LocalStorage.RemoveItemAsync(AuthStorageKeys.CurrentUserStorageKey);
                    
                    var returnUrl = Uri.EscapeDataString(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
                    NavigationManager.NavigateTo($"login?returnUrl={returnUrl}");
                    return false;
                }

                return true; // Token is still valid
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error checking token expiration");
                return true; // On error, let normal auth flow handle it
            }
        }
    }
}

