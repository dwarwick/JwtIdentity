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
                Converters = { new AnswerViewModelConverter(), new QuestionViewModelConverter() }
            };
            _httpClient = httpClientFactory.CreateClient("AuthorizedClient");
            this.navigationManager = navigationManager;
            this.snackbar = snackbar;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{endpoint}");
            _ = EnsureSuccess(response);
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
            _ = EnsureSuccess(response);

            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                return await response.Content.ReadFromJsonAsync<T>(_options);
            }

            return default;
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            var response = await _httpClient.DeleteAsync($"{endpoint}");
            _ = EnsureSuccess(response);
            return response.IsSuccessStatusCode;
        }

        public async Task<T> PostAsync<T>(string endpoint, T viewModel)
        {
            var response = await _httpClient.PostAsJsonAsync($"{endpoint}", viewModel, _options);
            _ = EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<T>(_options);
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
