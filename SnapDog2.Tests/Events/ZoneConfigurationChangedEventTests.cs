using System.Collections.Immutable;
using SnapDog2.Core.Events;
using Xunit;

namespace SnapDog2.Tests.Events;

/// <summary>
/// Unit tests for the ZoneConfigurationChangedEvent class.
/// Tests zone configuration change event creation, properties, and validation.
/// </summary>
public class ZoneConfigurationChangedEventTests
{
    private const string ValidZoneId = "zone-1";
    private const string ValidZoneName = "Living Room";

    [Fact]
    public void Create_WithMinimalParameters_ShouldCreateEvent()
    {
        // Act
        var evt = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName);

        // Assert
        Assert.Equal(ValidZoneId, evt.ZoneId);
        Assert.Equal(ValidZoneName, evt.ZoneName);
        Assert.Empty(evt.ClientIds);
        Assert.Empty(evt.AddedClientIds);
        Assert.Empty(evt.RemovedClientIds);
        Assert.Null(evt.CurrentStreamId);
        Assert.Null(evt.PreviousStreamId);
        Assert.False(evt.IsEnabledChanged);
        Assert.True(evt.IsEnabled);
        Assert.False(evt.VolumeSettingsChanged);
        Assert.Null(evt.NewDefaultVolume);
        Assert.Null(evt.NewMinVolume);
        Assert.Null(evt.NewMaxVolume);
        Assert.Empty(evt.ChangedProperties);
        Assert.Null(evt.ChangeReason);
        Assert.Null(evt.ChangedBy);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.True(evt.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateEventWithAllProperties()
    {
        // Arrange
        var clientIds = new[] { "client-1", "client-2", "client-3" };
        var addedClientIds = new[] { "client-2", "client-3" };
        var removedClientIds = new[] { "client-old" };
        var currentStreamId = "stream-1";
        var previousStreamId = "stream-old";
        var newDefaultVolume = 75;
        var newMinVolume = 10;
        var newMaxVolume = 100;
        var changedProperties = new Dictionary<string, object?>
        {
            { "Description", "Updated description" },
            { "Color", "#FF0000" },
            { "Priority", 5 },
        };
        var changeReason = "User reconfiguration";
        var changedBy = "admin";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = ZoneConfigurationChangedEvent.Create(
            ValidZoneId,
            ValidZoneName,
            clientIds,
            addedClientIds,
            removedClientIds,
            currentStreamId,
            previousStreamId,
            true,
            false,
            true,
            newDefaultVolume,
            newMinVolume,
            newMaxVolume,
            changedProperties,
            changeReason,
            changedBy,
            correlationId
        );

        // Assert
        Assert.Equal(ValidZoneId, evt.ZoneId);
        Assert.Equal(ValidZoneName, evt.ZoneName);
        Assert.Equal(clientIds, evt.ClientIds);
        Assert.Equal(addedClientIds, evt.AddedClientIds);
        Assert.Equal(removedClientIds, evt.RemovedClientIds);
        Assert.Equal(currentStreamId, evt.CurrentStreamId);
        Assert.Equal(previousStreamId, evt.PreviousStreamId);
        Assert.True(evt.IsEnabledChanged);
        Assert.False(evt.IsEnabled);
        Assert.True(evt.VolumeSettingsChanged);
        Assert.Equal(newDefaultVolume, evt.NewDefaultVolume);
        Assert.Equal(newMinVolume, evt.NewMinVolume);
        Assert.Equal(newMaxVolume, evt.NewMaxVolume);
        Assert.Equal(3, evt.ChangedProperties.Count);
        Assert.Equal("Updated description", evt.ChangedProperties["Description"]);
        Assert.Equal("#FF0000", evt.ChangedProperties["Color"]);
        Assert.Equal(5, evt.ChangedProperties["Priority"]);
        Assert.Equal(changeReason, evt.ChangeReason);
        Assert.Equal(changedBy, evt.ChangedBy);
        Assert.Equal(correlationId, evt.CorrelationId);
    }

    [Fact]
    public void Create_WithNullCollections_ShouldCreateEmptyCollections()
    {
        // Act
        var evt = ZoneConfigurationChangedEvent.Create(
            ValidZoneId,
            ValidZoneName,
            clientIds: null,
            addedClientIds: null,
            removedClientIds: null,
            changedProperties: null
        );

        // Assert
        Assert.Empty(evt.ClientIds);
        Assert.Empty(evt.AddedClientIds);
        Assert.Empty(evt.RemovedClientIds);
        Assert.Empty(evt.ChangedProperties);
    }

    [Fact]
    public void HasClientChanges_WithAddedClients_ShouldReturnTrue()
    {
        // Arrange
        var addedClientIds = new[] { "client-1", "client-2" };

        // Act
        var evt = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName, addedClientIds: addedClientIds);

        // Assert
        Assert.True(evt.HasClientChanges);
    }

    [Fact]
    public void HasClientChanges_WithRemovedClients_ShouldReturnTrue()
    {
        // Arrange
        var removedClientIds = new[] { "client-old" };

        // Act
        var evt = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName, removedClientIds: removedClientIds);

