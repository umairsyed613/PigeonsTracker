#nullable enable
using System.Net.Http.Json;
using Blazored.LocalStorage;
using PigeonsTracker.DataModels;

namespace PigeonsTracker.Services;

/// <summary>
/// Service for managing disease and cure data with local storage caching and version-based updates.
/// Designed for PWA support with offline-first capability.
/// </summary>
public interface IDiseaseAndCureService
{
    /// <summary>
    /// Gets all diseases. Returns cached data immediately and refreshes in background if needed.
    /// </summary>
    Task<List<DiseaseItem>> GetAllDiseasesAsync();

    /// <summary>
    /// Gets a specific disease by ID.
    /// </summary>
    Task<DiseaseItem?> GetDiseaseByIdAsync(string id);

    /// <summary>
    /// Forces a refresh of the data from the remote source.
    /// </summary>
    Task<bool> RefreshDataAsync();

    /// <summary>
    /// Checks if data is available in memory cache (synchronous check).
    /// </summary>
    bool HasCachedData { get; }

    /// <summary>
    /// Gets cached diseases synchronously. Returns empty list if not cached.
    /// </summary>
    List<DiseaseItem> GetCachedDiseases();

    /// <summary>
    /// Event triggered when data has been updated in the background.
    /// </summary>
    event Action? OnDataUpdated;
}

public class DiseaseAndCureService : IDiseaseAndCureService
{
    private const string CacheKey = "DiseaseAndCureData_v1";
    private const string VersionKey = "DiseaseAndCureVersion_v1";
    
    // Remote URL - can be changed to GitHub raw URL for production
    // Example: "https://raw.githubusercontent.com/umairsyed613/PigeonsTracker/main/data/diseases-data.json"
    private const string LocalDataUrl = "https://raw.githubusercontent.com/umairsyed613/PigeonsTracker/main/data/diseases-data.json";
    
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    
    // Static in-memory cache for faster access and persistence across service instances
    private static DiseaseAndCureData? _memoryCache;
    private static bool _isRefreshing;

    public event Action? OnDataUpdated;

    /// <summary>
    /// Checks if data is available in memory cache.
    /// </summary>
    public bool HasCachedData => _memoryCache?.Diseases.Count > 0;

    /// <summary>
    /// Gets cached diseases synchronously. Returns empty list if not cached.
    /// </summary>
    public List<DiseaseItem> GetCachedDiseases() => _memoryCache?.Diseases ?? new List<DiseaseItem>();

