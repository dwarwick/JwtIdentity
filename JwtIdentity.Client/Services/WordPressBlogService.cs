using System.Text.Json;
using System.Web; // For HttpUtility

namespace JwtIdentity.Services
{
    public class WordPressBlogService : IWordPressBlogService
    {
        private readonly HttpClient _httpClient;
        private string _siteDomain;
        private string _getUrl;
        private readonly IApiService _apiService;
        private readonly NavigationManager _navigationManager;
        private AppSettings AppSettings;

        public WordPressBlogService(HttpClient httpClient, IConfiguration config, IApiService apiService, NavigationManager navigationManager)
        {
            // Initialize the HttpClient and other dependencies here
            _httpClient = httpClient;
            _apiService = apiService;
            _navigationManager = navigationManager;
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

                var wpResponse = JsonSerializer.Deserialize<WordPressPostResponse>(json);

                foreach (var post in wpResponse.Posts)
                {
                    post.Url = $"{_navigationManager.BaseUri}blog/{post.Slug}";
                    post.Title = HttpUtility.HtmlDecode(post.Title);
                    post.Excerpt = HttpUtility.HtmlDecode(post.Excerpt);
                }   


                return wpResponse ?? new WordPressPostResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllPostsAsync: {ex.Message}");
                return new WordPressPostResponse();
            }
        }

        public async Task<WordPressPost> GetPostByPostSlugAsync(string postSlug)
        {
            try
            {
                AppSettings = await _apiService.GetAsync<AppSettings>("/api/appsettings");                
                _siteDomain = AppSettings.WordPress.SiteDomain ?? throw new Exception("WordPress site domain not configured");
                
                _getUrl = AppSettings.WordPress.SinglePostUrl ?? throw new Exception("WordPress Get Url not configured");
                _getUrl = _getUrl.Replace("{Wordpress:SiteDomain}", _siteDomain);
                _getUrl = _getUrl.Replace("{postSlug}", postSlug);

                // Add logging to verify API response
                Console.WriteLine("Fetching blog posts from WordPress API...");
                var response = await _httpClient.GetAsync(_getUrl);
                _ = response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {json}"); // Log the raw response for debugging

                var post = JsonSerializer.Deserialize<WordPressPost>(json);
                return post ?? new WordPressPost();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPostByPostIdAsync: {ex.Message}");
                return new WordPressPost();
            }
        }
    }
}
