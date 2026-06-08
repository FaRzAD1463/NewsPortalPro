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
        private readonly ISettingsService _settings;

        public NewsController(
            INewsService news,
            ICommentService comments,
            IAdsService ads,
            IAnalyticsService analytics,
            IServiceScopeFactory scopeFactory,
            ISettingsService settings)
        {
            _news = news;
            _comments = comments;
            _ads = ads;
            _analytics = analytics;
            _scopeFactory = scopeFactory;
            _settings = settings;
        }

        [HttpGet]
        [Route("news/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var news = await _news.GetBySlugAsync(slug);
            if (news == null) return NotFound();

            ViewBag.SidebarAds = await _ads
                .GetByPositionAsync(Models.AdPosition.Sidebar);

            var newsId = news.Id;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();
            var referrer = Request.Headers.Referer.ToString();

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider
                    .GetRequiredService<INewsService>();
                try
                {
                    await svc.IncrementViewAsync(
                        newsId, userId, ip, userAgent, referrer);
                }
                catch { }
            });

            return View(news);
        }

        [HttpGet]
        [Route("news")]
        public async Task<IActionResult> Index([FromQuery] NewsFilterDto filter)
        {
            filter.Page = filter.Page == 0 ? 1 : filter.Page;
            filter.PageSize = 20;
            var result = await _news.GetPublishedAsync(filter);
            ViewBag.Filter = filter;
            return View(result);
        }

        [HttpGet]
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

        // ── AddComment ─────────────────────────────────────────
        // Explicit route prevents conflict with news/{slug} route.
        // Token sent via "RequestVerificationToken" header
        // matching HeaderName in Program.cs antiforgery config.
        [HttpPost]
        [Route("News/AddComment")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddComment(
            [FromBody] CreateCommentDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new
                {
                    success = false,
                    message = string.Join(", ", errors)
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var id = await _comments.AddAsync(dto, userId, ip);

            var moderation =
                await _settings.GetAsync("CommentModeration") ?? "true";

            return Ok(new
            {
                success = true,
                commentId = id,
                isPending = moderation == "true",
                message = moderation == "true"
                    ? "মন্তব্য অনুমোদনের অপেক্ষায় আছে"
                    : "মন্তব্য সফলভাবে যোগ হয়েছে"
            });
        }
    }
}