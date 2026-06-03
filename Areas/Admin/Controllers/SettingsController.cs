using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;
using System.Security.Claims;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settings;

        public SettingsController(ISettingsService settings) => _settings = settings;

        public async Task<IActionResult> Index()
        {
            var general = await _settings.GetGroupAsync("General");
            var social = await _settings.GetGroupAsync("Social");
            var email = await _settings.GetGroupAsync("Email");
            var seo = await _settings.GetGroupAsync("SEO");
            var widgets = await _settings.GetGroupAsync("Widgets");
            var system = await _settings.GetGroupAsync("System");

            ViewBag.General = general;
            ViewBag.Social = social;
            ViewBag.Email = email;
            ViewBag.SEO = seo;
            ViewBag.Widgets = widgets;
            ViewBag.System = system;

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Dictionary<string, string> settings)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _settings.SetBulkAsync(settings, userId);
            TempData["Success"] = "সেটিংস সংরক্ষিত হয়েছে";
            return RedirectToAction(nameof(Index));
        }
    }
}