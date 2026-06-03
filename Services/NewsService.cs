using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using Newtonsoft.Json;

namespace NewsPortalPro.Services
{
    public class NewsService : INewsService
    {
        private readonly ApplicationDbContext _db;
        private readonly IDistributedCache _cache;
        private readonly ISEOService _seo;
        private readonly ILogger<NewsService> _logger;
        private readonly INotificationService _notifications;

        public NewsService(
            ApplicationDbContext db,
            IDistributedCache cache,
            ISEOService seo,
            ILogger<NewsService> logger,
            INotificationService notifications)
        {
            _db = db;
            _cache = cache;
            _seo = seo;
            _logger = logger;
            _notifications = notifications;
        }

        public async Task<PagedResult<NewsListDto>> GetPublishedAsync(NewsFilterDto filter)
        {
            var query = _db.News
                .Where(n => n.Status == NewsStatus.Published)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .Include(n => n.NewsTags).ThenInclude(nt => nt.Tag)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.CategorySlug))
                query = query.Where(n => n.Category.Slug == filter.CategorySlug);

            if (!string.IsNullOrEmpty(filter.TagSlug))
                query = query.Where(n => n.NewsTags.Any(nt => nt.Tag.Slug == filter.TagSlug));

            if (!string.IsNullOrEmpty(filter.AuthorId))
                query = query.Where(n => n.AuthorId == filter.AuthorId);

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(n =>
                    n.Title.Contains(filter.Search) ||
                    (n.Summary != null && n.Summary.Contains(filter.Search)));

            if (filter.Type.HasValue)
                query = query.Where(n => n.Type == filter.Type.Value);

            query = filter.Sort switch
            {
                "popular" => query.OrderByDescending(n => n.ViewCount),
                "comments" => query.OrderByDescending(n => n.CommentCount),
                _ => query.OrderByDescending(n => n.PublishedAt)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<NewsListDto>
            {
                Items = items.Select(MapToListDto).ToList(),
                TotalCount = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<NewsDetailDto?> GetBySlugAsync(string slug)
        {
            var cacheKey = $"news:slug:{slug}";
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (cached != null)
                    return JsonConvert.DeserializeObject<NewsDetailDto>(cached);
            }
            catch { /* cache miss — continue */ }

            var news = await _db.News
                .Where(n => n.Slug == slug && n.Status == NewsStatus.Published)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .Include(n => n.Editor)
                .Include(n => n.NewsTags).ThenInclude(nt => nt.Tag)
                .Include(n => n.Comments.Where(c =>
                    c.Status == CommentStatus.Approved && c.ParentId == null))
                    .ThenInclude(c => c.User)
                .Include(n => n.Comments.Where(c =>
                    c.Status == CommentStatus.Approved && c.ParentId == null))
                    .ThenInclude(c => c.Replies
                        .Where(r => r.Status == CommentStatus.Approved))
                        .ThenInclude(r => r.User)
                .Include(n => n.Reactions)
                .FirstOrDefaultAsync();

            if (news == null) return null;

            var related = await _db.News
                .Where(n => n.CategoryId == news.CategoryId
                    && n.Id != news.Id
                    && n.Status == NewsStatus.Published)
                .OrderByDescending(n => n.PublishedAt)
                .Take(5)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .ToListAsync();

            var dto = MapToDetailDto(news, related);

            try
            {
                await _cache.SetStringAsync(cacheKey,
                    JsonConvert.SerializeObject(dto),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
            }
            catch { /* cache write failure is non-critical */ }

            return dto;
        }

        public async Task<NewsDetailDto?> GetByIdAsync(int id)
        {
            var news = await _db.News
                .Include(n => n.Category)
                .Include(n => n.Author)
                .Include(n => n.NewsTags).ThenInclude(nt => nt.Tag)
                .FirstOrDefaultAsync(n => n.Id == id);

            return news == null ? null : MapToDetailDto(news, []);
        }

        public async Task<List<NewsListDto>> GetBreakingNewsAsync(int count = 5)
        {
            const string cacheKey = "news:breaking";
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (cached != null)
                    return JsonConvert.DeserializeObject<List<NewsListDto>>(cached)!;
            }
            catch { }

            var news = await _db.News
                .Where(n => n.IsBreaking && n.Status == NewsStatus.Published)
                .OrderByDescending(n => n.PublishedAt)
                .Take(count)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .ToListAsync();

            var dtos = news.Select(MapToListDto).ToList();

            try
            {
                await _cache.SetStringAsync(cacheKey,
                    JsonConvert.SerializeObject(dtos),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                    });
            }
            catch { }

            return dtos;
        }

