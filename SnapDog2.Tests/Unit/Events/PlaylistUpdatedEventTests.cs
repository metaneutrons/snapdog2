using System.Collections.Immutable;
using SnapDog2.Core.Events;
using Xunit;

namespace SnapDog2.Tests.Events;

/// <summary>
/// Unit tests for the PlaylistUpdatedEvent class.
/// Tests playlist update event creation, properties, and validation.
/// </summary>
public class PlaylistUpdatedEventTests
{
    private const string ValidPlaylistId = "playlist-1";
    private const string ValidPlaylistName = "My Favorites";

    [Fact]
    public void Create_WithMinimalParameters_ShouldCreateEvent()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(ValidPlaylistId, ValidPlaylistName, PlaylistUpdateType.MetadataUpdate);

        // Assert
        Assert.Equal(ValidPlaylistId, evt.PlaylistId);
        Assert.Equal(ValidPlaylistName, evt.PlaylistName);
        Assert.Equal(PlaylistUpdateType.MetadataUpdate, evt.UpdateType);
        Assert.Empty(evt.TrackIds);
        Assert.Empty(evt.AddedTrackIds);
        Assert.Empty(evt.RemovedTrackIds);
        Assert.Empty(evt.TrackMoves);
        Assert.Null(evt.PreviousName);
        Assert.Null(evt.PreviousTotalDurationSeconds);
        Assert.Null(evt.NewTotalDurationSeconds);
        Assert.Null(evt.Owner);
        Assert.Null(evt.UpdatedBy);
        Assert.Null(evt.UpdateReason);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.True(evt.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateEventWithAllProperties()
    {
        // Arrange
        var trackIds = new[] { "track-1", "track-2", "track-3" };
        var addedTrackIds = new[] { "track-2", "track-3" };
        var removedTrackIds = new[] { "track-old" };
        var trackMoves = new[] { TrackMove.Create("track-1", 0, 2), TrackMove.Create("track-2", 1, 0) };
        var previousName = "Old Playlist Name";
        var previousDuration = 1800;
        var newDuration = 2100;
        var owner = "user123";
        var updatedBy = "user456";
        var updateReason = "User reorganization";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.TracksReordered,
            trackIds,
            addedTrackIds,
            removedTrackIds,
            trackMoves,
            previousName,
            previousDuration,
            newDuration,
            owner,
            updatedBy,
            updateReason,
            correlationId
        );

        // Assert
        Assert.Equal(ValidPlaylistId, evt.PlaylistId);
        Assert.Equal(ValidPlaylistName, evt.PlaylistName);
        Assert.Equal(PlaylistUpdateType.TracksReordered, evt.UpdateType);
        Assert.Equal(trackIds, evt.TrackIds);
        Assert.Equal(addedTrackIds, evt.AddedTrackIds);
        Assert.Equal(removedTrackIds, evt.RemovedTrackIds);
        Assert.Equal(2, evt.TrackMoves.Count);
        Assert.Equal(previousName, evt.PreviousName);
        Assert.Equal(previousDuration, evt.PreviousTotalDurationSeconds);
        Assert.Equal(newDuration, evt.NewTotalDurationSeconds);
        Assert.Equal(owner, evt.Owner);
        Assert.Equal(updatedBy, evt.UpdatedBy);
        Assert.Equal(updateReason, evt.UpdateReason);
        Assert.Equal(correlationId, evt.CorrelationId);
    }

