using Microsoft.AspNetCore.Http;
using NewsPortalPro.DTOs;
using NewsPortalPro.Models;

namespace NewsPortalPro.Interfaces
{
    // ─────────────────────────────────────────────────────
    // NEWS SERVICE
    // ─────────────────────────────────────────────────────
    public interface INewsService
    {
        Task<PagedResult<NewsListDto>> GetPublishedAsync(NewsFilterDto filter);
        Task<NewsDetailDto?> GetBySlugAsync(string slug);
        Task<NewsDetailDto?> GetByIdAsync(int id);
        Task<List<NewsListDto>> GetBreakingNewsAsync(int count = 5);
        Task<List<NewsListDto>> GetFeaturedAsync(int count = 6);
        Task<List<NewsListDto>> GetByCategoryAsync(string categorySlug, int page, int pageSize);
        Task<List<NewsListDto>> GetRelatedAsync(int newsId, int categoryId, int count = 5);
        Task<List<NewsListDto>> GetTrendingAsync(int count = 10);
        Task<List<NewsListDto>> GetMostViewedAsync(int count = 10);
        Task<int> CreateAsync(CreateNewsDto dto, string authorId);
        Task<bool> UpdateAsync(int id, UpdateNewsDto dto, string editorId);
        Task<bool> DeleteAsync(int id);
        Task<bool> PublishAsync(int id);
        Task<bool> SetBreakingAsync(int id, bool isBreaking);
        Task<bool> SetFeaturedAsync(int id, bool isFeatured);
        Task IncrementViewAsync(int newsId, string? userId, string? ip, string? userAgent, string? referrer);
        Task<int> GetTotalCountAsync();
        Task<List<NewsListDto>> SearchAsync(string query, int page, int pageSize);
        Task<PagedResult<NewsListDto>> GetAllForAdminAsync(AdminNewsFilterDto filter);
        Task PublishScheduledAsync();
        Task CleanupOldViewsAsync();
    }

