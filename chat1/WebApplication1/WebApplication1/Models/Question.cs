namespace WebApplication1.Models
{
    public class Question
    {
        public int Id { get; set; } // Primary Key
        public string Text { get; set; } // Question Text
        public DateTime CreatedAt { get; set; } // Timestamp
    }

}
