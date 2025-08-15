namespace SnapDog2.Infrastructure.Audio;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;

/// <summary>
/// SoundFlow-based audio player for streaming to Snapcast sinks.
/// Handles HTTP audio streaming with format conversion and real-time processing.
/// </summary>
public sealed partial class MediaPlayer : IAsyncDisposable
{
    private readonly SoundFlowConfig _config;
    private readonly ILogger<MediaPlayer> _logger;
    private readonly int _zoneIndex;
    private readonly string _sinkPath;
    private readonly HttpClient _httpClient;

    // SoundFlow components (placeholder interfaces until SoundFlow package is available)
    private ISoundDevice? _device;
    private IAudioGraph? _audioGraph;
    private IHttpStreamSource? _streamSource;
    private IResampleProcessor? _resampler;
    private IFileOutputSink? _outputSink;

    private CancellationTokenSource? _streamingCts;
    private TrackInfo? _currentTrack;
    private DateTime? _playbackStartedAt;
    private bool _disposed;

    public MediaPlayer(
        SoundFlowConfig config,
        ILogger<MediaPlayer> logger,
        int zoneIndex,
        string sinkPath,
        HttpClient httpClient
    )
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._zoneIndex = zoneIndex;
        this._sinkPath = sinkPath ?? throw new ArgumentNullException(nameof(sinkPath));
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Gets the current playback status.
    /// </summary>
    public PlaybackStatus GetStatus()
    {
        return new PlaybackStatus
        {
            ZoneIndex = this._zoneIndex,
            IsPlaying = this._audioGraph != null && !this._disposed,
            CurrentTrack = this._currentTrack,
            AudioFormat = new AudioFormat(this._config.SampleRate, this._config.BitDepth, this._config.Channels),
            PlaybackStartedAt = this._playbackStartedAt,
            ActiveStreams = this._audioGraph != null ? 1 : 0,
            MaxStreams = this._config.MaxStreams,
        };
    }

