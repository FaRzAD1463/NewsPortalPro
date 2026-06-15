using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsPortalPro.Data.Migrations
{
    public partial class AddPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Homepage query — most critical ─────────────────
            // Used by: GetPublishedAsync, GetFeaturedAsync
            migrationBuilder.CreateIndex(
                name: "IX_News_Status_PublishedAt_IsDeleted",
                table: "News",
                columns: new[] { "Status", "PublishedAt", "IsDeleted" });

            // ── Breaking news ticker ───────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_News_IsBreaking_Status_IsDeleted",
                table: "News",
                columns: new[] { "IsBreaking", "Status", "IsDeleted" });

            // ── Featured news ──────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_News_IsFeatured_Status_IsDeleted",
                table: "News",
                columns: new[] { "IsFeatured", "Status", "IsDeleted" });

            // ── Category page — slug lookup ────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug_IsActive",
                table: "Categories",
                columns: new[] { "Slug", "IsActive" });

            // ── News view tracking ─────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_NewsViews_NewsId_ViewedAt",
                table: "NewsViews",
                columns: new[] { "NewsId", "ViewedAt" });

            // ── Visitor analytics ──────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_VisitorAnalytics_VisitedAt",
                table: "VisitorAnalytics",
                columns: new[] { "VisitedAt" });

            // ── Comments by news + status ──────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_Comments_NewsId_Status_IsDeleted",
                table: "Comments",
                columns: new[] { "NewsId", "Status", "IsDeleted" });

            // ── Notifications by user + read status ───────────
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                "IX_News_Status_PublishedAt_IsDeleted", "News");
            migrationBuilder.DropIndex(
                "IX_News_IsBreaking_Status_IsDeleted", "News");
            migrationBuilder.DropIndex(
                "IX_News_IsFeatured_Status_IsDeleted", "News");
            migrationBuilder.DropIndex(
                "IX_Categories_Slug_IsActive", "Categories");
            migrationBuilder.DropIndex(
                "IX_NewsViews_NewsId_ViewedAt", "NewsViews");
            migrationBuilder.DropIndex(
                "IX_VisitorAnalytics_VisitedAt", "VisitorAnalytics");
            migrationBuilder.DropIndex(
                "IX_Comments_NewsId_Status_IsDeleted", "Comments");
            migrationBuilder.DropIndex(
                "IX_Notifications_UserId_IsRead_CreatedAt",
                "Notifications");
        }
    }
}