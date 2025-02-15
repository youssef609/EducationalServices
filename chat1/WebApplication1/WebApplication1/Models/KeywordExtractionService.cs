namespace WebApplication1.Models
{
    public class KeywordExtractionService
    {
        public List<KeywordModel> ExtractKeywords(string text)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var filteredWords = words
                .Where(word => !StopWords.Contains(word.ToLower()))
                .GroupBy(word => word)
                .Select(g => new KeywordModel
                {
                    Word = g.Key,
                    Frequency = g.Count(),
                    RelevanceScore = g.Count() * 1.0 / words.Length
                })
                .OrderByDescending(k => k.Frequency)
                .Take(10)
                .ToList();

            return filteredWords;
        }

        private static readonly HashSet<string> StopWords = new HashSet<string>
    {
        "the", "is", "at", "which", "on", "and", "a", "to", "of", "in", "it","by","with","-","for","as","be"
    };
    }
}
