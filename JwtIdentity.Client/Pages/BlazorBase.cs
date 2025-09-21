using Blazored.LocalStorage;
using JwtIdentity.Client.Helpers;
using Microsoft.Extensions.Logging;

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
    }
}

