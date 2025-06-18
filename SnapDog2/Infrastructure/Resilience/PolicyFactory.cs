using Microsoft.Extensions.Logging;
using Polly;

namespace SnapDog2.Infrastructure.Resilience;

/// <summary>
/// Static factory class for creating Polly resilience policies with standardized configurations.
/// Provides pre-configured policies for retry, circuit breaker, timeout, and combined patterns
/// commonly used in external service integrations.
/// </summary>
public static class PolicyFactory
{
    /// <summary>
    /// Creates a simple retry policy for testing the API.
    /// </summary>
    /// <param name="maxRetryAttempts">Maximum number of retry attempts (default: 3)</param>
    /// <param name="logger">Optional logger for retry events</param>
    /// <returns>Configured retry policy</returns>
    public static IAsyncPolicy CreateRetryPolicy(int maxRetryAttempts = 3, ILogger? logger = null)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: maxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger?.LogWarning(
                        "Retry attempt {RetryCount} of {MaxAttempts} after {Delay}ms. Exception: {Exception}",
                        retryCount,
                        maxRetryAttempts,
                        timespan.TotalMilliseconds,
                        outcome.Message
                    );
                }
            );
    }

    /// <summary>
    /// Creates a circuit breaker policy that opens after consecutive failures.
    /// </summary>
    /// <param name="failureThreshold">Number of consecutive failures before opening circuit (default: 3)</param>
    /// <param name="breakDuration">Duration to keep circuit open (default: 30 seconds)</param>
    /// <param name="logger">Optional logger for circuit breaker events</param>
    /// <returns>Configured circuit breaker policy</returns>
    public static IAsyncPolicy CreateCircuitBreakerPolicy(
        int failureThreshold = 3,
        TimeSpan? breakDuration = null,
        ILogger? logger = null
    )
    {
        var duration = breakDuration ?? TimeSpan.FromSeconds(30);

        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: failureThreshold,
                durationOfBreak: duration,
                onBreak: (exception, timespan) =>
                {
                    logger?.LogError(
                        "Circuit breaker opened after {FailureCount} consecutive failures. Break duration: {BreakDuration}s. Exception: {Exception}",
                        failureThreshold,
                        timespan.TotalSeconds,
                        exception.Message
                    );
                },
                onReset: () =>
                {
                    logger?.LogInformation("Circuit breaker closed - service is healthy again");
                },
                onHalfOpen: () =>
                {
                    logger?.LogInformation("Circuit breaker half-opened - testing service availability");
                }
            );
    }

    /// <summary>
    /// Creates a timeout policy for operation duration limits.
    /// </summary>
    /// <param name="timeout">Maximum operation duration (default: 30 seconds)</param>
    /// <param name="logger">Optional logger for timeout events</param>
    /// <returns>Configured timeout policy</returns>
    public static IAsyncPolicy CreateTimeoutPolicy(TimeSpan? timeout = null, ILogger? logger = null)
    {
        var timeoutDuration = timeout ?? TimeSpan.FromSeconds(30);

        return Policy.TimeoutAsync(timeoutDuration);
    }

    /// <summary>
    /// Creates a combined policy with timeout, circuit breaker, and retry patterns.
    /// The order is: Timeout → Circuit Breaker → Retry, which ensures proper failure handling.
    /// </summary>
    /// <param name="maxRetryAttempts">Maximum number of retry attempts (default: 3)</param>
    /// <param name="circuitBreakerThreshold">Circuit breaker failure threshold (default: 3)</param>
    /// <param name="circuitBreakerDuration">Circuit breaker break duration (default: 30 seconds)</param>
    /// <param name="timeout">Operation timeout duration (default: 30 seconds)</param>
    /// <param name="logger">Optional logger for policy events</param>
    /// <returns>Configured combined policy</returns>
    public static IAsyncPolicy CreateCombinedPolicy(
        int maxRetryAttempts = 3,
        int circuitBreakerThreshold = 3,
        TimeSpan? circuitBreakerDuration = null,
        TimeSpan? timeout = null,
        ILogger? logger = null
    )
    {
        var breakDuration = circuitBreakerDuration ?? TimeSpan.FromSeconds(30);
        var timeoutDuration = timeout ?? TimeSpan.FromSeconds(30);

        var retryPolicy = CreateRetryPolicy(maxRetryAttempts, logger);
        var circuitBreakerPolicy = CreateCircuitBreakerPolicy(circuitBreakerThreshold, breakDuration, logger);
        var timeoutPolicy = CreateTimeoutPolicy(timeoutDuration, logger);

        // Combine policies: Timeout wraps CircuitBreaker wraps Retry
        return Policy.WrapAsync(timeoutPolicy, circuitBreakerPolicy, retryPolicy);
    }

    /// <summary>
    /// Creates a policy from configuration parameters.
    /// </summary>
    /// <param name="retryAttempts">Number of retry attempts</param>
    /// <param name="circuitBreakerThreshold">Circuit breaker failure threshold</param>
    /// <param name="circuitBreakerDuration">Circuit breaker break duration</param>
    /// <param name="defaultTimeout">Default operation timeout</param>
    /// <param name="logger">Optional logger for events</param>
    /// <returns>Configured policy based on provided parameters</returns>
    public static IAsyncPolicy CreateFromConfiguration(
        int retryAttempts,
        int circuitBreakerThreshold,
        TimeSpan circuitBreakerDuration,
        TimeSpan defaultTimeout,
        ILogger? logger = null
    )
    {
        return CreateCombinedPolicy(
            maxRetryAttempts: retryAttempts,
            circuitBreakerThreshold: circuitBreakerThreshold,
            circuitBreakerDuration: circuitBreakerDuration,
            timeout: defaultTimeout,
            logger: logger
        );
    }
}
