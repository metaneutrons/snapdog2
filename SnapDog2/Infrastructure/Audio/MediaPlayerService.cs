namespace SnapDog2.Infrastructure.Audio;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Notifications;

/// <summary>
/// LibVLC-based implementation of media player service.
/// Provides cross-platform audio streaming with LibVLC implementation.
/// </summary>
public sealed partial class MediaPlayerService : IMediaPlayerService, IAsyncDisposable, IDisposable
{
    private readonly SnapDog2.Core.Configuration.AudioConfig _config;
    private readonly ILogger<MediaPlayerService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMediator _mediator;
    private readonly IEnumerable<ZoneConfig> _zoneConfigs;

    private readonly ConcurrentDictionary<int, MediaPlayer> _players = new();
    private bool _disposed;

    public MediaPlayerService(
        IOptions<SnapDog2.Core.Configuration.AudioConfig> config,
        ILogger<MediaPlayerService> logger,
        ILoggerFactory loggerFactory,
        IMediator mediator,
        IEnumerable<ZoneConfig> zoneConfigs
    )
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _zoneConfigs = zoneConfigs ?? throw new ArgumentNullException(nameof(zoneConfigs));
    }

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
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (trackInfo == null)
                return Result.Failure(new ArgumentNullException(nameof(trackInfo)));

            var zoneConfig = _zoneConfigs.ElementAtOrDefault(zoneIndex - 1); // Zone IDs are 1-based
            if (zoneConfig == null)
            {
                return Result.Failure(new ArgumentException($"Zone {zoneIndex} not found"));
            }

            // Check if we're at the stream limit (max = number of configured zones)
            var maxStreams = _zoneConfigs.Count();
            if (_players.Count >= maxStreams)
            {
                LogMaxStreamsReached(_logger, maxStreams);
                return Result.Failure(
                    new InvalidOperationException($"Maximum number of concurrent streams ({maxStreams}) reached")
                );
            }

            // Stop existing playback for this zone
            await StopAsync(zoneIndex, cancellationToken);

            // Create LibVLC player for this zone
            var player = new MediaPlayer(
                _config,
                _loggerFactory.CreateLogger<MediaPlayer>(),
                _loggerFactory.CreateLogger<MetadataManager>(),
                zoneIndex,
                zoneConfig.Sink
            );

            _players[zoneIndex] = player;

            // Start streaming using the Id as the stream URL
            var streamUrl = trackInfo.Id; // TrackInfo.Id contains the stream URL
            var result = await player.StartStreamingAsync(streamUrl, trackInfo, cancellationToken);

            if (result.IsSuccess)
            {
                LogPlaybackStarted(_logger, zoneIndex, trackInfo.Title ?? "Unknown", streamUrl);

                // Publish playback started notification
                await _mediator.PublishAsync(
                    new TrackPlaybackStartedNotification(zoneIndex, trackInfo),
                    cancellationToken
                );
            }
            else
            {
                _players.TryRemove(zoneIndex, out _);
                await player.DisposeAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            LogPlaybackError(_logger, zoneIndex, ex);
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
            if (_players.TryRemove(zoneIndex, out var player))
            {
                await player.StopStreamingAsync();
                await player.DisposeAsync();

                LogPlaybackStopped(_logger, zoneIndex);

                // Publish playback stopped notification
                await _mediator.PublishAsync(new TrackPlaybackStoppedNotification(zoneIndex), cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogPlaybackError(_logger, zoneIndex, ex);
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
            var result = await StopAsync(zoneIndex, cancellationToken);

            if (result.IsSuccess)
            {
                LogPlaybackPaused(_logger, zoneIndex);

                // Publish playback paused notification
                await _mediator.PublishAsync(new TrackPlaybackPausedNotification(zoneIndex), cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            LogPlaybackError(_logger, zoneIndex, ex);
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
            var maxStreams = _zoneConfigs.Count();
            PlaybackStatus status;

            if (_players.TryGetValue(zoneIndex, out var player))
            {
                status = player.GetStatus();
                status.ActiveStreams = _players.Count;
                status.MaxStreams = maxStreams;
            }
            else
            {
                status = new PlaybackStatus
                {
                    ZoneIndex = zoneIndex,
                    IsPlaying = false,
                    ActiveStreams = _players.Count,
                    MaxStreams = maxStreams,
                    AudioFormat = new AudioFormat(_config.SampleRate, _config.BitDepth, _config.Channels),
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
            var maxStreams = _zoneConfigs.Count();

            // Get status for all configured zones
            for (int i = 0; i < _zoneConfigs.Count(); i++)
            {
                var zoneIndex = i + 1; // Zone IDs are 1-based
                var zoneConfig = _zoneConfigs.ElementAt(i);

                if (_players.TryGetValue(zoneIndex, out var player))
                {
                    var status = player.GetStatus();
                    status.ActiveStreams = _players.Count;
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
                            ActiveStreams = _players.Count,
                            MaxStreams = maxStreams,
                            AudioFormat = new AudioFormat(_config.SampleRate, _config.BitDepth, _config.Channels),
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
    /// Stops all active playback across all zones.
    /// </summary>
    public async Task<Result> StopAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stopTasks = _players.Keys.Select(zoneIndex => StopAsync(zoneIndex, cancellationToken));
            var results = await Task.WhenAll(stopTasks);

            var activeStreamsCount = _players.Count;

            var failures = results.Where(r => !r.IsSuccess).ToList();
            if (failures.Any())
            {
                var aggregateException = new AggregateException(failures.Select(f => f.Exception!));
                return Result.Failure(aggregateException);
            }

            LogAllPlaybackStopped(_logger, activeStreamsCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogPlaybackError(_logger, -1, ex);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Gets system-wide playback statistics.
    /// </summary>
    public Task<SnapDog2.Core.Models.Result<SnapDog2.Core.Models.PlaybackStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var maxStreams = _zoneConfigs.Count();
            var statistics = new PlaybackStatistics
            {
                ActiveStreams = _players.Count,
                MaxStreams = maxStreams,
                ConfiguredZones = _zoneConfigs.Count(),
                ActiveZones = _players.Count,
                AudioFormat = new AudioFormat(_config.SampleRate, _config.BitDepth, _config.Channels),
                UptimeSeconds = 0, // Could be tracked if needed
            };

            return Task.FromResult(
                SnapDog2.Core.Models.Result<SnapDog2.Core.Models.PlaybackStatistics>.Success(statistics)
            );
        }
        catch (Exception ex)
        {
            return Task.FromResult(SnapDog2.Core.Models.Result<SnapDog2.Core.Models.PlaybackStatistics>.Failure(ex));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true; // Set early to prevent re-entry

            try
            {
                // Stop all players with timeout to prevent hanging
                var players = _players.Values.ToList();
                _players.Clear();

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
                            _logger.LogWarning(ex, "Error disposing MediaPlayer during service cleanup");
                        }
                    });

                    await Task.WhenAll(disposeTasks);
                }

                LogServiceDisposed(_logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MediaPlayerService disposal");
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

        if (!_disposed)
        {
            _disposed = true; // Set early to prevent re-entry

            try
            {
                // Fire-and-forget disposal to prevent blocking during test cleanup
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var players = _players.Values.ToList();
                        _players.Clear();

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
                                    _logger.LogWarning(ex, "Error disposing MediaPlayer during background cleanup");
                                }
                            });

                            await Task.WhenAll(disposeTasks);
                        }

                        LogServiceDisposed(_logger);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during background MediaPlayerService disposal");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting background disposal task");
            }
        }
    }

    // Logger messages
    [LoggerMessage(
        EventId = 911,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "[LibVLCService] Started playback for zone {ZoneIndex}: {TrackTitle} from {StreamUrl}"
    )]
    private static partial void LogPlaybackStarted(ILogger logger, int zoneIndex, string trackTitle, string streamUrl);

    [LoggerMessage(
        EventId = 912,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "[LibVLCService] Stopped playback for zone {ZoneIndex}"
    )]
    private static partial void LogPlaybackStopped(ILogger logger, int zoneIndex);

    [LoggerMessage(
        EventId = 913,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "[LibVLCService] Paused playback for zone {ZoneIndex}"
    )]
    private static partial void LogPlaybackPaused(ILogger logger, int zoneIndex);

    [LoggerMessage(
        EventId = 914,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "[LibVLCService] Playback error for zone {ZoneIndex}"
    )]
    private static partial void LogPlaybackError(ILogger logger, int zoneIndex, Exception exception);

    [LoggerMessage(
        EventId = 915,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "[LibVLCService] Maximum concurrent streams reached: {MaxStreams}"
    )]
    private static partial void LogMaxStreamsReached(ILogger logger, int maxStreams);

    [LoggerMessage(
        EventId = 916,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "[LibVLCService] Stopped all playback - {ActiveStreams} streams stopped"
    )]
    private static partial void LogAllPlaybackStopped(ILogger logger, int activeStreams);

    [LoggerMessage(
        EventId = 917,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "[LibVLCService] Service disposed"
    )]
    private static partial void LogServiceDisposed(ILogger logger);
}
