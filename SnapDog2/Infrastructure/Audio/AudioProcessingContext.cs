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
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

/// <summary>
/// Event arguments for position changes.
/// </summary>
public class PositionChangedEventArgs : EventArgs
{
    public long PositionMs { get; init; }
    public float Progress { get; init; }
    public long DurationMs { get; init; }
}

/// <summary>
/// Event arguments for playback state changes.
/// </summary>
public class PlaybackStateChangedEventArgs : EventArgs
{
    public bool IsPlaying { get; init; }
    public VLCState State { get; init; }
}

/// <summary>
/// Event arguments for track info changes.
/// </summary>
public class TrackInfoChangedEventArgs : EventArgs
{
    public required TrackInfo TrackInfo { get; init; }
}

/// <summary>
/// Audio processing context using LibVLC for streaming and metadata extraction.
/// </summary>
public sealed partial class AudioProcessingContext : IAsyncDisposable, IDisposable
{
    private readonly LibVLC _libvlc;
    private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
    private readonly ILogger _logger;
    private readonly DirectoryInfo _tempDirectory;
    private readonly SystemConfig _systemConfig;
    private bool _disposed;

    // Debouncing for position events using config
    private DateTime _lastPositionUpdate = DateTime.MinValue;
    private readonly TimeSpan _positionDebounce;

    // Event for position changes
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;
    public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
    public event EventHandler<EventArgs>? EncounteredError;

    public AudioProcessingConfig Config { get; }
    public MetadataManager MetadataManager { get; }

    public AudioProcessingContext(
        AudioConfig config,
        ILogger logger,
        ILogger<MetadataManager> metadataLogger,
        ServicesConfig servicesConfig,
        SystemConfig systemConfig,
        string? tempDirectory = null
    )
    {
        // Initialize LibVLCSharp with proper error handling
        try
        {
            Core.Initialize();
        }
        catch (Exception ex)
        {
            this.LogLibVLCCoreInitializationFailed(ex);
            throw new InvalidOperationException(
                "LibVLC initialization failed. Check that libvlc5 and libvlccore9 are installed.",
                ex
            );
        }

        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._systemConfig = systemConfig ?? throw new ArgumentNullException(nameof(systemConfig));
        this._positionDebounce = TimeSpan.FromMilliseconds(servicesConfig.DebouncingMs);

        var args = config.LibVLCArgs;
        try
        {
            this._libvlc = new LibVLC(args);
            this._mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(this._libvlc);

            // Subscribe to LibVLC events for real-time position updates
            this.SetupEventHandlers();
        }
        catch (Exception ex)
        {
            this.LogLibVLCInstanceCreationFailed(ex, string.Join(" ", args));
            throw new InvalidOperationException(
                "LibVLC instance creation failed. Check LibVLC installation and arguments.",
                ex
            );
        }

        var tempDir = tempDirectory ?? config.TempDirectory;
        this._tempDirectory = Directory.CreateDirectory(tempDir);

        this.Config = new AudioProcessingConfig
        {
            SampleRate = config.SampleRate,
            BitsPerSample = config.BitDepth,
            Channels = config.Channels,
            Format = config.OutputFormat,
        };

        this.MetadataManager = new MetadataManager(metadataLogger);

        this.LogAudioProcessingContextInitialized(this._tempDirectory.FullName);
    }

