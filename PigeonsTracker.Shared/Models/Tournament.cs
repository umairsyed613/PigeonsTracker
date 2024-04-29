using System;
using System.Collections.Generic;

namespace PigeonsTracker.Shared.Models;

public class Tournament
{
    public string? FireStoreId { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public List<PigeonsTrackingRecord> TrackingRecords { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime StartsFrom { get; set; }
    public DateTime EndTo { get; set; }

    public string PosterImage { get; set; }

    public string Description { get; set; }

    public bool IsFixedBirds { get; set; }

    public bool IsApproved { get; set; }
}

public class PigeonsTrackingRecord
{
    public string Id { get; set; }

    public string RoofName { get; set; }
    public DateTime StartTime { get; set; }
    public List<PigeonTrackingRecord> Records { get; set; }
    public DateTime CreatedAt { get; set; }

    public TimeSpan? TotalFlyingTime { get; set; }
}

public class PigeonTrackingRecord
{
    public string BirdName { get; set; }
    public DateTime? EndTime { get; set; }

    public bool IsCrossed { get; set; }
    public TimeSpan? TotalBirdFlyingTime { get; set; }
}