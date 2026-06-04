using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using System.Security.Claims;

namespace NewsPortalPro.Controllers
{
    public class NewsController : Controller
    {
        private readonly INewsService _news;
        private readonly ICommentService _comments;
        private readonly IAdsService _ads;
        private readonly IAnalyticsService _analytics;
        private readonly IServiceScopeFactory _scopeFactory;

        public NewsController(
            INewsService news,
            ICommentService comments,
            IAdsService ads,
            IAnalyticsService analytics,
            IServiceScopeFactory scopeFactory)
        {
            _news = news;
            _comments = comments;
            _ads = ads;
            _analytics = analytics;
            _scopeFactory = scopeFactory;
        }

        [Route("news/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var news = await _news.GetBySlugAsync(slug);
            if (news == null) return NotFound();

            // Get sidebar ads BEFORE firing view increment
            ViewBag.SidebarAds = await _ads
                .GetByPositionAsync(Models.AdPosition.Sidebar);

            // Fire and forget with its OWN scope — fixes DbContext concurrency
            var newsId = news.Id;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();
            var referrer = Request.Headers.Referer.ToString();

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var newsService = scope.ServiceProvider
                    .GetRequiredService<INewsService>();
                try
                {
                    await newsService.IncrementViewAsync(
                        newsId, userId, ip, userAgent, referrer);
                }
                catch { /* non-critical */ }
            });

            return View(news);
        }

        [Route("news")]
        public async Task<IActionResult> Index([FromQuery] NewsFilterDto filter)
        {
            filter.Page = filter.Page == 0 ? 1 : filter.Page;
            filter.PageSize = 20;
            var result = await _news.GetPublishedAsync(filter);
            ViewBag.Filter = filter;
            return View(result);
        }

        [Route("tag/{slug}/{page?}")]
        public async Task<IActionResult> ByTag(string slug, int page = 1)
        {
            var filter = new NewsFilterDto
            {
                TagSlug = slug,
                Page = page,
                PageSize = 20
            };
            var result = await _news.GetPublishedAsync(filter);
            ViewBag.TagSlug = slug;
            return View("Index", result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddComment(
            [FromBody] CreateCommentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    message = "অবৈধ তথ্য"
                });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var id = await _comments.AddAsync(dto, userId, ip);

            return Ok(new
            {
                success = true,
                commentId = id,
                message = "মন্তব্য সফলভাবে জমা দেওয়া হয়েছে"
            });
        }
    }
}