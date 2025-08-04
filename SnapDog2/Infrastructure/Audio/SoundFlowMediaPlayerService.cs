namespace SnapDog2.Infrastructure.Audio;

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;
using SnapDog2.Server.Notifications;

/// <summary>
/// SoundFlow-based implementation of media player service.
/// Provides cross-platform audio streaming with native .NET implementation.
/// </summary>
public sealed partial class SoundFlowMediaPlayerService : IMediaPlayerService, IAsyncDisposable
{
    private readonly SoundFlowConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SoundFlowMediaPlayerService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMediator _mediator;
    private readonly IEnumerable<ZoneConfig> _zoneConfigs;

    private readonly ConcurrentDictionary<int, SoundFlowPlayer> _players = new();
    private bool _disposed;

    public SoundFlowMediaPlayerService(
        IOptions<SoundFlowConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<SoundFlowMediaPlayerService> logger,
        ILoggerFactory loggerFactory,
        IMediator mediator,
        IEnumerable<ZoneConfig> zoneConfigs
    )
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _zoneConfigs = zoneConfigs ?? throw new ArgumentNullException(nameof(zoneConfigs));
    }

    /// <summary>
    /// Starts playing audio for the specified zone.
    /// </summary>
    public async Task<Result> PlayAsync(int zoneId, TrackInfo trackInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (trackInfo == null)
                return Result.Failure(new ArgumentNullException(nameof(trackInfo)));

            var zoneConfig = _zoneConfigs.ElementAtOrDefault(zoneId - 1); // Zone IDs are 1-based
            if (zoneConfig == null)
            {
                return Result.Failure(new ArgumentException($"Zone {zoneId} not found"));
            }

            // Check if we're at the stream limit
            if (_players.Count >= _config.MaxStreams)
            {
                LogMaxStreamsReached(_logger, _config.MaxStreams);
                return Result.Failure(
                    new InvalidOperationException(
                        $"Maximum number of concurrent streams ({_config.MaxStreams}) reached"
                    )
                );
            }

            // Stop existing playback for this zone
            await StopAsync(zoneId, cancellationToken);

            // Create HTTP client with resilience policies
            var httpClient = _httpClientFactory.CreateClient("SoundFlowStreaming");
            httpClient.Timeout = TimeSpan.FromSeconds(_config.HttpTimeoutSeconds);

            // Create SoundFlow player for this zone
            var player = new SoundFlowPlayer(
                _config,
                _loggerFactory.CreateLogger<SoundFlowPlayer>(),
                zoneId,
                zoneConfig.Sink,
                httpClient
            );

            _players[zoneId] = player;

            // Start streaming
            var result = await player.StartStreamingAsync(trackInfo.Id, trackInfo, cancellationToken);

            if (result.IsSuccess)
            {
                LogPlaybackStarted(_logger, zoneId, trackInfo.Title, trackInfo.Id);

                // Publish playback started notification
                await _mediator.PublishAsync(
                    new TrackPlaybackStartedNotification(zoneId, trackInfo),
                    cancellationToken
                );
            }
            else
            {
                _players.TryRemove(zoneId, out _);
                await player.DisposeAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            LogPlaybackError(_logger, zoneId, ex);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Stops playback for the specified zone.
    /// </summary>
    public async Task<Result> StopAsync(int zoneId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_players.TryRemove(zoneId, out var player))
            {
                await player.StopStreamingAsync();
                await player.DisposeAsync();

                LogPlaybackStopped(_logger, zoneId);

                // Publish playback stopped notification
                await _mediator.PublishAsync(new TrackPlaybackStoppedNotification(zoneId), cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogPlaybackError(_logger, zoneId, ex);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Pauses playback for the specified zone.
    /// Note: For HTTP streaming, pause is equivalent to stop.
    /// </summary>
    public async Task<Result> PauseAsync(int zoneId, CancellationToken cancellationToken = default)
    {
        try
        {
            // For HTTP streaming, pause is equivalent to stop
            var result = await StopAsync(zoneId, cancellationToken);

            if (result.IsSuccess)
            {
                LogPlaybackPaused(_logger, zoneId);

                // Publish playback paused notification
                await _mediator.PublishAsync(new TrackPlaybackPausedNotification(zoneId), cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            LogPlaybackError(_logger, zoneId, ex);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Gets the current playback status for a zone.
    /// </summary>
    public Task<Result<PlaybackStatus>> GetStatusAsync(int zoneId, CancellationToken cancellationToken = default)
    {
        try
        {
            PlaybackStatus status;

            if (_players.TryGetValue(zoneId, out var player))
            {
                status = player.GetStatus();
                status.ActiveStreams = _players.Count;
                status.MaxStreams = _config.MaxStreams;
            }
            else
            {
                status = new PlaybackStatus
                {
                    ZoneId = zoneId,
                    IsPlaying = false,
                    ActiveStreams = _players.Count,
                    MaxStreams = _config.MaxStreams,
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

            // Get status for all configured zones
            for (int i = 0; i < _zoneConfigs.Count(); i++)
            {
                var zoneId = i + 1; // Zone IDs are 1-based
                var zoneConfig = _zoneConfigs.ElementAt(i);

                if (_players.TryGetValue(zoneId, out var player))
                {
                    var status = player.GetStatus();
                    status.ActiveStreams = _players.Count;
                    status.MaxStreams = _config.MaxStreams;
                    allStatuses.Add(status);
                }
                else
                {
                    allStatuses.Add(
                        new PlaybackStatus
                        {
                            ZoneId = zoneId,
                            IsPlaying = false,
                            ActiveStreams = _players.Count,
                            MaxStreams = _config.MaxStreams,
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
            var stopTasks = _players.Keys.Select(zoneId => StopAsync(zoneId, cancellationToken));
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
            var statistics = new PlaybackStatistics
            {
                ActiveStreams = _players.Count,
                MaxStreams = _config.MaxStreams,
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
            // Stop all players
            var disposeTasks = _players.Values.Select(p => p.DisposeAsync().AsTask());
            await Task.WhenAll(disposeTasks);

            _players.Clear();
            _disposed = true;

            LogServiceDisposed(_logger);
        }
    }

    // Logger messages
    [LoggerMessage(
        EventId = 911,
        Level = LogLevel.Information,
        Message = "[SoundFlowService] Started playback for zone {ZoneId}: {TrackTitle} from {StreamUrl}"
    )]
    private static partial void LogPlaybackStarted(ILogger logger, int zoneId, string trackTitle, string streamUrl);

    [LoggerMessage(
        EventId = 912,
        Level = LogLevel.Information,
        Message = "[SoundFlowService] Stopped playback for zone {ZoneId}"
    )]
    private static partial void LogPlaybackStopped(ILogger logger, int zoneId);

    [LoggerMessage(
        EventId = 913,
        Level = LogLevel.Information,
        Message = "[SoundFlowService] Paused playback for zone {ZoneId}"
    )]
    private static partial void LogPlaybackPaused(ILogger logger, int zoneId);

    [LoggerMessage(
        EventId = 914,
        Level = LogLevel.Error,
        Message = "[SoundFlowService] Playback error for zone {ZoneId}"
    )]
    private static partial void LogPlaybackError(ILogger logger, int zoneId, Exception exception);

    [LoggerMessage(
        EventId = 915,
        Level = LogLevel.Warning,
        Message = "[SoundFlowService] Maximum concurrent streams reached: {MaxStreams}"
    )]
    private static partial void LogMaxStreamsReached(ILogger logger, int maxStreams);

    [LoggerMessage(
        EventId = 916,
        Level = LogLevel.Information,
        Message = "[SoundFlowService] Stopped all playback - {ActiveStreams} streams stopped"
    )]
    private static partial void LogAllPlaybackStopped(ILogger logger, int activeStreams);

    [LoggerMessage(EventId = 917, Level = LogLevel.Information, Message = "[SoundFlowService] Service disposed")]
    private static partial void LogServiceDisposed(ILogger logger);
}
