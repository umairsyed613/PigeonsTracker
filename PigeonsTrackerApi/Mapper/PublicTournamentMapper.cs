using PigeonsTracker.Shared.Models;
using PigeonsTrackerApi.Models;

namespace PigeonsTrackerApi.Mapper;

public static class PublicTournamentMapper
{
    public static FsPublicTournament Map(this PublicTournament tournament)
    {
        return new FsPublicTournament
        {
            Id = tournament.Id,
            Name = tournament.Name,
            CreatedAt = DateTime.SpecifyKind(tournament.CreatedAt, DateTimeKind.Local).ToUniversalTime(),
            StartsFrom = DateTime.SpecifyKind(tournament.StartsFrom, DateTimeKind.Local).ToUniversalTime(),
            EndTo = DateTime.SpecifyKind(tournament.EndTo, DateTimeKind.Local).ToUniversalTime(),
            FlyingStartTimeTicks = tournament.FlyingStartTime.Ticks,
            CanManageLoftRecords = tournament.CanManageLoftRecords,
            ManagerCode = tournament.ManagerCode,
            ManagerRecoveryKey = tournament.ManagerRecoveryKey,
            CodeVersion = tournament.CodeVersion,
            LastCodeRegeneratedAt = tournament.LastCodeRegeneratedAt.HasValue
                ? DateTime.SpecifyKind(tournament.LastCodeRegeneratedAt.Value, DateTimeKind.Local).ToUniversalTime()
                : null,
            Lofts = tournament.Lofts.Select(l => new FsPublicTournamentLoft
            {
                LoftId = l.LoftId,
                LoftName = l.LoftName,
                BirdCount = l.BirdCount,
                HasBabyPigeon = l.HasBabyPigeon,
                LoftCode = l.LoftCode,
                CreatedAt = DateTime.SpecifyKind(l.CreatedAt, DateTimeKind.Local).ToUniversalTime()
            }).ToList(),
            DayRecords = tournament.DayRecords.Select(d => new FsPublicTournamentDayRecord
            {
                Id = d.Id,
                Date = DateTime.SpecifyKind(d.Date, DateTimeKind.Local).ToUniversalTime(),
                CreatedAt = DateTime.SpecifyKind(d.CreatedAt, DateTimeKind.Local).ToUniversalTime(),
                LoftRecords = d.LoftRecords.Select(lr => new FsPublicTournamentLoftDayRecord
                {
                    LoftId = lr.LoftId,
                    LoftName = lr.LoftName,
                    StartTime = DateTime.SpecifyKind(lr.StartTime, DateTimeKind.Local).ToUniversalTime(),
                    BirdRecords = lr.BirdRecords.Select(b => new FsPublicTournamentBirdRecord
                    {
                        BirdIndex = b.BirdIndex,
                        EndTime = b.EndTime.HasValue
                            ? DateTime.SpecifyKind(b.EndTime.Value, DateTimeKind.Local).ToUniversalTime()
                            : null,
                        TotalBirdFlyingTimeTicks = b.TotalBirdFlyingTime?.Ticks
                    }).ToList(),
                    BabyBird = lr.BabyBird is null
                        ? null
                        : new FsPublicTournamentBirdRecord
                        {
                            BirdIndex = lr.BabyBird.BirdIndex,
                            EndTime = lr.BabyBird.EndTime.HasValue
                                ? DateTime.SpecifyKind(lr.BabyBird.EndTime.Value, DateTimeKind.Local).ToUniversalTime()
                                : null,
                            TotalBirdFlyingTimeTicks = lr.BabyBird.TotalBirdFlyingTime?.Ticks
                        },
                    TotalHoursTicks = lr.TotalHours?.Ticks,
                    TotalLanded = lr.TotalLanded,
                    TotalNotLanded = lr.TotalNotLanded,
                    BabyPigeonSumTicks = lr.BabyPigeonSum?.Ticks,
                    UpdatedAt = lr.UpdatedAt.HasValue
                        ? DateTime.SpecifyKind(lr.UpdatedAt.Value, DateTimeKind.Local).ToUniversalTime()
                        : null
                }).ToList()
            }).ToList()
        };
    }

