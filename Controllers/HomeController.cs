using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;
using NewsPortalPro.ViewModels;

namespace NewsPortalPro.Controllers
{
    public class HomeController : Controller
    {
        private readonly INewsService _news;
        private readonly ICategoryService _categories;
        private readonly IAdsService _ads;
        private readonly ISettingsService _settings;
        private readonly Models.AdPosition _adPos = Models.AdPosition.Header;

        public HomeController(
            INewsService news,
            ICategoryService categories,
            IAdsService ads,
            ISettingsService settings)
        {
            _news = news;
            _categories = categories;
            _ads = ads;
            _settings = settings;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new HomeViewModel
            {
                BreakingNews = await _news.GetBreakingNewsAsync(8),
                FeaturedNews = await _news.GetFeaturedAsync(6),
                LatestNews = (await _news.GetPublishedAsync(
                    new NewsFilterDto { Page = 1, PageSize = 12 })).Items,
                TrendingNews = await _news.GetTrendingAsync(8),
                MostViewed = await _news.GetMostViewedAsync(8),
                Categories = await _categories.GetAllActiveAsync(),
                HeaderAds = await _ads.GetByPositionAsync(Models.AdPosition.Header),
                SidebarAds = await _ads.GetByPositionAsync(Models.AdPosition.Sidebar),
                SiteName = await _settings.GetAsync("SiteName") ?? "NewsPortal Pro"
            };

            // Load ALL active categories — both menu and non-menu
            var allCats = await _categories.GetAllActiveAsync();

            foreach (var cat in allCats)
            {
                var catNews = await _news.GetByCategoryAsync(cat.Slug, 1, 6);
                if (catNews.Any())
                    vm.CategoryNewsBlocks[cat.Slug] = (cat, catNews);
            }

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}