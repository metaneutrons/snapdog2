using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Core.State;

/// <summary>
/// Extension methods for common state operations.
/// </summary>
public static class StateExtensions
{
    /// <summary>
    /// Gets an audio stream by ID from the state.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <param name="streamId">The ID of the stream to find.</param>
    /// <returns>The audio stream if found; otherwise, null.</returns>
    public static AudioStream? GetAudioStream(this SnapDogState state, string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            return null;
        }

        return state.AudioStreams.TryGetValue(streamId, out var stream) ? stream : null;
    }

    /// <summary>
    /// Gets a client by ID from the state.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <param name="clientId">The ID of the client to find.</param>
    /// <returns>The client if found; otherwise, null.</returns>
    public static Client? GetClient(this SnapDogState state, string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        return state.Clients.TryGetValue(clientId, out var client) ? client : null;
    }

    /// <summary>
    /// Gets a zone by ID from the state.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <param name="zoneId">The ID of the zone to find.</param>
    /// <returns>The zone if found; otherwise, null.</returns>
    public static Zone? GetZone(this SnapDogState state, string zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            return null;
        }

        return state.Zones.TryGetValue(zoneId, out var zone) ? zone : null;
    }

    /// <summary>
    /// Gets a playlist by ID from the state.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <param name="playlistId">The ID of the playlist to find.</param>
    /// <returns>The playlist if found; otherwise, null.</returns>
    public static Playlist? GetPlaylist(this SnapDogState state, string playlistId)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            return null;
        }

        return state.Playlists.TryGetValue(playlistId, out var playlist) ? playlist : null;
    }

    /// <summary>
    /// Gets a radio station by ID from the state.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <param name="stationId">The ID of the radio station to find.</param>
    /// <returns>The radio station if found; otherwise, null.</returns>
    public static RadioStation? GetRadioStation(this SnapDogState state, string stationId)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            return null;
        }

        return state.RadioStations.TryGetValue(stationId, out var station) ? station : null;
    }

    /// <summary>
    /// Gets a track by ID from the state.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <param name="trackId">The ID of the track to find.</param>
    /// <returns>The track if found; otherwise, null.</returns>
    public static Track? GetTrack(this SnapDogState state, string trackId)
    {
        if (string.IsNullOrWhiteSpace(trackId))
        {
            return null;
        }

        return state.Tracks.TryGetValue(trackId, out var track) ? track : null;
    }

    /// <summary>
    /// Gets all clients assigned to a specific zone.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <param name="zoneId">The ID of the zone.</param>
    /// <returns>An enumerable of clients assigned to the zone.</returns>
    public static IEnumerable<Client> GetClientsInZone(this SnapDogState state, string zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            return Enumerable.Empty<Client>();
        }

        var zone = state.GetZone(zoneId);
        if (zone == null)
        {
            return Enumerable.Empty<Client>();
        }

        return zone
            .ClientIds.Select(clientId => state.GetClient(clientId))
            .Where(client => client != null)
            .Cast<Client>();
    }

    /// <summary>
    /// Gets all tracks in a specific playlist.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <param name="playlistId">The ID of the playlist.</param>
    /// <returns>An enumerable of tracks in the playlist, in order.</returns>
    public static IEnumerable<Track> GetTracksInPlaylist(this SnapDogState state, string playlistId)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            return Enumerable.Empty<Track>();
        }

        var playlist = state.GetPlaylist(playlistId);
        if (playlist == null)
        {
            return Enumerable.Empty<Track>();
        }

        return playlist.TrackIds.Select(trackId => state.GetTrack(trackId)).Where(track => track != null).Cast<Track>();
    }

    /// <summary>
    /// Gets all connected clients from the state.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <returns>An enumerable of connected clients.</returns>
    public static IEnumerable<Client> GetConnectedClients(this SnapDogState state)
    {
        return state.Clients.Values.Where(static client => client.IsConnected);
    }

    /// <summary>
    /// Gets all active zones from the state.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <returns>An enumerable of active zones.</returns>
    public static IEnumerable<Zone> GetActiveZones(this SnapDogState state)
    {
        return state.Zones.Values.Where(static zone => zone.IsActive);
    }

    /// <summary>
    /// Gets all enabled radio stations from the state.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <returns>An enumerable of enabled radio stations.</returns>
    public static IEnumerable<RadioStation> GetEnabledRadioStations(this SnapDogState state)
    {
        return state.RadioStations.Values.Where(static station => station.IsEnabled);
    }

    /// <summary>
    /// Gets all playing audio streams from the state.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <returns>An enumerable of playing audio streams.</returns>
    public static IEnumerable<AudioStream> GetPlayingStreams(this SnapDogState state)
    {
        return state.AudioStreams.Values.Where(static stream => stream.IsPlaying);
    }

    /// <summary>
    /// Checks if a client exists and is connected.
    /// </summary>
    /// <param name="state">The state to check.</param>
    /// <param name="clientId">The ID of the client to check.</param>
    /// <returns>True if the client exists and is connected; otherwise, false.</returns>
    public static bool IsClientConnected(this SnapDogState state, string clientId)
    {
        var client = state.GetClient(clientId);
        return client?.IsConnected == true;
    }

    /// <summary>
    /// Checks if a zone has any connected clients.
    /// </summary>
    /// <param name="state">The state to check.</param>
    /// <param name="zoneId">The ID of the zone to check.</param>
    /// <returns>True if the zone has any connected clients; otherwise, false.</returns>
    public static bool HasConnectedClients(this SnapDogState state, string zoneId)
    {
        return state.GetClientsInZone(zoneId).Any(static client => client.IsConnected);
    }

    /// <summary>
    /// Gets the current stream for a zone, if any.
    /// </summary>
    /// <param name="state">The state to search in.</param>
    /// <param name="zoneId">The ID of the zone.</param>
    /// <returns>The current audio stream for the zone if it exists; otherwise, null.</returns>
    public static AudioStream? GetCurrentStreamForZone(this SnapDogState state, string zoneId)
    {
        var zone = state.GetZone(zoneId);
        if (zone?.CurrentStreamId == null)
        {
            return null;
        }

        return state.GetAudioStream(zone.CurrentStreamId);
    }

    /// <summary>
    /// Gets summary statistics about the current state.
    /// </summary>
    /// <param name="state">The state to analyze.</param>
    /// <returns>A summary of state statistics.</returns>
    public static StateSummary GetSummary(this SnapDogState state)
    {
        return new StateSummary
        {
            TotalAudioStreams = state.AudioStreams.Count,
            PlayingAudioStreams = state.GetPlayingStreams().Count(),
            TotalClients = state.Clients.Count,
            ConnectedClients = state.GetConnectedClients().Count(),
            TotalZones = state.Zones.Count,
            ActiveZones = state.GetActiveZones().Count(),
            TotalPlaylists = state.Playlists.Count,
            TotalRadioStations = state.RadioStations.Count,
            EnabledRadioStations = state.GetEnabledRadioStations().Count(),
            TotalTracks = state.Tracks.Count,
            SystemStatus = state.SystemStatus,
            LastUpdated = state.LastUpdated,
            Version = state.Version,
            IsValid = state.IsValid(),
        };
    }
}

