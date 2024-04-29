using AspNetMonsters.Blazor.Geolocation;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PigeonsTracker.Services;

namespace PigeonsTracker;

public class Program
{
    public static async Task Main(string[] args)
    {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            //Console.WriteLine(@"IsProduction : " + builder.HostEnvironment.IsProduction());
            builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddLocalization();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddSingleton<LocationService>();

            builder.Services.AddScoped<WeatherService>();
            builder.Services.AddScoped<IPigeonTrackingService, PigeonTrackingService>();

            builder.Services.AddSingleton<AppState>();

            var host = builder.Build();
            await host.SetDefaultCulture();
            await host.RunAsync();
        }
}