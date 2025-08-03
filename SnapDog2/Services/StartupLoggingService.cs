using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration;
using SnapDog2.Extensions;

namespace SnapDog2.Services;

/// <summary>
/// Service that logs detailed information during application startup
/// </summary>
public class StartupLoggingService : IHostedService
{
    private readonly ILogger<StartupLoggingService> _logger;
    private readonly SnapDogConfiguration _config;

    public StartupLoggingService(ILogger<StartupLoggingService> logger, IOptions<SnapDogConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        LogStartupBanner();
        LogApplicationVersion();
        LogRuntimeInformation();
        LogLoadedAssemblies();
        LogEnvironmentInformation();
        LogConfiguration();
        LogEndBanner();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void LogStartupBanner()
    {
        _logger.LogInformation("════════════════════════════════");
        _logger.LogInformation("💑💑💑 SnapDog2 starting... 💑💑💑");
        _logger.LogInformation("════════════════════════════════");
    }

    private void LogEndBanner()
    {
        _logger.LogInformation("════════════════════════════════");
        _logger.LogInformation("SnapDog2 startup logging done.");
        _logger.LogInformation("════════════════════════════════");
    }

    private void LogApplicationVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        var buildDate = assembly.GetBuildDate();
        var buildMachine = assembly.GetBuildMachine();
        var buildUser = assembly.GetBuildUser();

        _logger.LogInformation("🚀 Application Information:");
        _logger.LogInformation("   Name: {ApplicationName}", assembly.GetName().Name);
        _logger.LogInformation("   Version: {Version}", version?.ToString() ?? "Unknown");
        _logger.LogInformation("   Informational Version: {InformationalVersion}", informationalVersion ?? "Unknown");
        _logger.LogInformation("   File Version: {FileVersion}", fileVersion ?? "Unknown");
        _logger.LogInformation(
            "   Build Date: {BuildDate}",
            buildDate?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Unknown"
        );
        _logger.LogInformation("   Build Machine: {BuildMachine}", buildMachine ?? "Unknown");
        _logger.LogInformation("   Build User: {BuildUser}", buildUser ?? "Unknown");
        _logger.LogInformation("   Location: {Location}", assembly.Location);
    }

    private void LogRuntimeInformation()
    {
        _logger.LogInformation("⚙️  Runtime Information:");
        _logger.LogInformation("   .NET Version: {DotNetVersion}", Environment.Version);
        _logger.LogInformation("   Runtime Version: {RuntimeVersion}", RuntimeInformation.FrameworkDescription);
        _logger.LogInformation("   OS: {OperatingSystem}", RuntimeInformation.OSDescription);
        _logger.LogInformation("   Architecture: {Architecture}", RuntimeInformation.OSArchitecture);
        _logger.LogInformation(
            "   Process Architecture: {ProcessArchitecture}",
            RuntimeInformation.ProcessArchitecture
        );
        _logger.LogInformation("   Working Directory: {WorkingDirectory}", Environment.CurrentDirectory);
        _logger.LogInformation("   Machine Name: {MachineName}", Environment.MachineName);
        _logger.LogInformation("   User Name: {UserName}", Environment.UserName);
        _logger.LogInformation("   Process ID: {ProcessId}", Environment.ProcessId);
        _logger.LogInformation("   Processor Count: {ProcessorCount}", Environment.ProcessorCount);
    }

    private void LogLoadedAssemblies()
    {
        _logger.LogInformation("📚 Key Libraries and Dependencies:");

        var keyAssemblies = new[]
        {
            "SnapcastClient",
            "MQTTnet",
            "Microsoft.AspNetCore.App",
            "Microsoft.Extensions.Hosting",
            "Microsoft.Extensions.Logging",
            "Microsoft.Extensions.DependencyInjection",
            "Serilog",
            "Serilog.AspNetCore",
            "Newtonsoft.Json",
            "FluentValidation",
            "MediatR",
            "Swashbuckle.AspNetCore",
            "HealthChecks.Network",
            "Cortex.Mediator",
            "EnvoyConfig",
        };

        var loadedAssemblies = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .OrderBy(a => a.GetName().Name)
            .ToList();

        // Log key assemblies first
        foreach (var assemblyName in keyAssemblies)
        {
            var assembly = loadedAssemblies.FirstOrDefault(a =>
                a.GetName().Name?.StartsWith(assemblyName, StringComparison.OrdinalIgnoreCase) == true
            );

            if (assembly != null)
            {
                LogAssemblyInfo(assembly, "🔑");
            }
        }

        // Log other loaded assemblies (limited to avoid spam)
        _logger.LogInformation("📦 Other Notable Assemblies:");
        var otherAssemblies = loadedAssemblies
            .Where(a =>
                !keyAssemblies.Any(key => a.GetName().Name?.StartsWith(key, StringComparison.OrdinalIgnoreCase) == true)
            )
            .Where(a => !IsSystemAssembly(a))
            .Take(15); // Limit to avoid log spam

        foreach (var assembly in otherAssemblies)
        {
            LogAssemblyInfo(assembly, "  ");
        }

        _logger.LogInformation("   Total Loaded Assemblies: {TotalCount}", loadedAssemblies.Count);
    }

