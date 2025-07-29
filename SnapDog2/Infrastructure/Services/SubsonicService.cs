using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Infrastructure.Resilience;
using MediatR;
using SnapDog2.Core.Events;
using System.Collections.Immutable;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Implementation of Subsonic API communication for music streaming operations.
/// Provides methods for connecting to Subsonic servers, managing playlists,
/// and streaming music content with comprehensive error handling and resilience.
/// </summary>
public class SubsonicService : ISubsonicService, IDisposable
{
    private readonly SubsonicConfiguration _config;
    private readonly IAsyncPolicy _resiliencePolicy;
    private readonly ILogger<SubsonicService> _logger;
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private bool _disposed;
    private bool _authenticated;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubsonicService"/> class.
    /// </summary>
    /// <param name="config">The Subsonic configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="mediator">The mediator for publishing events.</param>
    /// <param name="httpClientFactory">HTTP client factory for creating HTTP clients.</param>
    public SubsonicService(
        IOptions<SubsonicConfiguration> config,
        ILogger<SubsonicService> logger,
        IMediator mediator,
        IHttpClientFactory httpClientFactory)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        
        _httpClient = httpClientFactory.CreateClient(nameof(SubsonicService));
        _httpClient.BaseAddress = new Uri(_config.ServerUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        _resiliencePolicy = PolicyFactory.CreateFromConfiguration(
            retryAttempts: 3,
            circuitBreakerThreshold: 3,
            circuitBreakerDuration: TimeSpan.FromSeconds(30),
            defaultTimeout: TimeSpan.FromSeconds(_config.TimeoutSeconds),
            logger: _logger
        );

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _logger.LogDebug("Subsonic service initialized for server {ServerUrl}", _config.ServerUrl);
    }

