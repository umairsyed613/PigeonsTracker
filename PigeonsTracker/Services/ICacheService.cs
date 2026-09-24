using PigeonsTracker.DataModels;

namespace PigeonsTracker.Services;

public interface ICacheService
{
    Task<CacheEntry<T>?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T data, DateTime? syncAt = null);
    Task<DateTime?> GetLastSyncAtAsync(string key);
    Task UpdateSyncMetadataAsync(string key, DateTime syncAt);
    Task InvalidateAsync(string key);
    Task<bool> HasCacheAsync(string key);
}
