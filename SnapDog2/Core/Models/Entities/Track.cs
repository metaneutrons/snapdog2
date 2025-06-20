using System.Collections.Immutable;
using System.Globalization;

namespace SnapDog2.Core.Models.Entities;

/// <summary>
/// Represents an individual music track with metadata information.
/// Immutable domain entity for the SnapDog2 multi-audio zone management system.
/// </summary>
public sealed record Track
{
    /// <summary>
    /// Gets the unique identifier for the track.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the title of the track.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the artist(s) of the track.
    /// </summary>
    public string? Artist { get; init; }

    /// <summary>
    /// Gets the album name.
    /// </summary>
    public string? Album { get; init; }

    /// <summary>
    /// Gets the genre of the track.
    /// </summary>
    public string? Genre { get; init; }

    /// <summary>
    /// Gets the release year of the track.
    /// </summary>
    public int? Year { get; init; }

    /// <summary>
    /// Gets the track number within the album.
    /// </summary>
    public int? TrackNumber { get; init; }

    /// <summary>
    /// Gets the total number of tracks in the album.
    /// </summary>
    public int? TotalTracks { get; init; }

    /// <summary>
    /// Gets the duration of the track in seconds.
    /// </summary>
    public int? DurationSeconds { get; init; }

    /// <summary>
    /// Gets the file path or URL of the track.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; init; }

    /// <summary>
    /// Gets the bitrate of the track in kilobits per second.
    /// </summary>
    public int? BitrateKbps { get; init; }

    /// <summary>
    /// Gets the sample rate in Hz.
    /// </summary>
    public int? SampleRateHz { get; init; }

    /// <summary>
    /// Gets the number of audio channels.
    /// </summary>
    public int? Channels { get; init; }

    /// <summary>
    /// Gets the file format/codec of the track.
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Gets the album artist (different from track artist for compilations).
    /// </summary>
    public string? AlbumArtist { get; init; }

    /// <summary>
    /// Gets the composer of the track.
    /// </summary>
    public string? Composer { get; init; }

    /// <summary>
    /// Gets the conductor information.
    /// </summary>
    public string? Conductor { get; init; }

    /// <summary>
    /// Gets the record label.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets the International Standard Recording Code.
    /// </summary>
    public string? ISRC { get; init; }

    /// <summary>
    /// Gets the MusicBrainz track ID.
    /// </summary>
    public string? MusicBrainzTrackId { get; init; }

    /// <summary>
    /// Gets the MusicBrainz recording ID.
    /// </summary>
    public string? MusicBrainzRecordingId { get; init; }

    /// <summary>
    /// Gets additional tags associated with the track.
    /// </summary>
    public ImmutableDictionary<string, string> Tags { get; init; } = ImmutableDictionary<string, string>.Empty;

    /// <summary>
    /// Gets the path to album artwork file.
    /// </summary>
    public string? ArtworkPath { get; init; }

    /// <summary>
    /// Gets the lyrics of the track.
    /// </summary>
    public string? Lyrics { get; init; }

    /// <summary>
    /// Gets the timestamp when the track was added to the system.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the track was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the track was last played.
    /// </summary>
    public DateTime? LastPlayedAt { get; init; }

    /// <summary>
    /// Gets the number of times the track has been played.
    /// </summary>
    public int PlayCount { get; init; } = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="Track"/> record.
    /// </summary>
    public Track()
    {
        // Required properties must be set via object initializer
    }