    /// <summary>
    /// Processes an audio stream from a URL and extracts metadata.
    /// For streaming sources, this will continue until cancelled.
    /// </summary>
    /// <param name="sourceUrl">The source URL to process.</param>
    /// <param name="sourceType">The type of audio source.</param>
    /// <param name="outputPath">The output path where audio should be written (e.g., Snapcast sink). If null, uses temp directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audio processing result with output file and metadata.</returns>
    public async Task<AudioProcessingResult> ProcessAudioStreamAsync(
        string sourceUrl,
        AudioSourceType sourceType = AudioSourceType.Url,
        string? outputPath = null,
        CancellationToken cancellationToken = default
    )
    {
        if (this._disposed)
        {
            throw new ObjectDisposedException(nameof(AudioProcessingContext));
        }

        try
        {
            var sourceId = Guid.NewGuid().ToString("N");

            // Use provided output path or generate temp file path
            var finalOutputPath =
                outputPath ?? Path.Combine(this._tempDirectory.FullName, $"{sourceId}.{this.Config.Format}");
            var metadataPath = Path.ChangeExtension(finalOutputPath, ".json");

            this.LogStartingAudioProcessing(sourceUrl, finalOutputPath);

            // Debug: Log the actual URL being processed
            this.LogStreamingUrlDebug(sourceUrl);

            // Build media options for raw audio output
            var mediaOptions = this.BuildMediaOptions(finalOutputPath);

            using var media = new Media(this._libvlc, sourceUrl, FromType.FromLocation, mediaOptions.ToArray());

            // Extract metadata before starting playback
            var metadata = await this.MetadataManager.ExtractMetadataAsync(media, cancellationToken);

            // Set up the media player
            this._mediaPlayer.Media = media;

            // Start playback (which will write to the output file/pipe)
            var playResult = this._mediaPlayer.Play();
            if (!playResult)
            {
                return new AudioProcessingResult { Success = false, ErrorMessage = "Failed → start media playback" };
            }

            // Wait for playback to start
            var startTimeout = TimeSpan.FromSeconds(10);
            var startTime = DateTime.UtcNow;

            while (this._mediaPlayer.State == VLCState.Opening || this._mediaPlayer.State == VLCState.Buffering)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    this._mediaPlayer.Stop();
                    return new AudioProcessingResult { Success = false, ErrorMessage = "Operation was cancelled" };
                }

                if (DateTime.UtcNow - startTime > startTimeout)
                {
                    this._mediaPlayer.Stop();
                    return new AudioProcessingResult
                    {
                        Success = false,
                        ErrorMessage = "Timeout waiting for playback → start",
                    };
                }

                await Task.Delay(100, cancellationToken);
            }

            // Check if playback started successfully
            if (this._mediaPlayer.State == VLCState.Error)
            {
                return new AudioProcessingResult { Success = false, ErrorMessage = "Media playback failed" };
            }

            // Log media properties and format for debugging
            var mediaFormat = this._mediaPlayer.Media?.Mrl ?? "Unknown";
            var contentType = "Unknown";

            // Try to detect format from URL or media info
            if (mediaFormat.Contains(".mp3") || mediaFormat.Contains("format=mp3"))
            {
                contentType = "MP3";
            }
            else if (mediaFormat.Contains(".mp4") || mediaFormat.Contains(".m4a"))
            {
                contentType = "MP4/M4A";
            }
            else if (mediaFormat.Contains("radio") || mediaFormat.Contains("stream"))
            {
                contentType = "Radio Stream";
            }

            this.LogMediaPropertiesWithFormat(
                this._mediaPlayer.Length,
                this._mediaPlayer.IsSeekable,
                this._mediaPlayer.State.ToString(),
                contentType,
                mediaFormat
            );

            // Save metadata to JSON file (disabled - metadata available programmatically)
            // await this.MetadataManager.SaveMetadataAsync(metadata, metadataPath, cancellationToken);

            this.LogAudioStreamingStartedSuccessfully(finalOutputPath);

