namespace SnapDog2.Core.Models;

/// <summary>
/// Represents detailed information about a track.
/// </summary>
public record TrackInfo
{
    /// <summary>
    /// Gets the track index within the current playlist (1-based).
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Gets the unique track identifier or stream URL for radio.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the track title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the artist name or "Radio" for radio streams.
    /// </summary>
    public required string Artist { get; init; }

    /// <summary>
    /// Gets the album name or playlist name for radio.
    /// </summary>
    public string? Album { get; init; }

    /// <summary>
    /// Gets the track duration in seconds (null for radio/streams).
    /// </summary>
    public int? DurationSec { get; init; }

    /// <summary>
    /// Gets the current playback position in seconds.
    /// </summary>
    public int PositionSec { get; init; }

    /// <summary>
    /// Gets the cover art URL.
    /// </summary>
    public string? CoverArtUrl { get; init; }

    /// <summary>
    /// Gets the source type ("subsonic" or "radio").
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the track info was recorded.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
