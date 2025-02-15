using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication1
{
    public class NLPService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "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"; // Replace with your Hugging Face API Key
        private readonly string _embeddingEndpoint = "https://api-inference.huggingface.co/pipeline/feature-extraction/sentence-transformers/all-MiniLM-L6-v2"; // Adjusted for Text Embeddings API

        public NLPService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<double> CalculateSimilarity(string answer, string groundTruth)
        {
            if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(groundTruth))
            {
                throw new ArgumentException("Answer and Ground Truth cannot be null or empty.");
            }

            try
            {
                var vector1 = await EmbedText(answer);
                var vector2 = await EmbedText(groundTruth);

                return CosineSimilarity(vector1, vector2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating similarity: {ex.Message}");
                throw;
            }
        }

        private async Task<double[]> EmbedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null or empty.", nameof(text));
            }

            var requestBody = new { inputs = new[] { text } };
            var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            var response = await _httpClient.PostAsync(_embeddingEndpoint, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Embedding API failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}, Response: {responseBody}");
                throw new HttpRequestException($"Failed to generate embeddings: {response.ReasonPhrase}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            // Log the response to inspect its structure
            Console.WriteLine($"Embedding API Response: {responseJson}");

            // Parse the response
            var jsonResponse = JsonConvert.DeserializeObject<List<List<double>>>(responseJson);

            if (jsonResponse == null || jsonResponse.Count == 0 || jsonResponse[0] == null)
            {
                throw new Exception("Embedding response is empty or invalid.");
            }

            return jsonResponse[0].ToArray();
        }


        private double CosineSimilarity(double[] vector1, double[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vectors must be of the same length.");
            }

            double dotProduct = vector1.Zip(vector2, (a, b) => a * b).Sum();
            double magnitude1 = Math.Sqrt(vector1.Sum(v => v * v));
            double magnitude2 = Math.Sqrt(vector2.Sum(v => v * v));

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                throw new DivideByZeroException("Cannot calculate cosine similarity for zero-magnitude vectors.");
            }

            return dotProduct / (magnitude1 * magnitude2);
        }

        public async Task<string> GetAnswer(string documentText, string question, string modelEndpoint)
        {
            if (string.IsNullOrWhiteSpace(documentText) || string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("Document text and question cannot be null or empty.");
            }

            var requestBody = new
            {
                inputs = new
                {
                    question = question,
                    context = documentText
                }
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            var response = await _httpClient.PostAsync(modelEndpoint, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Answer generation API failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}, Response: {responseBody}");
                return "unknown";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseJson);

            return jsonResponse?.answer ?? "unknown";
        }
    }
}
