# Cross-Cutting Concerns

This chapter details essential concepts and patterns that permeate multiple layers and components of the SnapDog2 application. These cross-cutting concerns ensure consistency, reliability, and maintainability throughout the codebase. They include the standardized approach to error handling, the logging strategy, integration with observability tools, and input validation mechanisms.

## 5.1 Error Handling Strategy

SnapDog2 employs a strict and consistent error handling strategy centered around the **Result Pattern**. This pattern is **mandatory** for all operations within the `/Server` and `/Infrastructure` layers that can encounter predictable failures (e.g., business rule violations, external service unavailability after retries, resource not found).

**Core Principles:**

1. **Avoid Exceptions for Control Flow:** Exceptions are reserved *only* for truly unexpected, unrecoverable, or programmer errors (e.g., `ArgumentNullException`, `NullReferenceException` indicating bugs, `OutOfMemoryException`, critical startup failures). They must **never** be used to signal expected operational failures like "Zone Not Found" or "Snapcast Connection Unavailable".
2. **Result Pattern Encapsulation:** Operations that can fail gracefully must return either `SnapDog2.Core.Models.Result` (for void operations) or `SnapDog2.Core.Models.Result<T>` (for operations returning a value `T`). These objects encapsulate the success/failure status, an optional error message, and an optional originating `Exception` object (only populated if the failure was due to an *unexpected* exception caught at the infrastructure boundary).
3. **Boundary Exception Handling:** `try/catch` blocks should primarily exist within the `/Infrastructure` layer, specifically around calls to external libraries (`Sturd.SnapcastNet`, `Knx.Falcon.Sdk`, `MQTTnet`, `HttpClient`, etc.) or system resources that might throw exceptions. When an exception is caught here (and potentially *after* resilience policies like Polly have failed), it **must** be logged with context and immediately converted into a `Result.Failure(ex)` object before being returned to the calling layer (`/Server`).
4. **Propagation & Handling:** Callers (e.g., MediatR handlers in `/Server`, other services) **must** check the `IsSuccess` or `IsFailure` property of the returned `Result` object. If `IsFailure` is true, the caller should handle the failure appropriately (e.g., log a warning/error, return the failure `Result` further up the stack, potentially publish an `ErrorNotification`). The `Value` property of `Result<T>` should only be accessed if `IsSuccess` is true.
5. **API Layer Exception Handling:** The `/Api` layer translates `Result.Failure` outcomes received from MediatR handlers into appropriate HTTP error responses (e.g., 400 Bad Request, 404 Not Found, 500 Internal Server Error) using the standard `ApiResponse` structure (Section 11.4.1). Unhandled exceptions bubbling up to the API layer should be caught by global exception handling middleware, logged critically, and result in a generic 500 Internal Server Error response.

### 5.1.1 Result Pattern Implementation (Canonical Definition)

These records provide the standard way to represent operation outcomes throughout the application.

