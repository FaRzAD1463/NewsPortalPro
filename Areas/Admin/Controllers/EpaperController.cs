using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Data;
using NewsPortalPro.Models;
using Microsoft.EntityFrameworkCore;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor")]
    public class EpaperController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _upload;
        private readonly IWebHostEnvironment _env;

        public EpaperController(
            ApplicationDbContext db,
            IFileUploadService upload,
            IWebHostEnvironment env)
        {
            _db = db;
            _upload = upload;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var epapers = await _db.Epapers
                .OrderByDescending(e => e.PublishedDate)
                .Take(30)
                .ToListAsync();
            return View(epapers);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(52_428_800)] // 50MB for PDF
        [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
        public async Task<IActionResult> Create(
            IFormFile pdfFile,
            IFormFile? coverImage,
            string title,
            DateTime publishedDate)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                TempData["Error"] = "PDF ফাইল নির্বাচন করুন";
                return View();
            }

            if (!pdfFile.FileName.EndsWith(".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "শুধুমাত্র PDF ফাইল গ্রহণযোগ্য";
                return View();
            }

            // Save PDF to wwwroot/epapers/
            var pdfFolder = Path.Combine(
                _env.WebRootPath, "epapers");
            Directory.CreateDirectory(pdfFolder);

            var pdfFileName = $"epaper-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
            var pdfPath = Path.Combine(pdfFolder, pdfFileName);

            await using (var stream = new FileStream(
                pdfPath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(stream);
            }

            // Save cover image if provided
            string? coverUrl = null;
            if (coverImage != null && coverImage.Length > 0)
            {
                try
                {
                    var result = await _upload.UploadImageAsync(
                        coverImage, "epapers");
                    coverUrl = result.Url;
                }
                catch { }
            }

            var epaper = new Epaper
            {
                Title = title,
                PdfUrl = $"/epapers/{pdfFileName}",
                CoverImageUrl = coverUrl,
                PublishedDate = publishedDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Epapers.Add(epaper);
            await _db.SaveChangesAsync();

            TempData["Success"] = "ই-পেপার সফলভাবে আপলোড হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var epaper = await _db.Epapers.FindAsync(id);
            if (epaper == null) return NotFound();

            // Delete PDF file from disk
            if (!string.IsNullOrEmpty(epaper.PdfUrl))
            {
                var filePath = Path.Combine(
                    _env.WebRootPath,
                    epaper.PdfUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _db.Epapers.Remove(epaper);
            await _db.SaveChangesAsync();

            TempData["Success"] = "ই-পেপার মুছে ফেলা হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(int id)
        {
            // Deactivate all, then activate selected
            await _db.Epapers.ExecuteUpdateAsync(
                s => s.SetProperty(e => e.IsActive, false));

            var epaper = await _db.Epapers.FindAsync(id);
            if (epaper != null)
            {
                epaper.IsActive = true;
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }
    }
}