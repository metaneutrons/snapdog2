using System.Reflection;
using System.Runtime.InteropServices;
using SnapDog2.Core.Configuration;
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
        ShowKeyConfiguration(config);
        ShowServicesStatus(config);
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

        Console.WriteLine("🚀 Application Information:");
        Console.WriteLine($"   Name: {assembly.GetName().Name}");
        Console.WriteLine($"   Version: {version?.ToString() ?? "Unknown"}");
        Console.WriteLine($"   Informational Version: {informationalVersion ?? "Unknown"}");
    }

    private static void ShowGitVersionInformation()
    {
        var gitVersion = GitVersionHelper.GetVersionInfo();
        Console.WriteLine("📋 GitVersion Information:");
        Console.WriteLine($"   Version: {gitVersion.SemVer}");
        Console.WriteLine($"   Branch: {gitVersion.BranchName}");
        Console.WriteLine($"   Commit: {gitVersion.ShortSha} ({gitVersion.CommitDate})");
    }

    private static void ShowRuntimeInformation()
    {
        Console.WriteLine("⚙️  Runtime Information:");
        Console.WriteLine($"   .NET Version: {Environment.Version}");
        Console.WriteLine($"   OS: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"   Architecture: {RuntimeInformation.OSArchitecture}");
    }

    private static void ShowKeyConfiguration(SnapDogConfiguration config)
    {
        Console.WriteLine("⚙️  Key Configuration:");
        Console.WriteLine($"   Environment: {config.System.Environment}");
        Console.WriteLine($"   API Enabled: {config.Api.Enabled} (Port: {config.Api.Port})");
        Console.WriteLine($"   Zones: {config.Zones.Count}, Clients: {config.Clients.Count}");
    }

    private static void ShowServicesStatus(SnapDogConfiguration config)
    {
        Console.WriteLine("🔌 Services:");
        Console.WriteLine($"   Snapcast: {config.Services.Snapcast.Address}:{config.Services.Snapcast.JsonRpcPort}");

        var mqttStatus = config.Services.Mqtt.Enabled
            ? $"{config.Services.Mqtt.BrokerAddress}:{config.Services.Mqtt.Port}"
            : "Disabled";
        Console.WriteLine($"   MQTT: {mqttStatus}");

        var knxStatus = config.Services.Knx.Enabled
            ? $"{config.Services.Knx.Gateway}:{config.Services.Knx.Port}"
            : "Disabled";
        Console.WriteLine($"   KNX: {knxStatus}");

        var subsonicStatus = config.Services.Subsonic.Enabled ? config.Services.Subsonic.Url ?? "Enabled" : "Disabled";
        Console.WriteLine($"   Subsonic: {subsonicStatus}");
    }

    private static void ShowEndBanner()
    {
        Console.WriteLine("════════════════════════════════");
        Console.WriteLine("Starting service registrations...");
        Console.WriteLine("════════════════════════════════");
    }
}
