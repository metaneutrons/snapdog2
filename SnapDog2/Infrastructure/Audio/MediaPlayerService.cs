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
using SnapDog2.Shared.Models;

/// <summary>
/// LibVLC-based implementation of media player service.
/// Provides cross-platform audio streaming with LibVLC implementation.
/// </summary>
public sealed partial class MediaPlayerService(
    IOptions<AudioConfig> config,
    ILogger<MediaPlayerService> logger,
    ILoggerFactory loggerFactory,
    IHubContext<SnapDogHub> hubContext,
    ISubsonicService subsonicService,
    IEnumerable<ZoneConfig> zoneConfigs
) : IMediaPlayerService, IAsyncDisposable, IDisposable
{
    private readonly AudioConfig _config =
        config.Value ?? throw new ArgumentNullException(nameof(config));
    private readonly ILogger<MediaPlayerService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ILoggerFactory _loggerFactory =
        loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    private readonly IHubContext<SnapDogHub> _hubContext =
        hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    private readonly ISubsonicService _subsonicService =
        subsonicService ?? throw new ArgumentNullException(nameof(subsonicService));
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
            _logger.LogInformation("LookingForZone: {Details}", zoneIndex);
            _logger.LogInformation("AvailableZoneConfigs: {Details}", this._zoneConfigs.Count());

            var zoneConfigsList = this._zoneConfigs.ToList();
            for (var i = 0; i < zoneConfigsList.Count; i++)
            {
                _logger.LogInformation("Zone config {ZoneIndex}: {ZoneName}", i, zoneConfigsList[i].Name);
            }

            var zoneConfig = this._zoneConfigs.ElementAtOrDefault(zoneIndex - 1); // Zone IDs are 1-based
            if (zoneConfig == null)
            {
                _logger.LogInformation("Zone {ZoneIndex} not found (index {ZeroBasedIndex})", zoneIndex, zoneIndex - 1);
                return Result.Failure(new ArgumentException($"Zone {zoneIndex} not found"));
            }

            _logger.LogInformation("Found zone config {ZoneIndex}: {ZoneName}", zoneIndex, zoneConfig.Name);

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
                _logger.LogInformation("🎵 Setting metadata duration: {DurationMs}ms for track: {Title}", trackInfo.DurationMs, trackInfo.Title);
                player.SetMetadataDuration(trackInfo.DurationMs);

                _logger.LogInformation("Playing track {Title} on zone {ZoneIndex} from {StreamUrl}", trackInfo.Title, zoneIndex, streamUrl);

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
            _logger.LogInformation("Operation completed: {Param1} {Param2}", zoneIndex, ex);
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

                _logger.LogInformation("PlaybackStopped: {Details}", zoneIndex);

                // Publish playback stopped notification via SignalR
                await _hubContext.Clients.All.SendAsync("TrackPlaybackStopped", zoneIndex, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", zoneIndex, ex);
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
                _logger.LogInformation("PlaybackPaused: {Details}", zoneIndex);

                // Publish playback paused notification via SignalR
                await _hubContext.Clients.All.SendAsync("TrackPlaybackPaused", zoneIndex, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", zoneIndex, ex);
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

            _logger.LogInformation("AllPlaybackStopped: {Details}", activeStreamsCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", -1, ex);
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
            _logger.LogInformation("Operation completed: {Param1} {Param2}", zoneIndex, positionMs);

            if (!this._players.TryGetValue(zoneIndex, out var player))
            {
                _logger.LogInformation("PlayerNotFound: {Details}", zoneIndex);
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
                _logger.LogInformation("SeekNotSupported: {Details}", zoneIndex);
                return Task.FromResult(Result.Failure("Seeking not supported for this media"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", zoneIndex, ex);
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
            _logger.LogInformation("Operation completed: {Param1} {Param2}", zoneIndex, progress);

            if (!this._players.TryGetValue(zoneIndex, out var player))
            {
                _logger.LogInformation("PlayerNotFound: {Details}", zoneIndex);
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
                _logger.LogInformation("SeekNotSupported: {Details}", zoneIndex);
                return Task.FromResult(Result.Failure("Seeking not supported for this media"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", zoneIndex, ex);
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
                            _logger.LogInformation("DisposalWarning: {Details}", ex);
                        }
                    });

                    await Task.WhenAll(disposeTasks);
                }

                _logger.LogInformation("ServiceDisposed");
            }
            catch (Exception ex)
            {
                _logger.LogInformation("DisposalError: {Details}", ex);
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
                                    _logger.LogInformation("BackgroundDisposalWarning: {Details}", ex);
                                }
                            });

                            await Task.WhenAll(disposeTasks);
                        }

                        _logger.LogInformation("ServiceDisposed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("BackgroundDisposalError: {Details}", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation("TaskStartError: {Details}", ex);
            }
        }
    }

    // Logger messages
    [LoggerMessage(EventId = 116200, Level = LogLevel.Information, Message = "[LibVLCService] Started playback for zone {ZoneIndex}: {TrackTitle} from {StreamUrl}"
)]
    private static partial void LogPlaybackStarted(ILogger logger, int zoneIndex, string trackTitle, string streamUrl);

    [LoggerMessage(EventId = 116201, Level = LogLevel.Information, Message = "[LibVLCService] Stopped playback for zone {ZoneIndex}"
)]
    private static partial void LogPlaybackStopped(ILogger logger, int zoneIndex);

    [LoggerMessage(EventId = 116202, Level = LogLevel.Information, Message = "[LibVLCService] Paused playback for zone {ZoneIndex}"
)]
    private static partial void LogPlaybackPaused(ILogger logger, int zoneIndex);

    [LoggerMessage(EventId = 116203, Level = LogLevel.Error, Message = "[LibVLCService] Playback error for zone {ZoneIndex}"
)]
    private static partial void LogPlaybackError(ILogger logger, int zoneIndex, Exception exception);

    [LoggerMessage(EventId = 116204, Level = LogLevel.Warning, Message = "[LibVLCService] Maximum concurrent streams reached: {MaxStreams}"
)]
    private static partial void LogMaxStreamsReached(ILogger logger, int maxStreams);

    [LoggerMessage(EventId = 116205, Level = LogLevel.Information, Message = "[LibVLCService] Stopped all playback - {ActiveStreams} streams stopped"
)]
    private static partial void LogAllPlaybackStopped(ILogger logger, int activeStreams);

    [LoggerMessage(EventId = 116206, Level = LogLevel.Information, Message = "[LibVLCService] Service disposed"
)]
    private static partial void LogServiceDisposed(ILogger logger);

    [LoggerMessage(EventId = 116207, Level = LogLevel.Debug, Message = "[LibVLCService] Seeking zone {ZoneIndex} → position {PositionMs}ms"
)]
    private static partial void LogSeekingToPosition(ILogger logger, int zoneIndex, long positionMs);

    [LoggerMessage(EventId = 116208, Level = LogLevel.Debug, Message = "[LibVLCService] Seeking zone {ZoneIndex} → progress {Progress:P1}"
)]
    private static partial void LogSeekingToProgress(ILogger logger, int zoneIndex, float progress);

    [LoggerMessage(EventId = 116209, Level = LogLevel.Warning, Message = "[LibVLCService] Seek not implemented for zone {ZoneIndex} - returning success"
)]
    private static partial void LogSeekNotImplemented(ILogger logger, int zoneIndex);

    [LoggerMessage(EventId = 116210, Level = LogLevel.Error, Message = "[LibVLCService] Seek error for zone {ZoneIndex}"
)]
    private static partial void LogSeekError(ILogger logger, int zoneIndex, Exception ex);

    [LoggerMessage(EventId = 116211, Level = LogLevel.Warning, Message = "[LibVLCService] No active player found for zone {ZoneIndex}"
)]
    private static partial void LogPlayerNotFound(ILogger logger, int zoneIndex);

}
