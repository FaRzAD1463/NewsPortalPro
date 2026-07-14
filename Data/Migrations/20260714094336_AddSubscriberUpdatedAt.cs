using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsPortalPro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriberUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Subscribers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Subscribers");
        }
    }
}
