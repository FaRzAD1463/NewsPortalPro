using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Data;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/locations")]
    public class LocationsApiController : ControllerBase
    {
        [HttpGet("divisions")]
        public IActionResult GetDivisions() => Ok(BangladeshLocations.GetDivisions());

        [HttpGet("districts")]
        public IActionResult GetDistricts([FromQuery] string division)
        {
            if (string.IsNullOrWhiteSpace(division))
                return Ok(new List<string>());

            return Ok(BangladeshLocations.GetDistricts(division));
        }
    }
}