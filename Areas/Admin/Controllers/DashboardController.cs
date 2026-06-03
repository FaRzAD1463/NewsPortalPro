using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor")]
    public class DashboardController : Controller
    {
        private readonly IAnalyticsService _analytics;
        private readonly ICommentService _comments;
        private readonly ISettingsService _settings;

        public DashboardController(
            IAnalyticsService analytics,
            ICommentService comments,
            ISettingsService settings)
        {
            _analytics = analytics;
            _comments = comments;
            _settings = settings;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Stats = await _analytics.GetDashboardStatsAsync();
            ViewBag.DailyViews = await _analytics.GetDailyViewsAsync(30);
            ViewBag.TopNews = await _analytics.GetTopNewsAsync(10);
            ViewBag.CategoryStats = await _analytics.GetCategoryStatsAsync();
            ViewBag.PendingComments = await _comments.GetPendingCountAsync();
            return View();
        }
    }
}