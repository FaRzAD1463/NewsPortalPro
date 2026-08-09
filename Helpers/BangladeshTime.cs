namespace NewsPortalPro.Helpers
{
    /// <summary>
    /// Converts stored UTC timestamps to Bangladesh Standard Time (UTC+6,
    /// no daylight saving) for display. All DateTime values in the database
    /// are stored as UTC (DateTime.UtcNow) — this should only be used at
    /// the point of rendering to a user, never when saving or querying.
    /// </summary>
    public static class BangladeshTime
    {
        private static readonly TimeSpan Offset = TimeSpan.FromHours(6);

        public static DateTime ToLocal(this DateTime utc) =>
            DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(Offset);

        public static DateTime? ToLocal(this DateTime? utc) =>
            utc.HasValue ? ToLocal(utc.Value) : null;
    }
}