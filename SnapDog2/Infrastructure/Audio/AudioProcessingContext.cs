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
        var args = config.LibVLCArgs;
        this._libvlc = new LibVLC(args);
        this._mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(this._libvlc);
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
    /// </summary>
    /// <param name="sourceUrl">The source URL to process.</param>
    /// <param name="sourceType">The type of audio source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audio processing result with output file and metadata.</returns>
    public async Task<AudioProcessingResult> ProcessAudioStreamAsync(
        string sourceUrl,
        AudioSourceType sourceType = AudioSourceType.Url,
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
            var outputPath = Path.Combine(this._tempDirectory.FullName, $"{sourceId}.{this.Config.Format}");
            var metadataPath = Path.ChangeExtension(outputPath, ".json");

            this._logger.LogInformation(
                "Starting audio processing: Source={SourceUrl}, Output={OutputPath}",
                sourceUrl,
                outputPath
            );

            // Build media options for raw audio output
            var mediaOptions = this.BuildMediaOptions(outputPath);

            using var media = new Media(this._libvlc, sourceUrl, FromType.FromLocation, mediaOptions.ToArray());

            // Extract metadata before starting playback
            var metadata = await this.MetadataManager.ExtractMetadataAsync(media, cancellationToken);

            // Set up the media player
            this._mediaPlayer.Media = media;

            // Start playback (which will write to the output file)
            var playResult = this._mediaPlayer.Play();
            if (!playResult)
            {
                return new AudioProcessingResult { Success = false, ErrorMessage = "Failed to start media playback" };
            }

            // Wait for playback to start
            while (this._mediaPlayer.State == VLCState.Opening || this._mediaPlayer.State == VLCState.Buffering)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    this._mediaPlayer.Stop();
                    return new AudioProcessingResult { Success = false, ErrorMessage = "Operation was cancelled" };
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

            this._logger.LogInformation("Audio processing completed successfully: {OutputPath}", outputPath);

            return new AudioProcessingResult
            {
                Success = true,
                OutputFilePath = outputPath,
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
    /// Builds LibVLC media options for raw audio output.
    /// </summary>
    /// <param name="outputPath">The output file path.</param>
    /// <returns>List of media options.</returns>
    private List<string> BuildMediaOptions(string outputPath)
    {
        var options = new List<string>();

        // Set up transcoding to raw audio format
        var transcode =
            $"#transcode{{"
            + $"acodec=s{this.Config.BitsPerSample}l,"
            + $"ab={this.Config.SampleRate * this.Config.Channels * this.Config.BitsPerSample / 8},"
            + $"channels={this.Config.Channels},"
            + $"samplerate={this.Config.SampleRate}"
            + $"}}";

        // Set up file output
        var fileOutput = $"file{{dst={outputPath}}}";

        // Combine transcode and output
        options.Add($":sout={transcode}:{fileOutput}");

        // Additional options for better streaming
        options.Add(":no-sout-video");
        options.Add(":sout-keep");

        this._logger.LogDebug("Built media options: {Options}", string.Join(", ", options));

        return options;
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
