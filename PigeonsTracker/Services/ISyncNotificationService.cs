using PigeonsTracker.DataModels;

namespace PigeonsTracker.Services;

public interface ISyncNotificationService
{
    event Action<string>? OnDataUpdated;
    event Action<string, Exception>? OnSyncFailed;
    CacheSyncState GetState(string cacheKey);
    void UpdateState(string cacheKey, CacheSyncState state);
}
