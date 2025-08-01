namespace SnapDog2.Core.Configuration;

/// <summary>
/// Resilience policies configuration for external service calls.
/// </summary>
public class ResilienceConfiguration
{
    /// <summary>
    /// Gets or sets the number of retry attempts for failed operations.
    /// Maps to: SNAPDOG_SERVICES_RESILIENCE_RETRYATTEMPTS
    /// </summary>
    [Env(Key = "RETRYATTEMPTS", Default = 3)]
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the circuit breaker open duration in seconds.
    /// Maps to: SNAPDOG_SERVICES_RESILIENCE_CIRCUITBREAKERDURATION
    /// </summary>
    [Env(Key = "CIRCUITBREAKERDURATION", Default = 30)]
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold.
    /// Maps to: SNAPDOG_SERVICES_RESILIENCE_CIRCUITBREAKERTHRESHOLD
    /// </summary>
    [Env(Key = "CIRCUITBREAKERTHRESHOLD", Default = 3)]
    public int CircuitBreakerThreshold { get; set; } = 3;

    /// <summary>
    /// Gets or sets the default timeout for operations in seconds.
    /// Maps to: SNAPDOG_SERVICES_RESILIENCE_DEFAULTTIMEOUT
    /// </summary>
    [Env(Key = "DEFAULTTIMEOUT", Default = 30)]
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets the circuit breaker duration as TimeSpan.
    /// </summary>
    public TimeSpan CircuitBreakerDuration => TimeSpan.FromSeconds(CircuitBreakerDurationSeconds);

    /// <summary>
    /// Gets the default timeout as TimeSpan.
    /// </summary>
    public TimeSpan DefaultTimeout => TimeSpan.FromSeconds(DefaultTimeoutSeconds);
}
