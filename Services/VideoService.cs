using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

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
                    IsActive = v.IsActive,
                    ViewCount = v.ViewCount,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<PagedResult<VideoDto>> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.Videos
                .Where(v => v.IsActive && !v.IsDeleted)
                .OrderByDescending(v => v.CreatedAt);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VideoDto
                {
                    Id = v.Id,
                    Title = v.Title,
                    Description = v.Description,
                    VideoUrl = v.VideoUrl,
                    ThumbnailUrl = v.ThumbnailUrl,
                    Duration = v.Duration,
                    NewsId = v.NewsId,
                    IsActive = v.IsActive,
                    ViewCount = v.ViewCount,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<VideoDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ── GetByIdAsync — admin edit form ───────────────────────────
        // Read-only, so no tracking needed — the global NoTracking
        // default is fine here.
        public async Task<VideoDto?> GetByIdAsync(int id)
        {
            return await _db.Videos
                .Where(v => v.Id == id && !v.IsDeleted)
                .Select(v => new VideoDto
                {
                    Id = v.Id,
                    Title = v.Title,
                    Description = v.Description,
                    VideoUrl = v.VideoUrl,
                    ThumbnailUrl = v.ThumbnailUrl,
                    Duration = v.Duration,
                    NewsId = v.NewsId,
                    IsActive = v.IsActive,
                    ViewCount = v.ViewCount,
                    CreatedAt = v.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        // ── GetAllForAdminAsync — admin list (includes inactive) ─────
        public async Task<PagedResult<VideoDto>> GetAllForAdminAsync(int page, int pageSize)
        {
            var query = _db.Videos
                .Where(v => !v.IsDeleted)
                .OrderByDescending(v => v.CreatedAt);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VideoDto
                {
                    Id = v.Id,
                    Title = v.Title,
                    Description = v.Description,
                    VideoUrl = v.VideoUrl,
                    ThumbnailUrl = v.ThumbnailUrl,
                    Duration = v.Duration,
                    NewsId = v.NewsId,
                    IsActive = v.IsActive,
                    ViewCount = v.ViewCount,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<VideoDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ── CreateAsync ───────────────────────────────────────────────
        // Add() tracks the new entity regardless of the global query
        // tracking setting — NoTracking only affects queries, not
        // entities added directly to the change tracker. No AsTracking
        // needed here.
        public async Task<int> CreateAsync(CreateVideoDto dto)
        {
            var video = new Video
            {
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                VideoUrl = dto.VideoUrl.Trim(),
                ThumbnailUrl = dto.ThumbnailUrl,
                Duration = dto.Duration,
                NewsId = dto.NewsId,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _db.Videos.Add(video);
            await _db.SaveChangesAsync();

            return video.Id;
        }

        // ── UpdateAsync ───────────────────────────────────────────────
        // .AsTracking() is required here — the DbContext is configured
        // with QueryTrackingBehavior.NoTracking as the global default
        // (see Program.cs), so without this the entity loaded below
        // would not be tracked, field assignments would have no effect,
        // and SaveChangesAsync() would silently persist nothing.
        public async Task<bool> UpdateAsync(int id, UpdateVideoDto dto)
        {
            var video = await _db.Videos
                .AsTracking()
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

            if (video == null) return false;

            video.Title = dto.Title.Trim();
            video.Description = dto.Description?.Trim();
            video.VideoUrl = dto.VideoUrl.Trim();
            video.ThumbnailUrl = dto.ThumbnailUrl;
            video.Duration = dto.Duration;
            video.NewsId = dto.NewsId;
            video.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();
            return true;
        }

        // ── DeleteAsync — soft delete ────────────────────────────────
        // Same reasoning as UpdateAsync — .AsTracking() required.
        public async Task<bool> DeleteAsync(int id)
        {
            var video = await _db.Videos
                .AsTracking()
                .FirstOrDefaultAsync(v => v.Id == id);

            if (video == null) return false;

            video.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}