```csharp
// Defined in /Core/Models/Result.cs folder
namespace SnapDog2.Core.Models;

using System;
using System.Collections.Generic; // For EqualityComparer
using System.Diagnostics.CodeAnalysis; // For NotNullWhen

/// <summary>
/// Marker interface for Result types, useful for MediatR behaviors and constraints.
/// </summary>
public interface IResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    bool IsFailure { get; }

    /// <summary>
    /// Gets the error message if the operation failed. Returns null if successful.
    /// </summary>
    string? ErrorMessage { get; } // Nullable string

    /// <summary>
    /// Gets the exception that caused the operation to fail, if available. Returns null otherwise.
    /// </summary>
    Exception? Exception { get; } // Nullable exception
}

/// <summary>
/// Represents the outcome of an operation that does not return a value.
/// Use static factory methods Success() and Failure() to create instances.
/// </summary>
public class Result : IResult
{
    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure => !IsSuccess;

    /// <inheritdoc />
    public string? ErrorMessage { get; }

    /// <inheritdoc />
    public Exception? Exception { get; }

    /// <summary>
    /// Protected constructor to enforce usage of factory methods.
    /// </summary>
    protected internal Result(bool isSuccess, string? errorMessage, Exception? exception)
    {
        // Validate consistency: Success implies no error, Failure requires an error.
        if (isSuccess && (!string.IsNullOrEmpty(errorMessage) || exception != null))
            throw new InvalidOperationException("Assertion failed: A successful result cannot have an error message or exception.");
        if (!isSuccess && string.IsNullOrEmpty(errorMessage) && exception == null)
            throw new InvalidOperationException("Assertion failed: A failed result requires an error message or an exception.");

        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful Result instance.</returns>
    public static Result Success() => new(true, null, null);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed Result instance.</returns>
    public static Result Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new(false, errorMessage, null);
    }

    /// <summary>
    /// Creates a failed result from the specified exception.
    /// The exception message is used as the ErrorMessage.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed Result instance.</returns>
    public static Result Failure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new(false, exception.Message, exception);
    }

    // Optional: Implicit conversion for convenience, although explicit checks are often clearer.
    // public static implicit operator Result(Result<object> result) { ... }
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// Use static factory methods Success(T value) and Failure() to create instances.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
public class Result<T> : Result // Inherits properties from non-generic Result
{
    private readonly T? _value; // Store value, allow default for failure case

    /// <summary>
    /// Gets the value returned by the operation if successful.
    /// Accessing this property on a failed result will throw an InvalidOperationException.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed when IsFailure is true.</exception>
    [MaybeNull] // Can be null if T is a reference type or nullable value type
    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Cannot access the value of a failed result.");
            }
            return _value!; // Null-forgiving operator used intentionally after IsFailure check
        }
    }

    /// <summary>
    /// Protected constructor to enforce usage of factory methods.
    /// </summary>
    protected internal Result(bool isSuccess, T? value, string? errorMessage, Exception? exception)
        : base(isSuccess, errorMessage, exception)
    {
        _value = value; // Assign value even on failure path (will be default(T))
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value returned by the operation.</param>
    /// <returns>A successful Result instance containing the value.</returns>
    public static Result<T> Success(T value) => new(true, value, null, null);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// The Value property will return the default value for type T.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed Result instance.</returns>
    public new static Result<T> Failure(string errorMessage) // 'new' hides base method
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new(false, default, errorMessage, null);
    }

    /// <summary>
    /// Creates a failed result from the specified exception.
    /// The exception message is used as the ErrorMessage.
    /// The Value property will return the default value for type T.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed Result instance.</returns>
    public new static Result<T> Failure(Exception exception) // 'new' hides base method
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new(false, default, exception.Message, exception);
    }
}
```

### 5.1.2 Usage in Services

All service methods in `/Server` and `/Infrastructure` that perform operations which might fail operationally (network calls, business logic checks, finding resources) must return `Task<Result>` or `Task<Result<T>>`.

```csharp
// Example in IZoneManager (/Core/Abstractions)
public interface IZoneManager
{
    Task<Result<IZoneService>> GetZoneAsync(int zoneId); // Can fail if zone doesn't exist
    Task<Result> SomeZoneActionAsync(int zoneId, string parameter); // Can fail based on rules
    // ... other methods
}

// Example usage in a MediatR handler (/Server/Features/...)
public async Task<Result> Handle(SomeZoneCommand request, CancellationToken cancellationToken)
{
    var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
    if (zoneResult.IsFailure)
    {
        // Log failure reason (e.g., zone not found)
        _logger.LogWarning("Zone lookup failed for Zone {ZoneId}: {Error}", request.ZoneId, zoneResult.ErrorMessage);
        return zoneResult; // Propagate the failure Result
    }

    // Access Value only after checking IsSuccess (or IsFailure)
    var zoneService = zoneResult.Value;
    var actionResult = await zoneService.PerformActionAsync(request.Data).ConfigureAwait(false); // Returns Result

    if(actionResult.IsFailure) {
        _logger.LogError(actionResult.Exception, "Action failed for Zone {ZoneId}: {Error}", request.ZoneId, actionResult.ErrorMessage);
    } else {
         _logger.LogInformation("Action succeeded for Zone {ZoneId}", request.ZoneId);
    }

    return actionResult; // Return the result of the action
}
```

## 5.2 Logging Strategy

Logging uses the `Microsoft.Extensions.Logging.ILogger<T>` abstraction throughout the application. The concrete implementation is provided by **Serilog**, configured during application startup (`/Worker/Program.cs`).

* **Mandatory Pattern:** Use **LoggerMessage Source Generators** (Section 1.5) for all log messages to ensure optimal performance and compile-time checks.
* **Structured Logging:** Always log data using named placeholders in the message template (`_logger.LogInformation("Processing User {UserId} for Zone {ZoneId}", userId, zoneId);`). Avoid string interpolation directly in log messages (`$"..."`).
* **Log Levels:** Use appropriate `LogLevel` values (Trace, Debug, Information, Warning, Error, Critical) as defined in Section 1.5.1.3. Default minimum level configured via environment variables (Section 10).
* **Context Enrichment:** Logs are automatically enriched with Trace ID and Span ID via OpenTelemetry integration (Section 13) when using `ILogger`. Use logging scopes (`_logger.BeginScope(...)`) to add contextual information (like ZoneId, RequestId) to a series of related log messages.
* **Exception Logging:** When logging exceptions, **always** pass the `Exception` object as the first argument to the logging method (`_logger.LogError(ex, "Message template {Data}", data);`).

