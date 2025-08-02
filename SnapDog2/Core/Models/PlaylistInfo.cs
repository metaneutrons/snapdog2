namespace SnapDog2.Core.Models;

/// <summary>
/// Represents detailed information about a playlist.
/// </summary>
public record PlaylistInfo
{
    /// <summary>
    /// Gets the playlist identifier (can be "radio" for radio stations).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the playlist name (can be "Radio Stations" for radio).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the 1-based playlist index (1=Radio, 2=First Subsonic, etc.).
    /// </summary>
    public int? Index { get; init; }

    /// <summary>
    /// Gets the total number of tracks in the playlist.
    /// </summary>
    public int TrackCount { get; init; }

    /// <summary>
    /// Gets the total duration of the playlist in seconds (null for radio).
    /// </summary>
    public int? TotalDurationSec { get; init; }

    /// <summary>
    /// Gets the playlist description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the playlist cover art URL.
    /// </summary>
    public string? CoverArtUrl { get; init; }

    /// <summary>
    /// Gets the source type ("subsonic" or "radio").
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the playlist info was recorded.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