        // Assert
        Assert.True(evt.HasClientChanges);
    }

    [Fact]
    public void HasClientChanges_WithBothAddedAndRemovedClients_ShouldReturnTrue()
    {
        // Arrange
        var addedClientIds = new[] { "client-new" };
        var removedClientIds = new[] { "client-old" };

        // Act
        var evt = ZoneConfigurationChangedEvent.Create(
            ValidZoneId,
            ValidZoneName,
            addedClientIds: addedClientIds,
            removedClientIds: removedClientIds
        );

        // Assert
        Assert.True(evt.HasClientChanges);
    }

    [Fact]
    public void HasClientChanges_WithNoChanges_ShouldReturnFalse()
    {
        // Act
        var evt = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName);

        // Assert
        Assert.False(evt.HasClientChanges);
    }

    [Fact]
    public void HasStreamChange_WithDifferentStreams_ShouldReturnTrue()
    {
        // Act
        var evt = ZoneConfigurationChangedEvent.Create(
            ValidZoneId,
            ValidZoneName,
            currentStreamId: "stream-new",
            previousStreamId: "stream-old"
        );

        // Assert
        Assert.True(evt.HasStreamChange);
    }

    [Fact]
    public void HasStreamChange_WithSameStreams_ShouldReturnFalse()
    {
        // Act
        var evt = ZoneConfigurationChangedEvent.Create(
            ValidZoneId,
            ValidZoneName,
            currentStreamId: "stream-1",
            previousStreamId: "stream-1"
        );

        // Assert
        Assert.False(evt.HasStreamChange);
    }

    [Fact]
    public void HasStreamChange_WithBothNull_ShouldReturnFalse()
    {
        // Act
        var evt = ZoneConfigurationChangedEvent.Create(
            ValidZoneId,
            ValidZoneName,
            currentStreamId: null,
            previousStreamId: null
        );

        // Assert
        Assert.False(evt.HasStreamChange);
    }

    [Fact]
    public void HasStreamChange_WithOneNull_ShouldReturnTrue()
    {
        // Test current null, previous not null
        var evt1 = ZoneConfigurationChangedEvent.Create(
            ValidZoneId,
            ValidZoneName,
            currentStreamId: null,
            previousStreamId: "stream-old"
        );

        Assert.True(evt1.HasStreamChange);

        // Test current not null, previous null
        var evt2 = ZoneConfigurationChangedEvent.Create(
            ValidZoneId,
            ValidZoneName,
            currentStreamId: "stream-new",
            previousStreamId: null
        );

        Assert.True(evt2.HasStreamChange);
    }

    [Fact]
    public void Constructor_WithoutParameters_ShouldCreateEventWithDefaults()
    {
        // Act
        var evt = new ZoneConfigurationChangedEvent { ZoneId = ValidZoneId, ZoneName = ValidZoneName };

        // Assert
        Assert.Equal(ValidZoneId, evt.ZoneId);
        Assert.Equal(ValidZoneName, evt.ZoneName);
        Assert.Empty(evt.ClientIds);
        Assert.Empty(evt.AddedClientIds);
        Assert.Empty(evt.RemovedClientIds);
        Assert.Empty(evt.ChangedProperties);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
    }

    [Fact]
    public void Constructor_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = new ZoneConfigurationChangedEvent(correlationId) { ZoneId = ValidZoneId, ZoneName = ValidZoneName };

        // Assert
        Assert.Equal(correlationId, evt.CorrelationId);
    }

    [Fact]
    public void Event_ShouldImplementIDomainEvent()
    {
        // Arrange
        var evt = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName);

        // Act & Assert
        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }

    [Fact]
    public void Event_ShouldInheritFromDomainEvent()
    {
        // Arrange
        var evt = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName);

        // Act & Assert
        Assert.IsAssignableFrom<DomainEvent>(evt);
    }

    [Fact]
    public void RecordEquality_WithSameData_ShouldNotBeEqualDueToEventId()
    {
        // Arrange
        var evt1 = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName);
        var evt2 = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName);

        // Act & Assert
        Assert.NotEqual(evt1, evt2); // Different EventIds and timestamps
        Assert.NotEqual(evt1.EventId, evt2.EventId);
    }

    [Fact]
    public void RecordEquality_WithSameInstance_ShouldBeEqual()
    {
        // Arrange
        var evt = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName);

        // Act & Assert
        Assert.Equal(evt, evt);
    }

    [Fact]
    public void ToString_ShouldContainRelevantInformation()
    {
        // Arrange
        var evt = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName);

        // Act
        var stringRepresentation = evt.ToString();

        // Assert
        Assert.Contains(ValidZoneId, stringRepresentation);
        Assert.Contains(ValidZoneName, stringRepresentation);
    }

    [Fact]
    public void ZoneConfigurationScenarios_ShouldCoverCommonCases()
    {
        // Client assignment change
        var clientAssignmentChange = ZoneConfigurationChangedEvent.Create(
            "zone-living-room",
            "Living Room",
            clientIds: new[] { "client-1", "client-2", "client-3" },
            addedClientIds: new[] { "client-3" },
            removedClientIds: new[] { "client-old" },
            changeReason: "Client reassignment",
            changedBy: "admin"
        );

        Assert.True(clientAssignmentChange.HasClientChanges);
        Assert.Equal(3, clientAssignmentChange.ClientIds.Count);
        Assert.Single(clientAssignmentChange.AddedClientIds);
        Assert.Single(clientAssignmentChange.RemovedClientIds);
        Assert.Equal("admin", clientAssignmentChange.ChangedBy);

        // Stream change
        var streamChange = ZoneConfigurationChangedEvent.Create(
            "zone-kitchen",
            "Kitchen",
            currentStreamId: "radio-jazz",
            previousStreamId: "radio-rock",
            changeReason: "User preference change",
            changedBy: "user123"
        );

        Assert.True(streamChange.HasStreamChange);
        Assert.Equal("radio-jazz", streamChange.CurrentStreamId);
        Assert.Equal("radio-rock", streamChange.PreviousStreamId);
        Assert.False(streamChange.HasClientChanges);

        // Zone enable/disable
        var enableStatusChange = ZoneConfigurationChangedEvent.Create(
            "zone-bedroom",
            "Bedroom",
            isEnabledChanged: true,
            isEnabled: false,
            changeReason: "Maintenance mode",
            changedBy: "system"
        );

        Assert.True(enableStatusChange.IsEnabledChanged);
        Assert.False(enableStatusChange.IsEnabled);
        Assert.Equal("system", enableStatusChange.ChangedBy);

        // Volume settings change
        var volumeSettingsChange = ZoneConfigurationChangedEvent.Create(
            "zone-office",
            "Office",
            volumeSettingsChanged: true,
            newDefaultVolume: 60,
            newMinVolume: 5,
            newMaxVolume: 85,
            changeReason: "Office hours volume limits",
            changedBy: "admin"
        );

        Assert.True(volumeSettingsChange.VolumeSettingsChanged);
        Assert.Equal(60, volumeSettingsChange.NewDefaultVolume);
        Assert.Equal(5, volumeSettingsChange.NewMinVolume);
        Assert.Equal(85, volumeSettingsChange.NewMaxVolume);

        // Complex configuration change
        var complexChange = ZoneConfigurationChangedEvent.Create(
            "zone-main",
            "Main Zone",
            clientIds: new[] { "client-1", "client-2" },
            addedClientIds: new[] { "client-2" },
            currentStreamId: "playlist-party",
            previousStreamId: "radio-ambient",
            isEnabledChanged: true,
            isEnabled: true,
            volumeSettingsChanged: true,
            newDefaultVolume: 70,
            changedProperties: new Dictionary<string, object?>
            {
                { "Theme", "Party Mode" },
                { "AutoFade", true },
                { "MaxClients", 10 },
            },
            changeReason: "Party mode activation",
            changedBy: "user456"
        );

        Assert.True(complexChange.HasClientChanges);
        Assert.True(complexChange.HasStreamChange);
        Assert.True(complexChange.IsEnabledChanged);
        Assert.True(complexChange.IsEnabled);
        Assert.True(complexChange.VolumeSettingsChanged);
        Assert.Equal(3, complexChange.ChangedProperties.Count);
        Assert.Equal("Party Mode", complexChange.ChangedProperties["Theme"]);
        Assert.Equal(true, complexChange.ChangedProperties["AutoFade"]);
        Assert.Equal(10, complexChange.ChangedProperties["MaxClients"]);
    }

    [Fact]
    public void ImmutableCollections_ShouldBeImmutable()
    {
        // Arrange
        var originalClientIds = new[] { "client-1", "client-2" };
        var evt = ZoneConfigurationChangedEvent.Create(ValidZoneId, ValidZoneName, clientIds: originalClientIds);

        // Act & Assert - Should not be able to modify the collections
        Assert.IsType<ImmutableList<string>>(evt.ClientIds);
        Assert.IsType<ImmutableList<string>>(evt.AddedClientIds);
        Assert.IsType<ImmutableList<string>>(evt.RemovedClientIds);
        Assert.IsType<ImmutableDictionary<string, object?>>(evt.ChangedProperties);

        // Verify the collections contain the expected data
        Assert.Equal(originalClientIds, evt.ClientIds);
    }

    [Fact]
    public void Create_WithEmptyCollections_ShouldCreateEmptyImmutableCollections()
    {
        // Act
        var evt = ZoneConfigurationChangedEvent.Create(
            ValidZoneId,
            ValidZoneName,
            clientIds: Array.Empty<string>(),
            addedClientIds: Array.Empty<string>(),
            removedClientIds: Array.Empty<string>(),
            changedProperties: new Dictionary<string, object?>()
        );

        // Assert
        Assert.Empty(evt.ClientIds);
        Assert.Empty(evt.AddedClientIds);
        Assert.Empty(evt.RemovedClientIds);
        Assert.Empty(evt.ChangedProperties);
        Assert.False(evt.HasClientChanges);
    }
}
