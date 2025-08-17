namespace SnapDog2.Core.Models;

/// <summary>
/// Represents detailed information about a track.
/// Aligned with LibVLC MetadataType and Command Framework requirements.
/// </summary>
public record TrackInfo
{
    /// <summary>
    /// Gets the track index within the current playlist (1-based).
    /// Maps to TRACK_INDEX status.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Gets the track title.
    /// Maps to TRACK_METADATA_TITLE status from LibVLC MetadataType.Title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the artist name or "Radio" for radio streams.
    /// Maps to TRACK_METADATA_ARTIST status from LibVLC MetadataType.Artist.
    /// </summary>
    public required string Artist { get; init; }

    /// <summary>
    /// Gets the album name or playlist name for radio.
    /// Maps to TRACK_METADATA_ALBUM status from LibVLC MetadataType.Album.
    /// </summary>
    public string? Album { get; init; }

    /// <summary>
    /// Gets the track duration in milliseconds (null for radio/streams).
    /// Maps to TRACK_METADATA_DURATION status from LibVLC Media.Duration.
    /// </summary>
    public long? DurationMs { get; init; }

    /// <summary>
    /// Gets the current playback position in milliseconds.
    /// Maps to TRACK_POSITION_STATUS from MediaPlayer position.
    /// </summary>
    public long PositionMs { get; init; }

    /// <summary>
    /// Gets the current playback progress as percentage (0.0-1.0).
    /// Maps to TRACK_PROGRESS_STATUS calculated from position/duration.
    /// </summary>
    public float Progress { get; init; }

    /// <summary>
    /// Gets the cover art URL.
    /// Maps to TRACK_METADATA_COVER status from LibVLC MetadataType.ArtworkURL.
    /// </summary>
    public string? CoverArtUrl { get; init; }

    /// <summary>
    /// Gets the genre information.
    /// From LibVLC MetadataType.Genre.
    /// </summary>
    public string? Genre { get; init; }

    /// <summary>
    /// Gets the track number within the album.
    /// From LibVLC MetadataType.TrackNumber.
    /// </summary>
    public int? TrackNumber { get; init; }

    /// <summary>
    /// Gets the release year.
    /// From LibVLC MetadataType.Date.
    /// </summary>
    public int? Year { get; init; }

    /// <summary>
    /// Gets the track rating (0.0-1.0).
    /// From LibVLC MetadataType.Rating.
    /// </summary>
    public float? Rating { get; init; }

    /// <summary>
    /// Gets whether the track is currently playing.
    /// Maps to TRACK_PLAYING_STATUS from MediaPlayer state.
    /// </summary>
    public bool IsPlaying { get; init; }

    /// <summary>
    /// Gets the source type ("subsonic", "radio", "file", "stream").
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the media URL for playback (stream URL for radio, file path for local files).
    /// Used by MediaPlayerService for LibVLC playback.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the track info was recorded.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the complete metadata object with all LibVLC metadata.
    /// </summary>
    public AudioMetadata? Metadata { get; init; }
}
