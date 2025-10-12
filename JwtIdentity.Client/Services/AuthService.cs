
using Blazored.LocalStorage;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace JwtIdentity.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly AuthenticationStateProvider _customAuthStateProvider;
        private readonly ILocalStorageService LocalStorage;
        private readonly IApiService _apiService;

        public AuthService(AuthenticationStateProvider customAuthStateProvider, ILocalStorageService localStorage, IApiService apiService)
        {
            _customAuthStateProvider = customAuthStateProvider;
            LocalStorage = localStorage;
            _apiService = apiService;
        }

        public async Task<Response<ApplicationUserViewModel>> Login(ApplicationUserViewModel input)
        {
            Response<ApplicationUserViewModel> response;
            // Removed unused JsonSerializerOptions

            try
            {
                ((CustomAuthStateProvider)_customAuthStateProvider).CurrentUser = await _apiService.PostAsync<ApplicationUserViewModel>("api/auth/login", input);

                if (((CustomAuthStateProvider)_customAuthStateProvider).CurrentUser != null && !string.IsNullOrEmpty(((CustomAuthStateProvider)_customAuthStateProvider).CurrentUser?.Token))
                {
                    response = new Response<ApplicationUserViewModel>
                    {
                        Data = ((CustomAuthStateProvider)_customAuthStateProvider).CurrentUser,
                        Success = true,
                    };

                    if (OperatingSystem.IsBrowser())
                    {
                        await LocalStorage.SetItemAsync("authToken", response.Data.Token);
                    }
                    await ((CustomAuthStateProvider)_customAuthStateProvider).LoggedIn();

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

        public async Task<Response<ApplicationUserViewModel>> StartDemo()
        {
            Response<ApplicationUserViewModel> response;
            try
            {
                ((CustomAuthStateProvider)_customAuthStateProvider).CurrentUser = await _apiService.PostAsync<ApplicationUserViewModel>("api/auth/demo", null);

                if (((CustomAuthStateProvider)_customAuthStateProvider).CurrentUser != null && !string.IsNullOrEmpty(((CustomAuthStateProvider)_customAuthStateProvider).CurrentUser?.Token))
                {
                    response = new Response<ApplicationUserViewModel>
                    {
                        Data = ((CustomAuthStateProvider)_customAuthStateProvider).CurrentUser,
                        Success = true,
                    };

                    if (OperatingSystem.IsBrowser())
                    {
                        await LocalStorage.SetItemAsync("authToken", response.Data.Token);
                    }
                    await ((CustomAuthStateProvider)_customAuthStateProvider).LoggedIn();

                    return response;
                }
                else
                {
                    response = new Response<ApplicationUserViewModel>
                    {
                        Data = null,
                        Success = false,
                        Message = "Failed to start demo"
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

            ((CustomAuthStateProvider)_customAuthStateProvider).CurrentUser = null;
        }



        public async Task<int> GetUserId()
        {
            // get user id from the authentication state
            var authState = await ((CustomAuthStateProvider)this._customAuthStateProvider).GetAuthenticationStateAsync();

            if (authState.User.Identity?.IsAuthenticated != true)
            {
                return 0;
            }

            var user = authState.User;
            var idValue = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idValue, out var userId))
            {
                return userId;
            }
            return 0;
        }
    }
}
