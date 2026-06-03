using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Helpers;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using Newtonsoft.Json;
using Slugify;

namespace NewsPortalPro.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IDistributedCache _cache;
        private const string CacheKey = "categories:all";

        public CategoryService(ApplicationDbContext db, IDistributedCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<List<CategoryDto>> GetAllActiveAsync()
        {
            var cached = await _cache.GetStringAsync(CacheKey);
            if (cached != null) return JsonConvert.DeserializeObject<List<CategoryDto>>(cached)!;

            var cats = await _db.Categories
                .Where(c => c.IsActive && c.ParentId == null)
                .Include(c => c.Children.Where(ch => ch.IsActive))
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var dtos = cats.Select(MapToDto).ToList();
            await _cache.SetStringAsync(CacheKey, JsonConvert.SerializeObject(dtos),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

            return dtos;
        }

        public async Task<List<CategoryDto>> GetMenuCategoriesAsync() =>
            (await GetAllActiveAsync()).Where(c => c.ShowInMenu).ToList();

        public async Task<CategoryDto?> GetBySlugAsync(string slug)
        {
            var cat = await _db.Categories
                .Include(c => c.Children.Where(ch => ch.IsActive))
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
            return cat == null ? null : MapToDto(cat);
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var cat = await _db.Categories
                .Include(c => c.Children)
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == id);
            return cat == null ? null : MapToDto(cat);
        }

        public async Task<int> CreateAsync(CreateCategoryDto dto)
        {
            var helper = new SlugHelper();
            var slug = helper.GenerateSlug(dto.Name);

            var exists = await _db.Categories.AnyAsync(c => c.Slug == slug);
            if (exists) slug += $"-{DateTime.UtcNow.Ticks % 1000}";

            var cat = new Category
            {
                Name = dto.Name,
                Slug = slug,
                Description = dto.Description,
                ColorCode = dto.ColorCode,
                ParentId = dto.ParentId,
                DisplayOrder = dto.DisplayOrder,
                ShowInMenu = dto.ShowInMenu,
                MetaTitle = dto.MetaTitle,
                MetaDescription = dto.MetaDescription,
                MetaKeywords = dto.MetaKeywords
            };

            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKey);
            return cat.Id;
        }

        public async Task<bool> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return false;

            cat.Name = dto.Name;
            cat.Description = dto.Description;
            cat.ColorCode = dto.ColorCode;
            cat.ParentId = dto.ParentId;
            cat.DisplayOrder = dto.DisplayOrder;
            cat.ShowInMenu = dto.ShowInMenu;
            cat.IsActive = dto.IsActive;
            cat.MetaTitle = dto.MetaTitle;
            cat.MetaDescription = dto.MetaDescription;
            cat.MetaKeywords = dto.MetaKeywords;
            cat.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKey);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return false;
            cat.IsDeleted = true;
            cat.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKey);
            return true;
        }

        public async Task<List<CategoryWithCountDto>> GetWithNewsCountAsync()
        {
            return await _db.Categories
                .Where(c => c.IsActive)
                .Select(c => new CategoryWithCountDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ColorCode = c.ColorCode,
                    NewsCount = c.News.Count(n => n.Status == NewsStatus.Published)
                })
                .OrderByDescending(c => c.NewsCount)
                .ToListAsync();
        }

        private static CategoryDto MapToDto(Category c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            ColorCode = c.ColorCode,
            ParentId = c.ParentId,
            ParentName = c.Parent?.Name,
            DisplayOrder = c.DisplayOrder,
            IsActive = c.IsActive,
            ShowInMenu = c.ShowInMenu,
            MetaTitle = c.MetaTitle,
            MetaDescription = c.MetaDescription,
            Children = c.Children?.Select(MapToDto).ToList() ?? []
        };
    }
}