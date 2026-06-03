namespace NewsPortalPro.DTOs
{
    public class NewsletterDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsSent { get; set; }
        public DateTime? SentAt { get; set; }
        public int RecipientCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}