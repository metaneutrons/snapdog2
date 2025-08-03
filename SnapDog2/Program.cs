using System.Reflection;
using EnvoyConfig;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using SnapDog2.Core.Configuration;
using SnapDog2.Worker.DI;

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
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Is(Enum.Parse<LogEventLevel>(snapDogConfig.System.LogLevel, true))
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
    );

// Add file logging only if log file path is configured
if (!string.IsNullOrWhiteSpace(snapDogConfig.System.LogFile))
{
    loggerConfig.WriteTo.File(
        path: snapDogConfig.System.LogFile,
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 31,
        fileSizeLimitBytes: 100 * 1024 * 1024
    );
}

Log.Logger = loggerConfig.CreateLogger();

try
{
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

    // Add Command Processing (Cortex.Mediator)
    builder.Services.AddCommandProcessing();

    // Register configuration for IOptions pattern
    builder.Services.Configure<SnapDog2.Core.Configuration.ServicesConfig>(options =>
    {
        options.Snapcast = snapDogConfig.Services.Snapcast;
        options.Mqtt = snapDogConfig.Services.Mqtt;
        options.Knx = snapDogConfig.Services.Knx;
        options.Subsonic = snapDogConfig.Services.Subsonic;
    });

    // Add Snapcast services
    builder.Services.AddSnapcastServices();

    // Add MQTT services
    if (snapDogConfig.Services.Mqtt.Enabled)
    {
        builder.Services.AddMqttServices().ValidateMqttConfiguration();
    }

    // Add version logging service as first hosted service
    builder.Services.AddHostedService<SnapDog2.Services.StartupLoggingService>();

    // Add hosted service to initialize integration services on startup
    builder.Services.AddHostedService<SnapDog2.Worker.Services.IntegrationServicesHostedService>();

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
        healthChecksBuilder.AddTcpHealthCheck(
            options =>
            {
                options.AddHost(snapDogConfig.Services.Snapcast.Address, snapDogConfig.Services.Snapcast.JsonRpcPort);
            },
            name: "snapcast",
            tags: ["ready"]
        );

        if (snapDogConfig.Services.Mqtt.Enabled)
        {
            healthChecksBuilder.AddTcpHealthCheck(
                options =>
                {
                    options.AddHost(snapDogConfig.Services.Mqtt.BrokerAddress, snapDogConfig.Services.Mqtt.Port);
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

// Make Program class accessible to tests
public partial class Program { }
