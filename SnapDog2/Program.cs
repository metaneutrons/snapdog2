using System.CommandLine;
using dotenv.net;
using EnvoyConfig;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using SnapDog2.Core.Configuration;
using SnapDog2.Extensions;
using SnapDog2.Helpers;
using SnapDog2.Hosting;
using SnapDog2.Middleware;
using SnapDog2.Worker.DI;

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

    // Determine if debug logging is enabled
    var isDebugLogging =
        snapDogConfig.System.LogLevel.Equals("Debug", StringComparison.OrdinalIgnoreCase)
        || snapDogConfig.System.LogLevel.Equals("Trace", StringComparison.OrdinalIgnoreCase);

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

    // Register configuration
    builder.Services.AddSingleton(snapDogConfig);
    builder.Services.AddSingleton(snapDogConfig.System);
    builder.Services.AddSingleton(snapDogConfig.Telemetry);
    builder.Services.AddSingleton(snapDogConfig.Api);
    builder.Services.AddSingleton(snapDogConfig.Services);
    builder.Services.AddSingleton(snapDogConfig.SnapcastServer);

    // Configure resilient web host with port from configuration
    if (snapDogConfig.Api.Enabled)
    {
        builder.WebHost.UseResilientKestrel(snapDogConfig.Api, Log.Logger);
    }
    else
    {
        // If API is disabled, configure Kestrel to not listen on any ports
        builder.WebHost.UseKestrel(options =>
        {
            // Don't configure any endpoints - Kestrel won't listen on any ports
            Log.Information("API server is disabled - Kestrel will not bind to any ports");
        });
    }

    // Add Command Processing (Cortex.Mediator)
    builder.Services.AddCommandProcessing();

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

    // Add Snapcast services
    builder.Services.AddSnapcastServices();

    // Add MQTT services
    if (snapDogConfig.Services.Mqtt.Enabled)
    {
        builder.Services.AddMqttServices().ValidateMqttConfiguration();
    }

    // Add KNX services
    if (snapDogConfig.Services.Knx.Enabled)
    {
        builder.Services.AddKnxService(snapDogConfig);
    }

    // Skip hosted services in test environment
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Testing")
    {
        // Add version logging service as the very first hosted service (show environment info)
        builder.Services.AddHostedService<SnapDog2.Services.StartupInformationService>();

        // Add resilient startup service as second hosted service (then check if everything is healthy)
        builder.Services.AddHostedService<SnapDog2.Services.StartupService>();

        // Add hosted service to initialize integration services on startup
        builder.Services.AddHostedService<SnapDog2.Worker.Services.IntegrationServicesHostedService>();
    }

    // Register placeholder services
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.ISystemStatusService,
        SnapDog2.Infrastructure.Services.SystemStatusService
    >();
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IMetricsService,
        SnapDog2.Infrastructure.Services.MetricsService
    >();
    builder.Services.AddScoped<
        SnapDog2.Server.Services.Abstractions.IGlobalStatusService,
        SnapDog2.Server.Services.GlobalStatusService
    >();

    // Zone management services (placeholder implementations)
    builder.Services.AddScoped<SnapDog2.Core.Abstractions.IZoneManager, SnapDog2.Infrastructure.Services.ZoneManager>();

    // Client management services (placeholder implementations)
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IClientManager,
        SnapDog2.Infrastructure.Services.ClientManager
    >();

    // Playlist management services (placeholder implementations)
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IPlaylistManager,
        SnapDog2.Infrastructure.Services.PlaylistManager
    >();

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
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.MapControllers();
    }

    return app;
}

// Make Program class accessible to tests
public partial class Program { }
