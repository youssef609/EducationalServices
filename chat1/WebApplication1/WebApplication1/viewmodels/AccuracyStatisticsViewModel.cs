namespace WebApplication1.viewmodels
{
    public class AccuracyStatisticsViewModel
    {
        public string ModelName { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double AccuracyPercentage { get; set; }
        public double AverageTimeTaken { get; set; }
    }
}
