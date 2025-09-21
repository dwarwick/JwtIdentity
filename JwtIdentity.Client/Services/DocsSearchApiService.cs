using System.Net.Http.Json;

namespace JwtIdentity.Client.Services
{
    public class DocsSearchApiService(HttpClient http)
    {
        public record Hit(string url, string title, string section, string snippet, double score);

        public async Task<List<Hit>> SearchAsync(string query, int take = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var url = $"/api/search?q={Uri.EscapeDataString(query)}&take={Math.Clamp(take, 1, 50)}";
            var results = await http.GetFromJsonAsync<List<Hit>>(url);
            return results ?? [];
        }
    }
}
