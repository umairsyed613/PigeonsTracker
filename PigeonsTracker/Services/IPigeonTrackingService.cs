using PigeonsTracker.DataModels;
using PigeonsTracker.Shared.Models;

namespace PigeonsTracker.Services;

public interface IPigeonTrackingService
{
    public Task UpsertTournament(Tournament tournament);
    public Task<List<Tournament>> GetAllTournaments();
    public Task<Tournament> GetTournament(string id);
    public Task DeleteTournament(string id);

    public Task UpsertTracking(PigeonsTrackingRecord pigeonsTrackingRecord);
    public Task<List<PigeonsTrackingRecord>> GetAllTracking();
    public Task<PigeonsTrackingRecord> GetTracking(string id);
    public Task DeleteTracking(string id);
}