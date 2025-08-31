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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Resilience;
using SnapDog2.Server.Subsonic.Notifications;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;
using SubSonicMedia;
using SubSonicMedia.Models;
using SubSonicMedia.Responses.Search.Models;

/// <summary>
/// Enterprise-grade Subsonic service implementation using SubsonicMedia library.
/// Provides resilient operations with Polly policies for playlist and streaming functionality.
/// </summary>
public partial class SubsonicService : ISubsonicService, IAsyncDisposable
{
    private readonly SubsonicClient _subsonicClient;
    private readonly SubsonicConfig _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubsonicService> _logger;
    private readonly ResiliencePipeline _connectionPolicy;
    private readonly ResiliencePipeline _operationPolicy;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private bool _initialized = false;
    private bool _disposed = false;

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
        this._serviceProvider = serviceProvider;
        this._logger = logger;

        // Validate configuration
        if (!this._config.Enabled)
        {
            LogSubsonicDisabled(this._logger);
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

        LogSubsonicServiceInitialized(this._logger, this._config.Url, this._config.Username);
    }

    #region Helper Methods

    /// <summary>
    /// Publishes notifications using the injected mediator for better performance and reliability.
    /// </summary>
    private async Task PublishNotificationAsync<T>(T notification)
        where T : INotification
    {
        try
        {
            using var scope = this._serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.PublishAsync(notification);
        }
        catch (Exception ex)
        {
            LogNotificationPublishError(this._logger, typeof(T).Name, ex);
        }
    }

