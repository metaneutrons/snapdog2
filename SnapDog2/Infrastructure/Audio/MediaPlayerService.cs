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

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SnapDog2.Api.Hubs;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Integrations.Subsonic;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// LibVLC-based implementation of media player service.
/// Provides cross-platform audio streaming with LibVLC implementation.
/// </summary>
public sealed partial class MediaPlayerService(
    IOptions<AudioConfig> config,
    IOptions<ServicesConfig> servicesConfig,
    IOptions<SystemConfig> systemConfig,
    ILogger<MediaPlayerService> logger,
    ILoggerFactory loggerFactory,
    IHubContext<SnapDogHub> hubContext,
    ISubsonicService subsonicService,
    IZoneStateStore zoneStateStore,
    IEnumerable<ZoneConfig> zoneConfigs
) : IMediaPlayerService, IAsyncDisposable, IDisposable
{
    private readonly AudioConfig _config =
        config.Value ?? throw new ArgumentNullException(nameof(config));
    private readonly ServicesConfig _servicesConfig =
        servicesConfig.Value ?? throw new ArgumentNullException(nameof(servicesConfig));
    private readonly SystemConfig _systemConfig =
        systemConfig.Value ?? throw new ArgumentNullException(nameof(systemConfig));
    private readonly ILogger<MediaPlayerService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ILoggerFactory _loggerFactory =
        loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    private readonly IHubContext<SnapDogHub> _hubContext =
        hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    private readonly ISubsonicService _subsonicService =
        subsonicService ?? throw new ArgumentNullException(nameof(subsonicService));
    private readonly IZoneStateStore _zoneStateStore =
        zoneStateStore ?? throw new ArgumentNullException(nameof(zoneStateStore));
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

            // Debug logging for zone lookup
            LogLookingForZone(_logger, zoneIndex);
            LogAvailableZoneConfigs(_logger, this._zoneConfigs.Count());

            var zoneConfigsList = this._zoneConfigs.ToList();
            for (var i = 0; i < zoneConfigsList.Count; i++)
            {
                LogZoneConfig(_logger, i, zoneConfigsList[i].Name);
            }

            var zoneConfig = this._zoneConfigs.ElementAtOrDefault(zoneIndex - 1); // Zone IDs are 1-based
            if (zoneConfig == null)
            {
                LogZoneNotFound(_logger, zoneIndex, zoneIndex - 1);
                return Result.Failure(new ArgumentException($"Zone {zoneIndex} not found"));
            }

            LogFoundZoneConfig(_logger, zoneIndex, zoneConfig.Name);

            // Check if we're at the stream limit (max = number of configured zones)
            var maxStreams = this._zoneConfigs.Count();
            if (this._players.Count >= maxStreams)
            {
                LogMaxStreamsReached(_logger, maxStreams);
                return Result.Failure(
                    new InvalidOperationException($"Maximum number of concurrent streams ({maxStreams}) reached")
                );
            }

            // Stop existing playback for this zone
            await this.StopAsync(zoneIndex, cancellationToken);

            // Create LibVLC player for this zone
            var player = new MediaPlayer(
                this._config,
                this._servicesConfig,
                this._systemConfig,
                this._loggerFactory.CreateLogger<MediaPlayer>(),
                this._loggerFactory.CreateLogger<MetadataManager>(),
                zoneIndex,
                zoneConfig.Sink
            );

            this._players[zoneIndex] = player;

            // Resolve stream URL based on source type
            // All track sources now provide direct streaming URLs
            var streamUrl = trackInfo.Url;

            var result = await player.StartStreamingAsync(streamUrl, trackInfo, cancellationToken);

            if (result.IsSuccess)
            {
                // Set metadata duration for progress calculation (especially important for Subsonic streams)
                LogSettingMetadataDuration(_logger, trackInfo.DurationMs, trackInfo.Title);
                player.SetMetadataDuration(trackInfo.DurationMs);

                // Update zone state to Playing
                _zoneStateStore.UpdatePlaybackState(zoneIndex, PlaybackState.Playing);

                LogPlayingTrack(_logger, trackInfo.Title, zoneIndex, streamUrl);

                // Publish playback started notification via SignalR
                await _hubContext.Clients.All.SendAsync("TrackPlaybackStarted", zoneIndex, trackInfo, cancellationToken);
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
            LogPlayOperationFailed(_logger, zoneIndex, ex);
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

                // Update zone state to Stopped
                _zoneStateStore.UpdatePlaybackState(zoneIndex, PlaybackState.Stopped);

                LogPlaybackStoppedInfo(_logger, zoneIndex);

                // Publish playback stopped notification via SignalR
                await _hubContext.Clients.All.SendAsync("TrackPlaybackStopped", zoneIndex, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogPlayOperationFailed(_logger, zoneIndex, ex);
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
                // Update zone state to Paused
                _zoneStateStore.UpdatePlaybackState(zoneIndex, PlaybackState.Paused);

                LogPlaybackPausedInfo(_logger, zoneIndex);

                // Publish playback paused notification via SignalR
                await _hubContext.Clients.All.SendAsync("TrackPlaybackPaused", zoneIndex, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            LogPlayOperationFailed(_logger, zoneIndex, ex);
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

            LogAllPlaybackStoppedInfo(_logger, activeStreamsCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogStopAllOperationFailed(_logger, ex);
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
            LogSeekingToPositionInfo(_logger, zoneIndex, positionMs);

            if (!this._players.TryGetValue(zoneIndex, out var player))
            {
                LogPlayerNotFoundWarning(_logger, zoneIndex);
                return Task.FromResult(Result.Failure($"No active player found for zone {zoneIndex}"));
            }

            // Implement seeking via LibVLC
            var success = player.SeekToPosition(positionMs);
            if (success)
            {
                return Task.FromResult(Result.Success());
            }
            else
            {
                LogSeekNotSupported(_logger, zoneIndex);
                return Task.FromResult(Result.Failure("Seeking not supported for this media"));
            }
        }
        catch (Exception ex)
        {
            LogPlayOperationFailed(_logger, zoneIndex, ex);
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
            LogSeekingToProgressInfo(_logger, zoneIndex, progress);

            if (!this._players.TryGetValue(zoneIndex, out var player))
            {
                LogPlayerNotFoundWarning(_logger, zoneIndex);
                return Task.FromResult(Result.Failure($"No active player found for zone {zoneIndex}"));
            }

            // Implement seeking via LibVLC
            var success = player.SeekToProgress(progress);
            if (success)
            {
                return Task.FromResult(Result.Success());
            }
            else
            {
                LogSeekNotSupported(_logger, zoneIndex);
                return Task.FromResult(Result.Failure("Seeking not supported for this media"));
            }
        }
        catch (Exception ex)
        {
            LogPlayOperationFailed(_logger, zoneIndex, ex);
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
                            LogDisposalWarning(_logger, ex);
                        }
                    });

                    await Task.WhenAll(disposeTasks);
                }

                LogServiceDisposedInfo(_logger);
            }
            catch (Exception ex)
            {
                LogDisposalError(_logger, ex);
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
                                    LogBackgroundDisposalWarning(_logger, ex);
                                }
                            });

                            await Task.WhenAll(disposeTasks);
                        }

                        LogServiceDisposedInfo(_logger);
                    }
                    catch (Exception ex)
                    {
                        LogBackgroundDisposalError(_logger, ex);
                    }
                });
            }
            catch (Exception ex)
            {
                LogTaskStartError(_logger, ex);
            }
        }
    }

    // Logger messages
    [LoggerMessage(EventId = 16088, Level = LogLevel.Information, Message = "[LibVLCService] Started playback for zone {ZoneIndex}: {TrackTitle} from {StreamUrl}"
)]
    private static partial void LogPlaybackStarted(ILogger logger, int zoneIndex, string trackTitle, string streamUrl);

    [LoggerMessage(EventId = 16089, Level = LogLevel.Information, Message = "[LibVLCService] Stopped playback for zone {ZoneIndex}"
)]
    private static partial void LogPlaybackStopped(ILogger logger, int zoneIndex);

    [LoggerMessage(EventId = 16090, Level = LogLevel.Information, Message = "[LibVLCService] Paused playback for zone {ZoneIndex}"
)]
    private static partial void LogPlaybackPaused(ILogger logger, int zoneIndex);

    [LoggerMessage(EventId = 16091, Level = LogLevel.Error, Message = "[LibVLCService] Playback error for zone {ZoneIndex}"
)]
    private static partial void LogPlaybackError(ILogger logger, int zoneIndex, Exception exception);

    [LoggerMessage(EventId = 16092, Level = LogLevel.Warning, Message = "[LibVLCService] Maximum concurrent streams reached: {MaxStreams}"
)]
    private static partial void LogMaxStreamsReached(ILogger logger, int maxStreams);

    [LoggerMessage(EventId = 16093, Level = LogLevel.Information, Message = "[LibVLCService] Stopped all playback - {ActiveStreams} streams stopped"
)]
    private static partial void LogAllPlaybackStopped(ILogger logger, int activeStreams);

    [LoggerMessage(EventId = 16094, Level = LogLevel.Information, Message = "[LibVLCService] Service disposed"
)]
    private static partial void LogServiceDisposed(ILogger logger);

    [LoggerMessage(EventId = 16095, Level = LogLevel.Debug, Message = "[LibVLCService] Seeking zone {ZoneIndex} → position {PositionMs}ms"
)]
    private static partial void LogSeekingToPosition(ILogger logger, int zoneIndex, long positionMs);

    [LoggerMessage(EventId = 16096, Level = LogLevel.Debug, Message = "[LibVLCService] Seeking zone {ZoneIndex} → progress {Progress:P1}"
)]
    private static partial void LogSeekingToProgress(ILogger logger, int zoneIndex, float progress);

    [LoggerMessage(EventId = 16097, Level = LogLevel.Warning, Message = "[LibVLCService] Seek not implemented for zone {ZoneIndex} - returning success"
)]
    private static partial void LogSeekNotImplemented(ILogger logger, int zoneIndex);

    [LoggerMessage(EventId = 16098, Level = LogLevel.Error, Message = "[LibVLCService] Seek error for zone {ZoneIndex}"
)]
    private static partial void LogSeekError(ILogger logger, int zoneIndex, Exception ex);

    [LoggerMessage(EventId = 16099, Level = LogLevel.Warning, Message = "[LibVLCService] No active player found for zone {ZoneIndex}")]
    private static partial void LogPlayerNotFound(ILogger logger, int zoneIndex);

    [LoggerMessage(EventId = 16100, Level = LogLevel.Information, Message = "Looking for zone: {ZoneIndex}")]
    private static partial void LogLookingForZone(ILogger logger, int zoneIndex);

    [LoggerMessage(EventId = 16101, Level = LogLevel.Information, Message = "Available zone configs: {Count}")]
    private static partial void LogAvailableZoneConfigs(ILogger logger, int Count);

    [LoggerMessage(EventId = 16102, Level = LogLevel.Information, Message = "Zone config {ZoneIndex}: {ZoneName}")]
    private static partial void LogZoneConfig(ILogger logger, int ZoneIndex, string ZoneName);

    [LoggerMessage(EventId = 16103, Level = LogLevel.Warning, Message = "Zone {ZoneIndex} not found (index {ZeroBasedIndex})")]
    private static partial void LogZoneNotFound(ILogger logger, int ZoneIndex, int ZeroBasedIndex);

    [LoggerMessage(EventId = 16104, Level = LogLevel.Information, Message = "Found zone config {ZoneIndex}: {ZoneName}")]
    private static partial void LogFoundZoneConfig(ILogger logger, int ZoneIndex, string ZoneName);

    [LoggerMessage(EventId = 16105, Level = LogLevel.Information, Message = "Setting metadata duration: {DurationMs}ms for track: {Title}")]
    private static partial void LogSettingMetadataDuration(ILogger logger, long? DurationMs, string Title);

    [LoggerMessage(EventId = 16106, Level = LogLevel.Information, Message = "Playing track {Title} on zone {ZoneIndex} from {StreamUrl}")]
    private static partial void LogPlayingTrack(ILogger logger, string Title, int ZoneIndex, string StreamUrl);

    [LoggerMessage(EventId = 16107, Level = LogLevel.Error, Message = "Play operation failed for zone {ZoneIndex}")]
    private static partial void LogPlayOperationFailed(ILogger logger, int ZoneIndex, Exception ex);

    [LoggerMessage(EventId = 16108, Level = LogLevel.Information, Message = "Playback stopped for zone {ZoneIndex}")]
    private static partial void LogPlaybackStoppedInfo(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16109, Level = LogLevel.Error, Message = "Stop operation failed for zone {ZoneIndex}")]
    private static partial void LogStopOperationFailed(ILogger logger, int ZoneIndex, Exception ex);

    [LoggerMessage(EventId = 16110, Level = LogLevel.Information, Message = "Playback paused for zone {ZoneIndex}")]
    private static partial void LogPlaybackPausedInfo(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16111, Level = LogLevel.Error, Message = "Pause operation failed for zone {ZoneIndex}")]
    private static partial void LogPauseOperationFailed(ILogger logger, int ZoneIndex, Exception ex);

    [LoggerMessage(EventId = 16112, Level = LogLevel.Information, Message = "All playback stopped, active streams: {ActiveStreams}")]
    private static partial void LogAllPlaybackStoppedInfo(ILogger logger, int ActiveStreams);

    [LoggerMessage(EventId = 16113, Level = LogLevel.Error, Message = "Stop all operation failed")]
    private static partial void LogStopAllOperationFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 16114, Level = LogLevel.Information, Message = "Seeking to position {PositionMs}ms for zone {ZoneIndex}")]
    private static partial void LogSeekingToPositionInfo(ILogger logger, int ZoneIndex, long PositionMs);

    [LoggerMessage(EventId = 16115, Level = LogLevel.Warning, Message = "Player not found for zone {ZoneIndex}")]
    private static partial void LogPlayerNotFoundWarning(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16116, Level = LogLevel.Warning, Message = "Seek not supported for zone {ZoneIndex}")]
    private static partial void LogSeekNotSupported(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16117, Level = LogLevel.Information, Message = "Seeking to progress {Progress} for zone {ZoneIndex}")]
    private static partial void LogSeekingToProgressInfo(ILogger logger, int ZoneIndex, float Progress);

    [LoggerMessage(EventId = 16118, Level = LogLevel.Warning, Message = "Disposal warning")]
    private static partial void LogDisposalWarning(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 16119, Level = LogLevel.Information, Message = "Service disposed")]
    private static partial void LogServiceDisposedInfo(ILogger logger);

    [LoggerMessage(EventId = 16120, Level = LogLevel.Error, Message = "Disposal error")]
    private static partial void LogDisposalError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 16121, Level = LogLevel.Warning, Message = "Background disposal warning")]
    private static partial void LogBackgroundDisposalWarning(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 16122, Level = LogLevel.Error, Message = "Background disposal error")]
    private static partial void LogBackgroundDisposalError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 16123, Level = LogLevel.Error, Message = "Task start error")]
    private static partial void LogTaskStartError(ILogger logger, Exception ex);
}