/// <summary>
/// Represents a summary of the current state statistics.
/// </summary>
public sealed record StateSummary
{
    /// <summary>
    /// Gets the total number of audio streams.
    /// </summary>
    public int TotalAudioStreams { get; init; }

    /// <summary>
    /// Gets the number of currently playing audio streams.
    /// </summary>
    public int PlayingAudioStreams { get; init; }

    /// <summary>
    /// Gets the total number of clients.
    /// </summary>
    public int TotalClients { get; init; }

    /// <summary>
    /// Gets the number of connected clients.
    /// </summary>
    public int ConnectedClients { get; init; }

    /// <summary>
    /// Gets the total number of zones.
    /// </summary>
    public int TotalZones { get; init; }

    /// <summary>
    /// Gets the number of active zones.
    /// </summary>
    public int ActiveZones { get; init; }

    /// <summary>
    /// Gets the total number of playlists.
    /// </summary>
    public int TotalPlaylists { get; init; }

    /// <summary>
    /// Gets the total number of radio stations.
    /// </summary>
    public int TotalRadioStations { get; init; }

    /// <summary>
    /// Gets the number of enabled radio stations.
    /// </summary>
    public int EnabledRadioStations { get; init; }

    /// <summary>
    /// Gets the total number of tracks.
    /// </summary>
    public int TotalTracks { get; init; }

    /// <summary>
    /// Gets the current system status.
    /// </summary>
    public SystemStatus SystemStatus { get; init; }

    /// <summary>
    /// Gets the timestamp when the state was last updated.
    /// </summary>
    public DateTime LastUpdated { get; init; }

    /// <summary>
    /// Gets the current state version.
    /// </summary>
    public long Version { get; init; }

    /// <summary>
    /// Gets a value indicating whether the state is valid.
    /// </summary>
    public bool IsValid { get; init; }
}
