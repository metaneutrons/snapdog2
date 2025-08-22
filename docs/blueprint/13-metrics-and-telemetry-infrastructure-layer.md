# 12. Metrics and Telemetry (Infrastructure Layer)

This chapter details the strategy and implementation for observability within SnapDog2, encompassing metrics, distributed tracing, and correlated logging. The framework used is **OpenTelemetry with OTLP (OpenTelemetry Protocol)** for vendor-neutral telemetry export. SnapDog2 implements only OTLP - the choice of observability backend (Jaeger, SigNoz, Prometheus, etc.) is a deployment concern.

## 12.1. Overview

SnapDog2 follows a **vendor-neutral observability approach** using OpenTelemetry Protocol (OTLP). This provides maximum flexibility in choosing observability backends without requiring application code changes. The application exports all three observability signals (traces, metrics, logs) via OTLP to any compatible backend.

**Architecture:**
```
SnapDog2 Application → OTLP → [Jaeger | SigNoz | Prometheus | Any OTLP Backend]
```

**Supported Backends:**
- **Jaeger**: Distributed tracing
- **SigNoz**: Unified observability (traces, metrics, logs)
- **Prometheus**: Metrics collection (via OpenTelemetry Collector)
- **Grafana Cloud**: Managed observability
- **Any OTLP-compatible backend**

The three pillars of observability are:

* **Distributed Tracing:** Track request flow across components (API, Cortex.Mediator, Infrastructure Services)
* **Metrics:** Quantify performance, resource usage, and operational counts
* **Correlated Logging:** Link log events to specific traces and spans for simplified debugging

## 12.2. Configuration

### 12.2.1. Environment Variables

All telemetry configuration uses the `SNAPDOG_TELEMETRY_` prefix:

```bash
# Core telemetry settings
SNAPDOG_TELEMETRY_ENABLED=true                        # Default: false
SNAPDOG_TELEMETRY_SERVICE_NAME=SnapDog2               # Default: SnapDog2
SNAPDOG_TELEMETRY_SAMPLING_RATE=1.0                   # Default: 1.0

# OTLP Configuration (vendor-neutral)
SNAPDOG_TELEMETRY_OTLP_ENABLED=true                   # Default: false
SNAPDOG_TELEMETRY_OTLP_ENDPOINT=http://localhost:4317 # Default: http://localhost:4317
SNAPDOG_TELEMETRY_OTLP_PROTOCOL=grpc                  # Default: grpc (grpc|http/protobuf)
SNAPDOG_TELEMETRY_OTLP_HEADERS=key1=value1,key2=value2 # Optional authentication headers
SNAPDOG_TELEMETRY_OTLP_TIMEOUT=30                     # Default: 30 (seconds)
```

### 12.2.2. Backend-Specific Examples

**Jaeger:**
```bash
SNAPDOG_TELEMETRY_OTLP_ENDPOINT=http://jaeger:14268/api/traces
SNAPDOG_TELEMETRY_OTLP_PROTOCOL=http/protobuf
```

**SigNoz:**
```bash
SNAPDOG_TELEMETRY_OTLP_ENDPOINT=http://signoz-otel-collector:4317
SNAPDOG_TELEMETRY_OTLP_PROTOCOL=grpc
```

**OpenTelemetry Collector:**
```bash
SNAPDOG_TELEMETRY_OTLP_ENDPOINT=http://otel-collector:4317
SNAPDOG_TELEMETRY_OTLP_PROTOCOL=grpc
SNAPDOG_TELEMETRY_OTLP_HEADERS=authorization=Bearer token123
```

## 12.3. Implementation

### 12.3.1. TelemetryConfig Class

