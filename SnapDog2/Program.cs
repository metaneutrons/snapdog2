using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;
using dotenv.net;
using EnvoyConfig;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using SnapDog2.Core.Configuration;
using SnapDog2.Extensions;
using SnapDog2.Extensions.DependencyInjection;
using SnapDog2.Helpers;
using SnapDog2.Hosting;
using SnapDog2.Middleware;

// ═══════════════════════════════════════════════════════════════════════════════
// System.CommandLine Flow - Command-Line Argument Parsing
// ═══════════════════════════════════════════════════════════════════════════════

// Check if we're running in a test environment
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
{
    // For tests, create the web application directly without command-line parsing
    var testApp = CreateWebApplication(args);
    await testApp.RunAsync();
    return 0;
}

Option<FileInfo?> envFileOption = new("--env-file", "-e")
{
    Description = "Path to environment file to load (.env format)",
};

RootCommand rootCommand = new("SnapDog2 - The Snapcast-based Smart Home Audio System with MQTT & KNX integration")
{
    envFileOption,
};

// Parse and handle commands
var parseResult = rootCommand.Parse(args);

// Check if a command line was provided
var result = parseResult.Invoke();
if (result != 0 || args.Contains("--help") || args.Contains("-h") || args.Contains("--version"))
{
    return result;
}

