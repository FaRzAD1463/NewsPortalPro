using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Repositories
{
        public class CategoryRepository : ICategoryRepository
        {
        private readonly ApplicationDbContext _db;

        public CategoryRepository(ApplicationDbContext db) => _db = db;

        public async Task<Category?> GetByIdAsync(int id) =>
            await _db.Categories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<Category?> GetBySlugAsync(string slug) =>
            await _db.Categories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Slug == slug);

        public async Task<List<Category>> GetAllActiveAsync() =>
            await _db.Categories
                .Where(c => c.IsActive)
                .Include(c => c.Children.Where(ch => ch.IsActive))
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

        public async Task<int> AddAsync(Category category)
        {
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return category.Id;
        }

        public async Task UpdateAsync(Category category)
        {
            category.UpdatedAt = DateTime.UtcNow;
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var cat = await _db.Categories.FindAsync(id);
            if (cat != null)
            {
                cat.IsDeleted = true;
                cat.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
        }
}