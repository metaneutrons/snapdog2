# 6. Fault Tolerance Implementation (Infrastructure Layer)

## 6.1. Overview

Modern distributed systems and interactions with external services (like Snapcast servers, MQTT brokers, Subsonic APIs, KNX gateways) are inherently prone to transient failures, network issues, or temporary unavailability. SnapDog2 implements a comprehensive fault tolerance strategy to handle these situations gracefully, ensuring application stability and providing a more robust user experience.

The primary tool used for implementing resilience patterns is **Polly**, the standard and highly flexible .NET resilience and transient-fault-handling library. Polly policies are defined centrally and applied strategically within the **Infrastructure Layer** (`/Infrastructure`) services where interactions with external dependencies occur. This ensures that the core application logic (`/Server` layer) remains largely unaware of transient faults, dealing primarily with the final outcomes represented by the **Result Pattern** (Section 5.1).

## 6.2. Polly Integration and Policy Definitions

Polly provides a fluent API to define various resilience strategies. Common policies are defined in a central static class (`/Infrastructure/Resilience/ResiliencePolicies.cs`) for consistency and reuse. These policies incorporate logging (using LoggerMessage source generators) and OpenTelemetry tracing within their `onRetry` or `onBreak` delegates for observability.

```csharp
// Located in /Infrastructure/Resilience/ResiliencePolicies.cs
namespace SnapDog2.Infrastructure.Resilience;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly; // Core Polly namespace
using Polly.Contrib.WaitAndRetry; // For advanced backoff strategies
using Polly.Timeout; // For TimeoutRejectedException
using System.Diagnostics; // For ActivitySource
using Polly.Extensions.Http; // For HandleTransientHttpError

/// <summary>
/// Defines standard resilience policies using Polly for the application.
/// </summary>
public static partial class ResiliencePolicies
{
    // Central ActivitySource for resilience-related tracing spans (optional but useful)
    private static readonly ActivitySource ResilienceActivitySource = new("SnapDog2.Resilience");

    // Logger Messages (Defined using partial methods for source generation)
    [LoggerMessage(EventId = 701, Level = LogLevel.Warning, Message = "[Resilience] HTTP Request failed. Delaying {Delay} ms before retry {RetryAttempt}/{MaxRetries}. Uri: {RequestUri}")]
    static partial void LogHttpRetry(ILogger logger, double delay, int retryAttempt, int maxRetries, Uri? requestUri, Exception exception);

    [LoggerMessage(EventId = 702, Level = LogLevel.Error, Message = "[Resilience] Circuit breaker opened for {BreakDelayMs}ms due to failure. Uri: {RequestUri}")]
    static partial void LogCircuitBroken(ILogger logger, double breakDelayMs, Uri? requestUri, Exception exception);

    [LoggerMessage(EventId = 703, Level = LogLevel.Information, Message = "[Resilience] Circuit breaker reset. Uri: {RequestUri}")]
    static partial void LogCircuitReset(ILogger logger, Uri? requestUri);

    [LoggerMessage(EventId = 704, Level = LogLevel.Warning, Message = "[Resilience] Circuit breaker is half-open. Next call is a trial.")]
    static partial void LogCircuitHalfOpen(ILogger logger); // Added Uri for context

    [LoggerMessage(EventId = 705, Level = LogLevel.Warning, Message = "[Resilience] Operation '{OperationKey}' failed. Delaying {Delay} ms before retry {RetryAttempt}/{MaxRetries}.")]
    static partial void LogGeneralRetry(ILogger logger, string operationKey, double delay, int retryAttempt, int maxRetries, Exception exception);

    [LoggerMessage(EventId = 706, Level = LogLevel.Error, Message = "[Resilience] Timeout occurred after {TimeoutMs}ms for operation '{OperationKey}'.")]
    static partial void LogTimeout(ILogger logger, double timeoutMs, string operationKey);


    /// <summary>
    /// Gets a standard HTTP retry policy with exponential backoff and jitter.
    /// Handles transient HTTP errors (5xx, 408) and Polly timeouts.
    /// </summary>
    /// <param name="logger">Logger for retry attempts.</param>
    /// <param name="retryCount">Maximum number of retry attempts.</param>
    /// <returns>An asynchronous Polly policy for HttpResponseMessage.</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy(ILogger logger, int retryCount = 3)
    {
        // Use decorrelated jitter backoff V2 for better distribution of retries
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: retryCount);

        return HttpPolicyExtensions
            .HandleTransientHttpError() // Handles 5xx, 408 status codes
            .Or<TimeoutRejectedException>() // Handles timeouts injected by Polly's TimeoutPolicy
            .WaitAndRetryAsync(delay,
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Create a tracing activity for the retry attempt
                    using var activity = ResilienceActivitySource.StartActivity("HttpRetryAttempt");
                    activity?.AddTag("http.retry_attempt", retryAttempt);
                    activity?.AddTag("http.request.uri", context.GetHttpRequestMessage()?.RequestUri);
                    if(outcome.Exception != null) activity?.RecordException(outcome.Exception);

                    // Log the retry attempt using the source generated logger
                    LogHttpRetry(logger, timespan.TotalMilliseconds, retryAttempt, retryCount,
                                context.GetHttpRequestMessage()?.RequestUri, outcome.Exception!); // outcome.Exception should not be null here
                });
    }

    /// <summary>
    /// Gets a standard HTTP circuit breaker policy.
    /// Opens the circuit after a configured number of consecutive failures, preventing calls for a specified duration.
    /// </summary>
    /// <param name="logger">Logger for circuit state changes.</param>
    /// <param name="exceptionsAllowedBeforeBreaking">Number of consecutive exceptions allowed before breaking.</param>
    /// <param name="durationOfBreakSeconds">Duration the circuit stays open in seconds.</param>
    /// <returns>An asynchronous Polly policy for HttpResponseMessage.</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpCircuitBreakerPolicy(ILogger logger, int exceptionsAllowedBeforeBreaking = 5, int durationOfBreakSeconds = 30)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: exceptionsAllowedBeforeBreaking,
                durationOfBreak: TimeSpan.FromSeconds(durationOfBreakSeconds),
                onBreak: (outcome, breakDelay, context) => {
                     using var activity = ResilienceActivitySource.StartActivity("HttpCircuitBroken");
                     activity?.SetTag("http.request.uri", context.GetHttpRequestMessage()?.RequestUri);
                      if(outcome.Exception != null) activity?.RecordException(outcome.Exception);
                      activity?.SetTag("circuitbreaker.break_duration_ms", breakDelay.TotalMilliseconds);
                     LogCircuitBroken(logger, breakDelay.TotalMilliseconds, context.GetHttpRequestMessage()?.RequestUri, outcome.Exception!);
                },
                onReset: (context) => {
                    using var activity = ResilienceActivitySource.StartActivity("HttpCircuitReset");
                    activity?.SetTag("http.request.uri", context.GetHttpRequestMessage()?.RequestUri);
                    LogCircuitReset(logger, context.GetHttpRequestMessage()?.RequestUri);
                },
                onHalfOpen: () => { LogCircuitHalfOpen(logger); } // Log when circuit enters half-open state
            );
    }

    /// <summary>
    /// Gets a general-purpose retry policy for non-HTTP operations (e.g., TCP, library calls).
    /// Uses exponential backoff with jitter and an optional maximum retry count (negative for indefinite).
    /// </summary>
    /// <param name="logger">Logger for retry attempts.</param>
    /// <param name="operationKey">A key identifying the operation for logging/tracing.</param>
    /// <param name="retryCount">Number of retries. Use -1 for indefinite retries.</param>
    /// <param name="maxDelaySeconds">Maximum delay between retries.</param>
    /// <returns>An asynchronous Polly policy.</returns>
    public static IAsyncPolicy GetGeneralRetryPolicy(ILogger logger, string operationKey, int retryCount = 5, int maxDelaySeconds = 60)
    {
         bool retryForever = retryCount < 0;
         // Use a very large number for internal Polly count if forever, as WaitAndRetryForeverAsync needs different lambda signature
         int actualRetryCount = retryForever ? int.MaxValue - 1 : retryCount;
         var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(2), retryCount: actualRetryCount, maxDelay: TimeSpan.FromSeconds(maxDelaySeconds));

         // Define the action to take on each retry attempt
         Action<Exception, TimeSpan, int, Context> onRetryAction = (exception, timespan, attempt, context) =>
         {
             using var activity = ResilienceActivitySource.StartActivity("GeneralRetryAttempt");
             activity?.AddTag("operation.key", context.OperationKey ?? operationKey);
             activity?.AddTag("retry_attempt", attempt);
             activity?.RecordException(exception);
             // Log using the original requested retryCount (-1 for forever)
             LogGeneralRetry(logger, context.OperationKey ?? operationKey, timespan.TotalMilliseconds, attempt, retryCount, exception);
         };

         if(retryForever) {
             // Use WaitAndRetryForeverAsync for indefinite retries
             return Policy
                .Handle<Exception>(ex => ex is not OperationCanceledException) // Don't retry if cancellation was requested
                .WaitAndRetryForeverAsync(
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryAttempt), maxDelaySeconds)), // Exp backoff capped
                    onRetry: (exception, timespan, context) => { onRetryAction(exception, timespan, -1, context); } // Attempt number isn't available here easily
                );
         } else {
              // Use standard WaitAndRetryAsync for fixed number of retries
              return Policy
                .Handle<Exception>(ex => ex is not OperationCanceledException)
                .WaitAndRetryAsync(delay, onRetry: onRetryAction);
         }
    }

    /// <summary>
    /// Gets a standard timeout policy for individual operations.
    /// Uses Pessimistic strategy to ensure timeout enforcement via dedicated thread.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>An asynchronous Polly timeout policy.</returns>
    public static IAsyncPolicy GetTimeoutPolicy(TimeSpan timeout)
    {
        return Policy.TimeoutAsync(timeout, TimeoutStrategy.Pessimistic, onTimeoutAsync: (context, timespan, task, exception) => {
             // Log the timeout event
             var logger = context.GetLogger(); // Assumes logger is passed in context if possible, otherwise need specific logger instance
             if(logger != null) {
                  LogTimeout(logger, timespan.TotalMilliseconds, context.OperationKey ?? "UnknownOperation");
             }
             // Create a specific TimeoutRejectedException which can be handled by retry/circuit breaker policies
             throw new TimeoutRejectedException($"Operation timed out after {timespan.TotalMilliseconds}ms.");
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously - Expected for onTimeout delegate
        });
#pragma warning restore CS1998
    }

     // Helper to potentially get logger from context (requires passing it in ExecuteAsync)
     private static ILogger? GetLogger(this Context context) {
          if(context.TryGetValue("Logger", out var loggerObj) && loggerObj is ILogger logger) {
               return logger;
          }
          return null;
     }
}

// Helper to extract HttpRequestMessage from Polly Context for HttpClientFactory integration
namespace Polly.Extensions.Http {
     public static class HttpRequestMessageContextExtensions {
          private const string RequestKey = "HttpRequestMessage";
          public static Context WithHttpRequestMessage(this Context context, HttpRequestMessage request) {
               context[RequestKey] = request; return context;
          }
          public static HttpRequestMessage? GetHttpRequestMessage(this Context context) =>
               context.TryGetValue(RequestKey, out var request) && request is HttpRequestMessage httpRequestMessage ? httpRequestMessage : null;
     }
}
```

