using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Filters
{
    public class MenuCategoriesFilter(
        ICategoryService categories,
        ISettingsService settings) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            if (context.Controller is Controller controller)
            {
                var area = context.RouteData.Values["area"]
                    ?.ToString();

                if (string.IsNullOrEmpty(area))
                {
                    try
                    {
                        var menuCats = await categories.GetMenuCategoriesAsync();

                        if (menuCats.Count == 0)
                        {
                            menuCats = await categories.GetAllActiveAsync();
                        }

                        controller.ViewBag.MenuCategories = menuCats;
                        controller.ViewBag.FooterCategories = menuCats;

                        controller.ViewBag.SiteName =
                            await settings.GetAsync("SiteName")
                            ?? "নিউজপোর্টাল প্রো";

                        controller.ViewBag.SiteTagline =
                            await settings.GetAsync("SiteTagline")
                            ?? "বাংলাদেশের নির্ভরযোগ্য সংবাদ মাধ্যম";

                        controller.ViewBag.SiteDescription =
                            await settings.GetAsync("SiteDescription")
                            ?? "বাংলাদেশের নির্ভরযোগ্য সংবাদ মাধ্যম";

                        controller.ViewBag.LogoUrl =
                            await settings.GetAsync("LogoUrl")
                            ?? "/images/logo.png";

                        controller.ViewBag.SiteEmail =
                            await settings.GetAsync("SiteEmail")
                            ?? "info@newsportalpro.com";

                        controller.ViewBag.SitePhone =
                            await settings.GetAsync("SitePhone")
                            ?? "+880-1700-000000";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MenuCategoriesFilter] THREW: {ex}");
                    }
                }
            }

            await next();
        }
    }
}