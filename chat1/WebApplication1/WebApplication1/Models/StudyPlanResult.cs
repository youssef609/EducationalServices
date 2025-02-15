namespace WebApplication1.Models
{
    public class StudyPlanResult
    {
        public string Topic { get; set; }
        public List<Resource> Articles { get; set; }
        public List<Resource> Videos { get; set; } // Placeholder for future use
        public List<Resource> Books { get; set; } // Updated to use Resource class
    }

    public class Resource
    {
        public string Title { get; set; }
        public string Url { get; set; } // Changed from "Link" to "Url" for consistency
        public string Description { get; set; } // Added for article descriptions
        public string Author { get; set; } // Added for book authors
        public int? Year { get; set; } // Added for publication year
    }
}
