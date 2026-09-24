namespace PigeonsTracker.DataModels;

public record CacheEntry<T>(T Data, DateTime CachedAt, DateTime LastSyncAt);

public enum SyncStatus
{
    Idle,
    Syncing,
    Success,
    Failed
}

public record CacheSyncState(SyncStatus Status, DateTime? LastSyncAt, bool HasUnseenUpdates, Exception? LastError = null);
