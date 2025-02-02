namespace JwtIdentity.Client.Services
{
    public interface IApiService
    {
        HttpClient _httpClient { get; set; }
        Task<T> GetAsync<T>(string endpoint);
        Task<IEnumerable<T>> GetAllAsync<T>(string endpoint);
        Task<T> CreateAsync<T>(string endpoint, T viewModel);
        Task<T> UpdateAsync<T>(string endpoint, T viewModel);
        Task<bool> DeleteAsync(string endpoint);
    }
}