using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Controllers
{
    public class VideosController : Controller
    {
        private readonly IVideoService _videos;

        public VideosController(IVideoService videos) => _videos = videos;

        [HttpGet("/videos")]
        public async Task<IActionResult> Index(int page = 1)
        {
            ViewData["Title"] = "ভিডিও";
            var result = await _videos.GetPagedAsync(page, 12);
            return View(result);
        }
    }
}