    /// <summary>
    /// Fire-and-forget notification publishing for non-critical events.
    /// </summary>
    private void PublishNotificationFireAndForget<T>(T notification)
        where T : INotification
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await this.PublishNotificationAsync(notification);
            }
            catch
            {
                // Intentionally swallow exceptions in fire-and-forget scenarios
            }
        });
    }

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
                    BackoffType = validatedConfig.BackoffType?.ToLowerInvariant() switch
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
                        LogConnectionRetryAttempt(
                            this._logger,
                            this._config.Url ?? "unknown",
                            args.AttemptNumber + 1,
                            validatedConfig.MaxRetries + 1,
                            args.Outcome.Exception?.Message ?? "Unknown error"
                        );
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
            LogGettingPlaylists(this._logger);

            var result = await this._operationPolicy.ExecuteAsync(
                async (cancellationToken) =>
                {
                    var playlistsResponse = await this._subsonicClient.Playlists.GetPlaylistsAsync(
                        cancellationToken: cancellationToken
                    );

                    if (!playlistsResponse.IsSuccess)
                    {
                        var errorMessage = playlistsResponse.Error?.Message ?? "Unknown error";
                        throw new InvalidOperationException($"Failed to get playlists: {errorMessage}");
                    }

                    return playlistsResponse
                            .Playlists.Playlist?.Select<
                                SubSonicMedia.Responses.Playlists.Models.PlaylistSummary,
                                PlaylistInfo
                            >(MapToPlaylistInfoFromSummary)
                            .ToList()
                            .AsReadOnly() ?? new List<PlaylistInfo>().AsReadOnly();
                },
                cancellationToken
            );

            stopwatch.Stop();
            LogPlaylistsRetrieved(this._logger, result.Count);

            // Publish playlists retrieved notification
            this.PublishNotificationFireAndForget(
                new SubsonicPlaylistsRetrievedNotification(
                    this._config.Url ?? "unknown",
                    result.Count,
                    stopwatch.Elapsed
                )
            );

            return Result<IReadOnlyList<PlaylistInfo>>.Success(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogGetPlaylistsError(this._logger, ex);

            // Publish playlist retrieval failed notification
            this.PublishNotificationFireAndForget(
                new SubsonicPlaylistRetrievalFailedNotification(this._config.Url ?? "unknown", ex.Message)
            );

            return Result<IReadOnlyList<PlaylistInfo>>.Failure($"Failed to get playlists: {ex.Message}");
        }
        finally
        {
            this._operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<Api.Models.PlaylistWithTracks>> GetPlaylistAsync(
        string playlistIndex,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            return Result<Api.Models.PlaylistWithTracks>.Failure("Service has been disposed");
        }

        if (string.IsNullOrEmpty(playlistIndex))
        {
            return Result<Api.Models.PlaylistWithTracks>.Failure("Playlist Index cannot be null or empty");
        }

        await this._operationLock.WaitAsync(cancellationToken);
        try
        {
            LogGettingPlaylist(this._logger, playlistIndex);

            var result = await this._operationPolicy.ExecuteAsync(
                async (ct) =>
                {
                    var playlistResponse = await this._subsonicClient.Playlists.GetPlaylistAsync(playlistIndex, ct);

                    if (!playlistResponse.IsSuccess)
                    {
                        var errorMessage = playlistResponse.Error?.Message ?? "Unknown error";
                        LogPlaylistNotFound(this._logger, playlistIndex);
                        throw new InvalidOperationException(
                            $"Playlist with ID '{playlistIndex}' not found: {errorMessage}"
                        );
                    }

                    var playlist = playlistResponse.Playlist;
                    var playlistInfo = MapToPlaylistInfo(playlist);
                    var tracks =
                        playlist
                            .Entry?.Select<Song, TrackInfo>((song, index) => MapToTrackInfo(song, index + 1))
                            .ToList() ?? new List<TrackInfo>();

                    return new Api.Models.PlaylistWithTracks(playlistInfo, tracks);
                },
                cancellationToken
            );

            LogPlaylistRetrieved(this._logger, playlistIndex, result.Tracks.Count);

            // Publish playlist accessed notification
            this.PublishNotificationFireAndForget(
                new SubsonicPlaylistAccessedNotification(
                    this._config.Url ?? "unknown",
                    playlistIndex,
                    result.Info.Name,
                    result.Tracks.Count
                )
            );

            return Result<Api.Models.PlaylistWithTracks>.Success(result);
        }
        catch (Exception ex)
        {
            LogGetPlaylistError(this._logger, playlistIndex, ex);

            // Publish playlist access failed notification
            this.PublishNotificationFireAndForget(
                new SubsonicPlaylistAccessFailedNotification(this._config.Url ?? "unknown", playlistIndex, ex.Message)
            );

            return Result<Api.Models.PlaylistWithTracks>.Failure(
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
            LogGettingStreamUrl(this._logger, trackId);

            // Note: SubsonicMedia library doesn't provide direct URL access, only streams
            // For SnapDog2's use case, we need to construct the URL manually
            // This follows the Subsonic API specification for stream URLs
            var result = await this._operationPolicy.ExecuteAsync(
                async (ct) =>
                {
                    // Test that the track exists by attempting to get a stream
                    // This validates the track ID without actually downloading content
                    using var testStream = await this._subsonicClient.Media.StreamAsync(
                        trackId,
                        maxBitRate: 64,
                        cancellationToken: ct
                    );

                    // Construct the stream URL manually following Subsonic API spec
                    // Format: {serverUrl}/rest/stream?id={trackId}&u={username}&p={password}&v={apiVersion}&c={clientName}&f=json
                    var streamUrl =
                        $"{this._config.Url?.TrimEnd('/') ?? string.Empty}/rest/stream?id={trackId}&u={this._config.Username}&p={this._config.Password}&v=1.16.1&c=SnapDog2&f=json";

                    return streamUrl;
                },
                cancellationToken
            );

            LogStreamUrlRetrieved(this._logger, trackId);

            // Publish stream requested notification
            this.PublishNotificationFireAndForget(
                new SubsonicStreamRequestedNotification(this._config.Url ?? "unknown", trackId)
            );

            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            LogGetStreamUrlError(this._logger, trackId, ex);

            // Publish stream request failed notification
            this.PublishNotificationFireAndForget(
                new SubsonicStreamRequestFailedNotification(this._config.Url ?? "unknown", trackId, ex.Message)
            );

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
            LogTestingConnection(this._logger);

            await this._connectionPolicy.ExecuteAsync(
                async (ct) =>
                {
                    var pingResult = await this._subsonicClient.System.PingAsync(ct);

                    if (!pingResult.IsSuccess)
                    {
                        var errorMessage = pingResult.Error?.Message ?? "Unknown error";
                        LogConnectionTestFailed(this._logger);
                        throw new InvalidOperationException($"Subsonic server ping failed: {errorMessage}");
                    }
                },
                cancellationToken
            );

            LogConnectionTestSuccessful(this._logger);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogConnectionTestError(this._logger, ex);
            return Result.Failure($"Connection test failed: {ex.Message}");
        }
        finally
        {
            this._operationLock.Release();
        }
    }

    /// <summary>
    /// Maps SubsonicMedia PlaylistSummary to SnapDog2 PlaylistInfo.
    /// </summary>
    private static PlaylistInfo MapToPlaylistInfoFromSummary(
        SubSonicMedia.Responses.Playlists.Models.PlaylistSummary playlistSummary
    )
    {
        return new PlaylistInfo
        {
            SubsonicPlaylistId = playlistSummary.Id ?? string.Empty,
            Name = playlistSummary.Name ?? "Unknown Playlist",
            TrackCount = playlistSummary.SongCount,
            TotalDurationSec = playlistSummary.Duration > 0 ? playlistSummary.Duration : null,
            Description = playlistSummary.Comment,
            CoverArtUrl = playlistSummary.CoverArt,
            Source = "subsonic",
        };
    }

    /// <summary>
    /// Maps SubsonicMedia Playlist to SnapDog2 PlaylistInfo.
    /// </summary>
    private static PlaylistInfo MapToPlaylistInfo(SubSonicMedia.Responses.Playlists.Models.Playlist playlist)
    {
        return new PlaylistInfo
        {
            SubsonicPlaylistId = playlist.Id ?? string.Empty,
            Name = playlist.Name ?? "Unknown Playlist",
            TrackCount = playlist.SongCount,
            TotalDurationSec = playlist.Duration > 0 ? playlist.Duration : null,
            Description = playlist.Comment,
            CoverArtUrl = playlist.CoverArt,
            Source = "subsonic",
        };
    }

    /// <summary>
    /// Maps SubsonicMedia Song to SnapDog2 TrackInfo.
    /// </summary>
    private static TrackInfo MapToTrackInfo(Song song, int index)
    {
        return new TrackInfo
        {
            Index = index,
            Title = song.Title ?? "Unknown Track",
            Artist = song.Artist ?? "Unknown Artist",
            Album = song.Album,
            DurationMs = song.Duration > 0 ? song.Duration * 1000 : null, // Convert seconds to milliseconds
            PositionMs = 0, // Always start at beginning
            CoverArtUrl = song.CoverArt,
            Source = "subsonic",
            Url = song.Id ?? string.Empty, // Use song ID as URL for Subsonic tracks
        };
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

        LogInitializing(this._logger, this._config.Url ?? "unknown");

        // Publish initialization started notification
        await this.PublishNotificationAsync(
            new SubsonicInitializationStartedNotification(this._config.Url ?? "unknown")
        );

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
                LogConnectionRetryAttempt(
                    this._logger,
                    this._config.Url ?? "unknown",
                    1,
                    config.MaxRetries + 1,
                    "Initial attempt"
                );

                // Use Polly resilience for connection establishment
                var result = await this._connectionPolicy.ExecuteAsync(
                    async (ct) =>
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
                    LogConnectionEstablished(this._logger, this._config.Url ?? "unknown");
                    LogSubsonicServiceInitialized(
                        this._logger,
                        this._config.Url ?? "unknown",
                        this._config.Username ?? "unknown"
                    );

                    // Publish connection established notification
                    await this.PublishNotificationAsync(
                        new SubsonicConnectionEstablishedNotification(
                            this._config.Url ?? "unknown",
                            this._config.Username ?? "unknown"
                        )
                    );

                    return Result.Success();
                }
                else
                {
                    LogInitializationFailed(this._logger, result.ErrorMessage ?? "Unknown error");

                    // Publish connection test failed notification
                    this.PublishNotificationFireAndForget(
                        new SubsonicConnectionTestFailedNotification(
                            this._config.Url ?? "unknown",
                            result.ErrorMessage ?? "Unknown error"
                        )
                    );

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
            LogInitializationFailed(this._logger, ex.Message);

            // Publish service error notification
            this.PublishNotificationFireAndForget(
                new SubsonicServiceErrorNotification(this._config.Url ?? "unknown", "Initialization", ex.Message)
            );

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
            LogTestingConnection(this._logger);

            // Use the SubsonicClient to ping the server
            var pingResponse = await this._subsonicClient.System.PingAsync(cancellationToken);

            if (pingResponse != null)
            {
                LogConnectionTestSuccessful(this._logger);
                return Result.Success();
            }
            else
            {
                LogConnectionTestFailed(this._logger);
                return Result.Failure("Ping response was null");
            }
        }
        catch (Exception ex)
        {
            LogConnectionTestError(this._logger, ex);
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

        // Publish service disposed notification (fire-and-forget)
        this.PublishNotificationFireAndForget(new SubsonicServiceDisposedNotification(this._config.Url ?? "unknown"));

        this._operationLock.Dispose();
        this._subsonicClient?.Dispose();

        LogSubsonicServiceDisposed(this._logger);
        return ValueTask.CompletedTask;
    }

    #region Logging

    [LoggerMessage(
        EventId = 6600,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🚀 Initializing Subsonic connection to {Url}"
    )]
    private static partial void LogInitializing(ILogger logger, string url);

    [LoggerMessage(
        EventId = 6601,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🚀 Attempting Subsonic connection to {Url} (attempt {AttemptNumber}/{MaxAttempts}: {ErrorMessage})"
    )]
    private static partial void LogConnectionRetryAttempt(
        ILogger logger,
        string url,
        int attemptNumber,
        int maxAttempts,
        string errorMessage
    );

    [LoggerMessage(
        EventId = 6602,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Subsonic connection established successfully to {Url}"
    )]
    private static partial void LogConnectionEstablished(ILogger logger, string url);

    [LoggerMessage(
        EventId = 6603,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Subsonic service initialized for server: {Url}, user: {Username}"
    )]
    private static partial void LogSubsonicServiceInitialized(ILogger logger, string url, string username);

    [LoggerMessage(
        EventId = 6604,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Subsonic service initialization failed: {ErrorMessage}"
    )]
    private static partial void LogInitializationFailed(ILogger logger, string errorMessage);

    [LoggerMessage(
        EventId = 6605,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Subsonic service is disabled in configuration"
    )]
    private static partial void LogSubsonicDisabled(ILogger logger);

    [LoggerMessage(
        EventId = 6606,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting playlists from Subsonic server"
    )]
    private static partial void LogGettingPlaylists(ILogger logger);

    [LoggerMessage(
        EventId = 6607,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Retrieved {Count} playlists from Subsonic server"
    )]
    private static partial void LogPlaylistsRetrieved(ILogger logger, int count);

    [LoggerMessage(
        EventId = 6608,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to get playlists from Subsonic server"
    )]
    private static partial void LogGetPlaylistsError(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 6609,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting playlist: {PlaylistIndex}"
    )]
    private static partial void LogGettingPlaylist(ILogger logger, string playlistIndex);

    [LoggerMessage(
        EventId = 6610,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Retrieved playlist: {PlaylistIndex} with {TrackCount} tracks"
    )]
    private static partial void LogPlaylistRetrieved(ILogger logger, string playlistIndex, int trackCount);

    [LoggerMessage(
        EventId = 6611,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Playlist not found: {PlaylistIndex}"
    )]
    private static partial void LogPlaylistNotFound(ILogger logger, string playlistIndex);

    [LoggerMessage(
        EventId = 6612,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to get playlist: {PlaylistIndex}"
    )]
    private static partial void LogGetPlaylistError(ILogger logger, string playlistIndex, Exception ex);

    [LoggerMessage(
        EventId = 6613,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting stream URL for track: {TrackId}"
    )]
    private static partial void LogGettingStreamUrl(ILogger logger, string trackId);

    [LoggerMessage(
        EventId = 6614,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Retrieved stream URL for track: {TrackId}"
    )]
    private static partial void LogStreamUrlRetrieved(ILogger logger, string trackId);

    [LoggerMessage(
        EventId = 6615,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Stream URL not found for track: {TrackId}"
    )]
    private static partial void LogStreamUrlNotFound(ILogger logger, string trackId);

    [LoggerMessage(
        EventId = 6616,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to get stream URL for track: {TrackId}"
    )]
    private static partial void LogGetStreamUrlError(ILogger logger, string trackId, Exception ex);

    [LoggerMessage(
        EventId = 6617,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Testing connection to Subsonic server"
    )]
    private static partial void LogTestingConnection(ILogger logger);

    [LoggerMessage(
        EventId = 6618,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Subsonic connection test successful"
    )]
    private static partial void LogConnectionTestSuccessful(ILogger logger);

    [LoggerMessage(
        EventId = 6619,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Subsonic connection test failed"
    )]
    private static partial void LogConnectionTestFailed(ILogger logger);

    [LoggerMessage(
        EventId = 6620,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Subsonic connection test error"
    )]
    private static partial void LogConnectionTestError(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 6621,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Subsonic service disposed"
    )]
    private static partial void LogSubsonicServiceDisposed(ILogger logger);

    [LoggerMessage(
        EventId = 6622,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to publish notification {NotificationType}"
    )]
    private static partial void LogNotificationPublishError(ILogger logger, string notificationType, Exception ex);

    #endregion
}
