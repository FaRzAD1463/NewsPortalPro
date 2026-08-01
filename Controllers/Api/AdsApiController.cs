using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/ads")]
    [Produces("application/json")]
    public class AdsApiController : ControllerBase
    {
        private readonly IAdsService _ads;

        public AdsApiController(IAdsService ads) => _ads = ads;

        // ── Get ads by position ────────────────────────────────
        // Accepts both string ("Sidebar") and int ("2")

        [HttpGet("{position}")]
        public async Task<IActionResult> GetByPosition(
            string position,
            [FromQuery] int? categoryId = null)
        {
            AdPosition pos;

            if (Enum.TryParse<AdPosition>(
                    position, ignoreCase: true, out var byName))
            {
                pos = byName;
            }
            else if (int.TryParse(position, out var byInt) &&
                     Enum.IsDefined(typeof(AdPosition), byInt))
            {
                pos = (AdPosition)byInt;
            }
            else
            {
                return BadRequest(new
                {
                    error = "অবৈধ পজিশন। " +
                            "সঠিক মান: Header, Sidebar, Footer, " +
                            "InArticle, Popup, BelowTitle"
                });
            }

            var result = await _ads.GetByPositionAsync(pos, categoryId);
            return Ok(result);
        }

        // ── Get all active ads ─────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? categoryId = null)
        {
            var result = await _ads.GetAllActiveAsync();
            return Ok(result);
        }

        // ── Track impression (single — kept for backward
        //    compatibility / non-JS clients) ────────────────────

        [HttpPost("{id:int}/impression")]
        public async Task<IActionResult> TrackImpression(int id)
        {
            await _ads.TrackImpressionAsync(id);
            return Ok(new { success = true });
        }

        // ── Track impressions (batch) ───────────────────────────
        // Called by site.js's batched impression queue — accepts a
        // small array of ad IDs collected over a short client-side
        // window instead of one request per ad slot per page load.
        // Also the target of navigator.sendBeacon, which always POSTs
        // and cannot set a Content-Type header reliably across
        // browsers, so we accept the body as plain text and parse it
        // ourselves rather than relying on [FromBody] JSON binding.
        public class TrackImpressionsRequest
        {
            public List<string> AdIds { get; set; } = [];
        }

        [HttpPost("impressions")]
        public async Task<IActionResult> TrackImpressions(
      [FromBody] TrackImpressionsRequest request)
        {
            if (request?.AdIds == null || request.AdIds.Count == 0)
                return Ok(new { success = true, tracked = 0 });

            var ids = request.AdIds
                .Take(50)
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .Distinct()
                .ToList();

            await _ads.TrackImpressionsAsync(ids);

            return Ok(new { success = true, tracked = ids.Count });
        }

        // ── Track click ────────────────────────────────────────

        [HttpPost("{id:int}/click")]
        public async Task<IActionResult> TrackClick(int id)
        {
            await _ads.TrackClickAsync(id);
            return Ok(new { success = true });
        }
    }
}