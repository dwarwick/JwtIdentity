using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace JwtIdentity.Client.Services
{
    public class ApiService : IApiService
    {
        private readonly JsonSerializerOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private HttpClient? _httpClient;
        private readonly NavigationManager navigationManager;
        private readonly IServiceProvider serviceProvider;

        private ISnackbar Snackbar => serviceProvider.GetRequiredService<ISnackbar>();
        private HttpClient Client => _httpClient ??= _httpClientFactory.CreateClient("AuthorizedClient");

        public ApiService(IHttpClientFactory httpClientFactory, NavigationManager navigationManager, IServiceProvider serviceProvider)
        {
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                Converters =
                {
                    new AnswerViewModelConverter(),
                    new QuestionViewModelConverter()
                }
            };
            _httpClientFactory = httpClientFactory;
            this.navigationManager = navigationManager;
            this.serviceProvider = serviceProvider;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await Client.GetAsync($"{endpoint}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(error))
                {
                    _ = Snackbar.Add("There was a problem with the request", Severity.Error);
                    return default;
                }
                else
                {
                    _ = Snackbar.Add(error, Severity.Error);
                    return default;
                }
            }
            return await response.Content.ReadFromJsonAsync<T>(_options);
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>(string endpoint)
        {
            var response = await Client.GetAsync($"{endpoint}");
            _ = EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<IEnumerable<T>>(_options);
        }

        public async Task<T> UpdateAsync<T>(string endpoint, T viewModel)
        {
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
                        _ = Snackbar.Add("There was a problem with the request", Severity.Error);
                    }
                    else
                    {
                        _ = Snackbar.Add(error, Severity.Error);
                    }
                }
            }

            return default;
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            var response = await Client.DeleteAsync($"{endpoint}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(error))
                {
                    _ = Snackbar.Add("There was a problem with the request", Severity.Error);
                }
                else
                {
                    _ = Snackbar.Add(error, Severity.Error);
                }
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<T> PostAsync<T>(string endpoint, T viewModel)
        {
            var response = await Client.PostAsJsonAsync($"{endpoint}", viewModel, _options);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(error))
                {
                    _ = Snackbar.Add("There was a problem with the request", Severity.Error);
                    return default;
                }
                else
                {
                    _ = Snackbar.Add(error, Severity.Error);
                    return default;
                }
            }

            return await response.Content.ReadFromJsonAsync<T>(_options);
        }

        public async Task<R> PostAsync<T, R>(string endpoint, T viewModel)
        {
            var response = await Client.PostAsJsonAsync($"{endpoint}", viewModel, _options);

            if (!response.IsSuccessStatusCode)
            {
                _ = Snackbar.Add("There was a problem with the request", Severity.Error);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<R>(_options);
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
                _ = Snackbar.Add("The URL was invalid.", Severity.Error);

                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            return response;
        }
    }
}

