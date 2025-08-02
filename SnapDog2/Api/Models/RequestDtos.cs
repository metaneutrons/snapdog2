namespace SnapDog2.Api.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to play media in a zone.
/// </summary>
public record PlayRequest
{
    /// <summary>
    /// Gets or sets the media URL to play.
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>
    /// Gets or sets the track index to play (1-based).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? TrackIndex { get; set; }
}

/// <summary>
/// Request to set a specific track.
/// </summary>
public record SetTrackRequest
{
    /// <summary>
    /// Gets or sets the track index (1-based).
    /// </summary>
    [Range(1, int.MaxValue)]
    public required int Index { get; set; }
}

/// <summary>
/// Request to set a specific playlist.
/// </summary>
public record SetPlaylistRequest
{
    /// <summary>
    /// Gets or sets the playlist ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the playlist index (1-based).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? Index { get; set; }
}

/// <summary>
/// Request to set volume level.
/// </summary>
public record VolumeSetRequest
{
    /// <summary>
    /// Gets or sets the volume level (0-100).
    /// </summary>
    [Range(0, 100)]
    public required int Level { get; set; }
}

/// <summary>
/// Request to set mute state.
/// </summary>
public record MuteSetRequest
{
    /// <summary>
    /// Gets or sets whether mute is enabled.
    /// </summary>
    public required bool Enabled { get; set; }
}

/// <summary>
/// Request to set mode (repeat/shuffle).
/// </summary>
public record ModeSetRequest
{
    /// <summary>
    /// Gets or sets whether the mode is enabled.
    /// </summary>
    public required bool Enabled { get; set; }
}

/// <summary>
/// Request for volume step operations.
/// </summary>
public record StepRequest
{
    /// <summary>
    /// Gets or sets the step size.
    /// </summary>
    [Range(1, 100)]
    public int Step { get; set; } = 5;
}

/// <summary>
/// Request to set client latency.
/// </summary>
public record LatencySetRequest
{
    /// <summary>
    /// Gets or sets the latency in milliseconds.
    /// </summary>
    [Range(0, 10000)]
    public required int Milliseconds { get; set; }
}

/// <summary>
/// Request to assign client to zone.
/// </summary>
public record AssignZoneRequest
{
    /// <summary>
    /// Gets or sets the zone ID (1-based).
    /// </summary>
    [Range(1, int.MaxValue)]
    public required int ZoneId { get; set; }
}

/// <summary>
/// Request to rename a client.
/// </summary>
public record RenameRequest
{
    /// <summary>
    /// Gets or sets the new name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; set; }
}
