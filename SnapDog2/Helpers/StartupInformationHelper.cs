using System.Reflection;
using System.Runtime.InteropServices;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Enums;
using SnapDog2.Extensions;

namespace SnapDog2.Helpers;

/// <summary>
/// Helper class for displaying comprehensive startup information.
/// Provides immediate startup feedback before service registrations begin.
/// Uses Console.WriteLine for clean output without logger prefixes.
/// </summary>
public static class StartupInformationHelper
{
    /// <summary>
    /// Shows comprehensive startup information immediately during application startup.
    /// This ensures startup info appears before service registrations for better user experience.
    /// </summary>
    /// <param name="config">The SnapDog2 configuration</param>
    public static void ShowStartupInformation(SnapDogConfiguration config)
    {
        ShowStartupBanner();
        ShowApplicationInformation();
        ShowGitVersionInformation();
        ShowRuntimeInformation();
        ShowLoadedAssemblies();
        ShowEnvironmentInformation();
        ShowConfiguration(config);
        ShowEndBanner();
    }

    private static void ShowStartupBanner()
    {
        Console.WriteLine("════════════════════════════════");
        Console.WriteLine("💑💑💑 SnapDog2 starting... 💑💑💑");
        Console.WriteLine("════════════════════════════════");
    }

    private static void ShowApplicationInformation()
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

