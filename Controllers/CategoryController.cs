using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Controllers
{
    [Route("category/{slug}")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categories;
        private readonly INewsService _news;
        private readonly IAdsService _ads;

        public CategoryController(
            ICategoryService categories,
            INewsService news,
            IAdsService ads)
        {
            _categories = categories;
            _news = news;
            _ads = ads;
        }

        public async Task<IActionResult> Index(
            string slug, int page = 1, string sort = "latest")
        {
            var category = await _categories.GetBySlugAsync(slug);
            if (category == null) return NotFound();

            var filter = new NewsFilterDto
            {
                CategorySlug = slug,
                Page = page,
                PageSize = 16,
                Sort = sort
            };

            var news = await _news.GetPublishedAsync(filter);
            var trending = await _news.GetTrendingAsync(8);

            ViewBag.Category = category;
            ViewBag.Sort = sort;
            ViewBag.Trending = trending;
            ViewBag.SidebarAds = await _ads.GetByPositionAsync(
                Models.AdPosition.Sidebar);

            return View(news);
        }

        [HttpGet("more")]
        public async Task<IActionResult> LoadMore(
            string slug, int page, string sort = "latest")
        {
            var filter = new NewsFilterDto
            {
                CategorySlug = slug,
                Page = page,
                PageSize = 10,
                Sort = sort
            };
            var news = await _news.GetPublishedAsync(filter);
            return PartialView("_CatNewsListItem", news.Items);
        }
    }
}