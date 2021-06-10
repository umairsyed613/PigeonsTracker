using System.Collections.Generic;
using System.Threading.Tasks;
using PigeonsTracker.DataModels;

namespace PigeonsTracker.Services
{
    public interface IPigeonTrackingService
    {
        public Task UpsertTournament(Tournament tournament);

        public Task<List<Tournament>> GetAllTournaments();
        public Task<Tournament> GetTournament(string id);

        public Task DeleteTournament(string id);
    }
}
