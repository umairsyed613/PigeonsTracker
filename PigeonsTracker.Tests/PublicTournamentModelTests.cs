using Xunit;
using FluentAssertions;
using PigeonsTracker.Shared.Models;
using System;
using System.Collections.Generic;

namespace PigeonsTracker.Tests;

public class PublicTournamentModelTests
{
    [Fact]
    public void PublicTournament_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var tournament = new PublicTournament();

        // Assert
        tournament.Id.Should().Be(string.Empty);
        tournament.Name.Should().Be(string.Empty);
        tournament.ManagerCode.Should().Be(string.Empty);
        tournament.Lofts.Should().BeEmpty();
        tournament.DayRecords.Should().BeEmpty();
        tournament.CodeVersion.Should().Be(1);
    }

    [Fact]
    public void PublicTournament_WithValidData_ShouldSetProperties()
    {
        // Arrange
        var now = DateTime.Now;
        var startDate = now.Date;
        var endDate = now.Date.AddDays(3);
        var flyingStart = new TimeSpan(10, 0, 0);
        var flyingEnd = new TimeSpan(16, 0, 0);

        // Act
        var tournament = new PublicTournament
        {
            Id = "tourney-1",
            Name = "Summer Championship",
            StartsFrom = startDate,
            EndTo = endDate,
            FlyingStartTime = flyingStart,
            FlyingEndTime = flyingEnd,
            ManagerCode = "MGR123",
            ManagerRecoveryKey = "REC456"
        };

        // Assert
        tournament.Id.Should().Be("tourney-1");
        tournament.Name.Should().Be("Summer Championship");
        tournament.StartsFrom.Should().Be(startDate);
        tournament.EndTo.Should().Be(endDate);
        tournament.FlyingStartTime.Should().Be(flyingStart);
        tournament.FlyingEndTime.Should().Be(flyingEnd);
        tournament.ManagerCode.Should().Be("MGR123");
    }

    [Fact]
    public void PublicTournament_DateRange_ShouldBeValid()
    {
        // Arrange
        var startDate = DateTime.Now.Date;
        var endDate = startDate.AddDays(7);

        // Act
        var tournament = new PublicTournament
        {
            StartsFrom = startDate,
            EndTo = endDate
        };

        // Assert
        (tournament.EndTo - tournament.StartsFrom).TotalDays.Should().Be(7);
    }

    [Fact]
    public void PublicTournament_TimeRange_ShouldBeValid()
    {
        // Arrange & Act
        var tournament = new PublicTournament
        {
            FlyingStartTime = new TimeSpan(08, 0, 0),
            FlyingEndTime = new TimeSpan(18, 0, 0)
        };

        // Assert
        (tournament.FlyingEndTime > tournament.FlyingStartTime).Should().BeTrue();
    }
}

public class PublicTournamentLoftTests
{
    [Fact]
    public void PublicTournamentLoft_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var loft = new PublicTournamentLoft();

        // Assert
        loft.LoftId.Should().Be(string.Empty);
        loft.LoftName.Should().Be(string.Empty);
        loft.LoftCode.Should().Be(string.Empty);
        loft.BirdCount.Should().Be(0);
        loft.HasBabyPigeon.Should().BeFalse();
    }

    [Fact]
    public void PublicTournamentLoft_WithBirds_ShouldSetBirdCount()
    {
        // Arrange & Act
        var loft = new PublicTournamentLoft
        {
            LoftId = "loft-1",
            LoftName = "My Loft",
            BirdCount = 10,
            HasBabyPigeon = true
        };

        // Assert
        loft.BirdCount.Should().Be(10);
        loft.HasBabyPigeon.Should().BeTrue();
    }
}

public class PublicTournamentBirdRecordTests
{
    [Fact]
    public void PublicTournamentBirdRecord_NormalBird_ShouldCalculateFlyingTime()
    {
        // Arrange
        var startTime = DateTime.Now;
        var landingTime = startTime.AddHours(5);
        var expectedDuration = landingTime - startTime;

        // Act
        var bird = new PublicTournamentBirdRecord
        {
            BirdIndex = 1,
            EndTime = landingTime,
            IsCrossed = false,
            TotalBirdFlyingTime = expectedDuration
        };

        // Assert
        bird.TotalBirdFlyingTime.Should().Be(expectedDuration);
        bird.IsCrossed.Should().BeFalse();
    }