```csharp
// Example Serilog setup in /Worker/Program.cs
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    // Read configuration from appsettings.json / environment variables
    // .ReadFrom.Configuration(builder.Configuration) // Recommended approach
    // Manual configuration example:
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext() // Essential for scopes and Trace/Span ID
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] Trace={TraceId} Span={SpanId} {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.File(
        path: "logs/snapdog-.txt", // Configure path via settings
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] TraceId={TraceId} SpanId={SpanId} [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 31,
        fileSizeLimitBytes: 100 * 1024 * 1024
    )
    // Optionally add Seq sink:
    // .WriteTo.Seq(Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341")
    .CreateLogger();

// Register Serilog with the host
// builder.Host.UseSerilog();
```

## 5.3 Instrumentation and Metrics

Observability is achieved using **OpenTelemetry**.

* **Tracing:** Uses `.NET`'s built-in `System.Diagnostics.ActivitySource` for creating distributed traces. Instrumentation for ASP.NET Core and HttpClient is automatic. Manual activities (`_activitySource.StartActivity(...)`) are added for MediatR handlers and key infrastructure operations. Traces are exported via **OTLP** (Section 13).
* **Metrics:** Uses `.NET`'s built-in `System.Diagnostics.Metrics.Meter` for defining and recording application metrics. Instrumentation for ASP.NET Core, HttpClient, and Runtime is automatic. Custom metrics (request counts, durations, error rates) are defined and recorded via `IMetricsService`. Metrics are exposed via a **Prometheus scraping endpoint** (Section 13).

(See Section 13 for detailed OpenTelemetry setup and usage).

## 5.4 Validation

Input validation is primarily handled using the **FluentValidation** library.

* **Scope:** Validators are defined for MediatR Command objects (`IRequest<Result>`) and potentially for API Request DTOs.
* **Implementation:** Create validator classes inheriting from `AbstractValidator<TCommand>` in the `/Server/Features/.../Validators` folder structure. Define rules using FluentValidation's fluent API.
* **Execution:**
  * For MediatR commands, validation is executed automatically by the `ValidationBehavior` pipeline behavior (Section 6.4.2) registered in DI.
  * For API DTOs, validation can be integrated into the ASP.NET Core model binding pipeline (`builder.Services.AddFluentValidationAutoValidation();`).
* **Failure Handling:** If validation fails, the `ValidationBehavior` throws a `FluentValidation.ValidationException`. This exception should be caught by global exception handling middleware (in `/Api` or `/Worker`) and translated into a user-friendly error response (e.g., HTTP 400 Bad Request or 422 Unprocessable Entity with validation failure details) using the standard `ApiResponse` format.

```csharp
// Example Validator in /Server/Features/Zones/Commands/Validators/SetZoneVolumeCommandValidator.cs
namespace SnapDog2.Server.Features.Zones.Commands.Validators;

using FluentValidation;

/// <summary>
/// Validator for the SetZoneVolumeCommand.
/// </summary>
public class SetZoneVolumeCommandValidator : AbstractValidator<SetZoneVolumeCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetZoneVolumeCommandValidator"/> class.
    /// </summary>
    public SetZoneVolumeCommandValidator()
    {
        RuleFor(command => command.ZoneId)
            .GreaterThan(0)
            .WithMessage("Zone ID must be a positive integer.");

        RuleFor(command => command.Volume)
            .InclusiveBetween(0, 100)
            .WithMessage("Volume must be between 0 and 100 (inclusive).");
    }
}
```

## 5.5 Interplay

These cross-cutting concerns interact seamlessly:

* **Resilience (Sec 7) & Result (Sec 5.1):** Polly policies wrap calls in `/Infrastructure`. If all retries/attempts within a policy fail, the originating exception is caught and returned as `Result.Failure(ex)`.
* **Validation (Sec 5.4) & Result (Sec 5.1):** `ValidationBehavior` runs early in the MediatR pipeline. If validation fails, it throws `ValidationException`, preventing the command handler from executing. This exception does *not* typically result in a `Result.Failure` directly from the handler but is handled by higher-level middleware to produce an appropriate error response.
* **Logging (Sec 5.2) & Telemetry (Sec 13):** Logs are automatically enriched with `TraceId` and `SpanId` from the current OpenTelemetry `Activity`. Events like resilience retries, validation failures, command handling start/end, and `Result` failures are logged with structured context.
* **Metrics (Sec 13) & Handlers/Behaviors:** The `PerformanceBehavior` (Sec 6.4.3) uses `IMetricsService` to record durations and success/failure counts of MediatR requests. Other specific metrics can be recorded directly where relevant actions occur.
