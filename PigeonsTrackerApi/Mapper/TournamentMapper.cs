using PigeonsTracker.Shared.Models;
using PigeonsTrackerApi.Models;

namespace PigeonsTrackerApi.Mapper;

public static class TournamentMapper
{
    public static FsTournament Map(this Tournament tournament)
    {
        return new FsTournament
        {
            Id = tournament.Id,
            Name = tournament.Name,
            TrackingRecords = tournament.TrackingRecords?.Select(tr => new FsPigeonsTrackingRecord
            {
                Id = tr.Id,
                RoofName = tr.RoofName,
                StartTime =  DateTime.SpecifyKind(tr.StartTime, DateTimeKind.Local).ToUniversalTime(),
                Records = tr.Records?.Select(r => new FsPigeonTrackingRecord
                {
                    BirdName = r.BirdName,
                    EndTime = r.EndTime.HasValue ? DateTime.SpecifyKind(r.EndTime.Value, DateTimeKind.Local).ToUniversalTime() : null,
                    IsCrossed = r.IsCrossed,
                    TotalBirdFlyingTime = r.TotalBirdFlyingTime?.Ticks
                }).ToList(),
                CreatedAt = DateTime.SpecifyKind(tr.CreatedAt, DateTimeKind.Local).ToUniversalTime(),
                TotalFlyingTime = tr.TotalFlyingTime?.Ticks
            }).ToList(),
            CreatedAt = DateTime.SpecifyKind(tournament.CreatedAt, DateTimeKind.Local).ToUniversalTime(),
            StartsFrom = DateTime.SpecifyKind(tournament.StartsFrom, DateTimeKind.Local).ToUniversalTime(),
            EndTo = DateTime.SpecifyKind(tournament.EndTo, DateTimeKind.Local).ToUniversalTime(),
            PosterImage = tournament.PosterImage,
            Description = tournament.Description,
            IsFixedBirds = tournament.IsFixedBirds,
            IsApproved = tournament.IsApproved
        };
    }

    public static Tournament Map(this FsTournament tournament, string fireStoreId)
    {
        return new Tournament
        {
            FireStoreId = fireStoreId,
            Id = tournament.Id,
            Name = tournament.Name,
            TrackingRecords = tournament.TrackingRecords?.Select(tr => new PigeonsTrackingRecord
            {
                Id = tr.Id,
                RoofName = tr.RoofName,
                StartTime =  DateTime.SpecifyKind(tr.StartTime, DateTimeKind.Utc).ToLocalTime(),
                Records = tr.Records?.Select(r => new PigeonTrackingRecord
                {
                    BirdName = r.BirdName,
                    EndTime = r.EndTime.HasValue ? DateTime.SpecifyKind(r.EndTime.Value, DateTimeKind.Utc).ToLocalTime() : null,
                    IsCrossed = r.IsCrossed,
                    TotalBirdFlyingTime = r.TotalBirdFlyingTime.HasValue ? TimeSpan.FromTicks(r.TotalBirdFlyingTime.Value) : null,
                }).ToList(),
                CreatedAt = DateTime.SpecifyKind(tr.CreatedAt, DateTimeKind.Utc).ToLocalTime(),
                TotalFlyingTime = tr.TotalFlyingTime.HasValue ? TimeSpan.FromTicks(tr.TotalFlyingTime.Value) : null,
            }).ToList(),
            CreatedAt = DateTime.SpecifyKind(tournament.CreatedAt, DateTimeKind.Utc).ToLocalTime(),
            StartsFrom = DateTime.SpecifyKind(tournament.StartsFrom, DateTimeKind.Utc).ToLocalTime(),
            EndTo = DateTime.SpecifyKind(tournament.EndTo, DateTimeKind.Utc).ToLocalTime(),
            PosterImage = tournament.PosterImage,
            Description = tournament.Description,
            IsFixedBirds = tournament.IsFixedBirds,
            IsApproved = tournament.IsApproved
        };
    }
}