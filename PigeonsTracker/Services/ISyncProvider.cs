namespace PigeonsTracker.Services;

public interface ISyncProvider
{
    string CacheKey { get; }
    Task<bool> SyncAsync(DateTime? lastSyncAt, CancellationToken ct = default);
}
