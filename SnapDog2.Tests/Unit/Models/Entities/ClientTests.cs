using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Models.Entities;

/// <summary>
/// Unit tests for the Client domain entity.
/// Tests client lifecycle, volume changes, status updates, and business logic.
/// </summary>
public class ClientTests
{
    private readonly MacAddress _validMacAddress = new("AA:BB:CC:DD:EE:FF");
    private readonly IpAddress _validIpAddress = new("192.168.1.100");
    private const string ValidId = "test-client";
    private const string ValidName = "Test Client";
    private const int ValidVolume = 50;

    [Fact]
    public void Create_WithValidParameters_ShouldCreateClient()
    {
        // Act
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);

        // Assert
        Assert.Equal(ValidId, client.Id);
        Assert.Equal(ValidName, client.Name);
        Assert.Equal(_validMacAddress, client.MacAddress);
        Assert.Equal(_validIpAddress, client.IpAddress);
        Assert.Equal(ClientStatus.Disconnected, client.Status);
        Assert.Equal(50, client.Volume);
        Assert.False(client.IsMuted);
        Assert.Null(client.ZoneId);
        Assert.Null(client.Description);
        Assert.Null(client.Location);
        Assert.Null(client.LatencyMs);
        Assert.True(client.CreatedAt <= DateTime.UtcNow);
        Assert.Null(client.UpdatedAt);
        Assert.Null(client.LastSeen);
    }

    [Fact]
    public void Create_WithCustomStatusAndVolume_ShouldCreateWithSpecifiedValues()
    {
        // Act
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress, ClientStatus.Connected, 75);

        // Assert
        Assert.Equal(ClientStatus.Connected, client.Status);
        Assert.Equal(75, client.Volume);
        Assert.NotNull(client.LastSeen);
        Assert.True(client.LastSeen <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidId_ShouldThrowArgumentException(string? invalidId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => Client.Create(invalidId!, ValidName, _validMacAddress, _validIpAddress)
        );
        Assert.Contains("Client ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => Client.Create(ValidId, invalidName!, _validMacAddress, _validIpAddress)
        );
        Assert.Contains("Client name cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-10)]
    [InlineData(150)]
    public void Create_WithInvalidVolume_ShouldThrowArgumentException(int invalidVolume)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () =>
                Client.Create(
                    ValidId,
                    ValidName,
                    _validMacAddress,
                    _validIpAddress,
                    ClientStatus.Disconnected,
                    invalidVolume
                )
        );
        Assert.Contains("Volume must be between 0 and 100", exception.Message);
    }

    [Fact]
    public void WithStatus_WhenConnecting_ShouldUpdateStatusAndSetLastSeen()
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);
        var originalCreatedAt = client.CreatedAt;

        // Act
        var updatedClient = client.WithStatus(ClientStatus.Connected);

        // Assert
        Assert.Equal(ClientStatus.Connected, updatedClient.Status);
        Assert.NotNull(updatedClient.LastSeen);
        Assert.True(updatedClient.LastSeen <= DateTime.UtcNow);
        Assert.NotNull(updatedClient.UpdatedAt);
        Assert.True(updatedClient.UpdatedAt >= originalCreatedAt);
        Assert.Equal(originalCreatedAt, updatedClient.CreatedAt);
    }

    [Fact]
    public void WithStatus_WhenDisconnecting_ShouldUpdateStatusAndKeepLastSeen()
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress, ClientStatus.Connected);
        var originalLastSeen = client.LastSeen;

        // Act
        var updatedClient = client.WithStatus(ClientStatus.Disconnected);

        // Assert
        Assert.Equal(ClientStatus.Disconnected, updatedClient.Status);
        Assert.Equal(originalLastSeen, updatedClient.LastSeen); // Should preserve last seen time
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(100)]
    public void WithVolume_WithValidVolume_ShouldUpdateVolume(int newVolume)
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);

        // Act
        var updatedClient = client.WithVolume(newVolume);

        // Assert
        Assert.Equal(newVolume, updatedClient.Volume);
        Assert.NotNull(updatedClient.UpdatedAt);
        Assert.Equal(client.Id, updatedClient.Id);
        Assert.Equal(client.Name, updatedClient.Name);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-10)]
    [InlineData(150)]
    public void WithVolume_WithInvalidVolume_ShouldThrowArgumentException(int invalidVolume)
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => client.WithVolume(invalidVolume));
        Assert.Contains("Volume must be between 0 and 100", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithMute_ShouldUpdateMuteStatus(bool muteStatus)
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);

        // Act
        var updatedClient = client.WithMute(muteStatus);

        // Assert
        Assert.Equal(muteStatus, updatedClient.IsMuted);
        Assert.NotNull(updatedClient.UpdatedAt);
        Assert.Equal(client.Volume, updatedClient.Volume); // Volume should remain unchanged
    }

    [Fact]
    public void WithZone_ShouldUpdateZoneAssignment()
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);
        const string zoneId = "living-room";

        // Act
        var updatedClient = client.WithZone(zoneId);

        // Assert
        Assert.Equal(zoneId, updatedClient.ZoneId);
        Assert.NotNull(updatedClient.UpdatedAt);
        Assert.True(updatedClient.IsAssignedToZone);
    }

    [Fact]
    public void WithZone_WithNull_ShouldRemoveZoneAssignment()
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress).WithZone("test-zone");

        // Act
        var updatedClient = client.WithZone(null);

        // Assert
        Assert.Null(updatedClient.ZoneId);
        Assert.False(updatedClient.IsAssignedToZone);
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Fact]
    public void WithIpAddress_ShouldUpdateIpAddress()
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);
        var newIpAddress = new IpAddress("192.168.1.200");

        // Act
        var updatedClient = client.WithIpAddress(newIpAddress);

        // Assert
        Assert.Equal(newIpAddress, updatedClient.IpAddress);
        Assert.NotNull(updatedClient.UpdatedAt);
        Assert.Equal(client.MacAddress, updatedClient.MacAddress); // MAC should remain unchanged
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(500)]
    public void WithLatency_WithValidLatency_ShouldUpdateLatency(int latencyMs)
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);

        // Act
        var updatedClient = client.WithLatency(latencyMs);

        // Assert
        Assert.Equal(latencyMs, updatedClient.LatencyMs);
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Fact]
    public void WithLatency_WithNegativeLatency_ShouldThrowArgumentException()
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => client.WithLatency(-10));
        Assert.Contains("Latency cannot be negative", exception.Message);
    }

    [Fact]
    public void IsConnected_WhenStatusIsConnected_ShouldReturnTrue()
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress, ClientStatus.Connected);

        // Act & Assert
        Assert.True(client.IsConnected);
        Assert.False(client.IsDisconnected);
        Assert.False(client.HasError);
    }

    [Fact]
    public void IsDisconnected_WhenStatusIsDisconnected_ShouldReturnTrue()
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress, ClientStatus.Disconnected);

        // Act & Assert
        Assert.True(client.IsDisconnected);
        Assert.False(client.IsConnected);
        Assert.False(client.HasError);
    }

    [Fact]
    public void HasError_WhenStatusIsError_ShouldReturnTrue()
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress, ClientStatus.Error);

        // Act & Assert
        Assert.True(client.HasError);
        Assert.False(client.IsConnected);
        Assert.False(client.IsDisconnected);
    }

    [Theory]
    [InlineData(0, false, true)]
    [InlineData(50, false, false)]
    [InlineData(100, false, false)]
    [InlineData(0, true, true)]
    [InlineData(50, true, true)]
    [InlineData(100, true, true)]
    public void IsSilent_WithDifferentVolumeAndMuteStates_ShouldReturnCorrectValue(
        int volume,
        bool isMuted,
        bool expectedSilent
    )
    {
        // Arrange
        var client = Client
            .Create(ValidId, ValidName, _validMacAddress, _validIpAddress)
            .WithVolume(volume)
            .WithMute(isMuted);

        // Act & Assert
        Assert.Equal(expectedSilent, client.IsSilent);
    }

    [Theory]
    [InlineData(50, false, 50)]
    [InlineData(75, false, 75)]
    [InlineData(50, true, 0)]
    [InlineData(75, true, 0)]
    [InlineData(0, false, 0)]
    [InlineData(0, true, 0)]
    public void EffectiveVolume_WithDifferentVolumeAndMuteStates_ShouldReturnCorrectValue(
        int volume,
        bool isMuted,
        int expectedEffectiveVolume
    )
    {
        // Arrange
        var client = Client
            .Create(ValidId, ValidName, _validMacAddress, _validIpAddress)
            .WithVolume(volume)
            .WithMute(isMuted);

        // Act & Assert
        Assert.Equal(expectedEffectiveVolume, client.EffectiveVolume);
    }

    [Fact]
    public void Client_Immutability_ShouldCreateNewInstancesOnUpdate()
    {
        // Arrange
        var originalClient = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);

        // Act
        var updatedClient = originalClient.WithStatus(ClientStatus.Connected);

        // Assert
        Assert.NotSame(originalClient, updatedClient);
        Assert.Equal(ClientStatus.Disconnected, originalClient.Status);
        Assert.Equal(ClientStatus.Connected, updatedClient.Status);
        Assert.Null(originalClient.UpdatedAt);
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Fact]
    public void Client_WithComplexScenario_ShouldMaintainConsistency()
    {
        // Arrange
        var client = Client.Create(
            "living-room-speaker",
            "Living Room Speaker",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        // Act - Simulate a complex scenario with multiple updates
        var configuredClient = client
            .WithZone("living-room")
            .WithStatus(ClientStatus.Connected)
            .WithVolume(75)
            .WithLatency(50);

        var mutedClient = configuredClient.WithMute(true);
        var volumeAdjustedClient = mutedClient.WithVolume(90).WithMute(false);

        // Assert
        Assert.Equal("living-room-speaker", volumeAdjustedClient.Id);
        Assert.Equal("Living Room Speaker", volumeAdjustedClient.Name);
        Assert.Equal("living-room", volumeAdjustedClient.ZoneId);
        Assert.Equal(ClientStatus.Connected, volumeAdjustedClient.Status);
        Assert.Equal(90, volumeAdjustedClient.Volume);
        Assert.False(volumeAdjustedClient.IsMuted);
        Assert.Equal(50, volumeAdjustedClient.LatencyMs);
        Assert.True(volumeAdjustedClient.IsConnected);
        Assert.True(volumeAdjustedClient.IsAssignedToZone);
        Assert.False(volumeAdjustedClient.IsSilent);
        Assert.Equal(90, volumeAdjustedClient.EffectiveVolume);
        Assert.NotNull(volumeAdjustedClient.UpdatedAt);
        Assert.True(volumeAdjustedClient.UpdatedAt >= client.CreatedAt);
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("2001:db8::1")]
    public void WithIpAddress_WithDifferentIpFormats_ShouldUpdateCorrectly(string ipAddress)
    {
        // Arrange
        var client = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);
        var newIpAddress = new IpAddress(ipAddress);

        // Act
        var updatedClient = client.WithIpAddress(newIpAddress);

        // Assert
        Assert.Equal(newIpAddress, updatedClient.IpAddress);
        Assert.Equal(ipAddress, updatedClient.IpAddress.ToString());
    }

    [Fact]
    public void Client_EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var client1 = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);
        var client2 = Client.Create(ValidId, ValidName, _validMacAddress, _validIpAddress);
        var client3 = Client.Create("different-id", ValidName, _validMacAddress, _validIpAddress);

        // Act & Assert
        Assert.Equal(client1.Id, client2.Id);
        Assert.NotEqual(client1, client3);
        Assert.NotSame(client1, client2);
    }
}
