using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdsController : Controller
    {
        private readonly IAdsService _ads;

        public AdsController(IAdsService ads) => _ads = ads;

        public async Task<IActionResult> Index()
        {
            var ads = await _ads.GetAllForAdminAsync();
            return View(ads);
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateAdDto());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAdDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            await _ads.CreateAsync(dto);
            TempData["Success"] = "বিজ্ঞাপন তৈরি হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ads = await _ads.GetAllForAdminAsync();
            var ad = ads.FirstOrDefault(a => a.Id == id);
            if (ad == null) return NotFound();
            return View(new UpdateAdDto
            {
                Id = ad.Id,
                Title = ad.Title,
                ImageUrl = ad.ImageUrl,
                TargetUrl = ad.TargetUrl,
                HtmlCode = ad.HtmlCode,
                Position = ad.Position,
                Status = ad.Status,
                StartDate = ad.StartDate,
                EndDate = ad.EndDate,
                CategoryId = ad.CategoryId
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateAdDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            await _ads.UpdateAsync(id, dto);
            TempData["Success"] = "বিজ্ঞাপন আপডেট হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _ads.DeleteAsync(id);
            TempData["Success"] = "বিজ্ঞাপন মুছে ফেলা হয়েছে";
            return RedirectToAction(nameof(Index));
        }
    }
}