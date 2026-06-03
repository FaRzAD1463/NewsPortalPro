using Microsoft.Extensions.Caching.Memory;

namespace NewsPortalPro.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        private const int MaxRequests = 100;
        private const int WindowSeconds = 60;

        public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only rate limit API endpoints
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"ratelimit:{ip}:{DateTime.UtcNow:yyyyMMddHHmm}";

            var count = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(WindowSeconds);
                return 0;
            });

            count++;
            _cache.Set(key, count, TimeSpan.FromSeconds(WindowSeconds));

            context.Response.Headers["X-RateLimit-Limit"] = MaxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, MaxRequests - count).ToString();

            if (count > MaxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for IP: {IP}", ip);
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"error\":\"Too many requests. Please try again later.\"}");
                return;
            }

            await _next(context);
        }
    }
}