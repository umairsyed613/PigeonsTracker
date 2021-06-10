using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PigeonsTracker.DataModels
{
    public enum TournamentType
    {
        Tournament = 0,
        Practice = 1
    }

    public class Tournament
    {
        [Required] public string Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public TournamentType TournamentType { get; set; }
        public List<PigeonsTrackingRecord> TrackingRecords { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime StartsFrom { get; set; }
        public DateTime EndTo { get; set; }
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

        public TimeSpan? TotalBirdFlyingTime { get; set; }
    }
}
