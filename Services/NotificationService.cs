using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Hubs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<NewsHub> _hub;

        public NotificationService(ApplicationDbContext db, IHubContext<NewsHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int count = 20)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Link = n.Link,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId) =>
            await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task MarkAsReadAsync(int id, string userId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, DateTime.UtcNow));
        }

        public async Task SendToUserAsync(string userId, string title, string message,
            NotificationType type, string? link = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            await _hub.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Link,
                Type = notification.Type.ToString(),
                notification.CreatedAt
            });
        }

        public async Task BroadcastAsync(string title, string message, NotificationType type, string? link = null)
        {
            var users = await _db.Users.Where(u => u.IsActive).Select(u => u.Id).ToListAsync();

            var notifications = users.Select(userId => new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link
            }).ToList();

            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("ReceiveBroadcast", new { title, message, link });
        }

        public async Task SendBreakingNewsAlertAsync(int newsId, string title, string slug)
        {
            await _hub.Clients.All.SendAsync("BreakingNews", new
            {
                newsId,
                title,
                link = $"/news/{slug}",
                timestamp = DateTime.UtcNow
            });
        }
    }
}