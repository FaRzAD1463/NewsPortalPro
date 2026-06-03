using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using Slugify;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NewsPortalPro.Services
{
    public class SEOService : ISEOService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISettingsService _settings;

        public SEOService(ApplicationDbContext db, ISettingsService settings)
        {
            _db = db;
            _settings = settings;
        }

        public async Task<SEOMetaDto> GetMetaForPageAsync(string pageUrl)
        {
            var seo = await _db.SEOData
                .FirstOrDefaultAsync(s => s.PageUrl == pageUrl);
            if (seo == null) return new SEOMetaDto();

            return new SEOMetaDto
            {
                Title = seo.MetaTitle ?? string.Empty,
                Description = seo.MetaDescription ?? string.Empty,
                Keywords = seo.MetaKeywords,
                OgTitle = seo.OgTitle,
                OgDescription = seo.OgDescription,
                OgImage = seo.OgImage
            };
        }

        public async Task<string> GenerateSitemapAsync()
        {
            var siteUrl = await _settings.GetAsync("SiteUrl")
                ?? "https://newsportalpro.com";

            var news = await _db.News
                .Where(n => n.Status == NewsStatus.Published)
                .Select(n => new
                {
                    n.Slug,
                    n.UpdatedAt,
                    n.PublishedAt
                })
                .ToListAsync();

            var categories = await _db.Categories
                .Where(c => c.IsActive)
                .Select(c => new { c.Slug, c.UpdatedAt })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine(
                "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            sb.AppendLine(
                $"  <url><loc>{siteUrl}</loc>" +
                $"<changefreq>hourly</changefreq><priority>1.0</priority></url>");

            foreach (var cat in categories)
            {
                sb.AppendLine($"  <url>");
                sb.AppendLine($"    <loc>{siteUrl}/category/{cat.Slug}</loc>");
                sb.AppendLine($"    <lastmod>{(cat.UpdatedAt ?? DateTime.UtcNow):yyyy-MM-dd}</lastmod>");
                sb.AppendLine($"    <changefreq>daily</changefreq>");
                sb.AppendLine($"    <priority>0.8</priority>");
                sb.AppendLine($"  </url>");
            }

            foreach (var n in news)
            {
                sb.AppendLine($"  <url>");
                sb.AppendLine($"    <loc>{siteUrl}/news/{n.Slug}</loc>");
                sb.AppendLine($"    <lastmod>{(n.UpdatedAt ?? n.PublishedAt ?? DateTime.UtcNow):yyyy-MM-dd}</lastmod>");
                sb.AppendLine($"    <changefreq>weekly</changefreq>");
                sb.AppendLine($"    <priority>0.7</priority>");
                sb.AppendLine($"  </url>");
            }

            sb.AppendLine("</urlset>");
            return sb.ToString();
        }

        public async Task<string> GenerateNewsRssFeedAsync()
        {
            var siteUrl = await _settings.GetAsync("SiteUrl")
                ?? "https://newsportalpro.com";
            var siteName = await _settings.GetAsync("SiteName")
                ?? "NewsPortal Pro";
            var siteDesc = await _settings.GetAsync("SiteDescription")
                ?? "";

            var news = await _db.News
                .Where(n => n.Status == NewsStatus.Published)
                .OrderByDescending(n => n.PublishedAt)
                .Take(50)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .ToListAsync();

            var rss = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("rss",
                    new XAttribute("version", "2.0"),
                    new XElement("channel",
                        new XElement("title", siteName),
                        new XElement("link", siteUrl),
                        new XElement("description", siteDesc),
                        new XElement("language", "bn"),
                        news.Select(n =>
                            new XElement("item",
                                new XElement("title", n.Title),
                                new XElement("link", $"{siteUrl}/news/{n.Slug}"),
                                new XElement("description", n.Summary),
                                new XElement("pubDate", n.PublishedAt?.ToString("R")),
                                new XElement("category", n.Category?.Name),
                                new XElement("author", n.Author?.FullName))))));

            return rss.ToString();
        }

        public string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;

            try
            {
                var helper = new SlugHelper();
                var slug = helper.GenerateSlug(title);
                if (!string.IsNullOrEmpty(slug)) return slug;
            }
            catch { }

            // Fallback for Bengali and other non-ASCII
            var fallback = Regex.Replace(title.ToLower().Trim(), @"\s+", "-");
            fallback = Regex.Replace(fallback, @"[^\w\-]", "");
            fallback = Regex.Replace(fallback, @"-+", "-").Trim('-');

            return string.IsNullOrEmpty(fallback)
                ? $"news-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
                : fallback;
        }

        public int CalculateReadTime(string content)
        {
            if (string.IsNullOrEmpty(content)) return 1;
            var stripped = Regex.Replace(content, "<.*?>", "");
            var wordCount = stripped
                .Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Length;
            return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
        }
    }
}