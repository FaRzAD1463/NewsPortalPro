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
            if (status.HasValue)
                query = query.Where(n => n.Status == status.Value);
            return await query.CountAsync();
        }
    }
}