    /// <summary>
    /// Checks if the Subsonic server is available and responding.
    /// </summary>
    public async Task<bool> IsServerAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SubsonicService));

        try
        {
            _logger.LogDebug("Checking Subsonic server availability at {ServerUrl}", _config.ServerUrl);

            var response = await _resiliencePolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.GetAsync("/rest/ping.view" + GetAuthParameters(), cancellationToken);
            });

            var isAvailable = response.IsSuccessStatusCode;
            _logger.LogDebug("Subsonic server availability check result: {IsAvailable}", isAvailable);
            
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Subsonic server availability check failed");
            return false;
        }
    }

    /// <summary>
    /// Authenticates with the Subsonic server and validates credentials.
    /// </summary>
    public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SubsonicService));

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_authenticated)
            {
                _logger.LogDebug("Already authenticated with Subsonic server");
                return true;
            }

            return await _resiliencePolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("Authenticating with Subsonic server {ServerUrl}", _config.ServerUrl);

                var response = await _httpClient.GetAsync("/rest/ping.view" + GetAuthParameters(), cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var subsonicResponse = JsonSerializer.Deserialize<SubsonicResponse>(content, _jsonOptions);
                    
                    if (subsonicResponse?.Response?.Status == "ok")
                    {
                        _authenticated = true;
                        _logger.LogInformation("Successfully authenticated with Subsonic server");
                        await _mediator.Publish(new SubsonicAuthenticatedEvent(), cancellationToken);
                        return true;
                    }
                }

                _logger.LogWarning("Failed to authenticate with Subsonic server");
                return false;
            });
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves all playlists from the Subsonic server.
    /// </summary>
    public async Task<IEnumerable<Playlist>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SubsonicService));

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return Enumerable.Empty<Playlist>();
        }

        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            _logger.LogDebug("Retrieving playlists from Subsonic server");

            var response = await _httpClient.GetAsync("/rest/getPlaylists.view" + GetAuthParameters(), cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var subsonicResponse = JsonSerializer.Deserialize<SubsonicPlaylistsResponse>(content, _jsonOptions);
                
                if (subsonicResponse?.Response?.Playlists?.Playlist != null)
                {
                    var playlists = subsonicResponse.Response.Playlists.Playlist
                        .Select(ConvertToPlaylist)
                        .ToList();

                    _logger.LogDebug("Retrieved {PlaylistCount} playlists from Subsonic server", playlists.Count);
                    return playlists;
                }
            }

            _logger.LogWarning("Failed to retrieve playlists from Subsonic server");
            return Enumerable.Empty<Playlist>();
        });
    }

    /// <summary>
    /// Retrieves a specific playlist by identifier.
    /// </summary>
    public async Task<Playlist?> GetPlaylistAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SubsonicService));

        if (string.IsNullOrWhiteSpace(playlistId))
            throw new ArgumentException("Playlist ID cannot be null or whitespace", nameof(playlistId));

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return null;
        }

        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            _logger.LogDebug("Retrieving playlist {PlaylistId} from Subsonic server", playlistId);

            var response = await _httpClient.GetAsync($"/rest/getPlaylist.view?id={playlistId}" + GetAuthParameters(), cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var subsonicResponse = JsonSerializer.Deserialize<SubsonicPlaylistResponse>(content, _jsonOptions);
                
                if (subsonicResponse?.Response?.Playlist != null)
                {
                    var playlist = ConvertToPlaylist(subsonicResponse.Response.Playlist);
                    _logger.LogDebug("Retrieved playlist {PlaylistId} from Subsonic server", playlistId);
                    return playlist;
                }
            }

            _logger.LogWarning("Failed to retrieve playlist {PlaylistId} from Subsonic server", playlistId);
            return null;
        });
    }

    /// <summary>
    /// Retrieves tracks for a specific playlist.
    /// </summary>
    public async Task<IEnumerable<Track>> GetPlaylistTracksAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        var playlist = await GetPlaylistAsync(playlistId, cancellationToken);
        if (playlist == null) return Enumerable.Empty<Track>();
        
        // Note: This implementation returns track IDs only. Full track objects would need to be fetched separately.
        return Enumerable.Empty<Track>();
    }

    /// <summary>
    /// Searches for tracks, albums, or artists on the Subsonic server.
    /// </summary>
    public async Task<IEnumerable<Track>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SubsonicService));

        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query cannot be null or whitespace", nameof(query));

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return Enumerable.Empty<Track>();
        }

        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            _logger.LogDebug("Searching Subsonic server for: {Query}", query);

            var encodedQuery = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"/rest/search3.view?query={encodedQuery}" + GetAuthParameters(), cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var subsonicResponse = JsonSerializer.Deserialize<SubsonicSearchResponse>(content, _jsonOptions);
                
                if (subsonicResponse?.Response?.SearchResult3?.Song != null)
                {
                    var tracks = subsonicResponse.Response.SearchResult3.Song
                        .Select(ConvertToTrack)
                        .ToList();

                    _logger.LogDebug("Found {TrackCount} tracks for query: {Query}", tracks.Count, query);
                    return tracks;
                }
            }

            _logger.LogWarning("Search failed for query: {Query}", query);
            return Enumerable.Empty<Track>();
        });
    }

    /// <summary>
    /// Gets the streaming URL for a specific track.
    /// </summary>
    public async Task<string?> GetStreamUrlAsync(string trackId, int? maxBitRate = null, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SubsonicService));

        if (string.IsNullOrWhiteSpace(trackId))
            throw new ArgumentException("Track ID cannot be null or whitespace", nameof(trackId));

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return null;
        }

        try
        {
            var baseUrl = _config.ServerUrl.TrimEnd('/');
            var authParams = GetAuthParameters();
            var bitrateParam = maxBitRate.HasValue ? $"&maxBitRate={maxBitRate}" : "";
            
            var streamUrl = $"{baseUrl}/rest/stream.view?id={trackId}{authParams}{bitrateParam}";
            
            _logger.LogDebug("Generated stream URL for track {TrackId}", trackId);
            return streamUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate stream URL for track {TrackId}", trackId);
            return null;
        }
    }

    /// <summary>
    /// Gets a stream for a specific track.
    /// </summary>
    public async Task<Stream?> GetTrackStreamAsync(string trackId, int? maxBitRate = null, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SubsonicService));

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return null;
        }

        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            _logger.LogDebug("Getting stream for track {TrackId}", trackId);

            var bitrateParam = maxBitRate.HasValue ? $"&maxBitRate={maxBitRate}" : "";
            var response = await _httpClient.GetAsync($"/rest/stream.view?id={trackId}{GetAuthParameters()}{bitrateParam}", 
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                _logger.LogDebug("Successfully retrieved stream for track {TrackId}", trackId);
                
                // Publish streaming event
                await _mediator.Publish(new SubsonicTrackStreamedEvent(trackId), cancellationToken);
                
                return stream;
            }

            _logger.LogWarning("Failed to get stream for track {TrackId}", trackId);
            return null;
        });
    }

    /// <summary>
    /// Creates a new playlist on the Subsonic server.
    /// </summary>
    public async Task<Playlist?> CreatePlaylistAsync(string name, IEnumerable<string>? trackIds = null, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SubsonicService));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Playlist name cannot be null or whitespace", nameof(name));

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return null;
        }

        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            _logger.LogDebug("Creating playlist {PlaylistName} on Subsonic server", name);

            var encodedName = Uri.EscapeDataString(name);
            var trackIdsParam = trackIds != null ? $"&songId={string.Join("&songId=", trackIds)}" : "";
            
            var response = await _httpClient.GetAsync($"/rest/createPlaylist.view?name={encodedName}{trackIdsParam}" + GetAuthParameters(), cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var subsonicResponse = JsonSerializer.Deserialize<SubsonicPlaylistResponse>(content, _jsonOptions);
                
                if (subsonicResponse?.Response?.Playlist != null)
                {
                    var playlist = ConvertToPlaylist(subsonicResponse.Response.Playlist);
                    _logger.LogDebug("Successfully created playlist {PlaylistName}", name);
                    
                    await _mediator.Publish(new SubsonicPlaylistCreatedEvent(int.Parse(playlist.Id), name), cancellationToken);
                    return playlist;
                }
            }

            _logger.LogWarning("Failed to create playlist {PlaylistName}", name);
            return null;
        });
    }

    /// <summary>
    /// Updates an existing playlist on the Subsonic server.
    /// </summary>
    public async Task<bool> UpdatePlaylistAsync(string playlistId, string? name = null, IEnumerable<string>? trackIds = null, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SubsonicService));

        if (string.IsNullOrWhiteSpace(playlistId))
            throw new ArgumentException("Playlist ID cannot be null or whitespace", nameof(playlistId));

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return false;
        }

        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            _logger.LogDebug("Updating playlist {PlaylistId} on Subsonic server", playlistId);

            var nameParam = !string.IsNullOrWhiteSpace(name) ? $"&name={Uri.EscapeDataString(name)}" : "";
            var trackIdsParam = trackIds != null ? $"&songIdToAdd={string.Join("&songIdToAdd=", trackIds)}" : "";
            
            var response = await _httpClient.GetAsync($"/rest/updatePlaylist.view?playlistId={playlistId}{nameParam}{trackIdsParam}" + GetAuthParameters(), cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully updated playlist {PlaylistId}", playlistId);
                await _mediator.Publish(new SubsonicPlaylistUpdatedEvent(playlistId), cancellationToken);
                return true;
            }

            _logger.LogWarning("Failed to update playlist {PlaylistId}", playlistId);
            return false;
        });
    }

    /// <summary>
    /// Deletes a playlist from the Subsonic server.
    /// </summary>
    public async Task<bool> DeletePlaylistAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SubsonicService));

        if (string.IsNullOrWhiteSpace(playlistId))
            throw new ArgumentException("Playlist ID cannot be null or whitespace", nameof(playlistId));

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return false;
        }

        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            _logger.LogDebug("Deleting playlist {PlaylistId} from Subsonic server", playlistId);

            var response = await _httpClient.GetAsync($"/rest/deletePlaylist.view?id={playlistId}" + GetAuthParameters(), cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully deleted playlist {PlaylistId}", playlistId);
                await _mediator.Publish(new SubsonicPlaylistDeletedEvent(playlistId), cancellationToken);
                return true;
            }

            _logger.LogWarning("Failed to delete playlist {PlaylistId}", playlistId);
            return false;
        });
    }

    /// <summary>
    /// Ensures the service is authenticated with the Subsonic server.
    /// </summary>
    private async Task<bool> EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_authenticated)
            return true;

        return await AuthenticateAsync(cancellationToken);
    }

    /// <summary>
    /// Gets authentication parameters for Subsonic API calls.
    /// </summary>
    private string GetAuthParameters()
    {
        var salt = GenerateRandomSalt();
        var token = ComputeMD5Hash(_config.Password + salt);
        
        return $"?u={Uri.EscapeDataString(_config.Username)}&t={token}&s={salt}&v=1.16.1&c=SnapDog&f=json";
    }

    /// <summary>
    /// Generates a random salt for authentication.
    /// </summary>
    private static string GenerateRandomSalt()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Computes MD5 hash of the input string.
    /// </summary>
    private static string ComputeMD5Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Converts Subsonic playlist response to domain playlist entity.
    /// </summary>
    private static Playlist ConvertToPlaylist(SubsonicPlaylist subsonicPlaylist)
    {
        var trackIds = subsonicPlaylist.Entry?.Select(e => e.Id ?? string.Empty).Where(id => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
        
        return new Playlist
        {
            Id = subsonicPlaylist.Id ?? "0",
            Name = subsonicPlaylist.Name ?? string.Empty,
            Description = subsonicPlaylist.Comment,
            TrackIds = trackIds.ToImmutableList(),
            TotalDurationSeconds = subsonicPlaylist.Duration,
            IsPublic = subsonicPlaylist.Public ?? false,
            Owner = subsonicPlaylist.Owner ?? string.Empty,
            CreatedAt = subsonicPlaylist.Created ?? DateTime.UtcNow,
            UpdatedAt = subsonicPlaylist.Changed ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts Subsonic song/entry to domain track entity.
    /// </summary>
    private static Track ConvertToTrack(SubsonicSong subsonicSong)
    {
        return new Track
        {
            Id = subsonicSong.Id ?? "0",
            Title = subsonicSong.Title ?? string.Empty,
            Artist = subsonicSong.Artist ?? string.Empty,
            Album = subsonicSong.Album ?? string.Empty,
            DurationSeconds = subsonicSong.Duration,
            TrackNumber = subsonicSong.Track,
            Year = subsonicSong.Year,
            Genre = subsonicSong.Genre,
            BitrateKbps = subsonicSong.BitRate,
            CreatedAt = subsonicSong.Created ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Disposes the Subsonic service and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _httpClient?.Dispose();
        _connectionSemaphore?.Dispose();
        _disposed = true;
        
        _logger.LogDebug("Subsonic service disposed");
        GC.SuppressFinalize(this);
    }
}

#region Subsonic API Response Models

public class SubsonicResponse
{
    public SubsonicResponseData? Response { get; set; }
}

public class SubsonicResponseData
{
    public string? Status { get; set; }
    public string? Version { get; set; }
}

public class SubsonicPlaylistsResponse : SubsonicResponse
{
    public new SubsonicPlaylistsResponseData? Response { get; set; }
}

public class SubsonicPlaylistsResponseData : SubsonicResponseData
{
    public SubsonicPlaylistContainer? Playlists { get; set; }
}

public class SubsonicPlaylistContainer
{
    public List<SubsonicPlaylist>? Playlist { get; set; }
}

public class SubsonicPlaylistResponse : SubsonicResponse
{
    public new SubsonicPlaylistResponseData? Response { get; set; }
}

public class SubsonicPlaylistResponseData : SubsonicResponseData
{
    public SubsonicPlaylist? Playlist { get; set; }
}

public class SubsonicSearchResponse : SubsonicResponse
{
    public new SubsonicSearchResponseData? Response { get; set; }
}

public class SubsonicSearchResponseData : SubsonicResponseData
{
    public SubsonicSearchResult? SearchResult3 { get; set; }
}

public class SubsonicSearchResult
{
    public List<SubsonicSong>? Song { get; set; }
}

public class SubsonicPlaylist
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Comment { get; set; }
    public string? Owner { get; set; }
    public bool? Public { get; set; }
    public int? SongCount { get; set; }
    public int? Duration { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Changed { get; set; }
    public List<SubsonicSong>? Entry { get; set; }
}

public class SubsonicSong
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public int? Duration { get; set; }
    public int? Track { get; set; }
    public int? Year { get; set; }
    public string? Genre { get; set; }
    public int? BitRate { get; set; }
    public DateTime? Created { get; set; }
}

#endregion