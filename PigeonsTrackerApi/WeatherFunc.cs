using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PigeonsTracker.Shared.Models;
using PigeonsTracker.Shared.Requests;
using Newtonsoft.Json;

namespace PigeonsTracker
{
    public static class WeatherFunc
    {
        // api.openweathermap.org/data/2.5/weather?lat=59.8246951&lon=10.8161435&appid=
        private const string _apiBaseAddr = "https://api.openweathermap.org";
        private const string _request = "/data/2.5/weather?units=metric&lat={0}&lon={1}&appid={2}";

        private static string _apiKey = Environment.GetEnvironmentVariable("weatherapikey") ?? string.Empty;
        private static HttpClient _weatherHttpClient;

        [FunctionName("WeatherFunc")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")]
            HttpRequest req, ILogger log)
        {
            log.LogInformation("Running Weather Function");

            if (string.IsNullOrEmpty(_apiKey))
            {
                log.LogInformation("Weather Function Api key is missing");
                return new OkObjectResult(null);
            }

            var weatherReq = new WeatherRequest() { Latitude = req.Query["lat"], Longitude = req.Query["lng"] };
                //await JsonSerializer.DeserializeAsync<WeatherRequest>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, });

            if (string.IsNullOrEmpty(weatherReq.Latitude) || string.IsNullOrEmpty(weatherReq.Longitude)) { return new BadRequestResult(); }

            _weatherHttpClient ??= new HttpClient() { BaseAddress = new Uri(_apiBaseAddr) };

            var data = await _weatherHttpClient.GetStringAsync(
                string.Format(_request, weatherReq.Latitude, weatherReq.Longitude, _apiKey));

            /*if (!data.IsSuccessStatusCode) { return new OkObjectResult(null); }

            var response = await data.Content.ReadAsStreamAsync();*/

            var weatherResp = JsonConvert.DeserializeObject<OpenWeatherApiResult>(data);

            return new OkObjectResult(weatherResp);
        }
    }
}