```csharp
namespace SnapDog2.Core.Configuration;

/// <summary>
/// Telemetry and observability configuration.
/// SnapDog2 uses OpenTelemetry Protocol (OTLP) for vendor-neutral telemetry export.
/// </summary>
public class TelemetryConfig
{
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    [Env(Key = "SERVICE_NAME", Default = "SnapDog2")]
    public string ServiceName { get; set; } = "SnapDog2";

    [Env(Key = "SAMPLING_RATE", Default = 1.0)]
    public double SamplingRate { get; set; } = 1.0;

    [Env(NestedPrefix = "OTLP_")]
    public OtlpConfig Otlp { get; set; } = new();
}

/// <summary>
/// OTLP configuration for vendor-neutral telemetry export.
/// </summary>
public class OtlpConfig
{
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    [Env(Key = "ENDPOINT", Default = "http://localhost:4317")]
    public string Endpoint { get; set; } = "http://localhost:4317";

    [Env(Key = "PROTOCOL", Default = "grpc")]
    public string Protocol { get; set; } = "grpc";

    [Env(Key = "HEADERS")]
    public string? Headers { get; set; }

    [Env(Key = "TIMEOUT", Default = 30)]
    public int TimeoutSeconds { get; set; } = 30;
}
```

### 12.3.2. OpenTelemetry Setup

```csharp
// Program.cs - OpenTelemetry configuration
if (snapDogConfig.Telemetry.Enabled && snapDogConfig.Telemetry.Otlp.Enabled)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(snapDogConfig.Telemetry.ServiceName, "2.0.0")
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName,
                ["service.namespace"] = "snapdog"
            }))
        .WithTracing(tracing => tracing
            .SetSampler(new TraceIdRatioBasedSampler(snapDogConfig.Telemetry.SamplingRate))
            .AddAspNetCoreInstrumentation(options => options.RecordException = true)
            .AddHttpClientInstrumentation()
            .AddSource("SnapDog2.*")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(snapDogConfig.Telemetry.Otlp.Endpoint);
                options.Protocol = snapDogConfig.Telemetry.Otlp.Protocol.ToLowerInvariant() switch
                {
                    "grpc" => OtlpExportProtocol.Grpc,
                    "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                    _ => OtlpExportProtocol.Grpc
                };
                options.TimeoutMilliseconds = snapDogConfig.Telemetry.Otlp.TimeoutSeconds * 1000;
                
                if (!string.IsNullOrEmpty(snapDogConfig.Telemetry.Otlp.Headers))
                {
                    options.Headers = snapDogConfig.Telemetry.Otlp.Headers;
                }
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("SnapDog2.*")
            .AddOtlpExporter(/* same configuration as tracing */));
}
```

## 12.4. Scope

OpenTelemetry instrumentation covers critical application paths:

* **Incoming Requests:** ASP.NET Core instrumentation automatically traces API requests
* **Internal Processing:** Cortex.Mediator pipeline behaviors create spans for command/query handling
* **External Dependencies:**
  * `HttpClient` instrumentation automatically traces outgoing HTTP requests (Subsonic, etc.)
  * Manual instrumentation for infrastructure services (`SnapcastService`, `KnxService`, `MqttService`)
* **Custom Metrics:** Application-specific events and performance indicators

## 12.5. Telemetry Types

### 12.5.1. Traces
- **Goal:** Visualize operation flow and duration across components
- **Implementation:** `System.Diagnostics.ActivitySource` with automatic and manual instrumentation
- **Export:** OTLP to any compatible backend (Jaeger, SigNoz, etc.)
- **Sampling:** Configurable via `SNAPDOG_TELEMETRY_SAMPLING_RATE`

### 12.5.2. Metrics
- **Goal:** Quantitative data on application health and performance
- **Implementation:** `System.Diagnostics.Metrics.Meter` with custom instruments
- **Export:** OTLP to any compatible backend (Prometheus via collector, SigNoz, etc.)
- **Instruments:** Counters, histograms, gauges for key application events

### 12.5.3. Logs
- **Goal:** Detailed contextual information correlated with traces
- **Implementation:** `Microsoft.Extensions.Logging.ILogger<T>` with Serilog
- **Correlation:** Automatic enrichment with `TraceId` and `SpanId`
- **Export:** Structured logging with trace correlation

