# 18. Cortex.Mediator Implementation (Server Layer)

This chapter details the implementation of the Mediator pattern within SnapDog2's `/Server` layer, utilizing the **Cortex.Mediator** library. This pattern is central to the application's architecture, enabling a clean separation of concerns, facilitating the Command Query Responsibility Segregation (CQRS) pattern, reducing coupling between components, and providing a robust mechanism for handling cross-cutting concerns via pipeline behaviors.

## 18.1. Cortex.Mediator Integration and Configuration

Cortex.Mediator is integrated into the application's Dependency Injection (DI) container during startup. This involves registering the Cortex.Mediator services, discovering and registering all command, query, and notification handlers, and configuring the pipeline behaviors in the desired order of execution.

This registration typically occurs within a dedicated DI extension method in the `/Worker/DI` folder, called from `Program.cs`.

```csharp
// Example: /Worker/DI/CortexMediatorConfiguration.cs
namespace SnapDog2.Extensions.DependencyInjection;

using System.Reflection;
using Cortex.Mediator; // Cortex.Mediator namespace
using FluentValidation; // Required for AddValidatorsFromAssembly
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Server.Behaviors; // Location of pipeline behavior implementations

/// <summary>
/// Extension methods for configuring Cortex.Mediator services.
/// </summary>
public static class CortexMediatorConfiguration
{
    /// <summary>
    /// Adds Cortex.Mediator and related services (handlers, validators, behaviors) to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandProcessing(this IServiceCollection services)
    {
        // Determine the assembly containing the Cortex.Mediator handlers, validators, etc.
        // Assumes these primarily reside in the assembly where Server layer code exists.
        var serverAssembly = typeof(SnapDog2.Server.Behaviors.LoggingBehavior<,>).Assembly; // Get reference to Server assembly

        services.AddCortexMediator(cfg =>
        {
            // Automatically register all ICommandHandler<,>, IQueryHandler<,>, INotificationHandler<>
            // implementations found in the specified assembly.
            cfg.RegisterServicesFromAssembly(serverAssembly);

            // Register pipeline behaviors. Order is important and defines execution sequence.
            // Example Order: Logging -> Performance -> Validation -> Handler
            // Logging starts first, Performance wraps Validation+Handler, Validation runs before Handler.
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            // Add other custom behaviors (e.g., CachingBehavior, TransactionBehavior) here if needed.
        });

        // Automatically register all FluentValidation AbstractValidator<> implementations
        // found in the specified assembly. These are used by the ValidationBehavior.
        services.AddValidatorsFromAssembly(serverAssembly, ServiceLifetime.Transient); // Register validators

        return services;
    }
}
```

## 18.2. Command, Query, and Notification Structure

SnapDog2 strictly follows the CQRS pattern facilitated by Cortex.Mediator:

* **Commands:** Represent requests to change the system's state. They should be named imperatively (e.g., `SetVolumeCommand`, `AssignClientToZoneCommand`). They implement `ICommand<Result>` or `IQuery<Result<T>>` if they need to return data on success. They do not return queryable data directly.
* **Queries:** Represent requests to retrieve data without modifying state. They should be named descriptively based on the data requested (e.g., `GetZoneStateQuery`, `GetAllClientsQuery`). They implement `IQuery<Result<TResponse>>` where `TResponse` is the type of data being returned (typically a Core Model or a dedicated read model/DTO).
* **Notifications:** Represent events that have already occurred within the system (e.g., `PlaybackStateChangedNotification`, `SnapcastClientConnectedNotification`). They implement `INotification`. They are published using `IMediator.Publish()` and can have multiple independent handlers (`INotificationHandler<TNotification>`) that react to the event.

All Cortex.Mediator message types (Commands, Queries, Notifications) are typically defined as immutable `record` types within the `/Server` layer, often organized by feature or domain area (e.g., `/Server/Features/Zones/Commands`, `/Server/Features/Clients/Queries`, `/Server/Notifications`).

### 18.2.1. Command Example

