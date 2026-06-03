using NewsPortalPro.DTOs;

namespace NewsPortalPro.ViewModels
{
    public class ProfileViewModel
    {
        public UserDto User { get; set; } = null!;
        public List<NewsListDto> Bookmarks { get; set; } = [];
        public int TotalComments { get; set; }
    }
}