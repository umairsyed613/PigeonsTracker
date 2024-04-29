using System.Net.Http.Json;
using PigeonsTracker.Shared.Models;
using PigeonsTracker.Shared.Requests;

namespace PigeonsTrackerApi.Services;

// A new OpenWeatherMap service
public interface IOpenWeatherMapService
{
    Task<OpenWeatherApiResult> GetWeatherDataAsync(WeatherRequest weatherRequest);
}

public class OpenWeatherMapService : IOpenWeatherMapService
{
    private readonly HttpClient _weatherHttpClient;

    public OpenWeatherMapService(HttpClient httpClient)
    {
        _weatherHttpClient = httpClient;
    }

    public async Task<OpenWeatherApiResult> GetWeatherDataAsync(WeatherRequest weatherRequest)
    {
        // Your existing code to fetch weather data goes here

        var apiKey = Environment.GetEnvironmentVariable("weatherapikey") ?? throw new ArgumentNullException("Api key is missing");
        var baseAddr = "https://api.openweathermap.org";
        var requestUrl = $"/data/2.5/weather?units=metric&lat={weatherRequest.Latitude}&lon={weatherRequest.Longitude}&appid={apiKey}";

        _weatherHttpClient.BaseAddress = new Uri(baseAddr);
        var data = await _weatherHttpClient.GetFromJsonAsync<OpenWeatherApiResult>(requestUrl);

        return data;
    }
}