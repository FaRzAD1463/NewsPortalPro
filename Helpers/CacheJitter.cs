namespace NewsPortalPro.Helpers
{
    /// <summary>
    /// Adds random variance ("jitter") to a cache TTL so that many entries
    /// set around the same time (e.g. right after a bulk cache invalidation)
    /// don't all expire at the exact same instant later. Without this,
    /// a wave of cache misses can hit the database simultaneously — a
    /// "thundering herd" / cache stampede.
    /// </summary>
    public static class CacheJitter
    {
        /// <summary>
        /// Returns a TTL randomly varied by ± jitterPercent of the base
        /// duration. Default 20% spreads a 2-minute TTL across roughly
        /// 1m48s–2m12s, which is enough to desynchronize expirations
        /// across concurrent requests without meaningfully weakening
        /// the cache's effectiveness.
        /// </summary>
        public static TimeSpan Apply(TimeSpan baseTtl, double jitterPercent = 0.2)
        {
            var jitterRangeSeconds = baseTtl.TotalSeconds * jitterPercent;
            var offsetSeconds =
                (Random.Shared.NextDouble() * 2 - 1) * jitterRangeSeconds;

            var resultSeconds = Math.Max(1, baseTtl.TotalSeconds + offsetSeconds);
            return TimeSpan.FromSeconds(resultSeconds);
        }
    }
}