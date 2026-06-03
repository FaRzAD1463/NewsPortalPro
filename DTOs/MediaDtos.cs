namespace NewsPortalPro.DTOs
{
    public class PhotoDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string? AltText { get; set; }
        public string? Caption { get; set; }
        public int? GalleryId { get; set; }
        public int? NewsId { get; set; }
        public int DisplayOrder { get; set; }
        public long FileSizeBytes { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class VideoDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string? Duration { get; set; }
        public int? NewsId { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GalleryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public int? NewsId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PhotoDto> Photos { get; set; } = [];
    }
}