using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace JwtIdentity.Client.Services
{
    public class ApiService : IApiService
    {
        private readonly JsonSerializerOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private HttpClient _httpClient;
        private HttpClient _publicHttpClient;
        private readonly NavigationManager navigationManager;
        private readonly IServiceProvider serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private ISnackbar Snackbar => serviceProvider.GetRequiredService<ISnackbar>();
        private HttpClient Client => _httpClient ??= _httpClientFactory.CreateClient("AuthorizedClient");
        private HttpClient PublicClient => _publicHttpClient ??= _httpClientFactory.CreateClient("PublicClient");

        private void ShowSnackbar(string message, Severity severity)
        {
            if (OperatingSystem.IsBrowser())
            {
                _ = Snackbar.Add(message, severity);
            }
        }

        public ApiService(IHttpClientFactory httpClientFactory, NavigationManager navigationManager, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor = null)
        {
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
            _httpClientFactory = httpClientFactory;
            this.navigationManager = navigationManager;
            this.serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            SetAuthHeaderFromCookie();
            var response = await Client.GetAsync($"{endpoint}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(error))
                {
                    ShowSnackbar("There was a problem with the request", Severity.Error);
                    return default;
                }
                else
                {
                    ShowSnackbar(error, Severity.Error);
                    return default;
                }
            }

            if (response.Content.Headers.ContentType?.MediaType == "text/html")
            {
                ShowSnackbar("Unexpected response. Please log in.", Severity.Error);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(_options);
        }

        public async Task<T> GetPublicAsync<T>(string endpoint)
        {
            var response = await PublicClient.GetAsync($"{endpoint}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(error))
                {
                    ShowSnackbar("There was a problem with the request", Severity.Error);
                    return default;
                }
                else
                {
                    ShowSnackbar(error, Severity.Error);
                    return default;
                }
            }

            if (response.Content.Headers.ContentType?.MediaType == "text/html")
            {
                ShowSnackbar("Unexpected response. Please log in.", Severity.Error);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(_options);
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>(string endpoint)
        {
            SetAuthHeaderFromCookie();
            var response = await Client.GetAsync($"{endpoint}");
            _ = EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<IEnumerable<T>>(_options);
        }

        public async Task<T> UpdateAsync<T>(string endpoint, T viewModel)
        {
            SetAuthHeaderFromCookie();
            var response = await Client.PutAsJsonAsync($"{endpoint}", viewModel);

            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<T>(_options);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrEmpty(error))
                    {
                        ShowSnackbar("There was a problem with the request", Severity.Error);
                    }
                    else
                    {
                        ShowSnackbar(error, Severity.Error);
                    }
                }
            }

            return default;
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            SetAuthHeaderFromCookie();
            var response = await Client.DeleteAsync($"{endpoint}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(error))
                {
                    ShowSnackbar("There was a problem with the request", Severity.Error);
                }
                else
                {
                    ShowSnackbar(error, Severity.Error);
                }
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<T> PostAsync<T>(string endpoint, T viewModel)
        {
            SetAuthHeaderFromCookie();
            var response = await Client.PostAsJsonAsync($"{endpoint}", viewModel, _options);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    ShowSnackbar("Invalid username or password", Severity.Error);
                    return default;
                }

                if (string.IsNullOrEmpty(error))
                {
                    ShowSnackbar("There was a problem with the request", Severity.Error);
                    return default;
                }
                else
                {
                    ShowSnackbar(error, Severity.Error);
                    return default;
                }
            }

            return await response.Content.ReadFromJsonAsync<T>(_options);
        }

        public async Task<R> PostAsync<T, R>(string endpoint, T viewModel)
        {
            SetAuthHeaderFromCookie();
            var response = await Client.PostAsJsonAsync($"{endpoint}", viewModel, _options);

            if (!response.IsSuccessStatusCode)
            {
                ShowSnackbar("There was a problem with the request", Severity.Error);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<R>(_options);
        }

        private void SetAuthHeaderFromCookie()
        {
            if (OperatingSystem.IsBrowser())
            {
                return;
            }

            if (Client.DefaultRequestHeaders.Authorization != null)
            {
                return;
            }

            var token = _httpContextAccessor?.HttpContext?.Request.Cookies["authToken"];
            if (!string.IsNullOrEmpty(token))
            {
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        private HttpResponseMessage EnsureSuccess(HttpResponseMessage response)
        {
            try
            {
                _ = response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                navigationManager.NavigateTo("/");
                ShowSnackbar("The URL was invalid.", Severity.Error);

                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            return response;
        }
    }
}