    private void LogAssemblyInfo(Assembly assembly, string prefix)
    {
        var name = assembly.GetName();
        var version = name.Version?.ToString() ?? "Unknown";
        var location = assembly.GetSafeLocation();

        _logger.LogInformation("   {Prefix} {Name} v{Version} ({Location})", prefix, name.Name, version, location);
    }

    private void LogEnvironmentInformation()
    {
        _logger.LogInformation("🌍 Environment Information:");
        _logger.LogInformation(
            "   DOTNET_ENVIRONMENT: {Environment}",
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Not Set"
        );
        _logger.LogInformation(
            "   ASPNETCORE_ENVIRONMENT: {Environment}",
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not Set"
        );

        // Log SnapDog2 specific environment variables
        var snapdogEnvVars = Environment
            .GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .Where(entry => entry.Key.ToString()?.StartsWith("SNAPDOG", StringComparison.OrdinalIgnoreCase) == true)
            .OrderBy(entry => entry.Key.ToString())
            .ToList();

        if (snapdogEnvVars.Any())
        {
            _logger.LogInformation("   SnapDog2 Environment Variables:");
            foreach (var envVar in snapdogEnvVars)
            {
                var key = envVar.Key.ToString();
                var value = envVar.Value?.ToString();

                // Mask sensitive values
                if (
                    key?.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) == true
                    || key?.Contains("SECRET", StringComparison.OrdinalIgnoreCase) == true
                    || key?.Contains("TOKEN", StringComparison.OrdinalIgnoreCase) == true
                )
                {
                    value = "***";
                }

                _logger.LogInformation("     {Key}: {Value}", key, value ?? "Not Set");
            }
        }
        else
        {
            _logger.LogInformation("   No SnapDog2-specific environment variables found");
        }
    }

    private void LogConfiguration()
    {
        _logger.LogInformation("⚙️  SnapDog2 Configuration:");

        // NOTE: EnvoyConfig nested list parsing issue - collections show 0 items despite environment variables being present
        // Environment variables like SNAPDOG_ZONE_1_NAME, SNAPDOG_CLIENT_1_NAME, SNAPDOG_RADIO_1_NAME exist
        // but EnvoyConfig NestedListPrefix/NestedListSuffix attributes are not parsing them into collections

        // System Configuration
        _logger.LogInformation("   📋 System:");
        _logger.LogInformation("     Environment: {Environment}", _config.System.Environment);
        _logger.LogInformation("     Log Level: {LogLevel}", _config.System.LogLevel);
        _logger.LogInformation("     Debug Enabled: {DebugEnabled}", _config.System.DebugEnabled);
        _logger.LogInformation("     Health Checks Enabled: {HealthChecksEnabled}", _config.System.HealthChecksEnabled);
        _logger.LogInformation(
            "     Health Checks Timeout: {HealthChecksTimeout}s",
            _config.System.HealthChecksTimeout
        );

        // Services Configuration
        _logger.LogInformation("   🔌 Services:");

        // Snapcast
        _logger.LogInformation(
            "     Snapcast: {SnapcastAddress}:{SnapcastPort} (Timeout: {SnapcastTimeout}s)",
            _config.Services.Snapcast.Address,
            _config.Services.Snapcast.JsonRpcPort,
            _config.Services.Snapcast.Timeout
        );
        _logger.LogInformation(
            "       Auto Reconnect: {SnapcastAutoReconnect}, Interval: {SnapcastReconnectInterval}s",
            _config.Services.Snapcast.AutoReconnect,
            _config.Services.Snapcast.ReconnectInterval
        );

        // MQTT
        if (_config.Services.Mqtt.Enabled)
        {
            _logger.LogInformation(
                "     MQTT: {MqttBroker}:{MqttPort} (Client: {MqttClientId})",
                _config.Services.Mqtt.BrokerAddress,
                _config.Services.Mqtt.Port,
                _config.Services.Mqtt.ClientId
            );
            _logger.LogInformation(
                "       SSL: {MqttSslEnabled}, Keep Alive: {MqttKeepAlive}s",
                _config.Services.Mqtt.SslEnabled,
                _config.Services.Mqtt.KeepAlive
            );
            _logger.LogInformation(
                "       Username: {MqttUsername}",
                string.IsNullOrEmpty(_config.Services.Mqtt.Username) ? "Not configured" : "***"
            );
        }
        else
        {
            _logger.LogInformation("     MQTT: Disabled");
        }

        // KNX
        if (_config.Services.Knx.Enabled)
        {
            _logger.LogInformation(
                "     KNX: {KnxGateway}:{KnxPort} (Timeout: {KnxTimeout}s)",
                _config.Services.Knx.Gateway ?? "Not configured",
                _config.Services.Knx.Port,
                _config.Services.Knx.Timeout
            );
            _logger.LogInformation("       Auto Reconnect: {KnxAutoReconnect}", _config.Services.Knx.AutoReconnect);
        }
        else
        {
            _logger.LogInformation("     KNX: Disabled");
        }

        // Subsonic
        if (_config.Services.Subsonic.Enabled)
        {
            _logger.LogInformation(
                "     Subsonic: {SubsonicUrl} (User: {SubsonicUser}, Timeout: {SubsonicTimeout}ms)",
                _config.Services.Subsonic.Url ?? "Not configured",
                string.IsNullOrEmpty(_config.Services.Subsonic.Username) ? "Not configured" : "***",
                _config.Services.Subsonic.Timeout
            );
        }
        else
        {
            _logger.LogInformation("     Subsonic: Disabled");
        }

        // Snapcast Server Configuration
        _logger.LogInformation("   🎵 Snapcast Server:");
        _logger.LogInformation(
            "     Codec: {Codec}, Sample Format: {SampleFormat}",
            _config.SnapcastServer.Codec,
            _config.SnapcastServer.SampleFormat
        );
        _logger.LogInformation(
            "     Ports - Web: {WebServerPort}, WebSocket: {WebSocketPort}, JSON-RPC: {JsonRpcPort}",
            _config.SnapcastServer.WebServerPort,
            _config.SnapcastServer.WebSocketPort,
            _config.SnapcastServer.JsonRpcPort
        );

        // Zones Configuration
        _logger.LogInformation("   🏠 Zones ({ZoneCount} configured):", _config.Zones.Count);
        foreach (var (zone, index) in _config.Zones.Select((z, i) => (z, i + 1)))
        {
            _logger.LogInformation("     Zone {ZoneIndex}: {ZoneName} -> {ZoneSink}", index, zone.Name, zone.Sink);
            if (!string.IsNullOrEmpty(zone.Mqtt.BaseTopic))
            {
                _logger.LogInformation("       MQTT Base Topic: {ZoneMqttBaseTopic}", zone.Mqtt.BaseTopic);
            }
            if (zone.Knx.Enabled)
            {
                _logger.LogInformation("       KNX Enabled");
            }
        }

        // Clients Configuration
        _logger.LogInformation("   📱 Clients ({ClientCount} configured):", _config.Clients.Count);
        foreach (var (client, index) in _config.Clients.Select((c, i) => (c, i + 1)))
        {
            _logger.LogInformation(
                "     Client {ClientIndex}: {ClientName} (Zone {DefaultZone})",
                index,
                client.Name,
                client.DefaultZone
            );
            if (!string.IsNullOrEmpty(client.Mac))
            {
                _logger.LogInformation("       MAC: {ClientMac}", client.Mac);
            }
            if (!string.IsNullOrEmpty(client.Mqtt.BaseTopic))
            {
                _logger.LogInformation("       MQTT Base Topic: {ClientMqttBaseTopic}", client.Mqtt.BaseTopic);
            }
            if (client.Knx.Enabled)
            {
                _logger.LogInformation("       KNX Enabled");
            }
        }

        // Radio Stations Configuration
        _logger.LogInformation("   📻 Radio Stations ({RadioStationCount} configured):", _config.RadioStations.Count);
        foreach (var (station, index) in _config.RadioStations.Select((s, i) => (s, i + 1)))
        {
            _logger.LogInformation(
                "     Radio {RadioIndex}: {RadioName} -> {RadioUrl}",
                index,
                station.Name,
                station.Url
            );
        }

        // API Configuration
        _logger.LogInformation("   🔐 API:");
        _logger.LogInformation(
            "     Authentication: {AuthEnabled} ({ApiKeysCount} API keys configured)",
            _config.Api.AuthEnabled ? "Enabled" : "Disabled",
            _config.Api.ApiKeys.Count
        );

        // Telemetry Configuration
        if (_config.Telemetry.Enabled)
        {
            _logger.LogInformation(
                "   📊 Telemetry: Enabled (Service: {ServiceName}, Sampling: {SamplingRate})",
                _config.Telemetry.ServiceName,
                _config.Telemetry.SamplingRate
            );

            var telemetryTargets = new List<string>();
            if (_config.Telemetry.Otlp.Enabled)
                telemetryTargets.Add($"OTLP: {_config.Telemetry.Otlp.Endpoint}");
            if (_config.Telemetry.Prometheus.Enabled)
                telemetryTargets.Add(
                    $"Prometheus: :{_config.Telemetry.Prometheus.Port}{_config.Telemetry.Prometheus.Path}"
                );
            if (_config.Telemetry.Seq.Enabled && !string.IsNullOrEmpty(_config.Telemetry.Seq.Url))
                telemetryTargets.Add($"Seq: {_config.Telemetry.Seq.Url}");

            if (telemetryTargets.Any())
            {
                _logger.LogInformation("     Targets: {TelemetryTargets}", string.Join(", ", telemetryTargets));
            }
        }
        else
        {
            _logger.LogInformation("   📊 Telemetry: Disabled");
        }
    }

    private static bool IsSystemAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? "";
        return name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
            || name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase);
    }
}
