using System.Collections.Immutable;

namespace SnapDog2.Core.Events;

/// <summary>
/// Domain event triggered when a playlist is updated.
/// Contains information about the playlist and the changes made.
/// </summary>
public sealed record PlaylistUpdatedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the playlist.
    /// </summary>
    public required string PlaylistId { get; init; }

    /// <summary>
    /// Gets the name of the playlist.
    /// </summary>
    public required string PlaylistName { get; init; }

    /// <summary>
    /// Gets the type of update that occurred.
    /// </summary>
    public required PlaylistUpdateType UpdateType { get; init; }

    /// <summary>
    /// Gets the current list of track IDs in the playlist.
    /// </summary>
    public ImmutableList<string> TrackIds { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Gets the list of track IDs that were added to the playlist.
    /// </summary>
    public ImmutableList<string> AddedTrackIds { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Gets the list of track IDs that were removed from the playlist.
    /// </summary>
    public ImmutableList<string> RemovedTrackIds { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Gets the track moves that occurred (from position to position).
    /// </summary>
    public ImmutableList<TrackMove> TrackMoves { get; init; } = ImmutableList<TrackMove>.Empty;

    /// <summary>
    /// Gets the previous name of the playlist, if it was renamed.
    /// </summary>
    public string? PreviousName { get; init; }

    /// <summary>
    /// Gets the previous total duration in seconds, if it changed.
    /// </summary>
    public int? PreviousTotalDurationSeconds { get; init; }

    /// <summary>
    /// Gets the new total duration in seconds, if it changed.
    /// </summary>
    public int? NewTotalDurationSeconds { get; init; }

    /// <summary>
    /// Gets the owner of the playlist.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Gets the user or system that made the update.
    /// </summary>
    public string? UpdatedBy { get; init; }

    /// <summary>
    /// Gets the reason for the update.
    /// </summary>
    public string? UpdateReason { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistUpdatedEvent"/> record.
    /// </summary>
    public PlaylistUpdatedEvent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistUpdatedEvent"/> record with a correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    public PlaylistUpdatedEvent(string? correlationId)
        : base(correlationId) { }

    /// <summary>
    /// Creates a new playlist updated event.
    /// </summary>
    /// <param name="playlistId">The unique identifier of the playlist.</param>
    /// <param name="playlistName">The name of the playlist.</param>
    /// <param name="updateType">The type of update that occurred.</param>
    /// <param name="trackIds">The current list of track IDs in the playlist.</param>
    /// <param name="addedTrackIds">The list of track IDs that were added to the playlist.</param>
    /// <param name="removedTrackIds">The list of track IDs that were removed from the playlist.</param>
    /// <param name="trackMoves">The track moves that occurred.</param>
    /// <param name="previousName">The previous name of the playlist, if it was renamed.</param>
    /// <param name="previousTotalDurationSeconds">The previous total duration in seconds, if it changed.</param>
    /// <param name="newTotalDurationSeconds">The new total duration in seconds, if it changed.</param>
    /// <param name="owner">The owner of the playlist.</param>
    /// <param name="updatedBy">The user or system that made the update.</param>
    /// <param name="updateReason">The reason for the update.</param>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    /// <returns>A new <see cref="PlaylistUpdatedEvent"/> instance.</returns>
    public static PlaylistUpdatedEvent Create(
        string playlistId,
        string playlistName,
        PlaylistUpdateType updateType,
        IEnumerable<string>? trackIds = null,
        IEnumerable<string>? addedTrackIds = null,
        IEnumerable<string>? removedTrackIds = null,
        IEnumerable<TrackMove>? trackMoves = null,
        string? previousName = null,
        int? previousTotalDurationSeconds = null,
        int? newTotalDurationSeconds = null,
        string? owner = null,
        string? updatedBy = null,
        string? updateReason = null,
        string? correlationId = null
    )
    {
        return new PlaylistUpdatedEvent(correlationId)
        {
            PlaylistId = playlistId,
            PlaylistName = playlistName,
            UpdateType = updateType,
            TrackIds = trackIds?.ToImmutableList() ?? ImmutableList<string>.Empty,
            AddedTrackIds = addedTrackIds?.ToImmutableList() ?? ImmutableList<string>.Empty,
            RemovedTrackIds = removedTrackIds?.ToImmutableList() ?? ImmutableList<string>.Empty,
            TrackMoves = trackMoves?.ToImmutableList() ?? ImmutableList<TrackMove>.Empty,
            PreviousName = previousName,
            PreviousTotalDurationSeconds = previousTotalDurationSeconds,
            NewTotalDurationSeconds = newTotalDurationSeconds,
            Owner = owner,
            UpdatedBy = updatedBy,
            UpdateReason = updateReason,
        };
    }

    /// <summary>
    /// Gets a value indicating whether tracks were added to the playlist.
    /// </summary>
    public bool HasAddedTracks => AddedTrackIds.Count > 0;

    /// <summary>
    /// Gets a value indicating whether tracks were removed from the playlist.
    /// </summary>
    public bool HasRemovedTracks => RemovedTrackIds.Count > 0;

    /// <summary>
    /// Gets a value indicating whether tracks were moved within the playlist.
    /// </summary>
    public bool HasTrackMoves => TrackMoves.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the playlist was renamed.
    /// </summary>
    public bool WasRenamed => !string.IsNullOrEmpty(PreviousName) && PreviousName != PlaylistName;

    /// <summary>
    /// Gets a value indicating whether the playlist duration changed.
    /// </summary>
    public bool HasDurationChange => PreviousTotalDurationSeconds != NewTotalDurationSeconds;
}

/// <summary>
/// Represents the type of update that occurred to a playlist.
/// </summary>
public enum PlaylistUpdateType
{
    /// <summary>
    /// The playlist metadata was updated (name, description, etc.).
    /// </summary>
    MetadataUpdate,

    /// <summary>
    /// One or more tracks were added to the playlist.
    /// </summary>
    TracksAdded,

    /// <summary>
    /// One or more tracks were removed from the playlist.
    /// </summary>
    TracksRemoved,

    /// <summary>
    /// Tracks were reordered within the playlist.
    /// </summary>
    TracksReordered,

    /// <summary>
    /// The playlist was shuffled.
    /// </summary>
    Shuffled,

    /// <summary>
    /// The playlist was completely replaced with new tracks.
    /// </summary>
    Replaced,

    /// <summary>
    /// The playlist was cleared of all tracks.
    /// </summary>
    Cleared,
}

/// <summary>
/// Represents a track move within a playlist.
/// </summary>
public sealed record TrackMove
{
    /// <summary>
    /// Gets the ID of the track that was moved.
    /// </summary>
    public required string TrackId { get; init; }

    /// <summary>
    /// Gets the original position of the track (0-based index).
    /// </summary>
    public required int FromPosition { get; init; }

    /// <summary>
    /// Gets the new position of the track (0-based index).
    /// </summary>
    public required int ToPosition { get; init; }

    /// <summary>
    /// Creates a new track move.
    /// </summary>
    /// <param name="trackId">The ID of the track that was moved.</param>
    /// <param name="fromPosition">The original position of the track.</param>
    /// <param name="toPosition">The new position of the track.</param>
    /// <returns>A new <see cref="TrackMove"/> instance.</returns>
    public static TrackMove Create(string trackId, int fromPosition, int toPosition)
    {
        return new TrackMove
        {
            TrackId = trackId,
            FromPosition = fromPosition,
            ToPosition = toPosition,
        };
    }
}
