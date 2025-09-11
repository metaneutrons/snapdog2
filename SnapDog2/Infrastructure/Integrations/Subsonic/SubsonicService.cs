//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Infrastructure.Integrations.Subsonic;

using System.Diagnostics;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using SnapDog2.Api.Models;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Resilience;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;
using SubSonicMedia;
using SubSonicMedia.Models;
using SubSonicMedia.Responses.Playlists.Models;
using SubSonicMedia.Responses.Search.Models;

/// <summary>
/// Enterprise-grade Subsonic service implementation using SubsonicMedia library.
/// Provides resilient operations with Polly policies for playlist and streaming functionality.
/// </summary>
public partial class SubsonicService : ISubsonicService, IAsyncDisposable
{
    private readonly SubsonicClient _subsonicClient;
    private readonly SubsonicConfig _config;
    private readonly HttpConfig _httpConfig;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubsonicService> _logger;
    private readonly ResiliencePipeline _connectionPolicy;
    private readonly ResiliencePipeline _operationPolicy;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether the Subsonic service is connected and ready.
    /// </summary>
    public bool IsConnected => this._initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubsonicService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client with resilience policies.</param>
    /// <param name="configOptions">SnapDog configuration options.</param>
    /// <param name="serviceProvider">Service provider for notification publishing.</param>
    /// <param name="logger">Logger instance.</param>
    public SubsonicService(
        HttpClient httpClient,
        IOptions<SnapDogConfiguration> configOptions,
        IServiceProvider serviceProvider,
        ILogger<SubsonicService> logger
    )
    {
        this._config = configOptions.Value.Services.Subsonic;
        this._httpConfig = configOptions.Value.Http;
        this._serviceProvider = serviceProvider;
        this._logger = logger;

        // Validate configuration
        if (!this._config.Enabled)
        {
            _logger.LogInformation("SubsonicDisabled");
            throw new InvalidOperationException("Subsonic service is disabled in configuration");
        }

        if (string.IsNullOrEmpty(this._config.Url))
        {
            throw new InvalidOperationException("Subsonic URL is required");
        }

        if (string.IsNullOrEmpty(this._config.Username))
        {
            throw new InvalidOperationException("Subsonic username is required");
        }

        if (string.IsNullOrEmpty(this._config.Password))
        {
            throw new InvalidOperationException("Subsonic password is required");
        }

        // Configure resilience policies with retry callbacks
        this._connectionPolicy = this.CreateConnectionPolicy();
        this._operationPolicy = ResiliencePolicyFactory.CreatePipeline(
            this._config.Resilience.Operation,
            "SubsonicOperation"
        );

        // Create connection info for SubsonicMedia library
        var connectionInfo = new SubsonicConnectionInfo(this._config.Url, this._config.Username, this._config.Password);

        // Initialize SubsonicClient with the resilient HttpClient
        this._subsonicClient = new SubsonicClient(connectionInfo, httpClient: httpClient, logger: null);

        _logger.LogInformation("Operation completed: {Param1} {Param2}", this._config.Url, this._config.Username);
    }

    #region Helper Methods

    // Notification publishing removed - using direct service calls now

    /// <summary>
    /// Fire-and-forget notification publishing for non-critical events.
    /// </summary>
    // Fire-and-forget notification publishing removed

    #endregion

    /// <summary>
    /// Creates resilience policy for connection establishment with retry callbacks.
    /// </summary>
    private ResiliencePipeline CreateConnectionPolicy()
    {
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Connection);

