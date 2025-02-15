using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.context
{
    public class ChatbotDbContext : DbContext
    {
       
        public DbSet<DocumentModel> Documents { get; set; }
        public DbSet<AccuracyLog> AccuracyLogs { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<AnswerEvaluation> AnswerEvaluations { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Note> Notes { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=.;Database=chatbot;Trusted_Connection=true;TrustServerCertificate=true");
            base.OnConfiguring(optionsBuilder);
        }

    }
}