    /// <summary>
    /// Starts streaming audio from the specified URL to the sink.
    /// </summary>
    public async Task<Result> StartStreamingAsync(
        string audioUrl,
        TrackInfo trackInfo,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ObjectDisposedException.ThrowIf(this._disposed, this);

            if (string.IsNullOrWhiteSpace(audioUrl))
                return Result.Failure(new ArgumentException("Audio URL cannot be null or empty", nameof(audioUrl)));

            // Stop any existing stream
            await this.StopStreamingAsync();

            // Store track information
            this._currentTrack = trackInfo;
            this._playbackStartedAt = DateTime.UtcNow;

            // Create cancellation token source
            this._streamingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Initialize SoundFlow components
            await this.InitializeSoundFlowAsync(audioUrl);

            // Start the audio graph
            await this._audioGraph!.StartAsync(this._streamingCts.Token);

            LogStreamingStarted(this._logger, this._zoneIndex, audioUrl, this._sinkPath, trackInfo.Title);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogStreamingError(this._logger, this._zoneIndex, ex);
            await this.CleanupResourcesAsync();
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Stops the current audio stream.
    /// </summary>
    public async Task StopStreamingAsync()
    {
        if (this._streamingCts != null)
        {
            this._streamingCts.Cancel();
        }

        if (this._audioGraph != null)
        {
            await this._audioGraph.StopAsync();
        }

        await this.CleanupResourcesAsync();

        this._currentTrack = null;
        this._playbackStartedAt = null;

        LogStreamingStopped(this._logger, this._zoneIndex);
    }

    /// <summary>
    /// Initializes SoundFlow audio processing pipeline.
    /// </summary>
    private async Task InitializeSoundFlowAsync(string audioUrl)
    {
        // Create target audio format
        var targetFormat = new AudioFormat(this._config.SampleRate, this._config.BitDepth, this._config.Channels);

        // Initialize virtual audio device for file output
        this._device = await this.CreateVirtualDeviceAsync(targetFormat);

        // Create audio graph
        this._audioGraph = this.CreateAudioGraph(this._device);

        // Create HTTP stream source
        this._streamSource = this.CreateHttpStreamSource(audioUrl);

        // Create resampler for format conversion
        this._resampler = this.CreateResampleProcessor(targetFormat);

        // Create file output sink
        this._outputSink = this.CreateFileOutputSink(this._sinkPath, targetFormat);

        // Build audio processing pipeline: Source → Processor → Sink
        this._audioGraph.AddSource(this._streamSource);
        this._audioGraph.AddProcessor(this._resampler);
        this._audioGraph.AddSink(this._outputSink);

        // Configure audio graph settings
        this.ConfigureAudioGraph(this._audioGraph);

        LogAudioGraphInitialized(this._logger, this._zoneIndex, audioUrl, targetFormat.ToString());
    }

    /// <summary>
    /// Creates a virtual audio device for file output.
    /// </summary>
    private async Task<ISoundDevice> CreateVirtualDeviceAsync(AudioFormat format)
    {
        // Placeholder implementation - will use actual SoundFlow API
        return await Task.FromResult(new VirtualSoundDevice(format, this._config.BufferSize));
    }

    /// <summary>
    /// Creates an audio graph for processing pipeline.
    /// </summary>
    private IAudioGraph CreateAudioGraph(ISoundDevice device)
    {
        return new AudioGraph(device);
    }

    /// <summary>
    /// Creates HTTP stream source for audio input.
    /// </summary>
    private IHttpStreamSource CreateHttpStreamSource(string audioUrl)
    {
        return new HttpStreamSource(this._httpClient)
        {
            Url = audioUrl,
            AutoDetectFormat = this._config.AutoDetectFormat,
            TimeoutSeconds = this._config.HttpTimeoutSeconds,
        };
    }

    /// <summary>
    /// Creates resampler for audio format conversion.
    /// </summary>
    private IResampleProcessor CreateResampleProcessor(AudioFormat targetFormat)
    {
        return new ResampleProcessor(targetFormat)
        {
            Quality = ResampleQuality.High,
            RealtimeProcessing = this._config.RealtimeProcessing,
        };
    }

    /// <summary>
    /// Creates file output sink for writing to Snapcast sink.
    /// </summary>
    private IFileOutputSink CreateFileOutputSink(string sinkPath, AudioFormat format)
    {
        return new FileOutputSink(sinkPath)
        {
            Format = format,
            BufferSize = this._config.BufferSize,
            FlushInterval = TimeSpan.FromMilliseconds(50), // Low latency for real-time streaming
        };
    }

    /// <summary>
    /// Configures audio graph settings.
    /// </summary>
    private void ConfigureAudioGraph(IAudioGraph audioGraph)
    {
        audioGraph.RealtimeProcessing = this._config.RealtimeProcessing;
        audioGraph.ThreadPriority = this.ParseThreadPriority(this._config.ThreadPriority);
    }

    /// <summary>
    /// Cleans up SoundFlow resources.
    /// </summary>
    private async Task CleanupResourcesAsync()
    {
        // Cancel any ongoing streaming first
        if (this._streamingCts != null)
        {
            try
            {
                this._streamingCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
        }

        // Dispose resources in reverse order of creation to avoid dependencies
        if (this._outputSink != null)
        {
            try
            {
                await this._outputSink.FlushAsync();
                await this._outputSink.DisposeAsync();
            }
            catch (Exception ex)
            {
                // Log but don't throw during cleanup
                LogCleanupError(this._logger, this._zoneIndex, "OutputSink", ex);
            }
            finally
            {
                this._outputSink = null;
            }
        }

        if (this._resampler != null)
        {
            try
            {
                await this._resampler.DisposeAsync();
            }
            catch (Exception ex)
            {
                LogCleanupError(this._logger, this._zoneIndex, "Resampler", ex);
            }
            finally
            {
                this._resampler = null;
            }
        }

        if (this._streamSource != null)
        {
            try
            {
                await this._streamSource.DisposeAsync();
            }
            catch (Exception ex)
            {
                LogCleanupError(this._logger, this._zoneIndex, "StreamSource", ex);
            }
            finally
            {
                this._streamSource = null;
            }
        }

        if (this._audioGraph != null)
        {
            try
            {
                await this._audioGraph.DisposeAsync();
            }
            catch (Exception ex)
            {
                LogCleanupError(this._logger, this._zoneIndex, "AudioGraph", ex);
            }
            finally
            {
                this._audioGraph = null;
            }
        }

        if (this._device != null)
        {
            try
            {
                await this._device.DisposeAsync();
            }
            catch (Exception ex)
            {
                LogCleanupError(this._logger, this._zoneIndex, "Device", ex);
            }
            finally
            {
                this._device = null;
            }
        }

        if (this._streamingCts != null)
        {
            try
            {
                this._streamingCts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
            finally
            {
                this._streamingCts = null;
            }
        }
    }

    /// <summary>
    /// Parses thread priority string to enum.
    /// </summary>
    private ThreadPriority ParseThreadPriority(string priority)
    {
        return priority?.ToLowerInvariant() switch
        {
            "lowest" => ThreadPriority.Lowest,
            "belownormal" => ThreadPriority.BelowNormal,
            "normal" => ThreadPriority.Normal,
            "abovenormal" => ThreadPriority.AboveNormal,
            "highest" => ThreadPriority.Highest,
            _ => ThreadPriority.Normal,
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (!this._disposed)
        {
            try
            {
                await this.StopStreamingAsync();
            }
            catch (Exception ex)
            {
                // Log but don't throw during disposal
                LogCleanupError(this._logger, this._zoneIndex, "StopStreaming", ex);
            }
            finally
            {
                this._disposed = true;
            }
        }
    }

    // Logger messages
    [LoggerMessage(
        EventId = 901,
        Level = LogLevel.Information,
        Message = "[SoundFlow] Started streaming for zone {ZoneIndex} from {AudioUrl} to {SinkPath} - Track: {TrackTitle}"
    )]
    private static partial void LogStreamingStarted(
        ILogger logger,
        int zoneIndex,
        string audioUrl,
        string sinkPath,
        string trackTitle
    );

    [LoggerMessage(
        EventId = 902,
        Level = LogLevel.Information,
        Message = "[SoundFlow] Stopped streaming for zone {ZoneIndex}"
    )]
    private static partial void LogStreamingStopped(ILogger logger, int zoneIndex);

    [LoggerMessage(EventId = 903, Level = LogLevel.Error, Message = "[SoundFlow] Streaming error for zone {ZoneIndex}")]
    private static partial void LogStreamingError(ILogger logger, int zoneIndex, Exception exception);

    [LoggerMessage(
        EventId = 904,
        Level = LogLevel.Debug,
        Message = "[SoundFlow] Audio graph initialized for zone {ZoneIndex} - URL: {AudioUrl}, Format: {AudioFormat}"
    )]
    private static partial void LogAudioGraphInitialized(
        ILogger logger,
        int zoneIndex,
        string audioUrl,
        string audioFormat
    );

    [LoggerMessage(
        EventId = 905,
        Level = LogLevel.Warning,
        Message = "[SoundFlow] Error cleaning up {ComponentName} for zone {ZoneIndex}"
    )]
    private static partial void LogCleanupError(
        ILogger logger,
        int zoneIndex,
        string componentName,
        Exception exception
    );
}

// Placeholder interfaces and classes for SoundFlow components
// These will be replaced with actual SoundFlow types when the package is integrated

internal interface ISoundDevice : IAsyncDisposable { }

internal interface IAudioGraph : IAsyncDisposable
{
    bool RealtimeProcessing { get; set; }
    ThreadPriority ThreadPriority { get; set; }

    void AddSource(IHttpStreamSource source);
    void AddProcessor(IResampleProcessor processor);
    void AddSink(IFileOutputSink sink);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync();
}

internal interface IHttpStreamSource : IAsyncDisposable
{
    string Url { get; set; }
    bool AutoDetectFormat { get; set; }
    int TimeoutSeconds { get; set; }
}

internal interface IResampleProcessor : IAsyncDisposable
{
    AudioFormat TargetFormat { get; }
    ResampleQuality Quality { get; set; }
    bool RealtimeProcessing { get; set; }
}

internal interface IFileOutputSink : IAsyncDisposable
{
    AudioFormat Format { get; set; }
    int BufferSize { get; set; }
    TimeSpan FlushInterval { get; set; }

    Task FlushAsync();
}

internal enum ResampleQuality
{
    Low,
    Medium,
    High,
}

// Placeholder implementations - optimized for test scenarios
internal class VirtualSoundDevice : ISoundDevice
{
    private volatile bool _disposed;

    public VirtualSoundDevice(AudioFormat format, int bufferSize)
    {
        // Placeholder constructor - store parameters for potential debugging
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        // Use Task.Yield to ensure we don't block synchronously
        await Task.Yield();
    }
}

internal class AudioGraph : IAudioGraph
{
    private volatile bool _disposed;
    private volatile bool _started;

    public AudioGraph(ISoundDevice device)
    {
        // Placeholder constructor
    }

    public bool RealtimeProcessing { get; set; }
    public ThreadPriority ThreadPriority { get; set; }

    public void AddSource(IHttpStreamSource source)
    {
        if (_disposed)
            return;
        // Placeholder - no-op
    }

    public void AddProcessor(IResampleProcessor processor)
    {
        if (_disposed)
            return;
        // Placeholder - no-op
    }

    public void AddSink(IFileOutputSink sink)
    {
        if (_disposed)
            return;
        // Placeholder - no-op
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
            return;
        _started = true;
        await Task.Yield();
    }

    public async Task StopAsync()
    {
        if (_disposed)
            return;
        _started = false;
        await Task.Yield();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_started)
        {
            await StopAsync();
        }

        await Task.Yield();
    }
}

internal class HttpStreamSource : IHttpStreamSource
{
    private volatile bool _disposed;

    public HttpStreamSource(HttpClient httpClient)
    {
        // Placeholder constructor - don't store HttpClient to avoid disposal issues
    }

    public string Url { get; set; } = string.Empty;
    public bool AutoDetectFormat { get; set; }
    public int TimeoutSeconds { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        await Task.Yield();
    }
}

internal class ResampleProcessor : IResampleProcessor
{
    private volatile bool _disposed;

    public ResampleProcessor(AudioFormat targetFormat)
    {
        this.TargetFormat = targetFormat;
    }

    public AudioFormat TargetFormat { get; }
    public ResampleQuality Quality { get; set; }
    public bool RealtimeProcessing { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        await Task.Yield();
    }
}

internal class FileOutputSink : IFileOutputSink
{
    private volatile bool _disposed;

    public FileOutputSink(string filePath)
    {
        // Placeholder constructor - don't create actual files in tests
    }

    public AudioFormat Format { get; set; } = new(48000, 16, 2);
    public int BufferSize { get; set; }
    public TimeSpan FlushInterval { get; set; }

    public async Task FlushAsync()
    {
        if (_disposed)
            return;
        // Placeholder flush - just yield to simulate async work
        await Task.Yield();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        // Ensure flush is called before disposal
        await FlushAsync();
        await Task.Yield();
    }
}
