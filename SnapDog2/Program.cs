using System.Reflection;
using EnvoyConfig;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using SnapDog2.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Load .env file if it exists
LoadDotEnvFile();

// Set global prefix for all EnvoyConfig environment variables
EnvConfig.GlobalPrefix = "SNAPDOG_";

// Load configuration from environment variables using EnvoyConfig
SnapDogConfiguration snapDogConfig;
try
{
    snapDogConfig = EnvConfig.Load<SnapDogConfiguration>();
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to load configuration: {ex.Message}");
    throw;
}

// Configure Serilog based on configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(Enum.Parse<LogEventLevel>(snapDogConfig.System.LogLevel, true))
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.File(
        path: "logs/snapdog-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 31,
        fileSizeLimitBytes: 100 * 1024 * 1024
    )
    .CreateLogger();

try
{
    Log.Information("Starting SnapDog2 application");

    // Print configuration on startup
    PrintConfiguration(snapDogConfig);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Register configuration
    builder.Services.AddSingleton(snapDogConfig);
    builder.Services.AddSingleton(snapDogConfig.System);
    builder.Services.AddSingleton(snapDogConfig.Telemetry);
    builder.Services.AddSingleton(snapDogConfig.Api);
    builder.Services.AddSingleton(snapDogConfig.Services);
    builder.Services.AddSingleton(snapDogConfig.SnapcastServer);

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add health checks
    if (snapDogConfig.System.HealthChecksEnabled)
    {
        var healthChecksBuilder = builder.Services.AddHealthChecks();

        // Add basic application health check
        healthChecksBuilder.AddCheck(
            "self",
            () => HealthCheckResult.Healthy("Application is running"),
            tags: ["ready", "live"]
        );

        // Add external service health checks based on configuration
        if (snapDogConfig.Services.Snapcast.Enabled)
        {
            healthChecksBuilder.AddTcpHealthCheck(
                options =>
                {
                    options.AddHost(snapDogConfig.Services.Snapcast.Address, snapDogConfig.Services.Snapcast.Port);
                },
                name: "snapcast",
                tags: ["ready"]
            );
        }

        if (snapDogConfig.Services.ServicesMqtt.Enabled)
        {
            healthChecksBuilder.AddTcpHealthCheck(
                options =>
                {
                    options.AddHost(snapDogConfig.Services.ServicesMqtt.BrokerAddress, snapDogConfig.Services.ServicesMqtt.Port);
                },
                name: "mqtt",
                tags: ["ready"]
            );
        }
    }

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRouting();
    app.MapControllers();

    Log.Information("SnapDog2 application configured successfully");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static void LoadDotEnvFile()
{
    var envFile = ".env";
    if (!File.Exists(envFile))
        return;

    Console.WriteLine($"Loading environment variables from {envFile}");

    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmedLine = line.Trim();

        // Skip empty lines and comments
        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
            continue;

        var parts = trimmedLine.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var value = parts[1].Trim();

            // Remove quotes if present
            if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            // Only set if not already set (environment variables take precedence)
            if (Environment.GetEnvironmentVariable(key) == null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}

static void PrintConfiguration(SnapDogConfiguration config)
{
    Log.Information("=== SnapDog2 Configuration ===");

    // System Configuration
    Log.Information("System Configuration:");
    Log.Information("  Environment: {Environment}", config.System.Environment);
    Log.Information("  Log Level: {LogLevel}", config.System.LogLevel);
    Log.Information("  Debug Enabled: {DebugEnabled}", config.System.DebugEnabled);
    Log.Information("  Health Checks Enabled: {HealthChecksEnabled}", config.System.HealthChecksEnabled);
    Log.Information("  Health Checks Timeout: {HealthChecksTimeout}s", config.System.HealthChecksTimeout);
    Log.Information("  Health Checks Tags: {HealthChecksTags}", config.System.HealthChecksTags);
    Log.Information("  MQTT Base Topic: {MqttBaseTopic}", config.System.MqttBaseTopic);
    Log.Information("  MQTT Status Topic: {MqttStatusTopic}", config.System.MqttStatusTopic);

    // Telemetry Configuration
    Log.Information("Telemetry Configuration:");
    Log.Information("  Enabled: {TelemetryEnabled}", config.Telemetry.Enabled);
    Log.Information("  Service Name: {ServiceName}", config.Telemetry.ServiceName);
    Log.Information("  Sampling Rate: {SamplingRate}", config.Telemetry.SamplingRate);
    Log.Information("  OTLP Enabled: {OtlpEnabled}", config.Telemetry.Otlp.Enabled);
    if (config.Telemetry.Otlp.Enabled)
    {
        Log.Information("  OTLP Endpoint: {OtlpEndpoint}", config.Telemetry.Otlp.Endpoint);
        Log.Information(
            "  OTLP Agent: {OtlpAgent}:{OtlpPort}",
            config.Telemetry.Otlp.AgentAddress,
            config.Telemetry.Otlp.AgentPort
        );
    }
    Log.Information("  Prometheus Enabled: {PrometheusEnabled}", config.Telemetry.Prometheus.Enabled);
    if (config.Telemetry.Prometheus.Enabled)
    {
        Log.Information("  Prometheus Path: {PrometheusPath}", config.Telemetry.Prometheus.Path);
        Log.Information("  Prometheus Port: {PrometheusPort}", config.Telemetry.Prometheus.Port);
    }
    Log.Information("  TelemetrySeq Enabled: {SeqEnabled}", config.Telemetry.TelemetrySeq.Enabled);
    if (config.Telemetry.TelemetrySeq.Enabled && !string.IsNullOrEmpty(config.Telemetry.TelemetrySeq.Url))
    {
        Log.Information("  TelemetrySeq URL: {SeqUrl}", config.Telemetry.TelemetrySeq.Url);
    }

    // API Configuration
    Log.Information("API Configuration:");
    Log.Information("  Authentication Enabled: {AuthEnabled}", config.Api.AuthEnabled);
    Log.Information("  API Keys Count: {ApiKeysCount}", config.Api.ApiKeys.Count);

    // Services Configuration
    Log.Information("Services Configuration:");

    // Snapcast
    Log.Information("  Snapcast:");
    Log.Information("    Enabled: {SnapcastEnabled}", config.Services.Snapcast.Enabled);
    if (config.Services.Snapcast.Enabled)
    {
        Log.Information(
            "    Address: {SnapcastAddress}:{SnapcastPort}",
            config.Services.Snapcast.Address,
            config.Services.Snapcast.Port
        );
        Log.Information("    HTTP Port: {SnapcastHttpPort}", config.Services.Snapcast.HttpPort);
        Log.Information(
            "    Base URL: {SnapcastBaseUrl}",
            string.IsNullOrEmpty(config.Services.Snapcast.BaseUrl) ? "(empty)" : config.Services.Snapcast.BaseUrl
        );
        Log.Information("    Timeout: {SnapcastTimeout}s", config.Services.Snapcast.Timeout);
        Log.Information("    Auto Reconnect: {SnapcastAutoReconnect}", config.Services.Snapcast.AutoReconnect);
        Log.Information(
            "    Reconnect Interval: {SnapcastReconnectInterval}s",
            config.Services.Snapcast.ReconnectInterval
        );
    }

    // MQTT
    Log.Information("  MQTT:");
    Log.Information("    Enabled: {MqttEnabled}", config.Services.ServicesMqtt.Enabled);
    if (config.Services.ServicesMqtt.Enabled)
    {
        Log.Information(
            "    Broker: {MqttBroker}:{MqttPort}",
            config.Services.ServicesMqtt.BrokerAddress,
            config.Services.ServicesMqtt.Port
        );
        Log.Information("    Client ID: {MqttClientId}", config.Services.ServicesMqtt.ClientId);
        Log.Information("    SSL Enabled: {MqttSslEnabled}", config.Services.ServicesMqtt.SslEnabled);
        Log.Information(
            "    Username: {MqttUsername}",
            string.IsNullOrEmpty(config.Services.ServicesMqtt.Username) ? "Not configured" : "***"
        );
        Log.Information("    Keep Alive: {MqttKeepAlive}s", config.Services.ServicesMqtt.KeepAlive);
    }

    // KNX
    Log.Information("  KNX:");
    Log.Information("    Enabled: {KnxEnabled}", config.Services.ServicesKnx.Enabled);
    if (config.Services.ServicesKnx.Enabled)
    {
        Log.Information(
            "    Gateway: {KnxGateway}:{KnxPort}",
            config.Services.ServicesKnx.Gateway ?? "Not configured",
            config.Services.ServicesKnx.Port
        );
        Log.Information("    Timeout: {KnxTimeout}s", config.Services.ServicesKnx.Timeout);
        Log.Information("    Auto Reconnect: {KnxAutoReconnect}", config.Services.ServicesKnx.AutoReconnect);
    }

    // ServicesSubsonic
    Log.Information("  ServicesSubsonic:");
    Log.Information("    Enabled: {SubsonicEnabled}", config.Services.ServicesSubsonic.Enabled);
    if (config.Services.ServicesSubsonic.Enabled)
    {
        Log.Information("    URL: {SubsonicUrl}", config.Services.ServicesSubsonic.Url ?? "Not configured");
        Log.Information(
            "    Username: {SubsonicUsername}",
            string.IsNullOrEmpty(config.Services.ServicesSubsonic.Username) ? "Not configured" : "***"
        );
        Log.Information("    Timeout: {SubsonicTimeout}ms", config.Services.ServicesSubsonic.Timeout);
    }

    // Snapcast Server Configuration
    Log.Information("Snapcast Server Configuration:");
    Log.Information("  Codec: {SnapcastCodec}", config.SnapcastServer.Codec);
    Log.Information("  Sample Format: {SnapcastSampleFormat}", config.SnapcastServer.SampleFormat);
    Log.Information("  Web Server Port: {SnapcastWebServerPort}", config.SnapcastServer.WebServerPort);
    Log.Information("  WebSocket Port: {SnapcastWebSocketPort}", config.SnapcastServer.WebSocketPort);

    // Zones Configuration
    Log.Information("Zones Configuration:");
    Log.Information("  Zone Count: {ZoneCount}", config.Zones.Count);
    foreach (var (zone, index) in config.Zones.Select((z, i) => (z, i + 1)))
    {
        Log.Information("  Zone {ZoneIndex}: {ZoneName} -> {ZoneSink}", index, zone.Name, zone.Sink);
        if (!string.IsNullOrEmpty(zone.Mqtt.BaseTopic))
        {
            Log.Information("    MQTT Base Topic: {ZoneMqttBaseTopic}", zone.Mqtt.BaseTopic);
        }
        if (zone.Knx.Enabled)
        {
            Log.Information(
                "    KNX Enabled with {KnxAddressCount} addresses configured",
                CountNonNullKnxAddresses(zone.Knx)
            );
        }
    }

    // Clients Configuration
    Log.Information("Clients Configuration:");
    Log.Information("  Client Count: {ClientCount}", config.Clients.Count);
    foreach (var (client, index) in config.Clients.Select((c, i) => (c, i + 1)))
    {
        Log.Information(
            "  Client {ClientIndex}: {ClientName} (Zone {DefaultZone})",
            index,
            client.Name,
            client.DefaultZone
        );
        if (!string.IsNullOrEmpty(client.Mac))
        {
            Log.Information("    MAC: {ClientMac}", client.Mac);
        }
        if (!string.IsNullOrEmpty(client.Mqtt.BaseTopic))
        {
            Log.Information("    MQTT Base Topic: {ClientMqttBaseTopic}", client.Mqtt.BaseTopic);
        }
        if (client.Knx.Enabled)
        {
            Log.Information("    KNX Enabled");
        }
    }

    // Radio Stations Configuration
    Log.Information("Radio Stations Configuration:");
    Log.Information("  Radio Station Count: {RadioStationCount}", config.RadioStations.Count);
    foreach (var (station, index) in config.RadioStations.Select((s, i) => (s, i + 1)))
    {
        Log.Information("  Radio {RadioIndex}: {RadioName} -> {RadioUrl}", index, station.Name, station.Url);
    }

    Log.Information("=== End Configuration ===");
}

static int CountNonNullKnxAddresses(ZoneKnxConfig knx)
{
    var properties = typeof(ZoneKnxConfig)
        .GetProperties()
        .Where(p => p.PropertyType == typeof(string) && p.Name != "Enabled");

    return properties.Count(p => !string.IsNullOrEmpty((string?)p.GetValue(knx)));
}

// Make Program class accessible to tests
public partial class Program { }
