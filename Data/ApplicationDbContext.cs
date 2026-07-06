using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Models;

namespace NewsPortalPro.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ══════════════════════════════════════════════════════
        // DB SETS
        // ══════════════════════════════════════════════════════

        public DbSet<News> News => Set<News>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<NewsTag> NewsTags => Set<NewsTag>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Reaction> Reactions => Set<Reaction>();
        public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Advertisement> Advertisements => Set<Advertisement>();
        public DbSet<Poll> Polls => Set<Poll>();
        public DbSet<PollOption> PollOptions => Set<PollOption>();
        public DbSet<Vote> Votes => Set<Vote>();
        public DbSet<Gallery> Galleries => Set<Gallery>();
        public DbSet<Photo> Photos => Set<Photo>();
        public DbSet<Video> Videos => Set<Video>();
        public DbSet<NewsView> NewsViews => Set<NewsView>();
        public DbSet<VisitorAnalytics> VisitorAnalytics => Set<VisitorAnalytics>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
        public DbSet<Newsletter> Newsletters => Set<Newsletter>();
        public DbSet<Subscriber> Subscribers => Set<Subscriber>();
        public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
        public DbSet<SEOData> SEOData => Set<SEOData>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserFollowCategory> UserFollowCategories => Set<UserFollowCategory>();

        public DbSet<Epaper> Epapers { get; set; }

        // ══════════════════════════════════════════════════════
        // MODEL CONFIGURATION
        // ══════════════════════════════════════════════════════

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── Rename Identity tables ──────────────────────────
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<ApplicationRole>().ToTable("Roles");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>()
                .ToTable("UserRoles");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>()
                .ToTable("UserClaims");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>()
                .ToTable("UserLogins");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>()
                .ToTable("RoleClaims");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>()
                .ToTable("UserTokens");

            // ── ApplicationUser ─────────────────────────────────
            builder.Entity<ApplicationUser>(e =>
            {
                e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
                e.Property(u => u.ProfilePicture).HasMaxLength(200);
                e.Property(u => u.Bio).HasMaxLength(500);
                e.Property(u => u.Designation).HasMaxLength(100);
                e.Property(u => u.FacebookUrl).HasMaxLength(200);
                e.Property(u => u.TwitterUrl).HasMaxLength(200);

                e.HasIndex(u => u.IsActive);
                e.HasIndex(u => u.IsDeleted);
                e.HasIndex(u => u.CreatedAt);

                e.HasQueryFilter(u => !u.IsDeleted);
            });

            // ── ApplicationRole ─────────────────────────────────
            builder.Entity<ApplicationRole>(e =>
            {
                e.Property(r => r.Description).HasMaxLength(200);
            });

            // ── Permission ──────────────────────────────────────
            builder.Entity<Permission>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).HasMaxLength(100).IsRequired();
                e.Property(p => p.Description).HasMaxLength(200);
                e.Property(p => p.Module).HasMaxLength(50);
                e.HasIndex(p => p.Name).IsUnique();
            });

            // ── RolePermission ──────────────────────────────────
            builder.Entity<RolePermission>(e =>
            {
                e.HasKey(rp => rp.Id);
                e.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();

                e.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Category ────────────────────────────────────────
            builder.Entity<Category>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).HasMaxLength(100).IsRequired();
                e.Property(c => c.Slug).HasMaxLength(120).IsRequired();
                e.Property(c => c.Description).HasMaxLength(300);
                e.Property(c => c.ImageUrl).HasMaxLength(200);
                e.Property(c => c.ColorCode).HasMaxLength(10);
                e.Property(c => c.MetaTitle).HasMaxLength(160);
                e.Property(c => c.MetaDescription).HasMaxLength(300);
                e.Property(c => c.MetaKeywords).HasMaxLength(200);

                e.HasIndex(c => c.Slug).IsUnique();
                e.HasIndex(c => c.IsActive);
                e.HasIndex(c => c.IsDeleted);
                e.HasIndex(c => c.DisplayOrder);

                e.HasQueryFilter(c => !c.IsDeleted);

                e.HasOne(c => c.Parent)
                    .WithMany(c => c.Children)
                    .HasForeignKey(c => c.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── UserFollowCategory ──────────────────────────────
            builder.Entity<UserFollowCategory>(e =>
            {
                e.HasKey(ufc => ufc.Id);
                e.HasIndex(ufc => new { ufc.UserId, ufc.CategoryId }).IsUnique();

                e.HasOne(ufc => ufc.User)
                    .WithMany(u => u.FollowedCategories)
                    .HasForeignKey(ufc => ufc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ufc => ufc.Category)
                    .WithMany(c => c.Followers)
                    .HasForeignKey(ufc => ufc.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Tag ─────────────────────────────────────────────
            builder.Entity<Tag>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Name).HasMaxLength(80).IsRequired();
                e.Property(t => t.Slug).HasMaxLength(100).IsRequired();

                e.HasIndex(t => t.Slug).IsUnique();
                e.HasIndex(t => t.IsDeleted);

                e.HasQueryFilter(t => !t.IsDeleted);
            });

            // ── NewsTag (composite PK) ──────────────────────────
            builder.Entity<NewsTag>(e =>
            {
                e.HasKey(nt => new { nt.NewsId, nt.TagId });

                e.HasOne(nt => nt.News)
                    .WithMany(n => n.NewsTags)
                    .HasForeignKey(nt => nt.NewsId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(nt => nt.Tag)
                    .WithMany(t => t.NewsTags)
                    .HasForeignKey(nt => nt.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── News ─────────────────────────────────────────────
            builder.Entity<News>(e =>
            {
                e.HasKey(n => n.Id);
                e.Property(n => n.Title).HasMaxLength(300).IsRequired();
                e.Property(n => n.Slug).HasMaxLength(320).IsRequired();
                e.Property(n => n.Subtitle).HasMaxLength(400);
                e.Property(n => n.Content).IsRequired();
                e.Property(n => n.Summary).HasMaxLength(500);
                e.Property(n => n.FeaturedImage).HasMaxLength(300);
                e.Property(n => n.FeaturedImageAlt).HasMaxLength(200);
                e.Property(n => n.FeaturedImageCaption).HasMaxLength(300);
                e.Property(n => n.VideoUrl).HasMaxLength(500);
                e.Property(n => n.MetaTitle).HasMaxLength(160);
                e.Property(n => n.MetaDescription).HasMaxLength(300);
                e.Property(n => n.MetaKeywords).HasMaxLength(250);
                e.Property(n => n.CanonicalUrl).HasMaxLength(300);
                e.Property(n => n.AuthorId).IsRequired();

                e.HasIndex(n => n.Slug).IsUnique();
                e.HasIndex(n => n.Status);
                e.HasIndex(n => n.PublishedAt);
                e.HasIndex(n => n.IsBreaking);
                e.HasIndex(n => n.IsFeatured);
                e.HasIndex(n => n.IsDeleted);
                e.HasIndex(n => n.CategoryId);
                e.HasIndex(n => n.AuthorId);
                e.HasIndex(n => new { n.Status, n.PublishedAt });
                e.HasIndex(n => new { n.Status, n.IsBreaking });
                e.HasIndex(n => new { n.Status, n.IsFeatured });
                e.HasIndex(n => new { n.CategoryId, n.Status, n.PublishedAt });

                e.HasQueryFilter(n => !n.IsDeleted);

                e.HasOne(n => n.Category)
                    .WithMany(c => c.News)
                    .HasForeignKey(n => n.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(n => n.Author)
                    .WithMany(u => u.News)
                    .HasForeignKey(n => n.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(n => n.Editor)
                    .WithMany()
                    .HasForeignKey(n => n.EditorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Comment ──────────────────────────────────────────
            builder.Entity<Comment>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Content).IsRequired();
                e.Property(c => c.IpAddress).HasMaxLength(45);

                e.HasIndex(c => c.NewsId);
                e.HasIndex(c => c.UserId);
                e.HasIndex(c => c.Status);
                e.HasIndex(c => c.IsDeleted);
                e.HasIndex(c => c.ParentId);
                e.HasIndex(c => new { c.NewsId, c.Status });

                e.HasQueryFilter(c => !c.IsDeleted);

                e.HasOne(c => c.News)
                    .WithMany(n => n.Comments)
                    .HasForeignKey(c => c.NewsId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(c => c.Parent)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(c => c.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Reaction ─────────────────────────────────────────
            builder.Entity<Reaction>(e =>
            {
                e.HasKey(r => r.Id);

                e.HasIndex(r => new { r.NewsId, r.UserId }).IsUnique();
                e.HasIndex(r => r.NewsId);
                e.HasIndex(r => r.UserId);

                e.HasOne(r => r.News)
                    .WithMany(n => n.Reactions)
                    .HasForeignKey(r => r.NewsId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(r => r.User)
                    .WithMany(u => u.Reactions)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Bookmark ─────────────────────────────────────────
            builder.Entity<Bookmark>(e =>
            {
                e.HasKey(b => b.Id);

                e.HasIndex(b => new { b.UserId, b.NewsId }).IsUnique();
                e.HasIndex(b => b.UserId);

                e.HasOne(b => b.User)
                    .WithMany(u => u.Bookmarks)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(b => b.News)
                    .WithMany(n => n.Bookmarks)
                    .HasForeignKey(b => b.NewsId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Notification ─────────────────────────────────────
            builder.Entity<Notification>(e =>
            {
                e.HasKey(n => n.Id);
                e.Property(n => n.Title).HasMaxLength(200).IsRequired();
                e.Property(n => n.Message).HasMaxLength(500);
                e.Property(n => n.Link).HasMaxLength(300);

                e.HasIndex(n => n.UserId);
                e.HasIndex(n => n.IsRead);
                e.HasIndex(n => n.CreatedAt);
                e.HasIndex(n => new { n.UserId, n.IsRead });

                e.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Advertisement ─────────────────────────────────────
            builder.Entity<Advertisement>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Title).HasMaxLength(150).IsRequired();
                e.Property(a => a.ImageUrl).HasMaxLength(300);
                e.Property(a => a.TargetUrl).HasMaxLength(500);

                e.HasIndex(a => a.Position);
                e.HasIndex(a => a.Status);
                e.HasIndex(a => a.IsDeleted);
                e.HasIndex(a => new { a.Position, a.Status });

                e.HasQueryFilter(a => !a.IsDeleted);

                e.HasOne(a => a.Category)
                    .WithMany()
                    .HasForeignKey(a => a.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Poll ─────────────────────────────────────────────
            builder.Entity<Poll>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Question).HasMaxLength(300).IsRequired();

                e.HasIndex(p => p.IsActive);
                e.HasIndex(p => p.IsDeleted);

                e.HasQueryFilter(p => !p.IsDeleted);
            });

            // ── PollOption ────────────────────────────────────────
            builder.Entity<PollOption>(e =>
            {
                e.HasKey(po => po.Id);
                e.Property(po => po.OptionText).HasMaxLength(200).IsRequired();

                e.HasIndex(po => po.PollId);

                e.HasOne(po => po.Poll)
                    .WithMany(p => p.Options)
                    .HasForeignKey(po => po.PollId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Vote ──────────────────────────────────────────────
            builder.Entity<Vote>(e =>
            {
                e.HasKey(v => v.Id);
                e.Property(v => v.IpAddress).HasMaxLength(45);

                e.HasIndex(v => new { v.PollId, v.IpAddress });
                e.HasIndex(v => new { v.PollId, v.UserId });

                e.HasOne(v => v.Poll)
                    .WithMany(p => p.Votes)
                    .HasForeignKey(v => v.PollId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(v => v.Option)
                    .WithMany(po => po.Votes)
                    .HasForeignKey(v => v.PollOptionId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(v => v.User)
                    .WithMany(u => u.Votes)
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Gallery ───────────────────────────────────────────
            builder.Entity<Gallery>(e =>
            {
                e.HasKey(g => g.Id);
                e.Property(g => g.Title).HasMaxLength(200).IsRequired();
                e.Property(g => g.Description).HasMaxLength(400);
                e.Property(g => g.CoverImage).HasMaxLength(200);

                e.HasIndex(g => g.IsDeleted);
                e.HasIndex(g => g.NewsId);

                e.HasQueryFilter(g => !g.IsDeleted);

                e.HasOne(g => g.News)
                    .WithMany()
                    .HasForeignKey(g => g.NewsId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Photo ─────────────────────────────────────────────
            builder.Entity<Photo>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.ImageUrl).HasMaxLength(300).IsRequired();
                e.Property(p => p.ThumbnailUrl).HasMaxLength(300);
                e.Property(p => p.AltText).HasMaxLength(200);
                e.Property(p => p.Caption).HasMaxLength(300);

                e.HasIndex(p => p.GalleryId);
                e.HasIndex(p => p.NewsId);
                e.HasIndex(p => p.UploadedAt);

                e.HasOne(p => p.Gallery)
                    .WithMany(g => g.Photos)
                    .HasForeignKey(p => p.GalleryId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(p => p.News)
                    .WithMany(n => n.Photos)
                    .HasForeignKey(p => p.NewsId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Video ─────────────────────────────────────────────
            builder.Entity<Video>(e =>
            {
                e.HasKey(v => v.Id);
                e.Property(v => v.Title).HasMaxLength(300).IsRequired();
                e.Property(v => v.Description).HasMaxLength(500);
                e.Property(v => v.VideoUrl).HasMaxLength(500).IsRequired();
                e.Property(v => v.ThumbnailUrl).HasMaxLength(300);
                e.Property(v => v.Duration).HasMaxLength(20);

                e.HasIndex(v => v.IsDeleted);
                e.HasIndex(v => v.NewsId);

                e.HasQueryFilter(v => !v.IsDeleted);

                e.HasOne(v => v.News)
                    .WithMany()
                    .HasForeignKey(v => v.NewsId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── NewsView ──────────────────────────────────────────
            builder.Entity<NewsView>(e =>
            {
                e.HasKey(nv => nv.Id);
                e.Property(nv => nv.IpAddress).HasMaxLength(45);
                e.Property(nv => nv.UserAgent).HasMaxLength(300);
                e.Property(nv => nv.Referrer).HasMaxLength(300);
                e.Property(nv => nv.Country).HasMaxLength(10);
                e.Property(nv => nv.Device).HasMaxLength(50);

                e.HasIndex(nv => nv.NewsId);
                e.HasIndex(nv => nv.ViewedAt);
                e.HasIndex(nv => nv.IpAddress);
                e.HasIndex(nv => new { nv.NewsId, nv.IpAddress, nv.ViewedAt });

                e.HasOne(nv => nv.News)
                    .WithMany(n => n.NewsViews)
                    .HasForeignKey(nv => nv.NewsId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── VisitorAnalytics ──────────────────────────────────
            builder.Entity<VisitorAnalytics>(e =>
            {
                e.HasKey(va => va.Id);
                e.Property(va => va.IpAddress).HasMaxLength(45);
                e.Property(va => va.Page).HasMaxLength(300);
                e.Property(va => va.UserAgent).HasMaxLength(300);
                e.Property(va => va.Referrer).HasMaxLength(300);
                e.Property(va => va.Country).HasMaxLength(10);
                e.Property(va => va.Device).HasMaxLength(50);
                e.Property(va => va.Browser).HasMaxLength(50);

                e.HasIndex(va => va.VisitedAt);
                e.HasIndex(va => va.IpAddress);
                e.HasIndex(va => va.Device);
                e.HasIndex(va => va.UserId);
            });

            // ── AuditLog ──────────────────────────────────────────
            builder.Entity<AuditLog>(e =>
            {
                e.HasKey(al => al.Id);
                e.Property(al => al.Action).HasMaxLength(100).IsRequired();
                e.Property(al => al.EntityName).HasMaxLength(100);
                e.Property(al => al.EntityId).HasMaxLength(50);
                e.Property(al => al.IpAddress).HasMaxLength(45);
                e.Property(al => al.UserAgent).HasMaxLength(300);

                e.HasIndex(al => al.CreatedAt);
                e.HasIndex(al => al.UserId);
                e.HasIndex(al => al.Action);
                e.HasIndex(al => new { al.UserId, al.CreatedAt });
            });

            // ── ContactMessage ────────────────────────────────────
            builder.Entity<ContactMessage>(e =>
            {
                e.HasKey(cm => cm.Id);
                e.Property(cm => cm.Name).HasMaxLength(100).IsRequired();
                e.Property(cm => cm.Email).HasMaxLength(150).IsRequired();
                e.Property(cm => cm.Phone).HasMaxLength(20);
                e.Property(cm => cm.Subject).HasMaxLength(200).IsRequired();
                e.Property(cm => cm.Message).IsRequired();
                e.Property(cm => cm.IpAddress).HasMaxLength(45);

                e.HasIndex(cm => cm.Status);
                e.HasIndex(cm => cm.IsDeleted);
                e.HasIndex(cm => cm.CreatedAt);

                e.HasQueryFilter(cm => !cm.IsDeleted);
            });

            // ── Newsletter ────────────────────────────────────────
            builder.Entity<Newsletter>(e =>
            {
                e.HasKey(n => n.Id);
                e.Property(n => n.Subject).HasMaxLength(200).IsRequired();
                e.Property(n => n.Body).IsRequired();

                e.HasIndex(n => n.IsSent);
                e.HasIndex(n => n.CreatedAt);
            });

            // ── Subscriber ────────────────────────────────────────
            builder.Entity<Subscriber>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Email).HasMaxLength(150).IsRequired();
                e.Property(s => s.Name).HasMaxLength(100);
                e.Property(s => s.ConfirmationToken).HasMaxLength(100);
                e.Property(s => s.IpAddress).HasMaxLength(45);

                e.HasIndex(s => s.Email).IsUnique();
                e.HasIndex(s => s.IsActive);
                e.HasIndex(s => s.IsConfirmed);
            });

            // ── SiteSetting ───────────────────────────────────────
            builder.Entity<SiteSetting>(e =>
            {
                e.HasKey(ss => ss.Id);
                e.Property(ss => ss.Key).HasMaxLength(100).IsRequired();
                e.Property(ss => ss.Description).HasMaxLength(200);
                e.Property(ss => ss.Group).HasMaxLength(50);

                e.HasIndex(ss => ss.Key).IsUnique();
                e.HasIndex(ss => ss.Group);
            });

            // ── SEOData ───────────────────────────────────────────
            builder.Entity<SEOData>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.PageUrl).HasMaxLength(300).IsRequired();
                e.Property(s => s.MetaTitle).HasMaxLength(160);
                e.Property(s => s.MetaDescription).HasMaxLength(300);
                e.Property(s => s.MetaKeywords).HasMaxLength(250);
                e.Property(s => s.OgTitle).HasMaxLength(300);
                e.Property(s => s.OgDescription).HasMaxLength(300);
                e.Property(s => s.OgImage).HasMaxLength(300);
                e.Property(s => s.PageType).HasMaxLength(50);

                e.HasIndex(s => s.PageUrl).IsUnique();
            });

            // ══════════════════════════════════════════════════════
            // SEED DATA
            // ══════════════════════════════════════════════════════
            SeedData(builder);
        }

        // ══════════════════════════════════════════════════════
        // SEED DATA METHOD
        // ══════════════════════════════════════════════════════

        private static void SeedData(ModelBuilder builder)
        {
            // ── Roles ─────────────────────────────────────────────
            var adminRoleId = "role-admin-0000-0000-000000000001";
            var editorRoleId = "role-editor-000-0000-000000000002";
            var reporterRoleId = "role-reporter-00-0000-000000000003";
            var userRoleId = "role-user-0000-0000-000000000004";

            builder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "Full system access",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ApplicationRole
                {
                    Id = editorRoleId,
                    Name = "Editor",
                    NormalizedName = "EDITOR",
                    Description = "Manage and publish content",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ApplicationRole
                {
                    Id = reporterRoleId,
                    Name = "Reporter",
                    NormalizedName = "REPORTER",
                    Description = "Create and submit content",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ApplicationRole
                {
                    Id = userRoleId,
                    Name = "User",
                    NormalizedName = "USER",
                    Description = "Regular registered user",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // ── Permissions ───────────────────────────────────────
            builder.Entity<Permission>().HasData(
                new Permission { Id = 1, Name = "news.create", Module = "News", Description = "Create news articles", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 2, Name = "news.edit", Module = "News", Description = "Edit news articles", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 3, Name = "news.delete", Module = "News", Description = "Delete news articles", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 4, Name = "news.publish", Module = "News", Description = "Publish news articles", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 5, Name = "comment.manage", Module = "Comments", Description = "Manage all comments", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 6, Name = "user.manage", Module = "Users", Description = "Manage users", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 7, Name = "ads.manage", Module = "Ads", Description = "Manage advertisements", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 8, Name = "settings.edit", Module = "Settings", Description = "Edit site settings", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // ── Categories ────────────────────────────────────────
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            builder.Entity<Category>().HasData(
                // ── Main categories (show in menu) ──────────────────
                new Category { Id = 1, Name = "জাতীয়", Slug = "national", DisplayOrder = 1, ColorCode = "#e74c3c", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },
                new Category { Id = 2, Name = "রাজনীতি", Slug = "politics", DisplayOrder = 2, ColorCode = "#3498db", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },
                new Category { Id = 3, Name = "আন্তর্জাতিক", Slug = "international", DisplayOrder = 3, ColorCode = "#2ecc71", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },
                new Category { Id = 4, Name = "অর্থনীতি", Slug = "economy", DisplayOrder = 4, ColorCode = "#f39c12", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },
                new Category { Id = 5, Name = "খেলাধুলা", Slug = "sports", DisplayOrder = 5, ColorCode = "#9b59b6", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },
                new Category { Id = 6, Name = "বিনোদন", Slug = "entertainment", DisplayOrder = 6, ColorCode = "#e91e63", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },
                new Category { Id = 7, Name = "প্রযুক্তি", Slug = "technology", DisplayOrder = 7, ColorCode = "#00bcd4", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },
                new Category { Id = 8, Name = "স্বাস্থ্য", Slug = "health", DisplayOrder = 8, ColorCode = "#4caf50", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },
                new Category { Id = 9, Name = "শিক্ষা", Slug = "education", DisplayOrder = 9, ColorCode = "#ff9800", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },
                new Category { Id = 10, Name = "লাইফস্টাইল", Slug = "lifestyle", DisplayOrder = 10, ColorCode = "#795548", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },
                new Category { Id = 40, Name = "মতামত", Slug = "opinion", DisplayOrder = 40, ColorCode = "#607d8b", IsActive = true, ShowInMenu = true, CreatedAt = seedDate },

                // ── Sub-categories (not in main menu) ───────────────
                new Category { Id = 11, Name = "তথ্যপ্রযুক্তি", Slug = "information-technology", DisplayOrder = 11, ColorCode = "#00bcd4", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 12, Name = "আইন-আদালত", Slug = "court-of-law", DisplayOrder = 12, ColorCode = "#607d8b", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 13, Name = "বিশেষ", Slug = "special", DisplayOrder = 13, ColorCode = "#9c27b0", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 14, Name = "ফ্যাক্ট চেক", Slug = "fact-check", DisplayOrder = 14, ColorCode = "#f44336", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 15, Name = "অদম্য বাংলাদেশ", Slug = "indomitable-bangladesh", DisplayOrder = 15, ColorCode = "#009688", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 16, Name = "আলোর পথে", Slug = "on-the-path-of-light", DisplayOrder = 16, ColorCode = "#ff5722", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 17, Name = "বিশেষ সংবাদ", Slug = "special-news", DisplayOrder = 17, ColorCode = "#e74c3c", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 18, Name = "ধর্ম", Slug = "religion", DisplayOrder = 18, ColorCode = "#4caf50", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 19, Name = "রাজধানী", Slug = "capital", DisplayOrder = 19, ColorCode = "#2196f3", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 20, Name = "সাহিত্য", Slug = "literature", DisplayOrder = 20, ColorCode = "#9c27b0", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 21, Name = "শিল্প ও সংস্কৃতি", Slug = "art-and-culture", DisplayOrder = 21, ColorCode = "#ff9800", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 22, Name = "লিঙ্গ-জাতি", Slug = "race-gender", DisplayOrder = 22, ColorCode = "#e91e63", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 23, Name = "ম্যাগাজিন", Slug = "magazine", DisplayOrder = 23, ColorCode = "#795548", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 24, Name = "বার্ষিকী", Slug = "anniversary", DisplayOrder = 24, ColorCode = "#607d8b", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 25, Name = "মোহনা", Slug = "estuary", DisplayOrder = 25, ColorCode = "#009688", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 26, Name = "ভ্রমণ", Slug = "travel", DisplayOrder = 26, ColorCode = "#4caf50", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 27, Name = "কৃষি ও প্রকৃতি", Slug = "agriculture-and-nature", DisplayOrder = 27, ColorCode = "#8bc34a", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 28, Name = "ভিডিও", Slug = "video-news", DisplayOrder = 28, ColorCode = "#f44336", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 29, Name = "সংগঠন", Slug = "organization", DisplayOrder = 29, ColorCode = "#3f51b5", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 30, Name = "ধানসিঁড়ি", Slug = "dhansiri", DisplayOrder = 30, ColorCode = "#ff5722", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 31, Name = "বিজ্ঞপ্তি", Slug = "notification", DisplayOrder = 31, ColorCode = "#9e9e9e", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 32, Name = "অপরাধ", Slug = "crime", DisplayOrder = 32, ColorCode = "#f44336", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 33, Name = "জীবন যাপন", Slug = "life-lived", DisplayOrder = 33, ColorCode = "#795548", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 34, Name = "নির্বাসন", Slug = "exile", DisplayOrder = 34, ColorCode = "#607d8b", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 35, Name = "মিডিয়া", Slug = "media", DisplayOrder = 35, ColorCode = "#2196f3", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 36, Name = "বিজ্ঞান", Slug = "science", DisplayOrder = 36, ColorCode = "#00bcd4", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 37, Name = "উদ্বোধনী অনুষ্ঠান", Slug = "inaugural-event", DisplayOrder = 37, ColorCode = "#ff9800", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 38, Name = "বিশেষ আয়োজন", Slug = "special-arrangements", DisplayOrder = 38, ColorCode = "#9c27b0", IsActive = true, ShowInMenu = false, CreatedAt = seedDate },
                new Category { Id = 39, Name = "ঈদ সংখ্যা", Slug = "eid-number", DisplayOrder = 39, ColorCode = "#4caf50", IsActive = true, ShowInMenu = false, CreatedAt = seedDate }
            );

            // ── Site Settings ─────────────────────────────────────
            builder.Entity<SiteSetting>().HasData(
                new SiteSetting { Id = 1, Key = "SiteName", Value = "নিউজপোর্টাল প্রো", Group = "General", Description = "Site display name", UpdatedAt = seedDate },
                new SiteSetting { Id = 2, Key = "SiteUrl", Value = "https://newsportalpro.com", Group = "General", Description = "Primary site URL", UpdatedAt = seedDate },
                new SiteSetting { Id = 3, Key = "SiteDescription", Value = "বাংলাদেশের নির্ভরযোগ্য সংবাদ মাধ্যম", Group = "General", Description = "Site meta description", UpdatedAt = seedDate },
                new SiteSetting { Id = 4, Key = "SiteEmail", Value = "info@newsportalpro.com", Group = "General", Description = "Contact email address", UpdatedAt = seedDate },
                new SiteSetting { Id = 5, Key = "SitePhone", Value = "+880-1700-000000", Group = "General", Description = "Contact phone number", UpdatedAt = seedDate },
                new SiteSetting { Id = 6, Key = "LogoUrl", Value = "/images/logo.png", Group = "General", Description = "Logo image path", UpdatedAt = seedDate },
                new SiteSetting { Id = 7, Key = "FaviconUrl", Value = "/images/favicon.ico", Group = "General", Description = "Favicon path", UpdatedAt = seedDate },
                new SiteSetting { Id = 8, Key = "FacebookUrl", Value = "", Group = "Social", Description = "Facebook page URL", UpdatedAt = seedDate },
                new SiteSetting { Id = 9, Key = "TwitterUrl", Value = "", Group = "Social", Description = "Twitter/X profile URL", UpdatedAt = seedDate },
                new SiteSetting { Id = 10, Key = "YoutubeUrl", Value = "", Group = "Social", Description = "YouTube channel URL", UpdatedAt = seedDate },
                new SiteSetting { Id = 11, Key = "InstagramUrl", Value = "", Group = "Social", Description = "Instagram profile URL", UpdatedAt = seedDate },
                new SiteSetting { Id = 12, Key = "NewsPerPage", Value = "20", Group = "Content", Description = "Number of news per page", UpdatedAt = seedDate },
                new SiteSetting { Id = 13, Key = "CommentModeration", Value = "true", Group = "Content", Description = "Require comment approval", UpdatedAt = seedDate },
                new SiteSetting { Id = 14, Key = "AllowGuestComments", Value = "false", Group = "Content", Description = "Allow comments without login", UpdatedAt = seedDate },
                new SiteSetting { Id = 15, Key = "MaintenanceMode", Value = "false", Group = "System", Description = "Enable maintenance mode", UpdatedAt = seedDate },
                new SiteSetting { Id = 16, Key = "GoogleAnalyticsId", Value = "", Group = "SEO", Description = "Google Analytics tracking ID", UpdatedAt = seedDate },
                new SiteSetting { Id = 17, Key = "GoogleSiteVerification", Value = "", Group = "SEO", Description = "Google Search Console verification", UpdatedAt = seedDate },
                new SiteSetting { Id = 18, Key = "SmtpHost", Value = "", Group = "Email", Description = "SMTP server hostname", UpdatedAt = seedDate },
                new SiteSetting { Id = 19, Key = "SmtpPort", Value = "587", Group = "Email", Description = "SMTP server port", UpdatedAt = seedDate },
                new SiteSetting { Id = 20, Key = "SmtpUser", Value = "", Group = "Email", Description = "SMTP username", UpdatedAt = seedDate },
                new SiteSetting { Id = 21, Key = "SmtpPassword", Value = "", Group = "Email", Description = "SMTP password", UpdatedAt = seedDate },
                new SiteSetting { Id = 22, Key = "SmtpSenderName", Value = "নিউজপোর্টাল প্রো", Group = "Email", Description = "Email sender display name", UpdatedAt = seedDate },
                new SiteSetting { Id = 23, Key = "WeatherApiKey", Value = "", Group = "Widgets", Description = "OpenWeatherMap API key", UpdatedAt = seedDate },
                new SiteSetting { Id = 24, Key = "WeatherCity", Value = "Dhaka", Group = "Widgets", Description = "Default city for weather widget", UpdatedAt = seedDate },
                new SiteSetting { Id = 25, Key = "LiveTvEmbedUrl", Value = "", Group = "Widgets", Description = "Live TV iframe embed URL", UpdatedAt = seedDate },
                new SiteSetting { Id = 26, Key = "PrayerTimeCity", Value = "Dhaka", Group = "Widgets", Description = "City for prayer time widget", UpdatedAt = seedDate },
                new SiteSetting { Id = 27, Key = "MaxUploadSizeMB", Value = "10", Group = "System", Description = "Maximum file upload size in MB", UpdatedAt = seedDate },
                new SiteSetting { Id = 28, Key = "AllowedImageTypes", Value = "jpg,jpeg,png,webp,gif", Group = "System", Description = "Allowed image file extensions", UpdatedAt = seedDate }
            );
        }
    }
}