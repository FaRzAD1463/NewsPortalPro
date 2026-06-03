using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Middleware
{
    public class AnalyticsMiddleware
    {
        private readonly RequestDelegate _next;

        public AnalyticsMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, IAnalyticsService analytics)
        {
            await _next(context);

            // Only track successful HTML page views (not API, static, or admin)
            var path = context.Request.Path.Value ?? "";
            if (context.Response.StatusCode == 200 &&
                !path.StartsWith("/api") &&
                !path.StartsWith("/admin") &&
                !path.StartsWith("/hangfire") &&
                !path.StartsWith("/hubs") &&
                !path.Contains('.'))
            {
                var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var ip = context.Connection.RemoteIpAddress?.ToString();
                var ua = context.Request.Headers.UserAgent.ToString();
                var referrer = context.Request.Headers.Referer.ToString();

                _ = Task.Run(async () =>
                {
                    try { await analytics.RecordVisitAsync(path, userId, ip ?? "", ua, referrer); }
                    catch { /* swallow - analytics is non-critical */ }
                });
            }
        }
    }
}