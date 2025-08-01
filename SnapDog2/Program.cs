using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using SnapDog2.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from environment variables
var snapDogConfig = new SnapDogConfiguration();

// Simple configuration loading - we'll implement EnvoyConfig properly later
snapDogConfig.System.LogLevel = Environment.GetEnvironmentVariable("SNAPDOG_SYSTEM_LOG_LEVEL") ?? "Information";
snapDogConfig.System.Environment = Environment.GetEnvironmentVariable("SNAPDOG_SYSTEM_ENVIRONMENT") ?? "Development";
snapDogConfig.System.DebugEnabled = bool.Parse(Environment.GetEnvironmentVariable("SNAPDOG_SYSTEM_DEBUG_ENABLED") ?? "false");
snapDogConfig.System.HealthChecksEnabled = bool.Parse(Environment.GetEnvironmentVariable("SNAPDOG_SYSTEM_HEALTH_CHECKS_ENABLED") ?? "true");

snapDogConfig.Services.Snapcast.Enabled = bool.Parse(Environment.GetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_ENABLED") ?? "true");
snapDogConfig.Services.Snapcast.Address = Environment.GetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_ADDRESS") ?? "localhost";
snapDogConfig.Services.Snapcast.Port = int.Parse(Environment.GetEnvironmentVariable("SNAPDOG_SERVICES_SNAPCAST_PORT") ?? "1705");

snapDogConfig.Services.Mqtt.Enabled = bool.Parse(Environment.GetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_ENABLED") ?? "true");
snapDogConfig.Services.Mqtt.BrokerAddress = Environment.GetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS") ?? "localhost";
snapDogConfig.Services.Mqtt.Port = int.Parse(Environment.GetEnvironmentVariable("SNAPDOG_SERVICES_MQTT_PORT") ?? "1883");

snapDogConfig.Api.AuthEnabled = bool.Parse(Environment.GetEnvironmentVariable("SNAPDOG_API_AUTH_ENABLED") ?? "true");
var apiKey1 = Environment.GetEnvironmentVariable("SNAPDOG_API_APIKEY_1");
if (!string.IsNullOrEmpty(apiKey1))
{
    snapDogConfig.Api.ApiKeys.Add(apiKey1);
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

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add health checks
    if (snapDogConfig.System.HealthChecksEnabled)
    {
        var healthChecksBuilder = builder.Services.AddHealthChecks();
        
        // Add basic application health check
        healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy("Application is running"), tags: ["ready", "live"]);
        
        // Add external service health checks based on configuration
        if (snapDogConfig.Services.Snapcast.Enabled)
        {
            healthChecksBuilder.AddTcpHealthCheck(options =>
            {
                options.AddHost(snapDogConfig.Services.Snapcast.Address, snapDogConfig.Services.Snapcast.Port);
            }, name: "snapcast", tags: ["ready"]);
        }

        if (snapDogConfig.Services.Mqtt.Enabled)
        {
            healthChecksBuilder.AddTcpHealthCheck(options =>
            {
                options.AddHost(snapDogConfig.Services.Mqtt.BrokerAddress, snapDogConfig.Services.Mqtt.Port);
            }, name: "mqtt", tags: ["ready"]);
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

    // Telemetry Configuration
    Log.Information("Telemetry Configuration:");
    Log.Information("  Enabled: {TelemetryEnabled}", config.Telemetry.Enabled);
    Log.Information("  Service Name: {ServiceName}", config.Telemetry.ServiceName);
    Log.Information("  Sampling Rate: {SamplingRate}", config.Telemetry.SamplingRate);
    Log.Information("  OTLP Enabled: {OtlpEnabled}", config.Telemetry.Otlp.Enabled);
    if (config.Telemetry.Otlp.Enabled)
    {
        Log.Information("  OTLP Endpoint: {OtlpEndpoint}", config.Telemetry.Otlp.Endpoint);
        Log.Information("  OTLP Agent: {OtlpAgent}:{OtlpPort}", config.Telemetry.Otlp.AgentAddress, config.Telemetry.Otlp.AgentPort);
    }
    Log.Information("  Prometheus Enabled: {PrometheusEnabled}", config.Telemetry.Prometheus.Enabled);
    if (config.Telemetry.Prometheus.Enabled)
    {
        Log.Information("  Prometheus Path: {PrometheusPath}", config.Telemetry.Prometheus.Path);
        Log.Information("  Prometheus Port: {PrometheusPort}", config.Telemetry.Prometheus.Port);
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
        Log.Information("    Address: {SnapcastAddress}:{SnapcastPort}", config.Services.Snapcast.Address, config.Services.Snapcast.Port);
        Log.Information("    Timeout: {SnapcastTimeout}s", config.Services.Snapcast.Timeout);
        Log.Information("    Auto Reconnect: {SnapcastAutoReconnect}", config.Services.Snapcast.AutoReconnect);
        Log.Information("    Reconnect Interval: {SnapcastReconnectInterval}s", config.Services.Snapcast.ReconnectInterval);
    }

    // MQTT
    Log.Information("  MQTT:");
    Log.Information("    Enabled: {MqttEnabled}", config.Services.Mqtt.Enabled);
    if (config.Services.Mqtt.Enabled)
    {
        Log.Information("    Broker: {MqttBroker}:{MqttPort}", config.Services.Mqtt.BrokerAddress, config.Services.Mqtt.Port);
        Log.Information("    Client ID: {MqttClientId}", config.Services.Mqtt.ClientId);
    }

    // KNX
    Log.Information("  KNX:");
    Log.Information("    Enabled: {KnxEnabled}", config.Services.Knx.Enabled);
    if (config.Services.Knx.Enabled)
    {
        Log.Information("    Gateway: {KnxGateway}:{KnxPort}", config.Services.Knx.Gateway ?? "Not configured", config.Services.Knx.Port);
    }

    // Subsonic
    Log.Information("  Subsonic:");
    Log.Information("    Enabled: {SubsonicEnabled}", config.Services.Subsonic.Enabled);
    if (config.Services.Subsonic.Enabled)
    {
        Log.Information("    URL: {SubsonicUrl}", config.Services.Subsonic.Url ?? "Not configured");
        Log.Information("    Username: {SubsonicUsername}", string.IsNullOrEmpty(config.Services.Subsonic.Username) ? "Not configured" : "***");
    }

    Log.Information("=== End Configuration ===");
}

// Make Program class accessible to tests
public partial class Program { }