## 6.3. Resilience Strategies for Key Components

Specific policies are applied based on the nature of the interaction.

### 6.3.1. Snapcast Service Resilience (`SnapcastService`)

* **Connection (`InitializeAsync`, Reconnects):** Uses `GetGeneralRetryPolicy` configured for **indefinite retries** (`retryCount = -1`) with capped exponential backoff (e.g., max 60s delay). This ensures SnapDog2 persistently tries to reconnect to the essential Snapcast server. The operation key passed is "SnapcastConnect".
* **Operations (`SetClientVolumeAsync`, `AssignClientToGroupAsync`, etc.):** Uses `GetGeneralRetryPolicy` configured for a **limited number of retries** (e.g., `retryCount = 2`) with a shorter backoff period. This prevents hanging indefinitely on simple command failures but handles brief network glitches. The operation key passed reflects the operation name (e.g., "SnapcastSetVolume").
* **Timeout:** A short `GetTimeoutPolicy` (e.g., 5-10 seconds) wraps individual operations to prevent hangs.

### 6.3.2. MQTT Service Resilience (`MqttService`)

* **Connection (`ConnectAsync`):** Uses **Polly `GetGeneralRetryPolicy`** for the *initial* connection attempt (e.g., `retryCount = 5`, `maxDelaySeconds = 30`, key "MqttInitialConnect") to handle cases where the broker isn't ready immediately at startup.
* **Reconnection:** Relies primarily on **MQTTnet v5's built-in auto-reconnect mechanism**, configured via `MqttClientOptionsBuilder` (`WithAutoReconnectDelay`). Polly is *not* used for automatic reconnections triggered by the `DisconnectedAsync` event.
* **Publishing (`PublishAsync`):** Optionally wraps the `_mqttClient.PublishAsync` call with a very short, limited retry policy (`GetGeneralRetryPolicy` with `retryCount = 1` or `2`, short delay, key "MqttPublish") if transient publish errors under load are observed, though often not strictly necessary with QoS 1+.