```csharp
// Defined in /Server/Features/Zones/Commands/SetZoneVolumeCommand.cs
namespace SnapDog2.Server.Features.Zones.Commands;

using Cortex.Mediator;
using SnapDog2.Core.Models; // For Result
using SnapDog2.Core.Enums; // For CommandSource if defined in Core

/// <summary>
/// Command to set the volume for a specific zone.
/// </summary>
public record SetZoneVolumeCommand : ICommand<Result> // Returns non-generic Result (success/failure)
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the desired volume level (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal; // Default source
}

// Defined in /Server/Features/Zones/Commands/SetZoneVolumeCommandHandler.cs
namespace SnapDog2.Server.Features.Zones.Commands;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions; // For IZoneManager
using SnapDog2.Core.Models;

/// <summary>
/// Handles the SetZoneVolumeCommand.
/// </summary>
public partial class SetZoneVolumeCommandHandler : ICommandHandler<SetZoneVolumeCommand, Result> // Partial for logging
{
    private readonly IZoneManager _zoneManager; // Inject Core Abstraction
    private readonly ILogger<SetZoneVolumeCommandHandler> _logger;

    // Logger Messages
    [LoggerMessage(101, LogLevel.Information, "Handling SetZoneVolumeCommand for Zone {ZoneIndex} to {Volume} from {Source}")]
    private partial void LogHandling(int zoneIndex, int volume, CommandSource source);

    [LoggerMessage(102, LogLevel.Warning, "Zone {ZoneIndex} not found for SetZoneVolumeCommand.")]
    private partial void LogZoneNotFound(int zoneIndex);

    /// <summary>
    /// Initializes a new instance of the <see cref="SetZoneVolumeCommandHandler"/> class.
    /// </summary>
    public SetZoneVolumeCommandHandler(IZoneManager zoneManager, ILogger<SetZoneVolumeCommandHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(SetZoneVolumeCommand request, CancellationToken cancellationToken)
    {
        LogHandling(request.ZoneIndex, request.Volume, request.Source);

        // 1. Retrieve the target domain entity/service via Manager/Repository
        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            LogZoneNotFound(request.ZoneIndex);
            return zoneResult; // Propagate failure Result
        }
        var zone = zoneResult.Value;

        // 2. Delegate the action to the domain entity/service
        // The ZoneService itself handles state update logic, external calls, and notification publishing
        var setResult = await zone.SetVolumeAsync(request.Volume).ConfigureAwait(false);

        // 3. Return the Result from the domain operation
        return setResult;
    }
}
```

### 18.2.2. Query Example

```csharp
// Defined in /Server/Features/Clients/Queries/GetAllClientsQuery.cs
namespace SnapDog2.Server.Features.Clients.Queries;

using System.Collections.Generic;
using Cortex.Mediator;
using SnapDog2.Core.Models; // For Result<T> and ClientState

/// <summary>
/// Query to retrieve the state of all known clients.
/// </summary>
public record GetAllClientsQuery : IQuery<Result<List<ClientState>>>; // Response is list of Core models

// Defined in /Server/Features/Clients/Queries/GetAllClientsQueryHandler.cs
namespace SnapDog2.Server.Features.Clients.Queries;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper; // Optional: For mapping
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions; // For ISnapcastStateRepository, IClientManager
using SnapDog2.Core.Models;      // For ClientState
using Sturd.SnapcastNet.Models; // For raw Snapcast Client model

/// <summary>
/// Handles the GetAllClientsQuery.
/// </summary>
public partial class GetAllClientsQueryHandler : IQueryHandler<GetAllClientsQuery, Result<List<ClientState>>>
{
    private readonly ISnapcastStateRepository _snapcastStateRepo;
    private readonly IClientManager _clientManager; // For mapping internal ID/Zone Name
    private readonly IMapper _mapper; // Example if using AutoMapper
    private readonly ILogger<GetAllClientsQueryHandler> _logger;

    // Logger Messages
    [LoggerMessage(201, LogLevel.Information,"Handling GetAllClientsQuery.")] partial void LogHandling();
    [LoggerMessage(202, LogLevel.Error,"Error retrieving client states from repository.")] partial void LogError(Exception ex);
    [LoggerMessage(203, LogLevel.Warning,"Mapping info not found in ClientManager for Snapcast ID {SnapcastId}.")] partial void LogMappingInfoNotFound(string snapcastId);


    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllClientsQueryHandler"/> class.
    /// </summary>
    public GetAllClientsQueryHandler(
        ISnapcastStateRepository snapcastStateRepo,
        IClientManager clientManager,
        IMapper mapper,
        ILogger<GetAllClientsQueryHandler> logger)
    {
        _snapcastStateRepo = snapcastStateRepo;
        _clientManager = clientManager;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<Result<List<ClientState>>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken)
    {
        LogHandling();
        try
        {
            // 1. Get raw data from the state repository (Infrastructure)
            var snapClients = _snapcastStateRepo.GetAllClients();

            // 2. Map raw data to Core domain models, potentially enriching with data from Managers
            var clientStates = snapClients
                .Select(snapClient => MapSnapClientToClientState(snapClient)) // Use helper/mapper
                .ToList();

            // 3. Return success Result with mapped data
            return Task.FromResult(Result<List<ClientState>>.Success(clientStates));
        }
        catch (Exception ex)
        {
             LogError(ex);
             // Convert exception to Failure Result
             return Task.FromResult(Result<List<ClientState>>.Failure(ex));
        }
    }

    // Example Mapping Logic (may use AutoMapper profiles in practice)
    private ClientState MapSnapClientToClientState(Client snapClient)
    {
        var snapDogInfo = _clientManager.GetClientInfoBySnapcastId(snapClient.Id);
        if(snapDogInfo == null) LogMappingInfoNotFound(snapClient.Id);

        // Use AutoMapper or manual mapping
        return _mapper.Map<ClientState>(snapClient, opts => {
             opts.Items["InternalId"] = snapDogInfo?.InternalId ?? -1;
             opts.Items["SnapDogName"] = snapDogInfo?.ConfiguredName;
             opts.Items["CurrentZoneIndex"] = snapDogInfo?.CurrentZoneIndex;
        }); // Example using AutoMapper context items
    }
}
```

