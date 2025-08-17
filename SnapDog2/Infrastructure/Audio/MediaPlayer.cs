namespace SnapDog2.Infrastructure.Audio;

using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;

/// <summary>
/// LibVLC-based audio player for streaming to Snapcast sinks.
/// Handles HTTP audio streaming with format conversion and metadata extraction.
/// </summary>
public sealed partial class MediaPlayer(
    SnapDog2.Core.Configuration.AudioConfig config,
    ILogger<MediaPlayer> logger,
    ILogger<MetadataManager> metadataLogger,
    int zoneIndex,
    string sinkPath
) : IAsyncDisposable
{
    private readonly SnapDog2.Core.Configuration.AudioConfig _config =
        config ?? throw new ArgumentNullException(nameof(config));
    private readonly ILogger<MediaPlayer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ILogger<MetadataManager> _metadataLogger =
        metadataLogger ?? throw new ArgumentNullException(nameof(metadataLogger));
    private readonly int _zoneIndex = zoneIndex;
    private readonly string _sinkPath = sinkPath ?? throw new ArgumentNullException(nameof(sinkPath));

    private AudioProcessingContext? _processingContext;
    private CancellationTokenSource? _streamingCts;
    private TrackInfo? _currentTrack;
    private DateTime? _playbackStartedAt;
    private bool _disposed;

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
        if (this._disposed)
        {
            return Result.Failure(new ObjectDisposedException(nameof(MediaPlayer)));
        }

        try
        {
            // Stop any existing streaming
            await this.StopStreamingAsync();

            this._logger.LogInformation(
                "Starting audio streaming for zone {ZoneIndex}: {StreamUrl}",
                this._zoneIndex,
                streamUrl
            );

            // Create new processing context
            this._processingContext = new AudioProcessingContext(
                this._config,
                this._logger,
                this._metadataLogger,
                this._config.TempDirectory
            );
            this._streamingCts = new CancellationTokenSource();
            this._currentTrack = trackInfo;
            this._playbackStartedAt = DateTime.UtcNow;

            // Start processing in background - stream directly to sink
            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        // Ensure the sink directory exists
                        var sinkDirectory = Path.GetDirectoryName(this._sinkPath);
                        if (!string.IsNullOrEmpty(sinkDirectory))
                        {
                            Directory.CreateDirectory(sinkDirectory);
                        }

                        var result = await this._processingContext.ProcessAudioStreamAsync(
                            streamUrl,
                            AudioSourceType.Url,
                            this._sinkPath, // Stream directly to sink instead of temp file
                            this._streamingCts.Token
                        );

                        if (!result.Success)
                        {
                            this._logger.LogError("Audio processing failed: {ErrorMessage}", result.ErrorMessage);
                        }
                        else
                        {
                            this._logger.LogInformation(
                                "Audio streaming completed successfully for zone {ZoneIndex}",
                                this._zoneIndex
                            );
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        this._logger.LogDebug("Audio streaming was cancelled for zone {ZoneIndex}", this._zoneIndex);
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError(ex, "Error during audio streaming for zone {ZoneIndex}", this._zoneIndex);
                    }
                },
                this._streamingCts.Token
            );

            LogStreamingStarted(this._logger, this._zoneIndex, streamUrl, trackInfo.Title ?? "Unknown");
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogStreamingFailed(this._logger, ex, this._zoneIndex, streamUrl);
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
            if (this._streamingCts != null)
            {
                this._streamingCts.Cancel();
                this._streamingCts.Dispose();
                this._streamingCts = null;
            }

            if (this._processingContext != null)
            {
                this._processingContext.Stop();
                await this._processingContext.DisposeAsync();
                this._processingContext = null;
            }

            this._currentTrack = null;
            this._playbackStartedAt = null;

            LogStreamingStopped(this._logger, this._zoneIndex);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogStreamingStopFailed(this._logger, ex, this._zoneIndex);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Gets the current playback status.
    /// </summary>
    /// <returns>Current playback status.</returns>
    public PlaybackStatus GetStatus()
    {
        var isPlaying = this._processingContext?.IsPlaying == true && !this._disposed;

        return new PlaybackStatus
        {
            ZoneIndex = this._zoneIndex,
            IsPlaying = isPlaying,
            CurrentTrack = this._currentTrack,
            AudioFormat = new AudioFormat(this._config.SampleRate, this._config.BitDepth, this._config.Channels),
            PlaybackStartedAt = this._playbackStartedAt,
            ActiveStreams = isPlaying ? 1 : 0,
            MaxStreams = 1, // Each MediaPlayer handles exactly 1 stream
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (!this._disposed)
        {
            await this.StopStreamingAsync();
            this._disposed = true;
            LogPlayerDisposed(this._logger, this._zoneIndex);
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
