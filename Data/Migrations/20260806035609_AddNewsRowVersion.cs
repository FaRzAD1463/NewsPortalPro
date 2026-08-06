using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsPortalPro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "News",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_News_Division",
                table: "News",
                column: "Division");

            migrationBuilder.CreateIndex(
                name: "IX_News_Division_District",
                table: "News",
                columns: new[] { "Division", "District" });

            migrationBuilder.CreateIndex(
                name: "IX_News_Division_District_Status_PublishedAt",
                table: "News",
                columns: new[] { "Division", "District", "Status", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_News_Division",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_News_Division_District",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_News_Division_District_Status_PublishedAt",
                table: "News");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "News");
        }
    }
}
