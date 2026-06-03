using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using NewsPortalPro.Services;
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

        public NewsApiController(INewsService news, ICommentService comments)
        {
            _news = news;
            _comments = comments;
        }

        /// <summary>Get paginated published news</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] NewsFilterDto filter)
        {
            var result = await _news.GetPublishedAsync(filter);
            return Ok(result);
        }

        /// <summary>Get breaking news</summary>
        [HttpGet("breaking")]
        public async Task<IActionResult> GetBreaking([FromQuery] int count = 8)
        {
            var result = await _news.GetBreakingNewsAsync(count);
            return Ok(result);
        }

        /// <summary>Get featured news</summary>
        [HttpGet("featured")]
        public async Task<IActionResult> GetFeatured([FromQuery] int count = 6)
        {
            var result = await _news.GetFeaturedAsync(count);
            return Ok(result);
        }

        /// <summary>Get trending news</summary>
        [HttpGet("trending")]
        public async Task<IActionResult> GetTrending([FromQuery] int count = 10)
        {
            var result = await _news.GetTrendingAsync(count);
            return Ok(result);
        }

        /// <summary>Get most viewed news</summary>
        [HttpGet("most-viewed")]
        public async Task<IActionResult> GetMostViewed([FromQuery] int count = 10)
        {
            var result = await _news.GetMostViewedAsync(count);
            return Ok(result);
        }

        /// <summary>Get news by slug</summary>
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var result = await _news.GetBySlugAsync(slug);
            if (result == null) return NotFound(ApiResponse<string>.Fail("সংবাদ পাওয়া যায়নি"));
            return Ok(ApiResponse<NewsDetailDto>.Ok(result));
        }

        /// <summary>React to news</summary>
        [HttpPost("react")]
        [Authorize]
        public async Task<IActionResult> React([FromBody] ReactRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();

            var existing = db.Reactions
                .FirstOrDefault(r => r.NewsId == dto.NewsId && r.UserId == userId);

            if (existing != null)
            {
                if (existing.Type.ToString() == dto.ReactionType)
                {
                    db.Reactions.Remove(existing);
                }
                else
                {
                    existing.Type = Enum.Parse<ReactionType>(dto.ReactionType);
                }
            }
            else
            {
                db.Reactions.Add(new Reaction
                {
                    NewsId = dto.NewsId,
                    UserId = userId,
                    Type = Enum.Parse<ReactionType>(dto.ReactionType)
                });
            }

            await db.SaveChangesAsync();

            var counts = db.Reactions
                .Where(r => r.NewsId == dto.NewsId)
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            return Ok(new { success = true, counts });
        }

        /// <summary>Get related news</summary>
        [HttpGet("{id:int}/related")]
        public async Task<IActionResult> GetRelated(int id, [FromQuery] int count = 5)
        {
            var news = await _news.GetByIdAsync(id);
            if (news == null) return NotFound();
            var categoryId = await HttpContext.RequestServices
                .GetRequiredService<Data.ApplicationDbContext>()
                .News.Where(n => n.Id == id)
                .Select(n => n.CategoryId)
                .FirstOrDefaultAsync();
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