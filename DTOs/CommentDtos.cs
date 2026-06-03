using NewsPortalPro.Models;

namespace NewsPortalPro.DTOs
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatar { get; set; }
        public int NewsId { get; set; }
        public string NewsTitle { get; set; } = string.Empty;
        public string NewsSlug { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public CommentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CommentDto> Replies { get; set; } = [];
    }

    public class CreateCommentDto
    {
        public string Content { get; set; } = string.Empty;
        public int NewsId { get; set; }
        public int? ParentId { get; set; }
    }
}