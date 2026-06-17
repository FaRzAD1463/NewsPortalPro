using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;

        public MaintenanceModeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IServiceScopeFactory scopeFactory)
        {
            // Admin bypass — always let admin through
            if (context.Request.Path.StartsWithSegments("/Admin") ||
                context.Request.Path.StartsWithSegments("/Account") ||
                context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var settings = scope.ServiceProvider
                    .GetRequiredService<ISettingsService>();

                var maintenance = await settings.GetAsync("MaintenanceMode");

                if (maintenance == "true" &&
                    !context.User.IsInRole("Admin"))
                {
                    context.Response.StatusCode = 503;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(
                        "<html><body style='text-align:center;margin-top:100px'>" +
                        "<h1>রক্ষণাবেক্ষণ চলছে</h1>" +
                        "<p>আমরা শীঘ্রই ফিরে আসব।</p>" +
                        "</body></html>");
                    return;
                }
            }
            catch
            {
                // If settings can't be read, don't block the request
            }

            await _next(context);
        }
    }
}