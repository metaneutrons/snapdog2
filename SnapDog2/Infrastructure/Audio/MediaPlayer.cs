namespace SnapDog2.Infrastructure.Audio;

using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;

/// <summary>
/// LibVLC-based audio player for streaming to Snapcast sinks.
/// Handles HTTP audio streaming with format conversion and metadata extraction.
/// </summary>
public sealed partial class MediaPlayer : IAsyncDisposable
{
    private readonly SnapDog2.Core.Configuration.AudioConfig _config;
    private readonly ILogger<MediaPlayer> _logger;
    private readonly ILogger<MetadataManager> _metadataLogger;
    private readonly int _zoneIndex;
    private readonly string _sinkPath;

    private AudioProcessingContext? _processingContext;
    private CancellationTokenSource? _streamingCts;
    private TrackInfo? _currentTrack;
    private DateTime? _playbackStartedAt;
    private bool _disposed;

    public MediaPlayer(
        SnapDog2.Core.Configuration.AudioConfig config,
        ILogger<MediaPlayer> logger,
        ILogger<MetadataManager> metadataLogger,
        int zoneIndex,
        string sinkPath
    )
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metadataLogger = metadataLogger ?? throw new ArgumentNullException(nameof(metadataLogger));
        _zoneIndex = zoneIndex;
        _sinkPath = sinkPath ?? throw new ArgumentNullException(nameof(sinkPath));
    }

    /// <summary>
    /// Starts streaming audio from the specified URL to the Snapcast sink.
    /// </summary>
    /// <param name="streamUrl">The URL to stream from.</param>
    /// <param name="trackInfo">Information about the track being played.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<Result> StartStreamingAsync(
        string streamUrl,
        TrackInfo trackInfo,
        CancellationToken cancellationToken = default
    )
    {
        if (_disposed)
        {
            return Result.Failure(new ObjectDisposedException(nameof(MediaPlayer)));
        }

        try
        {
            // Stop any existing streaming
            await StopStreamingAsync();

            _logger.LogInformation("Starting audio streaming for zone {ZoneIndex}: {StreamUrl}", _zoneIndex, streamUrl);

            // Create new processing context
            _processingContext = new AudioProcessingContext(_config, _logger, _metadataLogger, _config.TempDirectory);
            _streamingCts = new CancellationTokenSource();
            _currentTrack = trackInfo;
            _playbackStartedAt = DateTime.UtcNow;

            // Start processing in background
            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        var result = await _processingContext.ProcessAudioStreamAsync(
                            streamUrl,
                            AudioSourceType.Url,
                            _streamingCts.Token
                        );

                        if (result.Success && !string.IsNullOrEmpty(result.OutputFilePath))
                        {
                            // Copy the processed audio to the Snapcast sink
                            await CopyToSinkAsync(result.OutputFilePath, _streamingCts.Token);
                        }
                        else
                        {
                            _logger.LogError("Audio processing failed: {ErrorMessage}", result.ErrorMessage);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Audio streaming was cancelled for zone {ZoneIndex}", _zoneIndex);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during audio streaming for zone {ZoneIndex}", _zoneIndex);
                    }
                },
                _streamingCts.Token
            );

            LogStreamingStarted(_logger, _zoneIndex, streamUrl, trackInfo.Title ?? "Unknown");
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogStreamingFailed(_logger, ex, _zoneIndex, streamUrl);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Stops the current audio streaming.
    /// </summary>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<Result> StopStreamingAsync()
    {
        try
        {
            if (_streamingCts != null)
            {
                _streamingCts.Cancel();
                _streamingCts.Dispose();
                _streamingCts = null;
            }

            if (_processingContext != null)
            {
                _processingContext.Stop();
                await _processingContext.DisposeAsync();
                _processingContext = null;
            }

            _currentTrack = null;
            _playbackStartedAt = null;

            LogStreamingStopped(_logger, _zoneIndex);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogStreamingStopFailed(_logger, ex, _zoneIndex);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Gets the current playback status.
    /// </summary>
    /// <returns>Current playback status.</returns>
    public PlaybackStatus GetStatus()
    {
        var isPlaying = _processingContext?.IsPlaying == true && !_disposed;

        return new PlaybackStatus
        {
            ZoneIndex = _zoneIndex,
            IsPlaying = isPlaying,
            CurrentTrack = _currentTrack,
            AudioFormat = new AudioFormat(_config.SampleRate, _config.BitDepth, _config.Channels),
            PlaybackStartedAt = _playbackStartedAt,
            ActiveStreams = isPlaying ? 1 : 0,
            MaxStreams = 1, // Each MediaPlayer handles exactly 1 stream
        };
    }

    /// <summary>
    /// Copies processed audio data to the Snapcast sink.
    /// </summary>
    /// <param name="sourceFilePath">Path to the processed audio file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task CopyToSinkAsync(string sourceFilePath, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Copying audio data from {SourcePath} to {SinkPath}", sourceFilePath, _sinkPath);

            // Ensure the sink directory exists
            var sinkDirectory = Path.GetDirectoryName(_sinkPath);
            if (!string.IsNullOrEmpty(sinkDirectory))
            {
                Directory.CreateDirectory(sinkDirectory);
            }

            // Stream copy the audio data to the sink
            using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sinkStream = new FileStream(_sinkPath, FileMode.Create, FileAccess.Write, FileShare.Read);

            await sourceStream.CopyToAsync(sinkStream, cancellationToken);
            await sinkStream.FlushAsync(cancellationToken);

            _logger.LogDebug("Audio data copied successfully to sink: {SinkPath}", _sinkPath);

            // Clean up the temporary file
            try
            {
                File.Delete(sourceFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary file: {FilePath}", sourceFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy audio data to sink: {SinkPath}", _sinkPath);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await StopStreamingAsync();
            _disposed = true;
            LogPlayerDisposed(_logger, _zoneIndex);
        }
    }

    // Logging methods using source generators for performance
    [LoggerMessage(
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Audio streaming started for zone {ZoneIndex}: {StreamUrl} - {TrackTitle}"
    )]
    private static partial void LogStreamingStarted(ILogger logger, int zoneIndex, string streamUrl, string trackTitle);

    [LoggerMessage(
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Audio streaming stopped for zone {ZoneIndex}"
    )]
    private static partial void LogStreamingStopped(ILogger logger, int zoneIndex);

    [LoggerMessage(
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to start audio streaming for zone {ZoneIndex}: {StreamUrl}"
    )]
    private static partial void LogStreamingFailed(
        ILogger logger,
        Exception exception,
        int zoneIndex,
        string streamUrl
    );

    [LoggerMessage(
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to stop audio streaming for zone {ZoneIndex}"
    )]
    private static partial void LogStreamingStopFailed(ILogger logger, Exception exception, int zoneIndex);

    [LoggerMessage(
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "MediaPlayer disposed for zone {ZoneIndex}"
    )]
    private static partial void LogPlayerDisposed(ILogger logger, int zoneIndex);
}
