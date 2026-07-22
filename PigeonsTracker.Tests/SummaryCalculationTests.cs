using Xunit;
using FluentAssertions;
using PigeonsTracker.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PigeonsTracker.Tests;

public class TournamentSummaryCalculationTests
{
    [Fact]
    public void CalculateTotalBirdsLanded_WithValidRecords_ShouldCountCorrectly()
    {
        // Arrange
        var loftDayRecord = new PublicTournamentLoftDayRecord
        {
            BirdRecords = new List<PublicTournamentBirdRecord>
            {
                new() { BirdIndex = 1, EndTime = DateTime.Now, IsCrossed = false },
                new() { BirdIndex = 2, EndTime = DateTime.Now, IsCrossed = false },
                new() { BirdIndex = 3, EndTime = null, IsCrossed = false },
                new() { BirdIndex = 4, EndTime = null, IsCrossed = true }
            }
        };

        // Act
        var landed = loftDayRecord.BirdRecords.Count(b => b.EndTime.HasValue && !b.IsCrossed);
        var notLanded = loftDayRecord.BirdRecords.Count(b => !b.EndTime.HasValue && !b.IsCrossed);

        // Assert
        landed.Should().Be(2);
        notLanded.Should().Be(1);
    }

    [Fact]
    public void CalculateAverageFlyingTime_MultipleBirds_ShouldCalculateCorrectly()
    {
        // Arrange
        var birds = new List<PublicTournamentBirdRecord>
        {
            new() { TotalBirdFlyingTime = new TimeSpan(5, 0, 0), IsCrossed = false },
            new() { TotalBirdFlyingTime = new TimeSpan(6, 0, 0), IsCrossed = false },
            new() { TotalBirdFlyingTime = new TimeSpan(7, 0, 0), IsCrossed = false }
        };

        // Act
        var totalMinutes = birds.Where(b => !b.IsCrossed && b.TotalBirdFlyingTime.HasValue)
                                .Sum(b => b.TotalBirdFlyingTime.Value.TotalMinutes);
        var averageMinutes = totalMinutes / 3;
        var averageTimeSpan = TimeSpan.FromMinutes(averageMinutes);

        // Assert
        totalMinutes.Should().Be(1080); // 5h + 6h + 7h
        averageTimeSpan.TotalHours.Should().BeApproximately(6, 0.01);
    }

    [Fact]
    public void CalculateLoftTotalHours_MultipleRecords_ShouldSumCorrectly()
    {
        // Arrange
        var birds = new List<PublicTournamentBirdRecord>
        {
            new() { TotalBirdFlyingTime = new TimeSpan(5, 30, 0), IsCrossed = false },
            new() { TotalBirdFlyingTime = new TimeSpan(6, 20, 0), IsCrossed = false },
            new() { TotalBirdFlyingTime = null, IsCrossed = true }
        };

        // Act
        var totalMinutes = birds.Where(b => !b.IsCrossed && b.TotalBirdFlyingTime.HasValue)
                                .Sum(b => b.TotalBirdFlyingTime.Value.TotalMinutes);
        var totalTime = TimeSpan.FromMinutes(totalMinutes);

        // Assert
        totalTime.Should().Be(new TimeSpan(11, 50, 0));
    }

    [Fact]
    public void CalculateBabyBirdSum_WithValidRecord_ShouldIncludeInTotal()
    {
        // Arrange
        var babyBirdTime = new TimeSpan(4, 45, 0);
        var normalBirdsTime = new TimeSpan(20, 0, 0);

        // Act
        var totalWithBaby = TimeSpan.FromMinutes(
            normalBirdsTime.TotalMinutes + babyBirdTime.TotalMinutes
        );

        // Assert
        totalWithBaby.Should().Be(new TimeSpan(24, 45, 0));
    }

    [Fact]
    public void CountOvertimeBirds_WithMixedRecords_ShouldCountCorrectly()
    {
        // Arrange
        var birds = new List<PublicTournamentBirdRecord>
        {
            new() { BirdIndex = 1, IsOvertime = true, IsCrossed = false },
            new() { BirdIndex = 2, IsOvertime = false, IsCrossed = false },
            new() { BirdIndex = 3, IsOvertime = true, IsCrossed = false },
            new() { BirdIndex = 4, IsOvertime = false, IsCrossed = true }
        };

        // Act
        var overtimeCount = birds.Count(b => b.IsOvertime);
        var onTimeCount = birds.Count(b => !b.IsOvertime && !b.IsCrossed);

        // Assert
        overtimeCount.Should().Be(2);
        onTimeCount.Should().Be(1);
    }
}

public class BirdIndexSummaryTests
{
    [Fact]
    public void BirdIndexSummary_WithMultipleLofts_ShouldTrackPerBird()
    {
        // Arrange
        var summary = new PublicTournamentBirdIndexSummaryResponse
        {
            MaxBirdCount = 5,
            Rows = new List<PublicTournamentBirdIndexSummaryRow>
            {
                new() { LoftName = "Loft A", BirdHoursTicks = new List<long?> { 
                    new TimeSpan(5, 0, 0).Ticks, 
                    new TimeSpan(6, 0, 0).Ticks,
                    new TimeSpan(5, 30, 0).Ticks,
                    new TimeSpan(6, 30, 0).Ticks,
                    new TimeSpan(5, 15, 0).Ticks
                }},
                new() { LoftName = "Loft B", BirdHoursTicks = new List<long?> { 
                    new TimeSpan(4, 45, 0).Ticks,
                    new TimeSpan(5, 30, 0).Ticks,
                    null,
                    new TimeSpan(6, 0, 0).Ticks,
                    new TimeSpan(5, 45, 0).Ticks
                }}
            }
        };

        // Act
        var loftABird1Ticks = summary.Rows[0].BirdHoursTicks[0];
        var loftABird1Time = loftABird1Ticks.HasValue ? new TimeSpan(loftABird1Ticks.Value) : TimeSpan.Zero;
        var loftBBird3IsNull = summary.Rows[1].BirdHoursTicks[2];

        // Assert
        loftABird1Time.TotalHours.Should().Be(5);
        loftBBird3IsNull.Should().BeNull();
    }

