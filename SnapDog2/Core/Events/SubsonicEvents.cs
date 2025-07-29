namespace SnapDog2.Core.Events;

/// <summary>
/// Base class for Subsonic-related events.
/// </summary>
public abstract record SubsonicEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Event published when Subsonic authentication is successful.
/// </summary>
public record SubsonicAuthenticatedEvent() : SubsonicEvent;

/// <summary>
/// Event published when a track is streamed from Subsonic.
/// </summary>
/// <param name="TrackId">The track identifier</param>
public record SubsonicTrackStreamedEvent(string TrackId) : SubsonicEvent;

/// <summary>
/// Event published when a playlist is created on Subsonic.
/// </summary>
/// <param name="PlaylistId">The playlist identifier</param>
/// <param name="Name">The playlist name</param>
public record SubsonicPlaylistCreatedEvent(int PlaylistId, string Name) : SubsonicEvent;

/// <summary>
/// Event published when a playlist is updated on Subsonic.
/// </summary>
/// <param name="PlaylistId">The playlist identifier</param>
public record SubsonicPlaylistUpdatedEvent(string PlaylistId) : SubsonicEvent;

/// <summary>
/// Event published when a playlist is deleted from Subsonic.
/// </summary>
/// <param name="PlaylistId">The playlist identifier</param>
public record SubsonicPlaylistDeletedEvent(string PlaylistId) : SubsonicEvent;