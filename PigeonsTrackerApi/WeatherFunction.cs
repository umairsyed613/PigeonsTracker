using System.Net.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PigeonsTracker.Shared.Models;
using PigeonsTracker.Shared.Requests;
using PigeonsTrackerApi.Services;

namespace PigeonsTrackerApi;

public class WeatherFunction
{
    private readonly ILogger<WeatherFunction> _logger;
    private readonly IOpenWeatherMapService _openWeatherMapService;

    public WeatherFunction(ILogger<WeatherFunction> logger, IOpenWeatherMapService openWeatherMapService)
    {
        _logger = logger;
        _openWeatherMapService = openWeatherMapService;
    }

    [Function("WeatherFunction")]
    public async Task<OpenWeatherApiResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("Running Weather Function");

        var weatherReq = new WeatherRequest() { Latitude = req.Query["lat"], Longitude = req.Query["lng"] };

        if (string.IsNullOrEmpty(weatherReq.Latitude) || string.IsNullOrEmpty(weatherReq.Longitude))
        {
            return null;
        }

        var data = await _openWeatherMapService.GetWeatherDataAsync(weatherReq);

        return data;
    }
}
/*
public class WeatherFunction
{
    private readonly ILogger<WeatherFunction> _logger;

    // api.openweathermap.org/data/2.5/weather?lat=59.8246951&lon=10.8161435&appid=
    private const string ApiBaseAddr = "https://api.openweathermap.org";
    private const string Request = "/data/2.5/weather?units=metric&lat={0}&lon={1}&appid={2}";

    private static readonly string _apiKey = Environment.GetEnvironmentVariable("weatherapikey") ?? throw new ArgumentNullException(nameof(_apiKey));
    private static HttpClient _weatherHttpClient;

    public WeatherFunction(ILogger<WeatherFunction> logger)
    {
        _logger = logger;
    }

    [Function("WeatherFunction")]
    public async Task<OpenWeatherApiResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")]
        HttpRequestData req)
    {
        _logger.LogInformation("Running Weather Function");

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogInformation("Weather Function Api key is missing");
            return null;
        }

        var weatherReq = new WeatherRequest() { Latitude = req.Query["lat"], Longitude = req.Query["lng"] };
        //await JsonSerializer.DeserializeAsync<WeatherRequest>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, });

        if (string.IsNullOrEmpty(weatherReq.Latitude) || string.IsNullOrEmpty(weatherReq.Longitude)) { return null; }

        _weatherHttpClient ??= new HttpClient() { BaseAddress = new Uri(ApiBaseAddr) };

        var data = await _weatherHttpClient.GetFromJsonAsync<OpenWeatherApiResult>(
            string.Format(Request, weatherReq.Latitude, weatherReq.Longitude, _apiKey));

        return data;
    }
}*/