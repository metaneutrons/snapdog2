using System.Collections.Immutable;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Core.State;

/// <summary>
/// Represents the complete immutable state of the SnapDog2 application.
/// Contains all domain entities and system status information.
/// </summary>
public sealed record SnapDogState
{
    /// <summary>
    /// Gets the collection of audio streams indexed by their ID.
    /// </summary>
    public ImmutableDictionary<string, AudioStream> AudioStreams { get; init; } =
        ImmutableDictionary<string, AudioStream>.Empty;

    /// <summary>
    /// Gets the collection of clients indexed by their ID.
    /// </summary>
    public ImmutableDictionary<string, Client> Clients { get; init; } = ImmutableDictionary<string, Client>.Empty;

    /// <summary>
    /// Gets the collection of zones indexed by their ID.
    /// </summary>
    public ImmutableDictionary<string, Zone> Zones { get; init; } = ImmutableDictionary<string, Zone>.Empty;

    /// <summary>
    /// Gets the collection of playlists indexed by their ID.
    /// </summary>
    public ImmutableDictionary<string, Playlist> Playlists { get; init; } = ImmutableDictionary<string, Playlist>.Empty;

    /// <summary>
    /// Gets the collection of radio stations indexed by their ID.
    /// </summary>
    public ImmutableDictionary<string, RadioStation> RadioStations { get; init; } =
        ImmutableDictionary<string, RadioStation>.Empty;

    /// <summary>
    /// Gets the collection of tracks indexed by their ID.
    /// </summary>
    public ImmutableDictionary<string, Track> Tracks { get; init; } = ImmutableDictionary<string, Track>.Empty;

    /// <summary>
    /// Gets the current system status.
    /// </summary>
    public SystemStatus SystemStatus { get; init; } = SystemStatus.Stopped;

    /// <summary>
    /// Gets the timestamp when the state was last updated.
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the current state version for optimistic concurrency control.
    /// </summary>
    public long Version { get; init; } = 1;

    /// <summary>
    /// Gets additional metadata about the current state.
    /// </summary>
    public ImmutableDictionary<string, object> Metadata { get; init; } = ImmutableDictionary<string, object>.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapDogState"/> record.
    /// </summary>
    public SnapDogState()
    {
        // All properties have default values via init accessors
    }

    /// <summary>
    /// Creates a new empty state instance.
    /// </summary>
    /// <returns>A new <see cref="SnapDogState"/> instance with default values.</returns>
    public static SnapDogState CreateEmpty()
    {
        return new SnapDogState { LastUpdated = DateTime.UtcNow, Version = 1 };
    }

