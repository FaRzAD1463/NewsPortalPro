using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public NewsApiController(
            INewsService news,
            ICommentService comments,
            ApplicationDbContext db)
        {
            _news = news;
            _comments = comments;
            _db = db;
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

            await _db.SaveChangesAsync();

            var counts = await _db.Reactions
                .Where(r => r.NewsId == dto.NewsId)
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                counts = counts.ToDictionary(x => x.Type, x => x.Count)
            });
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
    }

    public class ReactRequestDto
    {
        public int NewsId { get; set; }
        public string ReactionType { get; set; } = "Like";
    }
}