namespace JwtIdentity.Client.Services
{
    public interface IApiService
    {
        Task<T> GetAsync<T>(string endpoint);
        Task<T> GetPublicAsync<T>(string endpoint);
        Task<IEnumerable<T>> GetAllAsync<T>(string endpoint);
        Task<T> PostAsync<T>(string endpoint, T viewModel);
        Task<R> PostAsync<T, R>(string endpoint, T viewModel);
        Task<T> UpdateAsync<T>(string endpoint, T viewModel);
        Task<bool> DeleteAsync(string endpoint);
    }
}