    /// <summary>
    /// Creates a new state instance with the specified system status.
    /// </summary>
    /// <param name="systemStatus">The initial system status.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance.</returns>
    public static SnapDogState CreateWithStatus(SystemStatus systemStatus)
    {
        return new SnapDogState
        {
            SystemStatus = systemStatus,
            LastUpdated = DateTime.UtcNow,
            Version = 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with an updated audio stream.
    /// </summary>
    /// <param name="audioStream">The audio stream to add or update.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the updated stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when audioStream is null.</exception>
    public SnapDogState WithAudioStream(AudioStream audioStream)
    {
        if (audioStream == null)
        {
            throw new ArgumentNullException(nameof(audioStream));
        }

        return this with
        {
            AudioStreams = AudioStreams.SetItem(audioStream.Id, audioStream),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with a removed audio stream.
    /// </summary>
    /// <param name="streamId">The ID of the stream to remove.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the stream removed.</returns>
    /// <exception cref="ArgumentException">Thrown when streamId is invalid.</exception>
    public SnapDogState WithoutAudioStream(string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or empty.", nameof(streamId));
        }

        if (!AudioStreams.ContainsKey(streamId))
        {
            return this;
        }

        return this with
        {
            AudioStreams = AudioStreams.Remove(streamId),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with an updated client.
    /// </summary>
    /// <param name="client">The client to add or update.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the updated client.</returns>
    /// <exception cref="ArgumentNullException">Thrown when client is null.</exception>
    public SnapDogState WithClient(Client client)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        return this with
        {
            Clients = Clients.SetItem(client.Id, client),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with a removed client.
    /// </summary>
    /// <param name="clientId">The ID of the client to remove.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the client removed.</returns>
    /// <exception cref="ArgumentException">Thrown when clientId is invalid.</exception>
    public SnapDogState WithoutClient(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));
        }

        if (!Clients.ContainsKey(clientId))
        {
            return this;
        }

        return this with
        {
            Clients = Clients.Remove(clientId),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with an updated zone.
    /// </summary>
    /// <param name="zone">The zone to add or update.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the updated zone.</returns>
    /// <exception cref="ArgumentNullException">Thrown when zone is null.</exception>
    public SnapDogState WithZone(Zone zone)
    {
        if (zone == null)
        {
            throw new ArgumentNullException(nameof(zone));
        }

        return this with
        {
            Zones = Zones.SetItem(zone.Id, zone),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with a removed zone.
    /// </summary>
    /// <param name="zoneId">The ID of the zone to remove.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the zone removed.</returns>
    /// <exception cref="ArgumentException">Thrown when zoneId is invalid.</exception>
    public SnapDogState WithoutZone(string zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            throw new ArgumentException("Zone ID cannot be null or empty.", nameof(zoneId));
        }

        if (!Zones.ContainsKey(zoneId))
        {
            return this;
        }

        return this with
        {
            Zones = Zones.Remove(zoneId),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with an updated playlist.
    /// </summary>
    /// <param name="playlist">The playlist to add or update.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the updated playlist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when playlist is null.</exception>
    public SnapDogState WithPlaylist(Playlist playlist)
    {
        if (playlist == null)
        {
            throw new ArgumentNullException(nameof(playlist));
        }

        return this with
        {
            Playlists = Playlists.SetItem(playlist.Id, playlist),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with a removed playlist.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist to remove.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the playlist removed.</returns>
    /// <exception cref="ArgumentException">Thrown when playlistId is invalid.</exception>
    public SnapDogState WithoutPlaylist(string playlistId)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            throw new ArgumentException("Playlist ID cannot be null or empty.", nameof(playlistId));
        }

        if (!Playlists.ContainsKey(playlistId))
        {
            return this;
        }

        return this with
        {
            Playlists = Playlists.Remove(playlistId),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with an updated radio station.
    /// </summary>
    /// <param name="radioStation">The radio station to add or update.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the updated radio station.</returns>
    /// <exception cref="ArgumentNullException">Thrown when radioStation is null.</exception>
    public SnapDogState WithRadioStation(RadioStation radioStation)
    {
        if (radioStation == null)
        {
            throw new ArgumentNullException(nameof(radioStation));
        }

        return this with
        {
            RadioStations = RadioStations.SetItem(radioStation.Id, radioStation),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with a removed radio station.
    /// </summary>
    /// <param name="stationId">The ID of the radio station to remove.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the radio station removed.</returns>
    /// <exception cref="ArgumentException">Thrown when stationId is invalid.</exception>
    public SnapDogState WithoutRadioStation(string stationId)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            throw new ArgumentException("Station ID cannot be null or empty.", nameof(stationId));
        }

        if (!RadioStations.ContainsKey(stationId))
        {
            return this;
        }

        return this with
        {
            RadioStations = RadioStations.Remove(stationId),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with an updated track.
    /// </summary>
    /// <param name="track">The track to add or update.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the updated track.</returns>
    /// <exception cref="ArgumentNullException">Thrown when track is null.</exception>
    public SnapDogState WithTrack(Track track)
    {
        if (track == null)
        {
            throw new ArgumentNullException(nameof(track));
        }

        return this with
        {
            Tracks = Tracks.SetItem(track.Id, track),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with a removed track.
    /// </summary>
    /// <param name="trackId">The ID of the track to remove.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with the track removed.</returns>
    /// <exception cref="ArgumentException">Thrown when trackId is invalid.</exception>
    public SnapDogState WithoutTrack(string trackId)
    {
        if (string.IsNullOrWhiteSpace(trackId))
        {
            throw new ArgumentException("Track ID cannot be null or empty.", nameof(trackId));
        }

        if (!Tracks.ContainsKey(trackId))
        {
            return this;
        }

        return this with
        {
            Tracks = Tracks.Remove(trackId),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Creates a copy of the current state with updated system status.
    /// </summary>
    /// <param name="status">The new system status.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with updated status.</returns>
    public SnapDogState WithSystemStatus(SystemStatus status)
    {
        return this with { SystemStatus = status, LastUpdated = DateTime.UtcNow, Version = Version + 1 };
    }

    /// <summary>
    /// Creates a copy of the current state with additional metadata.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>A new <see cref="SnapDogState"/> instance with added metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when key is invalid.</exception>
    public SnapDogState WithMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Metadata key cannot be null or empty.", nameof(key));
        }

        return this with
        {
            Metadata = Metadata.SetItem(key, value),
            LastUpdated = DateTime.UtcNow,
            Version = Version + 1,
        };
    }

    /// <summary>
    /// Gets a value indicating whether the system is currently running.
    /// </summary>
    public bool IsRunning => SystemStatus == SystemStatus.Running;

    /// <summary>
    /// Gets a value indicating whether the system is stopped.
    /// </summary>
    public bool IsStopped => SystemStatus == SystemStatus.Stopped;

    /// <summary>
    /// Gets a value indicating whether the system has any error.
    /// </summary>
    public bool HasError => SystemStatus == SystemStatus.Error;

    /// <summary>
    /// Gets the total number of entities across all collections.
    /// </summary>
    public int TotalEntityCount =>
        AudioStreams.Count + Clients.Count + Zones.Count + Playlists.Count + RadioStations.Count + Tracks.Count;

    /// <summary>
    /// Validates the current state for consistency.
    /// </summary>
    /// <returns>True if the state is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        try
        {
            // Validate that all zone client references exist
            foreach (var zone in Zones.Values)
            {
                foreach (var clientId in zone.ClientIds)
                {
                    if (!Clients.ContainsKey(clientId))
                    {
                        return false;
                    }
                }

                // Validate that current stream exists if specified
                if (!string.IsNullOrWhiteSpace(zone.CurrentStreamId) && !AudioStreams.ContainsKey(zone.CurrentStreamId))
                {
                    return false;
                }
            }

            // Validate that playlist tracks exist
            foreach (var playlist in Playlists.Values)
            {
                foreach (var trackId in playlist.TrackIds)
                {
                    if (!Tracks.ContainsKey(trackId))
                    {
                        return false;
                    }
                }
            }

            // Validate that client zone assignments are consistent
            foreach (var client in Clients.Values)
            {
                if (!string.IsNullOrWhiteSpace(client.ZoneId))
                {
                    if (!Zones.ContainsKey(client.ZoneId))
                    {
                        return false;
                    }

                    var zone = Zones[client.ZoneId];
                    if (!zone.ClientIds.Contains(client.Id))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents the system status enumeration.
/// </summary>
public enum SystemStatus
{
    /// <summary>
    /// The system is stopped.
    /// </summary>
    Stopped = 0,

    /// <summary>
    /// The system is starting up.
    /// </summary>
    Starting = 1,

    /// <summary>
    /// The system is running normally.
    /// </summary>
    Running = 2,

    /// <summary>
    /// The system is stopping.
    /// </summary>
    Stopping = 3,

    /// <summary>
    /// The system has encountered an error.
    /// </summary>
    Error = 4,

    /// <summary>
    /// The system is in maintenance mode.
    /// </summary>
    Maintenance = 5,
}