### 18.2.3. Notification Example

```csharp
// Defined in /Server/Notifications/StatusChangedNotification.cs
namespace SnapDog2.Server.Notifications;

using System;
using Cortex.Mediator;

/// <summary>
/// Notification published when a tracked status changes within the system.
/// </summary>
/// <param name="StatusType">Identifier for the type of status that changed (e.g., "VOLUME_STATUS", "PLAYBACK_STATE"). Matches Command Framework Status IDs.</param>
/// <param name="TargetId">Identifier for the entity whose status changed (e.g., "zone_1", "client_5", "system").</param>
/// <param name="Value">The new value of the status.</param>
public record StatusChangedNotification(
    string StatusType,
    string TargetId,
    object Value
) : INotification
{
    /// <summary>
    /// Gets the UTC timestamp when the notification was created.
    /// </summary>
    public DateTime TimestampUtc { get; } = DateTime.UtcNow;
}

// Handler Example (e.g., in /Infrastructure/Mqtt/MqttStatusNotifier.cs)
namespace SnapDog2.Infrastructure.Mqtt; // Example location for handler

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions; // For IMqttService
using SnapDog2.Server.Notifications; // Reference notification definition

/// <summary>
/// Handles StatusChangedNotification to publish updates via MQTT.
/// </summary>
public partial class MqttStatusNotifier : INotificationHandler<StatusChangedNotification> // Partial for logging
{
    private readonly IMqttService _mqttService;
    private readonly ILogger<MqttStatusNotifier> _logger;

    // Logger Messages
    [LoggerMessage(501, LogLevel.Debug,"Handling StatusChangedNotification for {TargetId} ({StatusType}). Publishing to MQTT.")]
    private partial void LogHandlingNotification(string targetId, string statusType);
    [LoggerMessage(502, LogLevel.Error,"Error publishing MQTT status for {TargetId} ({StatusType}).")]
    private partial void LogPublishError(string targetId, string statusType, Exception ex);

    public MqttStatusNotifier(IMqttService mqttService, ILogger<MqttStatusNotifier> logger)
    {
        _mqttService = mqttService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Handle(StatusChangedNotification notification, CancellationToken cancellationToken)
    {
        LogHandlingNotification(notification.TargetId, notification.StatusType);
        try
        {
            // Determine topic and payload based on notification content
            // This requires mapping logic from the notification details to the MQTT structure defined in Section 9
            string topic = MapNotificationToMqttTopic(notification); // Implement helper
            string payload = SerializeMqttPayload(notification.Value); // Implement helper
            bool retain = ShouldRetainMqttMessage(notification.StatusType); // Implement helper

            if (!string.IsNullOrEmpty(topic))
            {
                // Publish using the infrastructure service abstraction
                var result = await _mqttService.PublishAsync(topic, payload, retain).ConfigureAwait(false);
                if(result.IsFailure) {
                    // Log failure from MQTT service publish attempt
                     LogWarning("MQTT Publish failed for {Topic}: {Error}", topic, result.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            LogPublishError(notification.TargetId, notification.StatusType, ex);
        }
    }

    // --- Helper methods for mapping/serialization ---
    private string MapNotificationToMqttTopic(StatusChangedNotification notification) {/* ... logic based on Sec 9 ... */ return string.Empty;}
    private string SerializeMqttPayload(object value) {/* ... logic (e.g., Json, simple string) ... */ return value?.ToString() ?? string.Empty;}
    private bool ShouldRetainMqttMessage(string statusType) {/* ... logic based on Sec 9 ... */ return true; } // Default to retain status

     // Example Warning Log Method (if needed)
    [LoggerMessage(503, LogLevel.Warning, "{WarningMessage}")]
    private partial void LogWarning(string warningMessage, params object[] args);
}
```

