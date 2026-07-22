using System.Net.Http.Json;
using System.Text.Json;
using PigeonsTracker.Shared.Models;

namespace PigeonsTracker.Services;

public class PublicTournamentSyncProvider : ISyncProvider
{
    private readonly HttpClient _httpClient;
    private readonly ICacheService _cacheService;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan FullSyncInterval = TimeSpan.FromHours(6);

    public string CacheKey => CacheKeys.PublicTournaments;

    public PublicTournamentSyncProvider(HttpClient httpClient, ICacheService cacheService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<bool> SyncAsync(DateTime? lastSyncAt, CancellationToken ct = default)
    {
        var cached = await _cacheService.GetAsync<List<PublicTournament>>(CacheKeys.PublicTournaments);
        var previous = cached?.Data ?? [];

        var isFullSyncDue = cached == null || DateTime.UtcNow - cached.CachedAt >= FullSyncInterval;

        // Step 1: Cheap check — skip full fetch when no changes and full sync is not due
        if (lastSyncAt.HasValue && !isFullSyncDue)
        {
            var sinceParam = lastSyncAt.Value.ToUniversalTime().ToString("O");
            try
            {
                var checkResponse = await _httpClient.GetAsync(
                    $"/api/publictournament/haschanges?since={Uri.EscapeDataString(sinceParam)}", ct);

                if (checkResponse.IsSuccessStatusCode)
                {
                    var result = await checkResponse.Content.ReadFromJsonAsync<HasChangesResponse>(JsonOptions, ct);
                    if (result?.HasChanges == false)
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // Step 2: Fetch authoritative latest list
        // NOTE: We intentionally use getall here to ensure deleted tournaments are removed from cache.
        List<PublicTournament> latest;
        try
        {
            latest = await _httpClient.GetFromJsonAsync<List<PublicTournament>>("/api/publictournament/getall", JsonOptions, ct) ?? [];
        }
        catch
        {
            return false;
        }

        // Step 3: Reconcile against cached list and remove stale per-tournament caches for deleted items

        var latestIds = latest.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        var removedIds = previous
            .Where(w => !string.IsNullOrWhiteSpace(w.Id) && !latestIds.Contains(w.Id))
            .Select(s => s.Id)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var removedId in removedIds)
        {
            await _cacheService.InvalidateAsync(CacheKeys.TournamentById(removedId));
            await _cacheService.InvalidateAsync(CacheKeys.BirdIndexSummary(removedId));
            await _cacheService.InvalidateAsync(CacheKeys.TotalsSummary(removedId));
        }

        var hasChanges = !HaveSameCacheFingerprint(previous, latest);

        await _cacheService.SetAsync(CacheKeys.PublicTournaments, latest, DateTime.UtcNow);
        return hasChanges;
    }

    private static bool HaveSameCacheFingerprint(IReadOnlyCollection<PublicTournament> left, IReadOnlyCollection<PublicTournament> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        var rightById = right
            .Where(w => !string.IsNullOrWhiteSpace(w.Id))
            .ToDictionary(k => k.Id, v => v, StringComparer.Ordinal);

        foreach (var leftItem in left)
        {
            if (string.IsNullOrWhiteSpace(leftItem.Id))
            {
                return false;
            }

            if (!rightById.TryGetValue(leftItem.Id, out var rightItem))
            {
                return false;
            }

            if (leftItem.UpdatedAt != rightItem.UpdatedAt ||
                leftItem.CreatedAt != rightItem.CreatedAt ||
                leftItem.CodeVersion != rightItem.CodeVersion)
            {
                return false;
            }
        }

        return true;
    }

    private sealed record HasChangesResponse(bool HasChanges);
}
