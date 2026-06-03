using NewsPortalPro.Data;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;

        public INewsRepository News { get; }
        public ICategoryRepository Categories { get; }

        public UnitOfWork(
            ApplicationDbContext db,
            INewsRepository news,
            ICategoryRepository categories)
        {
            _db = db;
            News = news;
            Categories = categories;
        }

        public async Task<int> SaveChangesAsync() =>
            await _db.SaveChangesAsync();

        public void Dispose() => _db.Dispose();
    }
}