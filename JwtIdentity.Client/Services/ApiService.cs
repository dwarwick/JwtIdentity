using System.Net.Http.Json;
using System.Text.Json;

namespace JwtIdentity.Client.Services
{
    public class ApiService<T> : IApiService<T> where T : class
    {
        public HttpClient _httpClient { get; set; }
        private readonly JsonSerializerOptions _options;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<T> GetAsync(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{endpoint}");
            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{endpoint}");
            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<T>>();
        }

        public async Task<T> CreateAsync(string endpoint, T viewModel)
        {
            var response = await _httpClient.PostAsJsonAsync($"{endpoint}", viewModel);
            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<T> UpdateAsync(string endpoint, T viewModel)
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
