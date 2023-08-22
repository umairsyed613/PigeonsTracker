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

        private static readonly string _storageKey = "0aab67a5b2164d939ae74d75955f1336";
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
                lastRead = DateTime.Now;
                LastUpdatedAt = lastRead.Value;
                Console.WriteLine($@"Reading weather data. {LastUpdatedAt}");

                /*_client.BaseAddress = new Uri("http://localhost:7071/");*/
                var temp = await _client.GetFromJsonAsync<OpenWeatherApiResult>(
                    $"/api/WeatherFunc?lat={lat}&lng={lng}");
                await SetLastRead(lastRead.Value);

                return temp;
            }

            return null;
        }

        private async Task<DateTime?> GetLastRead()
        {
            if (!await LocalStorage.ContainKeyAsync(_storageKey))
            {
                return null;
            }

            return await LocalStorage.GetItemAsync<DateTime>(_storageKey);
        }

        private async Task SetLastRead(DateTime dateTime)
        {
            await LocalStorage.SetItemAsync(_storageKey, dateTime);
        }
    }
}