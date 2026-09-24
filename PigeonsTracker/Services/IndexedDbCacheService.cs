using Microsoft.JSInterop;
using PigeonsTracker.DataModels;
using System.Text.Json;

namespace PigeonsTracker.Services;

internal sealed class IndexedDbCacheService(IJSRuntime js) : ICacheService
{
    public async Task<CacheEntry<T>?> GetAsync<T>(string key)
    {
        var entry = await js.InvokeAsync<JsonElement?>("idbCache.get", key);
        if (entry is null || entry.Value.ValueKind == JsonValueKind.Null)
            return null;

        var data = entry.Value.GetProperty("data").Deserialize<T>();
        if (data is null)
            return null;

        var cachedAt = entry.Value.GetProperty("cachedAt").GetDateTime();
        var lastSyncAt = entry.Value.GetProperty("lastSyncAt").GetDateTime();
        return new CacheEntry<T>(data, cachedAt, lastSyncAt);
    }

    public async Task SetAsync<T>(string key, T data, DateTime? syncAt = null)
    {
        var now = DateTime.UtcNow;
        await js.InvokeVoidAsync("idbCache.set", key, data, now, syncAt ?? now);
    }

    public async Task<DateTime?> GetLastSyncAtAsync(string key)
    {
        var entry = await js.InvokeAsync<JsonElement?>("idbCache.get", key);
        if (entry is null || entry.Value.ValueKind == JsonValueKind.Null)
            return null;

        return entry.Value.GetProperty("lastSyncAt").GetDateTime();
    }

    public async Task UpdateSyncMetadataAsync(string key, DateTime syncAt)
    {
        await js.InvokeVoidAsync("idbCache.updateMeta", key, syncAt);
    }

    public async Task InvalidateAsync(string key)
    {
        await js.InvokeVoidAsync("idbCache.remove", key);
    }

    public async Task<bool> HasCacheAsync(string key)
    {
        return await js.InvokeAsync<bool>("idbCache.has", key);
    }
}