### 6.3.3. KNX Service Resilience (`KnxService`)

* **Connection (`ConnectInternalAsync` within `InitializeAsync`):** Uses `GetGeneralRetryPolicy` configured for **indefinite retries** (`retryCount = -1`) with capped exponential backoff (e.g., max 30s delay), using operation key "KnxConnect". This handles both direct connection attempts and discovery retries.
* **Operations (`WriteToKnxAsync` called by `SendStatusAsync`):** Uses `GetGeneralRetryPolicy` configured with values from `KnxOptions` (`RetryCount`, `RetryInterval`) using operation key "KnxOperation".
* **Timeout:** A suitable `GetTimeoutPolicy` (e.g., matching `RetryInterval * RetryCount` + buffer) wraps individual KNX write/read operations.

### 6.3.4. HTTP Client Resilience (Subsonic, etc.)

Configured via `HttpClientFactory` extensions (`/Worker/DI/ResilienceExtensions.cs`). A standard pipeline is applied:

1. `GetTimeoutPolicy` (Overall request timeout, e.g., 15-30 seconds).
2. `GetHttpRetryPolicy` (Handles transient errors, e.g., 3 retries).
3. `GetHttpCircuitBreakerPolicy` (Prevents hammering failing service, e.g., 5 failures open for 30s).

