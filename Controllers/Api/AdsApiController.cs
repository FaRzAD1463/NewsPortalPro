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

            // Try parsing as enum name first
            if (Enum.TryParse<AdPosition>(
                    position, ignoreCase: true, out var byName))
            {
                pos = byName;
            }
            // Try parsing as int
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

        // ── Track impression ───────────────────────────────────
        [HttpPost("{id:int}/impression")]
        public async Task<IActionResult> TrackImpression(int id)
        {
            await _ads.TrackImpressionAsync(id);
            return Ok(new { success = true });
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