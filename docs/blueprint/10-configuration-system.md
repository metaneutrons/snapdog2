# Configuration System

SnapDog2 is designed for flexible deployment within containerized environments. Its configuration system leverages **EnvoyConfig**, a modern .NET configuration library that provides attribute-based environment variable binding, automatic validation, nested object mapping, and custom type conversion.

The configuration follows a **unified nested architecture** where all settings are organized into a single root [`SnapDogConfiguration`](SnapDogConfiguration.cs:1) class with strongly-typed nested objects for different subsystems. This eliminates configuration fragmentation while providing comprehensive type safety and validation.

## 10.1 Architecture Overview

### 10.1.1 Unified Configuration Structure

```mermaid
classDiagram
    class SnapDogConfiguration {
        +SystemConfig System
        +TelemetryConfig Telemetry
        +ApiConfig Api
        +ServicesConfig Services
        +List~ZoneConfig~ Zones
        +List~ClientConfig~ Clients
        +List~RadioStationConfig~ RadioStations
    }

    class SystemConfig {
        +string LogLevel
        +string Environment
    }

    class TelemetryConfig {
        +bool Enabled
        +string ServiceName
        +double SamplingRate
        +OtlpConfig Otlp
        +PrometheusConfig Prometheus
    }

    class ServicesConfig {
        +SnapcastConfig Snapcast
        +MqttConfig Mqtt
        +KnxConfig Knx
        +SubsonicConfig Subsonic
    }

    class ZoneConfig {
        +string Name
        +string Sink
        +ZoneMqttConfig Mqtt
        +ZoneKnxConfig Knx
    }

    class ClientConfig {
        +string Name
        +string Mac
        +int DefaultZone
        +ClientMqttConfig Mqtt
        +ClientKnxConfig Knx
    }

    SnapDogConfiguration --> SystemConfig
    SnapDogConfiguration --> TelemetryConfig
    SnapDogConfiguration --> ServicesConfig
    SnapDogConfiguration --> ZoneConfig
    SnapDogConfiguration --> ClientConfig
```

### 10.1.2 EnvoyConfig Features Utilized

- **`[Env]` Attribute**: Maps properties to environment variables with options for defaults, validation, and type conversion
- **`NestedListPrefix`**: Enables indexed configurations like `SNAPDOG_ZONE_1_*`, `SNAPDOG_CLIENT_2_*`
- **`NestedPrefix`**: Maps nested objects like `SNAPDOG_ZONE_1_MQTT_*` to sub-configuration classes
- **`MapPrefix`**: Handles key-value configurations like Snapcast settings
- **`ListPrefix`**: Maps numbered environment variables to lists (e.g., API keys)
- **Custom Type Converters**: Handle domain-specific types like [`KnxAddress`](KnxAddress.cs:1)
- **Built-in Validation**: Automatic validation with [`IValidateOptions<T>`](IValidateOptions.cs:1) for business logic

## 10.2 Environment Variable Structure

All environment variables use the global prefix `SNAPDOG_` and follow a hierarchical naming convention that maps directly to the nested configuration classes.

### 10.2.1 System Configuration

```bash
# Basic system settings
SNAPDOG_SYSTEM_LOG_LEVEL=Information          # Default: Information
SNAPDOG_SYSTEM_ENVIRONMENT=Production         # Default: Development
```

### 10.2.2 Telemetry Configuration

```bash
# Core telemetry settings
SNAPDOG_TELEMETRY_ENABLED=true                # Default: false
SNAPDOG_TELEMETRY_SERVICE_NAME=snapdog        # Default: SnapDog2
SNAPDOG_TELEMETRY_SAMPLING_RATE=1.0           # Default: 1.0

# OTLP exporter configuration
SNAPDOG_TELEMETRY_OTLP_ENABLED=true                # Default: false
SNAPDOG_TELEMETRY_OTLP_ENDPOINT=http://jaeger:4317  # Default: http://localhost:4317
SNAPDOG_TELEMETRY_OTLP_PROTOCOL=grpc               # Default: grpc
SNAPDOG_TELEMETRY_OTLP_HEADERS=Auth=Bearer-token   # Optional

# Prometheus metrics configuration
SNAPDOG_TELEMETRY_PROMETHEUS_ENABLED=true     # Default: false
SNAPDOG_TELEMETRY_PROMETHEUS_PORT=9090        # Default: 9090
SNAPDOG_TELEMETRY_PROMETHEUS_PATH=/metrics    # Default: /metrics
```

