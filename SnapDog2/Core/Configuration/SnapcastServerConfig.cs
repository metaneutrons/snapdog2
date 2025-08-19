namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// Snapcast server configuration (for container setup).
/// Maps environment variables with prefix: SNAPDOG_SNAPCAST_*
/// </summary>
public class SnapcastServerConfig
{
    /// <summary>
    /// SnapWeb HTTP port.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_HTTP_PORT
    /// </summary>
    [Env(Key = "WEBSERVER_PORT", Default = 1780)]
    public int WebServerPort { get; set; } = 1780;

    /// <summary>
    /// JSON-RPC WebSocket port.
    /// Maps to: SNAPDOG_SERVICES_SNAPCAST_WEBSOCKET_PORT
    /// </summary>
    [Env(Key = "WEBSOCKET_PORT", Default = 1704)]
    public int WebSocketPort { get; set; } = 1704;

    /// <summary>
    /// JSON-RPC API port.
    /// Maps to: SNAPDOG_SNAPCAST_JSONRPC_PORT
    /// </summary>
    [Env(Key = "JSONRPC_PORT", Default = 1705)]
    public int JsonRpcPort { get; set; } = 1705;
}
