using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsPortalPro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionDistrictToNews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "News",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Division",
                table: "News",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "District",
                table: "News");

            migrationBuilder.DropColumn(
                name: "Division",
                table: "News");
        }
    }
}
