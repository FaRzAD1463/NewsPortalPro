using NewsPortalPro.DTOs;

namespace NewsPortalPro.ViewModels
{
    public class NewsIndexViewModel
    {
        public PagedResult<NewsListDto> NewsResult { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = [];
        public string? CurrentSort { get; set; }
        public string? CurrentCategory { get; set; }
    }

    public class NewsDetailsViewModel
    {
        public NewsDetailDto News { get; set; } = null!;
        public List<AdvertisementDto> SidebarAds { get; set; } = [];
        public bool IsBookmarked { get; set; }
        public string? UserReaction { get; set; }
    }
}