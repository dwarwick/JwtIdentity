
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JwtIdentity.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient Client;
        private readonly AuthenticationStateProvider _customAuthStateProvider;
        private readonly ILocalStorageService LocalStorage;

        public AuthService(HttpClient client, AuthenticationStateProvider customAuthStateProvider, ILocalStorageService localStorage)
        {
            Client = client;
            _customAuthStateProvider = customAuthStateProvider;
            LocalStorage = localStorage;
        }

        //public async Task<string> Login(LoginModel loginModel)
        //{
        //    var response = await Client.PostAsJsonAsync("api/auth/login", loginModel);
        //    var token = await response.Content.ReadAsStringAsync();

        //    ((CustomAuthStateProvider)_customAuthStateProvider).NotifyUserAuthentication(token);
        //    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        //    return token;


        //}

        public async Task<Response<LoginModel>> Login(LoginModel model)
        {
            Response<LoginModel> response;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                var user = System.Text.Json.JsonSerializer.Serialize(model);
                var requestContent = new StringContent(user, Encoding.UTF8, "application/json");

                var responseMessage = await Client.PostAsync("api/auth/login", requestContent);

                if (responseMessage.IsSuccessStatusCode)
                {
                    var jsonString = responseMessage.Content.ReadAsStringAsync().Result;
                    var myObject = JsonSerializer.Deserialize<LoginModel>(jsonString, options);

                    response = new Response<LoginModel>
                    {
                        Data = myObject!,
                        Success = true,
                    };

                    await LocalStorage.SetItemAsync("authToken", response.Data.Token);
                    ((CustomAuthStateProvider)_customAuthStateProvider).NotifyUserAuthentication(response.Data.Token);
                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.Data.Token);

                    return response;
                }
                else
                {
                    return new Response<LoginModel> { Data = (LoginModel)Activator.CreateInstance(typeof(LoginModel))!, Message = responseMessage.ReasonPhrase ?? string.Empty, Success = false };
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task Logout()
        {
            await ((CustomAuthStateProvider)this._customAuthStateProvider).LoggedOut();
        }
    }
}