### 10.2.3 API Configuration

```bash
# API authentication
SNAPDOG_API_AUTH_ENABLED=true                 # Default: true
SNAPDOG_API_APIKEY_1=secret-key-1             # Required if auth enabled
SNAPDOG_API_APIKEY_2=secret-key-2             # Additional keys as needed
SNAPDOG_API_APIKEY_3=secret-key-3
```

### 10.2.4 Services Configuration

```bash
# Snapcast integration
SNAPDOG_SERVICES_SNAPCAST_HOST=snapcast-server     # Default: localhost
SNAPDOG_SERVICES_SNAPCAST_CONTROL_PORT=1705        # Default: 1705
SNAPDOG_SERVICES_SNAPCAST_STREAM_PORT=1704         # Default: 1704
SNAPDOG_SERVICES_SNAPCAST_HTTP_PORT=1780           # Default: 1780

# MQTT integration
SNAPDOG_SERVICES_MQTT_ENABLED=true                 # Default: true
SNAPDOG_SERVICES_MQTT_SERVER=mosquitto             # Default: localhost
SNAPDOG_SERVICES_MQTT_PORT=1883                    # Default: 1883
SNAPDOG_SERVICES_MQTT_USERNAME=snapdog             # Optional
SNAPDOG_SERVICES_MQTT_PASSWORD=snapdog             # Optional
SNAPDOG_SERVICES_MQTT_BASE_TOPIC=snapdog           # Default: snapdog
SNAPDOG_SERVICES_MQTT_CLIENT_ID=snapdog-server     # Default: snapdog-server
SNAPDOG_SERVICES_MQTT_USE_TLS=false                # Default: false

# KNX integration
SNAPDOG_SERVICES_KNX_ENABLED=true                  # Default: false
SNAPDOG_SERVICES_KNX_CONNECTION_TYPE=IpTunneling   # Default: IpRouting
SNAPDOG_SERVICES_KNX_GATEWAY_IP=192.168.1.100     # Required for IpTunneling
SNAPDOG_SERVICES_KNX_PORT=3671                     # Default: 3671
SNAPDOG_SERVICES_KNX_DEVICE_ADDRESS=15.15.250     # Default: 15.15.250
SNAPDOG_SERVICES_KNX_RETRY_COUNT=3                 # Default: 3
SNAPDOG_SERVICES_KNX_RETRY_INTERVAL=1000           # Default: 1000ms

# Subsonic integration
SNAPDOG_SERVICES_SUBSONIC_ENABLED=false           # Default: false
SNAPDOG_SERVICES_SUBSONIC_SERVER=http://subsonic:4533  # Required if enabled
SNAPDOG_SERVICES_SUBSONIC_USERNAME=admin          # Required if enabled
SNAPDOG_SERVICES_SUBSONIC_PASSWORD=password       # Required if enabled
SNAPDOG_SERVICES_SUBSONIC_TIMEOUT=10000           # Default: 10000ms
```

### 10.2.5 Zone Configuration (Nested Lists)

