namespace NewsPortalPro.DTOs
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = [];

        public static ApiResponse<T> Ok(T data, string? message = null) =>
            new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> Fail(string error) =>
            new() { Success = false, Errors = [error] };

        public static ApiResponse<T> Fail(List<string> errors) =>
            new() { Success = false, Errors = errors };
    }

    public class UploadResultDto
    {
        public string Url { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string? PublicId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long FileSizeBytes { get; set; }
    }

    public class DashboardStatsDto
    {
        public int TotalNews { get; set; }
        public int PublishedNews { get; set; }
        public int DraftNews { get; set; }
        public int TotalUsers { get; set; }
        public int TotalComments { get; set; }
        public int PendingComments { get; set; }
        public long TotalViews { get; set; }
        public int TodayViews { get; set; }
        public int TotalSubscribers { get; set; }
        public int ActiveAds { get; set; }
    }

    public class DailyViewsDto
    {
        public DateTime Date { get; set; }
        public int Views { get; set; }
    }

    public class TopNewsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public string? FeaturedImage { get; set; }
    }

    public class CategoryStatsDto
    {
        public string Name { get; set; } = string.Empty;
        public int NewsCount { get; set; }
        public string? ColorCode { get; set; }
    }

    public class VisitorStatsDto
    {
        public int TotalVisitors { get; set; }
        public int UniqueVisitors { get; set; }
        public Dictionary<string, int> DeviceBreakdown { get; set; } = [];
        public Dictionary<string, int> BrowserBreakdown { get; set; } = [];
    }

    public class SEOMetaDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Keywords { get; set; }
        public string? OgTitle { get; set; }
        public string? OgDescription { get; set; }
        public string? OgImage { get; set; }
        public string? CanonicalUrl { get; set; }
    }
}