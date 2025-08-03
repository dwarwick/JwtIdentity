using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace JwtIdentity.Client.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private readonly IServiceProvider _serviceProvider;
        private readonly JwtSecurityTokenHandler jwtSecurityTokenHandler;
        private readonly IHttpClientFactory _httpClientFactory;
        public HttpClient _httpClient { get; set; }

        public ApplicationUserViewModel CurrentUser { get; set; }

        public event Action OnLoggedOut;

        private IApiService ApiService => _serviceProvider.GetRequiredService<IApiService>();

        public CustomAuthStateProvider(Blazored.LocalStorage.ILocalStorageService localStorage, IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider)
        {
            _localStorage = localStorage;
            jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

            if (!OperatingSystem.IsBrowser())
            {
                return new AuthenticationState(anonymous);

            }

            var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            if (savedToken == null)
            {
                return new AuthenticationState(anonymous);
            }

            var tokenContent = jwtSecurityTokenHandler.ReadJwtToken(savedToken);

            if (tokenContent.ValidTo < DateTime.UtcNow)
            {
                await _localStorage.RemoveItemAsync("authToken");
                return new AuthenticationState(anonymous);
            }

            _httpClient ??= _httpClientFactory.CreateClient("AuthorizedClient");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);

            var claims = await GetClaims();

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

            var userId = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            CurrentUser = await ApiService.GetAsync<ApplicationUserViewModel>($"{ApiEndpoints.ApplicationUser}/{userId}");

            var authState = Task.FromResult(new AuthenticationState(user));

            NotifyAuthenticationStateChanged(authState);

            return await authState;
        }

        public async Task LoggedIn()
        {
            var claims = await this.GetClaims();
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            var authState = Task.FromResult(new AuthenticationState(user));
            this.NotifyAuthenticationStateChanged(authState);

            await GetAuthenticationStateAsync();
        }

        public async Task LoggedOut()
        {
            await this._localStorage.RemoveItemAsync("authToken");

            if (_httpClient != null && _httpClient.DefaultRequestHeaders?.Authorization != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }

            var nobody = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(nobody));

            CurrentUser = null;

            _ = await ApiService.PostAsync<object>($"{ApiEndpoints.Auth}/logout", null);

            this.NotifyAuthenticationStateChanged(authState);

            OnLoggedOut?.Invoke();
        }

        private async Task<List<Claim>> GetClaims()
        {
            if (!OperatingSystem.IsBrowser())
            {
                return new List<Claim>();
            }

            var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            var tokenContent = jwtSecurityTokenHandler.ReadJwtToken(savedToken);
            var claims = tokenContent.Claims.ToList();
            claims.Add(new Claim(ClaimTypes.Name, tokenContent.Subject));
            return claims;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}