```bash
# Zone 1 Configuration
SNAPDOG_ZONE_1_NAME=Living Room                    # Required
SNAPDOG_ZONE_1_SINK=/snapsinks/living-room        # Required

# Zone 1 MQTT Configuration
SNAPDOG_ZONE_1_MQTT_BASE_TOPIC=snapdog/zones/living-room
SNAPDOG_ZONE_1_MQTT_STATE_SET_TOPIC=state/set     # Default: state/set
SNAPDOG_ZONE_1_MQTT_TRACK_SET_TOPIC=track/set     # Default: track/set
SNAPDOG_ZONE_1_MQTT_PLAYLIST_SET_TOPIC=playlist/set   # Default: playlist/set
SNAPDOG_ZONE_1_MQTT_VOLUME_SET_TOPIC=volume/set   # Default: volume/set
SNAPDOG_ZONE_1_MQTT_MUTE_SET_TOPIC=mute/set       # Default: mute/set
SNAPDOG_ZONE_1_MQTT_STATE_TOPIC=state             # Default: state
SNAPDOG_ZONE_1_MQTT_VOLUME_TOPIC=volume           # Default: volume
SNAPDOG_ZONE_1_MQTT_MUTE_TOPIC=mute               # Default: mute
SNAPDOG_ZONE_1_MQTT_TRACK_TOPIC=track             # Default: track
SNAPDOG_ZONE_1_MQTT_PLAYLIST_TOPIC=playlist       # Default: playlist

# Zone 1 KNX Configuration
SNAPDOG_ZONE_1_KNX_ENABLED=true                   # Default: false
SNAPDOG_ZONE_1_KNX_PLAY=1/1/1                     # Optional KNX addresses
SNAPDOG_ZONE_1_KNX_PAUSE=1/1/2
SNAPDOG_ZONE_1_KNX_STOP=1/1/3
SNAPDOG_ZONE_1_KNX_TRACK_NEXT=1/1/4
SNAPDOG_ZONE_1_KNX_TRACK_PREVIOUS=1/1/5
SNAPDOG_ZONE_1_KNX_VOLUME=1/2/1
SNAPDOG_ZONE_1_KNX_VOLUME_STATUS=1/2/2
SNAPDOG_ZONE_1_KNX_VOLUME_UP=1/2/3
SNAPDOG_ZONE_1_KNX_VOLUME_DOWN=1/2/4
SNAPDOG_ZONE_1_KNX_MUTE=1/2/5
SNAPDOG_ZONE_1_KNX_MUTE_STATUS=1/2/6
SNAPDOG_ZONE_1_KNX_MUTE_TOGGLE=1/2/7

# Zone 2 Configuration
SNAPDOG_ZONE_2_NAME=Kitchen
SNAPDOG_ZONE_2_SINK=/snapsinks/kitchen
SNAPDOG_ZONE_2_MQTT_BASE_TOPIC=snapdog/zones/kitchen
SNAPDOG_ZONE_2_KNX_ENABLED=false
```

### 10.2.6 Client Configuration (Nested Lists)

```bash
# Client 1 Configuration
SNAPDOG_CLIENT_1_NAME=Living Room Speaker         # Required
SNAPDOG_CLIENT_1_MAC=AA:BB:CC:DD:EE:FF            # Optional
SNAPDOG_CLIENT_1_DEFAULT_ZONE=1                   # Default: 1

# Client 1 MQTT Configuration
SNAPDOG_CLIENT_1_MQTT_BASE_TOPIC=snapdog/clients/living-room
SNAPDOG_CLIENT_1_MQTT_VOLUME_SET_TOPIC=volume/set     # Default: volume/set
SNAPDOG_CLIENT_1_MQTT_MUTE_SET_TOPIC=mute/set         # Default: mute/set
SNAPDOG_CLIENT_1_MQTT_LATENCY_SET_TOPIC=latency/set   # Default: latency/set
SNAPDOG_CLIENT_1_MQTT_ZONE_SET_TOPIC=zone/set         # Default: zone/set
SNAPDOG_CLIENT_1_MQTT_CONNECTED_TOPIC=connected       # Default: connected
SNAPDOG_CLIENT_1_MQTT_VOLUME_TOPIC=volume             # Default: volume
SNAPDOG_CLIENT_1_MQTT_MUTE_TOPIC=mute                 # Default: mute
SNAPDOG_CLIENT_1_MQTT_LATENCY_TOPIC=latency           # Default: latency
SNAPDOG_CLIENT_1_MQTT_ZONE_TOPIC=zone                 # Default: zone
SNAPDOG_CLIENT_1_MQTT_STATE_TOPIC=state               # Default: state

# Client 1 KNX Configuration
SNAPDOG_CLIENT_1_KNX_ENABLED=true                     # Default: false
SNAPDOG_CLIENT_1_KNX_VOLUME=2/1/1                     # Optional KNX addresses
SNAPDOG_CLIENT_1_KNX_VOLUME_STATUS=2/1/2
SNAPDOG_CLIENT_1_KNX_VOLUME_UP=2/1/3
SNAPDOG_CLIENT_1_KNX_VOLUME_DOWN=2/1/4
SNAPDOG_CLIENT_1_KNX_MUTE=2/1/5
SNAPDOG_CLIENT_1_KNX_MUTE_STATUS=2/1/6
SNAPDOG_CLIENT_1_KNX_MUTE_TOGGLE=2/1/7
SNAPDOG_CLIENT_1_KNX_LATENCY=2/1/8
SNAPDOG_CLIENT_1_KNX_ZONE=2/1/9
SNAPDOG_CLIENT_1_KNX_CONNECTED_STATUS=2/1/10

# Client 2 Configuration
SNAPDOG_CLIENT_2_NAME=Kitchen Speaker
SNAPDOG_CLIENT_2_DEFAULT_ZONE=2
SNAPDOG_CLIENT_2_MQTT_BASE_TOPIC=snapdog/clients/kitchen
SNAPDOG_CLIENT_2_KNX_ENABLED=false
```

