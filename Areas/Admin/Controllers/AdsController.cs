using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using Microsoft.EntityFrameworkCore;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdsController : Controller
    {
        private readonly IAdsService _ads;
        private readonly IFileUploadService _upload;
        private readonly ApplicationDbContext _db;

        public AdsController(
            IAdsService ads,
            IFileUploadService upload,
            ApplicationDbContext db)
        {
            _ads = ads;
            _upload = upload;
            _db = db;
        }

        // ── Index ──────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var ads = await _ads.GetAllForAdminAsync();
            return View(ads);
        }

        // ── Create GET ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateViewBag();
            return View(new CreateAdDto
            {
                Status = AdStatus.Active,
                StartDate = DateTime.Today,
                DisplayOrder = 0
            });
        }

        // ── Create POST ────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(10_485_760)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
        public async Task<IActionResult> Create(
            CreateAdDto dto, IFormFile? imageFile)
        {
            // Upload image if provided
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    var result = await _upload.UploadImageAsync(
                        imageFile, "ads");
                    dto.ImageUrl = result.Url;
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError(
                        "imageFile", ex.Message);
                    await PopulateViewBag();
                    return View(dto);
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateViewBag();
                return View(dto);
            }

            await _ads.CreateAsync(dto);
            TempData["Success"] = "বিজ্ঞাপন সফলভাবে তৈরি হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // ── Edit GET ───────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Load directly by ID instead of loading all
            var ad = await _db.Advertisements.FindAsync(id);
            if (ad == null) return NotFound();

            await PopulateViewBag();

            return View(new UpdateAdDto
            {
                Id = ad.Id,
                Title = ad.Title,
                ImageUrl = ad.ImageUrl,
                TargetUrl = ad.TargetUrl,
                HtmlCode = ad.HtmlCode,
                Position = ad.Position,
                Status = ad.Status,
                DisplayOrder = ad.DisplayOrder,
                StartDate = ad.StartDate,
                EndDate = ad.EndDate,
                CategoryId = ad.CategoryId
            });
        }

        // ── Edit POST ──────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(10_485_760)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
        public async Task<IActionResult> Edit(
            int id, UpdateAdDto dto, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    var result = await _upload.UploadImageAsync(
                        imageFile, "ads");
                    dto.ImageUrl = result.Url;
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError(
                        "imageFile", ex.Message);
                    await PopulateViewBag();
                    return View(dto);
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateViewBag();
                return View(dto);
            }

            await _ads.UpdateAsync(id, dto);
            TempData["Success"] = "বিজ্ঞাপন আপডেট হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // ── Delete ─────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _ads.DeleteAsync(id);
            TempData["Success"] = "বিজ্ঞাপন মুছে ফেলা হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // ── Toggle Status ──────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var ad = await _db.Advertisements.FindAsync(id);
            if (ad == null) return NotFound();

            ad.Status = ad.Status == AdStatus.Active
                ? AdStatus.Inactive
                : AdStatus.Active;

            await _db.SaveChangesAsync();
            return Ok(new
            {
                success = true,
                status = ad.Status.ToString()
            });
        }

        // ── Populate ViewBag ───────────────────────────────────
        private async Task PopulateViewBag()
        {
            ViewBag.Positions = Enum.GetValues<AdPosition>()
                .Select(p => new SelectListItem
                {
                    Value = ((int)p).ToString(),
                    Text = p switch
                    {
                        AdPosition.Header => "হেডার",
                        AdPosition.Sidebar => "সাইডবার",
                        AdPosition.Footer => "ফুটার",
                        AdPosition.InArticle => "আর্টিকেলের মধ্যে",
                        AdPosition.Popup => "পপআপ",
                        AdPosition.BelowTitle => "শিরোনামের নিচে",
                        _ => p.ToString()
                    }
                }).ToList();

            ViewBag.Statuses = Enum.GetValues<AdStatus>()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s switch
                    {
                        AdStatus.Active => "সক্রিয়",
                        AdStatus.Inactive => "নিষ্ক্রিয়",
                        AdStatus.Expired => "মেয়াদ শেষ",
                        _ => s.ToString()
                    }
                }).ToList();

            ViewBag.Categories = await _db.Categories
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }
    }
}