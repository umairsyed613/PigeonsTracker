using System;
using System.Collections.Generic;

namespace PigeonsTracker.Shared.Models;

public class PublicTournament
{
    public string FireStoreId { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime StartsFrom { get; set; }
    public DateTime EndTo { get; set; }
    public TimeSpan FlyingStartTime { get; set; }
    public bool CanManageLoftRecords { get; set; }

    public string ManagerCode { get; set; } = string.Empty;
    public string ManagerRecoveryKey { get; set; } = string.Empty;

    public int CodeVersion { get; set; } = 1;
    public DateTime? LastCodeRegeneratedAt { get; set; }

    public List<PublicTournamentLoft> Lofts { get; set; } = [];
    public List<PublicTournamentDayRecord> DayRecords { get; set; } = [];
}

public class PublicTournamentLoft
{
    public string LoftId { get; set; } = string.Empty;
    public string LoftName { get; set; } = string.Empty;
    public int BirdCount { get; set; }
    public bool HasBabyPigeon { get; set; }
    public string LoftCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PublicTournamentDayRecord
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PublicTournamentLoftDayRecord> LoftRecords { get; set; } = [];
}

public class PublicTournamentLoftDayRecord
{
    public string LoftId { get; set; } = string.Empty;
    public string LoftName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public List<PublicTournamentBirdRecord> BirdRecords { get; set; } = [];
    public PublicTournamentBirdRecord BabyBird { get; set; }
    public TimeSpan? TotalHours { get; set; }
    public int TotalLanded { get; set; }
    public int TotalNotLanded { get; set; }
    public TimeSpan? BabyPigeonSum { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PublicTournamentBirdRecord
{
    public int BirdIndex { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? TotalBirdFlyingTime { get; set; }
}

public class PublicTournamentBirdIndexSummaryRow
{
    public string LoftName { get; set; } = string.Empty;
    public DateTime DateOfFlying { get; set; }
    public List<long?> BirdHoursTicks { get; set; } = [];
    public long TotalSumOfDayTicks { get; set; }
}

public class PublicTournamentBirdIndexSummaryResponse
{
    public int MaxBirdCount { get; set; }
    public List<PublicTournamentBirdIndexSummaryRow> Rows { get; set; } = [];
}

public class PublicTournamentTotalsSummaryRow
{
    public string LoftName { get; set; } = string.Empty;
    public DateTime DateOfFlying { get; set; }
    public int TotalLanded { get; set; }
    public int TotalNotLanded { get; set; }
    public long BabyPigeonSumTicks { get; set; }
    public long TotalHoursTicks { get; set; }
}
