using System.Text.Json.Serialization;

namespace WebApplication1.Models
{
    public class SummarizationResponse
    {
        [JsonPropertyName("summary_text")]
        public string SummaryText { get; set; }
    }


}
