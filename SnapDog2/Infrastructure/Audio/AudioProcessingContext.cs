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
    private readonly MediaPlayer _mediaPlayer;
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
        _libvlc = new LibVLC(args);
        _mediaPlayer = new MediaPlayer(_libvlc);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var tempDir = tempDirectory ?? config.TempDirectory;
        _tempDirectory = Directory.CreateDirectory(tempDir);

        Config = new AudioProcessingConfig
        {
            SampleRate = config.SampleRate,
            BitsPerSample = config.BitDepth,
            Channels = config.Channels,
            Format = config.OutputFormat,
        };

        MetadataManager = new MetadataManager(_libvlc, metadataLogger);

        _logger.LogDebug(
            "Audio processing context initialized with temp directory: {TempDirectory}",
            _tempDirectory.FullName
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
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AudioProcessingContext));
        }

        try
        {
            var sourceId = Guid.NewGuid().ToString("N");
            var outputPath = Path.Combine(_tempDirectory.FullName, $"{sourceId}.{Config.Format}");
            var metadataPath = Path.ChangeExtension(outputPath, ".json");

            _logger.LogInformation(
                "Starting audio processing: Source={SourceUrl}, Output={OutputPath}",
                sourceUrl,
                outputPath
            );

            // Build media options for raw audio output
            var mediaOptions = BuildMediaOptions(outputPath);

            using var media = new Media(_libvlc, sourceUrl, FromType.FromLocation, mediaOptions.ToArray());

            // Extract metadata before starting playback
            var metadata = await MetadataManager.ExtractMetadataAsync(media, cancellationToken);

            // Set up the media player
            _mediaPlayer.Media = media;

            // Start playback (which will write to the output file)
            var playResult = _mediaPlayer.Play();
            if (!playResult)
            {
                return new AudioProcessingResult { Success = false, ErrorMessage = "Failed to start media playback" };
            }

            // Wait for playback to start
            while (_mediaPlayer.State == VLCState.Opening || _mediaPlayer.State == VLCState.Buffering)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _mediaPlayer.Stop();
                    return new AudioProcessingResult { Success = false, ErrorMessage = "Operation was cancelled" };
                }

                await Task.Delay(100, cancellationToken);
            }

            // Check if playback started successfully
            if (_mediaPlayer.State == VLCState.Error)
            {
                return new AudioProcessingResult { Success = false, ErrorMessage = "Media playback failed" };
            }

            // Save metadata to JSON file
            await MetadataManager.SaveMetadataAsync(metadata, metadataPath, cancellationToken);

            _logger.LogInformation("Audio processing completed successfully: {OutputPath}", outputPath);

            return new AudioProcessingResult
            {
                Success = true,
                OutputFilePath = outputPath,
                MetadataPath = metadataPath,
                SourceId = sourceId,
                Config = Config,
                Metadata = metadata,
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Audio processing was cancelled");
            return new AudioProcessingResult { Success = false, ErrorMessage = "Operation was cancelled" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process audio stream: {SourceUrl}", sourceUrl);
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
            + $"acodec=s{Config.BitsPerSample}l,"
            + $"ab={Config.SampleRate * Config.Channels * Config.BitsPerSample / 8},"
            + $"channels={Config.Channels},"
            + $"samplerate={Config.SampleRate}"
            + $"}}";

        // Set up file output
        var fileOutput = $"file{{dst={outputPath}}}";

        // Combine transcode and output
        options.Add($":sout={transcode}:{fileOutput}");

        // Additional options for better streaming
        options.Add(":no-sout-video");
        options.Add(":sout-keep");

        _logger.LogDebug("Built media options: {Options}", string.Join(", ", options));

        return options;
    }

    /// <summary>
    /// Stops any current playback.
    /// </summary>
    public void Stop()
    {
        if (!_disposed && _mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Stop();
            _logger.LogDebug("Stopped media playback");
        }
    }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    public VLCState State => _mediaPlayer.State;

    /// <summary>
    /// Gets whether media is currently playing.
    /// </summary>
    public bool IsPlaying => _mediaPlayer.IsPlaying;

    public void Dispose()
    {
        if (!_disposed)
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libvlc?.Dispose();
            _disposed = true;

            _logger.LogDebug("Audio processing context disposed");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libvlc?.Dispose();
            _disposed = true;

            _logger.LogDebug("Audio processing context disposed asynchronously");
        }

        await Task.CompletedTask;
    }
}
