//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//

using System.CommandLine;
using dotenv.net;
using EnvoyConfig;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using SnapDog2.Api.Authentication;
using SnapDog2.Api.Hubs;
using SnapDog2.Api.Middleware;
using SnapDog2.Application.Extensions;
using SnapDog2.Application.Extensions.DependencyInjection;
using SnapDog2.Application.Services;
using SnapDog2.Application.Worker.Services;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Domain.Services;
using SnapDog2.Infrastructure.Audio;
using SnapDog2.Infrastructure.Hosting;
using SnapDog2.Infrastructure.Integrations.Knx;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Infrastructure.Integrations.SignalR;
using SnapDog2.Infrastructure.Integrations.Subsonic;
using SnapDog2.Infrastructure.Metrics;
using SnapDog2.Infrastructure.Notifications;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Infrastructure.Storage;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Helpers;
using StackExchange.Redis;

// ═══════════════════════════════════════════════════════════════════════════════
// System.CommandLine Flow - Command-Line Argument Parsing
// ═══════════════════════════════════════════════════════════════════════════════

Option<FileInfo?> envFileOption = new("--env-file", "-e")
{
    Description = "Path to environment file to load (.env format)",
};

// Add WebApplicationFactory arguments (used by integration tests)
Option<string?> environmentOption = new("--environment")
{
    Description = "Internal: WebApplicationFactory environment",
};

Option<string?> environmentOptionUpper = new("--ENVIRONMENT")
{
    Description = "Internal: WebApplicationFactory environment",
};

Option<string?> contentRootOption = new("--contentRoot")
{
    Description = "Internal: WebApplicationFactory content root",
};

Option<string?> applicationNameOption = new("--applicationName")
{
    Description = "Internal: WebApplicationFactory application name",
};

RootCommand rootCommand = new("SnapDog2 - The Snapcast-based Smart Home Audio System with MQTT & KNX integration")
{
    envFileOption,
    environmentOption, // Accept but ignore - WebApplicationFactory passes --environment
    environmentOptionUpper, // Accept but ignore - WebApplicationFactory passes --ENVIRONMENT
    contentRootOption, // Accept but ignore - not needed for REST API
    applicationNameOption, // Accept but ignore - purely internal
};

// Parse and handle commands
var parseResult = rootCommand.Parse(args);

// Check if parsing failed or help was requested
if (parseResult.Errors.Count > 0 || args.Contains("--help") || args.Contains("-h") || args.Contains("--version"))
{
    var result = parseResult.Invoke();
    if (result != 0 || args.Contains("--help") || args.Contains("-h") || args.Contains("--version"))
    {
        return result;
    }
}