        Console.WriteLine("🚀 Application Information:");
        Console.WriteLine($"   Name: {assembly.GetName().Name}");
        Console.WriteLine($"   Version: {version?.ToString() ?? "Unknown"}");
        Console.WriteLine($"   Informational Version: {informationalVersion ?? "Unknown"}");
        Console.WriteLine($"   File Version: {fileVersion ?? "Unknown"}");
        Console.WriteLine($"   Build Date: {buildDate?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Unknown"}");
        Console.WriteLine($"   Build Machine: {buildMachine ?? "Unknown"}");
        Console.WriteLine($"   Build User: {buildUser ?? "Unknown"}");
        Console.WriteLine($"   Location: {assembly.Location}");
    }

    private static void ShowGitVersionInformation()
    {
        var gitVersion = GitVersionHelper.GetVersionInfo();
        Console.WriteLine("📋 GitVersion Information:");
        Console.WriteLine($"   Version: {gitVersion.SemVer}");
        Console.WriteLine($"   Full Version: {gitVersion.FullSemVer}");
        Console.WriteLine($"   Informational Version: {gitVersion.InformationalVersion}");
        Console.WriteLine($"   Assembly Version: {gitVersion.AssemblySemVer}");
        Console.WriteLine($"   File Version: {gitVersion.AssemblySemFileVer}");
        Console.WriteLine($"   Branch: {gitVersion.BranchName}");
        Console.WriteLine($"   Commit: {gitVersion.ShortSha} ({gitVersion.CommitDate})");
        Console.WriteLine($"   Commits Since Version Source: {gitVersion.CommitsSinceVersionSource}");

        if (!string.IsNullOrEmpty(gitVersion.PreReleaseLabel))
        {
            Console.WriteLine($"   Pre-release: {gitVersion.PreReleaseTag}");
        }

        if (gitVersion.UncommittedChanges > 0)
        {
            Console.WriteLine($"   ⚠️ Uncommitted Changes: {gitVersion.UncommittedChanges}");
        }

        Console.WriteLine($"   Build Metadata: {gitVersion.FullBuildMetaData}");
    }

    private static void ShowRuntimeInformation()
    {
        Console.WriteLine("⚙️  Runtime Information:");
        Console.WriteLine($"   .NET Version: {Environment.Version}");
        Console.WriteLine($"   Runtime Version: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"   OS: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"   Architecture: {RuntimeInformation.OSArchitecture}");
        Console.WriteLine($"   Process Architecture: {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"   Working Directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"   Machine Name: {Environment.MachineName}");
        Console.WriteLine($"   User Name: {Environment.UserName}");
        Console.WriteLine($"   Process ID: {Environment.ProcessId}");
        Console.WriteLine($"   Processor Count: {Environment.ProcessorCount}");
    }

    private static void ShowLoadedAssemblies()
    {
        Console.WriteLine("📚 Libraries and Dependencies:");

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

        Console.WriteLine("  📚 Application Assemblies:");
        if (applicationAssemblies.Count != 0)
        {
            foreach (var assembly in applicationAssemblies)
            {
                LogAssemblyInfo(assembly, "     🚀");
            }
        }

        if (thirdPartyAssemblies.Any())
        {
            Console.WriteLine("  📚 Third-Party Assemblies:");
            foreach (var assembly in thirdPartyAssemblies)
            {
                LogAssemblyInfo(assembly, "     📱");
            }
        }

        if (systemAssemblies.Any())
        {
            Console.WriteLine("  📚 System Assemblies:");
            foreach (var assembly in systemAssemblies.Take(10)) // Limit to first 10 to avoid spam
            {
                LogAssemblyInfo(assembly, "     🔧");
            }
        }
    }

    private static void LogAssemblyInfo(Assembly assembly, string prefix)
    {
        var name = assembly.GetName();
        var version = name.Version?.ToString() ?? "Unknown";
        var location = assembly.GetSafeLocation();

        Console.WriteLine($"   {prefix} {name.Name} v{version} ({location})");
    }

    private static void ShowEnvironmentInformation()
    {
        Console.WriteLine("🌍 Environment Information:");
        Console.WriteLine(
            $"   DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Not Set"}"
        );
        Console.WriteLine(
            $"   ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not Set"}"
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
            Console.WriteLine("   SnapDog2 Environment Variables:");
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

                Console.WriteLine($"     {key}: {value ?? "Not Set"}");
            }
        }
        else
        {
            Console.WriteLine("   No SnapDog2-specific environment variables found");
        }
    }

    private static void ShowConfiguration(SnapDogConfiguration config)
    {
        Console.WriteLine("⚙️  SnapDog2 Configuration:");

        // System Configuration
        Console.WriteLine("   📋 System:");
        Console.WriteLine($"     Environment: {config.System.Environment}");
        Console.WriteLine($"     Log Level: {config.System.LogLevel}");
        Console.WriteLine($"     Debug Enabled: {config.System.DebugEnabled}");
        Console.WriteLine($"     Health Checks Enabled: {config.System.HealthChecksEnabled}");
        Console.WriteLine($"     Health Checks Timeout: {config.System.HealthChecksTimeout}s");

        // Services Configuration
        Console.WriteLine("   🔌 Services:");

        // Snapcast
        Console.WriteLine(
            $"     Snapcast: {config.Services.Snapcast.Address}:{config.Services.Snapcast.JsonRpcPort} (Timeout: {config.Services.Snapcast.Timeout}s)"
        );
        Console.WriteLine(
            $"       Auto Reconnect: {config.Services.Snapcast.AutoReconnect}, Interval: {config.Services.Snapcast.ReconnectInterval}s"
        );

        // MQTT
        if (config.Services.Mqtt.Enabled)
        {
            Console.WriteLine(
                $"     MQTT: {config.Services.Mqtt.BrokerAddress}:{config.Services.Mqtt.Port} (Client: {config.Services.Mqtt.ClientIndex})"
            );
            Console.WriteLine(
                $"       SSL: {config.Services.Mqtt.SslEnabled}, Keep Alive: {config.Services.Mqtt.KeepAlive}s"
            );
            Console.WriteLine(
                $"       Username: {(string.IsNullOrEmpty(config.Services.Mqtt.Username) ? "Not configured" : "***")}"
            );
        }
        else
        {
            Console.WriteLine("     MQTT: Disabled");
        }

        // KNX
        if (config.Services.Knx.Enabled)
        {
            var connectionType = config.Services.Knx.ConnectionType switch
            {
                KnxConnectionType.Tunnel => "IP Tunneling",
                KnxConnectionType.Router => "IP Routing",
                KnxConnectionType.Usb => "USB",
                _ => "Unknown",
            };
            Console.WriteLine(
                $"     KNX: {connectionType} - {config.Services.Knx.Gateway ?? "Auto-detect USB"}:{config.Services.Knx.Port} (Timeout: {config.Services.Knx.Timeout}s)"
            );
            Console.WriteLine($"       Auto Reconnect: {config.Services.Knx.AutoReconnect}");

            // Count KNX-enabled zones and clients
            var knxZoneCount = config.Zones.Count(z => z.Knx.Enabled);
            var knxClientCount = config.Clients.Count(c => c.Knx.Enabled);
            Console.WriteLine($"       KNX Integration: {knxZoneCount} zones, {knxClientCount} clients");
        }
        else
        {
            Console.WriteLine("     KNX: Disabled");
        }

        // Subsonic
        if (config.Services.Subsonic.Enabled)
        {
            Console.WriteLine(
                $"     Subsonic: {config.Services.Subsonic.Url ?? "Not configured"} (User: {(string.IsNullOrEmpty(config.Services.Subsonic.Username) ? "Not configured" : "***")}, Timeout: {config.Services.Subsonic.Timeout}ms)"
            );
        }
        else
        {
            Console.WriteLine("     Subsonic: Disabled");
        }

        // Snapcast Server Configuration
        Console.WriteLine("   🎵 Snapcast Server:");
        Console.WriteLine(
            $"     Codec: {config.SnapcastServer.Codec}, Sample Format: {config.SnapcastServer.SampleFormat}"
        );
        Console.WriteLine(
            $"     Ports - Web: {config.SnapcastServer.WebServerPort}, WebSocket: {config.SnapcastServer.WebSocketPort}, JSON-RPC: {config.SnapcastServer.JsonRpcPort}"
        );

        // Zones Configuration
        Console.WriteLine($"   🏠 Zones ({config.Zones.Count} configured):");
        foreach (var (zone, index) in config.Zones.Select((z, i) => (z, i + 1)))
        {
            Console.WriteLine($"     Zone {index}: {zone.Name} -> {zone.Sink}");
            if (!string.IsNullOrEmpty(zone.Mqtt.BaseTopic))
            {
                Console.WriteLine($"       MQTT Base Topic: {zone.Mqtt.BaseTopic}");
            }

            if (zone.Knx.Enabled)
            {
                Console.WriteLine("       KNX Enabled");
            }
        }

        // Clients Configuration
        Console.WriteLine($"   📱 Clients ({config.Clients.Count} configured):");
        foreach (var (client, index) in config.Clients.Select((c, i) => (c, i + 1)))
        {
            Console.WriteLine($"     Client {index}: {client.Name} (Zone {client.DefaultZone})");
            if (!string.IsNullOrEmpty(client.Mac))
            {
                Console.WriteLine($"       MAC: {client.Mac}");
            }

            if (!string.IsNullOrEmpty(client.Mqtt.BaseTopic))
            {
                Console.WriteLine($"       MQTT Base Topic: {client.Mqtt.BaseTopic}");
            }

            if (client.Knx.Enabled)
            {
                Console.WriteLine("       KNX Enabled");
            }
        }

        // Radio Stations Configuration
        Console.WriteLine($"   📻 Radio Stations ({config.RadioStations.Count} configured):");
        foreach (var (station, index) in config.RadioStations.Select((s, i) => (s, i + 1)))
        {
            Console.WriteLine($"     Radio {index}: {station.Name} -> {station.Url}");
        }

        // API Configuration
        Console.WriteLine("   🌐 API Server:");
        if (config.Api.Enabled)
        {
            Console.WriteLine($"     Status: Enabled on port {config.Api.Port}");
            Console.WriteLine(
                $"     Authentication: {(config.Api.AuthEnabled ? "Enabled" : "Disabled")} ({config.Api.ApiKeys.Count} API keys configured)"
            );

            if (config.Api.AuthEnabled && config.Api.ApiKeys.Count == 0)
            {
                Console.WriteLine("     ⚠️ Authentication is enabled but no API keys are configured!");
            }
        }
        else
        {
            Console.WriteLine("     Status: Disabled");
            Console.WriteLine("     No HTTP endpoints will be available");
        }

        // Telemetry Configuration
        if (config.Telemetry.Enabled)
        {
            Console.WriteLine(
                $"   📊 Telemetry: Enabled (Service: {config.Telemetry.ServiceName}, Sampling: {config.Telemetry.SamplingRate})"
            );

            var telemetryTargets = new List<string>();
            if (config.Telemetry.Otlp.Enabled)
            {
                telemetryTargets.Add($"OTLP: {config.Telemetry.Otlp.Endpoint}");
            }

            if (config.Telemetry.Prometheus.Enabled)
            {
                telemetryTargets.Add(
                    $"Prometheus: :{config.Telemetry.Prometheus.Port}{config.Telemetry.Prometheus.Path}"
                );
            }

            if (config.Telemetry.Seq.Enabled && !string.IsNullOrEmpty(config.Telemetry.Seq.Url))
            {
                telemetryTargets.Add($"Seq: {config.Telemetry.Seq.Url}");
            }

            if (telemetryTargets.Count != 0)
            {
                Console.WriteLine($"     Targets: {string.Join(", ", telemetryTargets)}");
            }
        }
        else
        {
            Console.WriteLine("   📊 Telemetry: Disabled");
        }
    }

    private static void ShowEndBanner()
    {
        Console.WriteLine("════════════════════════════════");
        Console.WriteLine("Starting service registrations...");
        Console.WriteLine("════════════════════════════════");
    }
}
