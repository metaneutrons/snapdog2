using SnapDog2.Core.Models.Entities;
using Xunit;

namespace SnapDog2.Tests.Models.Entities;

/// <summary>
/// Unit tests for the Zone domain entity.
/// Tests zone management, client assignments, stream control, and business logic.
/// </summary>
public class ZoneTests
{
    private const string ValidId = "test-zone";
    private const string ValidName = "Test Zone";
    private const string ValidDescription = "Test Description";

    [Fact]
    public void Create_WithValidParameters_ShouldCreateZone()
    {
        // Act
        var zone = Zone.Create(ValidId, ValidName);

        // Assert
        Assert.Equal(ValidId, zone.Id);
        Assert.Equal(ValidName, zone.Name);
        Assert.Null(zone.Description);
        Assert.Empty(zone.ClientIds);
        Assert.Null(zone.CurrentStreamId);
        Assert.Equal("#007bff", zone.Color);
        Assert.Equal("speaker", zone.Icon);
        Assert.Equal(50, zone.DefaultVolume);
        Assert.Equal(100, zone.MaxVolume);
        Assert.Equal(0, zone.MinVolume);
        Assert.True(zone.IsEnabled);
        Assert.Equal(1, zone.Priority);
        Assert.Null(zone.Tags);
        Assert.True(zone.StereoEnabled);
        Assert.Equal("high", zone.AudioQuality);
        Assert.True(zone.GroupingEnabled);
        Assert.True(zone.CreatedAt <= DateTime.UtcNow);
        Assert.Null(zone.UpdatedAt);
    }

