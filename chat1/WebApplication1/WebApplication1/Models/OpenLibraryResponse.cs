using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace WebApplication1.Models
{
    public class OpenLibraryResponse
    {
        [JsonPropertyName("docs")]
        public List<OpenLibraryDoc> Docs { get; set; }
    }

    public class OpenLibraryDoc
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("author_name")]
        public List<string> AuthorName { get; set; }

        [JsonPropertyName("first_publish_year")]
        public int FirstPublishYear { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }
    }

}
