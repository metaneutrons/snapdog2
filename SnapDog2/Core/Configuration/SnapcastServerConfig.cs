namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// Snapcast server configuration (for container setup).
/// Maps environment variables with prefix: SNAPDOG_SNAPCAST_*
/// </summary>
public class SnapcastServerConfig
{
    /// <summary>
    /// Audio codec for Snapcast server.
    /// Maps to: SNAPDOG_SNAPCAST_CODEC
    /// </summary>
    [Env(Key = "CODEC", Default = "flac")]
    public string Codec { get; set; } = "flac";

    /// <summary>
    /// Sample format (sample rate:bit depth:channels).
    /// Maps to: SNAPDOG_SNAPCAST_SAMPLEFORMAT
    /// </summary>
    [Env(Key = "SAMPLEFORMAT", Default = "48000:16:2")]
    public string SampleFormat { get; set; } = "48000:16:2";

    /// <summary>
    /// SnapWeb HTTP port.
    /// Maps to: SNAPDOG_SNAPCAST_WEBSERVER_PORT
    /// </summary>
    [Env(Key = "WEBSERVER_PORT", Default = 1780)]
    public int WebServerPort { get; set; } = 1780;

    /// <summary>
    /// JSON-RPC WebSocket port.
    /// Maps to: SNAPDOG_SNAPCAST_WEBSOCKET_PORT
    /// </summary>
    [Env(Key = "WEBSOCKET_PORT", Default = 1705)]
    public int WebSocketPort { get; set; } = 1705;
}
