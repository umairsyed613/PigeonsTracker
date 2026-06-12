using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PigeonsTrackerApi.Models;
using PigeonsTrackerApi.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddScoped<IOpenWeatherMapService, OpenWeatherMapService>();

        var firebaseSecret = Encoding.UTF8.GetString(Convert.FromBase64String(Environment.GetEnvironmentVariable("firebaseSecret")!));
        var unescapedJsonString = JsonConvert.DeserializeObject<string>(firebaseSecret);
        var parsed = JObject.Parse(unescapedJsonString);
        var projectId = parsed.Property("project_id")?.Value.ToString();
        using var credentialStream = new MemoryStream(Encoding.UTF8.GetBytes(unescapedJsonString));
        var serviceAccountCredential = ServiceAccountCredential.FromServiceAccountData(credentialStream);

        var fdb = new FirestoreDbBuilder()
        {
            ProjectId = projectId,
            GoogleCredential = GoogleCredential.FromServiceAccountCredential(serviceAccountCredential)
        }.Build();

        services.AddSingleton(fdb);
        AddFirestoreService<FsTournament>(services, "Tournaments");
        AddFirestoreService<FsPublicTournament>(services, "PublicTournaments");
        AddFirestoreService<FsUserApproved>(services, "ApprovedUsers");
        AddFirestoreService<FsPigeonDiseaseAndCure>(services, "PigeonsDiseaseAndCure");
        services.AddHttpClient();
    })
    .Build();

await host.RunAsync();


static void AddFirestoreService<T>(IServiceCollection services, string collectionPath) where T : class
{
    services.AddScoped<IFireStoreService<T>>(sp =>
    {
        var firestoreDb = sp.GetRequiredService<FirestoreDb>();
        return new FireStoreService<T>(firestoreDb, collectionPath);
    });
}