using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace NewsPortalPro.Models
{
        public class ApplicationUser : IdentityUser
        {
        [StringLength(100)] public string FullName { get; set; } = string.Empty;
        [StringLength(200)] public string? ProfilePicture { get; set; }
        [StringLength(500)] public string? Bio { get; set; }
        [StringLength(100)] public string? Designation { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        [StringLength(200)] public string? FacebookUrl { get; set; }
        [StringLength(200)] public string? TwitterUrl { get; set; }

        public ICollection<News> News { get; set; } = [];
        public ICollection<Comment> Comments { get; set; } = [];
        public ICollection<Bookmark> Bookmarks { get; set; } = [];
        public ICollection<Notification> Notifications { get; set; } = [];
        public ICollection<Reaction> Reactions { get; set; } = [];
        public ICollection<Vote> Votes { get; set; } = [];
        public ICollection<UserFollowCategory> FollowedCategories { get; set; } = [];
        }

        public class ApplicationRole : IdentityRole
        {
        [StringLength(200)] public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<RolePermission> RolePermissions { get; set; } = [];
        }

        public class Permission
        {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [StringLength(200)] public string? Description { get; set; }
        [StringLength(50)] public string? Module { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<RolePermission> RolePermissions { get; set; } = [];
        }

        public class RolePermission
        {
        public int Id { get; set; }
        [Required] public string RoleId { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public ApplicationRole Role { get; set; } = null!;
        public Permission Permission { get; set; } = null!;
        }

        public class Category
        {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [Required, StringLength(120)] public string Slug { get; set; } = string.Empty;
        [StringLength(300)] public string? Description { get; set; }
        [StringLength(200)] public string? ImageUrl { get; set; }
        [StringLength(10)] public string? ColorCode { get; set; }
        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public bool ShowInMenu { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        [StringLength(160)] public string? MetaTitle { get; set; }
        [StringLength(300)] public string? MetaDescription { get; set; }
        [StringLength(200)] public string? MetaKeywords { get; set; }

        public Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } = [];
        public ICollection<News> News { get; set; } = [];
        public ICollection<UserFollowCategory> Followers { get; set; } = [];
        }

        public class UserFollowCategory
        {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
        public ApplicationUser User { get; set; } = null!;
        public Category Category { get; set; } = null!;
        }

        public class Tag
        {
        public int Id { get; set; }
        [Required, StringLength(80)] public string Name { get; set; } = string.Empty;
        [Required, StringLength(100)] public string Slug { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<NewsTag> NewsTags { get; set; } = [];
        }

        public class NewsTag
        {
        public int NewsId { get; set; }
        public int TagId { get; set; }
        public News News { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
        }

        public enum NewsStatus { Draft, Published, Scheduled, Archived }
        public enum NewsType { Article, Video, Gallery, LiveBlog }

        public class News
        {
        public int Id { get; set; }
        [Required, StringLength(300)] public string Title { get; set; } = string.Empty;
        [Required, StringLength(320)] public string Slug { get; set; } = string.Empty;
        [StringLength(400)] public string? Subtitle { get; set; }
        [Required] public string Content { get; set; } = string.Empty;
        [StringLength(500)] public string? Summary { get; set; }
        [StringLength(300)] public string? FeaturedImage { get; set; }
        [StringLength(200)] public string? FeaturedImageAlt { get; set; }
        [StringLength(300)] public string? FeaturedImageCaption { get; set; }
        [StringLength(500)] public string? VideoUrl { get; set; }
        public int CategoryId { get; set; }
        [Required] public string AuthorId { get; set; } = string.Empty;
        public string? EditorId { get; set; }
        public NewsStatus Status { get; set; } = NewsStatus.Draft;
        public NewsType Type { get; set; } = NewsType.Article;
        public bool IsFeatured { get; set; } = false;
        public bool IsBreaking { get; set; } = false;
        public bool IsEditorPick { get; set; } = false;
        public bool AllowComments { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        public int CommentCount { get; set; } = 0;
        public int ShareCount { get; set; } = 0;
        public int ReadTimeMinutes { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? ScheduledAt { get; set; }
        [StringLength(160)] public string? MetaTitle { get; set; }
        [StringLength(300)] public string? MetaDescription { get; set; }
        [StringLength(250)] public string? MetaKeywords { get; set; }
        [StringLength(300)] public string? CanonicalUrl { get; set; }

        public Category Category { get; set; } = null!;
        public ApplicationUser Author { get; set; } = null!;
        public ApplicationUser? Editor { get; set; }
        public ICollection<NewsTag> NewsTags { get; set; } = [];
        public ICollection<Comment> Comments { get; set; } = [];
        public ICollection<Reaction> Reactions { get; set; } = [];
        public ICollection<Bookmark> Bookmarks { get; set; } = [];
        public ICollection<NewsView> NewsViews { get; set; } = [];
        public ICollection<Photo> Photos { get; set; } = [];
        }

        public enum CommentStatus { Pending, Approved, Rejected }

        public class Comment
        {
        public int Id { get; set; }
        [Required] public string Content { get; set; } = string.Empty;
        public int NewsId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public CommentStatus Status { get; set; } = CommentStatus.Pending;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        [StringLength(45)] public string? IpAddress { get; set; }

        public News News { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public Comment? Parent { get; set; }
        public ICollection<Comment> Replies { get; set; } = [];
        }

        public enum ReactionType { Like, Love, Haha, Sad, Angry, Wow }

        public class Reaction
        {
        public int Id { get; set; }
        public int NewsId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ReactionType Type { get; set; } = ReactionType.Like;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public News News { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        }

        public class Bookmark
        {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int NewsId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ApplicationUser User { get; set; } = null!;
        public News News { get; set; } = null!;
        }

        public enum NotificationType { Breaking, Comment, Reaction, System, NewsPublished }

        public class Notification
        {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
        [StringLength(500)] public string? Message { get; set; }
        [StringLength(300)] public string? Link { get; set; }
        public NotificationType Type { get; set; } = NotificationType.System;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public ApplicationUser User { get; set; } = null!;
        }

        public enum AdPosition {Header,Sidebar,InlineTop,InlineBottom,Footer,Popup,InArticle,BelowTitle }
        public enum AdStatus { Active, Inactive, Scheduled, Expired }

        public class Advertisement
        {
        public int Id { get; set; }
        [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
        [StringLength(300)] public string? ImageUrl { get; set; }
        [StringLength(500)] public string? TargetUrl { get; set; }
        public string? HtmlCode { get; set; }
        public AdPosition Position { get; set; } = AdPosition.Sidebar;
        public AdStatus Status { get; set; } = AdStatus.Active;
        public int DisplayOrder { get; set; } = 0;
        public int? CategoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int ImpressionCount { get; set; } = 0;
        public int ClickCount { get; set; } = 0;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Category? Category { get; set; }
        }

        public class Poll
        {
        public int Id { get; set; }
        [Required, StringLength(300)] public string Question { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public ICollection<PollOption> Options { get; set; } = [];
        public ICollection<Vote> Votes { get; set; } = [];
        }

        public class PollOption
        {
        public int Id { get; set; }
        public int PollId { get; set; }
        [Required, StringLength(200)] public string OptionText { get; set; } = string.Empty;
        public int VoteCount { get; set; } = 0;
        public Poll Poll { get; set; } = null!;
        public ICollection<Vote> Votes { get; set; } = [];
        }

        public class Vote
        {
        public int Id { get; set; }
        public int PollId { get; set; }
        public int PollOptionId { get; set; }
        public string? UserId { get; set; }
        [StringLength(45)] public string? IpAddress { get; set; }
        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
        public Poll Poll { get; set; } = null!;
        public PollOption Option { get; set; } = null!;
        public ApplicationUser? User { get; set; }
        }

        public class Gallery
        {
        public int Id { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
        [StringLength(400)] public string? Description { get; set; }
        [StringLength(200)] public string? CoverImage { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? NewsId { get; set; }
        public News? News { get; set; }
        public ICollection<Photo> Photos { get; set; } = [];
        }

        public class Photo
        {
        public int Id { get; set; }
        [Required, StringLength(300)] public string ImageUrl { get; set; } = string.Empty;
        [StringLength(300)] public string? ThumbnailUrl { get; set; }
        [StringLength(200)] public string? AltText { get; set; }
        [StringLength(300)] public string? Caption { get; set; }
        public int? GalleryId { get; set; }
        public int? NewsId { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public long FileSizeBytes { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string? UploadedById { get; set; }
        public Gallery? Gallery { get; set; }
        public News? News { get; set; }
        }

        public class Video
        {
        public int Id { get; set; }
        [Required, StringLength(300)] public string Title { get; set; } = string.Empty;
        [StringLength(500)] public string? Description { get; set; }
        [StringLength(500)] public string VideoUrl { get; set; } = string.Empty;
        [StringLength(300)] public string? ThumbnailUrl { get; set; }
        [StringLength(20)] public string? Duration { get; set; }
        public int? NewsId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public News? News { get; set; }
        }

        public class NewsView
        {
        public long Id { get; set; }
        public int NewsId { get; set; }
        public string? UserId { get; set; }
        [StringLength(45)] public string? IpAddress { get; set; }
        [StringLength(300)] public string? UserAgent { get; set; }
        [StringLength(300)] public string? Referrer { get; set; }
        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
        [StringLength(10)] public string? Country { get; set; }
        [StringLength(50)] public string? Device { get; set; }
        public News News { get; set; } = null!;
        }

        public class VisitorAnalytics
        {
        public long Id { get; set; }
        [StringLength(45)] public string? IpAddress { get; set; }
        [StringLength(300)] public string? Page { get; set; }
        [StringLength(300)] public string? UserAgent { get; set; }
        [StringLength(300)] public string? Referrer { get; set; }
        [StringLength(10)] public string? Country { get; set; }
        [StringLength(50)] public string? Device { get; set; }
        [StringLength(50)] public string? Browser { get; set; }
        public string? UserId { get; set; }
        public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
        public int SessionDurationSeconds { get; set; } = 0;
        }

        public class AuditLog
        {
        public long Id { get; set; }
        public string? UserId { get; set; }
        [StringLength(100)] public string Action { get; set; } = string.Empty;
        [StringLength(100)] public string? EntityName { get; set; }
        [StringLength(50)] public string? EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        [StringLength(45)] public string? IpAddress { get; set; }
        [StringLength(300)] public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; } = true;
        }

        public enum ContactStatus { New, InProgress, Resolved, Closed }

        public class ContactMessage
        {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [Required, StringLength(150)] public string Email { get; set; } = string.Empty;
        [StringLength(20)] public string? Phone { get; set; }
        [Required, StringLength(200)] public string Subject { get; set; } = string.Empty;
        [Required] public string Message { get; set; } = string.Empty;
        public ContactStatus Status { get; set; } = ContactStatus.New;
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(45)] public string? IpAddress { get; set; }
        }

        public class Newsletter
        {
        public int Id { get; set; }
        [Required, StringLength(200)] public string Subject { get; set; } = string.Empty;
        [Required] public string Body { get; set; } = string.Empty;
        public bool IsSent { get; set; } = false;
        public DateTime? SentAt { get; set; }
        public int RecipientCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedById { get; set; }
        }

        public class Subscriber
        {
        public int Id { get; set; }
        [Required, StringLength(150)] public string Email { get; set; } = string.Empty;
        [StringLength(100)] public string? Name { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsConfirmed { get; set; } = false;
        [StringLength(100)] public string? ConfirmationToken { get; set; }
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public DateTime? UnsubscribedAt { get; set; }
        [StringLength(45)] public string? IpAddress { get; set; }
        }

        public class SiteSetting
        {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        [StringLength(200)] public string? Description { get; set; }
        [StringLength(50)] public string? Group { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedById { get; set; }
        }

        public class SEOData
        {
        public int Id { get; set; }
        [Required, StringLength(300)] public string PageUrl { get; set; } = string.Empty;
        [StringLength(160)] public string? MetaTitle { get; set; }
        [StringLength(300)] public string? MetaDescription { get; set; }
        [StringLength(250)] public string? MetaKeywords { get; set; }
        [StringLength(300)] public string? OgTitle { get; set; }
        [StringLength(300)] public string? OgDescription { get; set; }
        [StringLength(300)] public string? OgImage { get; set; }
        [StringLength(50)] public string? PageType { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }
}