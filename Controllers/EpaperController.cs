using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Controllers
{
    public class EpaperController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly INewsService _news;

        public EpaperController(
            ApplicationDbContext db,
            INewsService news)
        {
            _db = db;
            _news = news;
        }

        // Main e-paper page
        public async Task<IActionResult> Index()
        {
            var epaper = await _db.Epapers
                .Where(e => e.IsActive)
                .OrderByDescending(e => e.PublishedDate)
                .FirstOrDefaultAsync();

            if (epaper == null)
            {
                ViewBag.NoPaper = true;
                return View();
            }

            // Get all published news for sidebar
            var news = await _db.News
                .Where(n => n.Status == Models.NewsStatus.Published
                         && !n.IsDeleted)
                .OrderByDescending(n => n.PublishedAt)
                .Take(50)
                .Include(n => n.Category)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Slug,
                    n.FeaturedImage,
                    n.Summary,
                    n.PublishedAt,
                    CategoryName = n.Category.Name,
                    CategoryColor = n.Category.ColorCode
                })
                .ToListAsync();

            ViewBag.NewsJson = System.Text.Json.JsonSerializer
                .Serialize(news);

            return View(epaper);
        }

        // Previous editions
        public async Task<IActionResult> Archive()
        {
            var epapers = await _db.Epapers
                .OrderByDescending(e => e.PublishedDate)
                .ToListAsync();
            return View(epapers);
        }
    }
}