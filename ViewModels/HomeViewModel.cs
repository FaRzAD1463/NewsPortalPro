using NewsPortalPro.DTOs;

namespace NewsPortalPro.ViewModels
{
        public class HomeViewModel
        {
        public List<NewsListDto> BreakingNews { get; set; } = [];
        public List<NewsListDto> FeaturedNews { get; set; } = [];
        public List<NewsListDto> LatestNews { get; set; } = [];
        public List<NewsListDto> TrendingNews { get; set; } = [];
        public List<NewsListDto> MostViewed { get; set; } = [];
        public List<CategoryDto> Categories { get; set; } = [];
        public List<AdvertisementDto> HeaderAds { get; set; } = [];
        public List<AdvertisementDto> SidebarAds { get; set; } = [];
        public string SiteName { get; set; } = string.Empty;
        public Dictionary<string, (CategoryDto Category, List<NewsListDto> News)> CategoryNewsBlocks { get; set; } = [];

        public List<VideoDto> Videos { get; set; } = [];
    }
}