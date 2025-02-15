namespace WebApplication1.Models
{
    public class AnswerEvaluation
    {
        public int Id { get; set; } // Primary Key
        public int QuestionId { get; set; } // Foreign Key
        public string ModelName { get; set; } // Model name (e.g., "ModelA")
        public string ModelAnswer { get; set; } // The model's answer
        public string CorrectAnswer { get; set; } // Ground truth or correct answer
        public double SimilarityScore { get; set; } // Similarity score
        public bool IsCorrect { get; set; } // Whether the model's answer was correct
        public DateTime Timestamp { get; set; } // When the answer was evaluated
        public double TimeTaken { get; set; }

        // Navigation property
        public Question Question { get; set; }
    }

}
