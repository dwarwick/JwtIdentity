using System.Text.Json;

namespace JwtIdentity.Services
{
    public class WordPressBlogService : IWordPressBlogService
    {
        private readonly HttpClient _httpClient;
        private string _siteDomain;
        private string _getUrl;
        private readonly IApiService _apiService;

        private AppSettings AppSettings;

        public WordPressBlogService(HttpClient httpClient, IConfiguration config, IApiService apiService)
        {
            _apiService = apiService;


            _httpClient = httpClient;
        }

        public async Task<WordPressPostResponse> GetAllPostsAsync()
        {
            try
            {
                AppSettings = await _apiService.GetAsync<AppSettings>("/api/appsettings");

                // Read the WordPress site domain from configuration (appsettings.json)
                _siteDomain = AppSettings.WordPress.SiteDomain ?? throw new Exception("WordPress site domain not configured");
                _getUrl = AppSettings.WordPress.GetUrl ?? throw new Exception("WordPress Get Url not configured");

                _getUrl = _getUrl.Replace("{Wordpress:SiteDomain}", _siteDomain);

                // Add logging to verify API response
                Console.WriteLine("Fetching blog posts from WordPress API...");
                var response = await _httpClient.GetAsync(_getUrl);
                _ = response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {json}"); // Log the raw response for debugging

                var posts = JsonSerializer.Deserialize<WordPressPostResponse>(json);
                return posts ?? new WordPressPostResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllPostsAsync: {ex.Message}");
                return new WordPressPostResponse();
            }
        }
    }
}
