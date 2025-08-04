namespace SnapDog2.Core.Models;

/// <summary>
/// Playback status information for a zone.
/// </summary>
public class PlaybackStatus
{
    /// <summary>
    /// Zone ID for this playback status.
    /// </summary>
    public int ZoneId { get; set; }

    /// <summary>
    /// Whether audio is currently playing in this zone.
    /// </summary>
    public bool IsPlaying { get; set; }

    /// <summary>
    /// Number of currently active audio streams across all zones.
    /// </summary>
    public int ActiveStreams { get; set; }

    /// <summary>
    /// Maximum number of concurrent streams allowed.
    /// </summary>
    public int MaxStreams { get; set; }

    /// <summary>
    /// Current track information if playing.
    /// </summary>
    public TrackInfo? CurrentTrack { get; set; }

    /// <summary>
    /// Audio format being used for playback.
    /// </summary>
    public AudioFormat? AudioFormat { get; set; }

    /// <summary>
    /// Timestamp when playback started (UTC).
    /// </summary>
    public DateTime? PlaybackStartedAt { get; set; }
}
