using SnapDog2.Core.Events;
using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Events;

/// <summary>
/// Unit tests for the ClientDisconnectedEvent class.
/// Tests client disconnection event creation, properties, and validation.
/// </summary>
public class ClientDisconnectedEventTests
{
    private const string ValidClientId = "client-1";
    private const string ValidClientName = "Test Client";
    private readonly MacAddress _validMacAddress = new("AA:BB:CC:DD:EE:FF");
    private readonly IpAddress _validIpAddress = new("192.168.1.100");

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateEvent()
    {
        // Act
        var evt = ClientDisconnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, _validIpAddress);

        // Assert
        Assert.Equal(ValidClientId, evt.ClientId);
        Assert.Equal(ValidClientName, evt.ClientName);
        Assert.Equal(_validMacAddress, evt.MacAddress);
        Assert.Equal(_validIpAddress, evt.IpAddress);
        Assert.Null(evt.AssignedZoneId);
        Assert.Null(evt.DisconnectionReason);
        Assert.Null(evt.ConnectionDurationSeconds);
        Assert.False(evt.IsGracefulDisconnection);
        Assert.Equal(0, evt.LastVolume);
        Assert.False(evt.WasMuted);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.True(evt.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateEventWithAllProperties()
    {
        // Arrange
        var assignedZoneId = "zone-1";
        var disconnectionReason = "Network timeout";
        var connectionDurationSeconds = 3600L;
        var isGracefulDisconnection = true;
        var lastVolume = 75;
        var wasMuted = true;
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            assignedZoneId,
            disconnectionReason,
            connectionDurationSeconds,
            isGracefulDisconnection,
            lastVolume,
            wasMuted,
            correlationId
        );

        // Assert
        Assert.Equal(ValidClientId, evt.ClientId);
        Assert.Equal(ValidClientName, evt.ClientName);
        Assert.Equal(_validMacAddress, evt.MacAddress);
        Assert.Equal(_validIpAddress, evt.IpAddress);
        Assert.Equal(assignedZoneId, evt.AssignedZoneId);
        Assert.Equal(disconnectionReason, evt.DisconnectionReason);
        Assert.Equal(connectionDurationSeconds, evt.ConnectionDurationSeconds);
        Assert.Equal(isGracefulDisconnection, evt.IsGracefulDisconnection);
        Assert.Equal(lastVolume, evt.LastVolume);
        Assert.Equal(wasMuted, evt.WasMuted);
        Assert.Equal(correlationId, evt.CorrelationId);
    }

    [Fact]
    public void Create_WithDisconnectionReason_ShouldSetReason()
    {
        // Arrange
        var reason = "User initiated disconnect";

        // Act
        var evt = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            disconnectionReason: reason
        );

