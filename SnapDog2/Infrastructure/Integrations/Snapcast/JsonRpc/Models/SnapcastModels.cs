using System.Text.Json.Serialization;

namespace SnapDog2.Infrastructure.Integrations.Snapcast.JsonRpc.Models;

// Request Models
public record ClientSetVolumeRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("volume")] VolumeInfo Volume);

public record ClientSetLatencyRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("latency")] int Latency);

public record ClientSetNameRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

public record GroupSetMuteRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("mute")] bool Mute);

public record GroupSetStreamRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("stream_id")] string StreamId);

public record GroupSetClientsRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("clients")] string[] Clients);

public record GroupSetNameRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

// Response Models
public record ClientSetVolumeResponse(
    [property: JsonPropertyName("volume")] VolumeInfo Volume);

public record ClientSetLatencyResponse(
    [property: JsonPropertyName("latency")] int Latency);

public record ClientSetNameResponse(
    [property: JsonPropertyName("name")] string Name);

public record GroupSetMuteResponse(
    [property: JsonPropertyName("mute")] bool Mute);

public record GroupSetStreamResponse(
    [property: JsonPropertyName("stream_id")] string StreamId);

public record GroupSetNameResponse(
    [property: JsonPropertyName("name")] string Name);

public record ServerGetStatusResponse(
    [property: JsonPropertyName("server")] ServerInfo Server);

public record ServerGetRpcVersionResponse(
    [property: JsonPropertyName("major")] int Major,
    [property: JsonPropertyName("minor")] int Minor,
    [property: JsonPropertyName("patch")] int Patch);

// Notification Models
public record ClientOnVolumeChangedNotification(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("volume")] VolumeInfo Volume);

public record ClientOnLatencyChangedNotification(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("latency")] int Latency);

public record ClientOnNameChangedNotification(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

public record ClientOnConnectNotification(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("client")] ClientInfo Client);

public record ClientOnDisconnectNotification(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("client")] ClientInfo Client);

public record GroupOnMuteNotification(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("mute")] bool Mute);

public record GroupOnStreamChangedNotification(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("stream_id")] string StreamId);

public record GroupOnNameChangedNotification(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

public record ServerOnUpdateNotification(
    [property: JsonPropertyName("server")] ServerInfo Server);

// Common Models
public record VolumeInfo(
    [property: JsonPropertyName("muted")] bool Muted,
    [property: JsonPropertyName("percent")] int Percent);

public record ClientInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("connected")] bool Connected,
    [property: JsonPropertyName("config")] ClientConfig Config,
    [property: JsonPropertyName("host")] HostInfo Host,
    [property: JsonPropertyName("lastSeen")] LastSeenInfo LastSeen,
    [property: JsonPropertyName("snapclient")] SnapclientInfo Snapclient);

public record ClientConfig(
    [property: JsonPropertyName("instance")] int Instance,
    [property: JsonPropertyName("latency")] int Latency,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("volume")] VolumeInfo Volume);

public record HostInfo(
    [property: JsonPropertyName("arch")] string Arch,
    [property: JsonPropertyName("ip")] string Ip,
    [property: JsonPropertyName("mac")] string Mac,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("os")] string Os);

public record LastSeenInfo(
    [property: JsonPropertyName("sec")] long Sec,
    [property: JsonPropertyName("usec")] long Usec);

public record SnapclientInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("protocolVersion")] int ProtocolVersion,
    [property: JsonPropertyName("version")] string Version);

public record GroupInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("muted")] bool Muted,
    [property: JsonPropertyName("stream_id")] string StreamId,
    [property: JsonPropertyName("clients")] ClientInfo[] Clients);

public record StreamInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("uri")] StreamUri Uri);

public record StreamUri(
    [property: JsonPropertyName("raw")] string Raw,
    [property: JsonPropertyName("scheme")] string Scheme,
    [property: JsonPropertyName("host")] string Host,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("fragment")] string Fragment,
    [property: JsonPropertyName("query")] Dictionary<string, string> Query);

public record ServerInfo(
    [property: JsonPropertyName("groups")] GroupInfo[] Groups,
    [property: JsonPropertyName("server")] ServerDetails Server,
    [property: JsonPropertyName("streams")] StreamInfo[] Streams);

public record ServerDetails(
    [property: JsonPropertyName("host")] HostInfo Host,
    [property: JsonPropertyName("snapserver")] SnapserverInfo Snapserver);

public record SnapserverInfo(
    [property: JsonPropertyName("controlProtocolVersion")] int ControlProtocolVersion,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("protocolVersion")] int ProtocolVersion,
    [property: JsonPropertyName("version")] string Version);
