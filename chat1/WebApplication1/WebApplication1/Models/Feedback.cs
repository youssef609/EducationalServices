namespace WebApplication1.Models
{
    public class Feedback
    {
        public int Id { get; set; } // Primary Key
        public string Model { get; set; } // Model name (e.g., "ModelA" or "ModelB")
        public string FeedbackType { get; set; } // Feedback type (e.g., "Positive" or "Negative")
        public string Comment { get; set; } // Optional user comment
        public DateTime Timestamp { get; set; } // When the feedback was submitted
    }
}
