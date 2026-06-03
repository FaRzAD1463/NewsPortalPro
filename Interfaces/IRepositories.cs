using NewsPortalPro.DTOs;
using NewsPortalPro.Models;

namespace NewsPortalPro.Interfaces
{
    public interface INewsRepository
    {
        Task<News?> GetByIdAsync(int id);
        Task<News?> GetBySlugAsync(string slug);
        Task<IQueryable<News>> GetQueryableAsync();
        Task<int> AddAsync(News news);
        Task UpdateAsync(News news);
        Task DeleteAsync(int id);
        Task<int> CountAsync(NewsStatus? status = null);
    }

    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(int id);
        Task<Category?> GetBySlugAsync(string slug);
        Task<List<Category>> GetAllActiveAsync();
        Task<int> AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(int id);
    }

    public interface IUnitOfWork : IDisposable
    {
        INewsRepository News { get; }
        ICategoryRepository Categories { get; }
        Task<int> SaveChangesAsync();
    }
}