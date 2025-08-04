namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// SoundFlow audio engine configuration.
/// Maps environment variables with prefix: SNAPDOG_SOUNDFLOW_*
/// </summary>
public class SoundFlowConfig
{
    /// <summary>
    /// Target sample rate for audio output.
    /// Maps to: SNAPDOG_SOUNDFLOW_SAMPLE_RATE
    /// </summary>
    [Env(Key = "SAMPLE_RATE", Default = 48000)]
    public int SampleRate { get; set; } = 48000;

    /// <summary>
    /// Target bit depth for audio output.
    /// Maps to: SNAPDOG_SOUNDFLOW_BIT_DEPTH
    /// </summary>
    [Env(Key = "BIT_DEPTH", Default = 16)]
    public int BitDepth { get; set; } = 16;

    /// <summary>
    /// Target number of audio channels.
    /// Maps to: SNAPDOG_SOUNDFLOW_CHANNELS
    /// </summary>
    [Env(Key = "CHANNELS", Default = 2)]
    public int Channels { get; set; } = 2;

    /// <summary>
    /// Audio buffer size in samples.
    /// Maps to: SNAPDOG_SOUNDFLOW_BUFFER_SIZE
    /// </summary>
    [Env(Key = "BUFFER_SIZE", Default = 1024)]
    public int BufferSize { get; set; } = 1024;

    /// <summary>
    /// Maximum concurrent audio streams.
    /// Maps to: SNAPDOG_SOUNDFLOW_MAX_STREAMS
    /// </summary>
    [Env(Key = "MAX_STREAMS", Default = 10)]
    public int MaxStreams { get; set; } = 10;

    /// <summary>
    /// HTTP connection timeout in seconds.
    /// Maps to: SNAPDOG_SOUNDFLOW_HTTP_TIMEOUT_SECONDS
    /// </summary>
    [Env(Key = "HTTP_TIMEOUT_SECONDS", Default = 30)]
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable audio format auto-detection.
    /// Maps to: SNAPDOG_SOUNDFLOW_AUTO_DETECT_FORMAT
    /// </summary>
    [Env(Key = "AUTO_DETECT_FORMAT", Default = true)]
    public bool AutoDetectFormat { get; set; } = true;

    /// <summary>
    /// Audio processing thread priority.
    /// Maps to: SNAPDOG_SOUNDFLOW_THREAD_PRIORITY
    /// </summary>
    [Env(Key = "THREAD_PRIORITY", Default = "Normal")]
    public string ThreadPriority { get; set; } = "Normal";

    /// <summary>
    /// Enable real-time audio processing.
    /// Maps to: SNAPDOG_SOUNDFLOW_REALTIME_PROCESSING
    /// </summary>
    [Env(Key = "REALTIME_PROCESSING", Default = true)]
    public bool RealtimeProcessing { get; set; } = true;
}
