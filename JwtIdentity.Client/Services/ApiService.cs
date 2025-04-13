using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JwtIdentity.Client.Services
{
    public class ApiService : IApiService
    {
        private readonly JsonSerializerOptions _options;
        private readonly HttpClient _httpClient;
        private readonly NavigationManager navigationManager;
        private readonly ISnackbar snackbar;

        public ApiService(IHttpClientFactory httpClientFactory, NavigationManager navigationManager, ISnackbar snackbar)
        {
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles, // Add this line                                                                  ,
                Converters = { 
                    new AnswerViewModelConverter(), 
                    new QuestionViewModelConverter()
                }
            };
            _httpClient = httpClientFactory.CreateClient("AuthorizedClient");
            this.navigationManager = navigationManager;
            this.snackbar = snackbar;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{endpoint}");
            if (!response.IsSuccessStatusCode)
            {
                // get th error message
                var error = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(error))
                {
                    _ = snackbar.Add("There was a problem with the request", Severity.Error);
                    return default;
                }
                else
                {
                    _ = snackbar.Add(error, Severity.Error);
                    return default;
                }
            }
            return await response.Content.ReadFromJsonAsync<T>(_options);
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{endpoint}");
            _ = EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<IEnumerable<T>>(_options);
        }

        public async Task<T> UpdateAsync<T>(string endpoint, T viewModel)
        {
            var response = await _httpClient.PutAsJsonAsync($"{endpoint}", viewModel);

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
                        _ = snackbar.Add("There was a problem with the request", Severity.Error);
                    }
                    else
                    {
                        _ = snackbar.Add(error, Severity.Error);
                    }
                }
            }

            return default;
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            var response = await _httpClient.DeleteAsync($"{endpoint}");

            if (!response.IsSuccessStatusCode)
            {
                _ = snackbar.Add("There was a problem with the request", Severity.Error);
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<T> PostAsync<T>(string endpoint, T viewModel)
        {
            var response = await _httpClient.PostAsJsonAsync($"{endpoint}", viewModel, _options);

            if (!response.IsSuccessStatusCode)
            {
                _ = snackbar.Add("There was a problem with the request", Severity.Error);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(_options);
        }

        public async Task<R> PostAsync<T, R>(string endpoint, T viewModel)
        {
            var response = await _httpClient.PostAsJsonAsync($"{endpoint}", viewModel, _options);

            if (!response.IsSuccessStatusCode)
            {
                _ = snackbar.Add("There was a problem with the request", Severity.Error);
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
                _ = snackbar.Add("The URL was invalid.", Severity.Error);

                // Return a replacement 200 (OK) so your app doesn't break further up:
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            return response;
        }
    }
}
