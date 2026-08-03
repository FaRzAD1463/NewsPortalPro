using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using NewsPortalPro.ViewModels;

namespace NewsPortalPro.Controllers
{
    public class HomeController(
        INewsService news,
        ICategoryService categories,
        IAdsService ads,
        ISettingsService settings,
        IPhotoService photos,
        IVideoService videos) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var vm = new HomeViewModel
            {
                BreakingNews = await news.GetBreakingNewsAsync(8),
                FeaturedNews = await news.GetFeaturedAsync(6),
                LatestNews = (await news.GetPublishedAsync(
                    new NewsFilterDto { Page = 1, PageSize = 12 })).Items,
                TrendingNews = await news.GetTrendingAsync(8),
                MostViewed = await news.GetMostViewedAsync(8),
                Categories = await categories.GetAllActiveAsync(),
                Photos = await photos.GetLatestAsync(8),
                Videos = await videos.GetLatestAsync(8),
                HeaderAds = await ads.GetByPositionAsync(Models.AdPosition.Header),
                SidebarAds = await ads.GetByPositionAsync(Models.AdPosition.Sidebar),
                SiteName = await settings.GetAsync("SiteName") ?? "NewsPortal Pro"
            };

            // Load ALL active categories — both menu and non-menu
            var allCats = await categories.GetAllActiveAsync();

            foreach (var cat in allCats)
            {
                var catNews = await news.GetByCategoryAsync(cat.Slug, 1, 6);
                if (catNews.Count > 0)
                    vm.CategoryNewsBlocks[cat.Slug] = (cat, catNews);
            }

            return View(vm);
        }

        public IActionResult Terms() => View();
        public IActionResult About() => View();

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}