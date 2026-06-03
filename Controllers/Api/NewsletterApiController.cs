using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.Models;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/newsletter")]
    [Produces("application/json")]
    public class NewsletterApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public NewsletterApiController(ApplicationDbContext db) => _db = db;

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { success = false, message = "ইমেইল প্রয়োজন" });

            var existing = await _db.Subscribers.FirstOrDefaultAsync(s => s.Email == dto.Email);
            if (existing != null)
            {
                if (existing.IsActive)
                    return Ok(new { success = false, message = "ইতিমধ্যে সাবস্ক্রাইব করা হয়েছে" });

                existing.IsActive = true;
                existing.UnsubscribedAt = null;
                await _db.SaveChangesAsync();
                return Ok(new { success = true, message = "পুনরায় সাবস্ক্রাইব করা হয়েছে" });
            }

            var token = Guid.NewGuid().ToString("N");
            _db.Subscribers.Add(new Subscriber
            {
                Email = dto.Email,
                Name = dto.Name,
                IsActive = true,
                IsConfirmed = true,
                ConfirmationToken = token,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _db.SaveChangesAsync();
            return Ok(new { success = true, message = "সাবস্ক্রিপশন সফল হয়েছে" });
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] SubscribeDto dto)
        {
            var subscriber = await _db.Subscribers.FirstOrDefaultAsync(s => s.Email == dto.Email);
            if (subscriber == null) return NotFound(new { success = false });

            subscriber.IsActive = false;
            subscriber.UnsubscribedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "আনসাবস্ক্রাইব হয়েছে" });
        }
    }

    public class SubscribeDto
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }
}