## 12.6. Benefits

### 12.6.1. Vendor Neutrality
- **Backend Agnostic:** Switch observability platforms without code changes
- **Future Proof:** Works with emerging OTLP-compatible platforms
- **Cost Flexibility:** Choose between self-hosted and managed solutions

### 12.6.2. Operational Benefits
- **Unified Configuration:** Single OTLP endpoint for all telemetry signals
- **Simple Deployment:** No backend-specific configuration in application
- **Easy Migration:** Change backends by updating deployment configuration

### 12.6.3. Development Experience
- **Consistent Instrumentation:** Same code works with any backend
- **Local Development:** Easy to enable/disable observability
- **Testing:** Mock OTLP endpoints for integration tests

## 12.7. OpenTelemetry Setup (DI / `/Worker/DI/ObservabilityExtensions.cs`)

OpenTelemetry pipelines for tracing, metrics, and logging are configured during application startup using Dependency Injection.

```csharp
// Located in /Worker/DI/ObservabilityExtensions.cs
namespace SnapDog2.Extensions.DependencyInjection;

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SnapDog2.Core.Configuration; // For TelemetryOptions, OtlpProtocol
using SnapDog2.Infrastructure.Observability; // For IMetricsService and its implementation
using OpenTelemetry.Exporter; // For OtlpExportProtocol
using Microsoft.AspNetCore.Builder; // For WebApplication extension method

/// <summary>
/// Extension methods for configuring observability (Telemetry, Metrics, Logging).
/// </summary>
public static class ObservabilityExtensions
{
    // Define shared ActivitySource and Meter for the application.
    // Services/components should obtain these or use DI if preferred.
    public static readonly ActivitySource SnapDogActivitySource = new("SnapDog2", GetVersion()); // Include version
    public static readonly Meter SnapDogMeter = new("SnapDog2", GetVersion()); // Include version

    /// <summary>
    /// Adds OpenTelemetry tracing, metrics, and logging integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapDogObservability(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind TelemetryOptions from configuration (e.g., "Telemetry" section)
        services.Configure<TelemetryOptions>(configuration.GetSection("Telemetry"));
        // Resolve options for immediate use during setup
        var telemetryOptions = configuration.GetSection("Telemetry").Get<TelemetryOptions>() ?? new TelemetryOptions();

        // Skip configuration if telemetry is disabled globally
        if (!telemetryOptions.Enabled)
        {
            Console.WriteLine("Telemetry is disabled via configuration."); // Use console before logger is fully configured
            return services;
        }

        // Define shared resource attributes for all telemetry signals
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(telemetryOptions.ServiceName, serviceVersion: GetVersion())
            .AddTelemetrySdk() // Includes basic SDK info
            .AddEnvironmentVariableDetector(); // Adds environment variables as resource attributes

        // Configure OpenTelemetry services
        services.AddOpenTelemetry()
            // --- Tracing Configuration ---
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(SnapDogActivitySource.Name) // Register the application's ActivitySource
                    // Add automatic instrumentation libraries:
                    .AddAspNetCoreInstrumentation(opts => { // Instrument ASP.NET Core requests
                        opts.RecordException = true; // Automatically record exceptions on spans
                        // Optionally filter out noisy endpoints like metrics/health/swagger
                        opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments(telemetryOptions.Prometheus.Path) &&
                                             !ctx.Request.Path.StartsWithSegments("/health") &&
                                             !ctx.Request.Path.StartsWithSegments("/swagger");
                     })
                    .AddHttpClientInstrumentation(opts => opts.RecordException = true); // Instrument outgoing HttpClient calls

                // Configure the OTLP Exporter for traces
                tracerProviderBuilder.AddOtlpExporter(otlpOptions =>
                {
                    try {
                        otlpOptions.Endpoint = new Uri(telemetryOptions.OtlpExporter.Endpoint);
                    } catch (UriFormatException ex) {
                        // Log configuration error - startup validation should catch this ideally
                         Console.Error.WriteLine($"ERROR: Invalid OTLP Endpoint format: {telemetryOptions.OtlpExporter.Endpoint}. {ex.Message}");
                         // Potentially default to a safe value or prevent startup?
                         otlpOptions.Endpoint = new Uri("http://localhost:4317"); // Safe default
                    }

                    if (telemetryOptions.OtlpExporter.Protocol == OtlpProtocol.Grpc) {
                         otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                    } else if (telemetryOptions.OtlpExporter.Protocol == OtlpProtocol.HttpProtobuf) {
                         otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                    } else {
                         Console.Error.WriteLine($"WARN: Invalid OTLP Protocol '{telemetryOptions.OtlpExporter.Protocol}'. Defaulting to gRPC.");
                         otlpOptions.Protocol = OtlpExportProtocol.Grpc; // Default protocol
                    }

                    if(!string.IsNullOrWhiteSpace(telemetryOptions.OtlpExporter.Headers)) {
                         otlpOptions.Headers = telemetryOptions.OtlpExporter.Headers;
                    }
                });

                // Add ConsoleExporter for debugging traces locally if needed
                // tracerProviderBuilder.AddConsoleExporter();

                // Configure Sampling strategy
                tracerProviderBuilder.SetSampler(new TraceIdRatioBasedSampler(telemetryOptions.SamplingRate));
            })
            // --- Metrics Configuration ---
            .WithMetrics(meterProviderBuilder =>
            {
                 meterProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddMeter(SnapDogMeter.Name) // Register the application's Meter
                    // Add automatic instrumentation libraries:
                    .AddRuntimeInstrumentation() // Collects GC counts, heap size, etc.
                    .AddProcessInstrumentation() // Collects CPU, memory usage for the process
                    .AddAspNetCoreInstrumentation() // Collects request duration, active requests, etc.
                    .AddHttpClientInstrumentation(); // Collects outgoing request duration, etc.

                // Configure the Prometheus Exporter if enabled
                if (telemetryOptions.Prometheus.Enabled) {
                     meterProviderBuilder.AddPrometheusExporter(opts => {
                        // Can configure scraping endpoint options here if needed, but usually done via MapPrometheusScrapingEndpoint
                     });
                } else {
                     // Add ConsoleExporter as a fallback if Prometheus is disabled
                      meterProviderBuilder.AddConsoleExporter((exporterOptions, metricReaderOptions) => {
                            metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 15000; // Export metrics to console every 15s
                      });
                }
            });

        // --- Logging Configuration (Integration with OpenTelemetry) ---
        // This ensures TraceId and SpanId are available to the logging pipeline (e.g., Serilog)
        services.AddLogging(loggingBuilder =>
        {
            // Clear default providers if Serilog is the sole provider
            // loggingBuilder.ClearProviders(); // Do this in Program.cs before UseSerilog() if needed

            loggingBuilder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);
                options.IncludeFormattedMessage = true; // Include formatted message in log records
                options.IncludeScopes = true; // Include logger scopes
                options.ParseStateValues = true; // Attempt to parse state values

                // Configure exporting logs via OTLP (Optional - separate from traces/metrics)
                // options.AddOtlpExporter(otlpOptions => { /* Configure OTLP endpoint/protocol for logs */ });

                // Add ConsoleExporter for logs (useful for seeing OTel-formatted logs)
                 options.AddConsoleExporter();
            });
        });

        // Register custom Metrics Service implementation
        services.AddSingleton<IMetricsService, OpenTelemetryMetricsService>();

        Console.WriteLine("OpenTelemetry Observability enabled and configured."); // Use console before logger might be ready
        return services;
    }

     /// <summary>
     /// Maps the Prometheus scraping endpoint if enabled in configuration.
     /// Call this on the `WebApplication` instance in Program.cs.
     /// </summary>
     public static WebApplication MapSnapDogObservability(this WebApplication app) {
          // Resolve options from the fully built host
          var telemetryOptions = app.Services.GetRequiredService<IOptions<TelemetryOptions>>().Value;
          if(telemetryOptions.Enabled && telemetryOptions.Prometheus.Enabled) {
               app.MapPrometheusScrapingEndpoint(telemetryOptions.Prometheus.Path);
               app.Logger.LogInformation("Prometheus metrics scraping endpoint configured at {Path}", telemetryOptions.Prometheus.Path);
          }
          return app;
     }


    private static string GetVersion() =>
        System.Reflection.Assembly.GetEntryAssembly()?.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
        System.Reflection.Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ??
        "unknown";
}
```

