using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using PigeonsTracker.Shared.Models;
using PigeonsTracker.Shared.Requests;

namespace PigeonsTracker.Services;

public class PublicTournamentService : IPublicTournamentService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ICacheService _cacheService;
    private readonly IBackgroundSyncService _backgroundSync;
    private readonly HttpClient _fallbackApiClient;

    private const string ManagerCredentialStorageKey = "public_tournament_manager_credentials";
    private const string LoftAccessCodeStorageKey = "public_tournament_loft_access_codes";
    private const string LocalFunctionsBaseUrl = "http://localhost:7071";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public PublicTournamentService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        ICacheService cacheService,
        IBackgroundSyncService backgroundSync)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _backgroundSync = backgroundSync ?? throw new ArgumentNullException(nameof(backgroundSync));
        _fallbackApiClient = new HttpClient
        {
            BaseAddress = new Uri(LocalFunctionsBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<List<PublicTournament>> GetAllPublicTournaments()
    {
        // Cache-first: return cached data immediately, trigger background delta-check
        if (await _cacheService.HasCacheAsync(CacheKeys.PublicTournaments))
        {
            var cached = await _cacheService.GetAsync<List<PublicTournament>>(CacheKeys.PublicTournaments);
            if (cached?.Data != null)
            {
                _ = _backgroundSync.ForceSyncAsync(CacheKeys.PublicTournaments);
                return cached.Data.OrderByDescending(o => o.CreatedAt).ToList();
            }
        }

        // No cache — fetch fresh and populate cache
        var data = await GetJsonWithFallback<List<PublicTournament>>("/api/publictournament/getall") ?? [];
        await _cacheService.SetAsync(CacheKeys.PublicTournaments, data);
        return data.OrderByDescending(o => o.CreatedAt).ToList();
    }

    public async Task<PublicTournament> GetPublicTournament(string id)
    {
        // 1. Try the list cache (populated by AllTournaments)
        if (await _cacheService.HasCacheAsync(CacheKeys.PublicTournaments))
        {
            var list = await _cacheService.GetAsync<List<PublicTournament>>(CacheKeys.PublicTournaments);
            var found = list?.Data?.FirstOrDefault(t => t.Id == id);
            if (found != null) return found;
        }

        // 2. Try the per-tournament cache (populated on direct load)
        var perTournamentKey = CacheKeys.TournamentById(id);
        if (await _cacheService.HasCacheAsync(perTournamentKey))
        {
            var cached = await _cacheService.GetAsync<PublicTournament>(perTournamentKey);
            if (cached?.Data != null) return cached.Data;
        }

        // 3. Fetch from API and cache individually
        var data = await GetJsonWithFallback<PublicTournament>($"/api/publictournament/get/{id}");
        if (data != null)
            await _cacheService.SetAsync(perTournamentKey, data);
        return data;
    }

    public async Task<PublicTournament> CreatePublicTournament(PublicTournament tournament)
    {
        var result = await PostJsonWithFallback<PublicTournament, PublicTournament>("/api/publictournament/create", tournament);
        if (result != null)
        {
            // Cache the new tournament individually
            await _cacheService.SetAsync(CacheKeys.TournamentById(result.Id), result);

            // Also add to the list cache if it exists
            if (await _cacheService.HasCacheAsync(CacheKeys.PublicTournaments))
            {
                var cached = await _cacheService.GetAsync<List<PublicTournament>>(CacheKeys.PublicTournaments);
                var list = cached?.Data ?? [];
                list.Add(result);
                await _cacheService.SetAsync(CacheKeys.PublicTournaments, list);
            }
        }
        return result;
    }

    public async Task<PublicTournamentDayRecord> UpsertDayRecord(PublicTournamentUpsertDayRecordRequest request)
    {
        var result = await PostJsonWithFallback<PublicTournamentUpsertDayRecordRequest, PublicTournamentDayRecord>("/api/publictournament/dayrecord/upsert", request);
        await _cacheService.InvalidateAsync(CacheKeys.PublicTournaments);
        await _cacheService.InvalidateAsync(CacheKeys.TournamentById(request.TournamentId));
        await _cacheService.InvalidateAsync(CacheKeys.BirdIndexSummary(request.TournamentId));
        await _cacheService.InvalidateAsync(CacheKeys.TotalsSummary(request.TournamentId));
        return result;
    }

    public async Task<PublicTournamentRegenerateCodesResponse> RegenerateCodes(PublicTournamentRegenerateCodesRequest request)
    {
        var result = await PostJsonWithFallback<PublicTournamentRegenerateCodesRequest, PublicTournamentRegenerateCodesResponse>("/api/publictournament/codes/regenerate", request);
        await _cacheService.InvalidateAsync(CacheKeys.PublicTournaments);
        await _cacheService.InvalidateAsync(CacheKeys.TournamentById(request.TournamentId));
        return result;
    }

    public async Task<PublicTournamentBirdIndexSummaryResponse> GetBirdIndexSummary(string tournamentId)
    {
        var cacheKey = CacheKeys.BirdIndexSummary(tournamentId);
        if (await _cacheService.HasCacheAsync(cacheKey))
        {
            var cached = await _cacheService.GetAsync<PublicTournamentBirdIndexSummaryResponse>(cacheKey);
            if (cached?.Data != null)
                return cached.Data;
        }

        var data = await GetJsonWithFallback<PublicTournamentBirdIndexSummaryResponse>($"/api/publictournament/summary/birdindex/{tournamentId}")
                   ?? new PublicTournamentBirdIndexSummaryResponse();
        await _cacheService.SetAsync(cacheKey, data);
        return data;
    }

    public async Task<List<PublicTournamentTotalsSummaryRow>> GetTotalsSummary(string tournamentId)
    {
        var cacheKey = CacheKeys.TotalsSummary(tournamentId);
        if (await _cacheService.HasCacheAsync(cacheKey))
        {
            var cached = await _cacheService.GetAsync<List<PublicTournamentTotalsSummaryRow>>(cacheKey);
            if (cached?.Data != null)
                return cached.Data;
        }

        var data = await GetJsonWithFallback<List<PublicTournamentTotalsSummaryRow>>($"/api/publictournament/summary/totals/{tournamentId}")
                   ?? [];
        await _cacheService.SetAsync(cacheKey, data);
        return data;
    }

    public async Task StoreManagerCredentials(string tournamentId, string managerCode, string recoveryKey)
    {
        var all = await _localStorage.GetItemAsync<Dictionary<string, ManagerCredential>>(ManagerCredentialStorageKey)
                  ?? new Dictionary<string, ManagerCredential>();

        all[tournamentId] = new ManagerCredential
        {
            ManagerCode = managerCode,
            RecoveryKey = recoveryKey
        };

        await _localStorage.SetItemAsync(ManagerCredentialStorageKey, all);
    }

    public async Task<(string managerCode, string recoveryKey)> GetManagerCredentials(string tournamentId)
    {
        var all = await _localStorage.GetItemAsync<Dictionary<string, ManagerCredential>>(ManagerCredentialStorageKey)
                  ?? new Dictionary<string, ManagerCredential>();

        if (!all.TryGetValue(tournamentId, out var credential) || credential == null)
        {
            return (string.Empty, string.Empty);
        }

        return (credential.ManagerCode ?? string.Empty, credential.RecoveryKey ?? string.Empty);
    }

    private class ManagerCredential
    {
        public string ManagerCode { get; set; }
        public string RecoveryKey { get; set; }
    }

    public async Task StoreLoftAccessCode(string tournamentId, string loftCode)
    {
        var all = await _localStorage.GetItemAsync<Dictionary<string, string>>(LoftAccessCodeStorageKey)
                  ?? new Dictionary<string, string>();
        all[tournamentId] = loftCode;
        await _localStorage.SetItemAsync(LoftAccessCodeStorageKey, all);
    }

    public async Task<string> GetLoftAccessCode(string tournamentId)
    {
        var all = await _localStorage.GetItemAsync<Dictionary<string, string>>(LoftAccessCodeStorageKey)
                  ?? new Dictionary<string, string>();
        return all.TryGetValue(tournamentId, out var code) ? code ?? string.Empty : string.Empty;
    }

    private async Task<T> GetJsonWithFallback<T>(string path)
    {
        var primaryResponse = await _httpClient.GetAsync(path);
        if (await IsLikelyJson(primaryResponse))
        {
            return await DeserializeOrThrow<T>(primaryResponse, path);
        }

        var fallbackResponse = await _fallbackApiClient.GetAsync(path);
        if (await IsLikelyJson(fallbackResponse))
        {
            return await DeserializeOrThrow<T>(fallbackResponse, path);
        }

        throw await BuildJsonError(path, primaryResponse, fallbackResponse);
    }

    private async Task<TResponse> PostJsonWithFallback<TRequest, TResponse>(string path, TRequest payload)
    {
        var primaryResponse = await _httpClient.PostAsJsonAsync(path, payload);
        if (await IsLikelyJson(primaryResponse))
        {
            return await DeserializeOrThrow<TResponse>(primaryResponse, path);
        }

        var fallbackResponse = await _fallbackApiClient.PostAsJsonAsync(path, payload);
        if (await IsLikelyJson(fallbackResponse))
        {
            return await DeserializeOrThrow<TResponse>(fallbackResponse, path);
        }

        throw await BuildJsonError(path, primaryResponse, fallbackResponse);
    }

    private static async Task<T> DeserializeOrThrow<T>(HttpResponseMessage response, string path)
    {
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync();
        var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
        if (data == null)
        {
            throw new InvalidOperationException($"Empty JSON response received for '{path}'.");
        }

        return data;
    }

    private static async Task<bool> IsLikelyJson(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrWhiteSpace(contentType) && contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var preview = await PreviewContent(response);
        return !preview.StartsWith("<", StringComparison.Ordinal);
    }

    private static async Task<Exception> BuildJsonError(string path, HttpResponseMessage primary, HttpResponseMessage fallback)
    {
        var primaryPreview = await PreviewContent(primary);
        var fallbackPreview = await PreviewContent(fallback);

        return new InvalidOperationException(
            $"PublicTournament API call failed for '{path}'. " +
            $"Primary={primary.StatusCode}, Preview='{primaryPreview}'. " +
            $"Fallback(http://localhost:7071)={fallback.StatusCode}, Preview='{fallbackPreview}'. " +
            "Make sure Azure Functions API is running.");
    }

    private static async Task<string> PreviewContent(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = text.Trim();
        return text.Length <= 80 ? text : text.Substring(0, 80);
    }
}
