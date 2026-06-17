namespace NewsPortalPro.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;

        // Simple in-memory rate limiter — no scoped services needed
        private static readonly Dictionary<string, (int Count, DateTime Reset)>
            _requests = new();
        private static readonly object _lock = new();

        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip admin and API (handled by AspNetCoreRateLimit)
            if (context.Request.Path.StartsWithSegments("/Admin") ||
                context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var ip = context.Connection.RemoteIpAddress?.ToString()
                  ?? "unknown";

            lock (_lock)
            {
                var now = DateTime.UtcNow;

                if (_requests.TryGetValue(ip, out var entry))
                {
                    if (now > entry.Reset)
                    {
                        // Reset window
                        _requests[ip] = (1, now.AddMinutes(1));
                    }
                    else if (entry.Count >= 300)
                    {
                        // Too many requests
                        context.Response.StatusCode = 429;
                        return;
                    }
                    else
                    {
                        _requests[ip] = (entry.Count + 1, entry.Reset);
                    }
                }
                else
                {
                    _requests[ip] = (1, now.AddMinutes(1));
                }

                // Cleanup old entries periodically
                if (_requests.Count > 10000)
                {
                    var expired = _requests
                        .Where(x => now > x.Value.Reset)
                        .Select(x => x.Key)
                        .ToList();
                    foreach (var key in expired)
                        _requests.Remove(key);
                }
            }

            await _next(context);
        }
    }
}