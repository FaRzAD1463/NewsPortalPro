using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using System.Security.Claims;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController(ISettingsService settings) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var general = await settings.GetGroupAsync("General");
            var social = await settings.GetGroupAsync("Social");
            var email = await settings.GetGroupAsync("Email");
            var seo = await settings.GetGroupAsync("SEO");
            var widgets = await settings.GetGroupAsync("Widgets");
            var system = await settings.GetGroupAsync("System");

            ViewBag.General = general;
            ViewBag.Social = social;
            ViewBag.Email = email;
            ViewBag.SEO = seo;
            ViewBag.Widgets = widgets;
            ViewBag.System = system;

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Dictionary<string, string> settingsForm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await settings.SetBulkAsync(settingsForm, userId);
            TempData["Success"] = "সেটিংস সংরক্ষিত হয়েছে";
            return RedirectToAction(nameof(Index));
        }
    }
}