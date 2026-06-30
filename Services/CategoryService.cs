using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
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

        public CategoryService(
            ApplicationDbContext db,
            IDistributedCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<List<CategoryDto>> GetAllActiveAsync()
        {
            try
            {
                var cached = await _cache.GetStringAsync(CacheKey);
                if (cached != null)
                    return JsonConvert
                        .DeserializeObject<List<CategoryDto>>(cached)!;
            }
            catch { }

            // Load flat list — no recursive Include to avoid
            // circular reference issues with EF Core
            var cats = await _db.Categories
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();

            var dtos = cats.Select(c => MapToDto(c, cats)).ToList();

            try
            {
                await _cache.SetStringAsync(
                    CacheKey,
                    JsonConvert.SerializeObject(dtos),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromHours(1)
                    });
            }
            catch { }

            return dtos;
        }

        public async Task<List<CategoryDto>> GetMenuCategoriesAsync()
        {
            var all = await GetAllActiveAsync();
            return all.Where(c => c.ShowInMenu).ToList();
        }

        public async Task<CategoryDto?> GetBySlugAsync(string slug)
        {
            var cat = await _db.Categories
                .Where(c => c.Slug == slug
                         && c.IsActive
                         && !c.IsDeleted)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return cat == null ? null : MapToDto(cat, null);
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var cat = await _db.Categories
                .Where(c => c.Id == id && !c.IsDeleted)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return cat == null ? null : MapToDto(cat, null);
        }

        public async Task<int> CreateAsync(CreateCategoryDto dto)
        {
            var helper = new SlugHelper();
            var slug = helper.GenerateSlug(dto.Name);

            if (string.IsNullOrEmpty(slug))
                slug = $"category-{DateTimeOffset.UtcNow
                    .ToUnixTimeSeconds()}";

            var exists = await _db.Categories
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Slug == slug);

            if (exists)
                slug += $"-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    % 1000}";

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

            try { await _cache.RemoveAsync(CacheKey); } catch { }

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
            try { await _cache.RemoveAsync(CacheKey); } catch { }
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return false;
            cat.IsDeleted = true;
            cat.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            try { await _cache.RemoveAsync(CacheKey); } catch { }
            return true;
        }

        public async Task<List<CategoryWithCountDto>>
            GetWithNewsCountAsync() =>
            await _db.Categories
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new CategoryWithCountDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ColorCode = c.ColorCode,
                    IsActive = c.IsActive,
                    ShowInMenu = c.ShowInMenu,
                    DisplayOrder = c.DisplayOrder,
                    ParentId = c.ParentId,
                    NewsCount = c.News.Count(
                        n => n.Status == NewsStatus.Published
                          && !n.IsDeleted)
                })
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

        // ── MapToDto — flat mapping, children resolved from list ──
        private static CategoryDto MapToDto(
            Category c,
            List<Category>? allCats)
        {
            var dto = new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                ColorCode = c.ColorCode,
                ParentId = c.ParentId,
                ParentName = null, // loaded separately if needed
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                ShowInMenu = c.ShowInMenu,
                MetaTitle = c.MetaTitle,
                MetaDescription = c.MetaDescription,
                Children = []
            };

            // Resolve children from the flat list if available
            if (allCats != null)
            {
                dto.Children = allCats
                    .Where(ch => ch.ParentId == c.Id
                              && ch.IsActive
                              && !ch.IsDeleted)
                    .Select(ch => MapToDto(ch, null))
                    .ToList();
            }

            return dto;
        }
    }
}