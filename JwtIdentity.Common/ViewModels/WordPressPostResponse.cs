using System.Text.Json.Serialization;

namespace JwtIdentity.Common.ViewModels
{
    public class WordPressPostResponse
    {
        [JsonPropertyName("found")]
        public int TotalFound { get; set; }

        [JsonPropertyName("posts")]
        public List<WordPressPost> Posts { get; set; } = new();

        // If needed, you can add properties for 'meta', etc.
    }

    public class WordPressPost
    {
        [JsonPropertyName("post_ID")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("excerpt")]
        public string Excerpt { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("URL")]
        public string Url { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        // You can include other fields if needed, e.g. Author, Categories, etc.
    }

}
