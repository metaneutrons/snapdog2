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
    private bool _disposed;

    // Event for position changes
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;
    public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;

    public AudioProcessingConfig Config { get; }
    public MetadataManager MetadataManager { get; }

    public AudioProcessingContext(
        AudioConfig config,
        ILogger logger,
        ILogger<MetadataManager> metadataLogger,
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

        var args = config.LibVLCArgs;
        try
        {
            this._libvlc = new LibVLC(args);
            this._mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(this._libvlc);

            // Subscribe to LibVLC events for real-time position updates
            // Temporarily disabled to isolate LibVLC issue
            // this.SetupEventHandlers();
        }
        catch (Exception ex)
        {
            this.LogLibVLCInstanceCreationFailed(ex, string.Join(" ", args));
            throw new InvalidOperationException(
                "LibVLC instance creation failed. Check LibVLC installation and arguments.",
                ex
            );
        }

        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                return new AudioProcessingResult { Success = false, ErrorMessage = "Failed to start media playback" };
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
                        ErrorMessage = "Timeout waiting for playback to start",
                    };
                }

                await Task.Delay(100, cancellationToken);
            }

            // Check if playback started successfully
            if (this._mediaPlayer.State == VLCState.Error)
            {
                return new AudioProcessingResult { Success = false, ErrorMessage = "Media playback failed" };
            }

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

        // Generate the appropriate LibVLC audio codec based on bit depth
        var audioCodec = GetLibVLCAudioCodec(this.Config.BitsPerSample);

        // For named pipes (Snapcast sinks), we need to use raw audio output
        // Use transcode with dynamic audio codec based on configuration
        var transcode =
            $"#transcode{{"
            + $"acodec={audioCodec}," // Dynamic codec based on bit depth
            + $"channels={this.Config.Channels},"
            + $"samplerate={this.Config.SampleRate}"
            + $"}}";

        // Use standard output with raw mux for named pipes
        var standardOutput = $"standard{{access=file,mux=raw,dst={outputPath}}}";

        // Combine transcode and output
        options.Add($":sout={transcode}:{standardOutput}");

        // Critical options for streaming to named pipes
        options.Add(":no-sout-video");
        options.Add(":sout-keep");
        options.Add(":no-sout-rtp-sap");
        options.Add(":no-sout-standard-sap");
        options.Add(":sout-all"); // Keep streaming even if no one is reading

        this.LogBuiltMediaOptions(audioCodec, this.Config.SampleRate, this.Config.Channels, string.Join(", ", options));

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
    public long DurationMs => this._mediaPlayer.Length;

    /// <summary>
    /// Sets up LibVLC event handlers for real-time position and state updates.
    /// </summary>
    private void SetupEventHandlers()
    {
        // TODO: Why is this method never called? Investigate LibVLC issue.
        this.LogSettingUpLibVlcEventHandlers();

        // Position change events (percentage-based)
        this._mediaPlayer.PositionChanged += (_, e) =>
        {
            try
            {
                var positionMs = (long)(e.Position * this._mediaPlayer.Length);
                this.LogLibVlcPositionChanged(e.Position, positionMs);

                this.PositionChanged?.Invoke(
                    this,
                    new PositionChangedEventArgs
                    {
                        PositionMs = positionMs,
                        Progress = e.Position,
                        DurationMs = this._mediaPlayer.Length,
                    }
                );
            }
            catch (Exception ex)
            {
                this.LogErrorHandlingPositionChangedEvent(ex);
            }
        };

        // Also keep TimeChanged as backup
        this._mediaPlayer.TimeChanged += (_, e) =>
        {
            try
            {
                this.LogLibVlcTimeChanged(e.Time);
                this.PositionChanged?.Invoke(
                    this,
                    new PositionChangedEventArgs
                    {
                        PositionMs = e.Time,
                        Progress = this._mediaPlayer.Position,
                        DurationMs = this._mediaPlayer.Length,
                    }
                );
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
    }

    public void Dispose()
    {
        if (!this._disposed)
        {
            this._mediaPlayer.Stop();
            this._mediaPlayer.Dispose();
            this._libvlc.Dispose();
            this._disposed = true;

            this.LogAudioProcessingContextDisposed();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!this._disposed)
        {
            this._mediaPlayer.Stop();
            this._mediaPlayer.Dispose();
            this._libvlc.Dispose();
            this._disposed = true;

            this.LogAudioProcessingContextDisposedAsynchronously();
        }

        await Task.CompletedTask;
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(
        EventId = 2100,
        Level = LogLevel.Debug,
        Message = "Audio processing context initialized with temp directory: {TempDirectory}"
    )]
    private partial void LogAudioProcessingContextInitialized(string TempDirectory);

    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Information,
        Message = "Starting audio processing: Source={SourceUrl}, Output={OutputPath}"
    )]
    private partial void LogStartingAudioProcessing(string SourceUrl, string OutputPath);

    [LoggerMessage(
        EventId = 2102,
        Level = LogLevel.Information,
        Message = "Audio streaming started successfully: {OutputPath}"
    )]
    private partial void LogAudioStreamingStartedSuccessfully(string OutputPath);

    [LoggerMessage(
        EventId = 2103,
        Level = LogLevel.Information,
        Message = "Maintaining continuous stream for URL source: {SourceUrl}"
    )]
    private partial void LogMaintainingContinuousStream(string SourceUrl);

    [LoggerMessage(
        EventId = 2104,
        Level = LogLevel.Debug,
        Message = "Stream status: State={State}, IsPlaying={IsPlaying}"
    )]
    private partial void LogStreamStatus(VLCState State, bool IsPlaying);

    [LoggerMessage(
        EventId = 2105,
        Level = LogLevel.Information,
        Message = "Stream cancelled for: {SourceUrl}"
    )]
    private partial void LogStreamCancelled(string SourceUrl);

    [LoggerMessage(
        EventId = 2106,
        Level = LogLevel.Warning,
        Message = "Stream ended with error for: {SourceUrl}"
    )]
    private partial void LogStreamEndedWithError(string SourceUrl);

    [LoggerMessage(
        EventId = 2107,
        Level = LogLevel.Information,
        Message = "Stream ended naturally for: {SourceUrl}"
    )]
    private partial void LogStreamEndedNaturally(string SourceUrl);

    [LoggerMessage(
        EventId = 2108,
        Level = LogLevel.Information,
        Message = "Audio processing completed: {OutputPath}"
    )]
    private partial void LogAudioProcessingCompleted(string OutputPath);

    [LoggerMessage(
        EventId = 2109,
        Level = LogLevel.Information,
        Message = "Audio processing was cancelled"
    )]
    private partial void LogAudioProcessingWasCancelled();

    [LoggerMessage(
        EventId = 2110,
        Level = LogLevel.Error,
        Message = "Failed to process audio stream: {SourceUrl}"
    )]
    private partial void LogFailedToProcessAudioStream(Exception ex, string SourceUrl);

    [LoggerMessage(
        EventId = 2111,
        Level = LogLevel.Debug,
        Message = "Built media options for pipe streaming: Codec={AudioCodec}, SampleRate={SampleRate}, Channels={Channels}, Options={Options}"
    )]
    private partial void LogBuiltMediaOptions(string AudioCodec, int SampleRate, int Channels, string Options);

    [LoggerMessage(
        EventId = 2112,
        Level = LogLevel.Debug,
        Message = "Stopped media playback"
    )]
    private partial void LogStoppedMediaPlayback();

    [LoggerMessage(
        EventId = 2113,
        Level = LogLevel.Debug,
        Message = "LibVLC IsPlaying Check - IsPlaying: {IsPlaying}, State: {State}"
    )]
    private partial void LogLibVlcIsPlayingCheck(bool IsPlaying, VLCState State);

    [LoggerMessage(
        EventId = 2114,
        Level = LogLevel.Debug,
        Message = "LibVLC Direct Access - Time: {Time}ms, State: {State}, IsPlaying: {IsPlaying}"
    )]
    private partial void LogLibVlcDirectAccess(long Time, VLCState State, bool IsPlaying);

    [LoggerMessage(
        EventId = 2115,
        Level = LogLevel.Information,
        Message = "🔧 Setting up LibVLC event handlers"
    )]
    private partial void LogSettingUpLibVlcEventHandlers();

    [LoggerMessage(
        EventId = 2116,
        Level = LogLevel.Debug,
        Message = "📍 LibVLC PositionChanged event: {Position}% = {PositionMs}ms"
    )]
    private partial void LogLibVlcPositionChanged(float Position, long PositionMs);

    [LoggerMessage(
        EventId = 2117,
        Level = LogLevel.Warning,
        Message = "Error handling PositionChanged event"
    )]
    private partial void LogErrorHandlingPositionChangedEvent(Exception ex);

    [LoggerMessage(
        EventId = 2118,
        Level = LogLevel.Debug,
        Message = "⏰ LibVLC TimeChanged event: {Time}ms"
    )]
    private partial void LogLibVlcTimeChanged(long Time);

    [LoggerMessage(
        EventId = 2119,
        Level = LogLevel.Warning,
        Message = "Error handling TimeChanged event"
    )]
    private partial void LogErrorHandlingTimeChangedEvent(Exception ex);

    [LoggerMessage(
        EventId = 2120,
        Level = LogLevel.Warning,
        Message = "Error handling Playing event"
    )]
    private partial void LogErrorHandlingPlayingEvent(Exception ex);

    [LoggerMessage(
        EventId = 2121,
        Level = LogLevel.Warning,
        Message = "Error handling Paused event"
    )]
    private partial void LogErrorHandlingPausedEvent(Exception ex);

    [LoggerMessage(
        EventId = 2122,
        Level = LogLevel.Warning,
        Message = "Error handling Stopped event"
    )]
    private partial void LogErrorHandlingStoppedEvent(Exception ex);

    [LoggerMessage(
        EventId = 2123,
        Level = LogLevel.Warning,
        Message = "Error handling EndReached event"
    )]
    private partial void LogErrorHandlingEndReachedEvent(Exception ex);

    [LoggerMessage(
        EventId = 2124,
        Level = LogLevel.Debug,
        Message = "Audio processing context disposed"
    )]
    private partial void LogAudioProcessingContextDisposed();

    [LoggerMessage(
        EventId = 2125,
        Level = LogLevel.Debug,
        Message = "Audio processing context disposed asynchronously"
    )]
    private partial void LogAudioProcessingContextDisposedAsynchronously();

    [LoggerMessage(
        EventId = 2126,
        Level = LogLevel.Error,
        Message = "Failed to initialize LibVLCSharp Core. Ensure LibVLC native libraries are properly installed."
    )]
    private partial void LogLibVLCCoreInitializationFailed(Exception ex);

    [LoggerMessage(
        EventId = 2127,
        Level = LogLevel.Error,
        Message = "Failed to create LibVLC instance with args: {Args}"
    )]
    private partial void LogLibVLCInstanceCreationFailed(Exception ex, string args);
}
