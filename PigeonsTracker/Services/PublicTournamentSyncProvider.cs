using System.Net.Http.Json;
using System.Text.Json;
using PigeonsTracker.Shared.Models;

namespace PigeonsTracker.Services;

public class PublicTournamentSyncProvider : ISyncProvider
{
    private readonly HttpClient _httpClient;
    private readonly ICacheService _cacheService;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string CacheKey => CacheKeys.PublicTournaments;

    public PublicTournamentSyncProvider(HttpClient httpClient, ICacheService cacheService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<bool> SyncAsync(DateTime? lastSyncAt, CancellationToken ct = default)
    {
        // Step 1: Cheap check — does anything need updating? (1 Firestore read via Limit(1))
        if (lastSyncAt.HasValue)
        {
            var sinceParam = lastSyncAt.Value.ToUniversalTime().ToString("O");
            try
            {
                var checkResponse = await _httpClient.GetAsync(
                    $"/api/publictournament/haschanges?since={Uri.EscapeDataString(sinceParam)}", ct);

                if (checkResponse.IsSuccessStatusCode)
                {
                    var result = await checkResponse.Content.ReadFromJsonAsync<HasChangesResponse>(JsonOptions, ct);
                    if (result?.HasChanges == false) return false;
                }
            }
            catch
            {
                // If the check fails, skip sync rather than cascade errors
                return false;
            }
        }

        // Step 2: Fetch delta (or full list if no lastSyncAt)
        var since = lastSyncAt?.ToUniversalTime().ToString("O");
        var url = since != null
            ? $"/api/publictournament/updates?since={Uri.EscapeDataString(since)}"
            : "/api/publictournament/getall";

        List<PublicTournament>? updates;
        try
        {
            updates = await _httpClient.GetFromJsonAsync<List<PublicTournament>>(url, JsonOptions, ct);
        }
        catch
        {
            return false;
        }

        if (updates is null or { Count: 0 }) return false;

        // Step 3: Merge into cache (upsert by Id)
        var cached = await _cacheService.GetAsync<List<PublicTournament>>(CacheKeys.PublicTournaments);
        var list = cached?.Data ?? [];

        foreach (var updated in updates)
        {
            var idx = list.FindIndex(t => t.Id == updated.Id);
            if (idx >= 0)
                list[idx] = updated;
            else
                list.Add(updated);
        }

        await _cacheService.SetAsync(CacheKeys.PublicTournaments, list, DateTime.UtcNow);
        return true;
    }

    private sealed record HasChangesResponse(bool HasChanges);
}
