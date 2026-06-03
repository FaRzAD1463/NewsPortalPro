using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/search")]
    [Produces("application/json")]
    public class SearchApiController : ControllerBase
    {
        private readonly ISearchService _search;

        public SearchApiController(ISearchService search) => _search = search;

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { error = "অনুসন্ধান শব্দ প্রয়োজন" });

            var result = await _search.SearchAsync(q, page, pageSize);
            return Ok(result);
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest([FromQuery] string q, [FromQuery] int count = 8)
        {
            if (string.IsNullOrWhiteSpace(q)) return Ok(new List<string>());
            var result = await _search.GetSuggestionsAsync(q, count);
            return Ok(result);
        }
    }
}