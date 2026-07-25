using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor")]
    public class PhotosController : Controller
    {
        private readonly IPhotoService _photos;
        private readonly IFileUploadService _upload;

        public PhotosController(IPhotoService photos, IFileUploadService upload)
        {
            _photos = photos;
            _upload = upload;
        }

        public async Task<IActionResult> Index()
        {
            var photos = await _photos.GetAllForAdminAsync();
            return View(photos);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(10_485_760)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
        public async Task<IActionResult> Create(
            IFormFile imageFile, string? altText, string? caption, int displayOrder)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "ছবি নির্বাচন করুন");
                return View();
            }

            try
            {
                var result = await _upload.UploadImageAsync(imageFile, "photos");
                await _photos.CreateAsync(result.Url, altText, caption, displayOrder);
                TempData["Success"] = "ছবি সফলভাবে যোগ হয়েছে";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
                return View();
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _photos.DeleteAsync(id);
            return Ok(new { success = deleted });
        }
    }
}