### 10.2.7 Radio Station Configuration (Nested Lists)

```bash
# Radio Station 1
SNAPDOG_RADIO_1_NAME=BBC Radio 1
SNAPDOG_RADIO_1_URL=http://stream.live.vc.bbcmedia.co.uk/bbc_radio_one

# Radio Station 2
SNAPDOG_RADIO_2_NAME=Jazz FM
SNAPDOG_RADIO_2_URL=http://jazz-wr04.ice.infomaniak.ch/jazz-wr04.mp3

# Radio Station 3
SNAPDOG_RADIO_3_NAME=Classical Radio
SNAPDOG_RADIO_3_URL=https://stream.srg-ssr.ch/rsc_de/aacp_96.m3u
```

## 10.3 Configuration Classes

All configuration classes are located in [`/Core/Configuration`](/Core/Configuration:1) and use EnvoyConfig attributes for automatic environment variable binding.

### 10.3.1 Root Configuration Class

```csharp
// --- /Core/Configuration/SnapDogConfiguration.cs ---
namespace SnapDog2.Core.Configuration;

using System.ComponentModel.DataAnnotations;
using EnvoyConfig.Attributes;

/// <summary>
/// Root configuration class for the SnapDog2 application.
/// Maps all environment variables starting with SNAPDOG_ to nested configuration objects.
/// </summary>
public class SnapDogConfiguration
{
    /// <summary>
    /// Basic system configuration settings.
    /// Maps environment variables with prefix: SNAPDOG_SYSTEM_*
    /// </summary>
    [Env(NestedPrefix = "SYSTEM_")]
    public SystemConfig System { get; set; } = new();

    /// <summary>
    /// Telemetry and observability configuration.
    /// Maps environment variables with prefix: SNAPDOG_TELEMETRY_*
    /// </summary>
    [Env(NestedPrefix = "TELEMETRY_")]
    public TelemetryConfig Telemetry { get; set; } = new();

    /// <summary>
    /// API authentication and security configuration.
    /// Maps environment variables with prefix: SNAPDOG_API_*
    /// </summary>
    [Env(NestedPrefix = "API_")]
    public ApiConfig Api { get; set; } = new();

    /// <summary>
    /// External services configuration (Snapcast, MQTT, KNX, Subsonic).
    /// Maps environment variables with prefix: SNAPDOG_SERVICES_*
    /// </summary>
    [Env(NestedPrefix = "SERVICES_")]
    public ServicesConfig Services { get; set; } = new();

    /// <summary>
    /// List of audio zone configurations.
    /// Maps environment variables with pattern: SNAPDOG_ZONE_X_*
    /// Where X is the zone index (1, 2, 3, etc.)
    /// </summary>
    [Env(NestedListPrefix = "ZONE_", NestedListSuffix = "_")]
    public List<ZoneConfig> Zones { get; set; } = [];

    /// <summary>
    /// List of client device configurations.
    /// Maps environment variables with pattern: SNAPDOG_CLIENT_X_*
    /// Where X is the client index (1, 2, 3, etc.)
    /// </summary>
    [Env(NestedListPrefix = "CLIENT_", NestedListSuffix = "_")]
    public List<ClientConfig> Clients { get; set; } = [];

    /// <summary>
    /// List of radio station configurations.
    /// Maps environment variables with pattern: SNAPDOG_RADIO_X_*
    /// Where X is the radio station index (1, 2, 3, etc.)
    /// </summary>
    [Env(NestedListPrefix = "RADIO_", NestedListSuffix = "_")]
    public List<RadioStationConfig> RadioStations { get; set; } = [];
}
```

### 10.3.2 System Configuration

