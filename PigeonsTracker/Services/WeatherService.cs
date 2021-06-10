using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using PigeonsTracker.Shared.Models;

namespace PigeonsTracker.Services
{
    public sealed class WeatherService
    {
        private readonly HttpClient _client;

        public DateTime LastUpdatedAt { get; set; }

        private OpenWeatherApiResult _cachedOpenWeatherApiResult;

        private static readonly string _storageKey = "0aab67a5b2164d939ae74d75955f1336";
        private ILocalStorageService LocalStorage { get; init; }

        public WeatherService(HttpClient client, ILocalStorageService localStorage)
        {
            LocalStorage = localStorage;
            _client = client;
        }

        /*private void InitHttpClient()
        {
            if (!string.IsNullOrEmpty(_apiKey)) { _client = new HttpClient() { BaseAddress = new Uri(_apiBaseAddr) }; }
        }*/


        public async Task<OpenWeatherApiResult> GetWeatherResult(string lat, string lng)
        {
            if (_client == null) { return null; }

            var lastRead = await GetLastRead();

            if (!lastRead.HasValue || DateTime.Now.Subtract(lastRead.Value).Minutes > 30)
            {
                try
                {
                    lastRead = DateTime.Now;
                    LastUpdatedAt = lastRead.Value;
                    Console.WriteLine($@"Reading weather data. {LastUpdatedAt}");

                    /*_client.BaseAddress = new Uri("http://localhost:7071/");*/
                    _cachedOpenWeatherApiResult = await _client.GetFromJsonAsync<OpenWeatherApiResult>($"/api/WeatherFunc?lat={lat}&lng={lng}");

                    /*if (resp.IsSuccessStatusCode)
                    {
                        _cachedOpenWeatherApiResult = await resp.Content.ReadFromJsonAsync<OpenWeatherApiResult>();
                    }*/

                    await SetLastRead(lastRead.Value);
                }
                catch (Exception e) { Console.WriteLine(e); }
            }

            return _cachedOpenWeatherApiResult;
        }

        private async Task<DateTime?> GetLastRead()
        {
            if (!await LocalStorage.ContainKeyAsync(_storageKey)) { return null; }

            return await LocalStorage.GetItemAsync<DateTime>(_storageKey);
        }

        private async Task SetLastRead(DateTime dateTime)
        {
            await LocalStorage.SetItemAsync(_storageKey, dateTime);
        }
    }
}
