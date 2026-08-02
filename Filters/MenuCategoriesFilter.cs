using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Filters
{
    public class MenuCategoriesFilter : IAsyncActionFilter
    {
        private readonly ICategoryService _categories;
        private readonly ISettingsService _settings;

        public MenuCategoriesFilter(
            ICategoryService categories,
            ISettingsService settings)
        {
            _categories = categories;
            _settings = settings;
        }

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
                        var menuCats = await _categories.GetMenuCategoriesAsync();

                        if (!menuCats.Any())
                        {
                            menuCats = await _categories.GetAllActiveAsync();
                        }

                        controller.ViewBag.MenuCategories = menuCats;
                        controller.ViewBag.FooterCategories = menuCats;

                        controller.ViewBag.SiteName =
                            await _settings.GetAsync("SiteName")
                            ?? "নিউজপোর্টাল প্রো";

                        controller.ViewBag.SiteTagline =
                            await _settings.GetAsync("SiteTagline")
                            ?? "বাংলাদেশের নির্ভরযোগ্য সংবাদ মাধ্যম";

                        controller.ViewBag.SiteDescription =
                            await _settings.GetAsync("SiteDescription")
                            ?? "বাংলাদেশের নির্ভরযোগ্য সংবাদ মাধ্যম";

                        controller.ViewBag.LogoUrl =
                            await _settings.GetAsync("LogoUrl")
                            ?? "/images/logo.png";

                        controller.ViewBag.SiteEmail =
                            await _settings.GetAsync("SiteEmail")
                            ?? "info@newsportalpro.com";

                        controller.ViewBag.SitePhone =
                            await _settings.GetAsync("SitePhone")
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