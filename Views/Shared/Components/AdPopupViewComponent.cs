using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.ViewComponents
{
    public class AdPopupViewComponent : ViewComponent
    {
        private readonly IAdsService _ads;

        public AdPopupViewComponent(IAdsService ads) => _ads = ads;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var ads = await _ads.GetByPositionAsync(AdPosition.Popup);
            return View(ads.Take(1).ToList());
        }
    }
}