        return new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = validatedConfig.MaxRetries,
                    Delay = TimeSpan.FromMilliseconds(validatedConfig.RetryDelayMs),
                    BackoffType = validatedConfig.BackoffType.ToLowerInvariant() switch
                    {
                        "linear" => DelayBackoffType.Linear,
                        "constant" => DelayBackoffType.Constant,
                        _ => DelayBackoffType.Exponential,
                    },
                    UseJitter = validatedConfig.UseJitter,
                    // Handle all exceptions for Subsonic connection issues
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    OnRetry = args =>
                    {
                        _logger.LogWarning("ConnectionRetryAttempt: {Url} {Attempt}/{MaxRetries} {Error}",
                            this._config.Url ?? "unknown",
                            args.AttemptNumber + 1,
                            validatedConfig.MaxRetries + 1,
                            args.Outcome.Exception?.Message ?? "Unknown error");
                        return ValueTask.CompletedTask;
                    },
                }
            )
            .AddTimeout(TimeSpan.FromSeconds(validatedConfig.TimeoutSeconds))
            .Build();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PlaylistInfo>>> GetPlaylistsAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result<IReadOnlyList<PlaylistInfo>>.Failure("Service has been disposed");
        }

        await this._operationLock.WaitAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("GettingPlaylists");

            var result = await this._operationPolicy.ExecuteAsync(
                async innerCancellationToken =>
                {
                    var playlistsResponse = await this._subsonicClient.Playlists.GetPlaylistsAsync(
                        cancellationToken: innerCancellationToken
                    );

                    if (!playlistsResponse.IsSuccess)
                    {
                        var errorMessage = playlistsResponse.Error?.Message ?? "Unknown error";
                        throw new InvalidOperationException($"Failed to get playlists: {errorMessage}");
                    }

                    return playlistsResponse
                            .Playlists.Playlist.Select(MapToPlaylistInfoFromSummary)
                            .ToList()
                            .AsReadOnly();
                },
                cancellationToken
            );

            stopwatch.Stop();
            _logger.LogInformation("PlaylistsRetrieved: {Details}", result.Count);

            // Notification publishing removed - using direct SignalR calls instead

            return Result<IReadOnlyList<PlaylistInfo>>.Success(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogInformation("GetPlaylistsError: {Details}", ex);

            // Publish playlist retrieval failed notification
            // Notification publishing removed - using direct SignalR calls instead

            return Result<IReadOnlyList<PlaylistInfo>>.Failure($"Failed to get playlists: {ex.Message}");
        }
        finally
        {
            this._operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<PlaylistWithTracks>> GetPlaylistAsync(
        string playlistIndex,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result<PlaylistWithTracks>.Failure("Service has been disposed");
        }

        if (string.IsNullOrEmpty(playlistIndex))
        {
            return Result<PlaylistWithTracks>.Failure("Playlist Index cannot be null or empty");
        }

        await this._operationLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("GettingPlaylist: {Details}", playlistIndex);

            var result = await this._operationPolicy.ExecuteAsync(
                async ct =>
                {
                    var playlistResponse = await this._subsonicClient.Playlists.GetPlaylistAsync(playlistIndex, ct);

                    if (!playlistResponse.IsSuccess)
                    {
                        var errorMessage = playlistResponse.Error?.Message ?? "Unknown error";
                        _logger.LogInformation("PlaylistNotFound: {Details}", playlistIndex);
                        throw new InvalidOperationException(
                            $"Playlist with ID '{playlistIndex}' not found: {errorMessage}"
                        );
                    }

                    var playlist = playlistResponse.Playlist;
                    var playlistInfo = MapToPlaylistInfo(playlist);
                    var tracks =
                        playlist
                            .Entry.Select<Song, TrackInfo>((song, index) => MapToTrackInfo(song, index + 1))
                            .ToList();

                    return new PlaylistWithTracks(playlistInfo, tracks);
                },
                cancellationToken
            );

            _logger.LogInformation("Operation completed: {Param1} {Param2}", playlistIndex, result.Tracks.Count);

            // Notification publishing removed - using direct SignalR calls instead

            return Result<PlaylistWithTracks>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", playlistIndex, ex);

            // Notification publishing removed - using direct SignalR calls instead

            return Result<PlaylistWithTracks>.Failure(
                $"Failed to get playlist '{playlistIndex}': {ex.Message}"
            );
        }
        finally
        {
            this._operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetStreamUrlAsync(string trackId, CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Result<string>.Failure("Service has been disposed");
        }

        if (string.IsNullOrEmpty(trackId))
        {
            return Result<string>.Failure("Track ID cannot be null or empty");
        }

        await this._operationLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("GettingStreamUrl: {Details}", trackId);

            // Note: SubsonicMedia library doesn't provide direct URL access, only streams
            // For SnapDog2's use case, we need to construct the URL manually
            // This follows the Subsonic API specification for stream URLs
            var result = await this._operationPolicy.ExecuteAsync(
                async ct =>
                {
                    // Test that the track exists by attempting to get a stream
                    // This validates the track ID without actually downloading content
                    using var testStream = await this._subsonicClient.Media.StreamAsync(
                        trackId,
                        maxBitRate: 64,
                        cancellationToken: ct
                    );

                    // Construct the stream URL manually following Subsonic API spec
                    // Format: {serverUrl}/rest/stream?id={trackId}&u={username}&p={password}&v={apiVersion}&c={clientName}&f=json&format=mp3
                    var streamUrl =
                        $"{this._config.Url?.TrimEnd('/') ?? string.Empty}/rest/stream?id={trackId}&u={this._config.Username}&p={this._config.Password}&v=1.16.1&c=SnapDog2&f=json&format=mp3";

                    return streamUrl;
                },
                cancellationToken
            );

            _logger.LogInformation("StreamUrlRetrieved: {Details}", trackId);

            // Notification publishing removed - using direct SignalR calls instead

            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", trackId, ex);

            // Notification publishing removed - using direct SignalR calls instead

            return Result<string>.Failure($"Failed to get stream URL for track '{trackId}': {ex.Message}");
        }
        finally
        {
            this._operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        await this._operationLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("TestingConnection");

            await this._connectionPolicy.ExecuteAsync(
                async ct =>
                {
                    var pingResult = await this._subsonicClient.System.PingAsync(ct);

                    if (!pingResult.IsSuccess)
                    {
                        var errorMessage = pingResult.Error?.Message ?? "Unknown error";
                        _logger.LogInformation("ConnectionTestFailed");
                        throw new InvalidOperationException($"Subsonic server ping failed: {errorMessage}");
                    }
                },
                cancellationToken
            );

            _logger.LogInformation("ConnectionTestSuccessful");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogInformation("ConnectionTestError: {Details}", ex);
            return Result.Failure($"Connection test failed: {ex.Message}");
        }
        finally
        {
            this._operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<CoverArtData>> GetCoverArtAsync(string coverId, CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Result<CoverArtData>.Failure("Service has been disposed");
        }

        if (string.IsNullOrWhiteSpace(coverId))
        {
            return Result<CoverArtData>.Failure("Cover ID cannot be null or empty");
        }

        await this._operationLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("GettingCoverArt: {Details}", coverId);

            var coverResult = await this._operationPolicy.ExecuteAsync(
                async ct =>
                {
                    // Create HTTP client for direct Subsonic API call
                    using var httpClient = new HttpClient();
                    var coverUrl = $"{this._config.Url}/rest/getCoverArt?id={coverId}&u={this._config.Username}&p={this._config.Password}&v=1.16.1&c=SnapDog2&f=json";

                    var response = await httpClient.GetAsync(coverUrl, ct);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException($"Cover art not found: {response.StatusCode}");
                    }

                    return await response.Content.ReadAsByteArrayAsync(ct);
                },
                cancellationToken
            );

            // Determine content type from image data
            var contentType = GetImageContentType(coverResult);

            var coverArtData = new CoverArtData
            {
                Data = coverResult,
                ContentType = contentType,
                ETag = coverId
            };

            _logger.LogInformation("Operation completed: {Param1} {Param2}", coverId, coverResult.Length);
            return Result<CoverArtData>.Success(coverArtData);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", coverId, ex);
            return Result<CoverArtData>.Failure($"Failed to get cover art: {ex.Message}");
        }
        finally
        {
            this._operationLock.Release();
        }
    }

    /// <summary>
    /// Determines the content type of an image from its binary data.
    /// </summary>
    private static string GetImageContentType(byte[] imageData)
    {
        if (imageData.Length >= 2)
        {
            // Check for JPEG magic bytes
            if (imageData[0] == 0xFF && imageData[1] == 0xD8)
            {
                return "image/jpeg";
            }

            // Check for PNG magic bytes
            if (imageData.Length >= 8 &&
                imageData[0] == 0x89 && imageData[1] == 0x50 &&
                imageData[2] == 0x4E && imageData[3] == 0x47)
            {
                return "image/png";
            }
        }

        // Default to JPEG
        return "image/jpeg";
    }

    /// <summary>
    /// Maps SubsonicMedia PlaylistSummary to SnapDog2 PlaylistInfo.
    /// </summary>
    private PlaylistInfo MapToPlaylistInfoFromSummary(
        PlaylistSummary playlistSummary
    )
    {
        return new PlaylistInfo
        {
            SubsonicPlaylistId = playlistSummary.Id,
            Name = playlistSummary.Name,
            TrackCount = playlistSummary.SongCount,
            TotalDurationSec = playlistSummary.Duration > 0 ? playlistSummary.Duration : null,
            Description = playlistSummary.Comment,
            CoverArtUrl = GetFullCoverUrl(playlistSummary.CoverArt),
            Source = "subsonic",
        };
    }

    /// <summary>
    /// Maps SubsonicMedia Playlist to SnapDog2 PlaylistInfo.
    /// </summary>
    private PlaylistInfo MapToPlaylistInfo(Playlist playlist)
    {
        return new PlaylistInfo
        {
            SubsonicPlaylistId = playlist.Id,
            Name = playlist.Name,
            TrackCount = playlist.SongCount,
            TotalDurationSec = playlist.Duration > 0 ? playlist.Duration : null,
            Description = playlist.Comment,
            CoverArtUrl = GetFullCoverUrl(playlist.CoverArt),
            Source = "subsonic",
        };
    }

    /// <summary>
    /// Maps SubsonicMedia Song to SnapDog2 TrackInfo.
    /// </summary>
    private TrackInfo MapToTrackInfo(Song song, int index)
    {
        // Construct the full streaming URL directly with OPUS format for better quality and position tracking
        var streamUrl = $"{this._config.Url?.TrimEnd('/') ?? string.Empty}/rest/stream?id={song.Id}&u={this._config.Username}&p={this._config.Password}&v=1.16.1&c=SnapDog2&f=json&format=opus&maxBitRate=192";

        _logger.LogInformation("🔗 Generated Subsonic streaming URL: {StreamUrl}", streamUrl);

        return new TrackInfo
        {
            Index = index,
            Title = song.Title,
            Artist = song.Artist,
            Album = song.Album,
            DurationMs = song.Duration > 0 ? song.Duration * 1000 : null, // Convert seconds to milliseconds
            PositionMs = 0, // Always start at beginning
            CoverArtUrl = GetFullCoverUrl(song.CoverArt),
            Source = "subsonic",
            Url = streamUrl, // Use full streaming URL instead of just ID
        };
    }

    /// <summary>
    /// Converts internal Subsonic cover ID to full API URL using configurable base URL.
    /// Enterprise-grade solution with reverse proxy support.
    /// </summary>
    private string? GetFullCoverUrl(string? coverId)
    {
        return string.IsNullOrWhiteSpace(coverId) ? null : $"{_httpConfig.BaseUrl}/api/v1/cover/{coverId}";
    }

    /// <inheritdoc />
    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        if (this._initialized)
        {
            return Result.Success();
        }

        _logger.LogInformation("Initializing: {Details}", this._config.Url ?? "unknown");

        // Initialization started - notification removed

        try
        {
            await this._operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (this._initialized)
                {
                    return Result.Success();
                }

                // Log first attempt before Polly execution
                var config = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Connection);
                _logger.LogWarning("ConnectionRetryAttempt: {Url} {Attempt}/{MaxRetries} {Error}",
                    this._config.Url ?? "unknown",
                    1,
                    config.MaxRetries + 1,
                    "Initial attempt");

                // Use Polly resilience for connection establishment
                var result = await this._connectionPolicy.ExecuteAsync(
                    async ct =>
                    {
                        // Test the connection by making a ping call
                        var pingResult = await this.TestConnectionInternalAsync(ct);
                        if (!pingResult.IsSuccess)
                        {
                            throw new InvalidOperationException(pingResult.ErrorMessage ?? "Connection test failed");
                        }
                        return Result.Success();
                    },
                    cancellationToken
                );

                if (result.IsSuccess)
                {
                    this._initialized = true;
                    _logger.LogInformation("ConnectionEstablished: {Details}", this._config.Url ?? "unknown");
                    _logger.LogInformation("SubsonicServiceInitialized: {Url} {Username}",
                        this._config.Url ?? "unknown",
                        this._config.Username ?? "unknown");

                    // Connection established - notification removed

                    return Result.Success();
                }
                else
                {
                    _logger.LogInformation("InitializationFailed: {Details}", result.ErrorMessage ?? "Unknown error");

                    // Notification publishing removed - using direct SignalR calls instead

                    return Result.Failure(
                        $"Subsonic service initialization failed: {result.ErrorMessage ?? "Unknown error"}"
                    );
                }
            }
            finally
            {
                this._operationLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("InitializationFailed: {Details}", ex.Message);

            // Notification publishing removed - using direct SignalR calls instead

            return Result.Failure($"Subsonic service initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Internal connection test method used during initialization.
    /// </summary>
    private async Task<Result> TestConnectionInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("TestingConnection");

            // Use the SubsonicClient to ping the server
            var pingResponse = await this._subsonicClient.System.PingAsync(cancellationToken);

            _logger.LogInformation("ConnectionTestSuccessful");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogInformation("ConnectionTestError: {Details}", ex);
            return Result.Failure($"Connection test failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (this._disposed)
        {
            return ValueTask.CompletedTask;
        }

        this._disposed = true;

        // Notification publishing removed - using direct SignalR calls instead

        this._operationLock.Dispose();
        this._subsonicClient.Dispose();

        _logger.LogInformation("SubsonicServiceDisposed");
        return ValueTask.CompletedTask;
    }

    #region Logging

    #endregion
}
