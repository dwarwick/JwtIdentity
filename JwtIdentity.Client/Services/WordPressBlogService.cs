using System.Text.Json;
using System.Web; // For HttpUtility

namespace JwtIdentity.Services
{
    public class WordPressBlogService : IWordPressBlogService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private string _siteDomain;
        private string _getUrl;
        private readonly IApiService _apiService;
        private readonly NavigationManager _navigationManager;
        private AppSettings AppSettings;

        public WordPressBlogService(IHttpClientFactory httpClientFactory, IConfiguration config, IApiService apiService, NavigationManager navigationManager)
        {
            _httpClientFactory = httpClientFactory;
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

                // Use the NoAuthClient for WordPress API requests
                var httpClient = _httpClientFactory.CreateClient("NoAuthClient");

                var response = await httpClient.GetAsync(_getUrl);
                _ = response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

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

                // Use the NoAuthClient for WordPress API requests
                var httpClient = _httpClientFactory.CreateClient("NoAuthClient");

                var response = await httpClient.GetAsync(_getUrl);
                _ = response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

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
