using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Services
{
    public class AdsService : IAdsService
    {
        private readonly ApplicationDbContext _db;

        public AdsService(ApplicationDbContext db) => _db = db;

        public async Task<List<AdvertisementDto>> GetByPositionAsync(
            AdPosition position, int? categoryId = null)
        {
            var now = DateTime.UtcNow;
            return await _db.Advertisements
                .Where(a => a.Position == position
                    && a.Status == AdStatus.Active
                    && !a.IsDeleted
                    && (a.StartDate == null || a.StartDate <= now)
                    && (a.EndDate == null || a.EndDate >= now)
                    && (a.CategoryId == null || a.CategoryId == categoryId))
                .OrderBy(a => a.DisplayOrder)
                .Select(a => new AdvertisementDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    ImageUrl = a.ImageUrl,
                    TargetUrl = a.TargetUrl,
                    HtmlCode = a.HtmlCode,
                    Position = a.Position,
                    Status = a.Status,
                    ImpressionCount = a.ImpressionCount,
                    ClickCount = a.ClickCount
                })
                .ToListAsync();
        }

        public async Task TrackImpressionAsync(int adId) =>
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE Advertisements SET ImpressionCount = ImpressionCount + 1 WHERE Id = {0}",
                adId);

        public async Task TrackClickAsync(int adId) =>
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE Advertisements SET ClickCount = ClickCount + 1 WHERE Id = {0}",
                adId);

        public async Task<List<AdvertisementDto>> GetAllForAdminAsync() =>
            await _db.Advertisements
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AdvertisementDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    ImageUrl = a.ImageUrl,
                    TargetUrl = a.TargetUrl,
                    HtmlCode = a.HtmlCode,
                    Position = a.Position,
                    Status = a.Status,
                    ImpressionCount = a.ImpressionCount,
                    ClickCount = a.ClickCount,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    CategoryId = a.CategoryId
                })
                .ToListAsync();

        public async Task<int> CreateAsync(CreateAdDto dto)
        {
            var ad = new Advertisement
            {
                Title = dto.Title,
                ImageUrl = dto.ImageUrl,
                TargetUrl = dto.TargetUrl,
                HtmlCode = dto.HtmlCode,
                Position = dto.Position,
                DisplayOrder = dto.DisplayOrder,
                CategoryId = dto.CategoryId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = AdStatus.Active
            };
            _db.Advertisements.Add(ad);
            await _db.SaveChangesAsync();
            return ad.Id;
        }

        public async Task<bool> UpdateAsync(int id, UpdateAdDto dto)
        {
            var ad = await _db.Advertisements.FindAsync(id);
            if (ad == null) return false;
            ad.Title = dto.Title;
            ad.ImageUrl = dto.ImageUrl;
            ad.TargetUrl = dto.TargetUrl;
            ad.HtmlCode = dto.HtmlCode;
            ad.Position = dto.Position;
            ad.DisplayOrder = dto.DisplayOrder;
            ad.Status = dto.Status;
            ad.CategoryId = dto.CategoryId;
            ad.StartDate = dto.StartDate;
            ad.EndDate = dto.EndDate;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var ad = await _db.Advertisements.FindAsync(id);
            if (ad == null) return false;
            ad.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}