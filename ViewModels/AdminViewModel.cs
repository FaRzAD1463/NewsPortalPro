using NewsPortalPro.DTOs;

namespace NewsPortalPro.ViewModels
{
    public class AdminDashboardViewModel
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<DailyViewsDto> DailyViews { get; set; } = [];
        public List<TopNewsDto> TopNews { get; set; } = [];
        public List<CategoryStatsDto> CategoryStats { get; set; } = [];
        public int PendingComments { get; set; }
    }
}