        public async Task<List<NewsListDto>> GetFeaturedAsync(int count = 6)
        {
            var news = await _db.News
                .Where(n => n.IsFeatured && n.Status == NewsStatus.Published)
                .OrderByDescending(n => n.PublishedAt)
                .Take(count)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .ToListAsync();

            return news.Select(MapToListDto).ToList();
        }

        public async Task<List<NewsListDto>> GetByCategoryAsync(
            string categorySlug, int page, int pageSize)
        {
            var news = await _db.News
                .Where(n => n.Category.Slug == categorySlug
                    && n.Status == NewsStatus.Published)
                .OrderByDescending(n => n.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .ToListAsync();

            return news.Select(MapToListDto).ToList();
        }

        public async Task<List<NewsListDto>> GetRelatedAsync(
            int newsId, int categoryId, int count = 5)
        {
            var news = await _db.News
                .Where(n => n.CategoryId == categoryId
                    && n.Id != newsId
                    && n.Status == NewsStatus.Published)
                .OrderByDescending(n => n.PublishedAt)
                .Take(count)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .ToListAsync();

            return news.Select(MapToListDto).ToList();
        }

        public async Task<List<NewsListDto>> GetTrendingAsync(int count = 10)
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var news = await _db.News
                .Where(n => n.Status == NewsStatus.Published
                    && n.PublishedAt >= cutoff)
                .OrderByDescending(n => n.ViewCount)
                .Take(count)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .ToListAsync();

            return news.Select(MapToListDto).ToList();
        }

        public async Task<List<NewsListDto>> GetMostViewedAsync(int count = 10)
        {
            var news = await _db.News
                .Where(n => n.Status == NewsStatus.Published)
                .OrderByDescending(n => n.ViewCount)
                .Take(count)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .ToListAsync();

            return news.Select(MapToListDto).ToList();
        }

        public async Task<int> CreateAsync(CreateNewsDto dto, string authorId)
        {
            var slug = _seo.GenerateSlug(dto.Title);
            slug = await EnsureUniqueSlugAsync(slug);

            var news = new News
            {
                Title = dto.Title,
                Slug = slug,
                Subtitle = dto.Subtitle,
                Content = dto.Content,
                Summary = dto.Summary ?? GenerateSummary(dto.Content),
                CategoryId = dto.CategoryId,
                AuthorId = authorId,
                Status = dto.Status,
                Type = dto.Type,
                IsFeatured = dto.IsFeatured,
                IsBreaking = dto.IsBreaking,
                AllowComments = dto.AllowComments,
                ScheduledAt = dto.ScheduledAt,
                FeaturedImage = dto.FeaturedImageUrl,
                FeaturedImageAlt = dto.FeaturedImageAlt,
                FeaturedImageCaption = dto.FeaturedImageCaption,
                VideoUrl = dto.VideoUrl,
                MetaTitle = dto.MetaTitle ?? dto.Title,
                MetaDescription = dto.MetaDescription ?? dto.Summary,
                MetaKeywords = dto.MetaKeywords,
                ReadTimeMinutes = _seo.CalculateReadTime(dto.Content),
                PublishedAt = dto.Status == NewsStatus.Published
                    ? DateTime.UtcNow : null
            };

            _db.News.Add(news);
            await _db.SaveChangesAsync();

            await SaveTagsAsync(news.Id, dto.Tags);

            if (dto.Status == NewsStatus.Published && dto.IsBreaking)
                await _notifications.SendBreakingNewsAlertAsync(
                    news.Id, news.Title, news.Slug);

            await InvalidateNewsCacheAsync();
            _logger.LogInformation("News created: {Id} - {Title}", news.Id, news.Title);

            return news.Id;
        }