// Handle environment file option
if (parseResult.GetValue(envFileOption) is { } parsedFile)
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
    StartupInformationHelper.ShowStartupInformation(snapDogConfig);

    // Register configuration using elegant extension methods
    builder.Services.ConfigureSnapDog(snapDogConfig);

    // Configure OpenTelemetry (if enabled)
    if (snapDogConfig.Telemetry.Enabled)
    {
        builder
            .Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource
                    .AddService(snapDogConfig.Telemetry.ServiceName, "2.0.0")
                    .AddAttributes(
                        new Dictionary<string, object>
                        {
                            ["deployment.environment"] = builder.Environment.EnvironmentName,
                            ["service.namespace"] = "snapdog",
                        }
                    )
            )
            .WithTracing(tracing =>
                tracing
                    .SetSampler(new TraceIdRatioBasedSampler(snapDogConfig.Telemetry.SamplingRate))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource("SnapDog2.*")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(snapDogConfig.Telemetry.Otlp.Endpoint);
                        options.Protocol = snapDogConfig.Telemetry.Otlp.Protocol.ToLowerInvariant() switch
                        {
                            "grpc" => OtlpExportProtocol.Grpc,
                            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                            _ => OtlpExportProtocol.Grpc,
                        };
                        options.TimeoutMilliseconds = snapDogConfig.Telemetry.Otlp.TimeoutSeconds * 1000;

                        if (!string.IsNullOrEmpty(snapDogConfig.Telemetry.Otlp.Headers))
                        {
                            options.Headers = snapDogConfig.Telemetry.Otlp.Headers;
                        }
                    })
            )
            .WithMetrics(metrics =>
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("SnapDog2.Application")
                    .AddMeter("SnapDog2.*")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(snapDogConfig.Telemetry.Otlp.Endpoint);
                        options.Protocol = snapDogConfig.Telemetry.Otlp.Protocol.ToLowerInvariant() switch
                        {
                            "grpc" => OtlpExportProtocol.Grpc,
                            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                            _ => OtlpExportProtocol.Grpc,
                        };
                        options.TimeoutMilliseconds = snapDogConfig.Telemetry.Otlp.TimeoutSeconds * 1000;

                        if (!string.IsNullOrEmpty(snapDogConfig.Telemetry.Otlp.Headers))
                        {
                            options.Headers = snapDogConfig.Telemetry.Otlp.Headers;
                        }
                    })
            );

        Log.Information(
            "OpenTelemetry configured with OTLP endpoint: {Endpoint} (Protocol: {Protocol})",
            snapDogConfig.Telemetry.Otlp.Endpoint,
            snapDogConfig.Telemetry.Otlp.Protocol
        );
    }
    else
    {
        Log.Information(
            "OpenTelemetry disabled (SNAPDOG_TELEMETRY_ENABLED={TelemetryEnabled})",
            snapDogConfig.Telemetry.Enabled
        );
    }

    // Command processing removed - using direct service calls now

    // Add HTTP client factory (required by MediaPlayerService and other services)
    builder.Services.AddHttpClient();

    // Add Snapcast services
    builder.Services.AddSnapcastServices();

    // Add MQTT services with smart publishing
    if (snapDogConfig.Services.Mqtt.Enabled)
    {
        builder.Services.AddMqttServices().ValidateMqttConfiguration();
        builder.Services.AddSmartMqttPublishing();
    }
    else
    {
        Log.Information("MQTT is disabled in configuration (SNAPDOG_SERVICES_MQTT_ENABLED=false)");
    }

    // Add KNX services
    if (snapDogConfig.Services.Knx.Enabled)
    {
        builder.Services.AddKnxService(snapDogConfig);
        builder.Services.AddScoped<IKnxCommandHandler, KnxCommandHandler>();
    }
    else
    {
        Log.Information("KNX is disabled in configuration (SNAPDOG_SERVICES_KNX_ENABLED=false)");
        // Register dummy handler for when KNX is disabled
        builder.Services.AddScoped<IKnxCommandHandler, KnxCommandHandler>();
    }

    // Notification processing configuration and services
    builder.Services.ConfigureNotificationProcessing();
    builder.Services.AddSingleton<NotificationQueue>();
    builder.Services.AddSingleton<INotificationQueue>(sp =>
        sp.GetRequiredService<NotificationQueue>()
    );
    builder.Services.AddHostedService<NotificationBackgroundService>();

    // Skip hosted services in test environment
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Testing")
    {
        // Add resilient startup service as first hosted service (check if everything is healthy)
        builder.Services.AddHostedService<StartupService>();

        // Add hosted service to initialize integration services on startup
        builder.Services.AddHostedService<IntegrationServicesHostedService>();

        // Add zone grouping configuration
        builder.Services.Configure<SnapcastConfig>(_ => { });

        // Zone grouping service
        builder.Services.AddScoped<IZoneGroupingService, ZoneGroupingService>();
        builder.Services.AddHostedService<ZoneGroupingBackgroundService>();

        // StatePublishingService removed - using direct SignalR calls instead

        // Register integration publishers (MQTT now uses event-driven publishing)
        // SignalR now uses direct state store connection
        builder.Services.AddHostedService<SignalRStateNotifier>();

        // All integrations now use direct state store connections
    }

    // Register status services
    builder.Services.AddScoped<
        IAppHealthCheckService,
        AppHealthCheckService
    >();
    builder.Services.AddScoped<
        IAppStatusService,
        AppStatusService
    >();
    builder.Services.AddSingleton<
        ICommandStatusService,
        CommandStatusService
    >();
    // GlobalStatusService removed - using direct SignalR calls instead

    // Zone management services
    builder.Services.AddSingleton<
        IZoneStateStore,
        InMemoryZoneStateStore
    >();

    // Client management services
    builder.Services.AddSingleton<
        IClientStateStore,
        InMemoryClientStateStore
    >();

    // Redis persistent state storage services
    if (snapDogConfig.Redis.Enabled)
    {
        // Register Redis connection multiplexer
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var configuration = ConfigurationOptions.Parse(snapDogConfig.Redis.ConnectionString);
            configuration.ConnectTimeout = snapDogConfig.Redis.TimeoutSeconds * 1000;
            configuration.SyncTimeout = snapDogConfig.Redis.TimeoutSeconds * 1000;
            configuration.DefaultDatabase = snapDogConfig.Redis.Database;
            configuration.AbortOnConnectFail = false; // Allow retries
            configuration.ConnectRetry = 3;

            return ConnectionMultiplexer.Connect(configuration);
        });

        // Register persistent state store
        builder.Services.AddSingleton<
            IPersistentStateStore
        >(provider =>
        {
            var redis = provider.GetRequiredService<IConnectionMultiplexer>();
            var logger = provider.GetRequiredService<ILogger<RedisPersistentStateStore>>();
            return new RedisPersistentStateStore(redis, snapDogConfig.Redis, logger);
        });

        // PersistentStateNotificationHandler removed - using direct state store calls instead

        // Register state restoration service (only if Redis is enabled)
        builder.Services.AddHostedService<StateRestorationService>();
    }
    else
    {
        Log.Information("🚫 Redis persistent state storage is disabled");
    }

    // Register state persistence service (checks Redis status internally)
    builder.Services.AddHostedService<StatePersistenceService>();
    builder.Services.AddSingleton<
        IZoneManager,
        ZoneManager
    >();

    // Media player services - Singleton to persist across requests and scopes
    builder.Services.AddSingleton<
        IMediaPlayerService,
        MediaPlayerService
    >();

    // Client management services - Singleton to match ZoneManager lifetime
    builder.Services.AddSingleton<
        IClientManager,
        ClientManager
    >();

    // Enterprise-grade metrics services
    builder.Services.AddSingleton<ApplicationMetrics>();
    builder.Services.AddSingleton<IApplicationMetrics>(provider =>
        provider.GetRequiredService<ApplicationMetrics>());
    builder.Services.AddSingleton<EnterpriseMetricsService>();
    builder.Services.AddSingleton<ZoneGroupingMetrics>();

    // Error tracking service
    builder.Services.AddSingleton<
        IErrorTrackingService,
        ErrorTrackingService
    >();

    // Register EnterpriseMetricsService as the IMetricsService implementation
    builder.Services.AddSingleton<IMetricsService>(provider =>
        provider.GetRequiredService<EnterpriseMetricsService>()
    );

    // Business metrics collection service
    builder.Services.AddHostedService<BusinessMetricsCollectionService>();

    // Playlist management services - changed to Singleton to fix DI scoping issues
    builder.Services.AddSingleton<
        IPlaylistManager,
        PlaylistManager
    >();

    // Add missing service registrations for mediator removal (Phase 2)
    // Note: ZoneService is created directly by ZoneManager, not registered in DI
    builder.Services.AddScoped<IClientService, ClientService>();

    // Subsonic integration service
    if (snapDogConfig.Services.Subsonic.Enabled)
    {
        builder.Services.AddHttpClient<
            ISubsonicService,
            SubsonicService
        >(client =>
        {
            client.Timeout = TimeSpan.FromMilliseconds(snapDogConfig.Services.Subsonic.Timeout);
        });
    }
    else
    {
        Log.Information("Subsonic is disabled in configuration (SNAPDOG_SERVICES_SUBSONIC_ENABLED=false)");
    }

    // Remove .NET 9 default port override to use SnapDog configuration
    Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", null);
    Environment.SetEnvironmentVariable("ASPNETCORE_HTTPS_PORTS", null);

    // Configure resilient web host with port from configuration
    if (snapDogConfig.Http.ApiEnabled)
    {
        builder.WebHost.UseResilientKestrel(snapDogConfig.Http);
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
    if (snapDogConfig.Http.ApiEnabled)
    {
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add SignalR
        builder.Services.AddSignalR();

        // Add authentication and authorization
        if (snapDogConfig.Http.ApiAuthEnabled && snapDogConfig.Http.ApiKeys.Count > 0)
        {
            // Configure API Key authentication
            builder
                .Services.AddAuthentication("ApiKey")
                .AddScheme<
                    AuthenticationSchemeOptions,
                    ApiKeyAuthenticationHandler
                >("ApiKey", null);

            builder.Services.AddAuthorization();

            Log.Information("API authentication enabled with {KeyCount} API keys", snapDogConfig.Http.ApiKeys.Count);
        }
        else
        {
            // Add minimal authentication scheme to satisfy authorization middleware
            builder.Services.AddAuthentication("Bearer")
                .AddScheme<AuthenticationSchemeOptions, NoOpAuthenticationHandler>("Bearer", null);

            builder.Services.AddAuthorization();

            Log.Information("API authentication disabled - ConditionalAuthorize attributes will allow all requests");
        }

        Log.Information("API server enabled on port {Port}", snapDogConfig.Http.HttpPort);
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

        if (snapDogConfig.Services.Knx.Enabled)
        {
            healthChecksBuilder.AddCheck(
                "knx",
                () =>
                {
                    try
                    {
                        // Simple UDP connectivity check for KNX gateway
                        using var client = new System.Net.Sockets.UdpClient();
                        client.Connect(snapDogConfig.Services.Knx.Gateway!, snapDogConfig.Services.Knx.Port);
                        return HealthCheckResult.Healthy($"KNX gateway reachable at {snapDogConfig.Services.Knx.Gateway}:{snapDogConfig.Services.Knx.Port}");
                    }
                    catch (Exception ex)
                    {
                        return HealthCheckResult.Unhealthy($"KNX gateway unreachable: {ex.Message}");
                    }
                },
                tags: ["ready"]
            );
        }
    }

    var app = builder.Build();

    // Add global exception handling as the first middleware
    app.UseGlobalExceptionHandling();

    // Add HTTP metrics middleware (after exception handling, before other middleware)
    app.UseMiddleware<HttpMetricsMiddleware>();

    // Configure the HTTP request pipeline (conditionally based on API configuration)
    if (snapDogConfig.Http.ApiEnabled)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseRouting();

        // Add authentication and authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Add WebUI static files and routing (conditionally based on configuration)
        if (snapDogConfig.Http.WebUiEnabled)
        {
            app.UseStaticFiles();
            Log.Information("WebUI enabled - serving static files from wwwroot");
        }
        else
        {
            Log.Information("WebUI disabled via configuration (SNAPDOG_HTTP_WEBUI_ENABLED=false)");
        }

        app.MapControllers();

        // Map SignalR hub
        app.MapHub<SnapDogHub>("/hubs/snapdog/v1");

        // Add WebUI fallback routing (conditionally based on configuration)
        if (snapDogConfig.Http.WebUiEnabled)
        {
            app.MapFallbackToFile("index.html");
            Log.Information("WebUI fallback routing enabled - SPA routing to index.html");
        }

        // Map health check endpoints
        if (snapDogConfig.System.HealthChecksEnabled)
        {
            app.MapHealthChecks("/health");
            app.MapHealthChecks(
                "/health/ready",
                new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready"),
                }
            );
            app.MapHealthChecks(
                "/health/live",
                new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("live"),
                }
            );
        }
    }

    return app;
}

// Make Program class accessible to tests
namespace SnapDog2
{
    public class Program { }
}
