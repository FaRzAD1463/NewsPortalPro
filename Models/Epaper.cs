namespace NewsPortalPro.Models
{
        public class Epaper
        {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PdfUrl { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public DateTime PublishedDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
}