        public async Task<bool> UpdateAsync(int id, UpdateNewsDto dto, string editorId)
        {
            var news = await _db.News
                .Include(n => n.NewsTags)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (news == null) return false;

            var wasPublished = news.Status == NewsStatus.Published;

            news.Title = dto.Title;
            news.Subtitle = dto.Subtitle;
            news.Content = dto.Content;
            news.Summary = dto.Summary ?? GenerateSummary(dto.Content);
            news.CategoryId = dto.CategoryId;
            news.EditorId = editorId;
            news.Status = dto.Status;
            news.Type = dto.Type;
            news.IsFeatured = dto.IsFeatured;
            news.IsBreaking = dto.IsBreaking;
            news.AllowComments = dto.AllowComments;
            news.ScheduledAt = dto.ScheduledAt;
            news.FeaturedImage = dto.FeaturedImageUrl ?? news.FeaturedImage;
            news.FeaturedImageAlt = dto.FeaturedImageAlt;
            news.FeaturedImageCaption = dto.FeaturedImageCaption;
            news.VideoUrl = dto.VideoUrl;
            news.MetaTitle = dto.MetaTitle ?? dto.Title;
            news.MetaDescription = dto.MetaDescription;
            news.MetaKeywords = dto.MetaKeywords;
            news.ReadTimeMinutes = _seo.CalculateReadTime(dto.Content);
            news.UpdatedAt = DateTime.UtcNow;

            if (!wasPublished && dto.Status == NewsStatus.Published)
                news.PublishedAt = DateTime.UtcNow;

            _db.NewsTags.RemoveRange(news.NewsTags);
            await _db.SaveChangesAsync();

            await SaveTagsAsync(news.Id, dto.Tags);
            await InvalidateNewsCacheAsync(news.Slug);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var news = await _db.News.FindAsync(id);
            if (news == null) return false;
            news.IsDeleted = true;
            news.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await InvalidateNewsCacheAsync(news.Slug);
            return true;
        }

        public async Task<bool> PublishAsync(int id)
        {
            var news = await _db.News.FindAsync(id);
            if (news == null) return false;
            news.Status = NewsStatus.Published;
            news.PublishedAt = DateTime.UtcNow;
            news.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            if (news.IsBreaking)
                await _notifications.SendBreakingNewsAlertAsync(
                    news.Id, news.Title, news.Slug);
            await InvalidateNewsCacheAsync();
            return true;
        }

        public async Task<bool> SetBreakingAsync(int id, bool isBreaking)
        {
            var news = await _db.News.FindAsync(id);
            if (news == null) return false;
            news.IsBreaking = isBreaking;
            news.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            try { await _cache.RemoveAsync("news:breaking"); } catch { }
            return true;
        }

