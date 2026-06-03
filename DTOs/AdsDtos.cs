using NewsPortalPro.Models;

namespace NewsPortalPro.DTOs
{
    public class AdvertisementDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? TargetUrl { get; set; }
        public string? HtmlCode { get; set; }
        public AdPosition Position { get; set; }
        public AdStatus Status { get; set; }
        public int ImpressionCount { get; set; }
        public int ClickCount { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CreateAdDto
    {
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? TargetUrl { get; set; }
        public string? HtmlCode { get; set; }
        public AdPosition Position { get; set; }
        public int DisplayOrder { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UpdateAdDto : CreateAdDto
    {
        public int Id { get; set; }
        public AdStatus Status { get; set; }
    }
}