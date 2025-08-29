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
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using SnapDog2.Authentication;
using SnapDog2.Core.Configuration;
using SnapDog2.Extensions;
using SnapDog2.Extensions.DependencyInjection;
using SnapDog2.Helpers;
using SnapDog2.Hosting;
using SnapDog2.Middleware;

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

    // Add command processing (Mediator, handlers, behaviors)
    builder.Services.AddCommandProcessing();

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
    }
    else
    {
        Log.Information("KNX is disabled in configuration (SNAPDOG_SERVICES_KNX_ENABLED=false)");
    }

    // Notification processing configuration and services
    builder.Services.ConfigureNotificationProcessing();
    builder.Services.AddSingleton<SnapDog2.Infrastructure.Notifications.NotificationQueue>();
    builder.Services.AddSingleton<SnapDog2.Core.Abstractions.INotificationQueue>(sp =>
        sp.GetRequiredService<SnapDog2.Infrastructure.Notifications.NotificationQueue>()
    );
    builder.Services.AddHostedService<SnapDog2.Infrastructure.Notifications.NotificationBackgroundService>();

    // Skip hosted services in test environment
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Testing")
    {
        // Add resilient startup service as first hosted service (check if everything is healthy)
        builder.Services.AddHostedService<SnapDog2.Services.StartupService>();

        // Add hosted service to initialize integration services on startup
        builder.Services.AddHostedService<SnapDog2.Worker.Services.IntegrationServicesHostedService>();

        // Add zone grouping configuration
        builder.Services.Configure<SnapDog2.Core.Configuration.SnapcastConfig>(_ => { });

        // Add continuous background service for automatic zone grouping
        builder.Services.AddHostedService<SnapDog2.Services.ZoneGroupingBackgroundService>();

        // Add hosted service to publish initial state after integration services are initialized
        // Skip in test environment to prevent hanging issues
        if (!builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddHostedService<SnapDog2.Services.StatePublishingService>();
        }
    }

    // Register status services
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IAppStatusService,
        SnapDog2.Infrastructure.Application.AppStatusService
    >();
    builder.Services.AddSingleton<
        SnapDog2.Core.Abstractions.ICommandStatusService,
        SnapDog2.Infrastructure.Services.CommandStatusService
    >();
    builder.Services.AddScoped<
        SnapDog2.Server.Features.Global.Services.Abstractions.IGlobalStatusService,
        SnapDog2.Server.Features.Global.Services.GlobalStatusService
    >();

    // Zone management services
    builder.Services.AddSingleton<
        SnapDog2.Core.Abstractions.IZoneStateStore,
        SnapDog2.Infrastructure.Storage.InMemoryZoneStateStore
    >();

    // Client management services
    builder.Services.AddSingleton<
        SnapDog2.Core.Abstractions.IClientStateStore,
        SnapDog2.Infrastructure.Storage.InMemoryClientStateStore
    >();

    // Redis persistent state storage services
    if (snapDogConfig.Redis.Enabled)
    {
        // Register Redis connection multiplexer
        builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
        {
            var configuration = StackExchange.Redis.ConfigurationOptions.Parse(snapDogConfig.Redis.ConnectionString);
            configuration.ConnectTimeout = snapDogConfig.Redis.TimeoutSeconds * 1000;
            configuration.SyncTimeout = snapDogConfig.Redis.TimeoutSeconds * 1000;
            configuration.DefaultDatabase = snapDogConfig.Redis.Database;
            configuration.AbortOnConnectFail = false; // Allow retries
            configuration.ConnectRetry = 3;

            return StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);
        });

        // Register persistent state store
        builder.Services.AddSingleton<
            SnapDog2.Core.Abstractions.IPersistentStateStore
        >(provider =>
        {
            var redis = provider.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
            var logger = provider.GetRequiredService<ILogger<SnapDog2.Infrastructure.Storage.RedisPersistentStateStore>>();
            return new SnapDog2.Infrastructure.Storage.RedisPersistentStateStore(redis, snapDogConfig.Redis, logger);
        });

        // Register persistent state notification handler for client state changes
        builder.Services.AddSingleton<
            Cortex.Mediator.Notifications.INotificationHandler<SnapDog2.Server.Features.Clients.Notifications.ClientStateChangedNotification>,
            SnapDog2.Infrastructure.Notifications.PersistentStateNotificationHandler
        >();

        // Register state restoration service (only if Redis is enabled)
        builder.Services.AddHostedService<SnapDog2.Infrastructure.Services.StateRestorationService>();
    }
    else
    {
        Log.Information("🚫 Redis persistent state storage is disabled");
    }
    builder.Services.AddSingleton<
        SnapDog2.Core.Abstractions.IZoneManager,
        SnapDog2.Infrastructure.Domain.ZoneManager
    >();

    // Status factory for centralized status notification creation - Singleton for performance
    builder.Services.AddSingleton<
        SnapDog2.Core.Abstractions.IStatusFactory,
        SnapDog2.Server.Features.Shared.Factories.StatusFactory
    >();

    // Media player services - Singleton to persist across requests and scopes
    builder.Services.AddSingleton<
        SnapDog2.Core.Abstractions.IMediaPlayerService,
        SnapDog2.Infrastructure.Audio.MediaPlayerService
    >();

    // Client management services
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IClientManager,
        SnapDog2.Infrastructure.Domain.ClientManager
    >();

    // Zone grouping service for managing Snapcast client grouping based on zone assignments
    builder.Services.AddScoped<
        SnapDog2.Core.Abstractions.IZoneGroupingService,
        SnapDog2.Infrastructure.Services.ZoneGroupingService
    >();

    // Enterprise-grade metrics services
    builder.Services.AddSingleton<SnapDog2.Infrastructure.Metrics.ApplicationMetrics>();
    builder.Services.AddSingleton<SnapDog2.Core.Abstractions.IApplicationMetrics>(provider =>
        provider.GetRequiredService<SnapDog2.Infrastructure.Metrics.ApplicationMetrics>());
    builder.Services.AddSingleton<SnapDog2.Infrastructure.Application.EnterpriseMetricsService>();
    builder.Services.AddSingleton<SnapDog2.Infrastructure.Metrics.ZoneGroupingMetrics>();

    // Error tracking service
    builder.Services.AddSingleton<
        SnapDog2.Core.Abstractions.IErrorTrackingService,
        SnapDog2.Infrastructure.Application.ErrorTrackingService
    >();

    // Register EnterpriseMetricsService as the IMetricsService implementation
    builder.Services.AddSingleton<SnapDog2.Core.Abstractions.IMetricsService>(provider =>
        provider.GetRequiredService<SnapDog2.Infrastructure.Application.EnterpriseMetricsService>()
    );

    // Business metrics collection service
    builder.Services.AddHostedService<SnapDog2.Services.BusinessMetricsCollectionService>();

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

        // Add authentication and authorization
        if (snapDogConfig.Http.ApiAuthEnabled && snapDogConfig.Http.ApiKeys.Count > 0)
        {
            // Configure API Key authentication
            builder
                .Services.AddAuthentication("ApiKey")
                .AddScheme<
                    Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                    ApiKeyAuthenticationHandler
                >("ApiKey", null);

            builder.Services.AddAuthorization();

            Log.Information("API authentication enabled with {KeyCount} API keys", snapDogConfig.Http.ApiKeys.Count);
        }
        else if (snapDogConfig.Http.ApiAuthEnabled)
        {
            // Auth is enabled but no API keys configured - use dummy authentication for development
            builder
                .Services.AddAuthentication("Dummy")
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DummyAuthenticationHandler>(
                    "Dummy",
                    null
                );

            builder.Services.AddAuthorization();

            Log.Warning(
                "API authentication enabled but no API keys configured - using dummy authentication for development"
            );
        }
        else
        {
            // Auth is disabled - use dummy authentication to satisfy [Authorize] attributes
            builder
                .Services.AddAuthentication("Dummy")
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DummyAuthenticationHandler>(
                    "Dummy",
                    null
                );

            builder.Services.AddAuthorization();

            Log.Information("API authentication disabled - using dummy authentication");
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
            healthChecksBuilder.AddCheck<SnapDog2.Infrastructure.HealthChecks.KnxHealthCheck>(
                "knx",
                tags: ["ready"]
            );
        }

        // Register health check service wrapper for testability
        builder.Services.AddScoped<
            SnapDog2.Core.Abstractions.IAppHealthCheckService,
            SnapDog2.Infrastructure.Application.AppHealthCheckService
        >();
    }

    // WebUI Configuration (add after existing service registrations)
    if (snapDogConfig.Http.WebUiEnabled)
    {
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Add anti-forgery services for enterprise security
        builder.Services.AddAntiforgery();

        // Configure forwarded headers for reverse proxy awareness
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Register generated transport client
        builder.Services.AddHttpClient<SnapDog2.WebUi.ApiClient.Generated.IGeneratedSnapDogClient, SnapDog2.WebUi.ApiClient.Generated.GeneratedSnapDogClient>(client =>
        {
            var baseUrl = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                ? $"http://localhost:{snapDogConfig.Http.HttpPort}/api/v1/"
                : $"http://127.0.0.1:{snapDogConfig.Http.HttpPort}/api/v1/";

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "SnapDog2-WebUI/1.0");
        });

        // Register business API client
        builder.Services.AddScoped<SnapDog2.WebUi.ApiClient.ISnapDogApiClient, SnapDog2.WebUi.ApiClient.SnapDogApiClient>();

        Log.Information("🌐 WebUI enabled with resilient API client configured");
    }

    var app = builder.Build();

    // Add global exception handling as the first middleware
    app.UseGlobalExceptionHandling();

    // Add HTTP metrics middleware (after exception handling, before other middleware)
    app.UseMiddleware<SnapDog2.Middleware.HttpMetricsMiddleware>();

    // Configure the HTTP request pipeline (conditionally based on API configuration)
    if (snapDogConfig.Http.ApiEnabled)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseRouting();

        // Add authentication and authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Map health check endpoints
        if (snapDogConfig.System.HealthChecksEnabled)
        {
            app.MapHealthChecks("/health");
            app.MapHealthChecks(
                "/health/ready",
                new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready"),
                }
            );
            app.MapHealthChecks(
                "/health/live",
                new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("live"),
                }
            );
        }
    }

    // Add at the end of the app configuration
    if (snapDogConfig.Http.WebUiEnabled)
    {
        try
        {
            // Use forwarded headers for reverse proxy
            app.UseForwardedHeaders();

            // Configure path base for reverse proxy
            if (!string.IsNullOrEmpty(snapDogConfig.Http.WebUiPath) && snapDogConfig.Http.WebUiPath != "/")
            {
                app.UsePathBase(snapDogConfig.Http.WebUiPath);
            }

            // Configure path base for reverse proxy
            if (!string.IsNullOrEmpty(snapDogConfig.Http.WebUiPath) && snapDogConfig.Http.WebUiPath != "/")
            {
                app.UsePathBase(snapDogConfig.Http.WebUiPath);
            }

            // Add anti-forgery middleware
            app.UseAntiforgery();

            // Map Razor components with base path
            app.MapRazorComponents<SnapDog2.WebUi.App>()
                .AddInteractiveServerRenderMode();

            Log.Information("🌐 WebUI routes configured at {Path}", snapDogConfig.Http.WebUiPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to configure WebUI");
            throw;
        }
    }

    return app;
}

// Make Program class accessible to tests
public partial class Program { }
