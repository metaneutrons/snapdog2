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

using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// LibVLC-based audio player for streaming to Snapcast sinks.
/// Handles HTTP audio streaming with format conversion and metadata extraction.
/// </summary>
public sealed partial class MediaPlayer(
    SnapDog2.Shared.Configuration.AudioConfig config,
    ILogger<MediaPlayer> logger,
    ILogger<MetadataManager> metadataLogger,
    int zoneIndex,
    string sinkPath
) : IAsyncDisposable
{
    private readonly SnapDog2.Shared.Configuration.AudioConfig _config =
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

            LogStartingStreaming(this._logger, this._zoneIndex, streamUrl);

            // Create new processing context
            this._processingContext = new AudioProcessingContext(
                this._config,
                this._logger,
                this._metadataLogger,
                this._config.TempDirectory
            );

            // Subscribe to real-time events
            this._processingContext.PositionChanged += this.OnPositionChanged;
            this._processingContext.PlaybackStateChanged += this.OnPlaybackStateChanged;
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
                            LogAudioProcessingFailed(this._logger, result.ErrorMessage ?? "Unknown error");
                        }
                        else
                        {
                            LogAudioProcessingSuccess(this._logger, this._zoneIndex, result.Metadata != null);

                            // Update track info with extracted metadata from LibVLC
                            if (result.Metadata != null)
                            {
                                var updatedTrack = this._currentTrack with
                                {
                                    Title = !string.IsNullOrWhiteSpace(result.Metadata.Title) ? result.Metadata.Title : this._currentTrack.Title,
                                    Artist = !string.IsNullOrWhiteSpace(result.Metadata.Artist) ? result.Metadata.Artist : this._currentTrack.Artist,
                                    Album = !string.IsNullOrWhiteSpace(result.Metadata.Album) ? result.Metadata.Album : this._currentTrack.Album,
                                    DurationMs = result.Metadata.Duration > 0 ? (int)result.Metadata.Duration : this._currentTrack.DurationMs,
                                };

                                this._currentTrack = updatedTrack;

                                LogMetadataExtracted(this._logger, this._zoneIndex,
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
                                LogNoMetadataAvailable(this._logger, this._zoneIndex);
                            }

                            LogAudioStreamingCompleted(this._logger, this._zoneIndex);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogStreamingCancelled(this._logger, this._zoneIndex);
                    }
                    catch (Exception ex)
                    {
                        LogStreamingError(this._logger, ex, this._zoneIndex);
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

        LogGetStatusDebug(
            this._logger,
            this._processingContext != null,
            this._processingContext?.IsPlaying ?? false,
            this._disposed,
            this._currentTrack != null
        );

        // Create updated track info with current position if we have a track and processing context
        var currentTrack = this._currentTrack;
        if (currentTrack != null && this._processingContext != null && isPlaying)
        {
            LogUpdatingTrackPosition(this._logger);

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
            LogNotUpdatingPosition(this._logger, currentTrack != null, this._processingContext != null, isPlaying);
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
            LogPositionEventError(this._logger, ex, this._zoneIndex);
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
            LogPlaybackStateEventError(this._logger, ex, this._zoneIndex);
        }
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
        Message = "✅ Metadata extracted for zone {ZoneIndex}: '{Title}' by '{Artist}'"
    )]
    private static partial void LogMetadataExtracted(ILogger logger, int zoneIndex, string title, string artist);

    [LoggerMessage(
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "🔍 Audio processing result for zone {ZoneIndex}: Success=true, HasMetadata={HasMetadata}"
    )]
    private static partial void LogAudioProcessingSuccess(ILogger logger, int zoneIndex, bool hasMetadata);

    [LoggerMessage(
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "⚠️ No metadata available for zone {ZoneIndex}"
    )]
    private static partial void LogNoMetadataAvailable(ILogger logger, int zoneIndex);

    [LoggerMessage(
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🎵 Audio streaming started for zone {ZoneIndex}"
    )]
    private static partial void LogAudioStreamingStarted(ILogger logger, int zoneIndex);

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

    [LoggerMessage(
        EventId = 2200,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Starting audio streaming for zone {ZoneIndex}: {StreamUrl}"
    )]
    private static partial void LogStartingStreaming(ILogger logger, int zoneIndex, string streamUrl);

    [LoggerMessage(
        EventId = 2201,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Audio processing failed: {ErrorMessage}"
    )]
    private static partial void LogAudioProcessingFailed(ILogger logger, string errorMessage);

    [LoggerMessage(
        EventId = 2202,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Audio streaming completed successfully for zone {ZoneIndex}"
    )]
    private static partial void LogAudioStreamingCompleted(ILogger logger, int zoneIndex);

    [LoggerMessage(
        EventId = 2203,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Audio streaming was cancelled for zone {ZoneIndex}"
    )]
    private static partial void LogStreamingCancelled(ILogger logger, int zoneIndex);

    [LoggerMessage(
        EventId = 2204,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error during audio streaming for zone {ZoneIndex}"
    )]
    private static partial void LogStreamingError(ILogger logger, Exception ex, int zoneIndex);

    [LoggerMessage(
        EventId = 2205,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "MediaPlayer.GetStatus() - ProcessingContext: {HasContext}, IsPlaying: {IsPlaying}, Disposed: {Disposed}, CurrentTrack: {HasTrack}"
    )]
    private static partial void LogGetStatusDebug(
        ILogger logger,
        bool hasContext,
        bool isPlaying,
        bool disposed,
        bool hasTrack
    );

    [LoggerMessage(
        EventId = 2206,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Updating track position from LibVLC..."
    )]
    private static partial void LogUpdatingTrackPosition(ILogger logger);

    [LoggerMessage(
        EventId = 2207,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "❌ NOT updating position - CurrentTrack: {HasTrack}, ProcessingContext: {HasContext}, IsPlaying: {IsPlaying}"
    )]
    private static partial void LogNotUpdatingPosition(ILogger logger, bool hasTrack, bool hasContext, bool isPlaying);

    [LoggerMessage(
        EventId = 2208,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Error forwarding position changed event for zone {ZoneIndex}"
    )]
    private static partial void LogPositionEventError(ILogger logger, Exception ex, int zoneIndex);

    [LoggerMessage(
        EventId = 2209,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Error forwarding playback state changed event for zone {ZoneIndex}"
    )]
    private static partial void LogPlaybackStateEventError(ILogger logger, Exception ex, int zoneIndex);
}