            // For streaming sources (like radio), keep the stream alive until cancelled
            if (
                sourceType == AudioSourceType.Url
                && (sourceUrl.StartsWith("http://") || sourceUrl.StartsWith("https://"))
            )
            {
                this.LogMaintainingContinuousStream(sourceUrl);

                // Keep streaming until cancelled or error occurs
                while (
                    !cancellationToken.IsCancellationRequested
                    && this._mediaPlayer.State != VLCState.Error
                    && this._mediaPlayer.State != VLCState.Ended
                )
                {
                    await Task.Delay(1000, cancellationToken);

                    // Log periodic status for debugging
                    if (DateTime.UtcNow.Second % 30 == 0) // Every 30 seconds
                    {
                        this.LogStreamStatus(this._mediaPlayer.State, this._mediaPlayer.IsPlaying);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    this.LogStreamCancelled(sourceUrl);
                }
                else if (this._mediaPlayer.State == VLCState.Error)
                {
                    this.LogStreamEndedWithError(sourceUrl);
                }
                else
                {
                    this.LogStreamEndedNaturally(sourceUrl);
                }
            }

            this.LogAudioProcessingCompleted(finalOutputPath);

            return new AudioProcessingResult
            {
                Success = true,
                OutputFilePath = finalOutputPath,
                MetadataPath = metadataPath,
                SourceId = sourceId,
                Config = this.Config,
                Metadata = metadata,
            };
        }
        catch (OperationCanceledException)
        {
            this.LogAudioProcessingWasCancelled();
            return new AudioProcessingResult { Success = false, ErrorMessage = "Operation was cancelled" };
        }
        catch (Exception ex)
        {
            this.LogFailedToProcessAudioStream(ex, sourceUrl);
            return new AudioProcessingResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Builds LibVLC media options for raw audio output to named pipe.
    /// Respects the audio configuration from environment variables.
    /// </summary>
    /// <param name="outputPath">The output pipe path.</param>
    /// <returns>List of media options.</returns>
    private List<string> BuildMediaOptions(string outputPath)
    {
        var options = new List<string>();

        // TODO: Find a more elegant and less resource-hungry solution than transcoding.
        // We have fixed bitrate sinks only, so we need some other software to handle
        // different bitrates and bit depths if VLC outputs varying formats.
        // Consider using a lightweight audio format converter or buffer that can
        // adapt to different input formats without full transcoding.

        var isDevelopment = _systemConfig.Environment == ApplicationEnvironment.Development;

        // Always use transcoding for consistent PCM format (fixes stream flickering)
        // Raw audio output causes format incompatibility with Snapcast
        var audioCodec = GetLibVLCAudioCodec(this.Config.BitsPerSample);
        var transcode = $"#transcode{{acodec={audioCodec},channels={this.Config.Channels},samplerate={this.Config.SampleRate}}}";
        var standardOutput = $"standard{{access=file,mux=raw,dst={outputPath}}}";
        options.Add($":sout={transcode}:{standardOutput}");
        this.LogTranscodingEnabled(audioCodec, this.Config.Channels, this.Config.SampleRate);

        // Critical options for streaming to named pipes
        options.Add(":no-sout-video");
        options.Add(":sout-keep");
        options.Add(":no-sout-rtp-sap");
        options.Add(":no-sout-standard-sap");
        options.Add(":sout-all"); // Keep streaming even if no one is reading

        // Additional options for continuous streaming
        options.Add(":network-caching=1000"); // 1 second network cache
        options.Add(":file-caching=300");     // 300ms file cache
        options.Add(":live-caching=300");     // 300ms live stream cache

        var codecForLogging = GetLibVLCAudioCodec(this.Config.BitsPerSample);
        this.LogBuiltMediaOptions(codecForLogging, this.Config.SampleRate, this.Config.Channels, string.Join(", ", options));

        return options;
    }

    /// <summary>
    /// Gets the appropriate LibVLC audio codec string based on bit depth.
    /// Maps bit depth to LibVLC PCM codec format.
    /// </summary>
    /// <param name="bitsPerSample">Bit depth from configuration.</param>
    /// <returns>LibVLC audio codec string.</returns>
    private static string GetLibVLCAudioCodec(int bitsPerSample)
    {
        return bitsPerSample switch
        {
            8 => "u8", // 8-bit unsigned PCM
            16 => "s16l", // 16-bit signed little-endian PCM (most common)
            24 => "s24l", // 24-bit signed little-endian PCM
            32 => "s32l", // 32-bit signed little-endian PCM
            _ => throw new ArgumentException(
                $"Unsupported bit depth: {bitsPerSample}. Supported values: 8, 16, 24, 32",
                nameof(bitsPerSample)
            ),
        };
    }

    /// <summary>
    /// Stops any current playback.
    /// </summary>
    public void Stop()
    {
        if (!this._disposed && this._mediaPlayer.IsPlaying)
        {
            this._mediaPlayer.Stop();
            this.LogStoppedMediaPlayback();
        }
    }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    public VLCState State => this._mediaPlayer.State;

    /// <summary>
    /// Gets whether media is currently playing.
    /// </summary>
    public bool IsPlaying
    {
        get
        {
            var isPlaying = this._mediaPlayer.IsPlaying;
            var state = this._mediaPlayer.State;

            this.LogLibVlcIsPlayingCheck(isPlaying, state);

            return isPlaying;
        }
    }

    private long _lastPositionMs = -1;
    private DateTime _lastPositionCheck = DateTime.MinValue;
    private int _stuckPositionCount = 0;

    /// <summary>
    /// Gets the current playback position in milliseconds.
    /// </summary>
    public long PositionMs
    {
        get
        {
            var time = this._mediaPlayer.Time;
            var state = this._mediaPlayer.State;
            var isPlaying = this._mediaPlayer.IsPlaying;

            this.LogLibVlcDirectAccess(time, state, isPlaying);

            // Position tracking validation - detect stuck position
            if (isPlaying && state == VLCState.Playing)
            {
                var now = DateTime.UtcNow;
                if (_lastPositionMs == time && time == 0 &&
                    now - _lastPositionCheck > TimeSpan.FromSeconds(5))
                {
                    _stuckPositionCount++;
                    if (_stuckPositionCount >= 3) // After 15 seconds of stuck position
                    {
                        this.LogPositionTrackingFailed(time, state.ToString());
                        _stuckPositionCount = 0; // Reset to avoid spam
                    }
                }
                else if (_lastPositionMs != time)
                {
                    _stuckPositionCount = 0; // Reset counter when position advances
                }

                _lastPositionMs = time;
                _lastPositionCheck = now;
            }

            return time;
        }
    }

    /// <summary>
    /// Gets the current playback position as a percentage (0.0-1.0).
    /// </summary>
    public float Progress => this._mediaPlayer.Position;

    /// <summary>
    /// Gets the total duration of the media in milliseconds.
    /// </summary>
    public long DurationMs => this.MetadataDurationMs ?? this._mediaPlayer.Length;

    /// <summary>
    /// Gets or sets the duration from track metadata (overrides LibVLC duration for streams).
    /// </summary>
    public long? MetadataDurationMs { get; set; }

    /// <summary>
    /// Seeks to a specific position in milliseconds.
    /// </summary>
    public bool SeekToPosition(long positionMs)
    {
        if (this._disposed || this._mediaPlayer == null || !this._mediaPlayer.IsSeekable)
        {
            return false;
        }

        this._mediaPlayer.Time = positionMs;
        return true;
    }

    /// <summary>
    /// Seeks to a specific progress percentage (0.0-1.0).
    /// </summary>
    public bool SeekToProgress(float progress)
    {
        if (this._disposed || this._mediaPlayer == null || !this._mediaPlayer.IsSeekable)
        {
            return false;
        }

        this._mediaPlayer.Position = Math.Clamp(progress, 0.0f, 1.0f);
        return true;
    }

    /// <summary>
    /// Sets up LibVLC event handlers for real-time position and state updates.
    /// </summary>
    private void SetupEventHandlers()
    {
        this.LogSettingUpLibVlcEventHandlers();

        // Position change events (percentage-based) with configurable debouncing
        this._mediaPlayer.PositionChanged += (_, e) =>
        {
            try
            {
                if (this._disposed || this._mediaPlayer == null)
                {
                    return;
                }

                // Debounce position updates using config
                var now = DateTime.UtcNow;
                if (now - this._lastPositionUpdate < this._positionDebounce)
                {
                    return;
                }
                this._lastPositionUpdate = now;

                var length = this._mediaPlayer.Length;
                var effectiveDuration = this.MetadataDurationMs ?? length;
                var positionMs = length > 0 ? (long)(e.Position * length) : 0;
                var progress = effectiveDuration > 0 ? (float)positionMs / effectiveDuration : 0.0f;

                // Only log and publish if position actually changed meaningfully (using debounce threshold and not 0ms spam)
                if (positionMs > 0 && Math.Abs(positionMs - this._lastPositionMs) > this._positionDebounce.TotalMilliseconds)
                {
                    this._lastPositionMs = positionMs;
                    this.LogLibVlcPositionChanged(progress, positionMs);

                    this.PositionChanged?.Invoke(
                        this,
                        new PositionChangedEventArgs
                        {
                            PositionMs = positionMs,
                            Progress = progress,
                            DurationMs = effectiveDuration,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                this.LogErrorHandlingPositionChangedEvent(ex);
            }
        };

        // Also keep TimeChanged as backup with configurable debouncing
        this._mediaPlayer.TimeChanged += (_, e) =>
        {
            try
            {
                if (this._disposed || this._mediaPlayer == null)
                {
                    return;
                }

                // Debounce time updates using config
                var now = DateTime.UtcNow;
                if (now - this._lastPositionUpdate < this._positionDebounce)
                {
                    return;
                }
                this._lastPositionUpdate = now;

                var length = this._mediaPlayer.Length;
                var effectiveDuration = this.MetadataDurationMs ?? length;
                var progress = effectiveDuration > 0 ? (float)e.Time / effectiveDuration : 0.0f;

                // Only log and publish if position actually changed meaningfully (not 0ms spam)
                if (e.Time > 0 && Math.Abs(e.Time - this._lastPositionMs) > this._positionDebounce.TotalMilliseconds)
                {
                    this._lastPositionMs = e.Time;
                    this.LogLibVlcTimeChanged(e.Time);
                    this.PositionChanged?.Invoke(
                        this,
                        new PositionChangedEventArgs
                        {
                            PositionMs = e.Time,
                            Progress = progress,
                            DurationMs = effectiveDuration,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                this.LogErrorHandlingTimeChangedEvent(ex);
            }
        };

        // Playback state change events
        this._mediaPlayer.Playing += (_, _) =>
        {
            try
            {
                if (this._disposed)
                {
                    return;
                }

                this.PlaybackStateChanged?.Invoke(
                    this,
                    new PlaybackStateChangedEventArgs { IsPlaying = true, State = VLCState.Playing }
                );
            }
            catch (Exception ex)
            {
                this.LogErrorHandlingPlayingEvent(ex);
            }
        };

        this._mediaPlayer.Paused += (_, _) =>
        {
            try
            {
                if (this._disposed)
                {
                    return;
                }

                this.PlaybackStateChanged?.Invoke(
                    this,
                    new PlaybackStateChangedEventArgs { IsPlaying = false, State = VLCState.Paused }
                );
            }
            catch (Exception ex)
            {
                this.LogErrorHandlingPausedEvent(ex);
            }
        };

        this._mediaPlayer.Stopped += (_, _) =>
        {
            try
            {
                if (this._disposed)
                {
                    return;
                }

                this.PlaybackStateChanged?.Invoke(
                    this,
                    new PlaybackStateChangedEventArgs { IsPlaying = false, State = VLCState.Stopped }
                );
            }
            catch (Exception ex)
            {
                this.LogErrorHandlingStoppedEvent(ex);
            }
        };

        this._mediaPlayer.EndReached += (_, _) =>
        {
            try
            {
                if (this._disposed)
                {
                    return;
                }

                this.PlaybackStateChanged?.Invoke(
                    this,
                    new PlaybackStateChangedEventArgs { IsPlaying = false, State = VLCState.Ended }
                );
            }
            catch (Exception ex)
            {
                this.LogErrorHandlingEndReachedEvent(ex);
            }
        };

        this._mediaPlayer.EncounteredError += (_, _) =>
        {
            try
            {
                if (this._disposed)
                {
                    return;
                }

                this.LogLibVlcEncounteredError();
                this.EncounteredError?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                this.LogErrorHandlingEncounteredErrorEvent(ex);
            }
        };
    }

    public void Dispose()
    {
        if (!this._disposed)
        {
            this._disposed = true;

            // Unsubscribe from events before disposing
            try
            {
                if (this._mediaPlayer != null)
                {
                    this._mediaPlayer.Stop();
                    // Clear event handlers
                    this._mediaPlayer.PositionChanged -= null;
                    this._mediaPlayer.TimeChanged -= null;
                    this._mediaPlayer.Playing -= null;
                    this._mediaPlayer.Paused -= null;
                    this._mediaPlayer.Stopped -= null;
                    this._mediaPlayer.EndReached -= null;
                    this._mediaPlayer.Dispose();
                }
                this._libvlc?.Dispose();
            }
            catch (Exception ex)
            {
                // Log but don't throw during disposal
                this.LogErrorDuringDisposal(ex);
            }

            this.LogAudioProcessingContextDisposed();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!this._disposed)
        {
            this._disposed = true;

            // Unsubscribe from events before disposing
            try
            {
                if (this._mediaPlayer != null)
                {
                    this._mediaPlayer.Stop();
                    // Clear event handlers
                    this._mediaPlayer.PositionChanged -= null;
                    this._mediaPlayer.TimeChanged -= null;
                    this._mediaPlayer.Playing -= null;
                    this._mediaPlayer.Paused -= null;
                    this._mediaPlayer.Stopped -= null;
                    this._mediaPlayer.EndReached -= null;
                    this._mediaPlayer.Dispose();
                }
                this._libvlc?.Dispose();
            }
            catch (Exception ex)
            {
                // Log but don't throw during disposal
                this.LogErrorDuringDisposal(ex);
            }

            this.LogAudioProcessingContextDisposedAsynchronously();
        }

        await Task.CompletedTask;
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(EventId = 16000, Level = LogLevel.Debug, Message = "Audio processing context initialized with temp directory: {TempDirectory}"
)]
    private partial void LogAudioProcessingContextInitialized(string TempDirectory);

    [LoggerMessage(EventId = 16001, Level = LogLevel.Information, Message = "Starting audio processing: Source={SourceUrl}, Output={OutputPath}"
)]
    private partial void LogStartingAudioProcessing(string SourceUrl, string OutputPath);

    [LoggerMessage(EventId = 16002, Level = LogLevel.Information, Message = "Audio streaming started successfully: {OutputPath}"
)]
    private partial void LogAudioStreamingStartedSuccessfully(string OutputPath);

    [LoggerMessage(EventId = 16003, Level = LogLevel.Information, Message = "Maintaining continuous stream for URL source: {SourceUrl}"
)]
    private partial void LogMaintainingContinuousStream(string SourceUrl);

    [LoggerMessage(EventId = 16004, Level = LogLevel.Debug, Message = "Stream status: State={State}, IsPlaying={IsPlaying}"
)]
    private partial void LogStreamStatus(VLCState State, bool IsPlaying);

    [LoggerMessage(EventId = 16005, Level = LogLevel.Information, Message = "Stream cancelled for: {SourceUrl}"
)]
    private partial void LogStreamCancelled(string SourceUrl);

    [LoggerMessage(EventId = 16006, Level = LogLevel.Warning, Message = "Stream ended with error for: {SourceUrl}"
)]
    private partial void LogStreamEndedWithError(string SourceUrl);

    [LoggerMessage(EventId = 16007, Level = LogLevel.Information, Message = "Stream ended naturally for: {SourceUrl}"
)]
    private partial void LogStreamEndedNaturally(string SourceUrl);

    [LoggerMessage(EventId = 16008, Level = LogLevel.Information, Message = "Audio processing completed: {OutputPath}"
)]
    private partial void LogAudioProcessingCompleted(string OutputPath);

    [LoggerMessage(EventId = 16009, Level = LogLevel.Information, Message = "Audio processing was cancelled"
)]
    private partial void LogAudioProcessingWasCancelled();

    [LoggerMessage(EventId = 16010, Level = LogLevel.Error, Message = "Failed → process audio stream: {SourceUrl}"
)]
    private partial void LogFailedToProcessAudioStream(Exception ex, string SourceUrl);

    [LoggerMessage(EventId = 16011, Level = LogLevel.Debug, Message = "Built media options for pipe streaming: Codec={AudioCodec}, SampleRate={SampleRate}, Channels={Channels}, Options={Options}"
)]
    private partial void LogBuiltMediaOptions(string AudioCodec, int SampleRate, int Channels, string Options);

    [LoggerMessage(EventId = 16012, Level = LogLevel.Debug, Message = "Stopped media playback"
)]
    private partial void LogStoppedMediaPlayback();

    [LoggerMessage(EventId = 16013, Level = LogLevel.Debug, Message = "LibVLC IsPlaying Check - IsPlaying: {IsPlaying}, State: {State}"
)]
    private partial void LogLibVlcIsPlayingCheck(bool IsPlaying, VLCState State);

    [LoggerMessage(EventId = 16014, Level = LogLevel.Debug, Message = "LibVLC Direct Access - Time: {Time}ms, State: {State}, IsPlaying: {IsPlaying}"
)]
    private partial void LogLibVlcDirectAccess(long Time, VLCState State, bool IsPlaying);

    [LoggerMessage(EventId = 16015, Level = LogLevel.Information, Message = "Setting up LibVLC event handlers"
)]
    private partial void LogSettingUpLibVlcEventHandlers();

    [LoggerMessage(EventId = 16016, Level = LogLevel.Debug, Message = "[POSITION] LibVLC PositionChanged event: {Position}% = {PositionMs}ms"
)]
    private partial void LogLibVlcPositionChanged(float Position, long PositionMs);

    [LoggerMessage(EventId = 16017, Level = LogLevel.Warning, Message = "Error handling PositionChanged event"
)]
    private partial void LogErrorHandlingPositionChangedEvent(Exception ex);

    [LoggerMessage(EventId = 16018, Level = LogLevel.Debug, Message = "LibVLC TimeChanged event: {Time}ms"
)]
    private partial void LogLibVlcTimeChanged(long Time);

    [LoggerMessage(EventId = 16019, Level = LogLevel.Warning, Message = "Error handling TimeChanged event"
)]
    private partial void LogErrorHandlingTimeChangedEvent(Exception ex);

    [LoggerMessage(EventId = 16020, Level = LogLevel.Warning, Message = "Error handling Playing event"
)]
    private partial void LogErrorHandlingPlayingEvent(Exception ex);

    [LoggerMessage(EventId = 16021, Level = LogLevel.Warning, Message = "Error handling Paused event"
)]
    private partial void LogErrorHandlingPausedEvent(Exception ex);

    [LoggerMessage(EventId = 16022, Level = LogLevel.Warning, Message = "Error handling Stopped event"
)]
    private partial void LogErrorHandlingStoppedEvent(Exception ex);

    [LoggerMessage(EventId = 16023, Level = LogLevel.Warning, Message = "Error handling EndReached event"
)]
    private partial void LogErrorHandlingEndReachedEvent(Exception ex);

    [LoggerMessage(EventId = 16024, Level = LogLevel.Error, Message = "LibVLC encountered an error during playback"
)]
    private partial void LogLibVlcEncounteredError();

    [LoggerMessage(EventId = 16025, Level = LogLevel.Warning, Message = "Error handling EncounteredError event"
)]
    private partial void LogErrorHandlingEncounteredErrorEvent(Exception ex);

    [LoggerMessage(EventId = 16026, Level = LogLevel.Debug, Message = "Audio processing context disposed"
)]
    private partial void LogAudioProcessingContextDisposed();

    [LoggerMessage(EventId = 16027, Level = LogLevel.Debug, Message = "Audio processing context disposed asynchronously"
)]
    private partial void LogAudioProcessingContextDisposedAsynchronously();

    [LoggerMessage(EventId = 16028, Level = LogLevel.Warning, Message = "Error during AudioProcessingContext disposal"
)]
    private partial void LogErrorDuringDisposal(Exception ex);

    [LoggerMessage(EventId = 16029, Level = LogLevel.Information, Message = "LibVLC Media Properties - Duration: {DurationMs}ms, Seekable: {IsSeekable}, State: {State}"
)]
    private partial void LogMediaProperties(long durationMs, bool isSeekable, string state);

    [LoggerMessage(EventId = 16030, Level = LogLevel.Information, Message = "[MEDIA] LibVLC Media Format - Duration: {DurationMs}ms, Seekable: {IsSeekable}, State: {State}, Format: {ContentType}, URL: {MediaUrl}"
)]
    private partial void LogMediaPropertiesWithFormat(long durationMs, bool isSeekable, string state, string contentType, string mediaUrl);

    [LoggerMessage(EventId = 16031, Level = LogLevel.Warning, Message = "[WARNING] Position tracking failed - Position stuck at {PositionMs}ms despite {State} state. Stream may not support seeking/position tracking."
)]
    private partial void LogPositionTrackingFailed(long positionMs, string state);

    [LoggerMessage(EventId = 16032, Level = LogLevel.Information, Message = "🔍 Streaming URL Debug: {StreamUrl}"
)]
    private partial void LogStreamingUrlDebug(string streamUrl);

    [LoggerMessage(EventId = 16033, Level = LogLevel.Error, Message = "Failed → initialize LibVLCSharp Core. Ensure LibVLC native libraries are properly installed."
)]
    private partial void LogLibVLCCoreInitializationFailed(Exception ex);

    [LoggerMessage(EventId = 16034, Level = LogLevel.Error, Message = "Failed → create LibVLC instance with args: {Args}"
)]
    private partial void LogLibVLCInstanceCreationFailed(Exception ex, string args);

    [LoggerMessage(EventId = 16035, Level = LogLevel.Information, Message = "Transcoding disabled in development mode for reduced resource usage")]
    private partial void LogTranscodingDisabled();

    [LoggerMessage(EventId = 16036, Level = LogLevel.Information, Message = "Transcoding enabled: codec={AudioCodec}, channels={Channels}, samplerate={SampleRate}")]
    private partial void LogTranscodingEnabled(string AudioCodec, int Channels, int SampleRate);
}
