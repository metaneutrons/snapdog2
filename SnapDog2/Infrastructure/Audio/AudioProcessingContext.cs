namespace SnapDog2.Infrastructure.Audio;

using System.Text.Json;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;

/// <summary>
/// Audio processing context using LibVLC for streaming and metadata extraction.
/// </summary>
public sealed class AudioProcessingContext : IAsyncDisposable, IDisposable
{
    private readonly LibVLC _libvlc;
    private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
    private readonly ILogger _logger;
    private readonly DirectoryInfo _tempDirectory;
    private bool _disposed;

    public AudioProcessingConfig Config { get; }
    public MetadataManager MetadataManager { get; }

    public AudioProcessingContext(
        SnapDog2.Core.Configuration.AudioConfig config,
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
            logger.LogError(
                ex,
                "Failed to initialize LibVLCSharp Core. Ensure LibVLC native libraries are properly installed."
            );
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create LibVLC instance with args: {Args}", string.Join(" ", args));
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

        this.MetadataManager = new MetadataManager(this._libvlc, metadataLogger);

        this._logger.LogDebug(
            "Audio processing context initialized with temp directory: {TempDirectory}",
            this._tempDirectory.FullName
        );
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

            this._logger.LogInformation(
                "Starting audio processing: Source={SourceUrl}, Output={OutputPath}",
                sourceUrl,
                finalOutputPath
            );

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

            // Save metadata to JSON file
            await this.MetadataManager.SaveMetadataAsync(metadata, metadataPath, cancellationToken);

            this._logger.LogInformation("Audio streaming started successfully: {OutputPath}", finalOutputPath);

            // For streaming sources (like radio), keep the stream alive until cancelled
            if (
                sourceType == AudioSourceType.Url
                && (sourceUrl.StartsWith("http://") || sourceUrl.StartsWith("https://"))
            )
            {
                this._logger.LogInformation("Maintaining continuous stream for URL source: {SourceUrl}", sourceUrl);

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
                        this._logger.LogDebug(
                            "Stream status: State={State}, IsPlaying={IsPlaying}",
                            this._mediaPlayer.State,
                            this._mediaPlayer.IsPlaying
                        );
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    this._logger.LogInformation("Stream cancelled for: {SourceUrl}", sourceUrl);
                }
                else if (this._mediaPlayer.State == VLCState.Error)
                {
                    this._logger.LogWarning("Stream ended with error for: {SourceUrl}", sourceUrl);
                }
                else
                {
                    this._logger.LogInformation("Stream ended naturally for: {SourceUrl}", sourceUrl);
                }
            }

            this._logger.LogInformation("Audio processing completed: {OutputPath}", finalOutputPath);

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
            this._logger.LogInformation("Audio processing was cancelled");
            return new AudioProcessingResult { Success = false, ErrorMessage = "Operation was cancelled" };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to process audio stream: {SourceUrl}", sourceUrl);
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
        var audioCodec = this.GetLibVLCAudioCodec(this.Config.BitsPerSample);

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

        this._logger.LogDebug(
            "Built media options for pipe streaming: Codec={AudioCodec}, SampleRate={SampleRate}, Channels={Channels}, Options={Options}",
            audioCodec,
            this.Config.SampleRate,
            this.Config.Channels,
            string.Join(", ", options)
        );

        return options;
    }

    /// <summary>
    /// Gets the appropriate LibVLC audio codec string based on bit depth.
    /// Maps bit depth to LibVLC PCM codec format.
    /// </summary>
    /// <param name="bitsPerSample">Bit depth from configuration.</param>
    /// <returns>LibVLC audio codec string.</returns>
    private string GetLibVLCAudioCodec(int bitsPerSample)
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
            this._logger.LogDebug("Stopped media playback");
        }
    }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    public VLCState State => this._mediaPlayer.State;

    /// <summary>
    /// Gets whether media is currently playing.
    /// </summary>
    public bool IsPlaying => this._mediaPlayer.IsPlaying;

    public void Dispose()
    {
        if (!this._disposed)
        {
            this._mediaPlayer?.Stop();
            this._mediaPlayer?.Dispose();
            this._libvlc?.Dispose();
            this._disposed = true;

            this._logger.LogDebug("Audio processing context disposed");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!this._disposed)
        {
            this._mediaPlayer?.Stop();
            this._mediaPlayer?.Dispose();
            this._libvlc?.Dispose();
            this._disposed = true;

            this._logger.LogDebug("Audio processing context disposed asynchronously");
        }

        await Task.CompletedTask;
    }
}
