using WebApplication1.Models;

namespace WebApplication1.viewmodels
{
    public class SummaryAndKeywordsViewModel
    {
        public string OriginalText { get; set; }
        public string Summary { get; set; }
        public List<KeywordModel> Keywords { get; set; }
    }

}
