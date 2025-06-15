# Configuration System

SnapDog2 is designed for flexible deployment, primarily within containerized environments like Docker. Consequently, its configuration is managed predominantly through **environment variables**. This approach aligns well with container orchestration platforms and simplifies setup across different systems.

Configuration values provided via environment variables are loaded at application startup into strongly-typed C# record classes located in the `/Core/Configuration` folder/namespace. This ensures type safety and provides easy access to configuration settings throughout the application via Dependency Injection (typically using the `IOptions<T>` pattern or direct singleton registration).

This section details the environment variables used, the structure of the corresponding configuration classes, the helper methods for loading values, and the mandatory startup validation process.

## 10.1 Environment Variables Overview

Configuration follows a consistent naming convention: `SNAPDOG_{COMPONENT}_{SETTING}`. The following table provides a comprehensive list of all available environment variables, their purpose, and default values used if the variable is not explicitly set. For indexed configurations (Zones and Clients), `{n}` represents the zone number (starting from 1) and `{m}` represents the client number (starting from 1).

**(Table 10.1.1: Comprehensive Environment Variable List)**

| Environment Variable                       | Default Value             | Description                                          | Component / Notes                  |
| :----------------------------------------- | :------------------------ | :----------------------------------------------------- | :--------------------------------- |
| **System Configuration**                   |                           |                                                        | `Worker` / `Core`                  |
| `SNAPDOG_LOG_LEVEL`                        | `Information`             | Logging level (Trace, Debug, Info, Warn, Error, Critical) | Logging                            |
| **Telemetry Configuration**                |                           |                                                        | `Infrastructure.Observability`     |
| `SNAPDOG_TELEMETRY_ENABLED`                | `false`                   | Enable OpenTelemetry integration                       | OTel Setup                         |
| `SNAPDOG_TELEMETRY_SERVICE_NAME`           | `SnapDog2`                | Service name for telemetry                           | OTel Setup                         |
| `SNAPDOG_TELEMETRY_SAMPLING_RATE`          | `1.0`                     | Trace sampling rate (0.0-1.0)                      | OTel Tracing                     |
| `SNAPDOG_TELEMETRY_OTLP_ENDPOINT`          | `http://localhost:4317`   | OTLP Exporter Endpoint (gRPC default)                | OTel OTLP Exporter               |
| `SNAPDOG_TELEMETRY_OTLP_PROTOCOL`          | `grpc`                    | OTLP Protocol (`grpc` or `HttpProtobuf`)           | OTel OTLP Exporter               |
| `SNAPDOG_TELEMETRY_OTLP_HEADERS`           | *N/A (Optional)*          | Optional OTLP headers (e.g., `Auth=...`)             | OTel OTLP Exporter               |
| **Prometheus Configuration**               |                           |                                                        | `Infrastructure.Observability`     |
| `SNAPDOG_PROMETHEUS_ENABLED`               | `false`                   | Enable Prometheus exporter                           | OTel Metrics                     |
| `SNAPDOG_PROMETHEUS_PATH`                  | `/metrics`                | Prometheus scrape endpoint path                      | API Endpoint Mapping             |
| **API Authentication**                     |                           |                                                        | `Api.Auth`                         |
| `SNAPDOG_API_AUTH_ENABLED`                 | `true`                    | Enable API key authentication                        | API Security                     |
| `SNAPDOG_API_APIKEY_{n}`                   | *N/A (Req. if Enabled)* | API key for requests (n = 1, 2...)                     | API Security                     |
| **Snapcast Configuration**                 |                           |                                                        | `Infrastructure.Snapcast`        |
| `SNAPDOG_SNAPCAST_HOST`                    | `localhost`               | Hostname or IP of Snapcast server                      | Snapcast Service                 |
| `SNAPDOG_SNAPCAST_CONTROL_PORT`            | `1705`                    | Snapcast JSON-RPC control port                         | Snapcast Service                 |
| `SNAPDOG_SNAPCAST_STREAM_PORT`             | `1704`                    | Snapcast Stream port (Informational)                 | (Not directly used by Service) |
| `SNAPDOG_SNAPCAST_HTTP_PORT`               | `1780`                    | Snapcast HTTP port (Informational)                   | (Not directly used by Service) |
| **Subsonic Configuration**                 |                           |                                                        | `Infrastructure.Subsonic`        |
| `SNAPDOG_SUBSONIC_ENABLED`                 | `false`                   | Enable Subsonic integration                            | Subsonic Service                 |
| `SNAPDOG_SUBSONIC_SERVER`                  | `http://localhost:4533`   | Subsonic server URL                                      | Subsonic Service                 |
| `SNAPDOG_SUBSONIC_USERNAME`                | `admin`                   | Subsonic username                                        | Subsonic Service                 |
| `SNAPDOG_SUBSONIC_PASSWORD`                | *N/A (Req. if Needed)*    | Subsonic password                                        | Subsonic Service                 |
| `SNAPDOG_SUBSONIC_TIMEOUT`                 | `10000`                   | Subsonic API request timeout (ms)                      | Subsonic Service / HttpClient    |
| **Radio Configuration**                    |                           |                                                        | `Server.Managers.PlaylistManager`|
| `SNAPDOG_RADIO_PLAYLISTNAME`               | `Radio`                   | Display name for the Radio playlist                      | Radio Configuration Loader       |
| `SNAPDOG_RADIO_{n}_NAME`                   | *N/A (Required)*          | Display name for Radio Station `n` (n=1, 2...)           | Radio Configuration Loader       |
| `SNAPDOG_RADIO_{n}_URL`                    | *N/A (Required)*          | **Streaming URL** for Radio Station `n`                  | Radio Configuration Loader       |
| `SNAPDOG_RADIO_{n}_IMAGE_URL`              | *N/A (Optional)*          | Icon/Logo URL for Radio Station `n`                      | Radio Configuration Loader       |
| **MQTT Configuration**                     |                           |                                                        | `Infrastructure.Mqtt`            |
| `SNAPDOG_MQTT_ENABLED`                     | `true`                    | Enable MQTT integration                                | Mqtt Service                     |
| `SNAPDOG_MQTT_SERVER`                      | `localhost`               | MQTT broker hostname or IP                             | Mqtt Service                     |
| `SNAPDOG_MQTT_PORT`                        | `1883`                    | MQTT broker port                                         | Mqtt Service                     |
| `SNAPDOG_MQTT_USERNAME`                    | *N/A (Optional)*          | MQTT broker username                                     | Mqtt Service                     |
| `SNAPDOG_MQTT_PASSWORD`                    | *N/A (Optional)*          | MQTT broker password                                     | Mqtt Service                     |
| `SNAPDOG_MQTT_BASE_TOPIC`                  | `snapdog`                 | Base topic for all MQTT messages                       | Mqtt Service                     |
| `SNAPDOG_MQTT_CLIENT_ID`                   | `snapdog-server`          | Client ID for MQTT connection                          | Mqtt Service                     |
| `SNAPDOG_MQTT_USE_TLS`                     | `false`                   | Use TLS for MQTT connection                            | Mqtt Service                     |
| **System-wide MQTT Topics**                |                           | *(Relative to Base Topic)*                              | `Infrastructure.Mqtt`            |
| `SNAPDOG_MQTT_STATUS_TOPIC`                | `status`                  | System status topic                                      | Mqtt Service                     |
| `SNAPDOG_MQTT_ERROR_TOPIC`                 | `error`                   | System errors topic                                      | Mqtt Service                     |
| `SNAPDOG_MQTT_VERSION_TOPIC`               | `version`                 | System version topic                                     | Mqtt Service                     |
| `SNAPDOG_MQTT_STATS_TOPIC`                 | `stats`                   | System statistics topic                                  | Mqtt Service                     |
| **KNX Configuration**                      |                           |                                                        | `Infrastructure.Knx`             |
| `SNAPDOG_KNX_ENABLED`                      | `false`                   | Enable KNX integration                                 | Knx Service                      |
| `SNAPDOG_KNX_CONNECTION_TYPE`              | `IpRouting`               | KNX connection type (`IpTunneling`, `IpRouting`, `Usb`)  | Knx Service                      |
| `SNAPDOG_KNX_GATEWAY_IP`                   | *N/A (Optional)*          | KNX gateway IP address (for IpTunneling)               | Knx Service                      |
| `SNAPDOG_KNX_PORT`                         | `3671`                    | KNX gateway port (for IpTunneling)                       | Knx Service                      |
| `SNAPDOG_KNX_DEVICE_ADDRESS`               | `15.15.250`               | KNX physical address for SnapDog2                        | Knx Service                      |
| `SNAPDOG_KNX_RETRY_COUNT`                  | `3`                       | Retry attempts for KNX commands                        | Knx Service / Polly            |
| `SNAPDOG_KNX_RETRY_INTERVAL`               | `1000`                    | Interval between KNX retries (ms)                      | Knx Service / Polly            |
| **Zone Configuration (`_n_`)**            |                           |                                                        | `Server.Managers.ZoneManager`      |
| `SNAPDOG_ZONE_{n}_NAME`                    | `Zone {n}`                | Name of zone {n} (n=1, 2...)                             | Zone Config Loader               |
| `SNAPDOG_ZONE_{n}_MQTT_BASETOPIC`          | `snapdog/zones/{n}`       | Base MQTT topic for zone {n}                             | Zone Config Loader               |
| `SNAPDOG_ZONE_{n}_SINK`                    | `/snapsinks/zone{n}`      | Snapcast sink path for zone {n}                          | Zone Config / Media Player       |
| **MQTT Zone Topics (`_n_`)**               |                           | *(Relative to Zone Base Topic)*                       | `Infrastructure.Mqtt`            |
| *(See Table 10.1.1.1)*                  | *(Defaults vary)*         | Zone MQTT command/status topics                      | MqttZoneConfig                   |
| **KNX Zone Configuration (`_n_`)**         |                           | *(Group Addresses)*                                   | `Infrastructure.Knx`             |
| `SNAPDOG_ZONE_{n}_KNX_ENABLED`             | `false`                   | Enable KNX for zone {n}                                  | KnxZoneConfig                    |
| *(See Table 10.1.1.2)*                  | *N/A (Req. if Enabled)*   | Zone KNX Group Addresses                             | KnxZoneConfig                    |
| **Client Configuration (`_m_`)**           |                           |                                                        | `Server.Managers.ClientManager`      |
| `SNAPDOG_CLIENT_{m}_NAME`                  | `Client {m}`              | Name of client {m} (m=1, 2...)                           | Client Config Loader             |
| `SNAPDOG_CLIENT_{m}_MAC`                   | *N/A (Optional)*          | MAC address of client {m}                              | Client Config Loader             |
| `SNAPDOG_CLIENT_{m}_MQTT_BASETOPIC`        | `snapdog/clients/{m}`     | Base MQTT topic for client {m}                         | Client Config Loader             |
| `SNAPDOG_CLIENT_{m}_DEFAULT_ZONE`          | `1`                       | Default zone ID (**1-based**) for client {m}         | Client Config / Client Manager |
| **MQTT Client Topics (`_m_`)**             |                           | *(Relative to Client Base Topic)*                     | `Infrastructure.Mqtt`            |
| *(See Table 10.1.1.3)*                  | *(Defaults vary)*         | Client MQTT command/status topics                    | MqttClientConfig                 |
| **KNX Client Configuration (`_m_`)**       |                           | *(Group Addresses)*                                   | `Infrastructure.Knx`             |
| `SNAPDOG_CLIENT_{m}_KNX_ENABLED`           | `false`                   | Enable KNX for client {m}                              | KnxClientConfig                  |
| *(See Section 9.4.3)*                   | *N/A (Req. if Enabled)*   | Client KNX Group Addresses                           | KnxClientConfig                  |

