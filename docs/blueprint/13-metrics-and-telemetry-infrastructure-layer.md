# Metrics and Telemetry (Infrastructure Layer)

This chapter details the strategy and implementation for observability within SnapDog2, encompassing metrics, distributed tracing, and correlated logging. The primary framework used is **OpenTelemetry**, leveraging the standard .NET abstractions (`System.Diagnostics.ActivitySource`, `System.Diagnostics.Metrics.Meter`) and configured exporters. Observability components reside primarily within the `/Infrastructure/Observability` folder.

## 13.1 Overview

Comprehensive observability is crucial for understanding application behavior, diagnosing issues, and monitoring performance, even in a single-server home application. SnapDog2 adopts OpenTelemetry as the standard framework to achieve this, providing:

* **Distributed Tracing:** To track the flow of requests and operations across different logical components (API, MediatR Handlers, Infrastructure Services).
* **Metrics:** To quantify application performance, resource usage, and key operational counts (e.g., commands processed, errors).
* **Correlated Logging:** To link log events directly to the specific trace and span that generated them, significantly simplifying debugging.

## 13.2 Scope

OpenTelemetry instrumentation aims to cover critical paths and components:

* **Incoming Requests:** ASP.NET Core instrumentation automatically traces incoming API requests.
* **Internal Processing:** MediatR pipeline behaviors (`LoggingBehavior`, `PerformanceBehavior`) are instrumented to create spans representing command/query handling.
* **External Dependencies:**
  * `HttpClient` instrumentation automatically traces outgoing HTTP requests (e.g., to Subsonic).
  * Manual instrumentation (creating specific `Activity` spans) is applied within key methods of infrastructure services (`SnapcastService`, `KnxService`, `MqttService`, `MediaPlayerService`) for significant operations or external interactions not covered automatically.
* **Custom Metrics:** Key application events and performance indicators are measured using custom `Meter` instruments.

## 13.3 Telemetry Types

1. **Metrics:**
    * **Goal:** Provide quantitative data on application health and performance.
    * **Implementation:** Uses `System.Diagnostics.Metrics.Meter`. Standard instruments provided by OpenTelemetry (`AspNetCore`, `HttpClient`, `Runtime`, `Process`) are enabled. Custom metrics (e.g., MediatR request counts/duration, playback events) defined via `IMetricsService`.
    * **Export:** Primarily exported in **Prometheus** format via an ASP.NET Core endpoint (`/metrics`), configured via `SNAPDOG_PROMETHEUS_*` variables (Section 10). Console exporter used as fallback/for debugging.
2. **Tracing:**
    * **Goal:** Visualize the flow and duration of operations across components.
    * **Implementation:** Uses `System.Diagnostics.ActivitySource`. Automatic instrumentation for ASP.NET Core and HttpClient. Manual instrumentation (`ActivitySource.StartActivity()`) within MediatR behaviors and key infrastructure service methods.
    * **Export:** Exported using the **OpenTelemetry Protocol (OTLP)**, configured via `SNAPDOG_TELEMETRY_OTLP_*` variables (Section 10). This allows sending traces to various compatible backends like Jaeger, Tempo, Grafana Cloud Traces, etc. Console exporter used as fallback/for debugging.
    * **Sampling:** Configured via `SNAPDOG_TELEMETRY_SAMPLING_RATE` (default 1.0 = sample all traces).
3. **Logging:**
    * **Goal:** Provide detailed, contextual event information correlated with traces.
    * **Implementation:** Uses `Microsoft.Extensions.Logging.ILogger<T>` with Serilog backend and **LoggerMessage Source Generators**.
    * **Correlation:** OpenTelemetry logging integration (configured in `Program.cs`) automatically enriches log events with the `TraceId` and `SpanId` of the current `Activity`. Serilog output templates are configured to include these IDs (Section 5.2).

## 13.4 OpenTelemetry Setup (DI / `/Worker/DI/ObservabilityExtensions.cs`)

