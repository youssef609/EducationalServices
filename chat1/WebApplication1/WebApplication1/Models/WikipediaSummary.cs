using System.Text.Json.Serialization;
namespace WebApplication1.Models
{
    public class WikipediaSummary
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("extract")]
        public string Extract { get; set; }

        [JsonPropertyName("content_urls")]
        public ContentUrls ContentUrls { get; set; }
    }

    public class ContentUrls
    {
        [JsonPropertyName("desktop")]
        public Desktop Desktop { get; set; }
    }

    public class Desktop
    {
        [JsonPropertyName("page")]
        public string Page { get; set; }
    }
}