```csharp
// --- /Core/Configuration/SystemConfig.cs ---
namespace SnapDog2.Core.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// Basic system configuration settings.
/// </summary>
public class SystemConfig
{
    /// <summary>
    /// Logging level for the application.
    /// Maps to: SNAPDOG_SYSTEM_LOG_LEVEL
    /// </summary>
    [Env(Key = "LOG_LEVEL", Default = "Information")]
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Application environment (Development, Staging, Production).
    /// Maps to: SNAPDOG_SYSTEM_ENVIRONMENT
    /// </summary>
    [Env(Key = "ENVIRONMENT", Default = "Development")]
    public string Environment { get; set; } = "Development";
}
```

### 10.3.3 Zone Configuration

```csharp
// --- /Core/Configuration/ZoneConfig.cs ---
namespace SnapDog2.Core.Configuration;

using System.ComponentModel.DataAnnotations;
using EnvoyConfig.Attributes;

/// <summary>
/// Configuration for an individual audio zone.
/// Maps environment variables like SNAPDOG_ZONE_X_* to properties.
/// </summary>
public class ZoneConfig
{
    /// <summary>
    /// Display name of the zone.
    /// Maps to: SNAPDOG_ZONE_X_NAME
    /// </summary>
    [Env(Key = "NAME")]
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Snapcast sink path for this zone.
    /// Maps to: SNAPDOG_ZONE_X_SINK
    /// </summary>
    [Env(Key = "SINK")]
    [Required]
    public string Sink { get; set; } = null!;

    /// <summary>
    /// MQTT configuration for this zone.
    /// Maps environment variables with prefix: SNAPDOG_ZONE_X_MQTT_*
    /// </summary>
    [Env(NestedPrefix = "MQTT_")]
    public ZoneMqttConfig Mqtt { get; set; } = new();

    /// <summary>
    /// KNX configuration for this zone.
    /// Maps environment variables with prefix: SNAPDOG_ZONE_X_KNX_*
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public ZoneKnxConfig Knx { get; set; } = new();
}
```

### 10.3.4 Client Configuration

```csharp
// --- /Core/Configuration/ClientConfig.cs ---
namespace SnapDog2.Core.Configuration;

using System.ComponentModel.DataAnnotations;
using EnvoyConfig.Attributes;

/// <summary>
/// Configuration for an individual client device.
/// Maps environment variables like SNAPDOG_CLIENT_X_* to properties.
/// </summary>
public class ClientConfig
{
    /// <summary>
    /// Display name of the client.
    /// Maps to: SNAPDOG_CLIENT_X_NAME
    /// </summary>
    [Env(Key = "NAME")]
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// MAC address of the client device.
    /// Maps to: SNAPDOG_CLIENT_X_MAC
    /// </summary>
    [Env(Key = "MAC")]
    [RegularExpression(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
        ErrorMessage = "MAC address must be in format XX:XX:XX:XX:XX:XX")]
    public string? Mac { get; set; }

    /// <summary>
    /// Default zone ID for this client (1-based).
    /// Maps to: SNAPDOG_CLIENT_X_DEFAULT_ZONE
    /// </summary>
    [Env(Key = "DEFAULT_ZONE", Default = 1)]
    [Range(1, 100)]
    public int DefaultZone { get; set; } = 1;

    /// <summary>
    /// MQTT configuration for this client.
    /// Maps environment variables with prefix: SNAPDOG_CLIENT_X_MQTT_*
    /// </summary>
    [Env(NestedPrefix = "MQTT_")]
    public ClientMqttConfig Mqtt { get; set; } = new();

    /// <summary>
    /// KNX configuration for this client.
    /// Maps environment variables with prefix: SNAPDOG_CLIENT_X_KNX_*
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public ClientKnxConfig Knx { get; set; } = new();
}
```

## 10.4 Custom Type Converters

### 10.4.1 KNX Address Converter

