namespace SnapDog2.Tests.Blueprint;

/// <summary>
/// Single source of truth for the SnapDog2 system blueprint.
/// This defines all commands, status, their protocols, endpoints, and implementation requirements.
/// </summary>
public static class SnapDogBlueprint
{
    /// <summary>
    /// Complete blueprint specification for all system features.
    /// </summary>
    public static readonly BlueprintSpecification Specification = new()
    {
        Commands = new[]
        {
            // === ZONE PLAYBACK COMMANDS ===
            new CommandSpec("PLAY")
            {
                Category = FeatureCategory.Zone,
                Description = "Start playback in a zone",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/play"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/play",
                RequiresImplementation = true
            },
            
            new CommandSpec("PAUSE")
            {
                Category = FeatureCategory.Zone,
                Description = "Pause playback in a zone", 
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/pause"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/pause",
                RequiresImplementation = true
            },
            
            new CommandSpec("STOP")
            {
                Category = FeatureCategory.Zone,
                Description = "Stop playback in a zone",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/stop"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/stop", 
                RequiresImplementation = true
            },

            // === VOLUME COMMANDS ===
            new CommandSpec("VOLUME")
            {
                Category = FeatureCategory.Zone,
                Description = "Set zone volume to specific level",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("PUT", "/api/v1/zones/{zoneIndex}/volume"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/volume",
                KnxExclusion = new("Handled by dedicated KNX volume actuators"),
                RequiresImplementation = true
            },
            
            new CommandSpec("VOLUME_UP")
            {
                Category = FeatureCategory.Zone,
                Description = "Increase zone volume",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/volume/up"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/volume_up",
                KnxExclusion = new("Handled by dedicated KNX volume actuators"),
                RequiresImplementation = true
            },
            
            new CommandSpec("VOLUME_DOWN")
            {
                Category = FeatureCategory.Zone,
                Description = "Decrease zone volume", 
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/volume/down"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/volume_down",
                KnxExclusion = new("Handled by dedicated KNX volume actuators"),
                RequiresImplementation = true
            },

            // === MUTE COMMANDS ===
            new CommandSpec("MUTE")
            {
                Category = FeatureCategory.Zone,
                Description = "Set zone mute state",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("PUT", "/api/v1/zones/{zoneIndex}/mute"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/mute",
                KnxExclusion = new("Handled by dedicated KNX mute actuators"),
                RequiresImplementation = true
            },
            
            new CommandSpec("MUTE_TOGGLE")
            {
                Category = FeatureCategory.Zone,
                Description = "Toggle zone mute state",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/mute/toggle"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/mute_toggle",
                KnxExclusion = new("Toggle commands require state synchronization unsuitable for KNX"),
                RequiresImplementation = true
            },

            // === TRACK COMMANDS ===
            new CommandSpec("TRACK")
            {
                Category = FeatureCategory.Zone,
                Description = "Set current track by index",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("PUT", "/api/v1/zones/{zoneIndex}/track"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/track",
                KnxExclusion = new("Complex track navigation not suitable for building automation"),
                RequiresImplementation = true
            },
            
            new CommandSpec("TRACK_NEXT")
            {
                Category = FeatureCategory.Zone,
                Description = "Skip to next track",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/track/next"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/track_next",
                KnxExclusion = new("Complex track navigation not suitable for building automation"),
                RequiresImplementation = true
            },
            
            new CommandSpec("TRACK_PREVIOUS")
            {
                Category = FeatureCategory.Zone,
                Description = "Skip to previous track",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/track/previous"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/track_previous",
                KnxExclusion = new("Complex track navigation not suitable for building automation"),
                RequiresImplementation = true
            },

            // === PLAYLIST COMMANDS ===
            new CommandSpec("PLAYLIST")
            {
                Category = FeatureCategory.Zone,
                Description = "Set current playlist",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("PUT", "/api/v1/zones/{zoneIndex}/playlist"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/playlist",
                RequiresImplementation = true
            },
            
            new CommandSpec("PLAYLIST_NEXT")
            {
                Category = FeatureCategory.Zone,
                Description = "Switch to next playlist",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/playlist/next"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/playlist_next",
                KnxExclusion = new("Complex playlist navigation not suitable for building automation"),
                RequiresImplementation = true
            },
            
            new CommandSpec("PLAYLIST_PREVIOUS")
            {
                Category = FeatureCategory.Zone,
                Description = "Switch to previous playlist",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/playlist/previous"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/playlist_previous",
                KnxExclusion = new("Complex playlist navigation not suitable for building automation"),
                RequiresImplementation = true
            },

            // === CLIENT COMMANDS ===
            new CommandSpec("CLIENT_VOLUME")
            {
                Category = FeatureCategory.Client,
                Description = "Set client volume",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("PUT", "/api/v1/clients/{clientIndex}/volume"),
                MqttTopic = "snapdog/client/{clientIndex}/command/volume",
                KnxExclusion = new("Client-specific network settings not suitable for building automation"),
                RequiresImplementation = true
            },
            
            new CommandSpec("CLIENT_LATENCY")
            {
                Category = FeatureCategory.Client,
                Description = "Set client audio latency",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("PUT", "/api/v1/clients/{clientIndex}/latency"),
                MqttTopic = "snapdog/client/{clientIndex}/command/latency",
                KnxExclusion = new("Network-specific setting not suitable for building automation"),
                RequiresImplementation = true
            },

            // === CONTROL COMMANDS ===
            new CommandSpec("CONTROL")
            {
                Category = FeatureCategory.Zone,
                Description = "General zone control command",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("POST", "/api/v1/zones/{zoneIndex}/control"),
                MqttTopic = "snapdog/zone/{zoneIndex}/command/control",
                RequiresImplementation = true
            },
        },

        Status = new[]
        {
            // === SYSTEM STATUS ===
            new StatusSpec("SYSTEM_STATUS")
            {
                Category = FeatureCategory.Global,
                Description = "Overall system health and status",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("GET", "/api/v1/system/status"),
                MqttTopic = "snapdog/system/status",
                KnxExclusion = new("Read-only system information not actionable via KNX"),
                RequiresImplementation = true
            },
            
            new StatusSpec("VERSION_INFO")
            {
                Category = FeatureCategory.Global,
                Description = "System version information",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("GET", "/api/v1/system/version"),
                MqttTopic = "snapdog/system/version",
                RequiresImplementation = true
            },

            // === ZONE STATUS ===
            new StatusSpec("ZONE_STATE")
            {
                Category = FeatureCategory.Zone,
                Description = "Complete zone state information",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("GET", "/api/v1/zones/{zoneIndex}"),
                MqttTopic = "snapdog/zone/{zoneIndex}/status/state",
                RequiresImplementation = true
            },
            
            new StatusSpec("VOLUME_STATUS")
            {
                Category = FeatureCategory.Zone,
                Description = "Current zone volume level",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("GET", "/api/v1/zones/{zoneIndex}/volume"),
                MqttTopic = "snapdog/zone/{zoneIndex}/status/volume",
                RequiresImplementation = true
            },
            
            new StatusSpec("MUTE_STATUS")
            {
                Category = FeatureCategory.Zone,
                Description = "Current zone mute state",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("GET", "/api/v1/zones/{zoneIndex}/mute"),
                MqttTopic = "snapdog/zone/{zoneIndex}/status/mute",
                RequiresImplementation = true
            },

            // === MQTT-ONLY STATUS ===
            new StatusSpec("CONTROL_STATUS")
            {
                Category = FeatureCategory.Zone,
                Description = "Control command execution status",
                Protocols = Protocol.Mqtt, // MQTT-only
                MqttTopic = "snapdog/zone/{zoneIndex}/status/control",
                RequiresImplementation = true
            },

            // === CLIENT STATUS ===
            new StatusSpec("CLIENT_STATE")
            {
                Category = FeatureCategory.Client,
                Description = "Complete client state information",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("GET", "/api/v1/clients/{clientIndex}"),
                MqttTopic = "snapdog/client/{clientIndex}/status/state",
                RequiresImplementation = true
            },
            
            new StatusSpec("CLIENT_VOLUME_STATUS")
            {
                Category = FeatureCategory.Client,
                Description = "Current client volume level",
                Protocols = Protocol.RestApi | Protocol.Mqtt,
                RestEndpoint = new("GET", "/api/v1/clients/{clientIndex}/volume"),
                MqttTopic = "snapdog/client/{clientIndex}/status/volume",
                RequiresImplementation = true
            },
        }
    };
}

/// <summary>
/// Complete blueprint specification containing all commands and status.
/// </summary>
public record BlueprintSpecification
{
    public required CommandSpec[] Commands { get; init; }
    public required StatusSpec[] Status { get; init; }
}

/// <summary>
/// Specification for a command in the system.
/// </summary>
public record CommandSpec(string Id)
{
    public required FeatureCategory Category { get; init; }
    public required string Description { get; init; }
    public required Protocol Protocols { get; init; }
    public RestEndpointSpec? RestEndpoint { get; init; }
    public string? MqttTopic { get; init; }
    public KnxExclusionSpec? KnxExclusion { get; init; }
    public bool RequiresImplementation { get; init; } = true;
    public bool IsRecentlyAdded { get; init; } = false;
    public string? Notes { get; init; }
}

/// <summary>
/// Specification for a status in the system.
/// </summary>
public record StatusSpec(string Id)
{
    public required FeatureCategory Category { get; init; }
    public required string Description { get; init; }
    public required Protocol Protocols { get; init; }
    public RestEndpointSpec? RestEndpoint { get; init; }
    public string? MqttTopic { get; init; }
    public KnxExclusionSpec? KnxExclusion { get; init; }
    public bool RequiresImplementation { get; init; } = true;
    public bool IsRecentlyAdded { get; init; } = false;
    public string? Notes { get; init; }
}

/// <summary>
/// REST API endpoint specification.
/// </summary>
public record RestEndpointSpec(string HttpMethod, string Path)
{
    public string? ControllerName { get; init; }
    public string? ActionName { get; init; }
}

/// <summary>
/// KNX exclusion specification with reasoning.
/// </summary>
public record KnxExclusionSpec(string Reason);

/// <summary>
/// Supported protocols for commands and status.
/// </summary>
[Flags]
public enum Protocol
{
    None = 0,
    RestApi = 1,
    Mqtt = 2,
    Knx = 4,
    All = RestApi | Mqtt | Knx
}

/// <summary>
/// Feature categories for organization.
/// </summary>
public enum FeatureCategory
{
    Global,
    Zone, 
    Client,
    Media,
    System
}
