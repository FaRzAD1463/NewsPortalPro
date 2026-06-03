using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;

        public MaintenanceModeMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, ISettingsService settings)
        {
            var maintenance = await settings.GetAsync("MaintenanceMode");
            if (maintenance == "true")
            {
                var path = context.Request.Path.Value ?? "";
                var isAdmin = context.User?.IsInRole("Admin") == true;

                if (!isAdmin && !path.StartsWith("/Account") && !path.StartsWith("/api/auth"))
                {
                    context.Response.StatusCode = 503;
                    await context.Response.WriteAsync(
                        "<html><body style='text-align:center;font-family:sans-serif;padding:50px'>" +
                        "<h1>সাইটটি রক্ষণাবেক্ষণে রয়েছে</h1>" +
                        "<p>আমরা শীঘ্রই ফিরে আসব। অপেক্ষার জন্য ধন্যবাদ।</p>" +
                        "</body></html>");
                    return;
                }
            }
            await _next(context);
        }
    }
}