    public static PublicTournament Map(this FsPublicTournament tournament, string firestoreId)
    {
        return new PublicTournament
        {
            FireStoreId = firestoreId,
            Id = tournament.Id,
            Name = tournament.Name,
            CreatedAt = DateTime.SpecifyKind(tournament.CreatedAt, DateTimeKind.Utc).ToLocalTime(),
            StartsFrom = DateTime.SpecifyKind(tournament.StartsFrom, DateTimeKind.Utc).ToLocalTime(),
            EndTo = DateTime.SpecifyKind(tournament.EndTo, DateTimeKind.Utc).ToLocalTime(),
            FlyingStartTime = TimeSpan.FromTicks(tournament.FlyingStartTimeTicks),
            CanManageLoftRecords = tournament.CanManageLoftRecords,
            ManagerCode = tournament.ManagerCode,
            ManagerRecoveryKey = tournament.ManagerRecoveryKey,
            CodeVersion = tournament.CodeVersion,
            LastCodeRegeneratedAt = tournament.LastCodeRegeneratedAt.HasValue
                ? DateTime.SpecifyKind(tournament.LastCodeRegeneratedAt.Value, DateTimeKind.Utc).ToLocalTime()
                : null,
            Lofts = tournament.Lofts.Select(l => new PublicTournamentLoft
            {
                LoftId = l.LoftId,
                LoftName = l.LoftName,
                BirdCount = l.BirdCount,
                HasBabyPigeon = l.HasBabyPigeon,
                LoftCode = l.LoftCode,
                CreatedAt = DateTime.SpecifyKind(l.CreatedAt, DateTimeKind.Utc).ToLocalTime()
            }).ToList(),
            DayRecords = tournament.DayRecords.Select(d => new PublicTournamentDayRecord
            {
                Id = d.Id,
                Date = DateTime.SpecifyKind(d.Date, DateTimeKind.Utc).ToLocalTime(),
                CreatedAt = DateTime.SpecifyKind(d.CreatedAt, DateTimeKind.Utc).ToLocalTime(),
                LoftRecords = d.LoftRecords.Select(lr => new PublicTournamentLoftDayRecord
                {
                    LoftId = lr.LoftId,
                    LoftName = lr.LoftName,
                    StartTime = DateTime.SpecifyKind(lr.StartTime, DateTimeKind.Utc).ToLocalTime(),
                    BirdRecords = lr.BirdRecords.Select(b => new PublicTournamentBirdRecord
                    {
                        BirdIndex = b.BirdIndex,
                        EndTime = b.EndTime.HasValue
                            ? DateTime.SpecifyKind(b.EndTime.Value, DateTimeKind.Utc).ToLocalTime()
                            : null,
                        TotalBirdFlyingTime = b.TotalBirdFlyingTimeTicks.HasValue
                            ? TimeSpan.FromTicks(b.TotalBirdFlyingTimeTicks.Value)
                            : null
                    }).ToList(),
                    BabyBird = lr.BabyBird is null
                        ? null
                        : new PublicTournamentBirdRecord
                        {
                            BirdIndex = lr.BabyBird.BirdIndex,
                            EndTime = lr.BabyBird.EndTime.HasValue
                                ? DateTime.SpecifyKind(lr.BabyBird.EndTime.Value, DateTimeKind.Utc).ToLocalTime()
                                : null,
                            TotalBirdFlyingTime = lr.BabyBird.TotalBirdFlyingTimeTicks.HasValue
                                ? TimeSpan.FromTicks(lr.BabyBird.TotalBirdFlyingTimeTicks.Value)
                                : null
                        },
                    TotalHours = lr.TotalHoursTicks.HasValue ? TimeSpan.FromTicks(lr.TotalHoursTicks.Value) : null,
                    TotalLanded = lr.TotalLanded,
                    TotalNotLanded = lr.TotalNotLanded,
                    BabyPigeonSum = lr.BabyPigeonSumTicks.HasValue ? TimeSpan.FromTicks(lr.BabyPigeonSumTicks.Value) : null,
                    UpdatedAt = lr.UpdatedAt.HasValue
                        ? DateTime.SpecifyKind(lr.UpdatedAt.Value, DateTimeKind.Utc).ToLocalTime()
                        : null
                }).ToList()
            }).ToList()
        };
    }
}
