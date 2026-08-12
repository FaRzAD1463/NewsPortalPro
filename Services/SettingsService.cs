using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NewsPortalPro.Data;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using Newtonsoft.Json;

namespace NewsPortalPro.Services
{
    public class SettingsService(ApplicationDbContext db, IDistributedCache cache) : ISettingsService
    {
        private const string CacheKey = "site:settings";
        private static readonly DistributedCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        public async Task<string?> GetAsync(string key)
        {
            var settings = await GetAllCachedAsync();
            return settings.GetValueOrDefault(key);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await GetAsync(key);
            if (value == null) return default;
            try
            {
                return JsonConvert.DeserializeObject<T>(value);
            }
            catch
            {
                return default;
            }
        }

        public async Task<Dictionary<string, string>> GetGroupAsync(string group) =>
            await db.SiteSettings
                .Where(s => s.Group == group)
                .ToDictionaryAsync(
                    s => s.Key,
                    s => s.Value ?? string.Empty);

        public async Task SetAsync(string key, string value,
            string? updatedById = null)
        {
            var setting = await db.SiteSettings
                .AsTracking()
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                db.SiteSettings.Add(new SiteSetting
                {
                    Key = key,
                    Value = value,
                    UpdatedById = updatedById,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedById = updatedById;
            }

            await db.SaveChangesAsync();
            await InvalidateCacheAsync();
        }

        public async Task SetBulkAsync(Dictionary<string, string> settings,
            string? updatedById = null)
        {
            foreach (var kvp in settings)
                await SetAsync(kvp.Key, kvp.Value, updatedById);
            await InvalidateCacheAsync();
        }

        // FIX: ISettingsService.InvalidateCache() is synchronous, but
        // IDistributedCache only exposes an async Remove. Fire-and-forget
        // is fine here — a cache invalidation delayed by a few
        // milliseconds is harmless, and the existing call sites in this
        // class (SetAsync/SetBulkAsync) already invalidate again via the
        // async path right after calling this, so the interface method
        // stays usable for any external caller that isn't in an async
        // context without changing its signature.

        public void InvalidateCache() => _ = InvalidateCacheAsync();

        private async Task InvalidateCacheAsync() =>
            await cache.RemoveAsync(CacheKey);

        // FIX: was IMemoryCache, which is per-process. Under more than
        // one app instance, each instance kept its own copy of this
        // cache — so calling InvalidateCache() on one instance (e.g. an
        // admin toggling MaintenanceMode) only cleared that instance's
        // copy. Every OTHER instance kept serving the stale value from
        // its own local cache for up to the full 30-minute TTL, which is
        // a real problem specifically for a flag that's supposed to take
        // effect immediately everywhere.
        //
        // Switched to IDistributedCache — already registered in
        // Program.cs, backed by Redis when configured or the in-memory
        // distributed cache fallback in Development. Being a single
        // shared store rather than one-per-instance, RemoveAsync now
        // invalidates the cache for every instance at once.
        private async Task<Dictionary<string, string>> GetAllCachedAsync()
        {
            var cached = await cache.GetStringAsync(CacheKey);
            if (cached != null)
            {
                try
                {
                    var deserialized =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(cached);
                    if (deserialized != null)
                        return deserialized;
                }
                catch
                {
                    // Corrupt cache entry — fall through and rebuild from DB
                }
            }

            var settings = await db.SiteSettings
                .ToDictionaryAsync(
                    s => s.Key,
                    s => s.Value ?? string.Empty);

            await cache.SetStringAsync(
                CacheKey,
                JsonConvert.SerializeObject(settings),
                CacheOptions);

            return settings;
        }
    }
}