### 10.1.1 Extracted Topic/GA Variable Tables

#### 10.1.1.1 MQTT Zone Topic Variables

*(Relative to `SNAPDOG_ZONE_{n}_MQTT_BASETOPIC`)*

| Env Var Suffix                             | Default Value          | Maps To Command/Status                  | Direction |
| :----------------------------------------- | :--------------------- | :-------------------------------------- | :-------- |
| **Commands**                               |                        |                                         |           |
| `_STATE_SET_TOPIC`                         | `state/set`            | `PLAY`/`PAUSE`/`STOP`/Modes via payload | Command   |
| `_TRACK_SET_TOPIC`                         | `track/set`            | `TRACK`                                 | Command   |
| `_PLAYLIST_SET_TOPIC`                      | `playlist/set`         | `PLAYLIST`                              | Command   |
| `_TRACK_REPEAT_SET_TOPIC`                  | `track_repeat/set`     | `TRACK_REPEAT`, `TRACK_REPEAT_TOGGLE`   | Command   |
| `_PLAYLIST_REPEAT_SET_TOPIC`               | `playlist_repeat/set`  | `PLAYLIST_REPEAT`, `PLAYLIST_REPEAT_TOGGLE`| Command   |
| `_PLAYLIST_SHUFFLE_SET_TOPIC`              | `playlist_shuffle/set` | `PLAYLIST_SHUFFLE`, `PLAYLIST_SHUFFLE_TOGGLE`| Command |
| `_VOLUME_SET_TOPIC`                        | `volume/set`           | `VOLUME`, `VOLUME_UP`, `VOLUME_DOWN`      | Command   |
| `_MUTE_SET_TOPIC`                          | `mute/set`             | `MUTE`, `MUTE_TOGGLE`                   | Command   |
| **Status**                                 |                        |                                         |           |
| `_STATE_TOPIC`                             | `state`                | `PLAYBACK_STATE`, `TRACK_REPEAT_STATUS`, `PLAYLIST_REPEAT_STATUS`, `PLAYLIST_SHUFFLE_STATUS`, `MUTE_STATUS` | Status    |
| `_VOLUME_TOPIC`                            | `volume`               | `VOLUME_STATUS`                         | Status    |
| `_MUTE_TOPIC`                              | `mute`                 | `MUTE_STATUS` (explicit bool)           | Status    |
| `_TRACK_TOPIC`                             | `track`                | `TRACK_INDEX`                           | Status    |
| `_PLAYLIST_TOPIC`                          | `playlist`             | `PLAYLIST_INDEX`                        | Status    |
| `_STATE_TOPIC`                             | `state`                | `ZONE_STATE` (Full JSON)                | Status    |
| `_TRACK_INFO_TOPIC`                        | `track/info`           | `TRACK_INFO` (JSON)                     | Status    |
| `_PLAYLIST_INFO_TOPIC`                     | `playlist/info`        | `PLAYLIST_INFO` (JSON)                  | Status    |
| `_TRACK_REPEAT_TOPIC`                      | `track_repeat`         | `TRACK_REPEAT_STATUS` (explicit bool)   | Status    |
| `_PLAYLIST_REPEAT_TOPIC`                   | `playlist_repeat`    | `PLAYLIST_REPEAT_STATUS` (explicit bool)| Status    |
| `_PLAYLIST_SHUFFLE_TOPIC`                  | `playlist_shuffle`   | `PLAYLIST_SHUFFLE_STATUS` (explicit bool)| Status    |

