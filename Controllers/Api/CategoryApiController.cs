using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/categories")]
    [Produces("application/json")]
    public class CategoryApiController : ControllerBase
    {
        private readonly ICategoryService _categories;

        public CategoryApiController(ICategoryService categories) => _categories = categories;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _categories.GetAllActiveAsync();
            return Ok(result);
        }

        [HttpGet("menu")]
        public async Task<IActionResult> GetMenu()
        {
            var result = await _categories.GetMenuCategoriesAsync();
            return Ok(result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var result = await _categories.GetBySlugAsync(slug);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("with-count")]
        public async Task<IActionResult> GetWithCount()
        {
            var result = await _categories.GetWithNewsCountAsync();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var id = await _categories.CreateAsync(dto);

            // FIX: previously used CreatedAtAction(nameof(GetBySlug), new { slug = dto.Name }, ...)
            // — dto.Name is not guaranteed to equal the actual generated slug
            // (spaces/casing/transliteration), so the response's Location
            // header could point to a URL that 404s. Returning Ok with the
            // id instead, since ICategoryService doesn't expose a way to
            // fetch the entity's real slug by id here. If you want a proper
            // 201 + correct Location header, add a GetByIdAsync method to
            // ICategoryService and swap this back to CreatedAtAction using
            // the returned entity's actual slug.

            return Ok(ApiResponse<object>.Ok(new { id }));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            var result = await _categories.UpdateAsync(id, dto);
            if (!result) return NotFound();
            return Ok(ApiResponse<string>.Ok("Updated"));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _categories.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(ApiResponse<string>.Ok("Deleted"));
        }
    }
}