using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.Models;
using System.ComponentModel.DataAnnotations;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/newsletter")]
    [Produces("application/json")]
    public class NewsletterApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public NewsletterApiController(
            ApplicationDbContext db) => _db = db;

        // ── Subscribe ──────────────────────────────────────────
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe(
            [FromBody] SubscribeDto dto)
        {
            // ── Input validation ───────────────────────────────
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new
                {
                    success = false,
                    message = "ইমেইল প্রয়োজন"
                });

            // Validate email format properly
            if (!new EmailAddressAttribute().IsValid(dto.Email))
                return BadRequest(new
                {
                    success = false,
                    message = "সঠিক ইমেইল ঠিকানা দিন"
                });

            // Normalize email
            var email = dto.Email.Trim().ToLowerInvariant();

            var existing = await _db.Subscribers
                .FirstOrDefaultAsync(s => s.Email == email);

            if (existing != null)
            {
                if (existing.IsActive)
                {
                    // Return same message whether active or not
                    // — prevents email enumeration
                    return Ok(new
                    {
                        success = true,
                        message = "সাবস্ক্রিপশন সফল হয়েছে"
                    });
                }

                // Reactivate existing inactive subscriber
                existing.IsActive = true;
                existing.UnsubscribedAt = null;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "সাবস্ক্রিপশন সফল হয়েছে"
                });
            }

            // New subscriber
            _db.Subscribers.Add(new Subscriber
            {
                Email = email,
                Name = dto.Name?.Trim(),
                IsActive = true,
                IsConfirmed = true,
                ConfirmationToken = Guid.NewGuid().ToString("N"),
                IpAddress = HttpContext.Connection
                                        .RemoteIpAddress?.ToString()
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "সাবস্ক্রিপশন সফল হয়েছে"
            });
        }

        // ── Unsubscribe ────────────────────────────────────────
        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe(
            [FromBody] SubscribeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { success = false });

            var email = dto.Email.Trim().ToLowerInvariant();

            var subscriber = await _db.Subscribers
                .FirstOrDefaultAsync(s => s.Email == email);

            // Always return success — prevents email enumeration
            if (subscriber != null && subscriber.IsActive)
            {
                subscriber.IsActive = false;
                subscriber.UnsubscribedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                message = "আনসাবস্ক্রাইব হয়েছে"
            });
        }

        // ── Unsubscribe via token link ─────────────────────────
        // /api/newsletter/unsubscribe-token?token=abc123
        [HttpGet("unsubscribe-token")]
        public async Task<IActionResult> UnsubscribeByToken(
            [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest();

            var subscriber = await _db.Subscribers
                .FirstOrDefaultAsync(
                    s => s.ConfirmationToken == token);

            if (subscriber == null)
                return Redirect("/?unsubscribed=notfound");

            subscriber.IsActive = false;
            subscriber.UnsubscribedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Redirect("/?unsubscribed=true");
        }
    }

    // ── SubscribeDto with proper validation ────────────────────
    public class SubscribeDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Name { get; set; }
    }
}