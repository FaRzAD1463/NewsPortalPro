using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Services
{
        public class AnalyticsService : IAnalyticsService
        {
        private readonly ApplicationDbContext _db;

        public AnalyticsService(ApplicationDbContext db) => _db = db;

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            return new DashboardStatsDto
            {
                TotalNews = await _db.News
                    .IgnoreQueryFilters()
                    .CountAsync(n => !n.IsDeleted),
                PublishedNews = await _db.News
                    .CountAsync(n => n.Status == NewsStatus.Published),
                DraftNews = await _db.News
                    .CountAsync(n => n.Status == NewsStatus.Draft),
                TotalUsers = await _db.Users
                    .CountAsync(u => !u.IsDeleted),
                TotalComments = await _db.Comments.CountAsync(),
                PendingComments = await _db.Comments
                    .CountAsync(c => c.Status == CommentStatus.Pending),
                TotalViews = await _db.NewsViews.LongCountAsync(),
                TodayViews = await _db.NewsViews
                    .CountAsync(v => v.ViewedAt.Date == today),
                TotalSubscribers = await _db.Subscribers
                    .CountAsync(s => s.IsActive),
                ActiveAds = await _db.Advertisements
                    .CountAsync(a => a.Status == AdStatus.Active)
            };
        }

            public async Task<List<DailyViewsDto>> GetDailyViewsAsync(int days = 30)
            {
            var from = DateTime.UtcNow.Date.AddDays(-days);
            return await _db.NewsViews
                .Where(v => v.ViewedAt.Date >= from)
                .GroupBy(v => v.ViewedAt.Date)
                .Select(g => new DailyViewsDto
                {
                    Date = g.Key,
                    Views = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();
            }

            public async Task<List<TopNewsDto>> GetTopNewsAsync(
            int count = 10, int days = 7)
            {
            var from = DateTime.UtcNow.AddDays(-days);
            return await _db.News
                .Where(n => n.Status == NewsStatus.Published
                    && n.PublishedAt >= from)
                .OrderByDescending(n => n.ViewCount)
                .Take(count)
                .Select(n => new TopNewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Slug = n.Slug,
                    ViewCount = n.ViewCount,
                    FeaturedImage = n.FeaturedImage
                })
                .ToListAsync();
            }

            public async Task<List<CategoryStatsDto>> GetCategoryStatsAsync() =>
            await _db.Categories
                .Where(c => c.IsActive)
                .Select(c => new CategoryStatsDto
                {
                    Name = c.Name,
                    ColorCode = c.ColorCode,
                    NewsCount = c.News
                        .Count(n => n.Status == NewsStatus.Published)
                })
                .OrderByDescending(c => c.NewsCount)
                .ToListAsync();

            public async Task RecordVisitAsync(string page, string? userId,
            string ip, string userAgent, string? referrer)
            {
            _db.VisitorAnalytics.Add(new VisitorAnalytics
            {
                Page = page,
                UserId = userId,
                IpAddress = ip,
                UserAgent = userAgent,
                Referrer = referrer,
                Device = DetectDevice(userAgent),
                Browser = DetectBrowser(userAgent),
                VisitedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            }

           public async Task<VisitorStatsDto> GetVisitorStatsAsync(int days = 30)
           {
            var from = DateTime.UtcNow.AddDays(-days);
            var visits = await _db.VisitorAnalytics
                .Where(v => v.VisitedAt >= from)
                .ToListAsync();

            return new VisitorStatsDto
            {
                TotalVisitors = visits.Count,
                UniqueVisitors = visits
                    .Select(v => v.IpAddress)
                    .Distinct()
                    .Count(),
                DeviceBreakdown = visits
                    .GroupBy(v => v.Device ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                BrowserBreakdown = visits
                    .GroupBy(v => v.Browser ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count())
            };
            }

           public async Task AggregateAsync()
           {
            var cutoff = DateTime.UtcNow.AddDays(-90);
            await _db.Database.ExecuteSqlRawAsync(
                "DELETE FROM VisitorAnalytics WHERE VisitedAt < {0}", cutoff);
           }

           private static string DetectDevice(string? ua)
           {
            if (string.IsNullOrEmpty(ua)) return "Unknown";
            if (ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
                return "Mobile";
            if (ua.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
                return "Tablet";
            return "Desktop";
           }

          private static string DetectBrowser(string? ua)
          {
            if (string.IsNullOrEmpty(ua)) return "Unknown";
            if (ua.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
                return "Chrome";
            if (ua.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
                return "Firefox";
            if (ua.Contains("Safari", StringComparison.OrdinalIgnoreCase))
                return "Safari";
            if (ua.Contains("Edge", StringComparison.OrdinalIgnoreCase))
                return "Edge";
            return "Other";
          }
        }
}