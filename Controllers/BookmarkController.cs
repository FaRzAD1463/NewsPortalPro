using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.Models;
using System.Security.Claims;

namespace NewsPortalPro.Controllers
{
    [Authorize]
    public class BookmarkController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BookmarkController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var bookmarks = await _db.Bookmarks
                .Where(b => b.UserId == userId)
                .Include(b => b.News).ThenInclude(n => n.Category)
                .Include(b => b.News).ThenInclude(n => n.Author)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(bookmarks);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle([FromBody] BookmarkToggleDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var existing = await _db.Bookmarks
                .FirstOrDefaultAsync(b => b.UserId == userId && b.NewsId == dto.NewsId);

            bool isBookmarked;
            if (existing != null)
            {
                _db.Bookmarks.Remove(existing);
                isBookmarked = false;
            }
            else
            {
                _db.Bookmarks.Add(new Bookmark { UserId = userId, NewsId = dto.NewsId });
                isBookmarked = true;
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true, isBookmarked });
        }

        [HttpGet]
        public async Task<IActionResult> Check(int newsId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var exists = await _db.Bookmarks
                .AnyAsync(b => b.UserId == userId && b.NewsId == newsId);
            return Ok(new { isBookmarked = exists });
        }
    }

    public class BookmarkToggleDto
    {
        public int NewsId { get; set; }
    }
}