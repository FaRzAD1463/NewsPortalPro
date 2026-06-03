using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;

        public MaintenanceModeMiddleware(RequestDelegate next) =>
            _next = next;

        public async Task InvokeAsync(
            HttpContext context,
            ISettingsService settings)
        {
            var maintenance = await settings.GetAsync("MaintenanceMode");

            if (maintenance == "true")
            {
                var path = context.Request.Path.Value ?? "";
                var isAdmin = context.User?.IsInRole("Admin") == true;

                if (!isAdmin &&
                    !path.StartsWith("/Account") &&
                    !path.StartsWith("/api/auth") &&
                    !path.StartsWith("/health"))
                {
                    context.Response.StatusCode = 503;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(@"
                        <html>
                        <head><meta charset='utf-8'></head>
                        <body style='text-align:center;font-family:sans-serif;padding:80px'>
                            <h1>সাইটটি রক্ষণাবেক্ষণে রয়েছে</h1>
                            <p>আমরা শীঘ্রই ফিরে আসব।</p>
                        </body>
                        </html>");
                    return;
                }
            }

            await _next(context);
        }
    }
}