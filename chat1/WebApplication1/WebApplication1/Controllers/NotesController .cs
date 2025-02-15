using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.context;
using WebApplication1.Models;
using Tesseract;
using System.Text;
using System.IO;
using WebApplication1.context;

namespace WebApplication1.Controllers
{
    public class NotesController : Controller
    {
        public ChatbotDbContext context { get; set; }
        private readonly ILogger<NotesController> _logger;

        public NotesController(ILogger<NotesController> logger)
        {
            context = new ChatbotDbContext();

            _logger = logger;
        }

        // Displays all notes with optional search functionality
        public async Task<IActionResult> Index(string searchQuery)
        {
            var notes = string.IsNullOrEmpty(searchQuery)
                ? await context.Notes.ToListAsync()
                : await context.Notes
                    .Where(n => n.Title.Contains(searchQuery) || n.Tags.Contains(searchQuery))
                    .ToListAsync();

            ViewBag.SearchQuery = searchQuery;
            return View(notes);
        }

        // Displays the form to create a new note
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Note note, IFormFile uploadedFile)
        {
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                ModelState.AddModelError("uploadedFile", "Please upload a valid file.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (uploadedFile != null && uploadedFile.Length > 0)
                    {
                        // Ensure the uploads directory exists
                        var uploadsDir = Path.Combine("wwwroot", "uploads");
                        if (!Directory.Exists(uploadsDir))
                        {
                            Directory.CreateDirectory(uploadsDir);
                        }

                        // Save the uploaded file
                        var filePath = Path.Combine(uploadsDir, uploadedFile.FileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await uploadedFile.CopyToAsync(stream);
                        }

                        note.FilePath = $"/uploads/{uploadedFile.FileName}";

                        // Extract text from file (OCR or PDF parser)
                        note.Content = await ExtractTextFromFile(filePath);
                    }

                    // Set creation timestamp and save note
                    note.CreatedAt = DateTime.Now;
                    context.Add(note);
                    await context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log and show error message if needed
                    _logger.LogError(ex, "Error creating note");
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the note.");
                }
            }

            // If ModelState is invalid, return the view with error messages
            return View(note);
        }


        private async Task<string> ExtractTextFromFile(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                return extension switch
                {
                    ".pdf" => await ExtractTextFromPdf(filePath),
                    ".png" or ".jpg" or ".jpeg" => ExtractTextFromImage(filePath),
                    _ => string.Empty,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from file.");
                return string.Empty;
            }
        }

        private async Task<string> ExtractTextFromPdf(string filePath)
        {
            var text = new StringBuilder();
            try
            {
                using var pdfReader = new iText.Kernel.Pdf.PdfReader(filePath);
                using var pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader);

                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var pageText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page);
                    text.AppendLine(pageText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF.");
            }
            return await Task.FromResult(text.ToString());
        }

        private string ExtractTextFromImage(string filePath)
        {
            try
            {
                using var engine = new TesseractEngine("./wwwroot/tessdata", "eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(filePath);
                using var page = engine.Process(img);
                return page.GetText();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from image.");
                return string.Empty;
            }
        }

        // View note details
        public async Task<IActionResult> Details(int id)
        {
            var note = await context.Notes.FindAsync(id);
            if (note == null) return NotFound();

            return View(note);
        }

        // Delete a note
        public async Task<IActionResult> Delete(int id)
        {
            var note = await context.Notes.FindAsync(id);
            if (note == null) return NotFound();

            context.Notes.Remove(note);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