    [Fact]
    public void Create_WithNullCollections_ShouldCreateEmptyCollections()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.TracksAdded,
            trackIds: null,
            addedTrackIds: null,
            removedTrackIds: null,
            trackMoves: null
        );

        // Assert
        Assert.Empty(evt.TrackIds);
        Assert.Empty(evt.AddedTrackIds);
        Assert.Empty(evt.RemovedTrackIds);
        Assert.Empty(evt.TrackMoves);
    }

    [Fact]
    public void HasAddedTracks_WithAddedTracks_ShouldReturnTrue()
    {
        // Arrange
        var addedTrackIds = new[] { "track-1", "track-2" };

        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.TracksAdded,
            addedTrackIds: addedTrackIds
        );

        // Assert
        Assert.True(evt.HasAddedTracks);
        Assert.Equal(2, evt.AddedTrackIds.Count);
    }

    [Fact]
    public void HasAddedTracks_WithNoAddedTracks_ShouldReturnFalse()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(ValidPlaylistId, ValidPlaylistName, PlaylistUpdateType.MetadataUpdate);

        // Assert
        Assert.False(evt.HasAddedTracks);
    }

    [Fact]
    public void HasRemovedTracks_WithRemovedTracks_ShouldReturnTrue()
    {
        // Arrange
        var removedTrackIds = new[] { "track-old" };

        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.TracksRemoved,
            removedTrackIds: removedTrackIds
        );

        // Assert
        Assert.True(evt.HasRemovedTracks);
        Assert.Single(evt.RemovedTrackIds);
    }

    [Fact]
    public void HasRemovedTracks_WithNoRemovedTracks_ShouldReturnFalse()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(ValidPlaylistId, ValidPlaylistName, PlaylistUpdateType.MetadataUpdate);

        // Assert
        Assert.False(evt.HasRemovedTracks);
    }

    [Fact]
    public void HasTrackMoves_WithTrackMoves_ShouldReturnTrue()
    {
        // Arrange
        var trackMoves = new[] { TrackMove.Create("track-1", 0, 2) };

        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.TracksReordered,
            trackMoves: trackMoves
        );

        // Assert
        Assert.True(evt.HasTrackMoves);
        Assert.Single(evt.TrackMoves);
    }

    [Fact]
    public void HasTrackMoves_WithNoTrackMoves_ShouldReturnFalse()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(ValidPlaylistId, ValidPlaylistName, PlaylistUpdateType.MetadataUpdate);

        // Assert
        Assert.False(evt.HasTrackMoves);
    }

    [Fact]
    public void WasRenamed_WithDifferentNames_ShouldReturnTrue()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.MetadataUpdate,
            previousName: "Old Name"
        );

        // Assert
        Assert.True(evt.WasRenamed);
        Assert.Equal("Old Name", evt.PreviousName);
        Assert.Equal(ValidPlaylistName, evt.PlaylistName);
    }

    [Fact]
    public void WasRenamed_WithSameName_ShouldReturnFalse()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.MetadataUpdate,
            previousName: ValidPlaylistName
        );

        // Assert
        Assert.False(evt.WasRenamed);
    }

    [Fact]
    public void WasRenamed_WithNullPreviousName_ShouldReturnFalse()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.MetadataUpdate,
            previousName: null
        );

        // Assert
        Assert.False(evt.WasRenamed);
    }

    [Fact]
    public void WasRenamed_WithEmptyPreviousName_ShouldReturnFalse()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.MetadataUpdate,
            previousName: ""
        );

        // Assert
        Assert.False(evt.WasRenamed);
    }

    [Fact]
    public void HasDurationChange_WithDifferentDurations_ShouldReturnTrue()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.TracksAdded,
            previousTotalDurationSeconds: 1800,
            newTotalDurationSeconds: 2100
        );

        // Assert
        Assert.True(evt.HasDurationChange);
        Assert.Equal(1800, evt.PreviousTotalDurationSeconds);
        Assert.Equal(2100, evt.NewTotalDurationSeconds);
    }

    [Fact]
    public void HasDurationChange_WithSameDuration_ShouldReturnFalse()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.MetadataUpdate,
            previousTotalDurationSeconds: 1800,
            newTotalDurationSeconds: 1800
        );

        // Assert
        Assert.False(evt.HasDurationChange);
    }

    [Fact]
    public void HasDurationChange_WithBothNull_ShouldReturnFalse()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.MetadataUpdate,
            previousTotalDurationSeconds: null,
            newTotalDurationSeconds: null
        );

        // Assert
        Assert.False(evt.HasDurationChange);
    }

    [Fact]
    public void Constructor_WithoutParameters_ShouldCreateEventWithDefaults()
    {
        // Act
        var evt = new PlaylistUpdatedEvent
        {
            PlaylistId = ValidPlaylistId,
            PlaylistName = ValidPlaylistName,
            UpdateType = PlaylistUpdateType.MetadataUpdate,
        };

        // Assert
        Assert.Equal(ValidPlaylistId, evt.PlaylistId);
        Assert.Equal(ValidPlaylistName, evt.PlaylistName);
        Assert.Equal(PlaylistUpdateType.MetadataUpdate, evt.UpdateType);
        Assert.Empty(evt.TrackIds);
        Assert.Empty(evt.AddedTrackIds);
        Assert.Empty(evt.RemovedTrackIds);
        Assert.Empty(evt.TrackMoves);
        Assert.Null(evt.CorrelationId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
    }

    [Fact]
    public void Constructor_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = new PlaylistUpdatedEvent(correlationId)
        {
            PlaylistId = ValidPlaylistId,
            PlaylistName = ValidPlaylistName,
            UpdateType = PlaylistUpdateType.MetadataUpdate,
        };

        // Assert
        Assert.Equal(correlationId, evt.CorrelationId);
    }

    [Fact]
    public void Event_ShouldImplementIDomainEvent()
    {
        // Arrange
        var evt = PlaylistUpdatedEvent.Create(ValidPlaylistId, ValidPlaylistName, PlaylistUpdateType.MetadataUpdate);

        // Act & Assert
        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }

    [Fact]
    public void Event_ShouldInheritFromDomainEvent()
    {
        // Arrange
        var evt = PlaylistUpdatedEvent.Create(ValidPlaylistId, ValidPlaylistName, PlaylistUpdateType.MetadataUpdate);

        // Act & Assert
        Assert.IsAssignableFrom<DomainEvent>(evt);
    }

    [Fact]
    public void RecordEquality_WithSameData_ShouldNotBeEqualDueToEventId()
    {
        // Arrange
        var evt1 = PlaylistUpdatedEvent.Create(ValidPlaylistId, ValidPlaylistName, PlaylistUpdateType.MetadataUpdate);
        var evt2 = PlaylistUpdatedEvent.Create(ValidPlaylistId, ValidPlaylistName, PlaylistUpdateType.MetadataUpdate);

        // Act & Assert
        Assert.NotEqual(evt1, evt2); // Different EventIds and timestamps
        Assert.NotEqual(evt1.EventId, evt2.EventId);
    }

    [Fact]
    public void RecordEquality_WithSameInstance_ShouldBeEqual()
    {
        // Arrange
        var evt = PlaylistUpdatedEvent.Create(ValidPlaylistId, ValidPlaylistName, PlaylistUpdateType.MetadataUpdate);

        // Act & Assert
        Assert.Equal(evt, evt);
    }

    [Fact]
    public void ToString_ShouldContainRelevantInformation()
    {
        // Arrange
        var evt = PlaylistUpdatedEvent.Create(ValidPlaylistId, ValidPlaylistName, PlaylistUpdateType.TracksAdded);

        // Act
        var stringRepresentation = evt.ToString();

        // Assert
        Assert.Contains(ValidPlaylistId, stringRepresentation);
        Assert.Contains(ValidPlaylistName, stringRepresentation);
        Assert.Contains("TracksAdded", stringRepresentation);
    }

    [Fact]
    public void PlaylistUpdateScenarios_ShouldCoverCommonCases()
    {
        // Metadata update (rename)
        var metadataUpdate = PlaylistUpdatedEvent.Create(
            "playlist-favorites",
            "My Updated Favorites",
            PlaylistUpdateType.MetadataUpdate,
            previousName: "My Favorites",
            updatedBy: "user123",
            updateReason: "Better name"
        );

        Assert.Equal(PlaylistUpdateType.MetadataUpdate, metadataUpdate.UpdateType);
        Assert.True(metadataUpdate.WasRenamed);
        Assert.Equal("My Favorites", metadataUpdate.PreviousName);
        Assert.Equal("user123", metadataUpdate.UpdatedBy);

        // Tracks added
        var tracksAdded = PlaylistUpdatedEvent.Create(
            "playlist-rock",
            "Rock Classics",
            PlaylistUpdateType.TracksAdded,
            trackIds: new[] { "track-1", "track-2", "track-3", "track-4" },
            addedTrackIds: new[] { "track-3", "track-4" },
            previousTotalDurationSeconds: 1200,
            newTotalDurationSeconds: 1500,
            owner: "user456",
            updatedBy: "user456",
            updateReason: "Added new discoveries"
        );

        Assert.True(tracksAdded.HasAddedTracks);
        Assert.Equal(2, tracksAdded.AddedTrackIds.Count);
        Assert.True(tracksAdded.HasDurationChange);
        Assert.Equal(300, tracksAdded.NewTotalDurationSeconds - tracksAdded.PreviousTotalDurationSeconds);

        // Tracks removed
        var tracksRemoved = PlaylistUpdatedEvent.Create(
            "playlist-chill",
            "Chill Vibes",
            PlaylistUpdateType.TracksRemoved,
            trackIds: new[] { "track-1", "track-2" },
            removedTrackIds: new[] { "track-old-1", "track-old-2" },
            previousTotalDurationSeconds: 2000,
            newTotalDurationSeconds: 1400,
            updatedBy: "user789",
            updateReason: "Removed outdated tracks"
        );

        Assert.True(tracksRemoved.HasRemovedTracks);
        Assert.Equal(2, tracksRemoved.RemovedTrackIds.Count);
        Assert.True(tracksRemoved.HasDurationChange);
        Assert.True(tracksRemoved.NewTotalDurationSeconds < tracksRemoved.PreviousTotalDurationSeconds);

        // Tracks reordered
        var tracksReordered = PlaylistUpdatedEvent.Create(
            "playlist-workout",
            "Workout Mix",
            PlaylistUpdateType.TracksReordered,
            trackIds: new[] { "track-3", "track-1", "track-2" },
            trackMoves: new[]
            {
                TrackMove.Create("track-3", 2, 0),
                TrackMove.Create("track-1", 0, 1),
                TrackMove.Create("track-2", 1, 2),
            },
            updatedBy: "user101",
            updateReason: "Better workout flow"
        );

        Assert.True(tracksReordered.HasTrackMoves);
        Assert.Equal(3, tracksReordered.TrackMoves.Count);
        Assert.False(tracksReordered.HasAddedTracks);
        Assert.False(tracksReordered.HasRemovedTracks);

        // Playlist shuffled
        var playlistShuffled = PlaylistUpdatedEvent.Create(
            "playlist-party",
            "Party Mix",
            PlaylistUpdateType.Shuffled,
            trackIds: new[] { "track-5", "track-1", "track-3", "track-2", "track-4" },
            updatedBy: "user202",
            updateReason: "Shuffle for variety"
        );

        Assert.Equal(PlaylistUpdateType.Shuffled, playlistShuffled.UpdateType);
        Assert.Equal(5, playlistShuffled.TrackIds.Count);

        // Playlist cleared
        var playlistCleared = PlaylistUpdatedEvent.Create(
            "playlist-temp",
            "Temporary Playlist",
            PlaylistUpdateType.Cleared,
            trackIds: Array.Empty<string>(),
            removedTrackIds: new[] { "track-1", "track-2", "track-3" },
            previousTotalDurationSeconds: 900,
            newTotalDurationSeconds: 0,
            updatedBy: "user303",
            updateReason: "Clean slate"
        );

        Assert.Equal(PlaylistUpdateType.Cleared, playlistCleared.UpdateType);
        Assert.Empty(playlistCleared.TrackIds);
        Assert.True(playlistCleared.HasRemovedTracks);
        Assert.Equal(0, playlistCleared.NewTotalDurationSeconds);

        // Playlist replaced
        var playlistReplaced = PlaylistUpdatedEvent.Create(
            "playlist-daily",
            "Daily Mix",
            PlaylistUpdateType.Replaced,
            trackIds: new[] { "new-1", "new-2", "new-3" },
            addedTrackIds: new[] { "new-1", "new-2", "new-3" },
            removedTrackIds: new[] { "old-1", "old-2" },
            previousTotalDurationSeconds: 800,
            newTotalDurationSeconds: 1100,
            updatedBy: "system",
            updateReason: "Daily refresh"
        );

        Assert.Equal(PlaylistUpdateType.Replaced, playlistReplaced.UpdateType);
        Assert.True(playlistReplaced.HasAddedTracks);
        Assert.True(playlistReplaced.HasRemovedTracks);
        Assert.True(playlistReplaced.HasDurationChange);
        Assert.Equal("system", playlistReplaced.UpdatedBy);
    }

    [Fact]
    public void ImmutableCollections_ShouldBeImmutable()
    {
        // Arrange
        var originalTrackIds = new[] { "track-1", "track-2" };
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.TracksAdded,
            trackIds: originalTrackIds
        );

        // Act & Assert - Should not be able to modify the collections
        Assert.IsType<ImmutableList<string>>(evt.TrackIds);
        Assert.IsType<ImmutableList<string>>(evt.AddedTrackIds);
        Assert.IsType<ImmutableList<string>>(evt.RemovedTrackIds);
        Assert.IsType<ImmutableList<TrackMove>>(evt.TrackMoves);

        // Verify the collections contain the expected data
        Assert.Equal(originalTrackIds, evt.TrackIds);
    }

    [Fact]
    public void Create_WithEmptyCollections_ShouldCreateEmptyImmutableCollections()
    {
        // Act
        var evt = PlaylistUpdatedEvent.Create(
            ValidPlaylistId,
            ValidPlaylistName,
            PlaylistUpdateType.MetadataUpdate,
            trackIds: Array.Empty<string>(),
            addedTrackIds: Array.Empty<string>(),
            removedTrackIds: Array.Empty<string>(),
            trackMoves: Array.Empty<TrackMove>()
        );

        // Assert
        Assert.Empty(evt.TrackIds);
        Assert.Empty(evt.AddedTrackIds);
        Assert.Empty(evt.RemovedTrackIds);
        Assert.Empty(evt.TrackMoves);
        Assert.False(evt.HasAddedTracks);
        Assert.False(evt.HasRemovedTracks);
        Assert.False(evt.HasTrackMoves);
    }
}

