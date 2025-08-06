using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration;
using SnapDog2.Extensions;
using SnapDog2.Helpers;

namespace SnapDog2.Services;

/// <summary>
/// Service that logs detailed information during application startup
/// </summary>
public class StartupInformationService : IHostedService
{
    private readonly ILogger<StartupInformationService> _logger;
    private readonly SnapDogConfiguration _config;

    public StartupInformationService(ILogger<StartupInformationService> logger, IOptions<SnapDogConfiguration> config)
    {
        this._logger = logger;
        this._config = config.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.LogStartupBanner();
        this.LogApplicationVersion();
        this.LogGitVersionInformation();
        this.LogRuntimeInformation();
        this.LogLoadedAssemblies();
        this.LogEnvironmentInformation();
        this.LogConfiguration();
        this.LogEndBanner();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void LogStartupBanner()
    {
        this._logger.LogInformation("════════════════════════════════");
        this._logger.LogInformation("💑💑💑 SnapDog2 starting... 💑💑💑");
        this._logger.LogInformation("════════════════════════════════");
    }

    private void LogEndBanner()
    {
        this._logger.LogInformation("════════════════════════════════");
        this._logger.LogInformation("SnapDog2 startup logging done.");
        this._logger.LogInformation("════════════════════════════════");
    }

    private void LogGitVersionInformation()
    {
        var gitVersion = GitVersionHelper.GetVersionInfo();

        this._logger.LogInformation("📋 GitVersion Information:");
        this._logger.LogInformation("   Version: {SemVer}", gitVersion.SemVer);
        this._logger.LogInformation("   Full Version: {FullSemVer}", gitVersion.FullSemVer);
        this._logger.LogInformation(
            "   Informational Version: {InformationalVersion}",
            gitVersion.InformationalVersion
        );
        this._logger.LogInformation("   Assembly Version: {AssemblySemVer}", gitVersion.AssemblySemVer);
        this._logger.LogInformation("   File Version: {AssemblySemFileVer}", gitVersion.AssemblySemFileVer);
        this._logger.LogInformation("   Branch: {BranchName}", gitVersion.BranchName);
        this._logger.LogInformation("   Commit: {ShortSha} ({CommitDate})", gitVersion.ShortSha, gitVersion.CommitDate);
        this._logger.LogInformation(
            "   Commits Since Version Source: {CommitsSinceVersionSource}",
            gitVersion.CommitsSinceVersionSource
        );

        if (!string.IsNullOrEmpty(gitVersion.PreReleaseLabel))
        {
            this._logger.LogInformation("   Pre-release: {PreReleaseTag}", gitVersion.PreReleaseTag);
        }

        if (gitVersion.UncommittedChanges > 0)
        {
            this._logger.LogInformation(
                "   ⚠️  Uncommitted Changes: {UncommittedChanges}",
                gitVersion.UncommittedChanges
            );
        }

        this._logger.LogInformation("   Build Metadata: {FullBuildMetaData}", gitVersion.FullBuildMetaData);
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

        this._logger.LogInformation("🚀 Application Information:");
        this._logger.LogInformation("   Name: {ApplicationName}", assembly.GetName().Name);
        this._logger.LogInformation("   Version: {Version}", version?.ToString() ?? "Unknown");
        this._logger.LogInformation(
            "   Informational Version: {InformationalVersion}",
            informationalVersion ?? "Unknown"
        );
        this._logger.LogInformation("   File Version: {FileVersion}", fileVersion ?? "Unknown");
        this._logger.LogInformation(
            "   Build Date: {BuildDate}",
            buildDate?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Unknown"
        );
        this._logger.LogInformation("   Build Machine: {BuildMachine}", buildMachine ?? "Unknown");
        this._logger.LogInformation("   Build User: {BuildUser}", buildUser ?? "Unknown");
        this._logger.LogInformation("   Location: {Location}", assembly.Location);
    }

    private void LogRuntimeInformation()
    {
        this._logger.LogInformation("⚙️  Runtime Information:");
        this._logger.LogInformation("   .NET Version: {DotNetVersion}", Environment.Version);
        this._logger.LogInformation("   Runtime Version: {RuntimeVersion}", RuntimeInformation.FrameworkDescription);
        this._logger.LogInformation("   OS: {OperatingSystem}", RuntimeInformation.OSDescription);
        this._logger.LogInformation("   Architecture: {Architecture}", RuntimeInformation.OSArchitecture);
        this._logger.LogInformation(
            "   Process Architecture: {ProcessArchitecture}",
            RuntimeInformation.ProcessArchitecture
        );
        this._logger.LogInformation("   Working Directory: {WorkingDirectory}", Environment.CurrentDirectory);
        this._logger.LogInformation("   Machine Name: {MachineName}", Environment.MachineName);
        this._logger.LogInformation("   User Name: {UserName}", Environment.UserName);
        this._logger.LogInformation("   Process ID: {ProcessId}", Environment.ProcessId);
        this._logger.LogInformation("   Processor Count: {ProcessorCount}", Environment.ProcessorCount);
    }

    private void LogLoadedAssemblies()
    {
        this._logger.LogInformation("📚 Libraries and Dependencies:");

        var assemblies = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .OrderBy(a => a.GetName().Name)
            .ToList();

        var applicationAssemblies = assemblies.Where(a => a.GetName().Name?.StartsWith("SnapDog") == true).ToList();

        var thirdPartyAssemblies = assemblies
            .Where(a =>
                !a.GetName().Name?.StartsWith("System") == true
                && !a.GetName().Name?.StartsWith("Microsoft") == true
                && !a.GetName().Name?.StartsWith("SnapDog") == true
            )
            .ToList();

        var systemAssemblies = assemblies
            .Where(a =>
                a.GetName().Name?.StartsWith("System") == true || a.GetName().Name?.StartsWith("Microsoft") == true
            )
            .ToList();

        this._logger.LogInformation("  📚 Application Assemblies:");

        if (applicationAssemblies.Count != 0)
        {
            foreach (var assembly in applicationAssemblies)
            {
                var name = assembly.GetName().Name ?? "Unknown";
                var version = assembly.GetName().Version?.ToString() ?? "Unknown";
                var location = string.IsNullOrEmpty(assembly.Location)
                    ? "In Memory"
                    : Path.GetFileName(assembly.Location);
                if (assembly != null)
                {
                    this.LogAssemblyInfo(assembly, "     🚀");
                }
            }
        }

        if (thirdPartyAssemblies.Any())
        {
            this._logger.LogInformation("  📚 Third-Party Assemblies:");
            foreach (var assembly in thirdPartyAssemblies)
            {
                var name = assembly.GetName().Name ?? "Unknown";
                var version = assembly.GetName().Version?.ToString() ?? "Unknown";
                var location = string.IsNullOrEmpty(assembly.Location)
                    ? "In Memory"
                    : Path.GetFileName(assembly.Location);
                this.LogAssemblyInfo(assembly, "     📱");
            }
        }

        if (systemAssemblies.Any())
        {
            this._logger.LogInformation("  📚 System Assemblies:");
            foreach (var assembly in systemAssemblies.Take(10)) // Limit to first 10 to avoid spam
            {
                var name = assembly.GetName().Name ?? "Unknown";
                var version = assembly.GetName().Version?.ToString() ?? "Unknown";
                var location = string.IsNullOrEmpty(assembly.Location)
                    ? "In Memory"
                    : Path.GetFileName(assembly.Location);
                this.LogAssemblyInfo(assembly, "     🔧");
            }
        }
    }

    private void LogAssemblyInfo(Assembly assembly, string prefix)
    {
        var name = assembly.GetName();
        var version = name.Version?.ToString() ?? "Unknown";
        var location = assembly.GetSafeLocation();

        this._logger.LogInformation("   {Prefix} {Name} v{Version} ({Location})", prefix, name.Name, version, location);
    }

    private void LogEnvironmentInformation()
    {
        this._logger.LogInformation("🌍 Environment Information:");
        this._logger.LogInformation(
            "   DOTNET_ENVIRONMENT: {Environment}",
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Not Set"
        );
        this._logger.LogInformation(
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

        if (snapdogEnvVars.Count != 0)
        {
            this._logger.LogInformation("   SnapDog2 Environment Variables:");
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

                this._logger.LogInformation("     {Key}: {Value}", key, value ?? "Not Set");
            }
        }
        else
        {
            this._logger.LogInformation("   No SnapDog2-specific environment variables found");
        }
    }

    private void LogConfiguration()
    {
        this._logger.LogInformation("⚙️  SnapDog2 Configuration:");

        // System Configuration
        this._logger.LogInformation("   📋 System:");
        this._logger.LogInformation("     Environment: {Environment}", this._config.System.Environment);
        this._logger.LogInformation("     Log Level: {LogLevel}", this._config.System.LogLevel);
        this._logger.LogInformation("     Debug Enabled: {DebugEnabled}", this._config.System.DebugEnabled);
        this._logger.LogInformation(
            "     Health Checks Enabled: {HealthChecksEnabled}",
            this._config.System.HealthChecksEnabled
        );
        this._logger.LogInformation(
            "     Health Checks Timeout: {HealthChecksTimeout}s",
            this._config.System.HealthChecksTimeout
        );

        // Services Configuration
        this._logger.LogInformation("   🔌 Services:");

        // Snapcast
        this._logger.LogInformation(
            "     Snapcast: {SnapcastAddress}:{SnapcastPort} (Timeout: {SnapcastTimeout}s)",
            this._config.Services.Snapcast.Address,
            this._config.Services.Snapcast.JsonRpcPort,
            this._config.Services.Snapcast.Timeout
        );
        this._logger.LogInformation(
            "       Auto Reconnect: {SnapcastAutoReconnect}, Interval: {SnapcastReconnectInterval}s",
            this._config.Services.Snapcast.AutoReconnect,
            this._config.Services.Snapcast.ReconnectInterval
        );

        // MQTT
        if (this._config.Services.Mqtt.Enabled)
        {
            this._logger.LogInformation(
                "     MQTT: {MqttBroker}:{MqttPort} (Client: {MqttClientId})",
                this._config.Services.Mqtt.BrokerAddress,
                this._config.Services.Mqtt.Port,
                this._config.Services.Mqtt.ClientId
            );
            this._logger.LogInformation(
                "       SSL: {MqttSslEnabled}, Keep Alive: {MqttKeepAlive}s",
                this._config.Services.Mqtt.SslEnabled,
                this._config.Services.Mqtt.KeepAlive
            );
            this._logger.LogInformation(
                "       Username: {MqttUsername}",
                string.IsNullOrEmpty(this._config.Services.Mqtt.Username) ? "Not configured" : "***"
            );
        }
        else
        {
            this._logger.LogInformation("     MQTT: Disabled");
        }

        // KNX
        if (this._config.Services.Knx.Enabled)
        {
            var connectionType = string.IsNullOrEmpty(this._config.Services.Knx.Gateway) ? "USB" : "IP Tunneling";
            this._logger.LogInformation(
                "     KNX: {ConnectionType} - {KnxGateway}:{KnxPort} (Timeout: {KnxTimeout}s)",
                connectionType,
                this._config.Services.Knx.Gateway ?? "Auto-detect USB",
                this._config.Services.Knx.Port,
                this._config.Services.Knx.Timeout
            );
            this._logger.LogInformation(
                "       Auto Reconnect: {KnxAutoReconnect}",
                this._config.Services.Knx.AutoReconnect
            );

            // Count KNX-enabled zones and clients
            var knxZoneCount = this._config.Zones.Count(z => z.Knx.Enabled);
            var knxClientCount = this._config.Clients.Count(c => c.Knx.Enabled);
            this._logger.LogInformation(
                "       KNX Integration: {ZoneCount} zones, {ClientCount} clients",
                knxZoneCount,
                knxClientCount
            );
        }
        else
        {
            this._logger.LogInformation("     KNX: Disabled");
        }

        // Subsonic
        if (this._config.Services.Subsonic.Enabled)
        {
            this._logger.LogInformation(
                "     Subsonic: {SubsonicUrl} (User: {SubsonicUser}, Timeout: {SubsonicTimeout}ms)",
                this._config.Services.Subsonic.Url ?? "Not configured",
                string.IsNullOrEmpty(this._config.Services.Subsonic.Username) ? "Not configured" : "***",
                this._config.Services.Subsonic.Timeout
            );
        }
        else
        {
            this._logger.LogInformation("     Subsonic: Disabled");
        }

        // Snapcast Server Configuration
        this._logger.LogInformation("   🎵 Snapcast Server:");
        this._logger.LogInformation(
            "     Codec: {Codec}, Sample Format: {SampleFormat}",
            this._config.SnapcastServer.Codec,
            this._config.SnapcastServer.SampleFormat
        );
        this._logger.LogInformation(
            "     Ports - Web: {WebServerPort}, WebSocket: {WebSocketPort}, JSON-RPC: {JsonRpcPort}",
            this._config.SnapcastServer.WebServerPort,
            this._config.SnapcastServer.WebSocketPort,
            this._config.SnapcastServer.JsonRpcPort
        );

        // Zones Configuration
        this._logger.LogInformation("   🏠 Zones ({ZoneCount} configured):", this._config.Zones.Count);
        foreach (var (zone, index) in this._config.Zones.Select((z, i) => (z, i + 1)))
        {
            this._logger.LogInformation("     Zone {ZoneIndex}: {ZoneName} -> {ZoneSink}", index, zone.Name, zone.Sink);
            if (!string.IsNullOrEmpty(zone.Mqtt.BaseTopic))
            {
                this._logger.LogInformation("       MQTT Base Topic: {ZoneMqttBaseTopic}", zone.Mqtt.BaseTopic);
            }

            if (zone.Knx.Enabled)
            {
                this._logger.LogInformation("       KNX Enabled");
            }
        }

        // Clients Configuration
        this._logger.LogInformation("   📱 Clients ({ClientCount} configured):", this._config.Clients.Count);
        foreach (var (client, index) in this._config.Clients.Select((c, i) => (c, i + 1)))
        {
            this._logger.LogInformation(
                "     Client {ClientIndex}: {ClientName} (Zone {DefaultZone})",
                index,
                client.Name,
                client.DefaultZone
            );
            if (!string.IsNullOrEmpty(client.Mac))
            {
                this._logger.LogInformation("       MAC: {ClientMac}", client.Mac);
            }

            if (!string.IsNullOrEmpty(client.Mqtt.BaseTopic))
            {
                this._logger.LogInformation("       MQTT Base Topic: {ClientMqttBaseTopic}", client.Mqtt.BaseTopic);
            }

            if (client.Knx.Enabled)
            {
                this._logger.LogInformation("       KNX Enabled");
            }
        }

        // Radio Stations Configuration
        this._logger.LogInformation(
            "   📻 Radio Stations ({RadioStationCount} configured):",
            this._config.RadioStations.Count
        );
        foreach (var (station, index) in this._config.RadioStations.Select((s, i) => (s, i + 1)))
        {
            this._logger.LogInformation(
                "     Radio {RadioIndex}: {RadioName} -> {RadioUrl}",
                index,
                station.Name,
                station.Url
            );
        }

        // API Configuration
        this._logger.LogInformation("   🌐 API Server:");
        if (this._config.Api.Enabled)
        {
            this._logger.LogInformation("     Status: Enabled on port {ApiPort}", this._config.Api.Port);
            this._logger.LogInformation(
                "     Authentication: {AuthEnabled} ({ApiKeysCount} API keys configured)",
                this._config.Api.AuthEnabled ? "Enabled" : "Disabled",
                this._config.Api.ApiKeys.Count
            );

            if (this._config.Api.AuthEnabled && this._config.Api.ApiKeys.Count == 0)
            {
                this._logger.LogWarning("     ⚠️  Authentication is enabled but no API keys are configured!");
            }
        }
        else
        {
            this._logger.LogInformation("     Status: Disabled");
            this._logger.LogInformation("     No HTTP endpoints will be available");
        }

        // Telemetry Configuration
        if (this._config.Telemetry.Enabled)
        {
            this._logger.LogInformation(
                "   📊 Telemetry: Enabled (Service: {ServiceName}, Sampling: {SamplingRate})",
                this._config.Telemetry.ServiceName,
                this._config.Telemetry.SamplingRate
            );

            var telemetryTargets = new List<string>();
            if (this._config.Telemetry.Otlp.Enabled)
            {
                telemetryTargets.Add($"OTLP: {this._config.Telemetry.Otlp.Endpoint}");
            }

            if (this._config.Telemetry.Prometheus.Enabled)
            {
                telemetryTargets.Add(
                    $"Prometheus: :{this._config.Telemetry.Prometheus.Port}{this._config.Telemetry.Prometheus.Path}"
                );
            }

            if (this._config.Telemetry.Seq.Enabled && !string.IsNullOrEmpty(this._config.Telemetry.Seq.Url))
            {
                telemetryTargets.Add($"Seq: {this._config.Telemetry.Seq.Url}");
            }

            if (telemetryTargets.Count != 0)
            {
                this._logger.LogInformation("     Targets: {TelemetryTargets}", string.Join(", ", telemetryTargets));
            }
        }
        else
        {
            this._logger.LogInformation("   📊 Telemetry: Disabled");
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
