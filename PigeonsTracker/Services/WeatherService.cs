using System.Net.Http.Json;
using Blazored.LocalStorage;
using PigeonsTracker.Shared.Models;

namespace PigeonsTracker.Services;

public sealed class WeatherService
{
    private readonly HttpClient _client;

    public DateTime LastUpdatedAt { get; set; }

    private const string StorageKey = "0aab67a5b2164d939ae74d75955f1336";
    private ILocalStorageService LocalStorage { get; init; }

    public WeatherService(HttpClient client, ILocalStorageService localStorage)
    {
            LocalStorage = localStorage;
            _client = client;
        }

    public async Task<OpenWeatherApiResult> GetWeatherResult(string lat, string lng, bool force)
    {
            if (_client == null)
            {
                return null;
            }

            var lastRead = await GetLastRead();

            if (force || !lastRead.HasValue || DateTime.Now.Subtract(lastRead.Value).Minutes > 30)
            {
                try
                {
                    lastRead = DateTime.Now;
                    LastUpdatedAt = lastRead.Value;

                    Console.WriteLine($@"Reading weather data. {LastUpdatedAt}");

                    // _client.BaseAddress = new Uri("http://localhost:7071/");

                    var temp = await _client.GetFromJsonAsync<OpenWeatherApiResult>($"/api/WeatherFunction?lat={lat}&lng={lng}");

                    await SetLastRead(lastRead.Value);

                    return temp;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return null;
                }
            }

            return null;
        }

    private async Task<DateTime?> GetLastRead()
    {
            if (!await LocalStorage.ContainKeyAsync(StorageKey))
            {
                return null;
            }

            return await LocalStorage.GetItemAsync<DateTime>(StorageKey);
        }

    private async Task SetLastRead(DateTime dateTime)
    {
            await LocalStorage.SetItemAsync(StorageKey, dateTime);
        }
}