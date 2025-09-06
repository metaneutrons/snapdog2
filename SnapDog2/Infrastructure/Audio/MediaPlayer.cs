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

            _logger.LogInformation("StartingStreaming: {Details}", this._zoneIndex, streamUrl);

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
                            _logger.LogInformation("AudioProcessingFailed: {Details}", result.ErrorMessage ?? "Unknown error");
                        }
                        else
                        {
                            _logger.LogInformation("AudioProcessingSuccess: {Details}", this._zoneIndex, result.Metadata != null);

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

                                _logger.LogInformation("MetadataExtracted: {Details}", this._zoneIndex,
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
                                _logger.LogInformation("NoMetadataAvailable: {Details}", this._zoneIndex);
                            }

                            _logger.LogInformation("AudioStreamingCompleted: {Details}", this._zoneIndex);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("StreamingCancelled: {Details}", this._zoneIndex);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("StreamingError: {Details}", ex, this._zoneIndex);
                    }
                },
                this._streamingCts.Token
            );

            _logger.LogInformation("StreamingStarted: {Details}", this._zoneIndex, streamUrl, trackInfo.Title);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogInformation("StreamingFailed: {Details}", ex, this._zoneIndex, streamUrl);
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

            _logger.LogInformation("StreamingStopped: {Details}", this._zoneIndex);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogInformation("StreamingStopFailed: {Details}", ex, this._zoneIndex);
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

        _logger.LogDebug("GetStatusDebug: HasContext={HasContext} IsPlaying={IsPlaying} Disposed={Disposed} HasTrack={HasTrack}",
            this._processingContext != null,
            this._processingContext?.IsPlaying ?? false,
            this._disposed,
            this._currentTrack != null);

        // Create updated track info with current position if we have a track and processing context
        var currentTrack = this._currentTrack;
        if (currentTrack != null && this._processingContext != null && isPlaying)
        {
            _logger.LogInformation("UpdatingTrackPosition");

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
            _logger.LogInformation("NotUpdatingPosition: {Details}", currentTrack != null, this._processingContext != null, isPlaying);
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
            _logger.LogInformation("PositionEventError: {Details}", ex, this._zoneIndex);
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
            _logger.LogInformation("PlaybackStateEventError: {Details}", ex, this._zoneIndex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!this._disposed)
        {
            await this.StopStreamingAsync();
            this._disposed = true;
            _logger.LogInformation("PlayerDisposed: {Details}", this._zoneIndex);
        }
    }

    // Logging methods using source generators for performance
}
