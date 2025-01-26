namespace JwtIdentity.Client.Services
{
    public interface IApiService<T> where T : class
    {
        HttpClient _httpClient { get; set; }
        Task<T> GetAsync(string endpoint);
        Task<IEnumerable<T>> GetAllAsync(string endpoint);
        Task<T> CreateAsync(string endpoint, T viewModel);
        Task<T> UpdateAsync(string endpoint, T viewModel);
        Task<bool> DeleteAsync(string endpoint);
    }
}