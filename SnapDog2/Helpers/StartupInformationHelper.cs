using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;
using SnapDog2.Extensions;

namespace SnapDog2.Helpers;

/// <summary>
/// Helper class for displaying comprehensive startup information.
/// Provides immediate startup feedback before service registrations begin.
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
        // Create a simple console logger for immediate output
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("StartupInfo");

        ShowStartupBanner(logger);
        ShowApplicationInformation(logger);
        ShowGitVersionInformation(logger);
        ShowRuntimeInformation(logger);
        ShowKeyConfiguration(logger, config);
        ShowServicesStatus(logger, config);
        ShowEndBanner(logger);
    }

    private static void ShowStartupBanner(ILogger logger)
    {
        logger.LogInformation("════════════════════════════════");
        logger.LogInformation("💑💑💑 SnapDog2 starting... 💑💑💑");
        logger.LogInformation("════════════════════════════════");
    }

    private static void ShowApplicationInformation(ILogger logger)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        logger.LogInformation("🚀 Application Information:");
        logger.LogInformation("   Name: {ApplicationName}", assembly.GetName().Name);
        logger.LogInformation("   Version: {Version}", version?.ToString() ?? "Unknown");
        logger.LogInformation("   Informational Version: {InformationalVersion}", informationalVersion ?? "Unknown");
    }

    private static void ShowGitVersionInformation(ILogger logger)
    {
        var gitVersion = GitVersionHelper.GetVersionInfo();
        logger.LogInformation("📋 GitVersion Information:");
        logger.LogInformation("   Version: {SemVer}", gitVersion.SemVer);
        logger.LogInformation("   Branch: {BranchName}", gitVersion.BranchName);
        logger.LogInformation("   Commit: {ShortSha} ({CommitDate})", gitVersion.ShortSha, gitVersion.CommitDate);
    }

    private static void ShowRuntimeInformation(ILogger logger)
    {
        logger.LogInformation("⚙️  Runtime Information:");
        logger.LogInformation("   .NET Version: {DotNetVersion}", Environment.Version);
        logger.LogInformation("   OS: {OperatingSystem}", RuntimeInformation.OSDescription);
        logger.LogInformation("   Architecture: {Architecture}", RuntimeInformation.OSArchitecture);
    }

    private static void ShowKeyConfiguration(ILogger logger, SnapDogConfiguration config)
    {
        logger.LogInformation("⚙️  Key Configuration:");
        logger.LogInformation("   Environment: {Environment}", config.System.Environment);
        logger.LogInformation("   API Enabled: {ApiEnabled} (Port: {ApiPort})", config.Api.Enabled, config.Api.Port);
        logger.LogInformation(
            "   Zones: {ZoneCount}, Clients: {ClientCount}",
            config.Zones.Count,
            config.Clients.Count
        );
    }

    private static void ShowServicesStatus(ILogger logger, SnapDogConfiguration config)
    {
        logger.LogInformation("🔌 Services:");
        logger.LogInformation(
            "   Snapcast: {SnapcastAddress}:{SnapcastPort}",
            config.Services.Snapcast.Address,
            config.Services.Snapcast.JsonRpcPort
        );
        logger.LogInformation(
            "   MQTT: {MqttStatus}",
            config.Services.Mqtt.Enabled
                ? $"{config.Services.Mqtt.BrokerAddress}:{config.Services.Mqtt.Port}"
                : "Disabled"
        );
        logger.LogInformation(
            "   KNX: {KnxStatus}",
            config.Services.Knx.Enabled ? $"{config.Services.Knx.Gateway}:{config.Services.Knx.Port}" : "Disabled"
        );
        logger.LogInformation(
            "   Subsonic: {SubsonicStatus}",
            config.Services.Subsonic.Enabled ? config.Services.Subsonic.Url ?? "Enabled" : "Disabled"
        );
    }

    private static void ShowEndBanner(ILogger logger)
    {
        logger.LogInformation("════════════════════════════════");
        logger.LogInformation("Starting service registrations...");
        logger.LogInformation("════════════════════════════════");
    }
}
