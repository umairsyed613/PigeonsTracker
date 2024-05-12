using Blazored.LocalStorage;
using PigeonsTracker.Shared.Models;

namespace PigeonsTracker.Services;

public class PigeonTrackingService : IPigeonTrackingService
{
    private const string TournamentKey = "92C792370CFF4357BB6FE81EFD9C356D";
    private const string TrackingKey = "0AB25660C5BF4CACBFBBB82F73A51D9A";

    private ILocalStorageService LocalStorage { get; init; }

    public PigeonTrackingService(ILocalStorageService localStorageService)
    {
        LocalStorage = localStorageService ?? throw new ArgumentNullException(nameof(localStorageService));
    }

    public async Task UpsertTournament(Tournament tournament)
    {
        /*using var client = new HttpClient();
        client.BaseAddress = new Uri("http://localhost:7071/");*/

        var allTour = await GetAllTournaments();

        if (allTour?.Count > 0)
        {
            var f = allTour.FirstOrDefault(a => a.Id == tournament.Id);

            // var update = false;
            if (f != null)
            {
                allTour.Remove(f);
                // update = true;
            }


            /*if (update)
            {
                var resp = await client.PostAsJsonAsync("api/data/update", tournament);
            }
            else
            {
                var respAdd1 = await client.PostAsJsonAsync("api/data/create", tournament);
                if (respAdd1.IsSuccessStatusCode)
                {
                    var content = await respAdd1.Content.ReadAsStreamAsync();
                    var fs = await JsonSerializer.DeserializeAsync<FireStoreUpsertResponse>(content);
                    Console.Write(fs);
                    tournament.FireStoreId = fs.Id;
                }
            }*/

            allTour.Add(tournament);
            await UpsertAllTournamentLists(allTour);
            return;
        }

        /*var respAdd = await client.PostAsJsonAsync("api/data/create", tournament);
        if (respAdd.IsSuccessStatusCode)
        {
            var content = await respAdd.Content.ReadAsStreamAsync();
            var fs = await JsonSerializer.DeserializeAsync<FireStoreUpsertResponse>(content);
            Console.Write(fs);
            tournament.FireStoreId = fs.Id;
        }*/

        await UpsertAllTournamentLists([tournament]);
    }

    public async Task<List<Tournament>> GetAllTournaments()
    {
        if (await LocalStorage.ContainKeyAsync(TournamentKey))
        {
            return await LocalStorage.GetItemAsync<List<Tournament>>(TournamentKey);
        }

        return null;
    }

    public async Task<Tournament> GetTournament(string id)
    {
        var allTour = await GetAllTournaments();

        return allTour?.Count > 0 ? allTour.FirstOrDefault(a => a.Id == id) : null;
    }

    public async Task DeleteTournament(string id)
    {
        var allTour = await GetAllTournaments();

        if (allTour?.Count > 0)
        {
            var f = allTour.FirstOrDefault(a => a.Id == id);

            if (f != null)
            {
                allTour.Remove(f);
                await UpsertAllTournamentLists(allTour);
            }
        }
    }

    public async Task UpsertTracking(PigeonsTrackingRecord pigeonsTrackingRecord)
    {
        var allTour = await GetAllTracking();

        if (allTour?.Count > 0)
        {
            var f = allTour.FirstOrDefault(a => a.Id == pigeonsTrackingRecord.Id);

            if (f != null)
            {
                allTour.Remove(f);
            }

            allTour.Add(pigeonsTrackingRecord);
            await UpsertAllTrackingList(allTour);
            return;
        }

        await UpsertAllTrackingList([pigeonsTrackingRecord]);
    }

    public async Task<List<PigeonsTrackingRecord>> GetAllTracking()
    {
        if (await LocalStorage.ContainKeyAsync(TrackingKey))
        {
            return await LocalStorage.GetItemAsync<List<PigeonsTrackingRecord>>(TrackingKey);
        }

        return null;
    }

    public async Task<PigeonsTrackingRecord> GetTracking(string id)
    {
        var allTour = await GetAllTracking();

        return allTour?.Count > 0 ? allTour.FirstOrDefault(a => a.Id == id) : null;
    }

    public async Task DeleteTracking(string id)
    {
        var allTour = await GetAllTracking();

        if (allTour?.Count > 0)
        {
            var f = allTour.FirstOrDefault(a => a.Id == id);

            if (f != null)
            {
                allTour.Remove(f);
                await UpsertAllTrackingList(allTour);
            }
        }
    }

    private async Task UpsertAllTrackingList(List<PigeonsTrackingRecord> data)
    {
        if (await LocalStorage.ContainKeyAsync(TrackingKey))
        {
            await LocalStorage.RemoveItemAsync(TrackingKey);
        }

        await LocalStorage.SetItemAsync(TrackingKey, data);
    }

    private async Task UpsertAllTournamentLists(List<Tournament> data)
    {
        if (await LocalStorage.ContainKeyAsync(TournamentKey))
        {
            await LocalStorage.RemoveItemAsync(TournamentKey);
        }

        await LocalStorage.SetItemAsync(TournamentKey, data);
    }
}