#### 10.1.1.2 KNX Zone Group Address Variables

*(Format: `SNAPDOG_ZONE_{n}_KNX_{SUFFIX}`. Value: KNX Group Address `x/y/z`. Required if `SNAPDOG_ZONE_{n}_KNX_ENABLED=true` and functionality needed)*

| Suffix                         | Maps To Command/Status          | Direction | DPT (See Appendix 20.3) |
| :----------------------------- | :------------------------------ | :-------- | :---------------------- |
| **Commands**                   |                                 |           |                         |
| `_PLAY`                        | `PLAY`, `PAUSE`                 | Command   | 1.001                   |
| `_STOP`                        | `STOP`                          | Command   | 1.001                   |
| `_TRACK_NEXT`                  | `TRACK_NEXT`                    | Command   | 1.007                   |
| `_TRACK_PREVIOUS`              | `TRACK_PREVIOUS`                | Command   | 1.007                   |
| `_TRACK_REPEAT`                | `TRACK_REPEAT`                  | Command   | 1.001                   |
| `_TRACK_REPEAT_TOGGLE`         | `TRACK_REPEAT_TOGGLE`           | Command   | 1.001                   |
| `_TRACK`                       | `TRACK`                         | Command   | 5.010                   |
| `_PLAYLIST`                    | `PLAYLIST`                      | Command   | 5.010                   |
| `_PLAYLIST_NEXT`               | `PLAYLIST_NEXT`                 | Command   | 1.007                   |
| `_PLAYLIST_PREVIOUS`           | `PLAYLIST_PREVIOUS`             | Command   | 1.007                   |
| `_PLAYLIST_SHUFFLE`            | `PLAYLIST_SHUFFLE`              | Command   | 1.001                   |
| `_PLAYLIST_SHUFFLE_TOGGLE`     | `PLAYLIST_SHUFFLE_TOGGLE`       | Command   | 1.001                   |
| `_PLAYLIST_REPEAT`             | `PLAYLIST_REPEAT`               | Command   | 1.001                   |
| `_PLAYLIST_REPEAT_TOGGLE`      | `PLAYLIST_REPEAT_TOGGLE`        | Command   | 1.001                   |
| `_KNX_VOLUME`                  | `VOLUME`                        | Command   | 5.001                   |
| `_KNX_VOLUME_DIM`              | `VOLUME_UP`, `VOLUME_DOWN`      | Command   | 3.007                   |
| `_KNX_MUTE`                    | `MUTE`                          | Command   | 1.001                   |
| `_KNX_MUTE_TOGGLE`             | `MUTE_TOGGLE`                   | Command   | 1.001                   |
| **Status**                     |                                 |           |                         |
| `_KNX_PLAYBACK_STATUS`         | `PLAYBACK_STATE`                | Status    | 1.001                   |
| `_KNX_TRACK_REPEAT_STATUS`     | `TRACK_REPEAT_STATUS`           | Status    | 1.001                   |
| `_KNX_TRACK_STATUS`            | `TRACK_INDEX`                   | Status    | 5.010                   |
| `_KNX_PLAYLIST_STATUS`         | `PLAYLIST_INDEX`                | Status    | 5.010                   |
| `_KNX_PLAYLIST_SHUFFLE_STATUS` | `PLAYLIST_SHUFFLE_STATUS`       | Status    | 1.001                   |
| `_KNX_PLAYLIST_REPEAT_STATUS`  | `PLAYLIST_REPEAT_STATUS`        | Status    | 1.001                   |
| `_KNX_VOLUME_STATUS`           | `VOLUME_STATUS`                 | Status    | 5.001                   |
| `_KNX_MUTE_STATUS`             | `MUTE_STATUS`                   | Status    | 1.001                   |

