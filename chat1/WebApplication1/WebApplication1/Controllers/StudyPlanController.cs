using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication1.Models;
using WebApplication1.viewmodels;
using Resource = WebApplication1.Models.Resource;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.Controllers
{
    public class StudyPlanController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StudyPlanController> _logger;

        public StudyPlanController(HttpClient httpClient, ILogger<StudyPlanController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePlan(string topic, string major)
        {
            var studyPlanResult = new StudyPlanResult
            {
                Topic = topic,
                Articles = await GetWikipediaArticles(topic),
                Videos = new List<Resource>(), // Placeholder for video resources
                Books = await GetOpenLibraryBooks(topic)
            };

            return View("Index", studyPlanResult);
        }

        private async Task<List<Resource>> GetWikipediaArticles(string topic)
        {
            try
            {
                var url = $"https://en.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Wikipedia API Response: {Json}", json);

                    var article = JsonSerializer.Deserialize<WikipediaSummary>(json);

                    if (article != null)
                    {
                        _logger.LogInformation("Deserialized Article: Title={Title}, URL={Url}, Description={Description}",
                            article.Title, article.ContentUrls?.Desktop?.Page, article.Extract);

                        return new List<Resource>
                {
                    new Resource
                    {
                        Title = article.Title,
                        Url = article.ContentUrls?.Desktop?.Page,
                        Description = article.Extract
                    }
                };
                    }
                }
                else
                {
                    _logger.LogError("Wikipedia API request failed with status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Wikipedia articles");
            }
            return new List<Resource>();
        }

        private async Task<List<Resource>> GetOpenLibraryBooks(string topic)
        {
            try
            {
                var url = $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(topic)}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenLibraryResponse>(json);

                    if (result != null && result.Docs != null)
                    {
                        _logger.LogInformation("Deserialized Open Library Response: {NumFound} books found", result.Docs.Count);

                        return result.Docs.Select(d => new Resource
                        {
                            Title = d.Title,
                            Url = $"https://openlibrary.org{d.Key}",
                            Author = d.AuthorName?.FirstOrDefault(),
                            Year = d.FirstPublishYear
                        }).ToList();
                    }
                }
                else
                {
                    _logger.LogError("Open Library API request failed with status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Open Library books");
            }
            return new List<Resource>();
        }
    }
}