## 18.3. Command Validation with FluentValidation

Validation logic for commands is encapsulated in `AbstractValidator<TCommand>` classes, typically located alongside the command definition (`/Server/Features/.../Commands/XyzCommandValidator.cs`). These validators are automatically executed by the `ValidationBehavior` pipeline stage.

```csharp
namespace SnapDog2.Server.Features.Zones.Commands.Validators; // Example location

using FluentValidation;
using SnapDog2.Core.Models; // Might need enums or constants

/// <summary>
/// Validator for the SetZoneVolumeCommand.
/// </summary>
public class SetZoneVolumeCommandValidator : AbstractValidator<SetZoneVolumeCommand>
{
    public SetZoneVolumeCommandValidator()
    {
        RuleFor(x => x.ZoneIndex)
            .GreaterThan(0).WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => x.Volume)
            .InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100.");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Invalid command source specified.");
    }
}
```

## 18.4. Pipeline Behaviors (`/Server/Behaviors`)

Implement `IPipelineBehavior<TRequest, TResponse>` to add cross-cutting concerns executed around command/query handlers.

### 18.4.1. Logging Behavior

Logs request handling details, duration, and success/failure status using the `IResult` interface. Creates OpenTelemetry Activities for tracing.

```csharp
// Located in /Server/Behaviors/LoggingBehavior.cs
namespace SnapDog2.Server.Behaviors;
// ... usings (Cortex.Mediator, Logging, Diagnostics, Core Models) ...

public partial class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage<TResponse>
    where TResponse : IResult // Constrain to ensure response has IsSuccess etc.
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private static readonly ActivitySource ActivitySource = new("SnapDog2.CortexMediator");

    // ... LoggerMessage definitions (LogHandling, LogSuccess, LogFailure, LogException) ...

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestType = requestName.Contains("Query") ? "Query" : "Command";
        var stopwatch = Stopwatch.StartNew();

        using var activity = ActivitySource.StartActivity($"{requestType}:{requestName}", ActivityKind.Internal);
        // ... Set activity tags ...

        LogHandling(requestType, requestName); // Use generated logger
        TResponse response;
        try {
             response = await next().ConfigureAwait(false); // Call next item in pipeline
             stopwatch.Stop();
             // Log outcome based on IResult
             if (response.IsSuccess) { LogSuccess(requestType, requestName, stopwatch.ElapsedMilliseconds); activity?.SetStatus(ActivityStatusCode.Ok); }
             else { LogFailure(requestType, requestName, stopwatch.ElapsedMilliseconds, response.ErrorMessage); /* Set Activity Error Status */ }
        } catch (Exception ex) {
             stopwatch.Stop(); LogException(ex, requestType, requestName, stopwatch.ElapsedMilliseconds); /* Set Activity Error Status */ throw;
        }
        return response;
    }
    // Logger definitions here...
    [LoggerMessage(/*...*/)] private partial void LogHandling(string requestType, string requestName);
    [LoggerMessage(/*...*/)] private partial void LogSuccess(string requestType, string requestName, long elapsedMilliseconds);
    [LoggerMessage(/*...*/)] private partial void LogFailure(string requestType, string requestName, long elapsedMilliseconds, string errorMessage);
    [LoggerMessage(/*...*/)] private partial void LogException(Exception ex, string requestType, string requestName, long elapsedMilliseconds);
}
```

