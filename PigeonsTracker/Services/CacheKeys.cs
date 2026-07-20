namespace PigeonsTracker.Services;

public static class CacheKeys
{
    public const string PublicTournaments = "cache_public_tournaments_v1";

    public static string BirdIndexSummary(string tournamentId) => $"cache_public_tournament_bird_summary_{tournamentId}_v1";
    public static string TotalsSummary(string tournamentId) => $"cache_public_tournament_totals_summary_{tournamentId}_v1";
}
