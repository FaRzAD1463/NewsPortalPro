using NewsPortalPro.Models;

namespace NewsPortalPro.DTOs
{
    public class NewsListDto
    {
        public int Id { get; set; }
        public string? Division { get; set; }
        public string? District { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? FeaturedImage { get; set; }
        public string? FeaturedImageAlt { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public string? CategoryColor { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int ReadTimeMinutes { get; set; }
        public bool IsBreaking { get; set; }
        public bool IsFeatured { get; set; }
        public NewsType Type { get; set; }
        public NewsStatus Status { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    public class NewsDetailDto : NewsListDto
    {
        public string Content { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? FeaturedImageCaption { get; set; }
        public string? VideoUrl { get; set; }
        public string? AuthorPicture { get; set; }
        public string? AuthorBio { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public string? CanonicalUrl { get; set; }
        public bool AllowComments { get; set; }
        public int ShareCount { get; set; }
        public List<CommentDto> Comments { get; set; } = [];
        public List<NewsListDto> RelatedNews { get; set; } = [];
        public Dictionary<string, int> Reactions { get; set; } = [];
    }

    public class CreateNewsDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Division { get; set; }
        public string? District { get; set; }
        public string? Subtitle { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public int CategoryId { get; set; }
        public NewsStatus Status { get; set; } = NewsStatus.Draft;
        public NewsType Type { get; set; } = NewsType.Article;
        public bool IsFeatured { get; set; }
        public bool IsBreaking { get; set; }
        public bool AllowComments { get; set; } = true;
        public DateTime? ScheduledAt { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public string? FeaturedImageAlt { get; set; }
        public string? FeaturedImageCaption { get; set; }
        public string? VideoUrl { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }

        public List<string> Tags { get; set; } = [];

        // Form binding helper — converts comma string ↔ List<string>
        public string TagsString
        {
            get => string.Join(", ", Tags);
            set => Tags = string.IsNullOrWhiteSpace(value)
                ? []
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(t => t.Trim())
                       .Where(t => !string.IsNullOrEmpty(t))
                       .ToList();
        }
    }

    public class UpdateNewsDto : CreateNewsDto
    {
        public int Id { get; set; }
    }

    public class NewsFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CategorySlug { get; set; }
        public string? TagSlug { get; set; }
        public string? AuthorId { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; } = "latest";
        public NewsType? Type { get; set; }
        public string? Division { get; set; }
        public string? District { get; set; }
    }

    public class AdminNewsFilterDto : NewsFilterDto
    {
        public NewsStatus? Status { get; set; }
        public bool? IsBreaking { get; set; }
        public bool? IsFeatured { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class SearchResultDto
    {
        public List<NewsListDto> News { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public string Query { get; set; } = string.Empty;
    }
}