### 18.4.2. Validation Behavior

Executes registered FluentValidation validators for the incoming `TRequest`. **Throws `ValidationException`** if validation fails, which is expected to be caught by global exception handling middleware (e.g., in the API layer) to return appropriate error responses (e.g., 400 Bad Request or 422 Unprocessable Entity).

```csharp
// Located in /Server/Behaviors/ValidationBehavior.cs
namespace SnapDog2.Server.Behaviors;
// ... usings (Cortex.Mediator, Logging, Diagnostics, FluentValidation) ...

public partial class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage<TResponse>
    // No IResult constraint here, validation happens before handler execution
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;
    private static readonly ActivitySource ActivitySource = new("SnapDog2.Validation");

    // Logger Messages
    [LoggerMessage(2001, LogLevel.Warning, "Validation failed for {RequestName}. Errors: {ValidationErrors}")]
    private partial void LogValidationFailure(string requestName, string validationErrors);
    [LoggerMessage(2002, LogLevel.Debug, "No validators found for {RequestName}. Skipping validation.")]
    private partial void LogNoValidators(string requestName);
    [LoggerMessage(2003, LogLevel.Debug, "Validation passed for {RequestName}.")]
    private partial void LogValidationPassed(string requestName);

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
         using var activity = ActivitySource.StartActivity($"Validate:{requestName}", ActivityKind.Internal);

        if (!_validators.Any())
        {
            LogNoValidators(requestName);
            activity?.SetTag("validation.skipped", true);
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            var errorMessages = string.Join("; ", failures.Select(f => $"'{f.PropertyName}': {f.ErrorMessage}"));
            LogValidationFailure(requestName, errorMessages);
            activity?.SetStatus(ActivityStatusCode.Error, "Validation Failed");
            activity?.SetTag("validation.errors", errorMessages);
            // Throw exception - expect middleware to handle this
            throw new ValidationException(failures);
        }

        LogValidationPassed(requestName);
         activity?.SetStatus(ActivityStatusCode.Ok);
        return await next().ConfigureAwait(false);
    }
}
```

### 18.4.3. Performance Behavior

Measures execution time of the subsequent pipeline stages (Validation + Handler). Reports metrics via `IMetricsService` and logs warnings for slow operations.

```csharp
// Located in /Server/Behaviors/PerformanceBehavior.cs
namespace SnapDog2.Server.Behaviors;
// ... usings (Cortex.Mediator, Logging, Diagnostics, Core Abstractions/Models) ...
using System.Diagnostics;

public partial class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly IMetricsService _metricsService; // Core Abstraction for metrics

    // Logger Messages
    [LoggerMessage(3001, LogLevel.Warning, "{RequestType} {RequestName} processing took {ElapsedMilliseconds}ms (Threshold: {ThresholdMs}ms)")]
    private partial void LogLongRunningRequest(string requestType, string requestName, long elapsedMilliseconds, int thresholdMs);

    // Configurable threshold
    private const int LongRunningThresholdMilliseconds = 500; // TODO: Make configurable

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, IMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var response = await next().ConfigureAwait(false);
            success = !(response is IResult r && r.IsFailure);
            return response;
        }
        catch
        {
            success = false; // Exception means failure for metric recording
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            var requestName = typeof(TRequest).Name;
            var requestType = requestName.Contains("Query") ? "Query" : "Command";

            // Record metric (Implementation in Infrastructure.Observability)
            _metricsService.RecordCortexMediatorRequestDuration(requestType, requestName, elapsedMilliseconds, success);

            if (elapsedMilliseconds > LongRunningThresholdMilliseconds)
            {
                LogLongRunningRequest(requestType, requestName, elapsedMilliseconds, LongRunningThresholdMilliseconds);
            }
        }
    }
}
```

## 18.5. Communication Layer Integration

Adapters (API Controllers, MQTT Service, KNX Service) convert external inputs into Cortex.Mediator `ICommand` and `IQuery` objects and dispatch them using `IMediator.Send()`.

## 18.6. Status Update Mechanism

Core Managers (`/Server/Managers`) or Services (`/Server/Features`) publish `INotification` objects via `IMediator.Publish()` after successfully changing state. Infrastructure Handlers (`/Infrastructure/*` or `/Server/*`) subscribe to these notifications to push updates externally (MQTT, KNX, SignalR etc.)
