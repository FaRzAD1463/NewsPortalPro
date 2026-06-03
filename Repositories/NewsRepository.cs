using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Repositories
{
    public class NewsRepository : INewsRepository
    {
        private readonly ApplicationDbContext _db;

        public NewsRepository(ApplicationDbContext db) => _db = db;

        public async Task<News?> GetByIdAsync(int id) =>
            await _db.News
                .Include(n => n.Category)
                .Include(n => n.Author)
                .Include(n => n.NewsTags).ThenInclude(nt => nt.Tag)
                .FirstOrDefaultAsync(n => n.Id == id);

        public async Task<News?> GetBySlugAsync(string slug) =>
            await _db.News
                .Include(n => n.Category)
                .Include(n => n.Author)
                .Include(n => n.NewsTags).ThenInclude(nt => nt.Tag)
                .FirstOrDefaultAsync(n => n.Slug == slug);

        public Task<IQueryable<News>> GetQueryableAsync() =>
            Task.FromResult(_db.News.AsQueryable());

        public async Task<int> AddAsync(News news)
        {
            _db.News.Add(news);
            await _db.SaveChangesAsync();
            return news.Id;
        }

        public async Task UpdateAsync(News news)
        {
            news.UpdatedAt = DateTime.UtcNow;
            _db.News.Update(news);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var news = await _db.News.FindAsync(id);
            if (news != null)
            {
                news.IsDeleted = true;
                news.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<int> CountAsync(NewsStatus? status = null)
        {
            var query = _db.News.AsQueryable();
            if (status.HasValue) query = query.Where(n => n.Status == status.Value);
            return await query.CountAsync();
        }
    }

    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _db;

        public CategoryRepository(ApplicationDbContext db) => _db = db;

        public async Task<Category?> GetByIdAsync(int id) =>
            await _db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id);

        public async Task<Category?> GetBySlugAsync(string slug) =>
            await _db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Slug == slug);

        public async Task<List<Category>> GetAllActiveAsync() =>
            await _db.Categories
                .Where(c => c.IsActive)
                .Include(c => c.Children)
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

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;
        public INewsRepository News { get; }
        public ICategoryRepository Categories { get; }

        public UnitOfWork(ApplicationDbContext db, INewsRepository news, ICategoryRepository cats)
        {
            _db = db;
            News = news;
            Categories = cats;
        }

        public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
        public void Dispose() => _db.Dispose();
    }
}