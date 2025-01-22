using System.Net.Http.Json;

namespace JwtIdentity.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient Client;

        public AuthService(HttpClient client)
        {
            Client = client;
        }

        public async Task<string> Login(LoginModel loginModel)
        {
            var response = await Client.PostAsJsonAsync("api/auth/login", loginModel);
            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;


        }
    }
}
