using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using NewsPortalPro.Services;
using System.Security.Claims;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor,Reporter")]
    public class MediaController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _upload;
        private readonly ILogger<MediaController> _logger;

        public MediaController(ApplicationDbContext db, IFileUploadService upload, ILogger<MediaController> logger)
        {
            _db = db;
            _upload = upload;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var pageSize = 30;
            var total = await _db.Photos.CountAsync();
            var photos = await _db.Photos
                .OrderByDescending(p => p.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            return View(photos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10_485_760)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "ফাইল নির্বাচন করুন" });

            try
            {
                var result = await _upload.UploadImageAsync(file, "media");
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var photo = new Photo
                {
                    ImageUrl = result.Url,
                    ThumbnailUrl = result.ThumbnailUrl,
                    FileSizeBytes = result.FileSizeBytes,
                    Width = result.Width,
                    Height = result.Height,
                    UploadedById = userId
                };

                _db.Photos.Add(photo);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    id = photo.Id,
                    url = result.Url,
                    thumbnail = result.ThumbnailUrl
                });
            }
            catch (ArgumentException ex)
            {
                // Validation-type errors (bad file type, too large, etc.)
                // are safe to relay — they describe the input, not internals.

                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // FIX: previously returned ex.Message directly to the client,
                // which can leak storage paths, provider details, or other
                // internal info. Logged server-side instead; client gets a
                // generic message.

                _logger.LogError(ex, "Media upload failed");
                return BadRequest(new { success = false, message = "আপলোড ব্যর্থ হয়েছে, পরে আবার চেষ্টা করুন" });
            }
        }

        // FIX: added the same Reporter-ownership check used in
        // NewsController — previously any Reporter could delete media
        // uploaded by any other user, not just their own.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var photo = await _db.Photos.FindAsync(id);
            if (photo == null) return NotFound();

            if (User.IsInRole("Reporter")
                && !User.IsInRole("Admin")
                && !User.IsInRole("Editor"))
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (photo.UploadedById != currentUserId)
                {
                    _logger.LogWarning(
                        "IDOR attempt: User {UserId} tried to delete " +
                        "photo {PhotoId}", currentUserId, id);
                    return Forbid();
                }
            }

            _db.Photos.Remove(photo);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var photos = await _db.Photos
                .OrderByDescending(p => p.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new { p.Id, p.ImageUrl, p.ThumbnailUrl, p.AltText, p.UploadedAt })
                .ToListAsync();
            return Ok(photos);
        }
    }
}