## 12.8. Custom Metrics (`IMetricsService` / `OpenTelemetryMetricsService`)

Define application-specific metrics using `System.Diagnostics.Metrics.Meter` via a dedicated service abstraction (`IMetricsService`) implemented in `/Infrastructure/Observability`.

```csharp
// Abstraction (/Core/Abstractions/IMetricsService.cs)
namespace SnapDog2.Core.Abstractions;

/// <summary>
/// Defines methods for recording application-specific metrics.
/// </summary>
public interface IMetricsService
{
    void RecordCortexMediatorRequestDuration(string requestType, string requestName, double durationMs, bool success);
    void RecordZonePlaybackEvent(int zoneIndex, string eventType); // e.g., "play", "stop", "pause", "next", "prev"
    void IncrementClientConnectionCounter(bool connected); // True for connect, false for disconnect
    void RecordExternalCallDuration(string serviceName, string operation, double durationMs, bool success);
    // Add more specific metric recording methods as needed
}

// Implementation (/Infrastructure/Observability/OpenTelemetryMetricsService.cs)
namespace SnapDog2.Infrastructure.Observability;

using System.Diagnostics.Metrics;
using System.Collections.Generic;
using SnapDog2.Core.Abstractions;
using SnapDog2.Extensions.DependencyInjection; // To access the static Meter instance defined in ObservabilityExtensions

/// <summary>
/// Implements IMetricsService using System.Diagnostics.Metrics for OpenTelemetry.
/// </summary>
public class OpenTelemetryMetricsService : IMetricsService
{
    // Define instruments using the shared Meter
    private readonly Counter<long> _cortexMediatorRequestCounter;
    private readonly Histogram<double>_cortexMediatorRequestDuration;
    private readonly Counter<long> _playbackEventCounter;
    private readonly Counter<long>_clientConnectionCounter;
    private readonly Histogram<double> _externalCallDuration;

    public OpenTelemetryMetricsService()
    {
        // Use the static Meter defined in ObservabilityExtensions
        var meter = ObservabilityExtensions.SnapDogMeter;

        _cortexMediatorRequestCounter = meter.CreateCounter<long>(
            "snapdog.cortex_mediator.requests.count",
            description: "Number of Cortex.Mediator requests processed.");

        _cortexMediatorRequestDuration = meter.CreateHistogram<double>(
            "snapdog.cortex_mediator.requests.duration",
            unit: "ms",
            description: "Duration of Cortex.Mediator request handling.");

        _playbackEventCounter = meter.CreateCounter<long>(
             "snapdog.zone.playback.events.count",
             description: "Number of zone playback events.");

        _clientConnectionCounter = meter.CreateCounter<long>(
             "snapdog.client.connections.count",
             description: "Number of client connect/disconnect events.");

         _externalCallDuration = meter.CreateHistogram<double>(
             "snapdog.external.calls.duration",
             unit: "ms",
             description: "Duration of calls to external services (Snapcast, Subsonic, KNX, etc.).");
    }

    public void RecordCortexMediatorRequestDuration(string requestType, string requestName, double durationMs, bool success)
    {
        var tags = new TagList // Use TagList for performance
        {
            { "request.type", requestType },
            { "request.name", requestName },
            { "success", success }
         };
        _cortexMediatorRequestCounter.Add(1, tags);
        _cortexMediatorRequestDuration.Record(durationMs, tags);
    }

    public void RecordZonePlaybackEvent(int zoneIndex, string eventType)
    {
         var tags = new TagList {
            { "zone.id", zoneIndex },
            { "event.type", eventType }
         };
        _playbackEventCounter.Add(1, tags);
    }

     public void IncrementClientConnectionCounter(bool connected) {
          var tags = new TagList {
               { "event.type", connected ? "connect" : "disconnect" }
          };
          _clientConnectionCounter.Add(1, tags);
     }

     public void RecordExternalCallDuration(string serviceName, string operation, double durationMs, bool success)
     {
          var tags = new TagList {
               { "external.service", serviceName }, // e.g., "Snapcast", "Subsonic", "KNX"
               { "external.operation", operation }, // e.g., "SetClientVolume", "GetPlaylists", "WriteGroupValue"
               { "success", success }
          };
          _externalCallDuration.Record(durationMs, tags);
     }
}
```

