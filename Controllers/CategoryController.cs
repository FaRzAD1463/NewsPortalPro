using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Controllers
{
    [Route("category/{slug}")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categories;
        private readonly INewsService _news;
        private readonly IAdsService _ads;

        public CategoryController(ICategoryService categories, INewsService news, IAdsService ads)
        {
            _categories = categories;
            _news = news;
            _ads = ads;
        }

        public async Task<IActionResult> Index(string slug, int page = 1)
        {
            var category = await _categories.GetBySlugAsync(slug);
            if (category == null) return NotFound();

            var filter = new NewsFilterDto
            {
                CategorySlug = slug,
                Page = page,
                PageSize = 20
            };

            var news = await _news.GetPublishedAsync(filter);
            ViewBag.Category = category;
            ViewBag.SidebarAds = await _ads.GetByPositionAsync(Models.AdPosition.Sidebar, category.Id);

            return View(news);
        }

        [HttpGet("more")]
        public async Task<IActionResult> LoadMore(string slug, int page)
        {
            var filter = new NewsFilterDto { CategorySlug = slug, Page = page, PageSize = 10 };
            var news = await _news.GetPublishedAsync(filter);
            return PartialView("_NewsCardList", news.Items);
        }
    }
}