using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Controllers.Api
{
    [Route("")]
    public class SitemapController : Controller
    {
        private readonly ISEOService _seo;

        public SitemapController(ISEOService seo) => _seo = seo;

        [HttpGet("sitemap.xml")]
        public async Task<IActionResult> Sitemap()
        {
            var xml = await _seo.GenerateSitemapAsync();
            return Content(xml, "application/xml");
        }

        [HttpGet("rss.xml")]
        public async Task<IActionResult> Rss()
        {
            var xml = await _seo.GenerateNewsRssFeedAsync();
            return Content(xml, "application/rss+xml");
        }
    }
}