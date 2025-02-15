using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApplication1.Models
{
    public class SummarizationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _huggingFaceApiKey;

        public SummarizationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _huggingFaceApiKey = configuration["HuggingFace:ApiKey"] ?? throw new ArgumentNullException("HuggingFace:ApiKey");
        }

        public async Task<string> SummarizeText(string inputText)
        {
            var requestContent = new
            {
                inputs = inputText,
                parameters = new
                {
                    max_length = 150,
                    min_length = 50,
                    length_penalty = 2.0,
                    num_beams = 4
                }
            };

            var jsonPayload = JsonSerializer.Serialize(requestContent, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            });

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api-inference.huggingface.co/models/google/pegasus-xsum"),
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {_huggingFaceApiKey}");

            const int maxRetries = 5;
            const int delayMilliseconds = 3000; // 3 seconds delay between retries

            for (int retry = 0; retry < maxRetries; retry++)
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    // Deserialize using the model
                    var result = JsonSerializer.Deserialize<List<SummarizationResponse>>(responseString);

                    // Return the first summary_text if available
                    return result?.FirstOrDefault()?.SummaryText ?? "Summary could not be generated.";
                }

                var errorDetails = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable &&
                    errorDetails.Contains("currently loading") &&
                    retry < maxRetries - 1)
                {
                    // Wait and retry
                    await Task.Delay(delayMilliseconds);
                    continue;
                }

                throw new Exception($"Error occurred while summarizing text: {response.StatusCode} - {errorDetails}");
            }

            throw new Exception("Maximum retry attempts exceeded. The model may still be loading. Please try again later.");
        }

        private class SummarizationResponse
        {
            [JsonPropertyName("summary_text")]
            public string SummaryText { get; set; }
        }
    }
}
