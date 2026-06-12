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
    private readonly HttpClient _fallbackApiClient;

    private const string ManagerCredentialStorageKey = "public_tournament_manager_credentials";
    private const string LocalFunctionsBaseUrl = "http://localhost:7071";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public PublicTournamentService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _fallbackApiClient = new HttpClient
        {
            BaseAddress = new Uri(LocalFunctionsBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<List<PublicTournament>> GetAllPublicTournaments()
    {
        return await GetJsonWithFallback<List<PublicTournament>>("/api/publictournament/getall") ?? [];
    }

    public async Task<PublicTournament> GetPublicTournament(string id)
    {
        return await GetJsonWithFallback<PublicTournament>($"/api/publictournament/get/{id}");
    }

    public async Task<PublicTournament> CreatePublicTournament(PublicTournament tournament)
    {
        return await PostJsonWithFallback<PublicTournament, PublicTournament>("/api/publictournament/create", tournament);
    }

    public async Task<PublicTournamentDayRecord> UpsertDayRecord(PublicTournamentUpsertDayRecordRequest request)
    {
        return await PostJsonWithFallback<PublicTournamentUpsertDayRecordRequest, PublicTournamentDayRecord>("/api/publictournament/dayrecord/upsert", request);
    }

    public async Task<PublicTournamentRegenerateCodesResponse> RegenerateCodes(PublicTournamentRegenerateCodesRequest request)
    {
        return await PostJsonWithFallback<PublicTournamentRegenerateCodesRequest, PublicTournamentRegenerateCodesResponse>("/api/publictournament/codes/regenerate", request);
    }

    public async Task<PublicTournamentBirdIndexSummaryResponse> GetBirdIndexSummary(string tournamentId)
    {
        return await GetJsonWithFallback<PublicTournamentBirdIndexSummaryResponse>($"/api/publictournament/summary/birdindex/{tournamentId}")
               ?? new PublicTournamentBirdIndexSummaryResponse();
    }

    public async Task<List<PublicTournamentTotalsSummaryRow>> GetTotalsSummary(string tournamentId)
    {
        return await GetJsonWithFallback<List<PublicTournamentTotalsSummaryRow>>($"/api/publictournament/summary/totals/{tournamentId}")
               ?? [];
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
