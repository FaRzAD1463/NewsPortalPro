using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly ApplicationDbContext _db;

        public PhotoService(ApplicationDbContext db) => _db = db;

        public async Task<List<PhotoDto>> GetLatestAsync(int count = 8)
        {
            return await _db.Photos
                .Where(p => p.GalleryId == null && p.NewsId == null)
                .OrderByDescending(p => p.DisplayOrder)
                .ThenByDescending(p => p.UploadedAt)
                .Take(count)
                .Select(p => new PhotoDto
                {
                    Id = p.Id,
                    ImageUrl = p.ImageUrl,
                    ThumbnailUrl = p.ThumbnailUrl,
                    AltText = p.AltText,
                    Caption = p.Caption,
                    DisplayOrder = p.DisplayOrder,
                    UploadedAt = p.UploadedAt
                })
                .ToListAsync();
        }

        public async Task<List<PhotoDto>> GetAllForAdminAsync()
        {
            return await _db.Photos
                .Where(p => p.GalleryId == null && p.NewsId == null)
                .OrderByDescending(p => p.UploadedAt)
                .Select(p => new PhotoDto
                {
                    Id = p.Id,
                    ImageUrl = p.ImageUrl,
                    ThumbnailUrl = p.ThumbnailUrl,
                    AltText = p.AltText,
                    Caption = p.Caption,
                    DisplayOrder = p.DisplayOrder,
                    UploadedAt = p.UploadedAt
                })
                .ToListAsync();
        }

        public async Task<int> CreateAsync(string imageUrl, string? altText, string? caption, int displayOrder)
        {
            var photo = new Photo
            {
                ImageUrl = imageUrl,
                AltText = altText,
                Caption = caption,
                DisplayOrder = displayOrder,
                UploadedAt = DateTime.UtcNow
            };
            _db.Photos.Add(photo);
            await _db.SaveChangesAsync();
            return photo.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var photo = await _db.Photos.FindAsync(id);
            if (photo == null) return false;
            _db.Photos.Remove(photo);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}