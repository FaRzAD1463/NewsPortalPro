namespace NewsPortalPro.DTOs
{
    public class PageViewDto
    {
        public string Page { get; set; } = string.Empty;
        public int Views { get; set; }
        public DateTime Date { get; set; }
    }

    public class DeviceStatsDto
    {
        public string Device { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class RealTimeStatsDto
    {
        public int ActiveUsers { get; set; }
        public List<string> ActivePages { get; set; } = [];
        public DateTime AsOf { get; set; } = DateTime.UtcNow;
    }
}