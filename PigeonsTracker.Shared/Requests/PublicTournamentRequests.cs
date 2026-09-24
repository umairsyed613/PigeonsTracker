using System;
using System.Collections.Generic;
using PigeonsTracker.Shared.Models;

namespace PigeonsTracker.Shared.Requests;

public class PublicTournamentUpsertDayRecordRequest
{
    public string TournamentId { get; set; } = string.Empty;
    public string LoftId { get; set; } = string.Empty;
    public DateTime DateOfFlying { get; set; }
    public string AccessCode { get; set; } = string.Empty;
    public PublicTournamentLoftDayRecord LoftRecord { get; set; } = new();
}

public class PublicTournamentRegenerateCodesRequest
{
    public string TournamentId { get; set; } = string.Empty;
    public string ManagerAccessCodeOrRecovery { get; set; } = string.Empty;
    public bool RegenerateManagerCode { get; set; } = true;
    public bool RegenerateRecoveryKey { get; set; }
    public bool RegenerateAllLoftCodes { get; set; } = true;
}

public class PublicTournamentRegenerateCodesResponse
{
    public string TournamentId { get; set; } = string.Empty;
    public int CodeVersion { get; set; }
    public DateTime RegeneratedAt { get; set; }
    public string ManagerCode { get; set; } = string.Empty;
    public string ManagerRecoveryKey { get; set; } = string.Empty;
    public Dictionary<string, string> LoftCodesByLoftId { get; set; } = new();
}
