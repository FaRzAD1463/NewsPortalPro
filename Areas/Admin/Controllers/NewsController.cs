using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;
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

        public NewsController(INewsService news, ICategoryService categories, IFileUploadService upload)
        {
            _news = news;
            _categories = categories;
            _upload = upload;
        }

        public async Task<IActionResult> Index([FromQuery] AdminNewsFilterDto filter)
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
        public async Task<IActionResult> Create(CreateNewsDto dto, IFormFile? featuredImage)
        {
            if (featuredImage != null)
            {
                var upload = await _upload.UploadImageAsync(featuredImage, "news");
                dto.FeaturedImageUrl = upload.Url;
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

            await PopulateCategoryList();
            var dto = new UpdateNewsDto
            {
                Id = id,
                Title = news.Title,
                Subtitle = news.Subtitle,
                Content = news.Content,
                Summary = news.Summary,
                CategoryId = int.TryParse(news.CategorySlug, out var cid) ? cid : 0,
                Status = Models.NewsStatus.Draft,
                IsFeatured = news.IsFeatured,
                IsBreaking = news.IsBreaking,
                FeaturedImageUrl = news.FeaturedImage,
                MetaTitle = news.MetaTitle,
                MetaDescription = news.MetaDescription,
                Tags = news.Tags
            };

            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateNewsDto dto, IFormFile? featuredImage)
        {
            if (featuredImage != null)
            {
                var upload = await _upload.UploadImageAsync(featuredImage, "news");
                dto.FeaturedImageUrl = upload.Url;
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

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _news.DeleteAsync(id);
            TempData["Success"] = "সংবাদ মুছে ফেলা হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Publish(int id)
        {
            await _news.PublishAsync(id);
            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleBreaking(int id, bool value)
        {
            await _news.SetBreakingAsync(id, value);
            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFeatured(int id, bool value)
        {
            await _news.SetFeaturedAsync(id, value);
            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var result = await _upload.UploadImageAsync(file);
            return Ok(new { url = result.Url, publicId = result.PublicId });
        }

        private async Task PopulateCategoryList()
        {
            var cats = await _categories.GetAllActiveAsync();
            ViewBag.Categories = new SelectList(cats, "Id", "Name");
        }
    }
}