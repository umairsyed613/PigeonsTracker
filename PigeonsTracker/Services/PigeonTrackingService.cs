using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using PigeonsTracker.DataModels;

namespace PigeonsTracker.Services
{
    public class PigeonTrackingService : IPigeonTrackingService
    {
        private static readonly string _tournamentKey = "92C792370CFF4357BB6FE81EFD9C356D";
        private ILocalStorageService LocalStorage { get; init; }

        public PigeonTrackingService(ILocalStorageService service) => LocalStorage = service ?? throw new ArgumentNullException(nameof(LocalStorage));

        public async Task UpsertTournament(Tournament tournament)
        {
            var allTour = await GetAllTournaments();

            if (allTour?.Count > 0)
            {
                var f = allTour.FirstOrDefault(a => a.Id == tournament.Id);

                if (f != null) { allTour.Remove(f); }

                allTour.Add(tournament);
                await UpsertAllTournamentLists(allTour);

                return;
            }

            await UpsertAllTournamentLists(new List<Tournament> { tournament });
        }

        public async Task<List<Tournament>> GetAllTournaments()
        {
            if (await LocalStorage.ContainKeyAsync(_tournamentKey)) { return await LocalStorage.GetItemAsync<List<Tournament>>(_tournamentKey); }

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

        private async Task UpsertAllTournamentLists(List<Tournament> data)
        {
            if (await LocalStorage.ContainKeyAsync(_tournamentKey)) { await LocalStorage.RemoveItemAsync(_tournamentKey); }

            await LocalStorage.SetItemAsync(_tournamentKey, data);
        }
    }
}