    /// <summary>
    /// Creates a new track with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the track.</param>
    /// <param name="title">The title of the track.</param>
    /// <param name="artist">The artist of the track.</param>
    /// <param name="album">The album name.</param>
    /// <returns>A new <see cref="Track"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static Track Create(string id, string title, string? artist = null, string? album = null)
    {
        ValidateParameters(id, title);

        return new Track
        {
            Id = id,
            Title = title,
            Artist = artist,
            Album = album,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current track with updated metadata.
    /// </summary>
    /// <param name="artist">The updated artist information.</param>
    /// <param name="album">The updated album information.</param>
    /// <param name="genre">The updated genre information.</param>
    /// <param name="year">The updated year information.</param>
    /// <returns>A new <see cref="Track"/> instance with updated metadata.</returns>
    public Track WithMetadata(string? artist = null, string? album = null, string? genre = null, int? year = null)
    {
        return this with
        {
            Artist = artist ?? Artist,
            Album = album ?? Album,
            Genre = genre ?? Genre,
            Year = year ?? Year,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current track with updated technical information.
    /// </summary>
    /// <param name="durationSeconds">The duration in seconds.</param>
    /// <param name="bitrateKbps">The bitrate in kilobits per second.</param>
    /// <param name="sampleRateHz">The sample rate in Hz.</param>
    /// <param name="channels">The number of audio channels.</param>
    /// <param name="format">The file format/codec.</param>
    /// <returns>A new <see cref="Track"/> instance with updated technical information.</returns>
    public Track WithTechnicalInfo(
        int? durationSeconds = null,
        int? bitrateKbps = null,
        int? sampleRateHz = null,
        int? channels = null,
        string? format = null
    )
    {
        return this with
        {
            DurationSeconds = durationSeconds ?? DurationSeconds,
            BitrateKbps = bitrateKbps ?? BitrateKbps,
            SampleRateHz = sampleRateHz ?? SampleRateHz,
            Channels = channels ?? Channels,
            Format = format ?? Format,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current track with an additional tag.
    /// </summary>
    /// <param name="key">The tag key.</param>
    /// <param name="value">The tag value.</param>
    /// <returns>A new <see cref="Track"/> instance with the added tag.</returns>
    /// <exception cref="ArgumentException">Thrown when key is invalid.</exception>
    public Track WithTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Tag key cannot be null or empty.", nameof(key));
        }

        return this with
        {
            Tags = Tags.SetItem(key, value ?? string.Empty),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current track with a removed tag.
    /// </summary>
    /// <param name="key">The tag key to remove.</param>
    /// <returns>A new <see cref="Track"/> instance with the tag removed.</returns>
    public Track WithoutTag(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !Tags.ContainsKey(key))
        {
            return this;
        }

        return this with
        {
            Tags = Tags.Remove(key),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current track with incremented play count and updated last played time.
    /// </summary>
    /// <returns>A new <see cref="Track"/> instance with updated play statistics.</returns>
    public Track WithPlayIncrement()
    {
        return this with { PlayCount = PlayCount + 1, LastPlayedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Gets the duration of the track as a TimeSpan.
    /// </summary>
    public TimeSpan? Duration => DurationSeconds.HasValue ? TimeSpan.FromSeconds(DurationSeconds.Value) : null;

    /// <summary>
    /// Gets the formatted duration string (mm:ss).
    /// </summary>
    public string FormattedDuration
    {
        get
        {
            if (!DurationSeconds.HasValue)
            {
                return "--:--";
            }

            var duration = TimeSpan.FromSeconds(DurationSeconds.Value);
            return duration.TotalHours >= 1 ? duration.ToString(@"h\:mm\:ss") : duration.ToString(@"m\:ss");
        }
    }

    /// <summary>
    /// Gets the file size formatted as a human-readable string.
    /// </summary>
    public string FormattedFileSize
    {
        get
        {
            if (!FileSizeBytes.HasValue)
            {
                return "Unknown";
            }

            var bytes = FileSizeBytes.Value;
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            double size = bytes;

            while (size >= 1024 && counter < suffixes.Length - 1)
            {
                size /= 1024;
                counter++;
            }

            return $"{size.ToString("0.##", CultureInfo.InvariantCulture)} {suffixes[counter]}";
        }
    }

    /// <summary>
    /// Gets a value indicating whether the track has been played.
    /// </summary>
    public bool HasBeenPlayed => PlayCount > 0;

    /// <summary>
    /// Gets a value indicating whether the track has complete metadata.
    /// </summary>
    public bool HasCompleteMetadata =>
        !string.IsNullOrWhiteSpace(Artist) && !string.IsNullOrWhiteSpace(Album) && !string.IsNullOrWhiteSpace(Genre);

    /// <summary>
    /// Gets a display name for the track (Artist - Title or just Title).
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Artist) ? Title : $"{Artist} - {Title}";

    /// <summary>
    /// Validates the track parameters.
    /// </summary>
    /// <param name="id">The track ID to validate.</param>
    /// <param name="title">The track title to validate.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    private static void ValidateParameters(string id, string title)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Track ID cannot be null or empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Track title cannot be null or empty.", nameof(title));
        }
    }
}