    [Fact]
    public void Create_WithDescription_ShouldCreateZoneWithDescription()
    {
        // Act
        var zone = Zone.Create(ValidId, ValidName, ValidDescription);

        // Assert
        Assert.Equal(ValidDescription, zone.Description);
        Assert.Equal(ValidId, zone.Id);
        Assert.Equal(ValidName, zone.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidId_ShouldThrowArgumentException(string? invalidId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Zone.Create(invalidId!, ValidName));
        Assert.Contains("Zone ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Zone.Create(ValidId, invalidName!));
        Assert.Contains("Zone name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void WithAddedClient_WithValidClientId_ShouldAddClient()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);
        const string clientId = "test-client";

        // Act
        var updatedZone = zone.WithAddedClient(clientId);

        // Assert
        Assert.Contains(clientId, updatedZone.ClientIds);
        Assert.Single(updatedZone.ClientIds);
        Assert.NotNull(updatedZone.UpdatedAt);
        Assert.True(updatedZone.HasClients);
        Assert.Equal(1, updatedZone.ClientCount);
        Assert.True(updatedZone.ContainsClient(clientId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void WithAddedClient_WithInvalidClientId_ShouldThrowArgumentException(string? invalidClientId)
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => zone.WithAddedClient(invalidClientId!));
        Assert.Contains("Client ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public void WithAddedClient_WithDuplicateClientId_ShouldThrowArgumentException()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName).WithAddedClient("test-client");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => zone.WithAddedClient("test-client"));
        Assert.Contains("Client 'test-client' is already assigned to zone", exception.Message);
    }

    [Fact]
    public void WithRemovedClient_WithExistingClientId_ShouldRemoveClient()
    {
        // Arrange
        const string clientId = "test-client";
        var zone = Zone.Create(ValidId, ValidName).WithAddedClient(clientId);

        // Act
        var updatedZone = zone.WithRemovedClient(clientId);

        // Assert
        Assert.DoesNotContain(clientId, updatedZone.ClientIds);
        Assert.Empty(updatedZone.ClientIds);
        Assert.NotNull(updatedZone.UpdatedAt);
        Assert.False(updatedZone.HasClients);
        Assert.Equal(0, updatedZone.ClientCount);
        Assert.False(updatedZone.ContainsClient(clientId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void WithRemovedClient_WithInvalidClientId_ShouldThrowArgumentException(string? invalidClientId)
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => zone.WithRemovedClient(invalidClientId!));
        Assert.Contains("Client ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public void WithRemovedClient_WithNonExistentClientId_ShouldThrowArgumentException()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => zone.WithRemovedClient("non-existent"));
        Assert.Contains("Client 'non-existent' is not assigned to zone", exception.Message);
    }

    [Fact]
    public void WithClients_WithValidClientIds_ShouldSetClients()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);
        var clientIds = new[] { "client-1", "client-2", "client-3" };

        // Act
        var updatedZone = zone.WithClients(clientIds);

        // Assert
        Assert.Equal(3, updatedZone.ClientIds.Count);
        Assert.Contains("client-1", updatedZone.ClientIds);
        Assert.Contains("client-2", updatedZone.ClientIds);
        Assert.Contains("client-3", updatedZone.ClientIds);
        Assert.NotNull(updatedZone.UpdatedAt);
        Assert.True(updatedZone.HasClients);
        Assert.Equal(3, updatedZone.ClientCount);
    }

    [Fact]
    public void WithClients_WithEmptyList_ShouldClearClients()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName).WithAddedClient("client-1").WithAddedClient("client-2");

        // Act
        var updatedZone = zone.WithClients(Array.Empty<string>());

        // Assert
        Assert.Empty(updatedZone.ClientIds);
        Assert.False(updatedZone.HasClients);
        Assert.Equal(0, updatedZone.ClientCount);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Fact]
    public void WithClients_WithNullList_ShouldThrowArgumentNullException()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => zone.WithClients(null!));
    }

    [Fact]
    public void WithClients_WithDuplicateClientIds_ShouldThrowArgumentException()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);
        var clientIds = new[] { "client-1", "client-2", "client-1" }; // Duplicate

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => zone.WithClients(clientIds));
        Assert.Contains("Duplicate client IDs found: client-1", exception.Message);
    }

    [Fact]
    public void WithClients_WithNullOrEmptyClientIds_ShouldFilterThem()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);
        var clientIds = new[] { "client-1", "", "client-2", "   ", "client-3" };

        // Act
        var updatedZone = zone.WithClients(clientIds);

        // Assert
        Assert.Equal(3, updatedZone.ClientIds.Count);
        Assert.Contains("client-1", updatedZone.ClientIds);
        Assert.Contains("client-2", updatedZone.ClientIds);
        Assert.Contains("client-3", updatedZone.ClientIds);
    }

    [Fact]
    public void WithCurrentStream_WithValidStreamId_ShouldSetCurrentStream()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);
        const string streamId = "test-stream";

        // Act
        var updatedZone = zone.WithCurrentStream(streamId);

        // Assert
        Assert.Equal(streamId, updatedZone.CurrentStreamId);
        Assert.True(updatedZone.HasCurrentStream);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Fact]
    public void WithCurrentStream_WithNull_ShouldClearCurrentStream()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName).WithCurrentStream("test-stream");

        // Act
        var updatedZone = zone.WithCurrentStream(null);

        // Assert
        Assert.Null(updatedZone.CurrentStreamId);
        Assert.False(updatedZone.HasCurrentStream);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithEnabled_ShouldUpdateEnabledStatus(bool enabled)
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);

        // Act
        var updatedZone = zone.WithEnabled(enabled);

        // Assert
        Assert.Equal(enabled, updatedZone.IsEnabled);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Theory]
    [InlineData(25, 0, 100)]
    [InlineData(50, 10, 90)]
    [InlineData(75, 25, 100)]
    [InlineData(0, 0, 50)]
    [InlineData(100, 50, 100)]
    public void WithVolumeSettings_WithValidSettings_ShouldUpdateVolumeSettings(
        int defaultVolume,
        int minVolume,
        int maxVolume
    )
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);

        // Act
        var updatedZone = zone.WithVolumeSettings(defaultVolume, minVolume, maxVolume);

        // Assert
        Assert.Equal(defaultVolume, updatedZone.DefaultVolume);
        Assert.Equal(minVolume, updatedZone.MinVolume);
        Assert.Equal(maxVolume, updatedZone.MaxVolume);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Theory]
    [InlineData(-1, 0, 100)] // Invalid min volume
    [InlineData(101, 0, 100)] // Invalid min volume
    [InlineData(50, -1, 100)] // Invalid min volume
    [InlineData(50, 101, 100)] // Invalid min volume
    [InlineData(50, 0, -1)] // Invalid max volume
    [InlineData(50, 0, 101)] // Invalid max volume
    public void WithVolumeSettings_WithInvalidVolumeRanges_ShouldThrowArgumentException(
        int defaultVolume,
        int minVolume,
        int maxVolume
    )
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => zone.WithVolumeSettings(defaultVolume, minVolume, maxVolume));
    }

    [Fact]
    public void WithVolumeSettings_WithMinGreaterThanMax_ShouldThrowArgumentException()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => zone.WithVolumeSettings(50, 75, 25));
        Assert.Contains("Minimum volume cannot be greater than maximum volume", exception.Message);
    }

    [Fact]
    public void WithVolumeSettings_WithDefaultOutsideRange_ShouldThrowArgumentException()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => zone.WithVolumeSettings(90, 10, 80));
        Assert.Contains("Default volume must be between 10 and 80", exception.Message);
    }

    [Fact]
    public void IsActive_WhenEnabledAndHasClients_ShouldReturnTrue()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName).WithEnabled(true).WithAddedClient("test-client");

        // Act & Assert
        Assert.True(zone.IsActive);
    }

    [Fact]
    public void IsActive_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName).WithEnabled(false).WithAddedClient("test-client");

        // Act & Assert
        Assert.False(zone.IsActive);
    }

    [Fact]
    public void IsActive_WhenEnabledButNoClients_ShouldReturnFalse()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName).WithEnabled(true);

        // Act & Assert
        Assert.False(zone.IsActive);
    }

    [Theory]
    [InlineData("client-1", true)]
    [InlineData("client-2", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ContainsClient_WithDifferentClientIds_ShouldReturnCorrectValue(string? clientId, bool expectedResult)
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName).WithAddedClient("client-1");

        // Act & Assert
        Assert.Equal(expectedResult, zone.ContainsClient(clientId!));
    }

    [Fact]
    public void Zone_Immutability_ShouldCreateNewInstancesOnUpdate()
    {
        // Arrange
        var originalZone = Zone.Create(ValidId, ValidName);

        // Act
        var updatedZone = originalZone.WithAddedClient("test-client");

        // Assert
        Assert.NotSame(originalZone, updatedZone);
        Assert.Empty(originalZone.ClientIds);
        Assert.Single(updatedZone.ClientIds);
        Assert.Null(originalZone.UpdatedAt);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Fact]
    public void Zone_WithComplexScenario_ShouldMaintainConsistency()
    {
        // Arrange
        var zone = Zone.Create("living-room", "Living Room", "Main entertainment area");

        // Act - Simulate a complex scenario with multiple updates
        var configuredZone = zone.WithAddedClient("speaker-1")
            .WithAddedClient("speaker-2")
            .WithCurrentStream("jazz-stream")
            .WithVolumeSettings(60, 10, 90)
            .WithEnabled(true);

        var updatedZone = configuredZone.WithRemovedClient("speaker-2").WithAddedClient("speaker-3");

        // Assert
        Assert.Equal("living-room", updatedZone.Id);
        Assert.Equal("Living Room", updatedZone.Name);
        Assert.Equal("Main entertainment area", updatedZone.Description);
        Assert.Equal(2, updatedZone.ClientCount);
        Assert.Contains("speaker-1", updatedZone.ClientIds);
        Assert.Contains("speaker-3", updatedZone.ClientIds);
        Assert.DoesNotContain("speaker-2", updatedZone.ClientIds);
        Assert.Equal("jazz-stream", updatedZone.CurrentStreamId);
        Assert.True(updatedZone.HasCurrentStream);
        Assert.Equal(60, updatedZone.DefaultVolume);
        Assert.Equal(10, updatedZone.MinVolume);
        Assert.Equal(90, updatedZone.MaxVolume);
        Assert.True(updatedZone.IsEnabled);
        Assert.True(updatedZone.IsActive);
        Assert.True(updatedZone.HasClients);
        Assert.NotNull(updatedZone.UpdatedAt);
        Assert.True(updatedZone.UpdatedAt >= zone.CreatedAt);
    }

    [Fact]
    public void Zone_WithMultipleClients_ShouldHandleCorrectly()
    {
        // Arrange
        var zone = Zone.Create(ValidId, ValidName);
        var clientIds = new[] { "client-1", "client-2", "client-3", "client-4", "client-5" };

        // Act
        var updatedZone = zone.WithClients(clientIds);

        // Assert
        Assert.Equal(5, updatedZone.ClientCount);
        Assert.True(updatedZone.HasClients);
        foreach (var clientId in clientIds)
        {
            Assert.True(updatedZone.ContainsClient(clientId));
        }
    }

    [Fact]
    public void Zone_EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var zone1 = Zone.Create(ValidId, ValidName);
        var zone2 = Zone.Create(ValidId, ValidName);
        var zone3 = Zone.Create("different-id", ValidName);

        // Act & Assert
        Assert.Equal(zone1.Id, zone2.Id);
        Assert.NotEqual(zone1, zone3);
        Assert.NotSame(zone1, zone2);
    }

    [Fact]
    public void Zone_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var zone = Zone.Create(ValidId, ValidName);

        // Assert
        Assert.Equal("#007bff", zone.Color);
        Assert.Equal("speaker", zone.Icon);
        Assert.Equal(50, zone.DefaultVolume);
        Assert.Equal(100, zone.MaxVolume);
        Assert.Equal(0, zone.MinVolume);
        Assert.True(zone.IsEnabled);
        Assert.Equal(1, zone.Priority);
        Assert.True(zone.StereoEnabled);
        Assert.Equal("high", zone.AudioQuality);
        Assert.True(zone.GroupingEnabled);
    }
}
