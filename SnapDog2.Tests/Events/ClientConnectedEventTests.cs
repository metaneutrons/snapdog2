using SnapDog2.Core.Events;
using SnapDog2.Core.Models.ValueObjects;
using Xunit;

namespace SnapDog2.Tests.Events;

/// <summary>
/// Unit tests for the ClientConnectedEvent class.
/// Tests client connection event creation, properties, and validation.
/// </summary>
public class ClientConnectedEventTests
{
    private const string ValidClientId = "client-1";
    private const string ValidClientName = "Test Client";
    private readonly MacAddress _validMacAddress = new("AA:BB:CC:DD:EE:FF");
    private readonly IpAddress _validIpAddress = new("192.168.1.100");

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateEvent()
    {
        // Act
        var evt = ClientConnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, _validIpAddress);

        // Assert
        Assert.Equal(ValidClientId, evt.ClientId);
        Assert.Equal(ValidClientName, evt.ClientName);
        Assert.Equal(_validMacAddress, evt.MacAddress);
        Assert.Equal(_validIpAddress, evt.IpAddress);
        Assert.Equal(50, evt.Volume); // Default volume
        Assert.Null(evt.AssignedZoneId);
        Assert.False(evt.IsFirstConnection);
        Assert.Null(evt.ClientVersion);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.True(evt.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateEventWithAllProperties()
    {
        // Arrange
        var volume = 75;
        var assignedZoneId = "zone-1";
        var isFirstConnection = true;
        var clientVersion = "1.2.3";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            volume,
            assignedZoneId,
            isFirstConnection,
            clientVersion,
            correlationId
        );

        // Assert
        Assert.Equal(ValidClientId, evt.ClientId);
        Assert.Equal(ValidClientName, evt.ClientName);
        Assert.Equal(_validMacAddress, evt.MacAddress);
        Assert.Equal(_validIpAddress, evt.IpAddress);
        Assert.Equal(volume, evt.Volume);
        Assert.Equal(assignedZoneId, evt.AssignedZoneId);
        Assert.Equal(isFirstConnection, evt.IsFirstConnection);
        Assert.Equal(clientVersion, evt.ClientVersion);
        Assert.Equal(correlationId, evt.CorrelationId);
    }

    [Fact]
    public void Create_WithCustomVolume_ShouldSetVolume()
    {
        // Arrange
        var customVolume = 25;

        // Act
        var evt = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            customVolume
        );

        // Assert
        Assert.Equal(customVolume, evt.Volume);
    }

    [Fact]
    public void Create_WithZoneAssignment_ShouldSetZoneId()
    {
        // Arrange
        var zoneId = "living-room";

        // Act
        var evt = ClientConnectedEvent.Create(
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
    public void Create_WithFirstConnection_ShouldSetFirstConnectionFlag()
    {
        // Act
        var evt = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            isFirstConnection: true
        );

        // Assert
        Assert.True(evt.IsFirstConnection);
    }

    [Fact]
    public void Create_WithClientVersion_ShouldSetClientVersion()
    {
        // Arrange
        var clientVersion = "SnapClient v2.1.0";

        // Act
        var evt = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            clientVersion: clientVersion
        );

        // Assert
        Assert.Equal(clientVersion, evt.ClientVersion);
    }

    [Fact]
    public void Constructor_WithoutParameters_ShouldCreateEventWithDefaults()
    {
        // Act
        var evt = new ClientConnectedEvent
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
        Assert.Equal(0, evt.Volume); // Default for struct
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
    }

    [Fact]
    public void Constructor_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = new ClientConnectedEvent(correlationId)
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
    public void Event_ShouldImplementIDomainEvent()
    {
        // Arrange
        var evt = ClientConnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, _validIpAddress);

        // Act & Assert
        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }

    [Fact]
    public void Event_ShouldInheritFromDomainEvent()
    {
        // Arrange
        var evt = ClientConnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, _validIpAddress);

        // Act & Assert
        Assert.IsAssignableFrom<DomainEvent>(evt);
    }

    [Fact]
    public void RecordEquality_WithSameData_ShouldNotBeEqualDueToEventId()
    {
        // Arrange
        var evt1 = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            75,
            "zone-1",
            true,
            "v1.0"
        );

        var evt2 = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            75,
            "zone-1",
            true,
            "v1.0"
        );

        // Act & Assert
        Assert.NotEqual(evt1, evt2); // Different EventIds and timestamps
        Assert.NotEqual(evt1.EventId, evt2.EventId);
    }

    [Fact]
    public void RecordEquality_WithSameInstance_ShouldBeEqual()
    {
        // Arrange
        var evt = ClientConnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, _validIpAddress);

        // Act & Assert
        Assert.Equal(evt, evt);
    }

    [Fact]
    public void ToString_ShouldContainRelevantInformation()
    {
        // Arrange
        var evt = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            75,
            "zone-1"
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
    public void VolumeRange_ShouldAcceptValidValues()
    {
        // Test various volume levels
        var volumeLevels = new[] { 0, 25, 50, 75, 100 };

        foreach (var volume in volumeLevels)
        {
            // Act
            var evt = ClientConnectedEvent.Create(
                ValidClientId,
                ValidClientName,
                _validMacAddress,
                _validIpAddress,
                volume
            );

            // Assert
            Assert.Equal(volume, evt.Volume);
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
            var evt = ClientConnectedEvent.Create(ValidClientId, ValidClientName, macAddress, _validIpAddress);

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
            var evt = ClientConnectedEvent.Create(ValidClientId, ValidClientName, _validMacAddress, ipAddress);

            // Assert
            Assert.Equal(ipAddress, evt.IpAddress);
        }
    }

    [Fact]
    public void ClientVersion_ShouldAcceptVariousFormats()
    {
        // Arrange
        var versions = new[] { "1.0.0", "v2.1.3-beta", "SnapClient 3.0", "Android/12 SnapClient/2.5.1", null };

        foreach (var version in versions)
        {
            // Act
            var evt = ClientConnectedEvent.Create(
                ValidClientId,
                ValidClientName,
                _validMacAddress,
                _validIpAddress,
                clientVersion: version
            );

            // Assert
            Assert.Equal(version, evt.ClientVersion);
        }
    }

    [Fact]
    public void FirstConnectionScenarios_ShouldWorkCorrectly()
    {
        // Test first connection
        var firstConnectionEvent = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            isFirstConnection: true
        );

        Assert.True(firstConnectionEvent.IsFirstConnection);

        // Test reconnection
        var reconnectionEvent = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            isFirstConnection: false
        );

        Assert.False(reconnectionEvent.IsFirstConnection);
    }

    [Fact]
    public void ZoneAssignment_ShouldHandleNullAndValidValues()
    {
        // Test without zone assignment
        var unassignedEvent = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress
        );

        Assert.Null(unassignedEvent.AssignedZoneId);

        // Test with zone assignment
        var assignedEvent = ClientConnectedEvent.Create(
            ValidClientId,
            ValidClientName,
            _validMacAddress,
            _validIpAddress,
            assignedZoneId: "kitchen"
        );

        Assert.Equal("kitchen", assignedEvent.AssignedZoneId);
    }
}
