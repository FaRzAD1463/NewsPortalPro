using NewsPortalPro.Interfaces;
using System.Security.Claims;

namespace NewsPortalPro.Middleware
{
    public class AnalyticsMiddleware
    {
        private readonly RequestDelegate _next;

        public AnalyticsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IServiceScopeFactory scopeFactory)
        {
            await _next(context);

            // Skip non-200 responses
            if (context.Response.StatusCode != 200)
                return;

            var path = context.Request.Path.ToString();

            // Skip non-page paths
            var skipPrefixes = new[]
            {
                "/api/", "/Admin/", "/hangfire",
                "/health", "/swagger", "/hubs/",
                "/uploads/", "/css/", "/js/",
                "/lib/", "/images/", "/favicon"
            };

            if (skipPrefixes.Any(prefix =>
                    path.StartsWith(prefix,
                        StringComparison.OrdinalIgnoreCase)))
                return;

            // ── Capture ALL values from HttpContext BEFORE Task.Run ──
            // HttpContext is disposed after the request completes.
            // Accessing it inside Task.Run causes IFeatureCollection
            // disposed exception.
            var capturedPath = path;
            var capturedUserId = context.User
                                        .FindFirstValue(
                                            ClaimTypes.NameIdentifier);
            var capturedIp = context.Connection.RemoteIpAddress
                                        ?.ToString() ?? "";
            var capturedUserAgent = context.Request.Headers.UserAgent
                                        .ToString();
            var capturedReferrer = context.Request.Headers.Referer
                                        .ToString();

            // Fire and forget — analytics must never block the response
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var analytics = scope.ServiceProvider
                        .GetRequiredService<IAnalyticsService>();

                    await analytics.RecordVisitAsync(
                        page: capturedPath,
                        userId: capturedUserId,
                        ip: capturedIp,
                        userAgent: capturedUserAgent,
                        referrer: capturedReferrer);
                }
                catch
                {
                    // Non-critical — swallow all exceptions
                }
            });
        }
    }
}