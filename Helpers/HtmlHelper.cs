using System.Text.RegularExpressions;

namespace NewsPortalPro.Helpers
{
    public static class HtmlHelpers
    {
        public static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            return Regex.Replace(html, "<.*?>", string.Empty);
        }

        public static string Truncate(string text, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var stripped = StripHtml(text);
            return stripped.Length <= maxLength
                ? stripped
                : stripped[..maxLength].TrimEnd() + suffix;
        }

        public static string GenerateSummary(string content, int wordCount = 50)
        {
            var text = StripHtml(content);
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length <= wordCount
                ? text
                : string.Join(' ', words.Take(wordCount)) + "...";
        }

        public static string TimeAgo(DateTime dateTime)
        {
            var diff = DateTime.UtcNow - dateTime;
            if (diff.TotalSeconds < 60) return "এইমাত্র";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} মিনিট আগে";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ঘন্টা আগে";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} দিন আগে";
            if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)} সপ্তাহ আগে";
            if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)} মাস আগে";
            return $"{(int)(diff.TotalDays / 365)} বছর আগে";
        }
    }
}