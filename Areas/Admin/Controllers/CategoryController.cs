using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categories;

        public CategoryController(ICategoryService categories) => _categories = categories;

        public async Task<IActionResult> Index()
        {
            var cats = await _categories.GetWithNewsCountAsync();
            return View(cats);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateParentList();
            return View(new CreateCategoryDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCategoryDto dto)
        {
            if (!ModelState.IsValid) { await PopulateParentList(); return View(dto); }
            await _categories.CreateAsync(dto);
            TempData["Success"] = "বিভাগ তৈরি হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var cat = await _categories.GetByIdAsync(id);
            if (cat == null) return NotFound();
            await PopulateParentList(id);
            return View(new UpdateCategoryDto
            {
                Id = cat.Id,
                Name = cat.Name,
                Description = cat.Description,
                ColorCode = cat.ColorCode,
                ParentId = cat.ParentId,
                DisplayOrder = cat.DisplayOrder,
                ShowInMenu = cat.ShowInMenu,
                IsActive = cat.IsActive,
                MetaTitle = cat.MetaTitle,
                MetaDescription = cat.MetaDescription
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid) { await PopulateParentList(id); return View(dto); }
            await _categories.UpdateAsync(id, dto);
            TempData["Success"] = "বিভাগ আপডেট হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _categories.DeleteAsync(id);
            TempData["Success"] = "বিভাগ মুছে ফেলা হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateParentList(int? excludeId = null)
        {
            var cats = await _categories.GetAllActiveAsync();
            var filtered = excludeId.HasValue ? cats.Where(c => c.Id != excludeId.Value) : cats;
            ViewBag.Parents = new SelectList(filtered, "Id", "Name");
        }
    }
}