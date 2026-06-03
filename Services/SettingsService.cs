using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NewsPortalPro.Data;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using Newtonsoft.Json;

namespace NewsPortalPro.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "site_settings";

        public SettingsService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<string?> GetAsync(string key)
        {
            var settings = await GetAllCachedAsync();
            return settings.GetValueOrDefault(key);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await GetAsync(key);
            if (value == null) return default;
            try { return JsonConvert.DeserializeObject<T>(value); }
            catch { return default; }
        }

        public async Task<Dictionary<string, string>> GetGroupAsync(string group)
        {
            return await _db.SiteSettings
                .Where(s => s.Group == group)
                .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty);
        }

        public async Task SetAsync(string key, string value, string? updatedById = null)
        {
            var setting = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                _db.SiteSettings.Add(new SiteSetting { Key = key, Value = value, UpdatedById = updatedById });
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedById = updatedById;
            }
            await _db.SaveChangesAsync();
            InvalidateCache();
        }

        public async Task SetBulkAsync(Dictionary<string, string> settings, string? updatedById = null)
        {
            foreach (var kvp in settings)
                await SetAsync(kvp.Key, kvp.Value, updatedById);
            InvalidateCache();
        }

        public void InvalidateCache() => _cache.Remove(CacheKey);

        private async Task<Dictionary<string, string>> GetAllCachedAsync()
        {
            if (_cache.TryGetValue(CacheKey, out Dictionary<string, string>? cached) && cached != null)
                return cached;

            var settings = await _db.SiteSettings
                .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty);

            _cache.Set(CacheKey, settings, TimeSpan.FromMinutes(30));
            return settings;
        }
    }
}