/// <summary>
/// Unit tests for the TrackMove record.
/// </summary>
public class TrackMoveTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateTrackMove()
    {
        // Arrange
        var trackId = "track-123";
        var fromPosition = 0;
        var toPosition = 3;

        // Act
        var trackMove = TrackMove.Create(trackId, fromPosition, toPosition);

        // Assert
        Assert.Equal(trackId, trackMove.TrackId);
        Assert.Equal(fromPosition, trackMove.FromPosition);
        Assert.Equal(toPosition, trackMove.ToPosition);
    }

    [Fact]
    public void Constructor_WithRequiredProperties_ShouldCreateTrackMove()
    {
        // Act
        var trackMove = new TrackMove
        {
            TrackId = "track-456",
            FromPosition = 2,
            ToPosition = 0,
        };

        // Assert
        Assert.Equal("track-456", trackMove.TrackId);
        Assert.Equal(2, trackMove.FromPosition);
        Assert.Equal(0, trackMove.ToPosition);
    }

    [Fact]
    public void RecordEquality_WithSameData_ShouldBeEqual()
    {
        // Arrange
        var trackMove1 = TrackMove.Create("track-1", 0, 2);
        var trackMove2 = TrackMove.Create("track-1", 0, 2);

        // Act & Assert
        Assert.Equal(trackMove1, trackMove2);
        Assert.Equal(trackMove1.GetHashCode(), trackMove2.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WithDifferentData_ShouldNotBeEqual()
    {
        // Arrange
        var trackMove1 = TrackMove.Create("track-1", 0, 2);
        var trackMove2 = TrackMove.Create("track-2", 0, 2);
        var trackMove3 = TrackMove.Create("track-1", 1, 2);
        var trackMove4 = TrackMove.Create("track-1", 0, 3);

        // Act & Assert
        Assert.NotEqual(trackMove1, trackMove2);
        Assert.NotEqual(trackMove1, trackMove3);
        Assert.NotEqual(trackMove1, trackMove4);
    }

    [Fact]
    public void ToString_ShouldContainRelevantInformation()
    {
        // Arrange
        var trackMove = TrackMove.Create("track-789", 5, 1);

        // Act
        var stringRepresentation = trackMove.ToString();

        // Assert
        Assert.Contains("track-789", stringRepresentation);
        Assert.Contains("5", stringRepresentation);
        Assert.Contains("1", stringRepresentation);
    }
}

/// <summary>
/// Unit tests for the PlaylistUpdateType enum.
/// </summary>
public class PlaylistUpdateTypeTests
{
    [Fact]
    public void PlaylistUpdateType_ShouldHaveExpectedValues()
    {
        // Assert all enum values exist
        Assert.True(Enum.IsDefined(typeof(PlaylistUpdateType), PlaylistUpdateType.MetadataUpdate));
        Assert.True(Enum.IsDefined(typeof(PlaylistUpdateType), PlaylistUpdateType.TracksAdded));
        Assert.True(Enum.IsDefined(typeof(PlaylistUpdateType), PlaylistUpdateType.TracksRemoved));
        Assert.True(Enum.IsDefined(typeof(PlaylistUpdateType), PlaylistUpdateType.TracksReordered));
        Assert.True(Enum.IsDefined(typeof(PlaylistUpdateType), PlaylistUpdateType.Shuffled));
        Assert.True(Enum.IsDefined(typeof(PlaylistUpdateType), PlaylistUpdateType.Replaced));
        Assert.True(Enum.IsDefined(typeof(PlaylistUpdateType), PlaylistUpdateType.Cleared));
    }

    [Fact]
    public void PlaylistUpdateType_ShouldHaveCorrectCount()
    {
        // Act
        var enumValues = Enum.GetValues<PlaylistUpdateType>();

        // Assert
        Assert.Equal(7, enumValues.Length);
    }
}
