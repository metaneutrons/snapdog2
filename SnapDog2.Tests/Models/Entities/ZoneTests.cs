using SnapDog2.Core.Models.Entities;
using Xunit;

namespace SnapDog2.Tests.Models.Entities;

/// <summary>
/// Unit tests for the Zone entity.
/// Tests creation, validation, manipulation, and business logic.
/// </summary>
public class ZoneTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateZone()
    {
        // Arrange
        var id = "test-zone";
        var name = "Test Zone";
        var description = "A test zone for unit testing";

        // Act
        var zone = Zone.Create(id, name, description);

        // Assert
        Assert.Equal(id, zone.Id);
        Assert.Equal(name, zone.Name);
        Assert.Equal(description, zone.Description);
        Assert.True(zone.IsEnabled);
        Assert.Empty(zone.ClientIds);
        Assert.Null(zone.CurrentStreamId);
        Assert.Equal(50, zone.DefaultVolume);
        Assert.Equal(0, zone.MinVolume);
        Assert.Equal(100, zone.MaxVolume);
        Assert.True(zone.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("", "Valid Name")]
    [InlineData("valid-id", "")]
    public void Create_WithInvalidParameters_ShouldThrowArgumentException(string id, string name)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Zone.Create(id, name));
    }

    [Fact]
    public void Create_WithNullId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Zone.Create(null!, "Valid Name"));
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Zone.Create("valid-id", null!));
    }

    [Fact]
    public void WithAddedClient_WithValidClientId_ShouldAddClient()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");
        var clientId = "client-1";

        // Act
        var updatedZone = zone.WithAddedClient(clientId);

        // Assert
        Assert.Single(updatedZone.ClientIds);
        Assert.Contains(clientId, updatedZone.ClientIds);
        Assert.True(updatedZone.HasClients);
        Assert.Equal(1, updatedZone.ClientCount);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Fact]
    public void WithAddedClient_WithDuplicateClientId_ShouldThrowArgumentException()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone").WithAddedClient("client-1");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => zone.WithAddedClient("client-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithAddedClient_WithInvalidClientId_ShouldThrowArgumentException(string clientId)
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => zone.WithAddedClient(clientId));
    }

    [Fact]
    public void WithAddedClient_WithNullClientId_ShouldThrowArgumentException()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => zone.WithAddedClient(null!));
    }

    [Fact]
    public void WithRemovedClient_WithExistingClientId_ShouldRemoveClient()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone").WithAddedClient("client-1").WithAddedClient("client-2");

        // Act
        var updatedZone = zone.WithRemovedClient("client-1");

        // Assert
        Assert.Single(updatedZone.ClientIds);
        Assert.DoesNotContain("client-1", updatedZone.ClientIds);
        Assert.Contains("client-2", updatedZone.ClientIds);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Fact]
    public void WithRemovedClient_WithNonExistentClientId_ShouldThrowArgumentException()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => zone.WithRemovedClient("non-existent"));
    }

    [Fact]
    public void WithClients_WithValidClientIds_ShouldSetClients()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");
        var clientIds = new[] { "client-1", "client-2", "client-3" };

        // Act
        var updatedZone = zone.WithClients(clientIds);

        // Assert
        Assert.Equal(3, updatedZone.ClientCount);
        Assert.Equal(clientIds, updatedZone.ClientIds);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Fact]
    public void WithClients_WithDuplicateClientIds_ShouldThrowArgumentException()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");
        var clientIds = new[] { "client-1", "client-2", "client-1" }; // Duplicate

        // Act & Assert
        Assert.Throws<ArgumentException>(() => zone.WithClients(clientIds));
    }

    [Fact]
    public void WithClients_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => zone.WithClients(null!));
    }

    [Fact]
    public void WithCurrentStream_WithValidStreamId_ShouldSetCurrentStream()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");
        var streamId = "stream-1";

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
        var zone = Zone.Create("test-zone", "Test Zone").WithCurrentStream("stream-1");

        // Act
        var updatedZone = zone.WithCurrentStream(null);

        // Assert
        Assert.Null(updatedZone.CurrentStreamId);
        Assert.False(updatedZone.HasCurrentStream);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Fact]
    public void WithEnabled_WithFalse_ShouldDisableZone()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act
        var updatedZone = zone.WithEnabled(false);

        // Assert
        Assert.False(updatedZone.IsEnabled);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Fact]
    public void WithVolumeSettings_WithValidSettings_ShouldUpdateVolumeSettings()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");
        var defaultVolume = 75;
        var minVolume = 10;
        var maxVolume = 90;

        // Act
        var updatedZone = zone.WithVolumeSettings(defaultVolume, minVolume, maxVolume);

        // Assert
        Assert.Equal(defaultVolume, updatedZone.DefaultVolume);
        Assert.Equal(minVolume, updatedZone.MinVolume);
        Assert.Equal(maxVolume, updatedZone.MaxVolume);
        Assert.NotNull(updatedZone.UpdatedAt);
    }

    [Theory]
    [InlineData(150, 0, 100)] // Default > Max
    [InlineData(50, -1, 100)] // Min < 0
    [InlineData(50, 0, 101)] // Max > 100
    [InlineData(50, 60, 50)] // Min > Max
    [InlineData(30, 40, 80)] // Default < Min
    [InlineData(90, 20, 80)] // Default > Max
    public void WithVolumeSettings_WithInvalidSettings_ShouldThrowArgumentException(
        int defaultVolume,
        int minVolume,
        int maxVolume
    )
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => zone.WithVolumeSettings(defaultVolume, minVolume, maxVolume));
    }

    [Fact]
    public void ContainsClient_WithExistingClient_ShouldReturnTrue()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone").WithAddedClient("client-1");

        // Act & Assert
        Assert.True(zone.ContainsClient("client-1"));
    }

    [Fact]
    public void ContainsClient_WithNonExistingClient_ShouldReturnFalse()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act & Assert
        Assert.False(zone.ContainsClient("non-existent"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ContainsClient_WithInvalidClientId_ShouldReturnFalse(string clientId)
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act & Assert
        Assert.False(zone.ContainsClient(clientId));
    }

    [Fact]
    public void ContainsClient_WithNullClientId_ShouldReturnFalse()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act & Assert
        Assert.False(zone.ContainsClient(null!));
    }

    [Fact]
    public void IsActive_WithEnabledZoneAndClients_ShouldReturnTrue()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone").WithAddedClient("client-1").WithEnabled(true);

        // Act & Assert
        Assert.True(zone.IsActive);
    }

    [Fact]
    public void IsActive_WithDisabledZone_ShouldReturnFalse()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone").WithAddedClient("client-1").WithEnabled(false);

        // Act & Assert
        Assert.False(zone.IsActive);
    }

    [Fact]
    public void IsActive_WithEnabledZoneButNoClients_ShouldReturnFalse()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone").WithEnabled(true);

        // Act & Assert
        Assert.False(zone.IsActive);
    }

    [Fact]
    public void HasClients_WithNoClients_ShouldReturnFalse()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act & Assert
        Assert.False(zone.HasClients);
        Assert.Equal(0, zone.ClientCount);
    }

    [Fact]
    public void HasClients_WithClients_ShouldReturnTrue()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone").WithAddedClient("client-1");

        // Act & Assert
        Assert.True(zone.HasClients);
        Assert.Equal(1, zone.ClientCount);
    }

    [Fact]
    public void HasCurrentStream_WithNoStream_ShouldReturnFalse()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act & Assert
        Assert.False(zone.HasCurrentStream);
    }

    [Fact]
    public void HasCurrentStream_WithStream_ShouldReturnTrue()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone").WithCurrentStream("stream-1");

        // Act & Assert
        Assert.True(zone.HasCurrentStream);
    }

    [Fact]
    public void Zone_ImmutabilityTest_ShouldNotModifyOriginal()
    {
        // Arrange
        var originalZone = Zone.Create("test-zone", "Test Zone");
        var originalClientCount = originalZone.ClientCount;
        var originalEnabled = originalZone.IsEnabled;

        // Act
        var modifiedZone = originalZone.WithAddedClient("client-1").WithEnabled(false);

        // Assert
        Assert.Equal(originalClientCount, originalZone.ClientCount);
        Assert.Equal(originalEnabled, originalZone.IsEnabled);
        Assert.NotEqual(originalZone.ClientCount, modifiedZone.ClientCount);
        Assert.NotEqual(originalZone.IsEnabled, modifiedZone.IsEnabled);
    }

    [Fact]
    public void Zone_DefaultValues_ShouldHaveExpectedDefaults()
    {
        // Arrange & Act
        var zone = Zone.Create("test-zone", "Test Zone");

        // Assert
        Assert.Equal("#007bff", zone.Color);
        Assert.Equal("speaker", zone.Icon);
        Assert.Equal(50, zone.DefaultVolume);
        Assert.Equal(0, zone.MinVolume);
        Assert.Equal(100, zone.MaxVolume);
        Assert.True(zone.IsEnabled);
        Assert.Equal(1, zone.Priority);
        Assert.True(zone.StereoEnabled);
        Assert.Equal("high", zone.AudioQuality);
        Assert.True(zone.GroupingEnabled);
        Assert.Empty(zone.ClientIds);
        Assert.Null(zone.CurrentStreamId);
    }

    [Fact]
    public void Zone_MultipleClientOperations_ShouldMaintainCorrectState()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");

        // Act
        var result = zone.WithAddedClient("client-1")
            .WithAddedClient("client-2")
            .WithAddedClient("client-3")
            .WithRemovedClient("client-2")
            .WithCurrentStream("stream-1");

        // Assert
        Assert.Equal(2, result.ClientCount);
        Assert.Contains("client-1", result.ClientIds);
        Assert.Contains("client-3", result.ClientIds);
        Assert.DoesNotContain("client-2", result.ClientIds);
        Assert.Equal("stream-1", result.CurrentStreamId);
        Assert.True(result.IsActive);
    }
}
