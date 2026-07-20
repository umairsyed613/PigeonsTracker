namespace PigeonsTracker.Services;

public interface IBackgroundSyncService
{
    Task StartAsync();
    Task ForceSyncAsync(string? cacheKey = null);
}