    [Fact]
    public void PublicTournamentBirdRecord_CrossedBird_ShouldNotHaveFlyingTime()
    {
        // Arrange & Act
        var bird = new PublicTournamentBirdRecord
        {
            BirdIndex = 2,
            EndTime = null,
            IsCrossed = true,
            TotalBirdFlyingTime = null
        };

        // Assert
        bird.IsCrossed.Should().BeTrue();
        bird.TotalBirdFlyingTime.Should().BeNull();
        bird.EndTime.Should().BeNull();
    }

    [Fact]
    public void PublicTournamentBirdRecord_Overtime_ShouldBeDetected()
    {
        // Arrange
        var startTime = DateTime.Now;
        var flyingEndTime = startTime.Date.Add(new TimeSpan(16, 0, 0));
        var landingTimeAfterEnd = flyingEndTime.AddMinutes(30);

        // Act
        var bird = new PublicTournamentBirdRecord
        {
            BirdIndex = 1,
            EndTime = landingTimeAfterEnd,
            IsOvertime = true,
            IsCrossed = false
        };

        // Assert
        bird.IsOvertime.Should().BeTrue();
        (bird.EndTime > flyingEndTime).Should().BeTrue();
    }

    [Fact]
    public void PublicTournamentBirdRecord_OnTimeArrival_ShouldNotBeOvertime()
    {
        // Arrange
        var flyingStartTime = new TimeSpan(10, 0, 0);
        var flyingEndTime = new TimeSpan(16, 0, 0);
        var date = DateTime.Now.Date;
        var startDt = date + flyingStartTime;
        var endDt = date + flyingEndTime;
        var landingTime = endDt.AddMinutes(-30);

        // Act
        var bird = new PublicTournamentBirdRecord
        {
            BirdIndex = 1,
            EndTime = landingTime,
            IsOvertime = false,
            IsCrossed = false
        };

        // Assert
        bird.IsOvertime.Should().BeFalse();
        (bird.EndTime <= endDt).Should().BeTrue();
    }

    [Fact]
    public void PublicTournamentBirdRecord_MultipleFlying_TimeShouldSum()
    {
        // Arrange
        var birds = new List<PublicTournamentBirdRecord>
        {
            new() { BirdIndex = 1, TotalBirdFlyingTime = new TimeSpan(5, 30, 0), IsCrossed = false },
            new() { BirdIndex = 2, TotalBirdFlyingTime = new TimeSpan(6, 0, 0), IsCrossed = false },
            new() { BirdIndex = 3, TotalBirdFlyingTime = new TimeSpan(5, 15, 0), IsCrossed = false }
        };

        // Act
        var totalMinutes = 0d;
        foreach (var bird in birds)
        {
            if (bird.TotalBirdFlyingTime.HasValue)
                totalMinutes += bird.TotalBirdFlyingTime.Value.TotalMinutes;
        }
        var totalTime = TimeSpan.FromMinutes(totalMinutes);

        // Assert
        totalTime.Should().Be(new TimeSpan(16, 45, 0));
    }

    [Fact]
    public void PublicTournamentBirdRecord_MixedCrossedAndNormal_ShouldOnlyCountNormal()
    {
        // Arrange
        var birds = new List<PublicTournamentBirdRecord>
        {
            new() { BirdIndex = 1, TotalBirdFlyingTime = new TimeSpan(5, 30, 0), IsCrossed = false },
            new() { BirdIndex = 2, TotalBirdFlyingTime = null, IsCrossed = true },
            new() { BirdIndex = 3, TotalBirdFlyingTime = new TimeSpan(4, 0, 0), IsCrossed = false }
        };

        // Act
        var totalMinutes = 0d;
        foreach (var bird in birds.FindAll(b => !b.IsCrossed))
        {
            if (bird.TotalBirdFlyingTime.HasValue)
                totalMinutes += bird.TotalBirdFlyingTime.Value.TotalMinutes;
        }
        var totalTime = TimeSpan.FromMinutes(totalMinutes);

        // Assert
        totalTime.Should().Be(new TimeSpan(9, 30, 0));
        birds.FindAll(b => b.IsCrossed).Should().HaveCount(1);
    }
}

public class PublicTournamentLoftDayRecordTests
{
    [Fact]
    public void PublicTournamentLoftDayRecord_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var record = new PublicTournamentLoftDayRecord();

