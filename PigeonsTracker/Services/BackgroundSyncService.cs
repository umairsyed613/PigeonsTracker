using Microsoft.Extensions.DependencyInjection;
using PigeonsTracker.DataModels;

namespace PigeonsTracker.Services;

public class BackgroundSyncService : IBackgroundSyncService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISyncNotificationService _notificationService;
    private CancellationTokenSource? _cts;
    private bool _started;

    public BackgroundSyncService(IServiceScopeFactory scopeFactory, ISyncNotificationService notificationService)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public Task StartAsync()
    {
        if (_started) return Task.CompletedTask;
        _started = true;
        _cts = new CancellationTokenSource();
        _ = RunPeriodicSyncAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public Task ForceSyncAsync(string? cacheKey = null)
    {
        return RunSyncCycleAsync(cacheKey);
    }

    private async Task RunPeriodicSyncAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(SyncInterval);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(ct);
                await RunSyncCycleAsync(null, ct);
            }
            catch (OperationCanceledException) { break; }
            catch { /* absorb unexpected errors to keep the timer alive */ }
        }
    }

    private async Task RunSyncCycleAsync(string? cacheKey = null, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var providers = scope.ServiceProvider.GetRequiredService<IEnumerable<ISyncProvider>>();

        foreach (var provider in providers)
        {
            if (cacheKey != null && provider.CacheKey != cacheKey) continue;

            var current = _notificationService.GetState(provider.CacheKey);
            _notificationService.UpdateState(provider.CacheKey,
                new CacheSyncState(SyncStatus.Syncing, current.LastSyncAt, false));

            try
            {
                var lastSyncAt = await cacheService.GetLastSyncAtAsync(provider.CacheKey);
                var hasUpdates = await provider.SyncAsync(lastSyncAt, ct);

                _notificationService.UpdateState(provider.CacheKey,
                    new CacheSyncState(SyncStatus.Success, DateTime.UtcNow, hasUpdates));
            }
            catch (Exception ex)
            {
                var state = _notificationService.GetState(provider.CacheKey);
                _notificationService.UpdateState(provider.CacheKey,
                    new CacheSyncState(SyncStatus.Failed, state.LastSyncAt, false, ex));
            }
        }
    }
}
