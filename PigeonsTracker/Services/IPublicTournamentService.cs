using PigeonsTracker.Shared.Models;
using PigeonsTracker.Shared.Requests;

namespace PigeonsTracker.Services;

public interface IPublicTournamentService
{
    Task<List<PublicTournament>> GetAllPublicTournaments();
    Task<PublicTournament> GetPublicTournament(string id);
    Task<PublicTournament> CreatePublicTournament(PublicTournament tournament);
    Task<PublicTournamentDayRecord> UpsertDayRecord(PublicTournamentUpsertDayRecordRequest request);
    Task<PublicTournamentRegenerateCodesResponse> RegenerateCodes(PublicTournamentRegenerateCodesRequest request);
    Task<PublicTournamentBirdIndexSummaryResponse> GetBirdIndexSummary(string tournamentId);
    Task<List<PublicTournamentTotalsSummaryRow>> GetTotalsSummary(string tournamentId);

    Task StoreManagerCredentials(string tournamentId, string managerCode, string recoveryKey);
    Task<(string managerCode, string recoveryKey)> GetManagerCredentials(string tournamentId);
}
