using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using NewsPortalPro.Services;
using System.Security.Claims;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationApiController : ControllerBase
    {
        private readonly INotificationService _notifications;

        public NotificationApiController(INotificationService notifications)
            => _notifications = notifications;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int count = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _notifications.GetUserNotificationsAsync(userId, count);
            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var count = await _notifications.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _notifications.MarkAsReadAsync(id, userId);
            return Ok(new { success = true });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _notifications.MarkAllAsReadAsync(userId);
            return Ok(new { success = true });
        }

        [HttpPost("broadcast")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastDto dto)
        {
            await _notifications.BroadcastAsync(dto.Title, dto.Message, NotificationType.System, dto.Link);
            return Ok(new { success = true });
        }
    }

    public class BroadcastDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Link { get; set; }
    }
}