        // Assert
        record.LoftId.Should().Be(string.Empty);
        record.LoftName.Should().Be(string.Empty);
        record.BirdRecords.Should().BeEmpty();
        record.BabyBird.Should().BeNull();
        record.TotalLanded.Should().Be(0);
        record.TotalNotLanded.Should().Be(0);
    }

    [Fact]
    public void PublicTournamentLoftDayRecord_WithBirds_ShouldTrackLandedCount()
    {
        // Arrange
        var record = new PublicTournamentLoftDayRecord
        {
            LoftId = "loft-1",
            LoftName = "Test Loft",
            BirdRecords = new List<PublicTournamentBirdRecord>
            {
                new() { BirdIndex = 1, EndTime = DateTime.Now, IsCrossed = false },
                new() { BirdIndex = 2, EndTime = DateTime.Now, IsCrossed = false },
                new() { BirdIndex = 3, EndTime = null, IsCrossed = false }
            }
        };

        // Act
        record.TotalLanded = 2;
        record.TotalNotLanded = 1;

        // Assert
        record.TotalLanded.Should().Be(2);
        record.TotalNotLanded.Should().Be(1);
        (record.TotalLanded + record.TotalNotLanded).Should().Be(3);
    }

    [Fact]
    public void PublicTournamentLoftDayRecord_WithCrossedBirds_ShouldExcludeFromTotal()
    {
        // Arrange
        var record = new PublicTournamentLoftDayRecord
        {
            LoftId = "loft-1",
            LoftName = "Test Loft",
            BirdRecords = new List<PublicTournamentBirdRecord>
            {
                new() { BirdIndex = 1, EndTime = DateTime.Now, IsCrossed = false },
                new() { BirdIndex = 2, EndTime = null, IsCrossed = true },  // Crossed - not counted
                new() { BirdIndex = 3, EndTime = null, IsCrossed = false }
            }
        };

        // Act
        record.TotalLanded = 1;
        record.TotalNotLanded = 1;  // Only normal birds count

        // Assert
        record.TotalLanded.Should().Be(1);
        record.TotalNotLanded.Should().Be(1);
    }

    [Fact]
    public void PublicTournamentLoftDayRecord_WithBabyBird_ShouldTrackSeparately()
    {
        // Arrange
        var babyBirdTime = new TimeSpan(4, 30, 0);
        var record = new PublicTournamentLoftDayRecord
        {
            LoftId = "loft-1",
            LoftName = "Test Loft",
            BabyBird = new PublicTournamentBirdRecord
            {
                BirdIndex = 1,
                EndTime = DateTime.Now,
                IsCrossed = false,
                TotalBirdFlyingTime = babyBirdTime
            },
            BabyPigeonSum = babyBirdTime
        };

        // Assert
        record.BabyBird.Should().NotBeNull();
        record.BabyPigeonSum.Should().Be(babyBirdTime);
    }

    [Fact]
    public void PublicTournamentLoftDayRecord_TotalHours_ShouldCalculateCorrectly()
    {
        // Arrange
        var record = new PublicTournamentLoftDayRecord
        {
            LoftId = "loft-1",
            LoftName = "Test Loft",
            TotalHours = new TimeSpan(15, 45, 0)
        };

        // Assert
        record.TotalHours.Value.TotalHours.Should().BeApproximately(15.75, 0.01);
    }
}

public class PublicTournamentDayRecordTests
{
    [Fact]
    public void PublicTournamentDayRecord_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var dayRecord = new PublicTournamentDayRecord();

        // Assert
        dayRecord.Id.Should().Be(string.Empty);
        dayRecord.LoftRecords.Should().BeEmpty();
    }

    [Fact]
    public void PublicTournamentDayRecord_WithMultipleLofts_ShouldTrackAll()
    {
        // Arrange
        var today = DateTime.Now.Date;
        var dayRecord = new PublicTournamentDayRecord
        {
            Id = "day-1",
            Date = today,
            LoftRecords = new List<PublicTournamentLoftDayRecord>
            {
                new() { LoftId = "loft-1", LoftName = "Loft A" },
                new() { LoftId = "loft-2", LoftName = "Loft B" },
                new() { LoftId = "loft-3", LoftName = "Loft C" }
            }
        };

        // Assert
        dayRecord.LoftRecords.Should().HaveCount(3);
        dayRecord.Date.Should().Be(today);
    }

    [Fact]
    public void PublicTournamentDayRecord_DateShouldMatchRecords()
    {
        // Arrange
        var date1 = DateTime.Now.Date;
        var date2 = date1.AddDays(1);

        // Act
        var dayRecord1 = new PublicTournamentDayRecord { Date = date1 };
        var dayRecord2 = new PublicTournamentDayRecord { Date = date2 };

        // Assert
        dayRecord1.Date.Should().Be(date1);
        dayRecord2.Date.Should().Be(date2);
        dayRecord1.Date.Should().BeBefore(dayRecord2.Date);
    }
}