    // ─────────────────────────────────────────────────────
    // CATEGORY SERVICE
    // ─────────────────────────────────────────────────────
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllActiveAsync();
        Task<List<CategoryDto>> GetMenuCategoriesAsync();
        Task<CategoryDto?> GetBySlugAsync(string slug);
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateCategoryDto dto);
        Task<bool> UpdateAsync(int id, UpdateCategoryDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<CategoryWithCountDto>> GetWithNewsCountAsync();
    }

    // ─────────────────────────────────────────────────────
    // COMMENT SERVICE
    // ─────────────────────────────────────────────────────
    public interface ICommentService
    {
        Task<List<CommentDto>> GetByNewsIdAsync(int newsId);
        Task<PagedResult<CommentDto>> GetPendingAsync(int page, int pageSize);
        Task<int> AddAsync(CreateCommentDto dto, string userId, string ip);
        Task<bool> ApproveAsync(int id);
        Task<bool> RejectAsync(int id);
        Task<bool> DeleteAsync(int id);
        Task<int> GetPendingCountAsync();
    }

    // ─────────────────────────────────────────────────────
    // NOTIFICATION SERVICE
    // ─────────────────────────────────────────────────────
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int count = 20);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int id, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task SendToUserAsync(string userId, string title, string message,
            NotificationType type, string? link = null);
        Task BroadcastAsync(string title, string message,
            NotificationType type, string? link = null);
        Task SendBreakingNewsAlertAsync(int newsId, string title, string slug);
    }

    // ─────────────────────────────────────────────────────
    // EMAIL SERVICE
    // ─────────────────────────────────────────────────────
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body, bool isHtml = true);
        Task SendEmailVerificationAsync(string email, string userName, string verificationLink);
        Task SendPasswordResetAsync(string email, string userName, string resetLink);
        Task SendNewsletterAsync(Newsletter newsletter, List<string> recipients);
        Task SendContactReplyAsync(ContactMessage message, string reply);
    }

    // ─────────────────────────────────────────────────────
    // SEARCH SERVICE
    // ─────────────────────────────────────────────────────
    public interface ISearchService
    {
        Task<SearchResultDto> SearchAsync(string query, int page = 1, int pageSize = 20);
        Task<List<string>> GetSuggestionsAsync(string query, int count = 8);
    }

    // ─────────────────────────────────────────────────────
    // ANALYTICS SERVICE
    // ─────────────────────────────────────────────────────
    public interface IAnalyticsService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<List<DailyViewsDto>> GetDailyViewsAsync(int days = 30);
        Task<List<TopNewsDto>> GetTopNewsAsync(int count = 10, int days = 7);
        Task<List<CategoryStatsDto>> GetCategoryStatsAsync();
        Task RecordVisitAsync(string page, string? userId, string ip,
            string userAgent, string? referrer);
        Task<VisitorStatsDto> GetVisitorStatsAsync(int days = 30);
        Task AggregateAsync();
    }

    // ─────────────────────────────────────────────────────
    // ADS SERVICE
    // ─────────────────────────────────────────────────────
    public interface IAdsService
    {
        Task<List<AdvertisementDto>> GetByPositionAsync(
           AdPosition position, int? categoryId = null);
        Task<List<AdvertisementDto>> GetAllForAdminAsync();
        Task<List<AdvertisementDto>> GetAllActiveAsync(); // ← add this
        Task<int> CreateAsync(CreateAdDto dto);
        Task<bool> UpdateAsync(int id, UpdateAdDto dto);
        Task<bool> DeleteAsync(int id);
        Task TrackImpressionAsync(int id);
        Task TrackClickAsync(int id);
    }

    // ─────────────────────────────────────────────────────
    // SEO SERVICE
    // ─────────────────────────────────────────────────────
    public interface ISEOService
    {
        Task<SEOMetaDto> GetMetaForPageAsync(string pageUrl);
        Task<string> GenerateSitemapAsync();
        Task<string> GenerateNewsRssFeedAsync();
        string GenerateSlug(string title);
        int CalculateReadTime(string content);
    }

    // ─────────────────────────────────────────────────────
    // SETTINGS SERVICE
    // ─────────────────────────────────────────────────────
    public interface ISettingsService
    {
        Task<string?> GetAsync(string key);
        Task<T?> GetAsync<T>(string key);
        Task<Dictionary<string, string>> GetGroupAsync(string group);
        Task SetAsync(string key, string value, string? updatedById = null);
        Task SetBulkAsync(Dictionary<string, string> settings, string? updatedById = null);
        void InvalidateCache();
    }

    // ─────────────────────────────────────────────────────
    // FILE UPLOAD SERVICE
    // ─────────────────────────────────────────────────────
    public interface IFileUploadService
    {
        Task<UploadResultDto> UploadImageAsync(IFormFile file, string folder = "news");
        Task<bool> DeleteAsync(string publicId);
        Task<UploadResultDto> UploadFromUrlAsync(string url, string folder = "news");
    }

    // ─────────────────────────────────────────────────────
    // REPOSITORIES
    // ─────────────────────────────────────────────────────
    public interface INewsRepository
    {
        Task<News?> GetByIdAsync(int id);
        Task<News?> GetBySlugAsync(string slug);
        Task<IQueryable<News>> GetQueryableAsync();
        Task<int> AddAsync(News news);
        Task UpdateAsync(News news);
        Task DeleteAsync(int id);
        Task<int> CountAsync(NewsStatus? status = null);
    }

    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(int id);
        Task<Category?> GetBySlugAsync(string slug);
        Task<List<Category>> GetAllActiveAsync();
        Task<int> AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(int id);
    }

    public interface IUnitOfWork : IDisposable
    {
        INewsRepository News { get; }
        ICategoryRepository Categories { get; }
        Task<int> SaveChangesAsync();
    }
}