```csharp
// --- /Core/Configuration/Converters/KnxAddressConverter.cs ---
namespace SnapDog2.Core.Configuration.Converters;

using System;
using EnvoyConfig.Conversion;
using EnvoyConfig.Logging;
using SnapDog2.Core.Models;

/// <summary>
/// Custom type converter for KnxAddress to integrate with EnvoyConfig.
/// Converts between string environment variable values and KnxAddress instances.
/// </summary>
public class KnxAddressConverter : ITypeConverter
{
    /// <summary>
    /// Converts a string value from an environment variable to a KnxAddress.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <param name="targetType">The target type (KnxAddress or KnxAddress?).</param>
    /// <param name="logger">Optional logger for warnings and errors.</param>
    /// <returns>A KnxAddress instance if conversion is successful; otherwise, null.</returns>
    public object? Convert(string? value, Type targetType, IEnvLogSink? logger)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Handle nullable KnxAddress
            if (targetType == typeof(KnxAddress?))
                return null;

            // For non-nullable, this will be handled by EnvoyConfig's validation
            return null;
        }

        if (KnxAddress.TryParse(value, out var result))
            return result;

        // Log the conversion error if logger is available
        logger?.Log(
            EnvLogLevel.Error,
            $"Failed to parse KNX address '{value}'. Expected format: 'Main/Middle/Sub' (e.g., '2/1/1')."
        );

        // Return null for invalid values - EnvoyConfig will handle the error appropriately
        return null;
    }
}
```

## 10.5 Configuration Validation

### 10.5.1 Business Logic Validation

```csharp
// --- /Core/Configuration/Validation/SnapDogConfigurationValidator.cs ---
namespace SnapDog2.Core.Configuration.Validation;

using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration;

/// <summary>
/// Validates SnapDogConfiguration for business logic constraints.
/// </summary>
public class SnapDogConfigurationValidator : IValidateOptions<SnapDogConfiguration>
{
    public ValidateOptionsResult Validate(string? name, SnapDogConfiguration options)
    {
        var failures = new List<string>();

        // Validate zone constraints
        ValidateZones(options.Zones, failures);

        // Validate client constraints
        ValidateClients(options.Clients, options.Zones, failures);

        // Validate radio stations
        ValidateRadioStations(options.RadioStations, failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateZones(List<ZoneConfig> zones, List<string> failures)
    {
        if (zones.Count == 0)
        {
            failures.Add("At least one zone must be configured.");
            return;
        }

        // Check for unique zone names
        var duplicateNames = zones
            .GroupBy(z => z.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateName in duplicateNames)
        {
            failures.Add($"Duplicate zone name found: '{duplicateName}'. Zone names must be unique.");
        }

        // Check for unique sinks
        var duplicateSinks = zones
            .GroupBy(z => z.Sink, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateSink in duplicateSinks)
        {
            failures.Add($"Duplicate zone sink found: '{duplicateSink}'. Zone sinks must be unique.");
        }

        // Validate KNX configuration for each zone
        foreach (var zone in zones.Where(z => z.Knx.Enabled))
        {
            if (zone.Knx.Volume == null && zone.Knx.VolumeStatus == null)
            {
                failures.Add($"Zone '{zone.Name}' has KNX enabled but no volume control addresses configured.");
            }
        }
    }

    private static void ValidateClients(List<ClientConfig> clients, List<ZoneConfig> zones, List<string> failures)
    {
        if (clients.Count == 0)
        {
            failures.Add("At least one client must be configured.");
            return;
        }

        // Check for unique client names
        var duplicateNames = clients
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateName in duplicateNames)
        {
            failures.Add($"Duplicate client name found: '{duplicateName}'. Client names must be unique.");
        }

        // Check for unique MAC addresses (if provided)
        var duplicateMacs = clients
            .Where(c => !string.IsNullOrEmpty(c.Mac))
            .GroupBy(c => c.Mac!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateMac in duplicateMacs)
        {
            failures.Add($"Duplicate client MAC address found: '{duplicateMac}'. MAC addresses must be unique.");
        }

        // Validate default zone references
        var maxZoneIndex = zones.Count;
        var invalidZoneClients = clients.Where(c => c.DefaultZone < 1 || c.DefaultZone > maxZoneIndex);

        foreach (var client in invalidZoneClients)
        {
            failures.Add($"Client '{client.Name}' references invalid default zone {client.DefaultZone}. Valid range: 1-{maxZoneIndex}.");
        }
    }

    private static void ValidateRadioStations(List<RadioStationConfig> stations, List<string> failures)
    {
        // Check for unique radio station names
        var duplicateNames = stations
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateName in duplicateNames)
        {
            failures.Add($"Duplicate radio station name found: '{duplicateName}'. Radio station names must be unique.");
        }

        // Validate URLs
        foreach (var station in stations)
        {
            if (!Uri.TryCreate(station.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                failures.Add($"Radio station '{station.Name}' has invalid URL: '{station.Url}'. Must be a valid HTTP/HTTPS URL.");
            }
        }
    }
}
```

