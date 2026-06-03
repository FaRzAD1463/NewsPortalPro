using Microsoft.AspNetCore.Mvc.Filters;
using NewsPortalPro.Data;
using NewsPortalPro.Models;

namespace NewsPortalPro.Filters
{
    public class AuditLogFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _db;

        public AuditLogFilter(ApplicationDbContext db) => _db = db;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var result = await next();

            // Only log state-changing actions
            var method = context.HttpContext.Request.Method;
            if (method == "POST" || method == "PUT" || method == "DELETE")
            {
                var userId = context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var action = $"{context.RouteData.Values["controller"]}.{context.RouteData.Values["action"]}";
                var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                var ua = context.HttpContext.Request.Headers.UserAgent.ToString();

                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    IpAddress = ip,
                    UserAgent = ua,
                    IsSuccess = result.Exception == null,
                    CreatedAt = DateTime.UtcNow
                });

                try { await _db.SaveChangesAsync(); } catch { /* non-critical */ }
            }
        }
    }

    public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
    {
        public bool Authorize(Hangfire.Dashboard.DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User?.IsInRole("Admin") == true;
        }
    }
}