Inject `IMetricsService` into components (like Cortex.Mediator Behaviors, Infrastructure Services) where metrics need to be recorded.

## 12.9. Manual Tracing Instrumentation

Use the shared `ActivitySource` (`ObservabilityExtensions.SnapDogActivitySource`) to manually create Activities (spans) for important operations not covered by automatic instrumentation. Use `using var activity = _activitySource.StartActivity(...)`, add relevant tags (`activity.SetTag`), record exceptions (`activity.RecordException`), and set status (`activity.SetStatus`).

```csharp
// Example in a service method (/Infrastructure/Subsonic/SubsonicService.cs)
public partial class SubsonicService : ISubsonicService
{
     private readonly HttpClient _httpClient;
     private static readonly ActivitySource _activitySource = ObservabilityExtensions.SnapDogActivitySource;
     private readonly IMetricsService _metricsService; // Inject metrics service
     // ... logger, config ...

     public async Task<Result<List<PlaylistInfo>>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
     {
          // Start a custom activity span for this specific operation
          using var activity = _activitySource.StartActivity("Subsonic.GetPlaylists", ActivityKind.Client);
          activity?.SetTag("subsonic.operation", "getPlaylists"); // Specific tag
          var stopwatch = Stopwatch.StartNew(); // Time the external call duration
          bool success = false;

          try {
               // HttpClient call is automatically instrumented, creating a child span
               var result = await _subsonicClient.GetPlaylistsAsync(cancellationToken).ConfigureAwait(false);
               // ... mapping logic ...
               success = true; // Assume success if no exception from library/mapping
               activity?.SetStatus(ActivityStatusCode.Ok); // Set span status to OK
               return Result<List<PlaylistInfo>>.Success(mappedPlaylists);
          } catch (Exception ex) {
               activity?.SetStatus(ActivityStatusCode.Error, ex.Message); // Set span status to Error
               activity?.RecordException(ex); // Record exception details on the span
               LogApiError(nameof(GetPlaylistsAsync), ex); // Log the error
               return Result<List<PlaylistInfo>>.Failure(ex);
          } finally {
               stopwatch.Stop();
               // Record custom duration metric for this specific external call
               _metricsService.RecordExternalCallDuration("Subsonic", "GetPlaylists", stopwatch.ElapsedMilliseconds, success);
          }
     }
}

// In Cortex.Mediator LoggingBehavior (/Server/Behaviors/LoggingBehavior.cs)
// using var activity = _activitySource.StartActivity($"{requestType}:{requestName}", ActivityKind.Internal);
// ... set tags, status, record exceptions based on handler outcome ...
```

## 12.10. Logging Correlation

Ensure Serilog (or chosen logging provider) is configured with OpenTelemetry integration (`loggingBuilder.AddOpenTelemetry(...)` in DI setup) and output templates include `{TraceId}` and `{SpanId}`. This automatically links logs to the currently active trace span.
