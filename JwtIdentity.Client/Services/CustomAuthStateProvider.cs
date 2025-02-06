using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace JwtIdentity.Client.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private readonly JwtSecurityTokenHandler jwtSecurityTokenHandler;
        public HttpClient _httpClient { get; set; }

        public CustomAuthStateProvider(Blazored.LocalStorage.ILocalStorageService localStorage, HttpClient httpClient)
        {
            _localStorage = localStorage;
            jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            if (savedToken == null)
            {
                return new AuthenticationState(user);
            }

            var tokenContent = this.jwtSecurityTokenHandler.ReadJwtToken(savedToken);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);

            if (tokenContent.ValidTo < DateTime.Now)
            {
                await _localStorage.RemoveItemAsync("authToken");
                return new AuthenticationState(user);
            }

            var claims = await this.GetClaims();

            user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

            var authState = Task.FromResult(new AuthenticationState(user));

            this.NotifyAuthenticationStateChanged(authState);

            return await authState;
        }

        public async Task LoggedIn()
        {
            var claims = await this.GetClaims();
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            var authState = Task.FromResult(new AuthenticationState(user));
            this.NotifyAuthenticationStateChanged(authState);
        }

        public async Task LoggedOut()
        {
            await this._localStorage.RemoveItemAsync("authToken");
            var nobody = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(nobody));
            this.NotifyAuthenticationStateChanged(authState);
        }

        private async Task<List<Claim>> GetClaims()
        {
            var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            var tokenContent = this.jwtSecurityTokenHandler.ReadJwtToken(savedToken);
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
