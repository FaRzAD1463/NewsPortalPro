using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : Controller
    {
        private readonly IAnalyticsService _analytics;

        public AnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

        public async Task<IActionResult> Index()
        {
            ViewBag.DailyViews = await _analytics.GetDailyViewsAsync(30);
            ViewBag.TopNews = await _analytics.GetTopNewsAsync(20, 30);
            ViewBag.CategoryStats = await _analytics.GetCategoryStatsAsync();
            ViewBag.VisitorStats = await _analytics.GetVisitorStatsAsync(30);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDailyViews(int days = 30)
        {
            var data = await _analytics.GetDailyViewsAsync(days);
            return Ok(data);
        }
    }
}