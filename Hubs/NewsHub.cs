using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NewsPortalPro.Hubs
{
    public class NewsHub : Hub
    {
        private readonly ILogger<NewsHub> _logger;

        public NewsHub(ILogger<NewsHub> logger) => _logger = logger;

        public override async Task OnConnectedAsync()
        {
            _logger.LogDebug("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogDebug("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinCategory(string categorySlug)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"category:{categorySlug}");
        }

        public async Task LeaveCategory(string categorySlug)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"category:{categorySlug}");
        }

        [Authorize]
        public async Task SendAdminBroadcast(string message)
        {
            if (Context.User?.IsInRole("Admin") == true)
                await Clients.All.SendAsync("ReceiveBroadcast", new { message, timestamp = DateTime.UtcNow });
        }
    }
}