        public async Task<bool> SetFeaturedAsync(int id, bool isFeatured)
        {
            var news = await _db.News.FindAsync(id);
            if (news == null) return false;
            news.IsFeatured = isFeatured;
            news.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task IncrementViewAsync(int newsId, string? userId,
            string? ip, string? userAgent, string? referrer)
        {
            var recentView = await _db.NewsViews
                .AnyAsync(v => v.NewsId == newsId
                    && v.IpAddress == ip
                    && v.ViewedAt >= DateTime.UtcNow.AddMinutes(-30));

            if (recentView) return;

            _db.NewsViews.Add(new NewsView
            {
                NewsId = newsId,
                UserId = userId,
                IpAddress = ip,
                UserAgent = userAgent,
                Referrer = referrer,
                ViewedAt = DateTime.UtcNow,
                Device = DetectDevice(userAgent)
            });

            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE News SET ViewCount = ViewCount + 1 WHERE Id = {0}", newsId);

            await _db.SaveChangesAsync();
        }

        public async Task<int> GetTotalCountAsync() =>
            await _db.News.CountAsync();

        public async Task<List<NewsListDto>> SearchAsync(
            string query, int page, int pageSize)
        {
            var news = await _db.News
                .Where(n => n.Status == NewsStatus.Published && (
                    n.Title.Contains(query) ||
                    n.Content.Contains(query) ||
                    (n.Summary != null && n.Summary.Contains(query))))
                .OrderByDescending(n => n.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .ToListAsync();

            return news.Select(MapToListDto).ToList();
        }

        public async Task<PagedResult<NewsListDto>> GetAllForAdminAsync(
            AdminNewsFilterDto filter)
        {
            var query = _db.News
                .IgnoreQueryFilters()
                .Where(n => !n.IsDeleted)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .AsQueryable();

            if (filter.Status.HasValue)
                query = query.Where(n => n.Status == filter.Status.Value);
            if (filter.IsBreaking.HasValue)
                query = query.Where(n => n.IsBreaking == filter.IsBreaking.Value);
            if (filter.IsFeatured.HasValue)
                query = query.Where(n => n.IsFeatured == filter.IsFeatured.Value);
            if (filter.FromDate.HasValue)
                query = query.Where(n => n.CreatedAt >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(n => n.CreatedAt <= filter.ToDate.Value);
            if (!string.IsNullOrEmpty(filter.CategorySlug))
                query = query.Where(n => n.Category.Slug == filter.CategorySlug);
            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(n => n.Title.Contains(filter.Search));

            query = query.OrderByDescending(n => n.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<NewsListDto>
            {
                Items = items.Select(MapToListDto).ToList(),
                TotalCount = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task PublishScheduledAsync()
        {
            var now = DateTime.UtcNow;
            var scheduled = await _db.News
                .Where(n => n.Status == NewsStatus.Scheduled
                    && n.ScheduledAt <= now)
                .ToListAsync();

            foreach (var news in scheduled)
            {
                news.Status = NewsStatus.Published;
                news.PublishedAt = now;
                news.UpdatedAt = now;
            }

            if (scheduled.Count > 0)
            {
                await _db.SaveChangesAsync();
                await InvalidateNewsCacheAsync();
                _logger.LogInformation(
                    "Published {Count} scheduled news", scheduled.Count);
            }
        }

        public async Task CleanupOldViewsAsync()
        {
            var cutoff = DateTime.UtcNow.AddDays(-90);
            await _db.Database.ExecuteSqlRawAsync(
                "DELETE FROM NewsViews WHERE ViewedAt < {0}", cutoff);
        }

        // ── Private Helpers ──────────────────────────────────

        private static NewsListDto MapToListDto(News n) => new()
        {
            Id = n.Id,
            Title = n.Title,
            Slug = n.Slug,
            Summary = n.Summary,
            FeaturedImage = n.FeaturedImage,
            FeaturedImageAlt = n.FeaturedImageAlt,
            CategoryName = n.Category?.Name ?? string.Empty,
            CategorySlug = n.Category?.Slug ?? string.Empty,
            CategoryColor = n.Category?.ColorCode,
            AuthorName = n.Author?.FullName ?? string.Empty,
            AuthorId = n.AuthorId,
            PublishedAt = n.PublishedAt,
            ViewCount = n.ViewCount,
            CommentCount = n.CommentCount,
            ReadTimeMinutes = n.ReadTimeMinutes,
            IsBreaking = n.IsBreaking,
            IsFeatured = n.IsFeatured,
            Type = n.Type,
            Tags = n.NewsTags?.Select(nt => nt.Tag.Name).ToList() ?? []
        };

        private static NewsDetailDto MapToDetailDto(News n, List<News> related) => new()
        {
            Id = n.Id,
            Title = n.Title,
            Slug = n.Slug,
            Subtitle = n.Subtitle,
            Summary = n.Summary,
            Content = n.Content,
            FeaturedImage = n.FeaturedImage,
            FeaturedImageAlt = n.FeaturedImageAlt,
            FeaturedImageCaption = n.FeaturedImageCaption,
            VideoUrl = n.VideoUrl,
            CategoryName = n.Category?.Name ?? string.Empty,
            CategorySlug = n.Category?.Slug ?? string.Empty,
            CategoryColor = n.Category?.ColorCode,
            AuthorName = n.Author?.FullName ?? string.Empty,
            AuthorId = n.AuthorId,
            AuthorPicture = n.Author?.ProfilePicture,
            AuthorBio = n.Author?.Bio,
            PublishedAt = n.PublishedAt,
            ViewCount = n.ViewCount,
            CommentCount = n.CommentCount,
            ShareCount = n.ShareCount,
            ReadTimeMinutes = n.ReadTimeMinutes,
            IsBreaking = n.IsBreaking,
            IsFeatured = n.IsFeatured,
            Type = n.Type,
            AllowComments = n.AllowComments,
            MetaTitle = n.MetaTitle,
            MetaDescription = n.MetaDescription,
            MetaKeywords = n.MetaKeywords,
            CanonicalUrl = n.CanonicalUrl,
            Tags = n.NewsTags?.Select(nt => nt.Tag.Name).ToList() ?? [],
            Comments = n.Comments?
                .Where(c => c.ParentId == null)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    UserId = c.UserId,
                    UserName = c.User?.FullName ?? "Anonymous",
                    UserAvatar = c.User?.ProfilePicture,
                    CreatedAt = c.CreatedAt,
                    NewsId = c.NewsId,
                    NewsTitle = n.Title,
                    NewsSlug = n.Slug,
                    Replies = c.Replies?
                        .Select(r => new CommentDto
                        {
                            Id = r.Id,
                            Content = r.Content,
                            UserId = r.UserId,
                            UserName = r.User?.FullName ?? "Anonymous",
                            UserAvatar = r.User?.ProfilePicture,
                            CreatedAt = r.CreatedAt,
                            NewsId = r.NewsId
                        }).ToList() ?? []
                }).ToList() ?? [],
            Reactions = n.Reactions?
                .GroupBy(r => r.Type.ToString())
                .ToDictionary(g => g.Key, g => g.Count()) ?? [],
            RelatedNews = related.Select(MapToListDto).ToList()
        };

        private async Task SaveTagsAsync(int newsId, List<string> tagNames)
        {
            foreach (var name in tagNames
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct())
            {
                var slug = name.ToLower().Replace(" ", "-");
                var tag = await _db.Tags
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Slug == slug);

                if (tag == null)
                {
                    tag = new Tag { Name = name, Slug = slug };
                    _db.Tags.Add(tag);
                    await _db.SaveChangesAsync();
                }

                var exists = await _db.NewsTags
                    .AnyAsync(nt => nt.NewsId == newsId && nt.TagId == tag.Id);

                if (!exists)
                    _db.NewsTags.Add(new NewsTag { NewsId = newsId, TagId = tag.Id });
            }
            await _db.SaveChangesAsync();
        }

        private async Task<string> EnsureUniqueSlugAsync(string slug)
        {
            var exists = await _db.News
                .IgnoreQueryFilters()
                .AnyAsync(n => n.Slug == slug);

            if (!exists) return slug;

            var counter = 1;
            string newSlug;
            do
            {
                newSlug = $"{slug}-{counter++}";
            }
            while (await _db.News
                .IgnoreQueryFilters()
                .AnyAsync(n => n.Slug == newSlug));

            return newSlug;
        }

        private static string GenerateSummary(string content)
        {
            var stripped = System.Text.RegularExpressions.Regex
                .Replace(content, "<.*?>", "");
            return stripped.Length > 300
                ? stripped[..300] + "..."
                : stripped;
        }

        private static string DetectDevice(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";
            if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
                return "Mobile";
            if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
                return "Tablet";
            return "Desktop";
        }

        private async Task InvalidateNewsCacheAsync(string? slug = null)
        {
            try
            {
                await _cache.RemoveAsync("news:breaking");
                await _cache.RemoveAsync("news:featured");
                await _cache.RemoveAsync("news:trending");
                if (slug != null)
                    await _cache.RemoveAsync($"news:slug:{slug}");
            }
            catch { /* cache invalidation is non-critical */ }
        }
    }
}