#### 10.1.1.3 MQTT Client Topic Variables

*(Relative to `SNAPDOG_CLIENT_{m}_MQTT_BASETOPIC`)*

| Env Var Suffix             | Default Value | Maps To Command/Status          | Direction |
| :------------------------- | :------------ | :------------------------------ | :-------- |
| **Commands**               |               |                                 |           |
| `_VOLUME_SET_TOPIC`        | `volume/set`  | `CLIENT_VOLUME`                 | Command   |
| `_MUTE_SET_TOPIC`          | `mute/set`    | `CLIENT_MUTE`, `CLIENT_MUTE_TOGGLE` | Command   |
| `_LATENCY_SET_TOPIC`       | `latency/set` | `CLIENT_LATENCY`                | Command   |
| `_ZONE_SET_TOPIC`          | `zone/set`    | `CLIENT_ZONE`                   | Command   |
| **Status**                 |               |                                 |           |
| `_CONNECTED_TOPIC`         | `connected`   | `CLIENT_CONNECTED`              | Status    |
| `_VOLUME_TOPIC`            | `volume`      | `CLIENT_VOLUME_STATUS`          | Status    |
| `_MUTE_TOPIC`              | `mute`        | `CLIENT_MUTE_STATUS`            | Status    |
| `_LATENCY_TOPIC`           | `latency`     | `CLIENT_LATENCY_STATUS`         | Status    |
| `_ZONE_TOPIC`              | `zone`        | `CLIENT_ZONE_STATUS`            | Status    |
| `_STATE_TOPIC`             | `state`       | `CLIENT_STATE`                  | Status    |

