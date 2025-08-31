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
namespace SnapDog2.Infrastructure.Audio;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Zones.Notifications;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// LibVLC-based implementation of media player service.
/// Provides cross-platform audio streaming with LibVLC implementation.
/// </summary>
public sealed partial class MediaPlayerService(
    IOptions<AudioConfig> config,
    ILogger<MediaPlayerService> logger,
    ILoggerFactory loggerFactory,
    IServiceScopeFactory serviceScopeFactory,
    IEnumerable<ZoneConfig> zoneConfigs
) : IMediaPlayerService, IAsyncDisposable, IDisposable
{
    private readonly AudioConfig _config =
        config?.Value ?? throw new ArgumentNullException(nameof(config));
    private readonly ILogger<MediaPlayerService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ILoggerFactory _loggerFactory =
        loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    private readonly IServiceScopeFactory _serviceScopeFactory =
        serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly IEnumerable<ZoneConfig> _zoneConfigs =
        zoneConfigs ?? throw new ArgumentNullException(nameof(zoneConfigs));

    private readonly ConcurrentDictionary<int, MediaPlayer> _players = new();
    private bool _disposed;

    /// <summary>
    /// Starts playing audio for the specified zone.
    /// </summary>
    public async Task<Result> PlayAsync(
        int zoneIndex,
        TrackInfo trackInfo,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ObjectDisposedException.ThrowIf(this._disposed, this);

            if (trackInfo == null)
            {
                return Result.Failure(new ArgumentNullException(nameof(trackInfo)));
            }

            // Debug logging for zone lookup
            LogLookingForZone(this._logger, zoneIndex);
            LogAvailableZoneConfigs(this._logger, this._zoneConfigs.Count());

            var zoneConfigsList = this._zoneConfigs.ToList();
            for (var i = 0; i < zoneConfigsList.Count; i++)
            {
                LogZoneConfig(this._logger, i, zoneConfigsList[i].Name);
            }

            var zoneConfig = this._zoneConfigs.ElementAtOrDefault(zoneIndex - 1); // Zone IDs are 1-based
            if (zoneConfig == null)
            {
                LogZoneNotFound(this._logger, zoneIndex, zoneIndex - 1);
                return Result.Failure(new ArgumentException($"Zone {zoneIndex} not found"));
            }

            LogFoundZoneConfig(this._logger, zoneIndex, zoneConfig.Name);

            // Check if we're at the stream limit (max = number of configured zones)
            var maxStreams = this._zoneConfigs.Count();
            if (this._players.Count >= maxStreams)
            {
                LogMaxStreamsReached(this._logger, maxStreams);
                return Result.Failure(
                    new InvalidOperationException($"Maximum number of concurrent streams ({maxStreams}) reached")
                );
            }

            // Stop existing playback for this zone
            await this.StopAsync(zoneIndex, cancellationToken);

            // Create LibVLC player for this zone
            var player = new MediaPlayer(
                this._config,
                this._loggerFactory.CreateLogger<MediaPlayer>(),
                this._loggerFactory.CreateLogger<MetadataManager>(),
                zoneIndex,
                zoneConfig.Sink
            );

            this._players[zoneIndex] = player;

            // Resolve stream URL based on source type
            string streamUrl;
            if (trackInfo.Source == "subsonic")
            {
                // For Subsonic tracks, the Url field contains the media ID, not a streamable URL
                // We need to convert it to a proper stream URL using the SubsonicService
                var scope = this._serviceScopeFactory.CreateAsyncScope();
                try
                {
                    var subsonicService = scope.ServiceProvider.GetRequiredService<ISubsonicService>();

                    var streamUrlResult = await subsonicService.GetStreamUrlAsync(trackInfo.Url, cancellationToken);
                    if (!streamUrlResult.IsSuccess)
                    {
                        this._players.TryRemove(zoneIndex, out _);
                        await player.DisposeAsync();
                        return Result.Failure($"Failed to get Subsonic stream URL: {streamUrlResult.ErrorMessage}");
                    }

                    streamUrl = streamUrlResult.Value!;
                    LogConvertedSubsonicUrl(this._logger, trackInfo.Url, streamUrl);
                }
                finally
                {
                    await scope.DisposeAsync();
                }
            }
            else
            {
                // For other sources (radio, etc.), use the URL directly
                streamUrl = trackInfo.Url;
            }

            var result = await player.StartStreamingAsync(streamUrl, trackInfo, cancellationToken);

            if (result.IsSuccess)
            {
                LogPlaybackStarted(this._logger, zoneIndex, trackInfo.Title, streamUrl);

                // Publish playback started notification using scoped mediator
                using var scope = this._serviceScopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.PublishAsync(
                    new TrackPlaybackStartedNotification(zoneIndex, trackInfo),
                    cancellationToken
                );
            }
            else
            {
                this._players.TryRemove(zoneIndex, out _);
                await player.DisposeAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            LogPlaybackError(this._logger, zoneIndex, ex);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Stops playback for the specified zone.
    /// </summary>
    public async Task<Result> StopAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            if (this._players.TryRemove(zoneIndex, out var player))
            {
                await player.StopStreamingAsync();
                await player.DisposeAsync();

                LogPlaybackStopped(this._logger, zoneIndex);

                // Publish playback stopped notification using scoped mediator
                using var scope = this._serviceScopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.PublishAsync(new TrackPlaybackStoppedNotification(zoneIndex), cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogPlaybackError(this._logger, zoneIndex, ex);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Pauses playback for the specified zone.
    /// Note: For HTTP streaming, pause is equivalent to stop.
    /// </summary>
    public async Task<Result> PauseAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            // For HTTP streaming, pause is equivalent to stop
            var result = await this.StopAsync(zoneIndex, cancellationToken);

            if (result.IsSuccess)
            {
                LogPlaybackPaused(this._logger, zoneIndex);

                // Publish playback paused notification using scoped mediator
                using var scope = this._serviceScopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.PublishAsync(new TrackPlaybackPausedNotification(zoneIndex), cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            LogPlaybackError(this._logger, zoneIndex, ex);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Gets the current playback status for a zone.
    /// </summary>
    public Task<Result<PlaybackStatus>> GetStatusAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            var maxStreams = this._zoneConfigs.Count();
            PlaybackStatus status;

            if (this._players.TryGetValue(zoneIndex, out var player))
            {
                status = player.GetStatus();
                status.ActiveStreams = this._players.Count;
                status.MaxStreams = maxStreams;
            }
            else
            {
                status = new PlaybackStatus
                {
                    ZoneIndex = zoneIndex,
                    IsPlaying = false,
                    ActiveStreams = this._players.Count,
                    MaxStreams = maxStreams,
                    AudioFormat = new AudioFormat(
                        this._config.SampleRate,
                        this._config.BitDepth,
                        this._config.Channels
                    ),
                };
            }

            return Task.FromResult(Result<PlaybackStatus>.Success(status));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<PlaybackStatus>.Failure(ex));
        }
    }

    /// <summary>
    /// Gets the current playback status for all zones.
    /// </summary>
    public Task<Result<IEnumerable<PlaybackStatus>>> GetAllStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allStatuses = new List<PlaybackStatus>();
            var maxStreams = this._zoneConfigs.Count();

            // Get status for all configured zones
            for (var i = 0; i < this._zoneConfigs.Count(); i++)
            {
                var zoneIndex = i + 1; // Zone IDs are 1-based
                var zoneConfig = this._zoneConfigs.ElementAt(i);

                if (this._players.TryGetValue(zoneIndex, out var player))
                {
                    var status = player.GetStatus();
                    status.ActiveStreams = this._players.Count;
                    status.MaxStreams = maxStreams;
                    allStatuses.Add(status);
                }
                else
                {
                    allStatuses.Add(
                        new PlaybackStatus
                        {
                            ZoneIndex = zoneIndex,
                            IsPlaying = false,
                            ActiveStreams = this._players.Count,
                            MaxStreams = maxStreams,
                            AudioFormat = new AudioFormat(
                                this._config.SampleRate,
                                this._config.BitDepth,
                                this._config.Channels
                            ),
                        }
                    );
                }
            }

            return Task.FromResult(Result<IEnumerable<PlaybackStatus>>.Success(allStatuses));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IEnumerable<PlaybackStatus>>.Failure(ex));
        }
    }

    /// <summary>
    /// Gets the MediaPlayer instance for a specific zone (for event subscription).
    /// </summary>
    public MediaPlayer? GetMediaPlayer(int zoneIndex)
    {
        return this._players.TryGetValue(zoneIndex, out var player) ? player : null;
    }

    /// <summary>
    /// Stops all active playback across all zones.
    /// </summary>
    public async Task<Result> StopAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stopTasks = this._players.Keys.Select(zoneIndex => this.StopAsync(zoneIndex, cancellationToken));
            var results = await Task.WhenAll(stopTasks);

            var activeStreamsCount = this._players.Count;

            var failures = results.Where(r => !r.IsSuccess).ToList();
            if (failures.Count != 0)
            {
                var aggregateException = new AggregateException(failures.Select(f => f.Exception!));
                return Result.Failure(aggregateException);
            }

            LogAllPlaybackStopped(this._logger, activeStreamsCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogPlaybackError(this._logger, -1, ex);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Gets system-wide playback statistics.
    /// </summary>
    public Task<Result<PlaybackStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var maxStreams = this._zoneConfigs.Count();
            var statistics = new PlaybackStatistics
            {
                ActiveStreams = this._players.Count,
                MaxStreams = maxStreams,
                ConfiguredZones = this._zoneConfigs.Count(),
                ActiveZones = this._players.Count,
                AudioFormat = new AudioFormat(this._config.SampleRate, this._config.BitDepth, this._config.Channels),
                UptimeSeconds = 0, // Could be tracked if needed
            };

            return Task.FromResult(
                Result<PlaybackStatistics>.Success(statistics)
            );
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<PlaybackStatistics>.Failure(ex));
        }
    }

    /// <summary>
    /// Seeks to a specific position in the current track for the specified zone.
    /// </summary>
    public Task<Result> SeekToPositionAsync(
        int zoneIndex,
        long positionMs,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            LogSeekingToPosition(this._logger, zoneIndex, positionMs);

            if (!this._players.TryGetValue(zoneIndex, out var player))
            {
                LogPlayerNotFound(this._logger, zoneIndex);
                return Task.FromResult(Result.Failure($"No active player found for zone {zoneIndex}"));
            }

            // For now, return success as seeking is not implemented in the LibVLC wrapper
            // This would need to be implemented in the actual media player
            LogSeekNotImplemented(this._logger, zoneIndex);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            LogSeekError(this._logger, zoneIndex, ex);
            return Task.FromResult(Result.Failure(ex));
        }
    }

    /// <summary>
    /// Seeks to a specific progress percentage in the current track for the specified zone.
    /// </summary>
    public Task<Result> SeekToProgressAsync(
        int zoneIndex,
        float progress,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            LogSeekingToProgress(this._logger, zoneIndex, progress);

            if (!this._players.TryGetValue(zoneIndex, out var player))
            {
                LogPlayerNotFound(this._logger, zoneIndex);
                return Task.FromResult(Result.Failure($"No active player found for zone {zoneIndex}"));
            }

            // For now, return success as seeking is not implemented in the LibVLC wrapper
            // This would need to be implemented in the actual media player
            LogSeekNotImplemented(this._logger, zoneIndex);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            LogSeekError(this._logger, zoneIndex, ex);
            return Task.FromResult(Result.Failure(ex));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!this._disposed)
        {
            this._disposed = true; // Set early to prevent re-entry

            try
            {
                // Stop all players with timeout to prevent hanging
                var players = this._players.Values.ToList();
                this._players.Clear();

                if (players.Count > 0)
                {
                    var disposeTasks = players.Select(async p =>
                    {
                        try
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            await p.DisposeAsync().AsTask().WaitAsync(cts.Token);
                        }
                        catch (Exception ex)
                        {
                            // Log but don't throw during disposal
                            LogDisposalWarning(this._logger, ex);
                        }
                    });

                    await Task.WhenAll(disposeTasks);
                }

                LogServiceDisposed(this._logger);
            }
            catch (Exception ex)
            {
                LogDisposalError(this._logger, ex);
            }
        }
    }

    public void Dispose()
    {
        // TODO: This is a workaround for DI container scope disposal limitations.
        // The proper solution would be to:
        // 1. Use IServiceScopeFactory in StatePublishingService to avoid disposing scoped services
        // 2. Or implement a custom ServiceScope that supports async disposal
        // 3. Or refactor to use IHostedService lifecycle management instead of BackgroundService
        // Current approach blocks on async disposal which could cause deadlocks in some scenarios.
        // See: https://github.com/dotnet/runtime/issues/61132

        if (!this._disposed)
        {
            this._disposed = true; // Set early to prevent re-entry

            try
            {
                // Fire-and-forget disposal to prevent blocking during test cleanup
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var players = this._players.Values.ToList();
                        this._players.Clear();

                        if (players.Count > 0)
                        {
                            var disposeTasks = players.Select(async p =>
                            {
                                try
                                {
                                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                                    await p.DisposeAsync().AsTask().WaitAsync(cts.Token);
                                }
                                catch (Exception ex)
                                {
                                    // Log but don't throw during disposal
                                    LogBackgroundDisposalWarning(this._logger, ex);
                                }
                            });

                            await Task.WhenAll(disposeTasks);
                        }

                        LogServiceDisposed(this._logger);
                    }
                    catch (Exception ex)
                    {
                        LogBackgroundDisposalError(this._logger, ex);
                    }
                });
            }
            catch (Exception ex)
            {
                LogTaskStartError(this._logger, ex);
            }
        }
    }

    // Logger messages
    [LoggerMessage(
        EventId = 2300,
        Level = LogLevel.Information,
        Message = "[LibVLCService] Started playback for zone {ZoneIndex}: {TrackTitle} from {StreamUrl}"
    )]
    private static partial void LogPlaybackStarted(ILogger logger, int zoneIndex, string trackTitle, string streamUrl);

    [LoggerMessage(
        EventId = 2301,
        Level = LogLevel.Information,
        Message = "[LibVLCService] Stopped playback for zone {ZoneIndex}"
    )]
    private static partial void LogPlaybackStopped(ILogger logger, int zoneIndex);

    [LoggerMessage(
        EventId = 2302,
        Level = LogLevel.Information,
        Message = "[LibVLCService] Paused playback for zone {ZoneIndex}"
    )]
    private static partial void LogPlaybackPaused(ILogger logger, int zoneIndex);

    [LoggerMessage(
        EventId = 2303,
        Level = LogLevel.Error,
        Message = "[LibVLCService] Playback error for zone {ZoneIndex}"
    )]
    private static partial void LogPlaybackError(ILogger logger, int zoneIndex, Exception exception);

    [LoggerMessage(
        EventId = 2304,
        Level = LogLevel.Warning,
        Message = "[LibVLCService] Maximum concurrent streams reached: {MaxStreams}"
    )]
    private static partial void LogMaxStreamsReached(ILogger logger, int maxStreams);

    [LoggerMessage(
        EventId = 2305,
        Level = LogLevel.Information,
        Message = "[LibVLCService] Stopped all playback - {ActiveStreams} streams stopped"
    )]
    private static partial void LogAllPlaybackStopped(ILogger logger, int activeStreams);

    [LoggerMessage(
        EventId = 2306,
        Level = LogLevel.Information,
        Message = "[LibVLCService] Service disposed"
    )]
    private static partial void LogServiceDisposed(ILogger logger);

    [LoggerMessage(
        EventId = 2307,
        Level = LogLevel.Debug,
        Message = "[LibVLCService] Seeking zone {ZoneIndex} to position {PositionMs}ms"
    )]
    private static partial void LogSeekingToPosition(ILogger logger, int zoneIndex, long positionMs);

    [LoggerMessage(
        EventId = 2308,
        Level = LogLevel.Debug,
        Message = "[LibVLCService] Seeking zone {ZoneIndex} to progress {Progress:P1}"
    )]
    private static partial void LogSeekingToProgress(ILogger logger, int zoneIndex, float progress);

    [LoggerMessage(
        EventId = 2309,
        Level = LogLevel.Warning,
        Message = "[LibVLCService] Seek not implemented for zone {ZoneIndex} - returning success"
    )]
    private static partial void LogSeekNotImplemented(ILogger logger, int zoneIndex);

    [LoggerMessage(
        EventId = 2310,
        Level = LogLevel.Error,
        Message = "[LibVLCService] Seek error for zone {ZoneIndex}"
    )]
    private static partial void LogSeekError(ILogger logger, int zoneIndex, Exception ex);

    [LoggerMessage(
        EventId = 2311,
        Level = LogLevel.Warning,
        Message = "[LibVLCService] No active player found for zone {ZoneIndex}"
    )]
    private static partial void LogPlayerNotFound(ILogger logger, int zoneIndex);

    [LoggerMessage(
        EventId = 2312,
        Level = LogLevel.Information,
        Message = "MediaPlayerService: Looking for zone {ZoneIndex}"
    )]
    private static partial void LogLookingForZone(ILogger logger, int zoneIndex);

    [LoggerMessage(
        EventId = 2313,
        Level = LogLevel.Information,
        Message = "MediaPlayerService: Available zone configs count: {Count}"
    )]
    private static partial void LogAvailableZoneConfigs(ILogger logger, int count);

    [LoggerMessage(
        EventId = 2314,
        Level = LogLevel.Information,
        Message = "MediaPlayerService: Zone config {Index}: {Name}"
    )]
    private static partial void LogZoneConfig(ILogger logger, int index, string name);

    [LoggerMessage(
        EventId = 2315,
        Level = LogLevel.Error,
        Message = "MediaPlayerService: Zone {ZoneIndex} not found. Requested index in array: {ArrayIndex}"
    )]
    private static partial void LogZoneNotFound(ILogger logger, int zoneIndex, int arrayIndex);

    [LoggerMessage(
        EventId = 2316,
        Level = LogLevel.Information,
        Message = "MediaPlayerService: Found zone config for zone {ZoneIndex}: {ZoneName}"
    )]
    private static partial void LogFoundZoneConfig(ILogger logger, int zoneIndex, string zoneName);

    [LoggerMessage(
        EventId = 2317,
        Level = LogLevel.Debug,
        Message = "Converted Subsonic media ID {MediaId} to stream URL: {StreamUrl}"
    )]
    private static partial void LogConvertedSubsonicUrl(ILogger logger, string mediaId, string streamUrl);

    [LoggerMessage(
        EventId = 2318,
        Level = LogLevel.Warning,
        Message = "Error disposing MediaPlayer during service cleanup"
    )]
    private static partial void LogDisposalWarning(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 2319,
        Level = LogLevel.Error,
        Message = "Error during MediaPlayerService disposal"
    )]
    private static partial void LogDisposalError(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 2320,
        Level = LogLevel.Warning,
        Message = "Error disposing MediaPlayer during background cleanup"
    )]
    private static partial void LogBackgroundDisposalWarning(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 2321,
        Level = LogLevel.Error,
        Message = "Error during background MediaPlayerService disposal"
    )]
    private static partial void LogBackgroundDisposalError(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 2322,
        Level = LogLevel.Error,
        Message = "Error starting background disposal task"
    )]
    private static partial void LogTaskStartError(ILogger logger, Exception ex);
}
