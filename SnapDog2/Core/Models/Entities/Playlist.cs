using System.Collections.Immutable;

namespace SnapDog2.Core.Models.Entities;

/// <summary>
/// Represents a music playlist with ID, name, and tracks.
/// Immutable domain entity for the SnapDog2 multi-audio zone management system.
/// </summary>
public sealed record Playlist
{
    /// <summary>
    /// Gets the unique identifier for the playlist.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the name of the playlist.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the immutable collection of track IDs in this playlist.
    /// </summary>
    public ImmutableList<string> TrackIds { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Gets the description of the playlist.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the owner/creator of the playlist.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Gets a value indicating whether the playlist is public.
    /// </summary>
    public bool IsPublic { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the playlist is a system playlist.
    /// </summary>
    public bool IsSystem { get; init; } = false;

    /// <summary>
    /// Gets additional tags associated with the playlist.
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// Gets the playlist cover art path or URL.
    /// </summary>
    public string? CoverArtPath { get; init; }

    /// <summary>
    /// Gets the total estimated duration in seconds (calculated from tracks).
    /// </summary>
    public int? TotalDurationSeconds { get; init; }

    /// <summary>
    /// Gets the timestamp when the playlist was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the playlist was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the playlist was last played.
    /// </summary>
    public DateTime? LastPlayedAt { get; init; }

    /// <summary>
    /// Gets the number of times the playlist has been played.
    /// </summary>
    public int PlayCount { get; init; } = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="Playlist"/> record.
    /// </summary>
    public Playlist()
    {
        // Required properties must be set via object initializer
    }

    /// <summary>
    /// Creates a new playlist with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the playlist.</param>
    /// <param name="name">The name of the playlist.</param>
    /// <param name="description">Optional description of the playlist.</param>
    /// <param name="owner">Optional owner of the playlist.</param>
    /// <returns>A new <see cref="Playlist"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static Playlist Create(string id, string name, string? description = null, string? owner = null)
    {
        ValidateParameters(id, name);

        return new Playlist
        {
            Id = id,
            Name = name,
            Description = description,
            Owner = owner,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current playlist with an added track.
    /// </summary>
    /// <param name="trackId">The track ID to add to the playlist.</param>
    /// <returns>A new <see cref="Playlist"/> instance with the track added.</returns>
    /// <exception cref="ArgumentException">Thrown when track ID is invalid.</exception>
    public Playlist WithAddedTrack(string trackId)
    {
        if (string.IsNullOrWhiteSpace(trackId))
        {
            throw new ArgumentException("Track ID cannot be null or empty.", nameof(trackId));
        }

        return this with
        {
            TrackIds = TrackIds.Add(trackId),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current playlist with a track inserted at a specific position.
    /// </summary>
    /// <param name="trackId">The track ID to insert.</param>
    /// <param name="position">The position to insert the track at (0-based index).</param>
    /// <returns>A new <see cref="Playlist"/> instance with the track inserted.</returns>
    /// <exception cref="ArgumentException">Thrown when track ID is invalid or position is out of range.</exception>
    public Playlist WithInsertedTrack(string trackId, int position)
    {
        if (string.IsNullOrWhiteSpace(trackId))
        {
            throw new ArgumentException("Track ID cannot be null or empty.", nameof(trackId));
        }

        if (position < 0 || position > TrackIds.Count)
        {
            throw new ArgumentException($"Position must be between 0 and {TrackIds.Count}.", nameof(position));
        }

        return this with
        {
            TrackIds = TrackIds.Insert(position, trackId),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current playlist with a removed track.
    /// </summary>
    /// <param name="trackId">The track ID to remove from the playlist.</param>
    /// <returns>A new <see cref="Playlist"/> instance with the track removed.</returns>
    /// <exception cref="ArgumentException">Thrown when track ID is invalid or not found.</exception>
    public Playlist WithRemovedTrack(string trackId)
    {
        if (string.IsNullOrWhiteSpace(trackId))
        {
            throw new ArgumentException("Track ID cannot be null or empty.", nameof(trackId));
        }

        if (!TrackIds.Contains(trackId))
        {
            throw new ArgumentException($"Track '{trackId}' is not in playlist '{Id}'.", nameof(trackId));
        }

        return this with
        {
            TrackIds = TrackIds.Remove(trackId),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current playlist with a track removed at a specific position.
    /// </summary>
    /// <param name="position">The position to remove the track from (0-based index).</param>
    /// <returns>A new <see cref="Playlist"/> instance with the track removed.</returns>
    /// <exception cref="ArgumentException">Thrown when position is out of range.</exception>
    public Playlist WithRemovedTrackAt(int position)
    {
        if (position < 0 || position >= TrackIds.Count)
        {
            throw new ArgumentException($"Position must be between 0 and {TrackIds.Count - 1}.", nameof(position));
        }

        return this with
        {
            TrackIds = TrackIds.RemoveAt(position),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current playlist with updated track list.
    /// </summary>
    /// <param name="trackIds">The new list of track IDs.</param>
    /// <returns>A new <see cref="Playlist"/> instance with updated track list.</returns>
    /// <exception cref="ArgumentNullException">Thrown when track IDs list is null.</exception>
    public Playlist WithTracks(IEnumerable<string> trackIds)
    {
        if (trackIds == null)
        {
            throw new ArgumentNullException(nameof(trackIds));
        }

        var validTrackIds = trackIds.Where(static id => !string.IsNullOrWhiteSpace(id)).ToList();

        return this with
        {
            TrackIds = validTrackIds.ToImmutableList(),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current playlist with tracks moved from one position to another.
    /// </summary>
    /// <param name="fromPosition">The current position of the track (0-based index).</param>
    /// <param name="toPosition">The new position for the track (0-based index).</param>
    /// <returns>A new <see cref="Playlist"/> instance with the track moved.</returns>
    /// <exception cref="ArgumentException">Thrown when positions are out of range.</exception>
    public Playlist WithMovedTrack(int fromPosition, int toPosition)
    {
        if (fromPosition < 0 || fromPosition >= TrackIds.Count)
        {
            throw new ArgumentException(
                $"From position must be between 0 and {TrackIds.Count - 1}.",
                nameof(fromPosition)
            );
        }

        if (toPosition < 0 || toPosition >= TrackIds.Count)
        {
            throw new ArgumentException($"To position must be between 0 and {TrackIds.Count - 1}.", nameof(toPosition));
        }

        if (fromPosition == toPosition)
        {
            return this;
        }

        var trackId = TrackIds[fromPosition];
        var newTrackIds = TrackIds.RemoveAt(fromPosition).Insert(toPosition, trackId);

        return this with
        {
            TrackIds = newTrackIds,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current playlist with shuffled track order.
    /// </summary>
    /// <param name="random">Optional random number generator for shuffling.</param>
    /// <returns>A new <see cref="Playlist"/> instance with shuffled tracks.</returns>
    public Playlist WithShuffledTracks(Random? random = null)
    {
        if (TrackIds.Count <= 1)
        {
            return this;
        }

        var rng = random ?? new Random();
        var shuffledTracks = TrackIds.ToList();

        // Fisher-Yates shuffle algorithm
        for (int i = shuffledTracks.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (shuffledTracks[i], shuffledTracks[j]) = (shuffledTracks[j], shuffledTracks[i]);
        }

        return this with
        {
            TrackIds = shuffledTracks.ToImmutableList(),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a copy of the current playlist with incremented play count and updated last played time.
    /// </summary>
    /// <returns>A new <see cref="Playlist"/> instance with updated play statistics.</returns>
    public Playlist WithPlayIncrement()
    {
        return this with { PlayCount = PlayCount + 1, LastPlayedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy of the current playlist with updated total duration.
    /// </summary>
    /// <param name="totalDurationSeconds">The total duration in seconds.</param>
    /// <returns>A new <see cref="Playlist"/> instance with updated duration.</returns>
    public Playlist WithTotalDuration(int? totalDurationSeconds)
    {
        return this with { TotalDurationSeconds = totalDurationSeconds, UpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Gets a value indicating whether the playlist has any tracks.
    /// </summary>
    public bool HasTracks => TrackIds.Count > 0;

    /// <summary>
    /// Gets the number of tracks in the playlist.
    /// </summary>
    public int TrackCount => TrackIds.Count;

    /// <summary>
    /// Gets a value indicating whether the playlist is empty.
    /// </summary>
    public bool IsEmpty => TrackIds.Count == 0;

    /// <summary>
    /// Gets a value indicating whether the playlist has been played.
    /// </summary>
    public bool HasBeenPlayed => PlayCount > 0;

    /// <summary>
    /// Gets the total duration as a TimeSpan.
    /// </summary>
    public TimeSpan? TotalDuration =>
        TotalDurationSeconds.HasValue ? TimeSpan.FromSeconds(TotalDurationSeconds.Value) : null;

    /// <summary>
    /// Gets the formatted total duration string.
    /// </summary>
    public string FormattedTotalDuration
    {
        get
        {
            if (!TotalDurationSeconds.HasValue)
            {
                return "--:--:--";
            }

            var duration = TimeSpan.FromSeconds(TotalDurationSeconds.Value);
            return duration.TotalHours >= 1 ? duration.ToString(@"h\:mm\:ss") : duration.ToString(@"mm\:ss");
        }
    }

    /// <summary>
    /// Determines if the playlist contains the specified track.
    /// </summary>
    /// <param name="trackId">The track ID to check.</param>
    /// <returns>True if the playlist contains the track; otherwise, false.</returns>
    public bool ContainsTrack(string trackId)
    {
        return !string.IsNullOrWhiteSpace(trackId) && TrackIds.Contains(trackId);
    }

    /// <summary>
    /// Gets the position of a track in the playlist.
    /// </summary>
    /// <param name="trackId">The track ID to find.</param>
    /// <returns>The 0-based index of the track, or -1 if not found.</returns>
    public int GetTrackPosition(string trackId)
    {
        if (string.IsNullOrWhiteSpace(trackId))
        {
            return -1;
        }

        return TrackIds.IndexOf(trackId);
    }

    /// <summary>
    /// Validates the playlist parameters.
    /// </summary>
    /// <param name="id">The playlist ID to validate.</param>
    /// <param name="name">The playlist name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    private static void ValidateParameters(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Playlist ID cannot be null or empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Playlist name cannot be null or empty.", nameof(name));
        }
    }
}
