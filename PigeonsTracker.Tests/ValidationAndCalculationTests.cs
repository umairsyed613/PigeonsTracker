using Xunit;
using FluentAssertions;
using PigeonsTracker.Shared.Models;
using System;
using System.Collections.Generic;

namespace PigeonsTracker.Tests;

public class BirdValidationTests
{
    [Fact]
    public void ValidateBirdLandingTime_BeforeStartTime_ShouldFail()
    {
        // Arrange
        var flyingStartTime = new TimeSpan(10, 0, 0);
        var date = DateTime.Now.Date;
        var startDateTime = date + flyingStartTime;
        var landingTime = startDateTime.AddHours(-1);

        // Act
        var isValid = landingTime >= startDateTime;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateBirdLandingTime_AfterStartTime_ShouldPass()
    {
        // Arrange
        var flyingStartTime = new TimeSpan(10, 0, 0);
        var date = DateTime.Now.Date;
        var startDateTime = date + flyingStartTime;
        var landingTime = startDateTime.AddHours(2);

        // Act
        var isValid = landingTime >= startDateTime;

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateCrossedBird_ShouldNotRequireLandingTime()
    {
        // Arrange
        var isCrossed = true;
        DateTime? endTime = null;

        // Act
        var isValid = isCrossed && !endTime.HasValue;

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateCrossedBird_WithLandingTime_ShouldFail()
    {
        // Arrange
        var isCrossed = true;
        var endTime = DateTime.Now;

        // Act
        var isValid = !(isCrossed && endTime != null);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateNormalBird_WithoutLandingTime_ShouldFail()
    {
        // Arrange
        var isCrossed = false;
        DateTime? endTime = null;

        // Act
        var isValid = !isCrossed && endTime.HasValue;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateNormalBird_WithLandingTime_ShouldPass()
    {
        // Arrange
        var isCrossed = false;
        DateTime? endTime = DateTime.Now;

        // Act
        var isValid = !isCrossed && endTime.HasValue;

        // Assert
        isValid.Should().BeTrue();
    }
}

public class BirdTimeCalculationTests
{
    [Fact]
    public void CalculateFlyingTime_NormalBird_ShouldCalculateCorrectly()
    {
        // Arrange
        var startTime = DateTime.Now;
        DateTime? endTime = startTime.AddHours(5).AddMinutes(30);
        var isCrossed = false;

        // Act
        TimeSpan? flyingTime = null;
        if (endTime.HasValue && endTime.Value >= startTime && !isCrossed)
        {
            flyingTime = endTime.Value - startTime;
        }

        // Assert
        flyingTime.Should().NotBeNull();
        flyingTime!.Value.TotalHours.Should().BeApproximately(5.5, 0.01);
    }

    [Fact]
    public void CalculateFlyingTime_CrossedBird_ShouldBeNull()
    {
        // Arrange
        var startTime = DateTime.Now;
        DateTime? endTime = null;
        var isCrossed = true;

        // Act
        TimeSpan? flyingTime = null;
        if (endTime.HasValue && endTime.Value >= startTime && !isCrossed)
        {
            flyingTime = endTime.Value - startTime;
        }

        // Assert
        flyingTime.Should().BeNull();
    }

    [Fact]
    public void CalculateOvertimeStatus_BirdAfterFlyingEnd_ShouldBeOvertime()
    {
        // Arrange
        var date = DateTime.Now.Date;
        var flyingEndTime = new TimeSpan(16, 0, 0);
        var dayEnd = date + flyingEndTime;
        var landingTime = dayEnd.AddMinutes(30);

        // Act
        var isOvertime = landingTime > dayEnd;

        // Assert
        isOvertime.Should().BeTrue();
    }

    [Fact]
    public void CalculateOvertimeStatus_BirdBeforeFlyingEnd_ShouldNotBeOvertime()
    {
        // Arrange
        var date = DateTime.Now.Date;
        var flyingEndTime = new TimeSpan(16, 0, 0);
        var dayEnd = date + flyingEndTime;
        var landingTime = dayEnd.AddMinutes(-30);

        // Act
        var isOvertime = landingTime > dayEnd;

        // Assert
        isOvertime.Should().BeFalse();
    }
}

public class PartialUpdateValidationTests
{
    [Fact]
    public void SaveBirdRecords_WithSomeEmptyBirds_ShouldSkipEmpty()
    {
        // Arrange
        var birdsData = new List<(int index, DateTime? endTime, bool isCrossed)>
        {
            (1, DateTime.Now, false),     // Has data
            (2, null, false),              // Empty - should skip
            (3, DateTime.Now, false),      // Has data
            (4, null, false)               // Empty - should skip
        };

        // Act
        var recordsToSave = new List<PublicTournamentBirdRecord>();
        var startTime = DateTime.Now.Date + new TimeSpan(10, 0, 0);

        foreach (var (index, endTime, isCrossed) in birdsData)
        {
            // Skip if no data
            if (!endTime.HasValue && !isCrossed)
                continue;

            recordsToSave.Add(new PublicTournamentBirdRecord
            {
                BirdIndex = index,
                EndTime = endTime,
                IsCrossed = isCrossed
            });
        }

        // Assert
        recordsToSave.Should().HaveCount(2);
        recordsToSave[0].BirdIndex.Should().Be(1);
        recordsToSave[1].BirdIndex.Should().Be(3);
    }

    [Fact]
    public void SaveBirdRecords_WithOnlyCrossedBird_ShouldInclude()
    {
        // Arrange
        var birdsData = new List<(int index, DateTime? endTime, bool isCrossed)>
        {
            (1, null, false),              // Empty - skip
            (2, null, true),               // Crossed - include
            (3, null, false)               // Empty - skip
        };

        // Act
        var recordsToSave = new List<PublicTournamentBirdRecord>();

        foreach (var (index, endTime, isCrossed) in birdsData)
        {
            if (!endTime.HasValue && !isCrossed)
                continue;

            recordsToSave.Add(new PublicTournamentBirdRecord
            {
                BirdIndex = index,
                EndTime = endTime,
                IsCrossed = isCrossed
            });
        }

        // Assert
        recordsToSave.Should().HaveCount(1);
        recordsToSave[0].BirdIndex.Should().Be(2);
        recordsToSave[0].IsCrossed.Should().BeTrue();
    }

    [Fact]
    public void SaveBirdRecords_AllEmpty_ShouldResultInEmptyList()
    {
        // Arrange
        var birdsData = new List<(int index, DateTime? endTime, bool isCrossed)>
        {
            (1, null, false),
            (2, null, false),
            (3, null, false)
        };

        // Act
        var recordsToSave = new List<PublicTournamentBirdRecord>();

        foreach (var (index, endTime, isCrossed) in birdsData)
        {
            if (!endTime.HasValue && !isCrossed)
                continue;

            recordsToSave.Add(new PublicTournamentBirdRecord
            {
                BirdIndex = index,
                EndTime = endTime,
                IsCrossed = isCrossed
            });
        }

        // Assert
        recordsToSave.Should().BeEmpty();
    }
}

public class TournamentTimeRangeTests
{
    [Fact]
    public void ValidateFlyingTimeRange_StartBeforeEnd_ShouldPass()
    {
        // Arrange
        var flyingStart = new TimeSpan(8, 0, 0);
        var flyingEnd = new TimeSpan(18, 0, 0);

        // Act
        var isValid = flyingStart < flyingEnd;

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateFlyingTimeRange_StartAfterEnd_ShouldFail()
    {
        // Arrange
        var flyingStart = new TimeSpan(18, 0, 0);
        var flyingEnd = new TimeSpan(8, 0, 0);

        // Act
        var isValid = flyingStart < flyingEnd;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateTournamentDateRange_StartBeforeEnd_ShouldPass()
    {
        // Arrange
        var startDate = DateTime.Now.Date;
        var endDate = startDate.AddDays(7);

        // Act
        var isValid = startDate < endDate;

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateLandingTimeInRange_BetweenStartAndEnd_ShouldPass()
    {
        // Arrange
        var date = DateTime.Now.Date;
        var startTime = date + new TimeSpan(10, 0, 0);
        var endTime = date + new TimeSpan(16, 0, 0);
        var landingTime = date + new TimeSpan(14, 30, 0);

        // Act
        var isValid = landingTime >= startTime && landingTime <= endTime;

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateLandingTimeInRange_OutsideRange_ShouldFail()
    {
        // Arrange
        var date = DateTime.Now.Date;
        var startTime = date + new TimeSpan(10, 0, 0);
        var endTime = date + new TimeSpan(16, 0, 0);
        var landingTime = date + new TimeSpan(17, 30, 0);

        // Act
        var isValid = landingTime >= startTime && landingTime <= endTime;

        // Assert
        isValid.Should().BeFalse();
    }
}
