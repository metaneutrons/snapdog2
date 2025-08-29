//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
using FluentAssertions;

namespace SnapDog2.Tests.Unit.Infrastructure.Services;

/// <summary>
/// Pure business logic tests for zone grouping validation algorithms.
/// Tests the core logic without any external dependencies or mocking.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Logic", "ZoneGrouping")]
public class ZoneGroupingLogicTests
{
    #region Core Validation Logic Tests

    [Fact]
    public void ValidateZoneGrouping_WhenClientInCorrectGroup_ShouldBeValid()
    {
        // Arrange
        var scenario = new ZoneGroupingScenario
        {
            ZoneId = 1,
            ZoneStreamId = "/snapsinks/zone1",
            ClientIndex = "living-room",
            Groups = new[]
            {
                CreateGroup("group1", "/snapsinks/zone1", "living-room", "kitchen"),
                CreateGroup("group2", "/snapsinks/zone2", "bedroom"),
            },
        };

        // Act
        var result = ValidateClientGrouping(scenario);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ExpectedGroupId.Should().Be("group1");
        result.ActualGroupId.Should().Be("group1");
        result.ValidationMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateZoneGrouping_WhenClientInWrongGroup_ShouldBeInvalid()
    {
        // Arrange - This tests the cross-zone grouping bug
        var scenario = new ZoneGroupingScenario
        {
            ZoneId = 2,
            ZoneStreamId = "/snapsinks/zone2",
            ClientIndex = "bedroom",
            Groups = new[]
            {
                CreateGroup("group1", "/snapsinks/zone1", "living-room", "kitchen", "bedroom"), // Wrong!
                CreateGroup("group2", "/snapsinks/zone2"), // Empty - bedroom should be here
            },
        };

        // Act
        var result = ValidateClientGrouping(scenario);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ExpectedGroupId.Should().Be("group2");
        result.ActualGroupId.Should().Be("group1");
        result.ValidationMessage.Should().Contain("bedroom is in group group1 but should be in group group2");
    }

    [Fact]
    public void ValidateZoneGrouping_WhenClientNotFound_ShouldBeInvalid()
    {
        // Arrange
        var scenario = new ZoneGroupingScenario
        {
            ZoneId = 1,
            ZoneStreamId = "/snapsinks/zone1",
            ClientIndex = "missing-client",
            Groups = new[]
            {
                CreateGroup("group1", "/snapsinks/zone1", "living-room"),
                CreateGroup("group2", "/snapsinks/zone2", "bedroom"),
            },
        };

        // Act
        var result = ValidateClientGrouping(scenario);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ExpectedGroupId.Should().Be("group1");
        result.ActualGroupId.Should().BeNull();
        result.ValidationMessage.Should().Contain("missing-client not found in any group");
    }

    [Fact]
    public void ValidateZoneGrouping_WhenNoGroupForZone_ShouldBeInvalid()
    {
        // Arrange
        var scenario = new ZoneGroupingScenario
        {
            ZoneId = 3,
            ZoneStreamId = "/snapsinks/zone3",
            ClientIndex = "office-client",
            Groups = new[]
            {
                CreateGroup("group1", "/snapsinks/zone1", "living-room"),
                CreateGroup("group2", "/snapsinks/zone2", "bedroom"),
                // No group for zone3!
            },
        };

        // Act
        var result = ValidateClientGrouping(scenario);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ExpectedGroupId.Should().BeNull();
        result.ValidationMessage.Should().Contain("No group found for zone stream /snapsinks/zone3");
    }

    #endregion

    #region Multi-Client Validation Tests

    [Fact]
    public void ValidateMultipleClients_WhenAllInCorrectGroups_ShouldBeValid()
    {
        // Arrange
        var groups = new[]
        {
            CreateGroup("group1", "/snapsinks/zone1", "living-room", "kitchen"),
            CreateGroup("group2", "/snapsinks/zone2", "bedroom", "office"),
        };

        var scenarios = new[]
        {
            new { ClientIndex = "living-room", ZoneStreamId = "/snapsinks/zone1" },
            new { ClientIndex = "kitchen", ZoneStreamId = "/snapsinks/zone1" },
            new { ClientIndex = "bedroom", ZoneStreamId = "/snapsinks/zone2" },
            new { ClientIndex = "office", ZoneStreamId = "/snapsinks/zone2" },
        };

        // Act & Assert
        foreach (var scenario in scenarios)
        {
            var testScenario = new ZoneGroupingScenario
            {
                ZoneId = 1,
                ZoneStreamId = scenario.ZoneStreamId,
                ClientIndex = scenario.ClientIndex,
                Groups = groups,
            };

            var result = ValidateClientGrouping(testScenario);
            result.IsValid.Should().BeTrue($"Client {scenario.ClientIndex} should be valid");
        }
    }

    [Fact]
    public void ValidateMultipleClients_WithMixedValidation_ShouldDetectIssues()
    {
        // Arrange - Mixed scenario with some correct, some incorrect
        var groups = new[]
        {
            CreateGroup("group1", "/snapsinks/zone1", "living-room", "bedroom"), // bedroom is wrong zone
            CreateGroup("group2", "/snapsinks/zone2", "kitchen"), // kitchen is wrong zone
        };

        // Act
        var livingRoomResult = ValidateClientGrouping(
            new ZoneGroupingScenario
            {
                ZoneId = 1,
                ZoneStreamId = "/snapsinks/zone1",
                ClientIndex = "living-room",
                Groups = groups,
            }
        );

        var bedroomResult = ValidateClientGrouping(
            new ZoneGroupingScenario
            {
                ZoneId = 2,
                ZoneStreamId = "/snapsinks/zone2",
                ClientIndex = "bedroom",
                Groups = groups,
            }
        );

        // Assert
        livingRoomResult.IsValid.Should().BeTrue("living-room is in correct group");
        bedroomResult.IsValid.Should().BeFalse("bedroom is in wrong group");
        bedroomResult.ValidationMessage.Should().Contain("bedroom is in group group1 but should be in group group2");
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateZoneGrouping_WithInvalidClientIndex_ShouldHandleGracefully(string? invalidClientIndex)
    {
        // Arrange
        var scenario = new ZoneGroupingScenario
        {
            ZoneId = 1,
            ZoneStreamId = "/snapsinks/zone1",
            ClientIndex = invalidClientIndex!,
            Groups = new[] { CreateGroup("group1", "/snapsinks/zone1", "valid-client") },
        };

        // Act
        var result = ValidateClientGrouping(scenario);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ValidationMessage.Should().Contain("not found in any group");
    }

    [Fact]
    public void ValidateZoneGrouping_WithEmptyGroups_ShouldHandleGracefully()
    {
        // Arrange
        var scenario = new ZoneGroupingScenario
        {
            ZoneId = 1,
            ZoneStreamId = "/snapsinks/zone1",
            ClientIndex = "test-client",
            Groups = Array.Empty<TestGroup>(),
        };

        // Act
        var result = ValidateClientGrouping(scenario);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ValidationMessage.Should().Contain("No group found for zone stream");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Core validation logic that mimics the ZoneGroupingService business rules.
    /// This is the pure algorithm without any external dependencies.
    /// </summary>
    private static ValidationResult ValidateClientGrouping(ZoneGroupingScenario scenario)
    {
        // Handle edge cases
        if (string.IsNullOrWhiteSpace(scenario.ClientIndex))
        {
            return new ValidationResult
            {
                IsValid = false,
                ExpectedGroupId = null,
                ActualGroupId = null,
                ValidationMessage = $"Client Index '{scenario.ClientIndex}' not found in any group",
            };
        }

        // Find the expected group for this zone (group with matching stream)
        var expectedGroup = scenario.Groups.FirstOrDefault(g => g.StreamId == scenario.ZoneStreamId);
        var expectedGroupId = expectedGroup?.Id;

        if (expectedGroup == null)
        {
            return new ValidationResult
            {
                IsValid = false,
                ExpectedGroupId = null,
                ActualGroupId = null,
                ValidationMessage = $"No group found for zone stream {scenario.ZoneStreamId}",
            };
        }

        // Find which group the client is actually in
        var actualGroup = scenario.Groups.FirstOrDefault(g => g.ClientIndexs.Contains(scenario.ClientIndex));
        var actualGroupId = actualGroup?.Id;

        // Validate
        var isValid = actualGroupId == expectedGroupId && actualGroupId != null;

        string? validationMessage = null;
        if (actualGroupId == null)
        {
            validationMessage = $"Client {scenario.ClientIndex} not found in any group";
        }
        else if (!isValid)
        {
            validationMessage =
                $"Client {scenario.ClientIndex} is in group {actualGroupId} but should be in group {expectedGroupId}";
        }

        return new ValidationResult
        {
            IsValid = isValid,
            ExpectedGroupId = expectedGroupId,
            ActualGroupId = actualGroupId,
            ValidationMessage = validationMessage,
        };
    }

    private static TestGroup CreateGroup(string id, string streamId, params string[] clientIndexs) =>
        new()
        {
            Id = id,
            StreamId = streamId,
            ClientIndexs = clientIndexs.ToList(),
        };

    #endregion

    #region Test Models

    private record ZoneGroupingScenario
    {
        public required int ZoneId { get; init; }
        public required string ZoneStreamId { get; init; }
        public required string ClientIndex { get; init; }
        public required TestGroup[] Groups { get; init; }
    }

    private record TestGroup
    {
        public required string Id { get; init; }
        public required string StreamId { get; init; }
        public required List<string> ClientIndexs { get; init; }
    }

    private record ValidationResult
    {
        public required bool IsValid { get; init; }
        public required string? ExpectedGroupId { get; init; }
        public required string? ActualGroupId { get; init; }
        public required string? ValidationMessage { get; init; }
    }

    #endregion
}