## 10.2 Configuration Classes

Load using `/Infrastructure/EnvConfigHelper`. Use `init;`. **Parsing errors handled by Validator.** Use `Knx.Falcon.GroupAddress?`. Define `OtlpProtocol`/`KnxConnectionType` Enums.

### 10.2.1 Base Helper (`EnvConfigHelper`)

```csharp
// Located in /Infrastructure or /Core/Configuration
namespace SnapDog2.Infrastructure;

using System;
using Knx.Falcon; // Use SDK's GroupAddress

/// <summary>
/// Helper methods for retrieving configuration values from environment variables.
/// These methods attempt parsing but defer strict validation.
/// </summary>
public static class EnvConfigHelper
{
    /// <summary> Gets string value or default. </summary>
    public static string GetValue(string name, string defaultValue) =>
        Environment.GetEnvironmentVariable(name) ?? defaultValue;

    /// <summary> Gets specific convertible type or default. Logs warning on conversion failure. </summary>
    public static T GetValue<T>(string name, T defaultValue) where T : IConvertible
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value)) return defaultValue;
        try {
            return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        } catch (Exception ex) {
             // Log warning about conversion failure to console error (real logger not available here)
             Console.Error.WriteLine($"WARN: Config load failed to convert env var '{name}' value '{value}' to type {typeof(T)}. Using default '{defaultValue}'. Ex: {ex.Message}");
            return defaultValue;
        }
    }

    /// <summary> Gets bool, recognizing true/1/yes (case-insensitive). </summary>
    public static bool GetBool(string name, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return value == "true" || value == "1" || value == "yes";
    }

    /// <summary> Attempts to parse GroupAddress, returns null if unset or invalid format. </summary>
    public static GroupAddress? TryParseGroupAddress(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (GroupAddress.TryParse(value, out var ga)) return ga;

        Console.Error.WriteLine($"WARN: Config load found invalid KNX Group Address format for env var '{name}': '{value}'. Format should be e.g., '1/2/3'.");
        return null; // Return null, validator MUST check if this GA was required.
    }

    /// <summary> Attempts to parse Enum (case-insensitive), returns default if unset or invalid format. </summary>
    public static TEnum TryParseEnum<TEnum>(string name, TEnum defaultValue) where TEnum : struct, Enum
    {
         var value = Environment.GetEnvironmentVariable(name);
         if (string.IsNullOrWhiteSpace(value)) return defaultValue;
         if(Enum.TryParse<TEnum>(value, ignoreCase: true, out var result)) return result;

         Console.Error.WriteLine($"WARN: Config load found invalid Enum value for env var '{name}': '{value}'. Expected one of {string.Join(", ", Enum.GetNames<TEnum>())}. Using default '{defaultValue}'.");
         return defaultValue; // Return default, validator MUST check if default is acceptable.
    }
}
```

### 10.2.2 Service-Specific Configuration Classes

These classes (`/Core/Configuration`) use the helper methods to load configuration values during application startup.

