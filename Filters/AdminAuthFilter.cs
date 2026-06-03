using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NewsPortalPro.Filters
{
    public class AdminAuthFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated == true)
            {
                context.Result = new RedirectToActionResult(
                    "Login", "Account",
                    new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            if (!user.IsInRole("Admin") && !user.IsInRole("Editor") && !user.IsInRole("Reporter"))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}