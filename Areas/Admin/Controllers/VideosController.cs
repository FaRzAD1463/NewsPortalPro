using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor,Reporter")]
    public class VideosController : Controller
    {
        private readonly IVideoService _videos;

        public VideosController(IVideoService videos)
        {
            _videos = videos;
        }

        // ── Index ──────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _videos.GetAllForAdminAsync(page, 20);
            return View(result);
        }

        // ── Create GET ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateVideoDto());
        }

        // ── Create POST ────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVideoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                ModelState.AddModelError(nameof(dto.Title), "শিরোনাম আবশ্যক");

            if (string.IsNullOrWhiteSpace(dto.VideoUrl))
                ModelState.AddModelError(nameof(dto.VideoUrl), "ভিডিও লিংক আবশ্যক");
            else if (!Uri.TryCreate(dto.VideoUrl, UriKind.Absolute, out _))
                ModelState.AddModelError(nameof(dto.VideoUrl), "সঠিক লিংক দিন");

            if (!ModelState.IsValid)
                return View(dto);

            await _videos.CreateAsync(dto);
            TempData["Success"] = "ভিডিও সফলভাবে যোগ হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // ── Edit GET ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var video = await _videos.GetByIdAsync(id);
            if (video == null) return NotFound();

            var dto = new UpdateVideoDto
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                VideoUrl = video.VideoUrl,
                ThumbnailUrl = video.ThumbnailUrl,
                Duration = video.Duration,
                NewsId = video.NewsId,
                IsActive = video.IsActive
            };

            return View(dto);
        }

        // ── Edit POST ──────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateVideoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                ModelState.AddModelError(nameof(dto.Title), "শিরোনাম আবশ্যক");

            if (string.IsNullOrWhiteSpace(dto.VideoUrl))
                ModelState.AddModelError(nameof(dto.VideoUrl), "ভিডিও লিংক আবশ্যক");
            else if (!Uri.TryCreate(dto.VideoUrl, UriKind.Absolute, out _))
                ModelState.AddModelError(nameof(dto.VideoUrl), "সঠিক লিংক দিন");

            if (!ModelState.IsValid)
                return View(dto);

            var updated = await _videos.UpdateAsync(id, dto);
            if (!updated) return NotFound();

            TempData["Success"] = "ভিডিও আপডেট হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // ── Delete ─────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _videos.DeleteAsync(id);
            return Ok(new { success = deleted });
        }
    }
}