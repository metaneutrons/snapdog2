namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// Global audio configuration for the entire system.
/// Used by both Snapcast server setup and LibVLC audio processing.
/// Maps environment variables with prefix: SNAPDOG_AUDIO_*
/// </summary>
public class AudioConfig
{
    /// <summary>
    /// Audio sample rate in Hz.
    /// Maps to: SNAPDOG_AUDIO_SAMPLE_RATE
    /// Used by both Snapcast and LibVLC for consistent audio format.
    /// </summary>
    [Env(Key = "SAMPLE_RATE", Default = 48000)]
    public int SampleRate { get; set; } = 48000;

    /// <summary>
    /// Audio bit depth in bits per sample.
    /// Maps to: SNAPDOG_AUDIO_BIT_DEPTH
    /// Used by both Snapcast and LibVLC for consistent audio format.
    /// </summary>
    [Env(Key = "BIT_DEPTH", Default = 16)]
    public int BitDepth { get; set; } = 16;

    /// <summary>
    /// Number of audio channels (1=mono, 2=stereo).
    /// Maps to: SNAPDOG_AUDIO_CHANNELS
    /// Used by both Snapcast and LibVLC for consistent audio format.
    /// </summary>
    [Env(Key = "CHANNELS", Default = 2)]
    public int Channels { get; set; } = 2;

    /// <summary>
    /// Audio codec for Snapcast server.
    /// Maps to: SNAPDOG_AUDIO_CODEC
    /// </summary>
    [Env(Key = "CODEC", Default = "flac")]
    public string Codec { get; set; } = "flac";

    /// <summary>
    /// HTTP connection timeout in seconds for streaming sources.
    /// Maps to: SNAPDOG_AUDIO_HTTP_TIMEOUT_SECONDS
    /// </summary>
    [Env(Key = "HTTP_TIMEOUT_SECONDS", Default = 20)]
    public int HttpTimeoutSeconds { get; set; } = 20;

    // ═══════════════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES (Not configurable via environment variables)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Snapcast sample format string in format "SampleRate:BitDepth:Channels".
    /// Computed from SampleRate, BitDepth, and Channels.
    /// Example: "48000:16:2"
    /// </summary>
    public string SnapcastSampleFormat => $"{this.SampleRate}:{this.BitDepth}:{this.Channels}";

    /// <summary>
    /// LibVLC command line arguments (hardcoded for consistency).
    /// </summary>
    public string[] LibVLCArgs => new[] { "--no-video", "--quiet" };

    /// <summary>
    /// LibVLC output format (hardcoded as raw for Snapcast compatibility).
    /// </summary>
    public string OutputFormat => "raw";

    /// <summary>
    /// LibVLC temporary directory (hardcoded).
    /// </summary>
    public string TempDirectory => "/tmp/snapdog_audio";

    /// <summary>
    /// Thread priority for audio processing (hardcoded).
    /// </summary>
    public string ThreadPriority => "Normal";

    /// <summary>
    /// Enable audio format auto-detection (hardcoded).
    /// </summary>
    public bool AutoDetectFormat => true;

    /// <summary>
    /// Enable real-time audio processing (hardcoded).
    /// </summary>
    public bool RealtimeProcessing => true;
}