```csharp
// --- /Core/Configuration/TelemetryOptions.cs ---
namespace SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure;
/// <summary> OTLP Exporter Protocol options. </summary>
public enum OtlpProtocol { Unknown, Grpc, HttpProtobuf }
/// <summary> Telemetry configuration root. </summary>
public class TelemetryOptions {
     public bool Enabled { get; init; } = EnvConfigHelper.GetBool("SNAPDOG_TELEMETRY_ENABLED", false);
     public string ServiceName { get; init; } = EnvConfigHelper.GetValue("SNAPDOG_TELEMETRY_SERVICE_NAME", "SnapDog2");
     public double SamplingRate { get; init; } = EnvConfigHelper.GetValue("SNAPDOG_TELEMETRY_SAMPLING_RATE", 1.0);
     public OtlpExporterOptions OtlpExporter { get; init; } = OtlpExporterOptions.Load();
     public PrometheusOptions Prometheus { get; init; } = PrometheusOptions.Load();
}
/// <summary> OTLP exporter specific options. </summary>
public class OtlpExporterOptions {
     public string Endpoint { get; init; }
     public OtlpProtocol Protocol { get; init; }
     public string? Headers { get; init; }
     public static OtlpExporterOptions Load() => new() {
         Endpoint = EnvConfigHelper.GetValue("SNAPDOG_TELEMETRY_OTLP_ENDPOINT", "http://localhost:4317"),
         Protocol = EnvConfigHelper.TryParseEnum<OtlpProtocol>("SNAPDOG_TELEMETRY_OTLP_PROTOCOL", OtlpProtocol.Grpc),
         Headers = EnvConfigHelper.GetValue("SNAPDOG_TELEMETRY_OTLP_HEADERS", (string?)null)
     };
}
/// <summary> Prometheus exporter specific options. </summary>
public class PrometheusOptions {
     public bool Enabled { get; init; }
     public string Path { get; init; }
     public static PrometheusOptions Load() => new() {
         Enabled = EnvConfigHelper.GetBool("SNAPDOG_PROMETHEUS_ENABLED", false),
         Path = EnvConfigHelper.GetValue("SNAPDOG_PROMETHEUS_PATH", "/metrics")
     };
}

// --- /Core/Configuration/KnxOptions.cs ---
namespace SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure;
using Knx.Falcon; // For GroupAddress type used internally by KnxZone/Client Config
/// <summary> KNX Connection Types. </summary>
public enum KnxConnectionType { Unknown, IpTunneling, IpRouting, Usb }
/// <summary> General KNX configuration. </summary>
public class KnxOptions {
    public bool Enabled { get; init; } = EnvConfigHelper.GetBool("SNAPDOG_KNX_ENABLED", false);
    public KnxConnectionType ConnectionType { get; init; } = EnvConfigHelper.TryParseEnum<KnxConnectionType>("SNAPDOG_KNX_CONNECTION_TYPE", KnxConnectionType.IpRouting);
    public string? GatewayIp { get; init; } = EnvConfigHelper.GetValue("SNAPDOG_KNX_GATEWAY_IP", (string?)null);
    public int Port { get; init; } = EnvConfigHelper.GetValue("SNAPDOG_KNX_PORT", 3671);
    public string DeviceAddress { get; init; } = EnvConfigHelper.GetValue("SNAPDOG_KNX_DEVICE_ADDRESS", "15.15.250");
    public int RetryCount { get; init; } = EnvConfigHelper.GetValue("SNAPDOG_KNX_RETRY_COUNT", 3);
    public int RetryInterval { get; init; } = EnvConfigHelper.GetValue("SNAPDOG_KNX_RETRY_INTERVAL", 1000);
}

// --- /Core/Configuration/KnxZoneConfig.cs ---
namespace SnapDog2.Core.Configuration;
using Knx.Falcon; // Use SDK's GroupAddress
using SnapDog2.Infrastructure;
/// <summary> Holds KNX Group Addresses for a specific Zone. </summary>
public class KnxZoneConfig {
     public int ZoneId { get; init; }
     public bool Enabled { get; init; }
     // Nullable GroupAddress properties loaded using TryParseGroupAddress
     public GroupAddress? PlayAddress { get; init; }
     public GroupAddress? StopAddress { get; init; }
     public GroupAddress? PlaybackStatusAddress { get; init; }
     public GroupAddress? TrackNextAddress { get; init; }
     public GroupAddress? TrackPreviousAddress { get; init; }
     public GroupAddress? TrackRepeatAddress { get; init; }
     public GroupAddress? TrackRepeatToggleAddress { get; init; }
     public GroupAddress? TrackRepeatStatusAddress { get; init; }
     public GroupAddress? TrackAddress { get; init; }
     public GroupAddress? TrackStatusAddress { get; init; }
     public GroupAddress? PlaylistAddress { get; init; }
     public GroupAddress? PlaylistStatusAddress { get; init; }
     public GroupAddress? PlaylistNextAddress { get; init; }
     public GroupAddress? PlaylistPreviousAddress { get; init; }
     public GroupAddress? PlaylistShuffleAddress { get; init; }
     public GroupAddress? PlaylistShuffleToggleAddress { get; init; }
     public GroupAddress? PlaylistShuffleStatusAddress { get; init; }
     public GroupAddress? PlaylistRepeatAddress { get; init; }
     public GroupAddress? PlaylistRepeatToggleAddress { get; init; }
     public GroupAddress? PlaylistRepeatStatusAddress { get; init; }
     public GroupAddress? VolumeAddress { get; init; }
     public GroupAddress? VolumeDimAddress { get; init; } // For Up/Down
     public GroupAddress? VolumeStatusAddress { get; init; }
     public GroupAddress? MuteAddress { get; init; }
     public GroupAddress? MuteToggleAddress { get; init; }
     public GroupAddress? MuteStatusAddress { get; init; }

     // Factory method to load from environment
     public static KnxZoneConfig Load(int zoneId) {
         var config = new KnxZoneConfig { ZoneId = zoneId };
         var prefix = $"SNAPDOG_ZONE_{zoneId}_KNX";
         config = config with { Enabled = EnvConfigHelper.GetBool($"{prefix}_ENABLED", false) };
         if(!config.Enabled) return config;

         // Load all properties using TryParseGroupAddress
         config = config with { PlayAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAY") };
         config = config with { StopAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_STOP") };
         config = config with { PlaybackStatusAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYBACK_STATUS") };
         config = config with { TrackNextAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_TRACK_NEXT") };
         config = config with { TrackPreviousAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_TRACK_PREVIOUS") };
         config = config with { TrackRepeatAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_TRACK_REPEAT") };
         config = config with { TrackRepeatToggleAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_TRACK_REPEAT_TOGGLE") };
         config = config with { TrackRepeatStatusAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_TRACK_REPEAT_STATUS") };
         config = config with { TrackAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_TRACK") };
         config = config with { TrackStatusAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_TRACK_STATUS") };
         config = config with { PlaylistAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYLIST") };
         config = config with { PlaylistStatusAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYLIST_STATUS") };
         config = config with { PlaylistNextAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYLIST_NEXT") };
         config = config with { PlaylistPreviousAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYLIST_PREVIOUS") };
         config = config with { PlaylistShuffleAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYLIST_SHUFFLE") };
         config = config with { PlaylistShuffleToggleAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYLIST_SHUFFLE_TOGGLE") };
         config = config with { PlaylistShuffleStatusAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYLIST_SHUFFLE_STATUS") };
         config = config with { PlaylistRepeatAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYLIST_REPEAT") };
         config = config with { PlaylistRepeatToggleAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYLIST_REPEAT_TOGGLE") };
         config = config with { PlaylistRepeatStatusAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_PLAYLIST_REPEAT_STATUS") };
         config = config with { VolumeAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_VOLUME") };
         config = config with { VolumeDimAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_VOLUME_DIM") };
         config = config with { VolumeStatusAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_VOLUME_STATUS") };
         config = config with { MuteAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_MUTE") };
         config = config with { MuteToggleAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_MUTE_TOGGLE") };
         config = config with { MuteStatusAddress = EnvConfigHelper.TryParseGroupAddress($"{prefix}_MUTE_STATUS") };
         // Add any missing GAs here...
         return config;
     }
}

// --- Other Config classes (ClientConfiguration, MqttZoneConfiguration, etc.) defined similarly ---
```