    [Fact]
    public void BirdIndexSummary_TotalSum_ShouldAggregateAll()
    {
        // Arrange
        var row = new PublicTournamentBirdIndexSummaryRow
        {
            LoftName = "Loft A",
            BirdHoursTicks = new List<long?>
            {
                new TimeSpan(5, 0, 0).Ticks,
                new TimeSpan(6, 0, 0).Ticks,
                new TimeSpan(5, 30, 0).Ticks
            },
            TotalSumOfDayTicks = 0
        };

        // Act
        row.TotalSumOfDayTicks = row.BirdHoursTicks
            .Where(t => t.HasValue)
            .Sum(t => t!.Value);

        var totalTime = new TimeSpan(row.TotalSumOfDayTicks);

        // Assert
        totalTime.Should().Be(new TimeSpan(16, 30, 0));
    }
}

public class TotalsSummaryTests
{
    [Fact]
    public void TotalsSummary_WithCompleteData_ShouldHaveAllFields()
    {
        // Arrange & Act
        var row = new PublicTournamentTotalsSummaryRow
        {
            LoftName = "Test Loft",
            DateOfFlying = DateTime.Now.Date,
            TotalLanded = 8,
            TotalNotLanded = 2,
            BabyPigeonSumTicks = new TimeSpan(4, 30, 0).Ticks,
            TotalHoursTicks = new TimeSpan(45, 0, 0).Ticks
        };

        // Assert
        row.LoftName.Should().Be("Test Loft");
        row.TotalLanded.Should().Be(8);
        row.TotalNotLanded.Should().Be(2);
        (row.TotalLanded + row.TotalNotLanded).Should().Be(10);
        
        var babyTime = new TimeSpan(row.BabyPigeonSumTicks);
        babyTime.TotalHours.Should().Be(4.5);
        
        var totalHours = new TimeSpan(row.TotalHoursTicks);
        totalHours.TotalHours.Should().Be(45);
    }

    [Fact]
    public void TotalsSummary_MultipleRows_ShouldAggregate()
    {
        // Arrange
        var rows = new List<PublicTournamentTotalsSummaryRow>
        {
            new() { LoftName = "Loft A", TotalLanded = 8, TotalNotLanded = 2 },
            new() { LoftName = "Loft B", TotalLanded = 9, TotalNotLanded = 1 },
            new() { LoftName = "Loft C", TotalLanded = 7, TotalNotLanded = 3 }
        };

        // Act
        var totalLanded = rows.Sum(r => r.TotalLanded);
        var totalNotLanded = rows.Sum(r => r.TotalNotLanded);
        var grandTotal = totalLanded + totalNotLanded;

        // Assert
        totalLanded.Should().Be(24);
        totalNotLanded.Should().Be(6);
        grandTotal.Should().Be(30);
    }
}

public class DateRangeFilteringTests
{
    [Fact]
    public void FilterDayRecordsByDate_ShouldReturnCorrectRecords()
    {
        // Arrange
        var startDate = DateTime.Now.Date;
        var endDate = startDate.AddDays(7);
        var dayRecords = new List<PublicTournamentDayRecord>
        {
            new() { Date = startDate, Id = "day-1" },
            new() { Date = startDate.AddDays(1), Id = "day-2" },
            new() { Date = startDate.AddDays(5), Id = "day-3" },
            new() { Date = endDate, Id = "day-4" },
            new() { Date = endDate.AddDays(1), Id = "day-5" }
        };

        // Act
        var filtered = dayRecords.Where(d => d.Date >= startDate && d.Date <= endDate).ToList();

        // Assert
        filtered.Should().HaveCount(4);
        filtered.All(d => d.Date >= startDate && d.Date <= endDate).Should().BeTrue();
    }

    [Fact]
    public void FilterBirdsByDate_ShouldReturnOnlyForSpecificDay()
    {
        // Arrange
        var targetDate = DateTime.Now.Date;
        var tournament = new PublicTournament
        {
            DayRecords = new List<PublicTournamentDayRecord>
            {
                new() { Date = targetDate, LoftRecords = new List<PublicTournamentLoftDayRecord>
                    { new() { LoftId = "loft-1" } } },
                new() { Date = targetDate.AddDays(1), LoftRecords = new List<PublicTournamentLoftDayRecord>
                    { new() { LoftId = "loft-2" } } }
            }
        };

        // Act
        var targetDayRecord = tournament.DayRecords.FirstOrDefault(d => d.Date == targetDate);

        // Assert
        targetDayRecord.Should().NotBeNull();
        if (targetDayRecord != null)
        {
            targetDayRecord.LoftRecords.Should().HaveCount(1);
            targetDayRecord.LoftRecords[0].LoftId.Should().Be("loft-1");
        }
    }
}
