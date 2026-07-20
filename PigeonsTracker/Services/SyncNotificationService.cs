using PigeonsTracker.DataModels;

namespace PigeonsTracker.Services;

public class SyncNotificationService : ISyncNotificationService
{
    private readonly Dictionary<string, CacheSyncState> _states = new();
    private readonly object _lock = new();

    public event Action<string>? OnDataUpdated;
    public event Action<string, Exception>? OnSyncFailed;

    public CacheSyncState GetState(string cacheKey)
    {
        lock (_lock)
        {
            return _states.TryGetValue(cacheKey, out var state)
                ? state
                : new CacheSyncState(SyncStatus.Idle, null, false);
        }
    }

    public void UpdateState(string cacheKey, CacheSyncState state)
    {
        lock (_lock)
        {
            _states[cacheKey] = state;
        }

        if (state.HasUnseenUpdates)
        {
            OnDataUpdated?.Invoke(cacheKey);
        }

        if (state.LastError != null && state.Status == SyncStatus.Failed)
        {
            OnSyncFailed?.Invoke(cacheKey, state.LastError);
        }
    }
}
