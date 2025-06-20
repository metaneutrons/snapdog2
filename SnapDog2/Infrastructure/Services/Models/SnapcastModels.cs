using System.Text.Json.Serialization;

namespace SnapDog2.Infrastructure.Services.Models;

/// <summary>
/// Represents a Snapcast server status response.
/// </summary>
public class SnapcastServerStatus
{
    /// <summary>
    /// Gets or sets the server information.
    /// </summary>
    [JsonPropertyName("server")]
    public SnapcastServer Server { get; set; } = new();
}

/// <summary>
/// Represents a Snapcast server.
/// </summary>
public class SnapcastServer
{
    /// <summary>
    /// Gets or sets the groups on the server.
    /// </summary>
    [JsonPropertyName("groups")]
    public List<SnapcastGroup> Groups { get; set; } = new();

    /// <summary>
    /// Gets or sets the streams on the server.
    /// </summary>
    [JsonPropertyName("streams")]
    public List<SnapcastStream> Streams { get; set; } = new();

    /// <summary>
    /// Gets or sets the server information.
    /// </summary>
    [JsonPropertyName("server")]
    public SnapcastServerInfo ServerInfo { get; set; } = new();
}

/// <summary>
/// Represents a Snapcast group.
/// </summary>
public class SnapcastGroup
{
    /// <summary>
    /// Gets or sets the group identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the group is muted.
    /// </summary>
    [JsonPropertyName("muted")]
    public bool Muted { get; set; }

    /// <summary>
    /// Gets or sets the stream identifier assigned to this group.
    /// </summary>
    [JsonPropertyName("stream_id")]
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the clients in this group.
    /// </summary>
    [JsonPropertyName("clients")]
    public List<SnapcastClient> Clients { get; set; } = new();
}

/// <summary>
/// Represents a Snapcast client.
/// </summary>
public class SnapcastClient
{
    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the client is connected.
    /// </summary>
    [JsonPropertyName("connected")]
    public bool Connected { get; set; }

    /// <summary>
    /// Gets or sets the client configuration.
    /// </summary>
    [JsonPropertyName("config")]
    public SnapcastClientConfig Config { get; set; } = new();

    /// <summary>
    /// Gets or sets the client host information.
    /// </summary>
    [JsonPropertyName("host")]
    public SnapcastHost Host { get; set; } = new();

    /// <summary>
    /// Gets or sets the last seen timestamp.
    /// </summary>
    [JsonPropertyName("lastSeen")]
    public SnapcastTimestamp LastSeen { get; set; } = new();

    /// <summary>
    /// Gets or sets the snapclient information.
    /// </summary>
    [JsonPropertyName("snapclient")]
    public SnapcastClientInfo Snapclient { get; set; } = new();
}

/// <summary>
/// Represents a Snapcast client configuration.
/// </summary>
public class SnapcastClientConfig
{
    /// <summary>
    /// Gets or sets the client instance number.
    /// </summary>
    [JsonPropertyName("instance")]
    public int Instance { get; set; }

    /// <summary>
    /// Gets or sets the client latency.
    /// </summary>
    [JsonPropertyName("latency")]
    public int Latency { get; set; }

    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client volume configuration.
    /// </summary>
    [JsonPropertyName("volume")]
    public SnapcastVolume Volume { get; set; } = new();
}

/// <summary>
/// Represents a Snapcast volume configuration.
/// </summary>
public class SnapcastVolume
{
    /// <summary>
    /// Gets or sets whether the client is muted.
    /// </summary>
    [JsonPropertyName("muted")]
    public bool Muted { get; set; }

    /// <summary>
    /// Gets or sets the volume percentage (0-100).
    /// </summary>
    [JsonPropertyName("percent")]
    public int Percent { get; set; }
}

/// <summary>
/// Represents a Snapcast host information.
/// </summary>
public class SnapcastHost
{
    /// <summary>
    /// Gets or sets the host architecture.
    /// </summary>
    [JsonPropertyName("arch")]
    public string Arch { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the host IP address.
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the host MAC address.
    /// </summary>
    [JsonPropertyName("mac")]
    public string Mac { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the host operating system.
    /// </summary>
    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Snapcast timestamp.
/// </summary>
public class SnapcastTimestamp
{
    /// <summary>
    /// Gets or sets the seconds component.
    /// </summary>
    [JsonPropertyName("sec")]
    public long Sec { get; set; }

    /// <summary>
    /// Gets or sets the microseconds component.
    /// </summary>
    [JsonPropertyName("usec")]
    public long Usec { get; set; }

    /// <summary>
    /// Gets the timestamp as a DateTime.
    /// </summary>
    public DateTime ToDateTime()
    {
        return DateTimeOffset.FromUnixTimeSeconds(Sec).AddTicks(Usec * 10).DateTime;
    }
}

/// <summary>
/// Represents a Snapcast client information.
/// </summary>
public class SnapcastClientInfo
{
    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the protocol version.
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; set; }

    /// <summary>
    /// Gets or sets the client version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Snapcast stream.
/// </summary>
public class SnapcastStream
{
    /// <summary>
    /// Gets or sets the stream identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stream status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stream URI information.
    /// </summary>
    [JsonPropertyName("uri")]
    public SnapcastStreamUri Uri { get; set; } = new();
}

/// <summary>
/// Represents a Snapcast stream URI.
/// </summary>
public class SnapcastStreamUri
{
    /// <summary>
    /// Gets or sets the URI fragment.
    /// </summary>
    [JsonPropertyName("fragment")]
    public string Fragment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URI host.
    /// </summary>
    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URI path.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URI query parameters.
    /// </summary>
    [JsonPropertyName("query")]
    public Dictionary<string, string> Query { get; set; } = new();

    /// <summary>
    /// Gets or sets the raw URI string.
    /// </summary>
    [JsonPropertyName("raw")]
    public string Raw { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URI scheme.
    /// </summary>
    [JsonPropertyName("scheme")]
    public string Scheme { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Snapcast server information.
/// </summary>
public class SnapcastServerInfo
{
    /// <summary>
    /// Gets or sets the server host information.
    /// </summary>
    [JsonPropertyName("host")]
    public SnapcastHost Host { get; set; } = new();

    /// <summary>
    /// Gets or sets the snapserver information.
    /// </summary>
    [JsonPropertyName("snapserver")]
    public SnapcastServerDetails Snapserver { get; set; } = new();
}

/// <summary>
/// Represents detailed Snapcast server information.
/// </summary>
public class SnapcastServerDetails
{
    /// <summary>
    /// Gets or sets the control protocol version.
    /// </summary>
    [JsonPropertyName("controlProtocolVersion")]
    public int ControlProtocolVersion { get; set; }

    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the protocol version.
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; set; }

    /// <summary>
    /// Gets or sets the server version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Represents a JSON-RPC request for Snapcast operations.
/// </summary>
public class SnapcastJsonRpcRequest
{
    /// <summary>
    /// Gets or sets the JSON-RPC version.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method parameters.
    /// </summary>
    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// Represents a JSON-RPC response from Snapcast operations.
/// </summary>
public class SnapcastJsonRpcResponse<T>
{
    /// <summary>
    /// Gets or sets the JSON-RPC version.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the result.
    /// </summary>
    [JsonPropertyName("result")]
    public T? Result { get; set; }

    /// <summary>
    /// Gets or sets the error information.
    /// </summary>
    [JsonPropertyName("error")]
    public SnapcastJsonRpcError? Error { get; set; }
}

/// <summary>
/// Represents a JSON-RPC error.
/// </summary>
public class SnapcastJsonRpcError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional error data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
