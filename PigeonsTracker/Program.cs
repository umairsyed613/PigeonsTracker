using AspNetMonsters.Blazor.Geolocation;
using Blazor.Analytics;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PigeonsTracker.Services;

namespace PigeonsTracker;

public class Program
{
    public static async Task Main(string[] args)
    {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddMudServices();

            //Console.WriteLine(@"IsProduction : " + builder.HostEnvironment.IsProduction());
            
            // Configure HttpClient with environment-aware API base:
            // - Local/loopback frontend => use local Functions host (:7071)
            // - Deployed frontend (Azure/static host) => keep same-origin base
            builder.Services.AddScoped(sp =>
            {
                var appBaseUri = new Uri(builder.HostEnvironment.BaseAddress);
                var isLocalHost = appBaseUri.IsLoopback ||
                                  appBaseUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);

                var apiBaseUri = isLocalHost
                    ? new Uri("http://localhost:7071")
                    : appBaseUri;

                var httpClient = new HttpClient
                {
                    BaseAddress = apiBaseUri,
                    Timeout = TimeSpan.FromSeconds(30)
                };

                httpClient.DefaultRequestHeaders.ConnectionClose = false;
                return httpClient;
            });
            
            builder.Services.AddLocalization();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddSingleton<LocationService>();

            builder.Services.AddScoped<WeatherService>();
            builder.Services.AddScoped<IPigeonTrackingService, PigeonTrackingService>();
            builder.Services.AddScoped<IPublicTournamentService, PublicTournamentService>();
            builder.Services.AddScoped<IDiseaseAndCureService, DiseaseAndCureService>();

            builder.Services.AddScoped<ICacheService, IndexedDbCacheService>();
            builder.Services.AddSingleton<ISyncNotificationService, SyncNotificationService>();
            builder.Services.AddSingleton<IBackgroundSyncService, BackgroundSyncService>();
            builder.Services.AddScoped<ISyncProvider, PublicTournamentSyncProvider>();

            builder.Services.AddScoped<SettingsService>();
            builder.Services.AddSingleton<AppState>();

            builder.Services.AddGoogleAnalytics("G-7X8JWPY5G0");

            var host = builder.Build();
            await host.SetDefaultCulture();
            await host.RunAsync();
        }
}