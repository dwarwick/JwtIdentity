
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JwtIdentity.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly AuthenticationStateProvider _customAuthStateProvider;
        private readonly ILocalStorageService LocalStorage;
        private readonly IApiService<LoginModel> _apiService;

        public AuthService(AuthenticationStateProvider customAuthStateProvider, ILocalStorageService localStorage, IApiService<LoginModel> apiService)
        {
            _customAuthStateProvider = customAuthStateProvider;
            LocalStorage = localStorage;
            _apiService = apiService;
        }

        public async Task<Response<LoginModel>> Login(LoginModel model)
        {
            Response<LoginModel> response;
            _ = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                model = await _apiService.CreateAsync("api/auth/login", model);

                if (model != null && !string.IsNullOrEmpty(model.Token))
                {


                    response = new Response<LoginModel>
                    {
                        Data = model,
                        Success = true,
                    };

                    await LocalStorage.SetItemAsync("authToken", response.Data.Token);
                    ((CustomAuthStateProvider)_customAuthStateProvider).NotifyUserAuthentication(response.Data.Token);
                    _apiService._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.Data.Token);

                    return response;
                }
                else
                {
                    response = new Response<LoginModel>
                    {
                        Data = null,
                        Success = false,
                        Message = "Invalid username or password"
                    };
                    return response;
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