```csharp
// In /Worker/DI/ResilienceExtensions.cs
namespace SnapDog2.Worker.DI;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout; // Required for TimeoutRejectedException
using SnapDog2.Core.Abstractions; // For ISubsonicService
using SnapDog2.Core.Configuration; // For SubsonicOptions
using SnapDog2.Infrastructure.Resilience; // For ResiliencePolicies
using SnapDog2.Infrastructure.Subsonic; // For SubsonicService implementation
using System;
using System.Net.Http;

public static class ResilienceExtensions
{
    public static IServiceCollection AddSnapDogResilience(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure IOptions<SubsonicOptions> if not done elsewhere
        services.Configure<SubsonicOptions>(configuration.GetSection("Subsonic"));

        // Resolve options to check if Subsonic is enabled
        var subsonicOptions = services.BuildServiceProvider().GetRequiredService<IOptions<SubsonicOptions>>().Value;
        var defaultTimeout = TimeSpan.FromSeconds(15); // Example default overall timeout

        // Register typed HttpClient for SubsonicService with resilience pipeline
        if (subsonicOptions.Enabled)
        {
             services.AddHttpClient<ISubsonicService, SubsonicService>(client =>
             {
                 client.BaseAddress = new Uri(subsonicOptions.Server);
                 client.Timeout = TimeSpan.FromMilliseconds(subsonicOptions.Timeout); // Per-attempt timeout if needed, else use Polly
                 client.DefaultRequestHeaders.Add("User-Agent", "SnapDog2/1.0");
             })
             .SetHandlerLifetime(TimeSpan.FromMinutes(5)) // Rotate HttpMessageHandler periodically
             .AddPolicyHandler((sp, req) => ResiliencePolicies.GetHttpRetryPolicy(sp.GetRequiredService<ILogger<SubsonicService>>()))
             .AddPolicyHandler((sp, req) => ResiliencePolicies.GetHttpCircuitBreakerPolicy(sp.GetRequiredService<ILogger<SubsonicService>>()))
             // Add overall timeout policy *outside* retry/circuit breaker
             .AddPolicyHandler(ResiliencePolicies.GetTimeoutPolicy(defaultTimeout));
        }

         // Configure a general-purpose named HttpClient if needed
         services.AddHttpClient("default")
             .SetHandlerLifetime(TimeSpan.FromMinutes(5))
             .AddPolicyHandler((sp, req) => ResiliencePolicies.GetHttpRetryPolicy(sp.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultHttpClient")))
             .AddPolicyHandler((sp, req) => ResiliencePolicies.GetHttpCircuitBreakerPolicy(sp.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultHttpClient")))
             .AddPolicyHandler(ResiliencePolicies.GetTimeoutPolicy(defaultTimeout));

        return services;
    }
}
```

## 6.4. Resilience Registration in DI Container

Resilience policies are primarily configured during DI setup using `HttpClientFactory` extensions or defined within services that utilize them directly (injecting `ILogger` for the policy's use).

By implementing these patterns, SnapDog2 gains robustness against common transient issues, leading to improved stability and user experience. Failures that persist after resilience attempts are converted to `Result.Failure`, allowing higher-level logic to handle them gracefully.
