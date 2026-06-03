using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using NewsPortalPro.Services;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/ads")]
    [Produces("application/json")]
    public class AdsApiController : ControllerBase
    {
        private readonly IAdsService _ads;

        public AdsApiController(IAdsService ads) => _ads = ads;

        [HttpGet("{position}")]
        public async Task<IActionResult> GetByPosition(string position, [FromQuery] int? categoryId = null)
        {
            if (!Enum.TryParse<AdPosition>(position, true, out var pos))
                return BadRequest(new { error = "অবৈধ পজিশন" });

            var result = await _ads.GetByPositionAsync(pos, categoryId);
            return Ok(result);
        }

        [HttpPost("{id:int}/impression")]
        public async Task<IActionResult> TrackImpression(int id)
        {
            await _ads.TrackImpressionAsync(id);
            return Ok(new { success = true });
        }

        [HttpPost("{id:int}/click")]
        public async Task<IActionResult> TrackClick(int id)
        {
            await _ads.TrackClickAsync(id);
            return Ok(new { success = true });
        }
    }
}