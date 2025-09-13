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

using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// LibVLC-based audio player for streaming to Snapcast sinks.
/// Handles HTTP audio streaming with format conversion and metadata extraction.
/// </summary>
public sealed partial class MediaPlayer(
    AudioConfig config,
    ILogger<MediaPlayer> logger,
    ILogger<MetadataManager> metadataLogger,
    int zoneIndex,
    string sinkPath
) : IAsyncDisposable
{
    private readonly AudioConfig _config =
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

    // Events for real-time updates
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;
    public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
    public event EventHandler<TrackInfoChangedEventArgs>? TrackInfoChanged;

    /// <summary>
    /// Seeks to a specific position in milliseconds.
    /// </summary>
    public bool SeekToPosition(long positionMs)
    {
        return _processingContext?.SeekToPosition(positionMs) ?? false;
    }

    /// <summary>
    /// Seeks to a specific progress percentage (0.0-1.0).
    /// </summary>
    public bool SeekToProgress(float progress)
    {
        return _processingContext?.SeekToProgress(progress) ?? false;
    }

    /// <summary>
    /// Sets the expected duration from track metadata (for streams where LibVLC can't determine duration).
    /// </summary>
    public void SetMetadataDuration(long? durationMs)
    {
        if (_processingContext != null)
        {
            _processingContext.MetadataDurationMs = durationMs;
            LogSetMetadataDuration(_logger, durationMs);
        }
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
        if (this._disposed)
        {
            return Result.Failure(new ObjectDisposedException(nameof(MediaPlayer)));
        }

        try
        {
            // Stop any existing streaming
            await this.StopStreamingAsync();

            LogStartedStreaming(_logger, this._zoneIndex, streamUrl);

            // Create new processing context
            try
            {
                this._processingContext = new AudioProcessingContext(
                    this._config,
                    this._logger,
                    this._metadataLogger,
                    this._config.TempDirectory
                );

                LogAudioProcessingContextCreated(_logger, this._zoneIndex);
            }
            catch (Exception ex)
            {
                LogAudioProcessingContextFailed(_logger, this._zoneIndex, ex.Message);
                throw;
            }

            // Subscribe to real-time events
            try
            {
                this._processingContext.PositionChanged += this.OnPositionChanged;
                this._processingContext.PlaybackStateChanged += this.OnPlaybackStateChanged;
                this._processingContext.EncounteredError += this.OnEncounteredError;
                LogEventHandlersSubscribed(_logger, this._zoneIndex);
            }
            catch (Exception ex)
            {
                LogEventSubscriptionFailed(_logger, this._zoneIndex, ex.Message);
                throw;
            }
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
                            LogAudioProcessingFailedWithMessage(_logger, result.ErrorMessage ?? "Unknown error");
                        }
                        else
                        {
                            LogAudioProcessingCompleted(_logger, this._zoneIndex, result.Metadata != null);

                            // Update track info with extracted metadata from LibVLC
                            if (result.Metadata != null)
                            {
                                var updatedTrack = this._currentTrack with
                                {
                                    // Only update title if we don't already have a good one from playlist
                                    Title = !string.IsNullOrWhiteSpace(this._currentTrack.Title) ? this._currentTrack.Title : (result.Metadata.Title ?? this._currentTrack.Title),
                                    Artist = !string.IsNullOrWhiteSpace(result.Metadata.Artist) ? result.Metadata.Artist : this._currentTrack.Artist,
                                    Album = !string.IsNullOrWhiteSpace(result.Metadata.Album) ? result.Metadata.Album : this._currentTrack.Album,
                                    DurationMs = result.Metadata.Duration > 0 ? (int)result.Metadata.Duration : this._currentTrack.DurationMs,
                                };

                                this._currentTrack = updatedTrack;

                                LogMetadataExtracted(_logger, this._zoneIndex,
                                    updatedTrack.Title ?? "Unknown",
                                    updatedTrack.Artist ?? "Unknown");

                                // Fire event to notify Zone of track info change
                                this.TrackInfoChanged?.Invoke(this, new TrackInfoChangedEventArgs
                                {
                                    TrackInfo = updatedTrack
                                });
                            }
                            else
                            {
                                LogNoMetadataAvailable(_logger, this._zoneIndex);
                            }

                            LogAudioStreamingCompleted(_logger, this._zoneIndex);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogStreamingCancelled(_logger, this._zoneIndex);
                    }
                    catch (Exception ex)
                    {
                        LogStreamingOperationFailed(_logger, ex, this._zoneIndex);
                    }
                },
                this._streamingCts.Token
            );

            LogStartedPlayback(_logger, this._zoneIndex, trackInfo.Title, streamUrl);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogPlaybackFailed(_logger, this._zoneIndex, streamUrl, ex.Message);
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

            LogStreamingStopped(_logger, this._zoneIndex);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogStreamingOperationFailed(_logger, ex, this._zoneIndex);
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

        LogGetStatusDebug(_logger,
            this._processingContext != null,
            this._processingContext?.IsPlaying ?? false,
            this._disposed,
            this._currentTrack != null);

        // Create updated track info with current position if we have a track and processing context
        var currentTrack = this._currentTrack;
        if (currentTrack != null && this._processingContext != null && isPlaying)
        {
            LogUpdatingTrackPosition(_logger);

            // Update track with real-time position information from LibVLC
            currentTrack = currentTrack with
            {
                IsPlaying = isPlaying,
                PositionMs = this._processingContext.PositionMs,
                Progress = this._processingContext.Progress,
                DurationMs =
                    this._processingContext.DurationMs > 0
                        ? this._processingContext.DurationMs
                        : currentTrack.DurationMs,
            };
        }
        else
        {
            LogPlaybackStatus(_logger, currentTrack != null, this._processingContext != null, isPlaying);
        }

        return new PlaybackStatus
        {
            ZoneIndex = this._zoneIndex,
            IsPlaying = isPlaying,
            CurrentTrack = currentTrack,
            AudioFormat = new AudioFormat(this._config.SampleRate, this._config.BitDepth, this._config.Channels),
            PlaybackStartedAt = this._playbackStartedAt,
            ActiveStreams = isPlaying ? 1 : 0,
            MaxStreams = 1, // Each MediaPlayer handles exactly 1 stream
        };
    }

    /// <summary>
    /// Handles position changes from LibVLC and forwards them.
    /// </summary>
    private void OnPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        try
        {
            this.PositionChanged?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            LogStreamingOperationFailed(_logger, ex, this._zoneIndex);
        }
    }

    /// <summary>
    /// Handles playback state changes from LibVLC and forwards them.
    /// </summary>
    private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        try
        {
            this.PlaybackStateChanged?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            LogStreamingOperationFailed(_logger, ex, this._zoneIndex);
        }
    }

    /// <summary>
    /// Handles LibVLC errors and logs them for debugging.
    /// </summary>
    private void OnEncounteredError(object? sender, EventArgs e)
    {
        try
        {
            LogLibVlcError(_logger, this._zoneIndex, sender?.ToString() ?? "Unknown error");
        }
        catch (Exception ex)
        {
            LogLibVlcErrorHandlingFailed(_logger, this._zoneIndex, ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!this._disposed)
        {
            await this.StopStreamingAsync();
            this._disposed = true;
            LogPlayerDisposed(_logger, this._zoneIndex);
        }
    }

    // Logging methods using source generators for performance
    [LoggerMessage(EventId = 16045, Level = LogLevel.Information, Message = "Set metadata duration: {DurationMs}ms for progress calculation")]
    private static partial void LogSetMetadataDuration(ILogger logger, long? DurationMs);

    [LoggerMessage(EventId = 16046, Level = LogLevel.Information, Message = "Started streaming for zone {ZoneIndex} from {StreamUrl}")]
    private static partial void LogStartedStreaming(ILogger logger, int ZoneIndex, string StreamUrl);

    [LoggerMessage(EventId = 16047, Level = LogLevel.Information, Message = "AudioProcessingContext created successfully for zone {ZoneIndex}")]
    private static partial void LogAudioProcessingContextCreated(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16048, Level = LogLevel.Error, Message = "Failed to create AudioProcessingContext for zone {ZoneIndex}: {Error}")]
    private static partial void LogAudioProcessingContextFailed(ILogger logger, int ZoneIndex, string Error);

    [LoggerMessage(EventId = 16049, Level = LogLevel.Information, Message = "Event handlers subscribed successfully for zone {ZoneIndex}")]
    private static partial void LogEventHandlersSubscribed(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16050, Level = LogLevel.Error, Message = "Failed to subscribe to events for zone {ZoneIndex}: {Error}")]
    private static partial void LogEventSubscriptionFailed(ILogger logger, int ZoneIndex, string Error);

    [LoggerMessage(EventId = 16051, Level = LogLevel.Error, Message = "Audio processing failed")]
    private static partial void LogAudioProcessingFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 16052, Level = LogLevel.Information, Message = "Audio processing completed for zone {ZoneIndex}, metadata available: {HasMetadata}")]
    private static partial void LogAudioProcessingCompleted(ILogger logger, int ZoneIndex, bool HasMetadata);

    [LoggerMessage(EventId = 16053, Level = LogLevel.Information, Message = "Metadata extracted for zone {ZoneIndex}: {Title} by {Artist}")]
    private static partial void LogMetadataExtracted(ILogger logger, int ZoneIndex, string? Title, string? Artist);

    [LoggerMessage(EventId = 16054, Level = LogLevel.Information, Message = "No metadata available for zone {ZoneIndex}")]
    private static partial void LogNoMetadataAvailable(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16055, Level = LogLevel.Information, Message = "Updating track position")]
    private static partial void LogUpdatingTrackPosition(ILogger logger);

    [LoggerMessage(EventId = 16056, Level = LogLevel.Information, Message = "Player disposed for zone {ZoneIndex}")]
    private static partial void LogPlayerDisposed(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16057, Level = LogLevel.Error, Message = "Audio processing failed: {ErrorMessage}")]
    private static partial void LogAudioProcessingFailedWithMessage(ILogger logger, string ErrorMessage);

    [LoggerMessage(EventId = 16058, Level = LogLevel.Information, Message = "Audio streaming completed for zone {ZoneIndex}")]
    private static partial void LogAudioStreamingCompleted(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16059, Level = LogLevel.Information, Message = "Streaming cancelled for zone {ZoneIndex}")]
    private static partial void LogStreamingCancelled(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16060, Level = LogLevel.Error, Message = "Streaming operation failed for zone {ZoneIndex}")]
    private static partial void LogStreamingOperationFailed(ILogger logger, Exception ex, int ZoneIndex);

    [LoggerMessage(EventId = 16061, Level = LogLevel.Information, Message = "Started playback on zone {ZoneIndex}: {Title} from {StreamUrl}")]
    private static partial void LogStartedPlayback(ILogger logger, int ZoneIndex, string Title, string StreamUrl);

    [LoggerMessage(EventId = 16062, Level = LogLevel.Error, Message = "Playback failed on zone {ZoneIndex} for {StreamUrl}: {Error}")]
    private static partial void LogPlaybackFailed(ILogger logger, int ZoneIndex, string StreamUrl, string Error);

    [LoggerMessage(EventId = 16063, Level = LogLevel.Information, Message = "Streaming stopped for zone {ZoneIndex}")]
    private static partial void LogStreamingStopped(ILogger logger, int ZoneIndex);

    [LoggerMessage(EventId = 16064, Level = LogLevel.Error, Message = "Stop streaming operation failed for zone {ZoneIndex}")]
    private static partial void LogStopStreamingOperationFailed(ILogger logger, Exception ex, int ZoneIndex);

    [LoggerMessage(EventId = 16065, Level = LogLevel.Debug, Message = "Status debug: HasContext={HasContext} IsPlaying={IsPlaying} Disposed={Disposed} HasTrack={HasTrack}")]
    private static partial void LogGetStatusDebug(ILogger logger, bool HasContext, bool IsPlaying, bool Disposed, bool HasTrack);

    [LoggerMessage(EventId = 16066, Level = LogLevel.Information, Message = "Playback status: track={HasTrack}, context={HasContext}, playing={IsPlaying}")]
    private static partial void LogPlaybackStatus(ILogger logger, bool HasTrack, bool HasContext, bool IsPlaying);

    [LoggerMessage(EventId = 16067, Level = LogLevel.Error, Message = "Get status operation failed for zone {ZoneIndex}")]
    private static partial void LogGetStatusOperationFailed(ILogger logger, Exception ex, int ZoneIndex);

    [LoggerMessage(EventId = 16068, Level = LogLevel.Error, Message = "Get progress operation failed for zone {ZoneIndex}")]
    private static partial void LogGetProgressOperationFailed(ILogger logger, Exception ex, int ZoneIndex);

    [LoggerMessage(EventId = 16069, Level = LogLevel.Error, Message = "LibVLC Error encountered for zone {ZoneIndex}: {ErrorDetails}")]
    private static partial void LogLibVlcError(ILogger logger, int ZoneIndex, string ErrorDetails);

    [LoggerMessage(EventId = 16070, Level = LogLevel.Error, Message = "Failed to handle LibVLC error for zone {ZoneIndex}")]
    private static partial void LogLibVlcErrorHandlingFailed(ILogger logger, int ZoneIndex, Exception ex);
}
