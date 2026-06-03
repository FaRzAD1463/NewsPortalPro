using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISettingsService _settings;

        public CommentService(ApplicationDbContext db, ISettingsService settings)
        {
            _db = db;
            _settings = settings;
        }

        public async Task<List<CommentDto>> GetByNewsIdAsync(int newsId)
        {
            var comments = await _db.Comments
                .Where(c => c.NewsId == newsId && c.Status == CommentStatus.Approved && c.ParentId == null)
                .Include(c => c.User)
                .Include(c => c.Replies.Where(r => r.Status == CommentStatus.Approved))
                    .ThenInclude(r => r.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(MapToDto).ToList();
        }

        public async Task<PagedResult<CommentDto>> GetPendingAsync(int page, int pageSize)
        {
            var query = _db.Comments
                .Where(c => c.Status == CommentStatus.Pending)
                .Include(c => c.User)
                .Include(c => c.News)
                .OrderByDescending(c => c.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<CommentDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<int> AddAsync(CreateCommentDto dto, string userId, string ip)
        {
            var moderation = await _settings.GetAsync("CommentModeration") ?? "true";
            var status = moderation == "true" ? CommentStatus.Pending : CommentStatus.Approved;

            var comment = new Comment
            {
                Content = Ganss.Xss.HtmlSanitizer.Default.Sanitize(dto.Content),
                NewsId = dto.NewsId,
                UserId = userId,
                ParentId = dto.ParentId,
                Status = status,
                IpAddress = ip
            };

            _db.Comments.Add(comment);

            if (status == CommentStatus.Approved)
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE News SET CommentCount = CommentCount + 1 WHERE Id = {0}", dto.NewsId);

            await _db.SaveChangesAsync();
            return comment.Id;
        }

        public async Task<bool> ApproveAsync(int id)
        {
            var comment = await _db.Comments.FindAsync(id);
            if (comment == null) return false;
            comment.Status = CommentStatus.Approved;
            comment.UpdatedAt = DateTime.UtcNow;
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE News SET CommentCount = CommentCount + 1 WHERE Id = {0}", comment.NewsId);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAsync(int id)
        {
            var comment = await _db.Comments.FindAsync(id);
            if (comment == null) return false;
            comment.Status = CommentStatus.Rejected;
            comment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var comment = await _db.Comments.FindAsync(id);
            if (comment == null) return false;
            comment.IsDeleted = true;
            if (comment.Status == CommentStatus.Approved)
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE News SET CommentCount = GREATEST(CommentCount - 1, 0) WHERE Id = {0}", comment.NewsId);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetPendingCountAsync() =>
            await _db.Comments.CountAsync(c => c.Status == CommentStatus.Pending);

        private static CommentDto MapToDto(Comment c) => new()
        {
            Id = c.Id,
            Content = c.Content,
            UserId = c.UserId,
            UserName = c.User?.FullName ?? "Anonymous",
            UserAvatar = c.User?.ProfilePicture,
            NewsId = c.NewsId,
            NewsTitle = c.News?.Title ?? string.Empty,
            NewsSlug = c.News?.Slug ?? string.Empty,
            ParentId = c.ParentId,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            Replies = c.Replies?.Where(r => r.Status == CommentStatus.Approved)
                .Select(r => new CommentDto
                {
                    Id = r.Id,
                    Content = r.Content,
                    UserId = r.UserId,
                    UserName = r.User?.FullName ?? "Anonymous",
                    UserAvatar = r.User?.ProfilePicture,
                    NewsId = r.NewsId,
                    CreatedAt = r.CreatedAt
                }).ToList() ?? []
        };
    }
}