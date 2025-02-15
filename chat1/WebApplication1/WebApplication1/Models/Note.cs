using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Note
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; }

        
        public string Content { get; set; }

        [StringLength(200, ErrorMessage = "Tags cannot exceed 200 characters.")]
        public string Tags { get; set; } // Tags for categorization, e.g., "AI, Data Structures"

       
        public DateTime CreatedAt { get; set; }

        [StringLength(500, ErrorMessage = "File path cannot exceed 500 characters.")]
        public string? FilePath { get; set; }
    }
}