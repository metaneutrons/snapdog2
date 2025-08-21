namespace SnapDog2.Core.Models;

/// <summary>
/// Represents the complete state of a zone.
/// </summary>
public record ZoneState
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the zone name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    public required SnapDog2.Core.Enums.PlaybackState PlaybackState { get; init; }

    /// <summary>
    /// Gets the zone volume (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets whether the zone is muted.
    /// </summary>
    public required bool Mute { get; init; }

    /// <summary>
    /// Gets whether track repeat is enabled.
    /// </summary>
    public required bool TrackRepeat { get; init; }

    /// <summary>
    /// Gets whether playlist repeat is enabled.
    /// </summary>
    public required bool PlaylistRepeat { get; init; }

    /// <summary>
    /// Gets whether playlist shuffle is enabled.
    /// </summary>
    public required bool PlaylistShuffle { get; init; }

    /// <summary>
    /// Gets the Snapcast group ID.
    /// </summary>
    public required string SnapcastGroupId { get; init; }

    /// <summary>
    /// Gets the Snapcast stream ID.
    /// </summary>
    public required string SnapcastStreamId { get; init; }

    /// <summary>
    /// Gets whether the Snapcast group is muted (raw state from Snapcast).
    /// </summary>
    public required bool IsSnapcastGroupMuted { get; init; }

    /// <summary>
    /// Gets the current playlist information.
    /// </summary>
    public PlaylistInfo? Playlist { get; init; }

    /// <summary>
    /// Gets the current track information.
    /// </summary>
    public TrackInfo? Track { get; init; }

    /// <summary>
    /// Gets the list of SnapDog2 Client IDs currently in this zone.
    /// </summary>
    public required int[] Clients { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the zone state was recorded.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