        // Assert
        Assert.Equal(reason, evt.DisconnectionReason);
    }

    [Fact]
    public void Create_WithAssignedZone_ShouldSetZoneId()
    {
        // Arrange
        var zoneId = "living-room";

        // Act
        var evt = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            assignedZoneId: zoneId
        );

        // Assert
        Assert.Equal(zoneId, evt.AssignedZoneId);
    }

    [Fact]
    public void Create_WithGracefulDisconnection_ShouldSetGracefulFlag()
    {
        // Act
        var evt = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            isGracefulDisconnection: true
        );

        // Assert
        Assert.True(evt.IsGracefulDisconnection);
    }

    [Fact]
    public void Create_WithConnectionDuration_ShouldSetDuration()
    {
        // Arrange
        var durationSeconds = 1800L; // 30 minutes

        // Act
        var evt = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            connectionDurationSeconds: durationSeconds
        );

        // Assert
        Assert.Equal(durationSeconds, evt.ConnectionDurationSeconds);
        Assert.Equal(TimeSpan.FromSeconds(durationSeconds), evt.ConnectionDuration);
    }

    [Fact]
    public void Create_WithLastVolume_ShouldSetVolume()
    {
        // Arrange
        var lastVolume = 85;

        // Act
        var evt = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            lastVolume: lastVolume
        );

        // Assert
        Assert.Equal(lastVolume, evt.LastVolume);
    }

    [Fact]
    public void Create_WithMutedState_ShouldSetMutedFlag()
    {
        // Act
        var evt = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            wasMuted: true
        );

        // Assert
        Assert.True(evt.WasMuted);
    }

    [Fact]
    public void Constructor_WithoutParameters_ShouldCreateEventWithDefaults()
    {
        // Act
        var evt = new ClientDisconnectedEvent
        {
            ClientId = ValidClientId,
            ClientName = ValidClientName,
            MacAddress = _validMacAddress,
            IpAddress = _validIpAddress,
        };

        // Assert
        Assert.Equal(ValidClientId, evt.ClientId);
        Assert.Equal(ValidClientName, evt.ClientName);
        Assert.Equal(_validMacAddress, evt.MacAddress);
        Assert.Equal(_validIpAddress, evt.IpAddress);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
    }

    [Fact]
    public void Constructor_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = new ClientDisconnectedEvent(correlationId)
        {
            ClientId = ValidClientId,
            ClientName = ValidClientName,
            MacAddress = _validMacAddress,
            IpAddress = _validIpAddress,
        };

        // Assert
        Assert.Equal(correlationId, evt.CorrelationId);
    }

    [Fact]
    public void ConnectionDuration_WithNullSeconds_ShouldReturnNull()
    {
        // Act
        var evt = ClientDisconnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, _validIpAddress);

        // Assert
        Assert.Null(evt.ConnectionDuration);
    }

    [Fact]
    public void ConnectionDuration_WithValidSeconds_ShouldReturnTimeSpan()
    {
        // Arrange
        var durationSeconds = 7200L; // 2 hours

        // Act
        var evt = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            connectionDurationSeconds: durationSeconds
        );

        // Assert
        Assert.NotNull(evt.ConnectionDuration);
        Assert.Equal(TimeSpan.FromHours(2), evt.ConnectionDuration);
    }

    [Fact]
    public void Event_ShouldImplementIDomainEvent()
    {
        // Arrange
        var evt = ClientDisconnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, _validIpAddress);

        // Act & Assert
        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }

    [Fact]
    public void Event_ShouldInheritFromDomainEvent()
    {
        // Arrange
        var evt = ClientDisconnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, _validIpAddress);

        // Act & Assert
        Assert.IsAssignableFrom<DomainEvent>(evt);
    }

    [Fact]
    public void RecordEquality_WithSameData_ShouldNotBeEqualDueToEventId()
    {
        // Arrange
        var evt1 = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            "zone-1",
            "timeout",
            3600L,
            true,
            75,
            false
        );

        var evt2 = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            "zone-1",
            "timeout",
            3600L,
            true,
            75,
            false
        );

        // Act & Assert
        Assert.NotEqual(evt1, evt2); // Different EventIds and timestamps
        Assert.NotEqual(evt1.EventId, evt2.EventId);
    }

    [Fact]
    public void RecordEquality_WithSameInstance_ShouldBeEqual()
    {
        // Arrange
        var evt = ClientDisconnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, _validIpAddress);

        // Act & Assert
        Assert.Equal(evt, evt);
    }

    [Fact]
    public void ToString_ShouldContainRelevantInformation()
    {
        // Arrange
        var evt = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            "zone-1",
            "Network error"
        );

        // Act
        var stringRepresentation = evt.ToString();

        // Assert
        Assert.Contains(ValidClientId, stringRepresentation);
        Assert.Contains(ValidClientName, stringRepresentation);
        Assert.Contains("AA:BB:CC:DD:EE:FF", stringRepresentation);
        Assert.Contains("192.168.1.100", stringRepresentation);
    }

    [Fact]
    public void DisconnectionReasons_ShouldAcceptVariousValues()
    {
        // Arrange
        var reasons = new[]
        {
            "User initiated",
            "Network timeout",
            "Connection lost",
            "Server shutdown",
            "Authentication failed",
            null,
        };

        foreach (var reason in reasons)
        {
            // Act
            var evt = ClientDisconnectedEvent.Create(
                ValidClientId,
                ValidClientName,
                _validMacAddress,
                _validIpAddress,
                disconnectionReason: reason
            );

            // Assert
            Assert.Equal(reason, evt.DisconnectionReason);
        }
    }

    [Fact]
    public void VolumeRange_ShouldAcceptValidValues()
    {
        // Test various volume levels
        var volumeLevels = new[] { 0, 25, 50, 75, 100 };

        foreach (var volume in volumeLevels)
        {
            // Act
            var evt = ClientDisconnectedEvent.Create(
                ValidClientId,
                ValidClientName,
                _validMacAddress,
                _validIpAddress,
                lastVolume: volume
            );

            // Assert
            Assert.Equal(volume, evt.LastVolume);
        }
    }

    [Fact]
    public void MacAddress_ShouldPreserveValue()
    {
        // Arrange
        var macAddresses = new[]
        {
            new MacAddress("00:11:22:33:44:55"),
            new MacAddress("FF:EE:DD:CC:BB:AA"),
            new MacAddress("12:34:56:78:9A:BC"),
        };

        foreach (var macAddress in macAddresses)
        {
            // Act
            var evt = ClientDisconnectedEvent.Create(ValidClientId, ValidClientName, macAddress, _validIpAddress);

            // Assert
            Assert.Equal(macAddress, evt.MacAddress);
        }
    }

    [Fact]
    public void IpAddress_ShouldPreserveValue()
    {
        // Arrange
        var ipAddresses = new[]
        {
            new IpAddress("192.168.1.1"),
            new IpAddress("10.0.0.1"),
            new IpAddress("172.16.0.1"),
        };

        foreach (var ipAddress in ipAddresses)
        {
            // Act
            var evt = ClientDisconnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, ipAddress);

            // Assert
            Assert.Equal(ipAddress, evt.IpAddress);
        }
    }

    [Fact]
    public void DisconnectionScenarios_ShouldWorkCorrectly()
    {
        // Test graceful disconnect
        var gracefulDisconnectEvent = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            "zone-1",
            "User logout",
            1800L,
            true,
            50,
            false
        );

        Assert.True(gracefulDisconnectEvent.IsGracefulDisconnection);
        Assert.Equal("User logout", gracefulDisconnectEvent.DisconnectionReason);

        // Test unexpected disconnect
        var unexpectedDisconnectEvent = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            "zone-1",
            "Network failure",
            900L,
            false,
            75,
            true
        );

        Assert.False(unexpectedDisconnectEvent.IsGracefulDisconnection);
        Assert.Equal("Network failure", unexpectedDisconnectEvent.DisconnectionReason);
    }

    [Fact]
    public void AssignedZoneId_ShouldHandleNullAndValidValues()
    {
        // Test without zone assignment
        var noZoneEvent = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress
        );

        Assert.Null(noZoneEvent.AssignedZoneId);

        // Test with zone assignment
        var withZoneEvent = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            assignedZoneId: "bedroom"
        );

        Assert.Equal("bedroom", withZoneEvent.AssignedZoneId);
    }

    [Fact]
    public void CommonDisconnectionScenarios_ShouldCoverRealWorldCases()
    {
        // Graceful shutdown
        var gracefulShutdown = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            "living-room",
            "User logout",
            7200L, // 2 hours
            true,
            65,
            false
        );

        Assert.Equal("User logout", gracefulShutdown.DisconnectionReason);
        Assert.Equal("living-room", gracefulShutdown.AssignedZoneId);
        Assert.True(gracefulShutdown.IsGracefulDisconnection);
        Assert.Equal(TimeSpan.FromHours(2), gracefulShutdown.ConnectionDuration);
        Assert.Equal(65, gracefulShutdown.LastVolume);
        Assert.False(gracefulShutdown.WasMuted);

        // Network failure
        var networkFailure = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            "kitchen",
            "Connection timeout",
            450L, // 7.5 minutes
            false,
            80,
            true
        );

        Assert.Equal("Connection timeout", networkFailure.DisconnectionReason);
        Assert.Equal("kitchen", networkFailure.AssignedZoneId);
        Assert.False(networkFailure.IsGracefulDisconnection);
        Assert.Equal(TimeSpan.FromMinutes(7.5), networkFailure.ConnectionDuration);
        Assert.Equal(80, networkFailure.LastVolume);
        Assert.True(networkFailure.WasMuted);

        // Server maintenance
        var serverMaintenance = ClientDisconnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            null,
            "Server maintenance",
            null,
            true,
            0,
            false
        );

        Assert.Equal("Server maintenance", serverMaintenance.DisconnectionReason);
        Assert.Null(serverMaintenance.AssignedZoneId);
        Assert.True(serverMaintenance.IsGracefulDisconnection);
        Assert.Null(serverMaintenance.ConnectionDuration);
        Assert.Equal(0, serverMaintenance.LastVolume);
        Assert.False(serverMaintenance.WasMuted);
    }
}
