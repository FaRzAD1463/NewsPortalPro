using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Middleware
{
    public class AnalyticsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AnalyticsMiddleware> _logger;

        public AnalyticsMiddleware(
            RequestDelegate next,
            ILogger<AnalyticsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IAnalyticsService analytics)
        {
            await _next(context);

            var path = context.Request.Path.Value ?? "";

            if (context.Response.StatusCode == 200 &&
                !path.StartsWith("/api") &&
                !path.StartsWith("/Admin") &&
                !path.StartsWith("/hangfire") &&
                !path.StartsWith("/hubs") &&
                !path.StartsWith("/health") &&
                !path.Contains('.'))
            {
                var userId = context.User?
                    .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?
                    .Value;
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "";
                var ua = context.Request.Headers.UserAgent.ToString();
                var referrer = context.Request.Headers.Referer.ToString();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await analytics.RecordVisitAsync(
                            path, userId, ip, ua, referrer);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Analytics recording failed");
                    }
                });
            }
        }
    }
}