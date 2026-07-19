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
                        // Try menu categories first
                        var menuCats = await _categories
                            .GetMenuCategoriesAsync();

                        // ── Fallback: if ShowInMenu=false for all,
                        //    use all active categories instead ──────
                        if (!menuCats.Any())
                        {
                            menuCats = await _categories
                                .GetAllActiveAsync();
                        }

                        controller.ViewBag.MenuCategories = menuCats;
                        controller.ViewBag.FooterCategories = menuCats;

                        controller.ViewBag.SiteName =
                            await _settings.GetAsync("SiteName")
                            ?? "নিউজপোর্টাল প্রো";

                        controller.ViewBag.SiteTagline =
                            await _settings.GetAsync("SiteTagline")
                            ?? "বাংলাদেশের নির্ভরযোগ্য সংবাদ মাধ্যম";

                        controller.ViewBag.LogoUrl =
                            await _settings.GetAsync("LogoUrl")
                            ?? "/images/logo.png";
                    }
                    catch { }
                }
            }

            await next();
        }
    }
}