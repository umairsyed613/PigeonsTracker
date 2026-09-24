using Blazored.LocalStorage;
using PigeonsTracker.DataModels;

namespace PigeonsTracker.Services;

public class LocalStorageCacheService : ICacheService
{
    private readonly ILocalStorageService _localStorage;

    public LocalStorageCacheService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
    }

    public async Task<CacheEntry<T>?> GetAsync<T>(string key)
    {
        var data = await _localStorage.GetItemAsync<T>($"{key}_data");
        if (data == null) return null;

        var meta = await _localStorage.GetItemAsync<CacheMeta>($"{key}_meta");
        var cachedAt = meta?.CachedAt ?? DateTime.UtcNow;
        var syncAt = meta?.LastSyncAt ?? DateTime.UtcNow;

        return new CacheEntry<T>(data, cachedAt, syncAt);
    }

    public async Task SetAsync<T>(string key, T data, DateTime? syncAt = null)
    {
        var now = DateTime.UtcNow;
        await _localStorage.SetItemAsync($"{key}_data", data);
        await _localStorage.SetItemAsync($"{key}_meta", new CacheMeta
        {
            CachedAt = now,
            LastSyncAt = syncAt ?? now
        });
    }

    public async Task<DateTime?> GetLastSyncAtAsync(string key)
    {
        var meta = await _localStorage.GetItemAsync<CacheMeta>($"{key}_meta");
        return meta?.LastSyncAt;
    }

    public async Task UpdateSyncMetadataAsync(string key, DateTime syncAt)
    {
        var meta = await _localStorage.GetItemAsync<CacheMeta>($"{key}_meta")
                   ?? new CacheMeta { CachedAt = DateTime.UtcNow };
        meta.LastSyncAt = syncAt;
        await _localStorage.SetItemAsync($"{key}_meta", meta);
    }

    public async Task InvalidateAsync(string key)
    {
        await _localStorage.RemoveItemAsync($"{key}_data");
        await _localStorage.RemoveItemAsync($"{key}_meta");
    }

    public async Task<bool> HasCacheAsync(string key)
    {
        return await _localStorage.ContainKeyAsync($"{key}_data");
    }

    private class CacheMeta
    {
        public DateTime CachedAt { get; set; }
        public DateTime LastSyncAt { get; set; }
    }
}
