using SnapDog2.Core.Events;
using Xunit;

namespace SnapDog2.Tests.Events;

/// <summary>
/// Unit tests for the VolumeChangedEvent class.
/// Tests volume change event creation, properties, and validation.
/// </summary>
public class VolumeChangedEventTests
{
    private const string ValidEntityId = "client-1";
    private const string ValidEntityName = "Test Client";

    [Fact]
    public void CreateForClient_WithValidParameters_ShouldCreateEvent()
    {
        // Arrange
        var previousVolume = 50;
        var newVolume = 75;

        // Act
        var evt = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, previousVolume, newVolume);

        // Assert
        Assert.Equal(ValidEntityId, evt.EntityId);
        Assert.Equal(ValidEntityName, evt.EntityName);
        Assert.Equal(previousVolume, evt.PreviousVolume);
        Assert.Equal(newVolume, evt.NewVolume);
        Assert.Equal(VolumeEntityType.Client, evt.EntityType);
        Assert.False(evt.PreviousMuteStatus);
        Assert.False(evt.NewMuteStatus);
        Assert.Null(evt.ZoneId);
        Assert.Null(evt.ChangedBy);
        Assert.Null(evt.ChangeReason);
        Assert.False(evt.IsAutomaticChange);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.True(evt.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void CreateForZone_WithValidParameters_ShouldCreateEvent()
    {
        // Arrange
        var previousVolume = 30;
        var newVolume = 60;

        // Act
        var evt = VolumeChangedEvent.CreateForZone(ValidEntityId, ValidEntityName, previousVolume, newVolume);

        // Assert
        Assert.Equal(ValidEntityId, evt.EntityId);
        Assert.Equal(ValidEntityName, evt.EntityName);
        Assert.Equal(previousVolume, evt.PreviousVolume);
        Assert.Equal(newVolume, evt.NewVolume);
        Assert.Equal(VolumeEntityType.Zone, evt.EntityType);
        Assert.False(evt.PreviousMuteStatus);
        Assert.False(evt.NewMuteStatus);
        Assert.Equal(ValidEntityId, evt.ZoneId); // Zone ID is set to the entity ID for zones
    }

    [Fact]
    public void CreateForClient_WithAllParameters_ShouldCreateEventWithAllProperties()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var zoneId = "zone-1";
        var changedBy = "user123";
        var changeReason = "User adjustment";

        // Act
        var evt = VolumeChangedEvent.CreateForClient(
            ValidEntityId,
            ValidEntityName,
            50,
            75,
            false,
            true,
            zoneId,
            changedBy,
            changeReason,
            false,
            correlationId
        );

        // Assert
        Assert.Equal(correlationId, evt.CorrelationId);
        Assert.Equal(zoneId, evt.ZoneId);
        Assert.Equal(changedBy, evt.ChangedBy);
        Assert.Equal(changeReason, evt.ChangeReason);
        Assert.False(evt.PreviousMuteStatus);
        Assert.True(evt.NewMuteStatus);
        Assert.False(evt.IsAutomaticChange);
    }

    [Fact]
    public void CreateForZone_WithAllParameters_ShouldCreateEventWithAllProperties()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var changedBy = "system";
        var changeReason = "Auto adjustment";

        // Act
        var evt = VolumeChangedEvent.CreateForZone(
            ValidEntityId,
            ValidEntityName,
            30,
            60,
            changedBy,
            changeReason,
            true,
            correlationId
        );