## 10.3 Configuration Validation (`/Worker/ConfigurationValidator.cs`)

**Mandatory startup step.** Runs after DI build. Retrieves loaded configs (`IOptions<T>`, lists, singletons). **Throws `InvalidOperationException` on critical errors** (missing required vars, invalid required GAs/Enums). Logs warnings.

```csharp
// In /Worker/ConfigurationValidator.cs
namespace SnapDog2.Worker;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration;
using Knx.Falcon; // For GroupAddress checks

/// <summary>
/// Performs validation of loaded application configuration at startup.
/// </summary>
public static partial class ConfigurationValidator
{
    // Logger Messages
    [LoggerMessage(1, LogLevel.Critical, "Configuration validation failed: {ErrorMessage}")]
    private static partial void LogValidationErrorCritical(ILogger logger, string errorMessage);
    [LoggerMessage(2, LogLevel.Warning, "Configuration validation warning: {WarningMessage}")]
    private static partial void LogValidationWarning(ILogger logger, string warningMessage);
    [LoggerMessage(3, LogLevel.Information, "Configuration validated successfully.")]
    private static partial void LogValidationSuccess(ILogger logger);

    /// <summary>
    /// Validates the application configuration retrieved from the service provider.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <returns>True if configuration is valid, False otherwise.</returns>
    public static bool Validate(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ConfigurationValidation");
        var validationErrors = new List<string>();
        var validationWarnings = new List<string>();

        try {
            // Retrieve configurations using GetRequiredService or IOptions
            var knxOptions = services.GetRequiredService<IOptions<KnxOptions>>().Value;
            var zones = services.GetRequiredService<List<ZoneConfig>>();
            var clients = services.GetRequiredService<List<ClientConfig>>();
            var apiAuth = services.GetRequiredService<ApiAuthConfiguration>();
            var mqttOptions = services.GetRequiredService<IOptions<MqttOptions>>().Value;
            var snapcastOptions = services.GetRequiredService<IOptions<SnapcastOptions>>().Value;
            var subsonicOptions = services.GetRequiredService<IOptions<SubsonicOptions>>().Value;
            var telemetryOptions = services.GetRequiredService<IOptions<TelemetryOptions>>().Value;

            // --- Validate KNX Options ---
            if (knxOptions.Enabled) {
                if (knxOptions.ConnectionType == KnxConnectionType.Unknown) {
                    validationErrors.Add($"Invalid or missing 'SNAPDOG_KNX_CONNECTION_TYPE'. Value '{Environment.GetEnvironmentVariable("SNAPDOG_KNX_CONNECTION_TYPE")}' is not valid. Must be one of {string.Join(", ", Enum.GetNames<KnxConnectionType>())}.");
                }
                if (knxOptions.ConnectionType == KnxConnectionType.IpTunneling && string.IsNullOrWhiteSpace(knxOptions.GatewayIp)) {
                     validationWarnings.Add("'SNAPDOG_KNX_CONNECTION_TYPE' is IpTunneling, but 'SNAPDOG_KNX_GATEWAY_IP' not set; relying solely on discovery.");
                }
                 // Validate required GAs within zones/clients IF KNX is enabled for them
                 foreach(var zone in zones.Where(z => z.Knx != null && z.Knx.Enabled)) {
                      ValidateRequiredGa(validationErrors, $"SNAPDOG_ZONE_{zone.Id}_KNX_VOLUME_STATUS", zone.Knx.VolumeStatusAddress);
                      ValidateRequiredGa(validationErrors, $"SNAPDOG_ZONE_{zone.Id}_KNX_PLAYBACK_STATUS", zone.Knx.PlaybackStatusAddress);
                      // Add more ValidateRequiredGa calls for GAs essential for basic function...
                 }
                 foreach(var client in clients.Where(c => c.Knx != null && c.Knx.Enabled)) {
                      ValidateRequiredGa(validationErrors, $"SNAPDOG_CLIENT_{client.Id}_KNX_VOLUME_STATUS", client.Knx.VolumeStatusAddress);
                      // Add more ValidateRequiredGa calls...
                 }
            }

             // --- Validate API Auth ---
             if(apiAuth.Enabled && apiAuth.ApiKeys.Count == 0) {
                  validationErrors.Add("API Authentication is enabled via SNAPDOG_API_AUTH_ENABLED, but no API keys (SNAPDOG_API_APIKEY_n) were configured.");
             }

             // --- Validate Telemetry ---
             if(telemetryOptions.Enabled && telemetryOptions.OtlpExporter.Protocol == OtlpProtocol.Unknown) {
                  validationErrors.Add($"Invalid or missing 'SNAPDOG_TELEMETRY_OTLP_PROTOCOL'. Value '{Environment.GetEnvironmentVariable("SNAPDOG_TELEMETRY_OTLP_PROTOCOL")}' is not valid. Must be one of {string.Join(", ", Enum.GetNames<OtlpProtocol>())}.");
             }

            // --- Validate Zones and Clients ---
            if (!zones.Any()) { validationErrors.Add("No zones configured via 'SNAPDOG_ZONE_{n}_NAME'. At least one zone is required."); }
            if (!clients.Any()) { validationWarnings.Add("No clients configured via 'SNAPDOG_CLIENT_{m}_NAME'. Playback will not be possible."); }
            // Check for duplicate zone sinks
             var duplicateSinks = zones.GroupBy(z => z.SnapcastSink).Where(g => g.Count() > 1).Select(g => g.Key);
             if (duplicateSinks.Any()) {
                  validationErrors.Add($"Duplicate Snapcast sinks configured in SNAPDOG_ZONE_n_SINK: {string.Join(", ", duplicateSinks)}");
             }
             // Check client default zones exist
             var zoneIds = zones.Select(z => z.Id).ToHashSet();
             foreach (var client in clients) {
                 if (!zoneIds.Contains(client.DefaultZoneId)) {
                      validationErrors.Add($"Client '{client.Name}' (ID: {client.Id}) configured with 'SNAPDOG_CLIENT_{client.Id}_DEFAULT_ZONE={client.DefaultZoneId}' references a non-existent zone ID.");
                 }
             }

             // --- Validate MQTT ---
             if (mqttOptions.Enabled) {
                  if (string.IsNullOrWhiteSpace(mqttOptions.Server)) { validationErrors.Add("MQTT is enabled but 'SNAPDOG_MQTT_SERVER' is not set."); }
             }
             // --- Validate Snapcast ---
             if (string.IsNullOrWhiteSpace(snapcastOptions.Host)) { validationErrors.Add("'SNAPDOG_SNAPCAST_HOST' is required."); }
             // --- Validate Subsonic ---
             if (subsonicOptions.Enabled) {
                  if (string.IsNullOrWhiteSpace(subsonicOptions.Server)) { validationErrors.Add("Subsonic is enabled but 'SNAPDOG_SUBSONIC_SERVER' is not set."); }
                  if (string.IsNullOrWhiteSpace(subsonicOptions.Username)) { validationErrors.Add("Subsonic is enabled but 'SNAPDOG_SUBSONIC_USERNAME' is not set."); }
                  // Password might be optional
             }


        } catch (Exception ex) {
             // Catch errors during retrieval from DI (e.g., missing registrations)
             validationErrors.Add($"Critical error retrieving configuration from DI: {ex.Message}");
             LogValidationErrorCritical(logger, validationErrors.Last(), ex);
             return false; // Cannot proceed
        }

        // Log warnings
        foreach(var warning in validationWarnings) LogValidationWarning(logger, warning);

        // Log errors and return validation result
        if (validationErrors.Any()) {
            logger.LogError("Configuration validation failed with {ErrorCount} errors.", validationErrors.Count);
            foreach(var error in validationErrors) LogValidationErrorCritical(logger, error);
            // Throw an aggregate exception to halt startup clearly
            throw new AggregateException("Configuration validation failed. See logs for details.",
                validationErrors.Select(e => new InvalidOperationException(e)));
            // return false; // Or just return false
        }

        LogValidationSuccess(logger);
        return true; // Indicate success
    }

     /// <summary>
     /// Helper for validating required Group Addresses were parsed correctly.
     /// </summary>
     private static void ValidateRequiredGa(List<string> errors, string envVarName, GroupAddress? ga) {
          var envVarValue = Environment.GetEnvironmentVariable(envVarName);
          if(string.IsNullOrWhiteSpace(envVarValue)) {
               // Variable was not set, but it's required
               errors.Add($"Required KNX Group Address environment variable '{envVarName}' is not set.");
          } else if (ga == null) {
               // Variable was set, but EnvConfigHelper failed to parse it (warning already logged)
               errors.Add($"Required KNX Group Address environment variable '{envVarName}' has an invalid format: '{envVarValue}'. Use e.g., '1/2/3'.");
          }
     }
}
```
