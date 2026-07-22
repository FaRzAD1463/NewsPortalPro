using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Services
{
    public class VideoService : IVideoService
    {
        private readonly ApplicationDbContext _db;

        public VideoService(ApplicationDbContext db) => _db = db;

        public async Task<List<VideoDto>> GetLatestAsync(int count = 8)
        {
            return await _db.Videos
                .Where(v => v.IsActive && !v.IsDeleted)
                .OrderByDescending(v => v.CreatedAt)
                .Take(count)
                .Select(v => new VideoDto
                {
                    Id = v.Id,
                    Title = v.Title,
                    Description = v.Description,
                    VideoUrl = v.VideoUrl,
                    ThumbnailUrl = v.ThumbnailUrl,
                    Duration = v.Duration,
                    NewsId = v.NewsId,
                    ViewCount = v.ViewCount,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();
        }
    }
}