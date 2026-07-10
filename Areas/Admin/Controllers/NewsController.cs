using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using System.Security.Claims;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor,Reporter")]
    public class NewsController : Controller
    {
        private readonly INewsService _news;
        private readonly ICategoryService _categories;
        private readonly IFileUploadService _upload;
        private readonly Data.ApplicationDbContext _db;
        private readonly ILogger<NewsController> _logger;

        public NewsController(
            INewsService news,
            ICategoryService categories,
            IFileUploadService upload,
            Data.ApplicationDbContext db,
            ILogger<NewsController> logger)
        {
            _news = news;
            _categories = categories;
            _upload = upload;
            _db = db;
            _logger = logger;
        }

        // ── Index ──────────────────────────────────────────────────
        public async Task<IActionResult> Index(
            [FromQuery] AdminNewsFilterDto filter)
        {
            var result = await _news.GetAllForAdminAsync(filter);
            ViewBag.Filter = filter;
            ViewBag.Categories = await _categories.GetAllActiveAsync();
            return View(result);
        }

        // ── Create GET ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoryList();
            return View(new CreateNewsDto());
        }

        // ── Create POST ────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(10_485_760)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
        public async Task<IActionResult> Create(
            CreateNewsDto dto,
            IFormFile? featuredImage,
            [FromForm(Name = "Status")] string? statusOverride)
        {
            // Submit button value overrides dropdown
            if (!string.IsNullOrEmpty(statusOverride) &&
                int.TryParse(statusOverride, out var statusInt))
            {
                dto.Status = (Models.NewsStatus)statusInt;
            }

            if (featuredImage != null)
            {
                try
                {
                    var upload = await _upload.UploadImageAsync(
                        featuredImage, "news");
                    dto.FeaturedImageUrl = upload.Url;
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError(
                        "featuredImage", ex.Message);
                    await PopulateCategoryList();
                    return View(dto);
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateCategoryList();
                return View(dto);
            }

            var authorId = User.FindFirstValue(
                ClaimTypes.NameIdentifier)!;
            await _news.CreateAsync(dto, authorId);

            TempData["Success"] = dto.Status ==
                Models.NewsStatus.Published
                    ? "সংবাদ সফলভাবে প্রকাশিত হয়েছে"
                    : "সংবাদ ড্রাফট হিসেবে সংরক্ষিত হয়েছে";

            return RedirectToAction(nameof(Index));
        }

        // ── Edit GET ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var news = await _news.GetByIdAsync(id);
            if (news == null) return NotFound();

            if (User.IsInRole("Reporter")
                && !User.IsInRole("Admin")
                && !User.IsInRole("Editor"))
            {
                var currentUserId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);
                if (news.AuthorId != currentUserId)
                {
                    _logger.LogWarning(
                        "IDOR: User {U} tried to edit news {N}",
                        currentUserId, id);
                    return Forbid();
                }
            }

            var categoryId = 0;
            if (!string.IsNullOrEmpty(news.CategorySlug))
            {
                categoryId = await _db.Categories
                    .Where(c => c.Slug == news.CategorySlug
                             && !c.IsDeleted)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();
            }

            await PopulateCategoryList();
            ViewBag.NewsSlug = news.Slug;

            var dto = new UpdateNewsDto
            {
                Id = id,
                Title = news.Title,
                Subtitle = news.Subtitle,
                Content = news.Content,
                Summary = news.Summary,
                CategoryId = categoryId,
                Status = news.Status,
                IsFeatured = news.IsFeatured,
                IsBreaking = news.IsBreaking,
                AllowComments = news.AllowComments,
                FeaturedImageUrl = news.FeaturedImage,
                FeaturedImageAlt = news.FeaturedImageAlt,
                FeaturedImageCaption = news.FeaturedImageCaption,
                VideoUrl = news.VideoUrl,
                MetaTitle = news.MetaTitle,
                MetaDescription = news.MetaDescription,
                MetaKeywords = news.MetaKeywords,
                Tags = news.Tags ?? []
            };

            return View(dto);
        }

        // ── Edit POST ──────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(10_485_760)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
        public async Task<IActionResult> Edit(
            int id,
            UpdateNewsDto dto,
            IFormFile? featuredImage,
            [FromForm(Name = "Status")] string? statusOverride)
        {
            // Submit button value overrides dropdown
            if (!string.IsNullOrEmpty(statusOverride) &&
                int.TryParse(statusOverride, out var statusInt))
            {
                dto.Status = (Models.NewsStatus)statusInt;
            }

            if (User.IsInRole("Reporter")
                && !User.IsInRole("Admin")
                && !User.IsInRole("Editor"))
            {
                var newsOwner = await _news.GetByIdAsync(id);
                if (newsOwner == null) return NotFound();
                var currentUserId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);
                if (newsOwner.AuthorId != currentUserId)
                {
                    _logger.LogWarning(
                        "IDOR POST: User {U} tried to edit {N}",
                        currentUserId, id);
                    return Forbid();
                }
            }

            if (featuredImage != null)
            {
                try
                {
                    var upload = await _upload.UploadImageAsync(
                        featuredImage, "news");
                    dto.FeaturedImageUrl = upload.Url;
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError(
                        "featuredImage", ex.Message);
                    await PopulateCategoryList();
                    return View(dto);
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateCategoryList();
                return View(dto);
            }

            var editorId = User.FindFirstValue(
                ClaimTypes.NameIdentifier)!;
            await _news.UpdateAsync(id, dto, editorId);

            TempData["Success"] = dto.Status ==
                Models.NewsStatus.Published
                    ? "সংবাদ সফলভাবে প্রকাশিত হয়েছে"
                    : "সংবাদ আপডেট হয়েছে";

            return RedirectToAction(nameof(Index));
        }

        // ── Delete ─────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin,Editor,Reporter")]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Reporter")
                && !User.IsInRole("Admin")
                && !User.IsInRole("Editor"))
            {
                var newsOwner = await _news.GetByIdAsync(id);
                if (newsOwner == null) return NotFound();
                var currentUserId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);
                if (newsOwner.AuthorId != currentUserId)
                    return Forbid();
            }

            var deleted = await _news.DeleteAsync(id);
            return Ok(new { success = deleted });
        }

        // ── Publish ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            await _news.PublishAsync(id);
            return Ok(new { success = true });
        }

        // ── Toggle Breaking ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBreaking(
            int id, bool value)
        {
            await _news.SetBreakingAsync(id, value);
            return Ok(new { success = true });
        }

        // ── Toggle Featured ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeatured(
            int id, bool value)
        {
            await _news.SetFeaturedAsync(id, value);
            return Ok(new { success = true });
        }

        // ── Image Upload ───────────────────────────────────────────
        [HttpPost]
        [RequestSizeLimit(10_485_760)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                var result = await _upload.UploadImageAsync(file);
                return Ok(new
                {
                    url = result.Url,
                    publicId = result.PublicId
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── Populate Categories ────────────────────────────────────
        private async Task PopulateCategoryList()
        {
            var cats = await _db.Categories
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            ViewBag.Categories = new SelectList(cats, "Id", "Name");
        }
    }
}