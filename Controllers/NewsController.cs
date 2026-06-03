using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;
using System.Security.Claims;

namespace NewsPortalPro.Controllers
{
    public class NewsController : Controller
    {
        private readonly INewsService _news;
        private readonly ICommentService _comments;
        private readonly IAdsService _ads;
        private readonly IAnalyticsService _analytics;

        public NewsController(
            INewsService news,
            ICommentService comments,
            IAdsService ads,
            IAnalyticsService analytics)
        {
            _news = news;
            _comments = comments;
            _ads = ads;
            _analytics = analytics;
        }

        [Route("news/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var news = await _news.GetBySlugAsync(slug);
            if (news == null) return NotFound();

            // Track view asynchronously
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _ = _news.IncrementViewAsync(news.Id, userId, ip,
                Request.Headers.UserAgent, Request.Headers.Referer);

            ViewBag.SidebarAds = await _ads.GetByPositionAsync(Models.AdPosition.Sidebar);
            ViewBag.InlineAd = (await _ads.GetByPositionAsync(Models.AdPosition.InlineTop)).FirstOrDefault();

            return View(news);
        }

        [Route("news")]
        public async Task<IActionResult> Index([FromQuery] NewsFilterDto filter)
        {
            filter.PageSize = 20;
            var result = await _news.GetPublishedAsync(filter);
            return View(result);
        }

        [Route("tag/{slug}/{page?}")]
        public async Task<IActionResult> ByTag(string slug, int page = 1)
        {
            var filter = new NewsFilterDto { TagSlug = slug, Page = page, PageSize = 20 };
            var result = await _news.GetPublishedAsync(filter);
            ViewBag.TagSlug = slug;
            return View("Index", result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> AddComment([FromBody] CreateCommentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "অবৈধ তথ্য" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var id = await _comments.AddAsync(dto, userId, ip);

            return Ok(new { success = true, commentId = id, message = "মন্তব্য সফলভাবে জমা দেওয়া হয়েছে" });
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> React([FromBody] ReactDto dto)
        {
            // Handled by API controller; proxy for convenience
            return Ok();
        }
    }

    public class ReactDto
    {
        public int NewsId { get; set; }
        public string ReactionType { get; set; } = "Like";
    }
}