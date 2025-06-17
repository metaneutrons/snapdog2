using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Models.Entities;

/// <summary>
/// Unit tests for the Client entity.
/// Tests creation, validation, manipulation, and business logic.
/// </summary>
public class ClientTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateClient()
    {
        // Arrange
        var id = "test-client";
        var name = "Test Client";
        var macAddress = new MacAddress("AA:BB:CC:DD:EE:FF");
        var ipAddress = new IpAddress("192.168.1.100");
        var status = ClientStatus.Connected;
        var volume = 75;

        // Act
        var client = Client.Create(id, name, macAddress, ipAddress, status, volume);

        // Assert
        Assert.Equal(id, client.Id);
        Assert.Equal(name, client.Name);
        Assert.Equal(macAddress, client.MacAddress);
        Assert.Equal(ipAddress, client.IpAddress);
        Assert.Equal(status, client.Status);
        Assert.Equal(volume, client.Volume);
        Assert.False(client.IsMuted);
        Assert.Null(client.ZoneId);
        Assert.True(client.CreatedAt <= DateTime.UtcNow);
        Assert.NotNull(client.LastSeen);
    }

    [Fact]
    public void Create_WithDefaultParameters_ShouldUseDefaults()
    {
        // Arrange
        var id = "test-client";
        var name = "Test Client";
        var macAddress = new MacAddress("AA:BB:CC:DD:EE:FF");
        var ipAddress = new IpAddress("192.168.1.100");

        // Act
        var client = Client.Create(id, name, macAddress, ipAddress);

        // Assert
        Assert.Equal(ClientStatus.Disconnected, client.Status);
        Assert.Equal(50, client.Volume);
        Assert.False(client.IsMuted);
        Assert.Null(client.LastSeen);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidId_ShouldThrowArgumentException(string id)
    {
        // Arrange
        var macAddress = new MacAddress("AA:BB:CC:DD:EE:FF");
        var ipAddress = new IpAddress("192.168.1.100");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Client.Create(id, "Valid Name", macAddress, ipAddress));
    }

    [Fact]
    public void Create_WithNullId_ShouldThrowArgumentException()
    {
        // Arrange
        var macAddress = new MacAddress("AA:BB:CC:DD:EE:FF");
        var ipAddress = new IpAddress("192.168.1.100");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Client.Create(null!, "Valid Name", macAddress, ipAddress));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var macAddress = new MacAddress("AA:BB:CC:DD:EE:FF");
        var ipAddress = new IpAddress("192.168.1.100");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Client.Create("valid-id", name, macAddress, ipAddress));
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var macAddress = new MacAddress("AA:BB:CC:DD:EE:FF");
        var ipAddress = new IpAddress("192.168.1.100");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Client.Create("valid-id", null!, macAddress, ipAddress));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-10)]
    [InlineData(150)]
    public void Create_WithInvalidVolume_ShouldThrowArgumentException(int volume)
    {
        // Arrange
        var macAddress = new MacAddress("AA:BB:CC:DD:EE:FF");
        var ipAddress = new IpAddress("192.168.1.100");

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => Client.Create("valid-id", "Valid Name", macAddress, ipAddress, volume: volume)
        );
    }

    [Fact]
    public void WithStatus_WithConnectedStatus_ShouldUpdateStatusAndLastSeen()
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100"),
            ClientStatus.Disconnected
        );

        // Act
        var updatedClient = client.WithStatus(ClientStatus.Connected);

        // Assert
        Assert.Equal(ClientStatus.Connected, updatedClient.Status);
        Assert.True(updatedClient.IsConnected);
        Assert.False(updatedClient.IsDisconnected);
        Assert.NotNull(updatedClient.LastSeen);
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Fact]
    public void WithStatus_WithDisconnectedStatus_ShouldUpdateStatusKeepLastSeen()
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100"),
            ClientStatus.Connected
        );
        var originalLastSeen = client.LastSeen;

        // Act
        var updatedClient = client.WithStatus(ClientStatus.Disconnected);

        // Assert
        Assert.Equal(ClientStatus.Disconnected, updatedClient.Status);
        Assert.False(updatedClient.IsConnected);
        Assert.True(updatedClient.IsDisconnected);
        Assert.Equal(originalLastSeen, updatedClient.LastSeen);
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
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        // Act
        var updatedClient = client.WithVolume(newVolume);

        // Assert
        Assert.Equal(newVolume, updatedClient.Volume);
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-10)]
    [InlineData(150)]
    public void WithVolume_WithInvalidVolume_ShouldThrowArgumentException(int newVolume)
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => client.WithVolume(newVolume));
    }

    [Fact]
    public void WithMute_WithTrue_ShouldMuteClient()
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100"),
            volume: 75
        );

        // Act
        var mutedClient = client.WithMute(true);

        // Assert
        Assert.True(mutedClient.IsMuted);
        Assert.True(mutedClient.IsSilent);
        Assert.Equal(0, mutedClient.EffectiveVolume);
        Assert.Equal(75, mutedClient.Volume); // Original volume preserved
        Assert.NotNull(mutedClient.UpdatedAt);
    }

    [Fact]
    public void WithMute_WithFalse_ShouldUnmuteClient()
    {
        // Arrange
        var client = Client
            .Create(
                "test-client",
                "Test Client",
                new MacAddress("AA:BB:CC:DD:EE:FF"),
                new IpAddress("192.168.1.100"),
                volume: 75
            )
            .WithMute(true);

        // Act
        var unmutedClient = client.WithMute(false);

        // Assert
        Assert.False(unmutedClient.IsMuted);
        Assert.False(unmutedClient.IsSilent);
        Assert.Equal(75, unmutedClient.EffectiveVolume);
        Assert.NotNull(unmutedClient.UpdatedAt);
    }

    [Fact]
    public void WithZone_WithValidZoneId_ShouldAssignToZone()
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );
        var zoneId = "test-zone";

        // Act
        var updatedClient = client.WithZone(zoneId);

        // Assert
        Assert.Equal(zoneId, updatedClient.ZoneId);
        Assert.True(updatedClient.IsAssignedToZone);
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Fact]
    public void WithZone_WithNull_ShouldUnassignFromZone()
    {
        // Arrange
        var client = Client
            .Create("test-client", "Test Client", new MacAddress("AA:BB:CC:DD:EE:FF"), new IpAddress("192.168.1.100"))
            .WithZone("test-zone");

        // Act
        var updatedClient = client.WithZone(null);

        // Assert
        Assert.Null(updatedClient.ZoneId);
        Assert.False(updatedClient.IsAssignedToZone);
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Fact]
    public void WithIpAddress_WithNewIpAddress_ShouldUpdateIpAddress()
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );
        var newIpAddress = new IpAddress("192.168.1.200");

        // Act
        var updatedClient = client.WithIpAddress(newIpAddress);

        // Assert
        Assert.Equal(newIpAddress, updatedClient.IpAddress);
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void WithLatency_WithValidLatency_ShouldUpdateLatency(int latencyMs)
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        // Act
        var updatedClient = client.WithLatency(latencyMs);

        // Assert
        Assert.Equal(latencyMs, updatedClient.LatencyMs);
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Fact]
    public void WithLatency_WithNull_ShouldClearLatency()
    {
        // Arrange
        var client = Client
            .Create("test-client", "Test Client", new MacAddress("AA:BB:CC:DD:EE:FF"), new IpAddress("192.168.1.100"))
            .WithLatency(100);

        // Act
        var updatedClient = client.WithLatency(null);

        // Assert
        Assert.Null(updatedClient.LatencyMs);
        Assert.NotNull(updatedClient.UpdatedAt);
    }

    [Fact]
    public void WithLatency_WithNegativeLatency_ShouldThrowArgumentException()
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => client.WithLatency(-1));
    }

    [Fact]
    public void IsConnected_WithConnectedStatus_ShouldReturnTrue()
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100"),
            ClientStatus.Connected
        );

        // Act & Assert
        Assert.True(client.IsConnected);
        Assert.False(client.IsDisconnected);
        Assert.False(client.HasError);
    }

    [Fact]
    public void IsDisconnected_WithDisconnectedStatus_ShouldReturnTrue()
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100"),
            ClientStatus.Disconnected
        );

        // Act & Assert
        Assert.False(client.IsConnected);
        Assert.True(client.IsDisconnected);
        Assert.False(client.HasError);
    }

    [Fact]
    public void HasError_WithErrorStatus_ShouldReturnTrue()
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100"),
            ClientStatus.Error
        );

        // Act & Assert
        Assert.False(client.IsConnected);
        Assert.False(client.IsDisconnected);
        Assert.True(client.HasError);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(50, false)]
    [InlineData(100, false)]
    public void IsSilent_WithVolumeZeroOrMuted_ShouldReturnCorrectValue(int volume, bool isMuted)
    {
        // Arrange
        var client = Client
            .Create(
                "test-client",
                "Test Client",
                new MacAddress("AA:BB:CC:DD:EE:FF"),
                new IpAddress("192.168.1.100"),
                volume: volume
            )
            .WithMute(isMuted);

        // Act
        var expectedSilent = volume == 0 || isMuted;

        // Assert
        Assert.Equal(expectedSilent, client.IsSilent);
    }

    [Theory]
    [InlineData(50, false, 50)]
    [InlineData(50, true, 0)]
    [InlineData(0, false, 0)]
    [InlineData(100, false, 100)]
    public void EffectiveVolume_WithDifferentStates_ShouldReturnCorrectValue(int volume, bool isMuted, int expected)
    {
        // Arrange
        var client = Client
            .Create(
                "test-client",
                "Test Client",
                new MacAddress("AA:BB:CC:DD:EE:FF"),
                new IpAddress("192.168.1.100"),
                volume: volume
            )
            .WithMute(isMuted);

        // Act & Assert
        Assert.Equal(expected, client.EffectiveVolume);
    }

    [Fact]
    public void Client_ImmutabilityTest_ShouldNotModifyOriginal()
    {
        // Arrange
        var originalClient = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100"),
            ClientStatus.Disconnected,
            50
        );
        var originalStatus = originalClient.Status;
        var originalVolume = originalClient.Volume;
        var originalMuted = originalClient.IsMuted;

        // Act
        var modifiedClient = originalClient.WithStatus(ClientStatus.Connected).WithVolume(75).WithMute(true);

        // Assert
        Assert.Equal(originalStatus, originalClient.Status);
        Assert.Equal(originalVolume, originalClient.Volume);
        Assert.Equal(originalMuted, originalClient.IsMuted);
        Assert.NotEqual(originalClient.Status, modifiedClient.Status);
        Assert.NotEqual(originalClient.Volume, modifiedClient.Volume);
        Assert.NotEqual(originalClient.IsMuted, modifiedClient.IsMuted);
    }

    [Fact]
    public void Client_ComplexOperationsSequence_ShouldMaintainCorrectState()
    {
        // Arrange
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        // Act
        var result = client
            .WithStatus(ClientStatus.Connected)
            .WithVolume(80)
            .WithZone("living-room")
            .WithMute(true)
            .WithLatency(50)
            .WithIpAddress(new IpAddress("192.168.1.150"));

        // Assert
        Assert.True(result.IsConnected);
        Assert.Equal(80, result.Volume);
        Assert.Equal(0, result.EffectiveVolume);
        Assert.True(result.IsMuted);
        Assert.True(result.IsSilent);
        Assert.Equal("living-room", result.ZoneId);
        Assert.True(result.IsAssignedToZone);
        Assert.Equal(50, result.LatencyMs);
        Assert.Equal("192.168.1.150", result.IpAddress.ToString());
    }

    [Fact]
    public void Client_DefaultValues_ShouldHaveExpectedDefaults()
    {
        // Arrange & Act
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );

        // Assert
        Assert.Equal(ClientStatus.Disconnected, client.Status);
        Assert.Equal(50, client.Volume);
        Assert.False(client.IsMuted);
        Assert.Null(client.ZoneId);
        Assert.Null(client.Description);
        Assert.Null(client.Location);
        Assert.Null(client.LatencyMs);
        Assert.Null(client.LastSeen);
        Assert.Null(client.UpdatedAt);
        Assert.True(client.CreatedAt <= DateTime.UtcNow);
    }
}
