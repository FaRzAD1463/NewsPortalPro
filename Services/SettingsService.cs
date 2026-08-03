using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NewsPortalPro.Data;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using Newtonsoft.Json;

namespace NewsPortalPro.Services
{
    public class SettingsService(ApplicationDbContext db, IMemoryCache cache) : ISettingsService
    {
        private const string CacheKey = "site:settings";

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
            InvalidateCache();
        }

        public async Task SetBulkAsync(Dictionary<string, string> settings,
            string? updatedById = null)
        {
            foreach (var kvp in settings)
                await SetAsync(kvp.Key, kvp.Value, updatedById);
            InvalidateCache();
        }

        public void InvalidateCache() =>
            cache.Remove(CacheKey);

        private async Task<Dictionary<string, string>> GetAllCachedAsync()
        {
            if (cache.TryGetValue(CacheKey,
                out Dictionary<string, string>? cached) && cached != null)
                return cached;

            var settings = await db.SiteSettings
                .ToDictionaryAsync(
                    s => s.Key,
                    s => s.Value ?? string.Empty);

            cache.Set(CacheKey, settings, TimeSpan.FromMinutes(30));
            return settings;
        }
    }
}