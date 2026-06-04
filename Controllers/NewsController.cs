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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Fire and forget — non blocking
            _ = _news.IncrementViewAsync(
                news.Id, userId, ip,
                Request.Headers.UserAgent,
                Request.Headers.Referer);

            ViewBag.SidebarAds = await _ads
                .GetByPositionAsync(Models.AdPosition.Sidebar);

            return View(news);
        }

        [Route("news")]
        public async Task<IActionResult> Index([FromQuery] NewsFilterDto filter)
        {
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