OpenTelemetry pipelines for tracing, metrics, and logging are configured during application startup using Dependency Injection.

```csharp
// Located in /Worker/DI/ObservabilityExtensions.cs
namespace SnapDog2.Worker.DI;

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

## 13.5 Custom Metrics (`IMetricsService` / `OpenTelemetryMetricsService`)

Define application-specific metrics using `System.Diagnostics.Metrics.Meter` via a dedicated service abstraction (`IMetricsService`) implemented in `/Infrastructure/Observability`.

```csharp
// Abstraction (/Core/Abstractions/IMetricsService.cs)
namespace SnapDog2.Core.Abstractions;

/// <summary>
/// Defines methods for recording application-specific metrics.
/// </summary>
public interface IMetricsService
{
    void RecordMediatrRequestDuration(string requestType, string requestName, double durationMs, bool success);
    void RecordZonePlaybackEvent(int zoneId, string eventType); // e.g., "play", "stop", "pause", "next", "prev"
    void IncrementClientConnectionCounter(bool connected); // True for connect, false for disconnect
    void RecordExternalCallDuration(string serviceName, string operation, double durationMs, bool success);
    // Add more specific metric recording methods as needed
}

// Implementation (/Infrastructure/Observability/OpenTelemetryMetricsService.cs)
namespace SnapDog2.Infrastructure.Observability;

using System.Diagnostics.Metrics;
using System.Collections.Generic;
using SnapDog2.Core.Abstractions;
using SnapDog2.Worker.DI; // To access the static Meter instance defined in ObservabilityExtensions

/// <summary>
/// Implements IMetricsService using System.Diagnostics.Metrics for OpenTelemetry.
/// </summary>
public class OpenTelemetryMetricsService : IMetricsService
{
    // Define instruments using the shared Meter
    private readonly Counter<long> _mediatrRequestCounter;
    private readonly Histogram<double>_mediatrRequestDuration;
    private readonly Counter<long> _playbackEventCounter;
    private readonly Counter<long>_clientConnectionCounter;
    private readonly Histogram<double> _externalCallDuration;

    public OpenTelemetryMetricsService()
    {
        // Use the static Meter defined in ObservabilityExtensions
        var meter = ObservabilityExtensions.SnapDogMeter;

        _mediatrRequestCounter = meter.CreateCounter<long>(
            "snapdog.mediatr.requests.count",
            description: "Number of MediatR requests processed.");

        _mediatrRequestDuration = meter.CreateHistogram<double>(
            "snapdog.mediatr.requests.duration",
            unit: "ms",
            description: "Duration of MediatR request handling.");

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

    public void RecordMediatrRequestDuration(string requestType, string requestName, double durationMs, bool success)
    {
        var tags = new TagList // Use TagList for performance
        {
            { "request.type", requestType },
            { "request.name", requestName },
            { "success", success }
         };
        _mediatrRequestCounter.Add(1, tags);
        _mediatrRequestDuration.Record(durationMs, tags);
    }

    public void RecordZonePlaybackEvent(int zoneId, string eventType)
    {
         var tags = new TagList {
            { "zone.id", zoneId },
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

Inject `IMetricsService` into components (like MediatR Behaviors, Infrastructure Services) where metrics need to be recorded.

## 13.6 Manual Tracing Instrumentation

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

// In MediatR LoggingBehavior (/Server/Behaviors/LoggingBehavior.cs)
// using var activity = _activitySource.StartActivity($"{requestType}:{requestName}", ActivityKind.Internal);
// ... set tags, status, record exceptions based on handler outcome ...
```

## 13.7 Logging Correlation

Ensure Serilog (or chosen logging provider) is configured with OpenTelemetry integration (`loggingBuilder.AddOpenTelemetry(...)` in DI setup) and output templates include `{TraceId}` and `{SpanId}`. This automatically links logs to the currently active trace span.
