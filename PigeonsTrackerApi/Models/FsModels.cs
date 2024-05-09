using Google.Cloud.Firestore;

namespace PigeonsTrackerApi.Models;

[FirestoreData]
public class FsTournament
{
    [FirestoreProperty] public string Id { get; set; }
    [FirestoreProperty] public string Name { get; set; }
    [FirestoreProperty] public List<FsPigeonsTrackingRecord> TrackingRecords { get; set; }
    [FirestoreProperty] public DateTime CreatedAt { get; set; }
    [FirestoreProperty] public DateTime StartsFrom { get; set; }
    [FirestoreProperty] public DateTime EndTo { get; set; }
    [FirestoreProperty] public string PosterImage { get; set; }
    [FirestoreProperty] public string Description { get; set; }
    [FirestoreProperty] public bool IsFixedBirds { get; set; }
    [FirestoreProperty] public bool IsApproved { get; set; }
}

[FirestoreData]
public class FsPigeonsTrackingRecord
{
    [FirestoreProperty] public string Id { get; set; }
    [FirestoreProperty] public string RoofName { get; set; }
    [FirestoreProperty] public DateTime StartTime { get; set; }
    [FirestoreProperty] public List<FsPigeonTrackingRecord> Records { get; set; }
    [FirestoreProperty] public DateTime CreatedAt { get; set; }
    [FirestoreProperty] public long? TotalFlyingTime { get; set; }
}

[FirestoreData]
public class FsPigeonTrackingRecord
{
    [FirestoreProperty] public string BirdName { get; set; }
    [FirestoreProperty] public DateTime? EndTime { get; set; }
    [FirestoreProperty] public bool IsCrossed { get; set; }
    [FirestoreProperty] public long? TotalBirdFlyingTime { get; set; }
}

[FirestoreData]
public class FsUserApproved
{
    [FirestoreProperty] public string UserId { get; set; }
}