        // Assert
        Assert.Equal(correlationId, evt.CorrelationId);
        Assert.Equal(changedBy, evt.ChangedBy);
        Assert.Equal(changeReason, evt.ChangeReason);
        Assert.True(evt.IsAutomaticChange);
    }

    [Fact]
    public void VolumeChange_ShouldCalculateCorrectly()
    {
        // Test volume increase
        var increaseEvent = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 40, 70);

        Assert.Equal(30, increaseEvent.VolumeChange);
        Assert.True(increaseEvent.IsVolumeIncrease);
        Assert.False(increaseEvent.IsVolumeDecrease);

        // Test volume decrease
        var decreaseEvent = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 80, 45);

        Assert.Equal(-35, decreaseEvent.VolumeChange);
        Assert.False(decreaseEvent.IsVolumeIncrease);
        Assert.True(decreaseEvent.IsVolumeDecrease);

        // Test no change
        var noChangeEvent = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 50, 50);

        Assert.Equal(0, noChangeEvent.VolumeChange);
        Assert.False(noChangeEvent.IsVolumeIncrease);
        Assert.False(noChangeEvent.IsVolumeDecrease);
    }

    [Fact]
    public void MuteStatusChange_ShouldBeDetected()
    {
        // Test muting
        var muteEvent = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 50, 50, false, true);

        Assert.True(muteEvent.IsMuteStatusChanged);
        Assert.True(muteEvent.WasMuted);
        Assert.False(muteEvent.WasUnmuted);

        // Test unmuting
        var unmuteEvent = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 50, 50, true, false);

        Assert.True(unmuteEvent.IsMuteStatusChanged);
        Assert.False(unmuteEvent.WasMuted);
        Assert.True(unmuteEvent.WasUnmuted);

        // Test no mute status change
        var noMuteChangeEvent = VolumeChangedEvent.CreateForClient(
            ValidEntityId,
            ValidEntityName,
            50,
            75,
            false,
            false
        );

        Assert.False(noMuteChangeEvent.IsMuteStatusChanged);
        Assert.False(noMuteChangeEvent.WasMuted);
        Assert.False(noMuteChangeEvent.WasUnmuted);
    }

    [Fact]
    public void EntityType_ShouldBeCorrectForClientAndZone()
    {
        // Test client event
        var clientEvent = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 50, 75);

        Assert.Equal(VolumeEntityType.Client, clientEvent.EntityType);
        Assert.True(clientEvent.IsClientVolumeChange);
        Assert.False(clientEvent.IsZoneVolumeChange);

        // Test zone event
        var zoneEvent = VolumeChangedEvent.CreateForZone(ValidEntityId, ValidEntityName, 50, 75);

        Assert.Equal(VolumeEntityType.Zone, zoneEvent.EntityType);
        Assert.False(zoneEvent.IsClientVolumeChange);
        Assert.True(zoneEvent.IsZoneVolumeChange);
    }

    [Fact]
    public void Constructor_WithoutParameters_ShouldCreateEventWithDefaults()
    {
        // Act
        var evt = new VolumeChangedEvent
        {
            EntityId = ValidEntityId,
            EntityName = ValidEntityName,
            EntityType = VolumeEntityType.Client,
            PreviousVolume = 50,
            NewVolume = 75,
        };

        // Assert
        Assert.Equal(ValidEntityId, evt.EntityId);
        Assert.Equal(ValidEntityName, evt.EntityName);
        Assert.Equal(VolumeEntityType.Client, evt.EntityType);
        Assert.Equal(50, evt.PreviousVolume);
        Assert.Equal(75, evt.NewVolume);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
    }

    [Fact]
    public void Constructor_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = new VolumeChangedEvent(correlationId)
        {
            EntityId = ValidEntityId,
            EntityName = ValidEntityName,
            EntityType = VolumeEntityType.Zone,
            PreviousVolume = 30,
            NewVolume = 60,
        };

        // Assert
        Assert.Equal(correlationId, evt.CorrelationId);
    }

    [Fact]
    public void Event_ShouldImplementIDomainEvent()
    {
        // Arrange
        var evt = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 50, 75);

        // Act & Assert
        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }

    [Fact]
    public void Event_ShouldInheritFromDomainEvent()
    {
        // Arrange
        var evt = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 50, 75);

        // Act & Assert
        Assert.IsAssignableFrom<DomainEvent>(evt);
    }

    [Fact]
    public void RecordEquality_WithSameData_ShouldNotBeEqualDueToEventId()
    {
        // Arrange
        var evt1 = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 50, 75);

        var evt2 = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 50, 75);

        // Act & Assert
        Assert.NotEqual(evt1, evt2); // Different EventIds and timestamps
        Assert.NotEqual(evt1.EventId, evt2.EventId);
    }

    [Fact]
    public void RecordEquality_WithSameInstance_ShouldBeEqual()
    {
        // Arrange
        var evt = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 50, 75);

        // Act & Assert
        Assert.Equal(evt, evt);
    }

    [Fact]
    public void ToString_ShouldContainRelevantInformation()
    {
        // Arrange
        var evt = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, 50, 75);

        // Act
        var stringRepresentation = evt.ToString();

        // Assert
        Assert.Contains(ValidEntityId, stringRepresentation);
        Assert.Contains(ValidEntityName, stringRepresentation);
        Assert.Contains("50", stringRepresentation);
        Assert.Contains("75", stringRepresentation);
    }

    [Fact]
    public void VolumeRange_ShouldAcceptValidValues()
    {
        // Test various volume combinations
        var volumeCombinations = new[]
        {
            (0, 25),
            (25, 50),
            (50, 75),
            (75, 100),
            (100, 0),
            (50, 50), // No change
        };

        foreach (var (previous, current) in volumeCombinations)
        {
            // Act
            var evt = VolumeChangedEvent.CreateForClient(ValidEntityId, ValidEntityName, previous, current);

            // Assert
            Assert.Equal(previous, evt.PreviousVolume);
            Assert.Equal(current, evt.NewVolume);
            Assert.Equal(current - previous, evt.VolumeChange);
        }
    }

    [Fact]
    public void AutomaticChange_ShouldBeDetected()
    {
        // Test manual change
        var manualChange = VolumeChangedEvent.CreateForClient(
            ValidEntityId,
            ValidEntityName,
            50,
            75,
            isAutomaticChange: false
        );

        Assert.False(manualChange.IsAutomaticChange);

        // Test automatic change
        var automaticChange = VolumeChangedEvent.CreateForZone(
            ValidEntityId,
            ValidEntityName,
            50,
            75,
            isAutomaticChange: true
        );

        Assert.True(automaticChange.IsAutomaticChange);
    }

    [Fact]
    public void VolumeChangeScenarios_ShouldCoverCommonCases()
    {
        // User increases volume
        var userIncrease = VolumeChangedEvent.CreateForClient(
            "client-1",
            "Living Room Speaker",
            45,
            60,
            false,
            false,
            "living-room",
            "user123",
            "User adjustment",
            false
        );

        Assert.True(userIncrease.IsVolumeIncrease);
        Assert.Equal(15, userIncrease.VolumeChange);
        Assert.Equal("user123", userIncrease.ChangedBy);
        Assert.Equal("User adjustment", userIncrease.ChangeReason);
        Assert.False(userIncrease.IsAutomaticChange);

        // System mutes client
        var systemMute = VolumeChangedEvent.CreateForClient(
            "client-2",
            "Kitchen Speaker",
            70,
            70,
            false,
            true,
            "kitchen",
            "system",
            "Auto-mute during call",
            true
        );

        Assert.True(systemMute.WasMuted);
        Assert.True(systemMute.IsMuteStatusChanged);
        Assert.Equal(0, systemMute.VolumeChange);
        Assert.Equal("system", systemMute.ChangedBy);
        Assert.True(systemMute.IsAutomaticChange);

        // Zone default volume change
        var zoneVolumeChange = VolumeChangedEvent.CreateForZone(
            "zone-1",
            "Living Room",
            50,
            65,
            "admin",
            "Zone reconfiguration"
        );

        Assert.True(zoneVolumeChange.IsZoneVolumeChange);
        Assert.False(zoneVolumeChange.IsClientVolumeChange);
        Assert.Equal(15, zoneVolumeChange.VolumeChange);
        Assert.Equal("admin", zoneVolumeChange.ChangedBy);
        Assert.Equal("zone-1", zoneVolumeChange.ZoneId);

        // Client unmute with volume restore
        var unmuteRestore = VolumeChangedEvent.CreateForClient(
            "client-3",
            "Bedroom Speaker",
            0,
            55,
            true,
            false,
            "bedroom",
            "user456",
            "Unmute and restore volume"
        );

        Assert.True(unmuteRestore.WasUnmuted);
        Assert.True(unmuteRestore.IsVolumeIncrease);
        Assert.Equal(55, unmuteRestore.VolumeChange);
        Assert.Equal("user456", unmuteRestore.ChangedBy);
    }
}