    public DiseaseAndCureService(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<DiseaseItem>> GetAllDiseasesAsync()
    {
        // Try memory cache first
        if (_memoryCache?.Diseases.Count > 0)
        {
            // Trigger background refresh check
            _ = CheckAndRefreshInBackgroundAsync();
            return _memoryCache.Diseases;
        }

        // Try local storage
        var cachedData = await GetFromLocalStorageAsync();
        if (cachedData?.Diseases.Count > 0)
        {
            _memoryCache = cachedData;
            // Trigger background refresh check
            _ = CheckAndRefreshInBackgroundAsync();
            return cachedData.Diseases;
        }

        // Fetch from remote/local source
        var freshData = await FetchDataAsync();
        if (freshData != null)
        {
            _memoryCache = freshData;
            await SaveToLocalStorageAsync(freshData);
            return freshData.Diseases;
        }

        return new List<DiseaseItem>();
    }

    public async Task<DiseaseItem?> GetDiseaseByIdAsync(string id)
    {
        var diseases = await GetAllDiseasesAsync();
        return diseases.FirstOrDefault(d => d.Id == id);
    }

    public async Task<bool> RefreshDataAsync()
    {
        try
        {
            var freshData = await FetchDataAsync();
            if (freshData != null)
            {
                _memoryCache = freshData;
                await SaveToLocalStorageAsync(freshData);
                OnDataUpdated?.Invoke();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing disease data: {ex.Message}");
        }

        return false;
    }

    private async Task CheckAndRefreshInBackgroundAsync()
    {
        // Prevent multiple simultaneous refresh attempts
        if (_isRefreshing) return;

        try
        {
            _isRefreshing = true;

            var remoteData = await FetchDataAsync();
            if (remoteData == null) return;

            var cachedVersion = await GetCachedVersionAsync();
            
            // Check if remote version is newer
            if (string.IsNullOrEmpty(cachedVersion) || IsNewerVersion(remoteData.Version, cachedVersion))
            {
                Console.WriteLine($"Updating disease data from version {cachedVersion} to {remoteData.Version}");
                
                _memoryCache = remoteData;
                await SaveToLocalStorageAsync(remoteData);
                
                // Notify subscribers that data has been updated
                OnDataUpdated?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Background refresh failed: {ex.Message}");
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private async Task<DiseaseAndCureData?> FetchDataAsync()
    {
        try
        {
            var data = await _httpClient.GetFromJsonAsync<DiseaseAndCureData>(LocalDataUrl);
            return data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching disease data: {ex.Message}");
            return null;
        }
    }

    private async Task<DiseaseAndCureData?> GetFromLocalStorageAsync()
    {
        try
        {
            if (await _localStorage.ContainKeyAsync(CacheKey))
            {
                return await _localStorage.GetItemAsync<DiseaseAndCureData>(CacheKey);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading from local storage: {ex.Message}");
        }

        return null;
    }

    private async Task SaveToLocalStorageAsync(DiseaseAndCureData data)
    {
        try
        {
            await _localStorage.SetItemAsync(CacheKey, data);
            await _localStorage.SetItemAsync(VersionKey, data.Version);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving to local storage: {ex.Message}");
        }
    }

    private async Task<string?> GetCachedVersionAsync()
    {
        try
        {
            if (await _localStorage.ContainKeyAsync(VersionKey))
            {
                return await _localStorage.GetItemAsync<string>(VersionKey);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading version from local storage: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Compares two semantic version strings to determine if the new version is greater.
    /// Supports versions like "1.0.0", "1.0", "1"
    /// </summary>
    private static bool IsNewerVersion(string newVersion, string cachedVersion)
    {
        if (string.IsNullOrEmpty(newVersion)) return false;
        if (string.IsNullOrEmpty(cachedVersion)) return true;

        try
        {
            var newParts = newVersion.Split('.').Select(int.Parse).ToArray();
            var cachedParts = cachedVersion.Split('.').Select(int.Parse).ToArray();

            var maxLength = Math.Max(newParts.Length, cachedParts.Length);

            for (int i = 0; i < maxLength; i++)
            {
                var newPart = i < newParts.Length ? newParts[i] : 0;
                var cachedPart = i < cachedParts.Length ? cachedParts[i] : 0;

                if (newPart > cachedPart) return true;
                if (newPart < cachedPart) return false;
            }

            return false; // Versions are equal
        }
        catch
        {
            // If parsing fails, do a simple string comparison
            return !string.Equals(newVersion, cachedVersion, StringComparison.OrdinalIgnoreCase);
        }
    }
}

/// <summary>
/// Helper class for formatting disease cure content for display.
/// </summary>
public static class DiseaseContentFormatter
{
    /// <summary>
    /// Truncates text to a specified length for preview display.
    /// </summary>
    public static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // Remove markdown formatting for preview
        var cleanText = text.Replace("**", "").Replace("\n", " ").Replace("\r", "");

        if (cleanText.Length <= maxLength) return cleanText;

        return cleanText.Substring(0, maxLength).TrimEnd() + "...";
    }

    /// <summary>
    /// Parses cure content into formatted paragraphs for display.
    /// </summary>
    public static List<ContentParagraph> GetFormattedContent(string content)
    {
        var paragraphs = new List<ContentParagraph>();

        if (string.IsNullOrEmpty(content)) return paragraphs;

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

            // Check if it's a heading (bold text with **)
            if (trimmedLine.StartsWith("**") && trimmedLine.EndsWith("**"))
            {
                paragraphs.Add(new ContentParagraph
                {
                    Text = trimmedLine.Replace("**", "").TrimEnd(':'),
                    IsHeading = true
                });
            }
            // Check if it's a bullet point
            else if (trimmedLine.StartsWith("- "))
            {
                paragraphs.Add(new ContentParagraph
                {
                    Text = trimmedLine.Substring(2),
                    IsBullet = true
                });
            }
            else
            {
                paragraphs.Add(new ContentParagraph
                {
                    Text = trimmedLine.Replace("**", "")
                });
            }
        }

        return paragraphs;
    }
}
