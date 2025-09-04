namespace SnapDog2.Infrastructure.Integrations.Snapcast.Models;

/// <summary>
/// Represents a Snapcast client (replaces SnapcastClient.Models.SnapClient)
/// </summary>
public struct SnapClient
{
    public string Id { get; set; }
    public bool Connected { get; set; }
    public ClientConfig Config { get; set; }
    public HostInfo Host { get; set; }
    public LastSeenInfo LastSeen { get; set; }
    public SnapclientInfo Snapclient { get; set; }
}

/// <summary>
/// Represents client configuration (replaces SnapcastClient.Models.ClientConfig)
/// </summary>
public struct ClientConfig
{
    public int Instance { get; set; }
    public int Latency { get; set; }
    public string Name { get; set; }
    public ClientVolume Volume { get; set; }
}

/// <summary>
/// Represents client volume (replaces SnapcastClient.Models.ClientVolume)
/// </summary>
public struct ClientVolume
{
    public bool Muted { get; set; }
    public int Percent { get; set; }
}

/// <summary>
/// Represents host information (replaces SnapcastClient.Models.HostInfo)
/// </summary>
public struct HostInfo
{
    public string Arch { get; set; }
    public string Ip { get; set; }
    public string Mac { get; set; }
    public string Name { get; set; }
    public string Os { get; set; }
}

/// <summary>
/// Represents last seen information (replaces SnapcastClient.Models.LastSeenInfo)
/// </summary>
public struct LastSeenInfo
{
    public long Sec { get; set; }
    public long Usec { get; set; }
}

/// <summary>
/// Represents snapclient information (replaces SnapcastClient.Models.SnapclientInfo)
/// </summary>
public struct SnapclientInfo
{
    public string Name { get; set; }
    public int ProtocolVersion { get; set; }
    public string Version { get; set; }
}

/// <summary>
/// Represents a Snapcast group (replaces SnapcastClient.Models.Group)
/// </summary>
public struct Group
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool Muted { get; set; }
    public string StreamId { get; set; }
    public SnapClient[] Clients { get; set; }
}

/// <summary>
/// Represents a Snapcast stream (replaces SnapcastClient.Models.Stream)
/// </summary>
public struct Stream
{
    public string Id { get; set; }
    public string Status { get; set; }
    public StreamUri Uri { get; set; }
}

/// <summary>
/// Represents stream URI information (replaces SnapcastClient.Models.StreamUri)
/// </summary>
public struct StreamUri
{
    public string Raw { get; set; }
    public string Scheme { get; set; }
    public string Host { get; set; }
    public string Path { get; set; }
    public string Fragment { get; set; }
    public Dictionary<string, string> Query { get; set; }
}

/// <summary>
/// Represents Snapcast server (replaces SnapcastClient.Models.Server)
/// </summary>
public struct Server
{
    public Group[] Groups { get; set; }
    public ServerDetails ServerInfo { get; set; }
    public Stream[] Streams { get; set; }
}

/// <summary>
/// Represents server details (replaces SnapcastClient.Models.ServerDetails)
/// </summary>
public struct ServerDetails
{
    public HostInfo Host { get; set; }
    public SnapserverInfo Snapserver { get; set; }
}

/// <summary>
/// Represents snapserver information (replaces SnapcastClient.Models.SnapserverInfo)
/// </summary>
public struct SnapserverInfo
{
    public int ControlProtocolVersion { get; set; }
    public string Name { get; set; }
    public int ProtocolVersion { get; set; }
    public string Version { get; set; }
}
