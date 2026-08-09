using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using System.Security.Claims;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/news")]
    [Produces("application/json")]
    public class NewsApiController : ControllerBase
    {
        private readonly INewsService _news;
        private readonly ICommentService _comments;
        private readonly ApplicationDbContext _db;
        private readonly IDistributedCache _cache;

        public NewsApiController(
            INewsService news,
            ICommentService comments,
            ApplicationDbContext db,
            IDistributedCache cache)
        {
            _news = news;
            _comments = comments;
            _db = db;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] NewsFilterDto filter)
        {
            var result = await _news.GetPublishedAsync(filter);
            return Ok(result);
        }

        [HttpGet("breaking")]
        public async Task<IActionResult> GetBreaking([FromQuery] int count = 8)
        {
            var result = await _news.GetBreakingNewsAsync(count);
            return Ok(result);
        }

        [HttpGet("featured")]
        public async Task<IActionResult> GetFeatured([FromQuery] int count = 6)
        {
            var result = await _news.GetFeaturedAsync(count);
            return Ok(result);
        }

        [HttpGet("trending")]
        public async Task<IActionResult> GetTrending([FromQuery] int count = 10)
        {
            var result = await _news.GetTrendingAsync(count);
            return Ok(result);
        }

        [HttpGet("most-viewed")]
        public async Task<IActionResult> GetMostViewed([FromQuery] int count = 10)
        {
            var result = await _news.GetMostViewedAsync(count);
            return Ok(result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var result = await _news.GetBySlugAsync(slug);
            if (result == null)
                return NotFound(ApiResponse<string>.Fail("সংবাদ পাওয়া যায়নি"));
            return Ok(ApiResponse<NewsDetailDto>.Ok(result));
        }

        // ── React ────────────────────────────────────────────────────
        // Two rapid clicks on the same reaction button (double-click,
        // or a retried request after a slow network response) can both
        // read "no existing reaction" before either inserts — the
        // unique index on (NewsId, UserId) then rejects the second
        // SaveChangesAsync(). Instead of that surfacing as an unhandled
        // 500, treat it as a harmless race: detach our conflicting
        // tracked entity and return the current, correct counts.

        [HttpPost("react")]
        [Authorize]
        public async Task<IActionResult> React([FromBody] ReactRequestDto dto)
        {
            // FIX: Enum.Parse threw an unhandled exception (→ 500) if the
            // client sent an invalid ReactionType string. Using TryParse
            // and returning a clean 400 instead.

            if (!Enum.TryParse<ReactionType>(dto.ReactionType, true, out var reactionType))
                return BadRequest(new { success = false, message = "অবৈধ রিঅ্যাকশন টাইপ" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var existing = await _db.Reactions
                .FirstOrDefaultAsync(r =>
                    r.NewsId == dto.NewsId && r.UserId == userId);

            if (existing != null)
            {
                if (existing.Type == reactionType)
                    _db.Reactions.Remove(existing);
                else
                    existing.Type = reactionType;
            }
            else
            {
                _db.Reactions.Add(new Reaction
                {
                    NewsId = dto.NewsId,
                    UserId = userId,
                    Type = reactionType
                });
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                var conflictingEntry = _db.ChangeTracker.Entries<Reaction>()
                    .FirstOrDefault(e => e.Entity.NewsId == dto.NewsId && e.Entity.UserId == userId);
                if (conflictingEntry != null)
                    conflictingEntry.State = EntityState.Detached;
            }

            var counts = await _db.Reactions
                .Where(r => r.NewsId == dto.NewsId)
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // ── Invalidate the cached article so its Reactions dictionary
            // (used by GetBySlugAsync) reflects this change immediately
            // instead of showing stale counts for up to 5 minutes. The
            // live counts already returned above are correct regardless —
            // this only fixes the next full-article fetch (e.g. a reload).

            var slug = await _db.News
                .Where(n => n.Id == dto.NewsId)
                .Select(n => n.Slug)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(slug))
            {
                try { await _cache.RemoveAsync($"news:slug:{slug}"); }
                catch { }
            }

            return Ok(new
            {
                success = true,
                counts = counts.ToDictionary(x => x.Type, x => x.Count)
            });
        }

        // ── My Reaction ──────────────────────────────────────────────
        // Returns the current user's existing reaction (if any) on this
        // article, so the article page can correctly restore which
        // button should show as "reacted" on load. Without this, the
        // frontend has no way to know about a reaction made in a
        // previous session, and the toggle logic in reactToNews() on
        // the client gets out of sync with the DB.

        [HttpGet("{id:int}/my-reaction")]
        [Authorize]
        public async Task<IActionResult> GetMyReaction(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var reaction = await _db.Reactions
                .Where(r => r.NewsId == id && r.UserId == userId)
                .Select(r => r.Type.ToString())
                .FirstOrDefaultAsync();

            return Ok(new { reactionType = reaction });
        }

        [HttpGet("{id:int}/related")]
        public async Task<IActionResult> GetRelated(int id, [FromQuery] int count = 5)
        {
            // Get categoryId as a concrete value first
            var categoryId = await _db.News
                .Where(n => n.Id == id)
                .Select(n => n.CategoryId)
                .FirstOrDefaultAsync();

            if (categoryId == 0) return NotFound();

            var related = await _news.GetRelatedAsync(id, categoryId, count);
            return Ok(related);
        }

        [HttpGet("{id:int}/detail")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var news = await _db.News
                .Where(n => n.Id == id && n.Status == NewsStatus.Published)
                .Include(n => n.Category)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Slug,
                    n.Summary,
                    n.Content,
                    n.FeaturedImage,
                    n.PublishedAt,
                    CategoryName = n.Category.Name,
                    CategorySlug = n.Category.Slug,
                    CategoryColor = n.Category.ColorCode
                })
                .FirstOrDefaultAsync();

            if (news == null) return NotFound();
            return Ok(news);
        }
    }



    public class ReactRequestDto
    {
        public int NewsId { get; set; }
        public string ReactionType { get; set; } = "Like";
    }
}