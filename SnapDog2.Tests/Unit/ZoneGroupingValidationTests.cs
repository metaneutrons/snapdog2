using FluentAssertions;
using SnapDog2.Core.Models;
using SnapDog2.Tests.Fixtures.Shared;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Unit;

/// <summary>
/// Unit tests for zone grouping validation logic.
/// Tests the core logic that detects when clients are in wrong zone groups.
/// </summary>
[Collection(TestCategories.Unit)]
[Trait("Category", TestCategories.Unit)]
[Trait("TestType", TestTypes.Service)]
[Trait("TestSpeed", TestSpeed.Fast)]
public class ZoneGroupingValidationTests
{
    private readonly ITestOutputHelper _output;

    public ZoneGroupingValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ZoneGroupingLogic_WhenClientInCorrectGroup_ShouldBeValid()
    {
        // Arrange
        _output.WriteLine("🧪 Testing zone grouping logic - client in correct group");

        var zoneStreamId = "/snapsinks/zone2";
        var clientId = "bedroom";
        var expectedGroupId = "group2";

        // Client is in the correct group for its zone
        var groups = new[]
        {
            CreateGroup("group1", "/snapsinks/zone1", new[] { "living-room", "kitchen" }),
            CreateGroup("group2", "/snapsinks/zone2", new[] { "bedroom" }), // Correct!
        };

        // Act
        var result = ValidateClientGrouping(clientId, zoneStreamId, groups);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ExpectedGroupId.Should().Be(expectedGroupId);
        result.ActualGroupId.Should().Be(expectedGroupId);
        result.Issue.Should().BeNull();

        _output.WriteLine("✅ Client in correct group validated successfully");
    }

    [Fact]
    public void ZoneGroupingLogic_WhenClientInWrongGroup_ShouldBeInvalid()
    {
        // Arrange - This tests the specific bug we fixed
        _output.WriteLine("🧪 Testing zone grouping logic - client in wrong group");

        var zoneStreamId = "/snapsinks/zone2";
        var clientId = "bedroom";

        // Bedroom client is in group1 (zone1's group) instead of group2 (zone2's group)
        var groups = new[]
        {
            CreateGroup("group1", "/snapsinks/zone1", new[] { "living-room", "kitchen", "bedroom" }), // Wrong!
            CreateGroup("group2", "/snapsinks/zone2", new string[0]), // Empty
        };

        // Act
        var result = ValidateClientGrouping(clientId, zoneStreamId, groups);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ExpectedGroupId.Should().Be("group2");
        result.ActualGroupId.Should().Be("group1");
        result.Issue.Should().Contain("bedroom is in group group1 but should be in group group2");

        _output.WriteLine("✅ Cross-zone grouping issue correctly detected");
    }

    [Fact]
    public void ZoneGroupingLogic_WhenClientNotFound_ShouldBeInvalid()
    {
        // Arrange
        _output.WriteLine("🧪 Testing zone grouping logic - client not found");

        var zoneStreamId = "/snapsinks/zone2";
        var clientId = "missing-client";

        var groups = new[]
        {
            CreateGroup("group1", "/snapsinks/zone1", new[] { "living-room", "kitchen" }),
            CreateGroup("group2", "/snapsinks/zone2", new[] { "bedroom" }),
        };

        // Act
        var result = ValidateClientGrouping(clientId, zoneStreamId, groups);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ExpectedGroupId.Should().Be("group2");
        result.ActualGroupId.Should().BeNull();
        result.Issue.Should().Contain("missing-client not found in any group");

        _output.WriteLine("✅ Missing client correctly detected");
    }

    #region Helper Methods

    private static SimpleGroup CreateGroup(string id, string streamId, string[] clientIds)
    {
        return new SimpleGroup
        {
            Id = id,
            StreamId = streamId,
            ClientIds = clientIds.ToList(),
        };
    }

    /// <summary>
    /// Simplified validation logic that mimics the fixed AnalyzeZoneGrouping method.
    /// This tests the core logic: clients should be in the group that matches their zone's stream.
    /// </summary>
    private static ValidationResult ValidateClientGrouping(string clientId, string zoneStreamId, SimpleGroup[] groups)
    {
        // Find the expected group for this zone (group with matching stream)
        var expectedGroup = groups.FirstOrDefault(g => g.StreamId == zoneStreamId);
        var expectedGroupId = expectedGroup?.Id;

        // Find which group the client is actually in
        var actualGroup = groups.FirstOrDefault(g => g.ClientIds.Contains(clientId));
        var actualGroupId = actualGroup?.Id;

        // Validate
        var isValid = actualGroupId == expectedGroupId && actualGroupId != null;

        string? issue = null;
        if (actualGroupId == null)
        {
            issue = $"Client {clientId} not found in any group";
        }
        else if (!isValid)
        {
            issue = $"Client {clientId} is in group {actualGroupId} but should be in group {expectedGroupId}";
        }

        return new ValidationResult
        {
            IsValid = isValid,
            ExpectedGroupId = expectedGroupId,
            ActualGroupId = actualGroupId,
            Issue = issue,
        };
    }

    #endregion

    #region Test Models

    private record SimpleGroup
    {
        public required string Id { get; init; }
        public required string StreamId { get; init; }
        public required List<string> ClientIds { get; init; }
    }

    private record ValidationResult
    {
        public required bool IsValid { get; init; }
        public required string? ExpectedGroupId { get; init; }
        public required string? ActualGroupId { get; init; }
        public required string? Issue { get; init; }
    }

    #endregion
}