## 10.6 Dependency Injection Setup

### 10.6.1 Configuration Registration

```csharp
// --- /Worker/Program.cs ---
namespace SnapDog2.Worker;

using EnvoyConfig;
using EnvoyConfig.Conversion;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Configuration.Converters;
using SnapDog2.Core.Configuration.Validation;

var builder = WebApplication.CreateBuilder(args);

// Set global prefix for all EnvoyConfig environment variables
EnvConfig.GlobalPrefix = "SNAPDOG_";

// Register custom type converters
TypeConverterRegistry.RegisterConverter(typeof(KnxAddress), new KnxAddressConverter());
TypeConverterRegistry.RegisterConverter(typeof(KnxAddress?), new KnxAddressConverter());

// Register the root configuration
var snapDogConfig = EnvConfig.Get<SnapDogConfiguration>();
builder.Services.AddSingleton(snapDogConfig);

// Register individual configuration sections for easier injection
builder.Services.AddSingleton(snapDogConfig.System);
builder.Services.AddSingleton(snapDogConfig.Telemetry);
builder.Services.AddSingleton(snapDogConfig.Api);
builder.Services.AddSingleton(snapDogConfig.Services);

// Register configuration validators
builder.Services.AddSingleton<IValidateOptions<SnapDogConfiguration>, SnapDogConfigurationValidator>();

// Validate configuration at startup
builder.Services.AddOptions<SnapDogConfiguration>()
    .Configure(options =>
    {
        // Copy values from the EnvoyConfig instance
        options.System = snapDogConfig.System;
        options.Telemetry = snapDogConfig.Telemetry;
        options.Api = snapDogConfig.Api;
        options.Services = snapDogConfig.Services;
        options.Zones = snapDogConfig.Zones;
        options.Clients = snapDogConfig.Clients;
        options.RadioStations = snapDogConfig.RadioStations;
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// Additional startup validation
var logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    var config = app.Services.GetRequiredService<SnapDogConfiguration>();
    var validator = app.Services.GetRequiredService<IValidateOptions<SnapDogConfiguration>>();

    var validationResult = validator.Validate(null, config);
    if (validationResult.Failed)
    {
        throw new InvalidOperationException($"Configuration validation failed: {string.Join("; ", validationResult.Failures)}");
    }

    logger.LogInformation("Configuration validation completed successfully. Loaded {ZoneCount} zones, {ClientCount} clients, and {RadioStationCount} radio stations.",
        config.Zones.Count, config.Clients.Count, config.RadioStations.Count);
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Configuration validation failed");
    throw;
}

app.Run();
```

## 10.7 Configuration Benefits

### 10.7.1 Type Safety & Validation

- **Compile-time checking**: Configuration properties are strongly typed
- **Automatic validation**: Data annotations and custom validators catch errors early
- **Null safety**: Nullable reference types prevent null reference exceptions
- **Range validation**: Numeric properties have range constraints

### 10.7.2 Developer Experience

- **IntelliSense support**: Full IDE support for configuration properties
- **Self-documenting**: XML documentation on all configuration classes
- **Clear structure**: Hierarchical organization mirrors environment variable structure
- **Easy testing**: Configuration objects can be easily mocked and tested

### 10.7.3 Operational Benefits

- **Fail-fast startup**: Configuration errors are caught immediately during application startup
- **Clear error messages**: Detailed validation messages for troubleshooting
- **Container optimized**: Static configuration perfect for containerized deployments
- **Environment agnostic**: Same configuration structure works across all environments

### 10.7.4 Maintainability

- **Single source of truth**: All configuration in one unified structure
- **Extensible**: Easy to add new configuration properties without breaking existing code
- **Backward compatible**: EnvoyConfig handles missing environment variables gracefully
- **Version controlled**: Configuration schema evolves with the codebase

This modern configuration system provides a robust foundation for SnapDog2's deployment and operational requirements while maintaining developer productivity and system reliability.
