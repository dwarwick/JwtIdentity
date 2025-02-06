using System.Net.Http.Json;
using System.Text.Json;

namespace JwtIdentity.Client.Services
{
    public class ApiService : IApiService
    {
        private readonly JsonSerializerOptions _options;

        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _httpClient = httpClient;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{endpoint}");
            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{endpoint}");
            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<T>>();
        }

        public async Task<T> CreateAsync<T>(string endpoint, T viewModel)
        {
            var response = await _httpClient.PostAsJsonAsync($"{endpoint}", viewModel);
            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<T> UpdateAsync<T>(string endpoint, T viewModel)
        {
            var response = await _httpClient.PutAsJsonAsync($"{endpoint}", viewModel);
            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            var response = await _httpClient.DeleteAsync($"{endpoint}");
            _ = response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
    }

}
