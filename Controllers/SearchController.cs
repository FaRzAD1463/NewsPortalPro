using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Controllers
{
    public class SearchController : Controller
    {
        private readonly ISearchService _search;

        public SearchController(ISearchService search) =>
            _search = search;

        [HttpGet]
        [Route("Search")]
        public async Task<IActionResult> Index(
            string q, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(q))
                return View(null);

            var result = await _search.SearchAsync(q, page);
            ViewBag.Query = q;
            return View(result);
        }
    }
}