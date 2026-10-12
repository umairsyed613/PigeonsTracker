using Xunit;
using FluentAssertions;
using System.Text.Json;
using System.Collections.Generic;
using PigeonsTracker.Shared.Models;

namespace PigeonsTracker.Tests;

public class IndexedDbCacheServiceDeserializationTests
{
    [Fact]
    public void Deserialize_WithPropertyNameCaseInsensitive_ShouldProperlyDeserializeComplexObjects()
    {
        // Arrange: Simulate what comes back from IndexedDB through JavaScript
        // When JS stores and retrieves objects, they retain their original property casing
        var jsonString = """
        {
            "key": "tournaments",
            "data": [
                {
                    "fireStoreId": "fs-123",
                    "id": "tournament-1",
                    "name": "Summer Championship",
                    "createdAt": "2024-01-15T10:30:00Z",
                    "startsFrom": "2024-06-01T00:00:00Z",
                    "endTo": "2024-06-15T00:00:00Z",
                    "flyingStartTime": "10:00:00",
                    "flyingEndTime": "16:00:00",
                    "canManageLoftRecords": true,
                    "managerCode": "MGR123",
                    "managerRecoveryKey": "REC456",
                    "codeVersion": 1,
                    "lastCodeRegeneratedAt": null,
                    "updatedAt": "2024-01-15T10:30:00Z",
                    "posterImage": null,
                    "lofts": [],
                    "dayRecords": []
                }
            ],
            "cachedAt": "2024-01-15T10:30:00Z",
            "lastSyncAt": "2024-01-15T10:30:00Z"
        }
        """;

        var jsonElement = JsonDocument.Parse(jsonString).RootElement;
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act: Deserialize the data field as if we're retrieving from cache
        var dataElement = jsonElement.GetProperty("data");
        var tournaments = dataElement.Deserialize<List<PublicTournament>>(jsonOptions);

        // Assert
        tournaments.Should().NotBeNull();
        tournaments.Should().HaveCount(1);

        var tournament = tournaments![0];
        tournament.FireStoreId.Should().Be("fs-123");
        tournament.Id.Should().Be("tournament-1");
        tournament.Name.Should().Be("Summer Championship");
        tournament.ManagerCode.Should().Be("MGR123");
        tournament.ManagerRecoveryKey.Should().Be("REC456");
        tournament.CodeVersion.Should().Be(1);
        tournament.CanManageLoftRecords.Should().BeTrue();
        tournament.Lofts.Should().BeEmpty();
        tournament.DayRecords.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_WithoutPropertyNameCaseInsensitive_ShouldFailToDeserialize()
    {
        // Arrange: Simulate what comes back from IndexedDB
        var jsonString = """
        {
            "data": [
                {
                    "fireStoreId": "fs-123",
                    "id": "tournament-1",
                    "name": "Summer Championship",
                    "createdAt": "2024-01-15T10:30:00Z",
                    "startsFrom": "2024-06-01T00:00:00Z",
                    "endTo": "2024-06-15T00:00:00Z",
                    "flyingStartTime": "10:00:00",
                    "flyingEndTime": "16:00:00",
                    "canManageLoftRecords": true,
                    "managerCode": "MGR123",
                    "managerRecoveryKey": "REC456",
                    "codeVersion": 1,
                    "lastCodeRegeneratedAt": null,
                    "updatedAt": "2024-01-15T10:30:00Z",
                    "posterImage": null,
                    "lofts": [],
                    "dayRecords": []
                }
            ]
        }
        """;

        var jsonElement = JsonDocument.Parse(jsonString).RootElement;
        // Deliberate: NOT using PropertyNameCaseInsensitive
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };

        // Act: Deserialize the data field without case-insensitive option
        var dataElement = jsonElement.GetProperty("data");
        var tournaments = dataElement.Deserialize<List<PublicTournament>>(jsonOptions);

        // Assert: Properties won't be populated because the casing doesn't match
        tournaments.Should().NotBeNull();
        tournaments.Should().HaveCount(1);

        var tournament = tournaments![0];
        // Without case-insensitive matching, properties remain as defaults
        tournament.FireStoreId.Should().Be(string.Empty);  // Default value, not populated
        tournament.Id.Should().Be(string.Empty);  // Default value, not populated
        tournament.Name.Should().Be(string.Empty);  // Default value, not populated
    }

    [Fact]
    public void Deserialize_SummaryResponseWithCaseInsensitive_ShouldProperlyPopulateProperties()
    {
        // Arrange: Test with PublicTournamentBirdIndexSummaryResponse
        var jsonString = """
        {
            "key": "bird-index-summary",
            "data": {
                "maxBirdCount": 10,
                "rows": [
                    {
                        "loftName": "Main Loft",
                        "dateOfFlying": "2024-01-15T00:00:00Z",
                        "birdHoursTicks": [18000000000, 21600000000, 19800000000],
                        "totalSumOfDayTicks": 59400000000
                    }
                ]
            },
            "cachedAt": "2024-01-15T10:30:00Z",
            "lastSyncAt": "2024-01-15T10:30:00Z"
        }
        """;

        var jsonElement = JsonDocument.Parse(jsonString).RootElement;
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var dataElement = jsonElement.GetProperty("data");
        var summary = dataElement.Deserialize<PublicTournamentBirdIndexSummaryResponse>(jsonOptions);

        // Assert
        summary.Should().NotBeNull();
        summary!.MaxBirdCount.Should().Be(10);
        summary.Rows.Should().HaveCount(1);

        var row = summary.Rows[0];
        row.LoftName.Should().Be("Main Loft");
        row.BirdHoursTicks.Should().HaveCount(3);
        row.TotalSumOfDayTicks.Should().Be(59400000000);
    }
}
