namespace WebApplication1.Models
{
    public class AccuracyLog
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string? GroundTruth { get; set; }
        public string SelectedModel { get; set; }
        public string ModelAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public DateTime Timestamp { get; set; }

        // Add these properties for tracking accuracy statistics
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double AccuracyPercentage { get; set; }
    }
}
