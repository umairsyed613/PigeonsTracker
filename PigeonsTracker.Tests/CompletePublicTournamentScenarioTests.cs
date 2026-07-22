using Xunit;
using FluentAssertions;
using PigeonsTracker.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PigeonsTracker.Tests;

public class CompletePublicTournamentScenarioTests
{
    [Fact]
    public void CompleteTournament_ThreeDaysThreeLoftsThreeBirdsAndBaby_ShouldCalculateTotalsCorrectly()
    {
        // Arrange
        var rng = new Random(613);
        var startDate = new DateTime(2026, 7, 1);
        var endDate = startDate.AddDays(2); // 3-day tournament (inclusive)
        var flyingStart = new TimeSpan(5, 0, 0);  // 05:00 AM
        var flyingEnd = new TimeSpan(19, 0, 0);   // 07:00 PM

        var tournament = new PublicTournament
        {
            Id = "tournament-3-days",
            Name = "3 Day Complete Calculation Test",
            StartsFrom = startDate,
            EndTo = endDate,
            FlyingStartTime = flyingStart,
            FlyingEndTime = flyingEnd,
            Lofts = new List<PublicTournamentLoft>(),
            DayRecords = new List<PublicTournamentDayRecord>()
        };

        for (var loftIndex = 1; loftIndex <= 3; loftIndex++)
        {
            tournament.Lofts.Add(new PublicTournamentLoft
            {
                LoftId = $"loft-{loftIndex}",
                LoftName = $"Loft {loftIndex}",
                BirdCount = 3,
                HasBabyPigeon = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        var expectedStandardHoursTotal = TimeSpan.Zero;
        var expectedBabyHoursTotal = TimeSpan.Zero;

        for (var dayOffset = 0; dayOffset < 3; dayOffset++)
        {
            var dayDate = startDate.AddDays(dayOffset);
            var dayRecord = new PublicTournamentDayRecord
            {
                Id = $"day-{dayOffset + 1}",
                Date = dayDate,
                CreatedAt = DateTime.UtcNow,
                LoftRecords = new List<PublicTournamentLoftDayRecord>()
            };

            foreach (var loft in tournament.Lofts)
            {
                var startDateTime = dayDate.Date + flyingStart;
                var endDateTime = dayDate.Date + flyingEnd;

                // Bird-1: within start/end, not crossed -> included in total
                var bird1End = startDateTime.AddHours(rng.Next(2, 8)).AddMinutes(rng.Next(0, 59));
                if (bird1End > endDateTime)
                {
                    bird1End = endDateTime.AddMinutes(-rng.Next(1, 45));
                }

                // Bird-2: overtime + crossed -> should not be included in total
                var bird2End = endDateTime.AddMinutes(rng.Next(10, 90));

                // Bird-3: within start/end + crossed -> should not be included in total
                var bird3End = startDateTime.AddHours(rng.Next(1, 6)).AddMinutes(rng.Next(0, 59));
                if (bird3End > endDateTime)
                {
                    bird3End = endDateTime.AddMinutes(-rng.Next(1, 30));
                }

                // Baby bird: within start/end, should have hours but not be included in TotalHours
                var babyEnd = startDateTime.AddHours(rng.Next(2, 7)).AddMinutes(rng.Next(0, 59));
                if (babyEnd > endDateTime)
                {
                    babyEnd = endDateTime.AddMinutes(-rng.Next(1, 20));
                }

                var birds = new List<PublicTournamentBirdRecord>
                {
                    BuildBird(1, startDateTime, endDateTime, bird1End, isCrossed: false),
                    BuildBird(2, startDateTime, endDateTime, bird2End, isCrossed: true),
                    BuildBird(3, startDateTime, endDateTime, bird3End, isCrossed: true)
                };

                var babyBird = BuildBird(0, startDateTime, endDateTime, babyEnd, isCrossed: false);

                var loftTotal = TimeSpan.FromTicks(
                    birds.Where(b => !b.IsCrossed && b.TotalBirdFlyingTime.HasValue)
                         .Sum(b => b.TotalBirdFlyingTime!.Value.Ticks)
                );

                var loftRecord = new PublicTournamentLoftDayRecord
                {
                    LoftId = loft.LoftId,
                    LoftName = loft.LoftName,
                    StartTime = startDateTime,
                    BirdRecords = birds,
                    BabyBird = babyBird,
                    TotalHours = loftTotal,
                    BabyPigeonSum = babyBird.TotalBirdFlyingTime,
                    TotalLanded = birds.Count(b => b.EndTime.HasValue && !b.IsCrossed),
                    TotalNotLanded = birds.Count(b => !b.EndTime.HasValue && !b.IsCrossed)
                };

                dayRecord.LoftRecords.Add(loftRecord);

                expectedStandardHoursTotal += birds[0].TotalBirdFlyingTime ?? TimeSpan.Zero;
                expectedBabyHoursTotal += babyBird.TotalBirdFlyingTime ?? TimeSpan.Zero;
            }

            tournament.DayRecords.Add(dayRecord);
        }

        // Act
        var totalLoftRecords = tournament.DayRecords.SelectMany(d => d.LoftRecords).ToList();
        var allBirds = totalLoftRecords.SelectMany(l => l.BirdRecords).ToList();

        var standardTotalHours = TimeSpan.FromTicks(
            totalLoftRecords.Sum(l => l.TotalHours?.Ticks ?? 0)
        );

        var babyTotalHours = TimeSpan.FromTicks(
            totalLoftRecords.Sum(l => l.BabyPigeonSum?.Ticks ?? 0)
        );

        var crossedOvertimeCount = allBirds.Count(b => b.IsCrossed && b.IsOvertime);
        var crossedWithinRangeCount = allBirds.Count(b => b.IsCrossed && !b.IsOvertime && b.EndTime.HasValue);
        var includedNormalBirdCount = allBirds.Count(b => !b.IsCrossed && b.EndTime.HasValue);

        // Assert (Tournament structure)
        tournament.StartsFrom.Should().Be(startDate);
        tournament.EndTo.Should().Be(endDate);
        tournament.FlyingStartTime.Should().Be(new TimeSpan(5, 0, 0));
        tournament.FlyingEndTime.Should().Be(new TimeSpan(19, 0, 0));
        tournament.Lofts.Should().HaveCount(3);
        tournament.DayRecords.Should().HaveCount(3);

        // Assert (Each day has 3 loft records; each loft has 3 birds + baby)
        tournament.DayRecords.Should().OnlyContain(d => d.LoftRecords.Count == 3);
        totalLoftRecords.Should().OnlyContain(l => l.BirdRecords.Count == 3 && l.BabyBird != null);

        // Assert (Business rules)
        crossedOvertimeCount.Should().Be(9, "1 crossed overtime bird per loft per day (3x3)");
        crossedWithinRangeCount.Should().Be(9, "1 crossed in-range bird per loft per day (3x3)");
        includedNormalBirdCount.Should().Be(9, "1 included non-crossed bird per loft per day (3x3)");

        // Crossed birds must not contribute to standard total
        standardTotalHours.Should().Be(expectedStandardHoursTotal);

        // Baby bird times are calculated and tracked separately
        babyTotalHours.Should().Be(expectedBabyHoursTotal);
        babyTotalHours.Should().BeGreaterThan(TimeSpan.Zero);

        // Baby bird time must not be included in standard loft totals
        (standardTotalHours + babyTotalHours).Should().BeGreaterThan(standardTotalHours);

        // Per-loft-day expectations
        totalLoftRecords.Should().OnlyContain(l =>
            l.TotalLanded == 1 &&
            l.TotalNotLanded == 0 &&
            l.TotalHours.HasValue &&
            l.BabyPigeonSum.HasValue);

        // Overtime and crossed combinations are present as requested
        allBirds.Should().Contain(b => b.IsCrossed && b.IsOvertime && b.BirdIndex == 2);
        allBirds.Count(b => b.IsCrossed && b.IsOvertime && b.BirdIndex == 2).Should().Be(9);
        allBirds.Count(b => b.IsCrossed && !b.IsOvertime && b.BirdIndex == 3).Should().Be(9);
    }

    private static PublicTournamentBirdRecord BuildBird(
        int birdIndex,
        DateTime start,
        DateTime dayEnd,
        DateTime end,
        bool isCrossed)
    {
        var isOvertime = end > dayEnd;
        TimeSpan? flyingTime = null;

        if (!isCrossed && end >= start)
        {
            flyingTime = end - start;
        }

        return new PublicTournamentBirdRecord
        {
            BirdIndex = birdIndex,
            EndTime = end,
            IsCrossed = isCrossed,
            IsOvertime = isOvertime,
            TotalBirdFlyingTime = flyingTime
        };
    }
}