// Handle environment file option
if (parseResult.GetValue(envFileOption) is FileInfo parsedFile)
{
    try
    {
        Console.WriteLine($"📁 Loading environment file: {parsedFile.FullName}");
        DotEnv.Fluent().WithExceptions().WithEnvFiles(parsedFile.FullName).WithTrimValues().Load();
        Console.WriteLine("✅ Environment file loaded successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed to load environment file: {ex.Message}");
        return 1;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Start Services - Original Working Logic
// ═══════════════════════════════════════════════════════════════════════════════

var app = CreateWebApplication(args);

try
{
    Log.Information("SnapDog2 application configured successfully");

    // Use resilient host wrapper to handle startup exceptions gracefully
    var resilientHost = app.UseResilientStartup(true);
    await resilientHost.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Environment.ExitCode = 3;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;

// ═══════════════════════════════════════════════════════════════════════════════
// Web Application Creation Method (for both normal and test usage)
// ═══════════════════════════════════════════════════════════════════════════════

static WebApplication CreateWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

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
    var loggerConfig = new LoggerConfiguration()
        .MinimumLevel.Is(Enum.Parse<LogEventLevel>(snapDogConfig.System.LogLevel, true))
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Extensions.Hosting.Internal.Host", LogEventLevel.Fatal)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        );

    // Add file logging only if log file path is configured
    if (!string.IsNullOrWhiteSpace(snapDogConfig.System.LogFile))
    {
        loggerConfig.WriteTo.File(
            snapDogConfig.System.LogFile,
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
            retainedFileCountLimit: 31,
            fileSizeLimitBytes: 100 * 1024 * 1024
        );
    }

    Log.Logger = loggerConfig.CreateLogger();

    // Configuration will be logged by StartupVersionLoggingService

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Show startup banner immediately (before any service registrations)
    ShowStartupInformation(snapDogConfig);

    // Register configuration
    builder.Services.AddSingleton(snapDogConfig);
    builder.Services.AddSingleton(snapDogConfig.System);
    builder.Services.AddSingleton(snapDogConfig.Telemetry);
    builder.Services.AddSingleton(snapDogConfig.Api);
    builder.Services.AddSingleton(snapDogConfig.Services);
    builder.Services.AddSingleton(snapDogConfig.SnapcastServer);

    // Register configuration for IOptions pattern
    builder.Services.Configure<SnapDogConfiguration>(options =>
    {
        // Copy all configuration sections
        options.System = snapDogConfig.System;
        options.Telemetry = snapDogConfig.Telemetry;
        options.Api = snapDogConfig.Api;
        options.Services = snapDogConfig.Services;
        options.SnapcastServer = snapDogConfig.SnapcastServer;
        options.Zones = snapDogConfig.Zones;
        options.Clients = snapDogConfig.Clients;
        options.RadioStations = snapDogConfig.RadioStations;
    });

    // Add command processing (Mediator, handlers, behaviors)
    builder.Services.AddCommandProcessing();

    // Add Snapcast services
    builder.Services.AddSnapcastServices();

    // Register zone and client configurations for services that need them
    builder.Services.Configure<List<ZoneConfig>>(options =>
    {
        options.Clear();
        options.AddRange(snapDogConfig.Zones);
    });

    builder.Services.Configure<List<ClientConfig>>(options =>
    {
        options.Clear();
        options.AddRange(snapDogConfig.Clients);
    });

    // Add MQTT services
    if (snapDogConfig.Services.Mqtt.Enabled)
    {
        builder.Services.AddMqttServices().ValidateMqttConfiguration();
    }
    else
    {
        Log.Information("MQTT is disabled in configuration (SNAPDOG_SERVICES_MQTT_ENABLED=false)");
    }

    // Add KNX services
    if (snapDogConfig.Services.Knx.Enabled)
    {
        builder.Services.AddKnxService(snapDogConfig);
    }
    else
    {
        Log.Information("KNX is disabled in configuration (SNAPDOG_SERVICES_KNX_ENABLED=false)");
    }

    // Skip hosted services in test environment
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Testing")
    {
        // Add resilient startup service as first hosted service (check if everything is healthy)
        builder.Services.AddHostedService<SnapDog2.Services.StartupService>();

        // Add hosted service to initialize integration services on startup
        builder.Services.AddHostedService<SnapDog2.Worker.Services.IntegrationServicesHostedService>();

        // Add hosted service to publish initial state after integration services are initialized
        builder.Services.AddHostedService<SnapDog2.Services.StatePublishingService>();
    }

    // Register placeholder services
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IAppStatusService,
        SnapDog2.Infrastructure.Application.AppStatusService
    >();
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IMetricsService,
        SnapDog2.Infrastructure.Application.MetricsService
    >();
    builder.Services.AddScoped<
        SnapDog2.Server.Features.Global.Services.Abstractions.IGlobalStatusService,
        SnapDog2.Server.Features.Global.Services.GlobalStatusService
    >();

    // Zone management services (production implementations)
    builder.Services.AddScoped<SnapDog2.Core.Abstractions.IZoneManager, SnapDog2.Infrastructure.Domain.ZoneManager>();

    // Media player services
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IMediaPlayerService,
        SnapDog2.Infrastructure.Audio.MediaPlayerService
    >();

    // Client management services
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IClientManager,
        SnapDog2.Infrastructure.Domain.ClientManager
    >();

    // Playlist management services
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IPlaylistManager,
        SnapDog2.Infrastructure.Domain.PlaylistManager
    >();

    // Subsonic integration service
    if (snapDogConfig.Services.Subsonic.Enabled)
    {
        builder.Services.AddHttpClient<
            SnapDog2.Core.Abstractions.ISubsonicService,
            SnapDog2.Infrastructure.Integrations.Subsonic.SubsonicService
        >(client =>
        {
            client.Timeout = TimeSpan.FromMilliseconds(snapDogConfig.Services.Subsonic.Timeout);
        });
    }
    else
    {
        Log.Information("Subsonic is disabled in configuration (SNAPDOG_SERVICES_SUBSONIC_ENABLED=false)");
    }

    // Configure resilient web host with port from configuration
    if (snapDogConfig.Api.Enabled)
    {
        builder.WebHost.UseResilientKestrel(snapDogConfig.Api, Log.Logger);
    }
    else if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
    {
        // For tests, we still need to configure Kestrel even when API is disabled
        // so that WebApplicationFactory can create HTTP clients
        Log.Information("🧪 Test environment detected - configuring minimal Kestrel for testing");
        builder.WebHost.UseKestrel();
    }
    else
    {
        Log.Information("🌐 API is disabled - Kestrel will not bind to any ports.");
    }

    // Add services to the container (conditionally based on API configuration)
    if (snapDogConfig.Api.Enabled)
    {
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        Log.Information("API server enabled on port {Port}", snapDogConfig.Api.Port);
    }
    else
    {
        Log.Information("API server is disabled via configuration (SNAPDOG_API_ENABLED=false)");
    }

    // Add health checks
    if (snapDogConfig.System.HealthChecksEnabled)
    {
        var healthChecksBuilder = builder.Services.AddHealthChecks();

        // Add basic application health check
        healthChecksBuilder.AddCheck(
            "self",
            () => HealthCheckResult.Healthy("Application is running"),
            ["ready", "live"]
        );

        // Add external service health checks based on configuration
        healthChecksBuilder.AddTcpHealthCheck(
            options =>
            {
                options.AddHost(snapDogConfig.Services.Snapcast.Address, snapDogConfig.Services.Snapcast.JsonRpcPort);
            },
            "snapcast",
            tags: ["ready"]
        );

        if (snapDogConfig.Services.Mqtt.Enabled)
        {
            healthChecksBuilder.AddTcpHealthCheck(
                options =>
                {
                    options.AddHost(snapDogConfig.Services.Mqtt.BrokerAddress, snapDogConfig.Services.Mqtt.Port);
                },
                "mqtt",
                tags: ["ready"]
            );
        }

        if (snapDogConfig.Services.Knx.Enabled && !string.IsNullOrEmpty(snapDogConfig.Services.Knx.Gateway))
        {
            healthChecksBuilder.AddTcpHealthCheck(
                options =>
                {
                    options.AddHost(snapDogConfig.Services.Knx.Gateway, snapDogConfig.Services.Knx.Port);
                },
                "knx",
                tags: ["ready"]
            );
        }
    }

    var app = builder.Build();

    // Add global exception handling as the first middleware
    app.UseGlobalExceptionHandling();

    // Configure the HTTP request pipeline (conditionally based on API configuration)
    if (snapDogConfig.Api.Enabled)
    {
        // if (app.Environment.IsDevelopment())
        // FIXME: maybe?! :=)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.MapControllers();
    }

    return app;
}

/// <summary>
/// Shows comprehensive startup information immediately during application startup.
/// This replaces the StartupInformationService to ensure startup info appears before service registrations.
/// </summary>
static void ShowStartupInformation(SnapDogConfiguration config)
{
    // Create a simple console logger for immediate output
    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var logger = loggerFactory.CreateLogger("StartupInfo");

    // Startup Banner
    logger.LogInformation("════════════════════════════════");
    logger.LogInformation("💑💑💑 SnapDog2 starting... 💑💑💑");
    logger.LogInformation("════════════════════════════════");

    // Application Information
    var assembly = Assembly.GetExecutingAssembly();
    var version = assembly.GetName().Version;
    var informationalVersion = assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion;

    logger.LogInformation("🚀 Application Information:");
    logger.LogInformation("   Name: {ApplicationName}", assembly.GetName().Name);
    logger.LogInformation("   Version: {Version}", version?.ToString() ?? "Unknown");
    logger.LogInformation("   Informational Version: {InformationalVersion}", informationalVersion ?? "Unknown");

    // GitVersion Information
    var gitVersion = GitVersionHelper.GetVersionInfo();
    logger.LogInformation("📋 GitVersion Information:");
    logger.LogInformation("   Version: {SemVer}", gitVersion.SemVer);
    logger.LogInformation("   Branch: {BranchName}", gitVersion.BranchName);
    logger.LogInformation("   Commit: {ShortSha} ({CommitDate})", gitVersion.ShortSha, gitVersion.CommitDate);

    // Runtime Information
    logger.LogInformation("⚙️  Runtime Information:");
    logger.LogInformation("   .NET Version: {DotNetVersion}", Environment.Version);
    logger.LogInformation("   OS: {OperatingSystem}", RuntimeInformation.OSDescription);
    logger.LogInformation("   Architecture: {Architecture}", RuntimeInformation.OSArchitecture);

    // Key Configuration
    logger.LogInformation("⚙️  Key Configuration:");
    logger.LogInformation("   Environment: {Environment}", config.System.Environment);
    logger.LogInformation("   API Enabled: {ApiEnabled} (Port: {ApiPort})", config.Api.Enabled, config.Api.Port);
    logger.LogInformation("   Zones: {ZoneCount}, Clients: {ClientCount}", config.Zones.Count, config.Clients.Count);

    // Services Status
    logger.LogInformation("🔌 Services:");
    logger.LogInformation(
        "   Snapcast: {SnapcastAddress}:{SnapcastPort}",
        config.Services.Snapcast.Address,
        config.Services.Snapcast.JsonRpcPort
    );
    logger.LogInformation(
        "   MQTT: {MqttStatus}",
        config.Services.Mqtt.Enabled ? $"{config.Services.Mqtt.BrokerAddress}:{config.Services.Mqtt.Port}" : "Disabled"
    );
    logger.LogInformation(
        "   KNX: {KnxStatus}",
        config.Services.Knx.Enabled ? $"{config.Services.Knx.Gateway}:{config.Services.Knx.Port}" : "Disabled"
    );
    logger.LogInformation(
        "   Subsonic: {SubsonicStatus}",
        config.Services.Subsonic.Enabled ? config.Services.Subsonic.Url ?? "Enabled" : "Disabled"
    );

    // End Banner
    logger.LogInformation("════════════════════════════════");
    logger.LogInformation("Starting service registrations...");
    logger.LogInformation("════════════════════════════════");
}

// Make Program class accessible to tests
public partial class Program { }
