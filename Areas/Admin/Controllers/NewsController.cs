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

        public async Task<IActionResult> Index(
            [FromQuery] AdminNewsFilterDto filter)
        {
            var result = await _news.GetAllForAdminAsync(filter);
            ViewBag.Filter = filter;
            ViewBag.Categories = await _categories.GetAllActiveAsync();
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoryList();
            return View(new CreateNewsDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(10_485_760)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
        public async Task<IActionResult> Create(
            CreateNewsDto dto, IFormFile? featuredImage)
        {
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
                    ModelState.AddModelError("featuredImage", ex.Message);
                    await PopulateCategoryList();
                    return View(dto);
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateCategoryList();
                return View(dto);
            }

            var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var id = await _news.CreateAsync(dto, authorId);
            TempData["Success"] = "সংবাদ সফলভাবে তৈরি হয়েছে";
            return RedirectToAction(nameof(Index));
        }

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
                        "IDOR attempt: User {UserId} tried to edit " +
                        "news {NewsId} owned by {OwnerId}",
                        currentUserId, id, news.AuthorId);
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

            var dto = new UpdateNewsDto
            {
                Id = id,
                Title = news.Title,
                Subtitle = news.Subtitle,
                Content = news.Content,
                Summary = news.Summary,
                CategoryId = categoryId,
                Status = Models.NewsStatus.Draft,
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
                Tags = news.Tags
            };

            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(10_485_760)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
        public async Task<IActionResult> Edit(
            int id, UpdateNewsDto dto, IFormFile? featuredImage)
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
                {
                    _logger.LogWarning(
                        "IDOR POST attempt: User {UserId} tried to " +
                        "edit news {NewsId}",
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
                    ModelState.AddModelError("featuredImage", ex.Message);
                    await PopulateCategoryList();
                    return View(dto);
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateCategoryList();
                return View(dto);
            }

            var editorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _news.UpdateAsync(id, dto, editorId);
            TempData["Success"] = "সংবাদ সফলভাবে আপডেট হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // ── Delete — ValidateAntiForgeryToken is the critical fix ──
        // Without it the form post is rejected silently and nothing happens
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                {
                    _logger.LogWarning(
                        "IDOR delete attempt: User {UserId} tried " +
                        "to delete news {NewsId}",
                        currentUserId, id);
                    return Forbid();
                }
            }

            var deleted = await _news.DeleteAsync(id);
            if (!deleted)
            {
                TempData["Error"] = "সংবাদ মুছে ফেলা যায়নি";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "সংবাদ মুছে ফেলা হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // ── Publish ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            await _news.PublishAsync(id);
            return Ok(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBreaking(int id, bool value)
        {
            await _news.SetBreakingAsync(id, value);
            return Ok(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeatured(int id, bool value)
        {
            await _news.SetFeaturedAsync(id, value);
            return Ok(new { success = true });
        }

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