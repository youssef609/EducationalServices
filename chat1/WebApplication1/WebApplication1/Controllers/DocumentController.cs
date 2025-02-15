using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using WebApplication1.context;
using NAudio.Wave;
using WebApplication1.viewmodels;

namespace WebApplication1.Controllers
{
    public class DocumentController : Controller
    {
        private readonly NLPService _nlpService;
        private readonly SummarizationService _summarizationService;
        private readonly KeywordExtractionService _keywordExtractionService;
       
        public ChatbotDbContext context { get; set; }

        public DocumentController(NLPService nlpService, SummarizationService summarizationService, KeywordExtractionService keywordExtractionService)
        {
            _nlpService = nlpService;
            context = new ChatbotDbContext();
            _summarizationService = summarizationService;
            _keywordExtractionService = keywordExtractionService;
            
        }
        public IActionResult UploadAndProcess() // for summraization 
        {
            return View();
        }
        private string ExtractTextFromDocxSumarization(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(stream, false))
                {
                    Body body = wordDoc.MainDocumentPart.Document.Body;
                    return body.InnerText;
                }
            }
        }
        private string ExtractTextFromPdfSumarization(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            {
                PdfReader pdfReader = new PdfReader(stream);
                PdfDocument pdfDoc = new PdfDocument(pdfReader);
                StringBuilder text = new StringBuilder();

                for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    text.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
                }

                return text.ToString();
            }
        }


        [HttpPost]
        public async Task<IActionResult> SummarizeAndExtractKeywords(IFormFile documentFile)
        {
            if (documentFile == null || documentFile.Length == 0)
            {
                ViewData["Error"] = "Please upload a valid document.";
                return View("UploadAndProcess");
            }

            string fileExtension = Path.GetExtension(documentFile.FileName).ToLower();
            string extractedText;

            try
            {
                if (fileExtension == ".docx")
                {
                    extractedText = ExtractTextFromDocxSumarization(documentFile);
                }
                else if (fileExtension == ".pdf")
                {
                    extractedText = ExtractTextFromPdfSumarization(documentFile);
                }
                else
                {
                    ViewData["Error"] = "Unsupported file format. Please upload a DOCX or PDF file.";
                    return View("UploadAndProcess");
                }

                var summary = await _summarizationService.SummarizeText(extractedText);
                var extractedKeywords = _keywordExtractionService.ExtractKeywords(extractedText);

                var keywords = extractedKeywords.Select(keyword => new KeywordModel
                {
                    Word = keyword.Word,
                    Frequency = keyword.Frequency,
                    RelevanceScore = keyword.RelevanceScore
                }).ToList();

                var viewModel = new SummaryAndKeywordsViewModel
                {
                    OriginalText = extractedText,
                    Summary = summary,
                    Keywords = keywords
                };

                return View("UploadAndProcess", viewModel);
            }
            catch (Exception ex)
            {
                ViewData["Error"] = $"An error occurred while processing the document: {ex.Message}";
                return View("UploadAndProcess");
            }
        }







        private string ExtractTextFromDocx(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(stream, false))
                {
                    Body body = wordDoc.MainDocumentPart.Document.Body;
                    return body.InnerText;
                }
            }
        }
        private async Task<string> ExtractTextFromFileAsync(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            return await reader.ReadToEndAsync();
        }



        [HttpGet]
        public IActionResult UploadDocument()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Content("No file selected");

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var filePath = Path.Combine(uploadsDir, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string extractedText = file.FileName.EndsWith(".pdf")
                ? ExtractTextFromPdf(filePath)
                : file.FileName.EndsWith(".docx")
                    ? ExtractTextFromDocx(filePath)
                    : throw new ArgumentException("Unsupported file format.");

            var document = new DocumentModel
            {
                FileName = file.FileName,
                FilePath = filePath,
                ExtractedText = extractedText
            };

            context.Documents.Add(document);
            context.SaveChanges();

            return RedirectToAction("AskQuestion");
        }

        [HttpGet]
        public IActionResult AskQuestion()
        {
            var model = context.Documents.ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AskQuestion(string question, string selectedDocument, string selectedModel, string groundTruth = null)
        {
            var newQuestion = new Question
            {
                Text = question,
                CreatedAt = DateTime.UtcNow
            };
            context.Questions.Add(newQuestion);
            context.SaveChanges();

            var questionId = newQuestion.Id;
            var document = context.Documents.FirstOrDefault(doc => doc.FileName == selectedDocument);
            if (document == null) return BadRequest("Document not found.");

            var modelEndpoints = new Dictionary<string, string>
    {
        { "ModelA", "https://api-inference.huggingface.co/models/deepset/roberta-base-squad2" },
        { "ModelB", "https://api-inference.huggingface.co/models/bert-large-uncased-whole-word-masking-finetuned-squad" }
    };

            if (!modelEndpoints.ContainsKey(selectedModel)) return BadRequest("Invalid model selected.");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var answer = await _nlpService.GetAnswer(document.ExtractedText, question, modelEndpoints[selectedModel]);
            stopwatch.Stop();
            var timeTaken = stopwatch.Elapsed.TotalMilliseconds;
            double similarityScore = 0;
            bool isCorrect = false;

            if (!string.IsNullOrEmpty(groundTruth))
            {
                // Calculate cosine similarity between the model answer and the ground truth
                similarityScore = await _nlpService.CalculateSimilarity(answer, groundTruth);
                isCorrect = similarityScore >= 0.5; // Threshold for determining correctness

                // Log the evaluation details
                var evaluation = new AnswerEvaluation
                {
                    QuestionId = questionId,
                    ModelName = selectedModel,
                    ModelAnswer = answer,
                    CorrectAnswer = groundTruth,
                    SimilarityScore = similarityScore,
                    IsCorrect = isCorrect,
                    Timestamp = DateTime.UtcNow,
                    TimeTaken = timeTaken
                };

                context.AnswerEvaluations.Add(evaluation);

                // Update cumulative statistics
                UpdateAccuracyStatistics(selectedModel, similarityScore >= 0.5);
                context.SaveChanges();

                // Display accuracy result
                ViewData["Accuracy"] = isCorrect ? "Correct" : $"Incorrect (Similarity: {similarityScore:F2})";
            }

            // Provide feedback to the user
            ViewData["Answer"] = answer;
            ViewData["GroundTruth"] = groundTruth ?? "Not provided";

            return View("AskQuestion", context.Documents.ToList());
        }

        [HttpGet]
        public IActionResult CompareModels()
        {
            var similarityThreshold = 0.5;
            var data = context.AnswerEvaluations
                .GroupBy(a => a.ModelName)
                .Select(g => new 
                {
                    ModelName = g.Key,
                    CorrectAnswers = g.Count(e => e.IsCorrect),
                    IncorrectAnswers = g.Count(e => !e.IsCorrect),
                    AccuracyPercentage = g.Count(e => e.SimilarityScore >= similarityThreshold) * 100.0 / g.Count()
                })
                .ToList();

            var viewModel = data.Select(d => new CompareModelsViewModel
            {
                ModelName = d.ModelName,
                CorrectAnswers = d.CorrectAnswers,
                IncorrectAnswers = d.IncorrectAnswers,
                AccuracyPercentage = d.AccuracyPercentage,
            }).ToList();

            return View(viewModel);
        }




        [HttpGet]
        public IActionResult AccuracyStatistics()
        {
            var similarityThreshold = 0.5; // Define the cosine similarity threshold

            var statistics = context.AnswerEvaluations
                .GroupBy(a => a.ModelName)
                .Select(g => new AccuracyStatisticsViewModel
                {
                    ModelName = g.Key,
                    TotalQuestions = g.Count(),
                    CorrectAnswers = g.Count(e => e.SimilarityScore >= similarityThreshold),
                    AccuracyPercentage = g.Count(e => e.SimilarityScore >= similarityThreshold) * 100.0 / g.Count(),
                     AverageTimeTaken = g.Average(e => e.TimeTaken)
                })
                .ToList();

            return View(statistics);
        }

        private void UpdateAccuracyStatistics(string model, bool isCorrect)
        {
            var stats = context.AccuracyLogs.FirstOrDefault(a => a.SelectedModel == model);
            if (stats == null)
            {
                stats = new AccuracyLog
                {
                    SelectedModel = model,
                    TotalQuestions = 0,
                    CorrectAnswers = 0,
                    AccuracyPercentage = 0
                };
                context.AccuracyLogs.Add(stats);
            }

            stats.TotalQuestions++;
            if (isCorrect) stats.CorrectAnswers++;
            stats.AccuracyPercentage = (double)stats.CorrectAnswers / stats.TotalQuestions * 100;

            context.SaveChanges();
        }

        private string ConvertToWav(string inputFilePath, string outputDirectory)
        {
            var outputFilePath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputFilePath) + ".wav");

            using (var reader = new AudioFileReader(inputFilePath))
            using (var resampler = new WaveFormatConversionStream(new WaveFormat(16000, 16, 1), reader))
            {
                WaveFileWriter.CreateWaveFile(outputFilePath, resampler);
            }

            return outputFilePath;
        }

        public string ExtractTextFromDocx(string docxPath)
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(docxPath, false))
            {
                var body = wordDoc.MainDocumentPart.Document.Body;
                return body.InnerText;
            }
        }

        public string ExtractTextFromPdf(string pdfPath)
        {
            using (var pdfReader = new PdfReader(pdfPath))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                StringBuilder text = new StringBuilder();
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    var pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i));
                    text.Append(pageText);
                }
                return text.ToString();
            }
        }
    }
}
