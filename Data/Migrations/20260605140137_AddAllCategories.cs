using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NewsPortalPro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "ColorCode", "CreatedAt", "Description", "DisplayOrder", "ImageUrl", "IsActive", "IsDeleted", "MetaDescription", "MetaKeywords", "MetaTitle", "Name", "ParentId", "ShowInMenu", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { 11, "#00bcd4", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 11, null, true, false, null, null, null, "তথ্যপ্রযুক্তি", null, false, "information-technology", null },
                    { 12, "#607d8b", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, null, true, false, null, null, null, "আইন-আদালত", null, false, "court-of-law", null },
                    { 13, "#9c27b0", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 13, null, true, false, null, null, null, "বিশেষ", null, false, "special", null },
                    { 14, "#f44336", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 14, null, true, false, null, null, null, "ফ্যাক্ট চেক", null, false, "fact-check", null },
                    { 15, "#009688", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 15, null, true, false, null, null, null, "অদম্য বাংলাদেশ", null, false, "indomitable-bangladesh", null },
                    { 16, "#ff5722", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 16, null, true, false, null, null, null, "আলোর পথে", null, false, "on-the-path-of-light", null },
                    { 17, "#e74c3c", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 17, null, true, false, null, null, null, "বিশেষ সংবাদ", null, false, "special-news", null },
                    { 18, "#4caf50", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 18, null, true, false, null, null, null, "ধর্ম", null, false, "religion", null },
                    { 19, "#2196f3", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 19, null, true, false, null, null, null, "রাজধানী", null, false, "capital", null },
                    { 20, "#9c27b0", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 20, null, true, false, null, null, null, "সাহিত্য", null, false, "literature", null },
                    { 21, "#ff9800", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 21, null, true, false, null, null, null, "শিল্প ও সংস্কৃতি", null, false, "art-and-culture", null },
                    { 22, "#e91e63", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 22, null, true, false, null, null, null, "লিঙ্গ-জাতি", null, false, "race-gender", null },
                    { 23, "#795548", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 23, null, true, false, null, null, null, "ম্যাগাজিন", null, false, "magazine", null },
                    { 24, "#607d8b", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 24, null, true, false, null, null, null, "বার্ষিকী", null, false, "anniversary", null },
                    { 25, "#009688", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 25, null, true, false, null, null, null, "মোহনা", null, false, "estuary", null },
                    { 26, "#4caf50", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 26, null, true, false, null, null, null, "ভ্রমণ", null, false, "travel", null },
                    { 27, "#8bc34a", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 27, null, true, false, null, null, null, "কৃষি ও প্রকৃতি", null, false, "agriculture-and-nature", null },
                    { 28, "#f44336", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 28, null, true, false, null, null, null, "ভিডিও", null, false, "video-news", null },
                    { 29, "#3f51b5", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 29, null, true, false, null, null, null, "সংগঠন", null, false, "organization", null },
                    { 30, "#ff5722", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 30, null, true, false, null, null, null, "ধানসিঁড়ি", null, false, "dhansiri", null },
                    { 31, "#9e9e9e", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 31, null, true, false, null, null, null, "বিজ্ঞপ্তি", null, false, "notification", null },
                    { 32, "#f44336", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 32, null, true, false, null, null, null, "অপরাধ", null, false, "crime", null },
                    { 33, "#795548", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 33, null, true, false, null, null, null, "জীবন যাপন", null, false, "life-lived", null },
                    { 34, "#607d8b", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 34, null, true, false, null, null, null, "নির্বাসন", null, false, "exile", null },
                    { 35, "#2196f3", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 35, null, true, false, null, null, null, "মিডিয়া", null, false, "media", null },
                    { 36, "#00bcd4", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 36, null, true, false, null, null, null, "বিজ্ঞান", null, false, "science", null },
                    { 37, "#ff9800", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 37, null, true, false, null, null, null, "উদ্বোধনী অনুষ্ঠান", null, false, "inaugural-event", null },
                    { 38, "#9c27b0", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 38, null, true, false, null, null, null, "বিশেষ আয়োজন", null, false, "special-arrangements", null },
                    { 39, "#4caf50", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 39, null, true, false, null, null, null, "ঈদ সংখ্যা", null, false, "eid-number", null },
                    { 40, "#607d8b", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 40, null, true, false, null, null, null, "মতামত", null, true, "opinion", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 40);
        }
    }
}
