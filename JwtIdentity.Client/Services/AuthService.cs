
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
        private readonly IApiService _apiService;

        public ApplicationUserViewModel? CurrentUser { get; set; }

        public AuthService(AuthenticationStateProvider customAuthStateProvider, ILocalStorageService localStorage, IApiService apiService)
        {
            _customAuthStateProvider = customAuthStateProvider;
            LocalStorage = localStorage;
            _apiService = apiService;
        }

        public async Task<Response<ApplicationUserViewModel>> Login(ApplicationUserViewModel input)
        {
            Response<ApplicationUserViewModel> response;
            _ = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                CurrentUser = await _apiService.CreateAsync<ApplicationUserViewModel>("api/auth/login", input);

                if (CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.Token))
                {


                    response = new Response<ApplicationUserViewModel>
                    {
                        Data = CurrentUser,
                        Success = true,
                    };

                    await LocalStorage.SetItemAsync("authToken", response.Data.Token);
                    await ((CustomAuthStateProvider)_customAuthStateProvider).LoggedIn();
                    ((CustomAuthStateProvider)_customAuthStateProvider)._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.Data.Token);

                    return response;
                }
                else
                {
                    response = new Response<ApplicationUserViewModel>
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

            CurrentUser = null;
        }
    }
}
