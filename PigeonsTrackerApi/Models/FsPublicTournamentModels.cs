using Google.Cloud.Firestore;

namespace PigeonsTrackerApi.Models;

[FirestoreData]
public class FsPublicTournament
{
    [FirestoreProperty] public string Id { get; set; } = string.Empty;
    [FirestoreProperty] public string Name { get; set; } = string.Empty;
    [FirestoreProperty] public DateTime CreatedAt { get; set; }
    [FirestoreProperty] public DateTime StartsFrom { get; set; }
    [FirestoreProperty] public DateTime EndTo { get; set; }
    [FirestoreProperty] public long FlyingStartTimeTicks { get; set; }
    [FirestoreProperty] public bool CanManageLoftRecords { get; set; }

    [FirestoreProperty] public string ManagerCode { get; set; } = string.Empty;
    [FirestoreProperty] public string ManagerRecoveryKey { get; set; } = string.Empty;

    [FirestoreProperty] public int CodeVersion { get; set; }
    [FirestoreProperty] public DateTime? LastCodeRegeneratedAt { get; set; }

    [FirestoreProperty] public List<FsPublicTournamentLoft> Lofts { get; set; } = [];
    [FirestoreProperty] public List<FsPublicTournamentDayRecord> DayRecords { get; set; } = [];
}

[FirestoreData]
public class FsPublicTournamentLoft
{
    [FirestoreProperty] public string LoftId { get; set; } = string.Empty;
    [FirestoreProperty] public string LoftName { get; set; } = string.Empty;
    [FirestoreProperty] public int BirdCount { get; set; }
    [FirestoreProperty] public bool HasBabyPigeon { get; set; }
    [FirestoreProperty] public string LoftCode { get; set; } = string.Empty;
    [FirestoreProperty] public DateTime CreatedAt { get; set; }
}

[FirestoreData]
public class FsPublicTournamentDayRecord
{
    [FirestoreProperty] public string Id { get; set; } = string.Empty;
    [FirestoreProperty] public DateTime Date { get; set; }
    [FirestoreProperty] public DateTime CreatedAt { get; set; }
    [FirestoreProperty] public List<FsPublicTournamentLoftDayRecord> LoftRecords { get; set; } = [];
}

[FirestoreData]
public class FsPublicTournamentLoftDayRecord
{
    [FirestoreProperty] public string LoftId { get; set; } = string.Empty;
    [FirestoreProperty] public string LoftName { get; set; } = string.Empty;
    [FirestoreProperty] public DateTime StartTime { get; set; }
    [FirestoreProperty] public List<FsPublicTournamentBirdRecord> BirdRecords { get; set; } = [];
    [FirestoreProperty] public FsPublicTournamentBirdRecord BabyBird { get; set; }
    [FirestoreProperty] public long? TotalHoursTicks { get; set; }
    [FirestoreProperty] public int TotalLanded { get; set; }
    [FirestoreProperty] public int TotalNotLanded { get; set; }
    [FirestoreProperty] public long? BabyPigeonSumTicks { get; set; }
    [FirestoreProperty] public DateTime? UpdatedAt { get; set; }
}

[FirestoreData]
public class FsPublicTournamentBirdRecord
{
    [FirestoreProperty] public int BirdIndex { get; set; }
    [FirestoreProperty] public DateTime? EndTime { get; set; }
    [FirestoreProperty] public long? TotalBirdFlyingTimeTicks { get; set; }
}
