using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.ViewComponents
{
    public class AdSlotViewComponent : ViewComponent
    {
        private readonly IAdsService _ads;

        public AdSlotViewComponent(IAdsService ads) => _ads = ads;

        public async Task<IViewComponentResult> InvokeAsync(AdPosition position, int? categoryId = null)
        {
            var ads = await _ads.GetByPositionAsync(position, categoryId);
            return View(ads.Take(1).ToList());
        }
    }
}