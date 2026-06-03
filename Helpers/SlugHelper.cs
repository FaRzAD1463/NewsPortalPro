using System.Text.RegularExpressions;

namespace NewsPortalPro.Helpers
{
    public static class SlugHelper
    {
        public static string Generate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Lowercase and trim
            var slug = text.Trim().ToLower();

            // Replace spaces and special chars with hyphen
            slug = Regex.Replace(slug, @"[^\w\s-]", "");
            slug = Regex.Replace(slug, @"[\s_]+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            // If empty after cleaning (e.g. pure Bengali), use timestamp
            if (string.IsNullOrEmpty(slug))
                slug = $"news-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            return slug;
        }

        public static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text[..maxLength].TrimEnd('-');
        }
    }
}