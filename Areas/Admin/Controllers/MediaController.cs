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

        public MediaController(ApplicationDbContext db, IFileUploadService upload)
        {
            _db = db;
            _upload = upload;
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
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var photo = await _db.Photos.FindAsync(id);
            if (photo == null) return NotFound();
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