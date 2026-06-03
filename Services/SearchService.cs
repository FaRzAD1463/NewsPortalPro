using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Services
{
    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _db;

        public SearchService(ApplicationDbContext db) => _db = db;

        public async Task<SearchResultDto> SearchAsync(string query, int page = 1, int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new SearchResultDto { Query = query };

            var q = query.Trim();
            var dbQuery = _db.News
                .Where(n => n.Status == NewsStatus.Published && (
                    n.Title.Contains(q) ||
                    n.Content.Contains(q) ||
                    (n.Summary != null && n.Summary.Contains(q)) ||
                    n.NewsTags.Any(nt => nt.Tag.Name.Contains(q)) ||
                    n.Category.Name.Contains(q)))
                .Include(n => n.Category)
                .Include(n => n.Author)
                .Include(n => n.NewsTags).ThenInclude(nt => nt.Tag)
                .OrderByDescending(n => n.PublishedAt);

            var total = await dbQuery.CountAsync();
            var items = await dbQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new SearchResultDto
            {
                Query = query,
                TotalCount = total,
                Page = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                News = items.Select(n => new NewsListDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Slug = n.Slug,
                    Summary = n.Summary,
                    FeaturedImage = n.FeaturedImage,
                    CategoryName = n.Category?.Name ?? "",
                    CategorySlug = n.Category?.Slug ?? "",
                    AuthorName = n.Author?.FullName ?? "",
                    PublishedAt = n.PublishedAt,
                    ViewCount = n.ViewCount,
                    ReadTimeMinutes = n.ReadTimeMinutes,
                    Tags = n.NewsTags?.Select(nt => nt.Tag.Name).ToList() ?? []
                }).ToList()
            };
        }

        public async Task<List<string>> GetSuggestionsAsync(string query, int count = 8)
        {
            if (string.IsNullOrWhiteSpace(query)) return [];

            return await _db.News
                .Where(n => n.Status == NewsStatus.Published && n.Title.Contains(query))
                .OrderByDescending(n => n.ViewCount)
                .Take(count)
                .Select(n => n.Title)
                .ToListAsync();
        }
    }
}