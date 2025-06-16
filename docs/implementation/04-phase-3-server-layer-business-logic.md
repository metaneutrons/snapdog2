# Phase 3: Server Layer & Business Logic

## Overview

Phase 3 implements the complete server layer using MediatR CQRS patterns, business logic handlers, validation, and domain events. This phase transforms the domain foundation into a fully functional business layer.

**Deliverable**: Business logic application with complete MediatR CQRS implementation and domain event processing.

## Objectives

### Primary Goals

- [ ] Implement complete MediatR CQRS pattern with commands and queries
- [ ] Create all business logic handlers for audio streaming operations
- [ ] Integrate FluentValidation for comprehensive input validation
- [ ] Implement domain event publishing and handling system
- [ ] Create pipeline behaviors for cross-cutting concerns
- [ ] Establish business rule validation and enforcement

### Success Criteria

- All audio streaming operations available as commands/queries
- Business rules properly enforced in handlers
- Domain events published for all state changes
- Validation integrated into MediatR pipeline
- 95%+ test coverage for all business logic
- Console application demonstrates complete business workflows

## Implementation Steps

### Step 1: MediatR Commands Implementation

#### 1.1 Audio Stream Commands

```csharp
namespace SnapDog.Server.Features.AudioStreams.Commands;

/// <summary>
/// Command to create a new audio stream.
/// </summary>
/// <param name="Name">Stream name</param>
/// <param name="Codec">Audio codec to use</param>
/// <param name="SampleRate">Sample rate in Hz</param>
/// <param name="BitDepth">Bit depth for samples</param>
/// <param name="Channels">Number of audio channels</param>
public record CreateAudioStreamCommand(
    string Name,
    AudioCodec Codec,
    int SampleRate,
    int BitDepth,
    int Channels) : IRequest<Result<AudioStream>>;

/// <summary>
/// Command to start an audio stream.
/// </summary>
/// <param name="StreamId">ID of stream to start</param>
/// <param name="StartedBy">User or system starting the stream</param>
public record StartAudioStreamCommand(
    int StreamId,
    string StartedBy) : IRequest<Result>;

/// <summary>
/// Command to stop an audio stream.
/// </summary>
/// <param name="StreamId">ID of stream to stop</param>
/// <param name="StoppedBy">User or system stopping the stream</param>
public record StopAudioStreamCommand(
    int StreamId,
    string StoppedBy) : IRequest<Result>;

/// <summary>
/// Command to delete an audio stream.
/// </summary>
/// <param name="StreamId">ID of stream to delete</param>
/// <param name="DeletedBy">User or system deleting the stream</param>
public record DeleteAudioStreamCommand(
    int StreamId,
    string DeletedBy) : IRequest<Result>;
```

#### 1.2 Client Management Commands

```csharp
namespace SnapDog.Server.Features.Clients.Commands;

/// <summary>
/// Command to register a new client.
/// </summary>
/// <param name="Name">Client name</param>
/// <param name="MacAddress">Client MAC address</param>
/// <param name="IpAddress">Client IP address</param>
/// <param name="ZoneId">Initial zone assignment</param>
public record RegisterClientCommand(
    string Name,
    string MacAddress,
    string IpAddress,
    int? ZoneId) : IRequest<Result<Client>>;

/// <summary>
/// Command to set client volume.
/// </summary>
/// <param name="ClientId">Client ID</param>
/// <param name="Volume">Volume level (0-100)</param>
/// <param name="UpdatedBy">User or system making the change</param>
public record SetClientVolumeCommand(
    int ClientId,
    int Volume,
    string UpdatedBy) : IRequest<Result>;

/// <summary>
/// Command to set client mute status.
/// </summary>
/// <param name="ClientId">Client ID</param>
/// <param name="Muted">Mute status</param>
/// <param name="UpdatedBy">User or system making the change</param>
public record SetClientMuteCommand(
    int ClientId,
    bool Muted,
    string UpdatedBy) : IRequest<Result>;

/// <summary>
/// Command to move client to different zone.
/// </summary>
/// <param name="ClientId">Client ID</param>
/// <param name="ZoneId">Target zone ID</param>
/// <param name="MovedBy">User or system making the change</param>
public record MoveClientToZoneCommand(
    int ClientId,
    int ZoneId,
    string MovedBy) : IRequest<Result>;
```

### Step 2: MediatR Queries Implementation

#### 2.1 Audio Stream Queries

```csharp
namespace SnapDog.Server.Features.AudioStreams.Queries;

/// <summary>
/// Query to get all audio streams.
/// </summary>
public record GetAllAudioStreamsQuery() : IRequest<Result<IEnumerable<AudioStream>>>;

/// <summary>
/// Query to get audio stream by ID.
/// </summary>
/// <param name="StreamId">Stream ID to retrieve</param>
public record GetAudioStreamByIdQuery(int StreamId) : IRequest<Result<AudioStream>>;

/// <summary>
/// Query to get all active audio streams.
/// </summary>
public record GetActiveAudioStreamsQuery() : IRequest<Result<IEnumerable<AudioStream>>>;

/// <summary>
/// Query to get streams by codec type.
/// </summary>
/// <param name="Codec">Codec type to filter by</param>
public record GetStreamsByCodecQuery(AudioCodec Codec) : IRequest<Result<IEnumerable<AudioStream>>>;
```

#### 2.2 System Status Queries

```csharp
namespace SnapDog.Server.Features.System.Queries;

/// <summary>
/// Query to get complete system status.
/// </summary>
public record GetSystemStatusQuery() : IRequest<Result<SystemStatus>>;

/// <summary>
/// Query to get system health information.
/// </summary>
public record GetSystemHealthQuery() : IRequest<Result<SystemHealth>>;

/// <summary>
/// System status information.
/// </summary>
/// <param name="ActiveStreams">Number of active streams</param>
/// <param name="ConnectedClients">Number of connected clients</param>
/// <param name="TotalZones">Total number of zones</param>
/// <param name="SystemUptime">How long system has been running</param>
/// <param name="LastUpdated">When status was last updated</param>
public record SystemStatus(
    int ActiveStreams,
    int ConnectedClients,
    int TotalZones,
    TimeSpan SystemUptime,
    DateTime LastUpdated);

/// <summary>
/// System health information.
/// </summary>
/// <param name="OverallStatus">Overall system health status</param>
/// <param name="Components">Health status of individual components</param>
/// <param name="LastHealthCheck">When health was last checked</param>
public record SystemHealth(
    HealthStatus OverallStatus,
    Dictionary<string, ComponentHealth> Components,
    DateTime LastHealthCheck);
```

### Step 3: Command Handlers Implementation

#### 3.1 Audio Stream Command Handlers

```csharp
namespace SnapDog.Server.Features.AudioStreams.Handlers;

/// <summary>
/// Handler for creating audio streams.
/// </summary>
public class CreateAudioStreamHandler : IRequestHandler<CreateAudioStreamCommand, Result<AudioStream>>
{
    private readonly IAudioStreamRepository _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateAudioStreamHandler> _logger;

    public CreateAudioStreamHandler(
        IAudioStreamRepository repository,
        IMediator mediator,
        ILogger<CreateAudioStreamHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AudioStream>> Handle(CreateAudioStreamCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating audio stream: {StreamName}", request.Name);

        try
        {
            // Create domain entity
            var stream = new AudioStream(
                request.Name,
                request.Codec,
                request.SampleRate,
                request.BitDepth,
                request.Channels);

            // Persist to repository
            var createdStream = await _repository.CreateAsync(stream, cancellationToken);

            // Publish domain event
            var streamCreatedEvent = new StreamCreatedEvent(
                createdStream.Id,
                createdStream.Name,
                createdStream.Codec.ToString(),
                "System");

            await _mediator.Publish(streamCreatedEvent, cancellationToken);

            _logger.LogInformation("Successfully created audio stream: {StreamId}", createdStream.Id);
            return Result<AudioStream>.Success(createdStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audio stream: {StreamName}", request.Name);
            return Result<AudioStream>.Failure($"Failed to create stream: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for starting audio streams.
/// </summary>
public class StartAudioStreamHandler : IRequestHandler<StartAudioStreamCommand, Result>
{
    private readonly IAudioStreamRepository _repository;
    private readonly ISnapcastService _snapcastService;
    private readonly IMediator _mediator;
    private readonly ILogger<StartAudioStreamHandler> _logger;

    public StartAudioStreamHandler(
        IAudioStreamRepository repository,
        ISnapcastService snapcastService,
        IMediator mediator,
        ILogger<StartAudioStreamHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(StartAudioStreamCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting audio stream: {StreamId} by {StartedBy}",
            request.StreamId, request.StartedBy);

        try
        {
            // Get stream from repository
            var stream = await _repository.GetByIdAsync(request.StreamId, cancellationToken);
            if (stream == null)
            {
                _logger.LogWarning("Stream not found: {StreamId}", request.StreamId);
                return Result.Failure($"Stream not found: {request.StreamId}");
            }

            // Check if stream can be started
            var startResult = stream.Start();
            if (startResult.IsFailure)
            {
                _logger.LogWarning("Cannot start stream {StreamId}: {Error}",
                    request.StreamId, startResult.Error);
                return startResult;
            }

            // Update stream in repository
            await _repository.UpdateAsync(stream, cancellationToken);

            // Start stream in Snapcast (if configured)
            if (!string.IsNullOrEmpty(stream.SnapcastSinkName))
            {
                var snapcastResult = await _snapcastService.StartStreamAsync(
                    stream.SnapcastSinkName, cancellationToken);

                if (snapcastResult.IsFailure)
                {
                    _logger.LogWarning("Failed to start Snapcast stream: {Error}", snapcastResult.Error);
                    // Continue anyway - stream is started in our system
                }
            }

            // Publish domain event
            var streamStartedEvent = new StreamStartedEvent(
                stream.Id,
                stream.Name,
                request.StartedBy);

            await _mediator.Publish(streamStartedEvent, cancellationToken);

            _logger.LogInformation("Successfully started audio stream: {StreamId}", request.StreamId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audio stream: {StreamId}", request.StreamId);
            return Result.Failure($"Failed to start stream: {ex.Message}");
        }
    }
}
```

### Step 4: Validation Implementation

#### 4.1 Command Validators

```csharp
namespace SnapDog.Server.Features.AudioStreams.Validators;

/// <summary>
/// Validator for CreateAudioStreamCommand.
/// </summary>
public class CreateAudioStreamValidator : AbstractValidator<CreateAudioStreamCommand>
{
    public CreateAudioStreamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Stream name is required")
            .MaximumLength(100)
            .WithMessage("Stream name cannot exceed 100 characters")
            .Must(BeValidStreamName)
            .WithMessage("Stream name contains invalid characters");

        RuleFor(x => x.Codec)
            .IsInEnum()
            .WithMessage("Invalid audio codec specified");

        RuleFor(x => x.SampleRate)
            .GreaterThan(0)
            .WithMessage("Sample rate must be positive")
            .Must(BeValidSampleRate)
            .WithMessage("Sample rate must be a standard value (8000, 11025, 22050, 44100, 48000, 88200, 96000)");

        RuleFor(x => x.BitDepth)
            .GreaterThan(0)
            .WithMessage("Bit depth must be positive")
            .Must(BeValidBitDepth)
            .WithMessage("Bit depth must be 8, 16, 24, or 32");

        RuleFor(x => x.Channels)
            .InclusiveBetween(1, 8)
            .WithMessage("Channels must be between 1 and 8");
    }

    private static bool BeValidStreamName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        // Check for invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        return !name.Any(c => invalidChars.Contains(c));
    }

    private static bool BeValidSampleRate(int sampleRate)
    {
        var validRates = new[] { 8000, 11025, 22050, 44100, 48000, 88200, 96000 };
        return validRates.Contains(sampleRate);
    }

    private static bool BeValidBitDepth(int bitDepth)
    {
        var validDepths = new[] { 8, 16, 24, 32 };
        return validDepths.Contains(bitDepth);
    }
}

/// <summary>
/// Validator for SetClientVolumeCommand.
/// </summary>
public class SetClientVolumeValidator : AbstractValidator<SetClientVolumeCommand>
{
    public SetClientVolumeValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0)
            .WithMessage("Client ID must be positive");

        RuleFor(x => x.Volume)
            .InclusiveBetween(0, 100)
            .WithMessage("Volume must be between 0 and 100");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("UpdatedBy is required")
            .MaximumLength(50)
            .WithMessage("UpdatedBy cannot exceed 50 characters");
    }
}
```

### Step 5: Pipeline Behaviors

#### 5.1 Validation Behavior

```csharp
namespace SnapDog.Server.Behaviors;

/// <summary>
/// Pipeline behavior that validates requests using FluentValidation.
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogDebug("Validating request: {RequestName}", requestName);

        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .Where(r => !r.IsValid)
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Any())
            {
                _logger.LogWarning("Validation failed for {RequestName}: {Errors}",
                    requestName, string.Join(", ", failures.Select(f => f.ErrorMessage)));

                throw new ValidationException(failures);
            }
        }

        _logger.LogDebug("Validation passed for {RequestName}", requestName);
        return await next();
    }
}

/// <summary>
/// Pipeline behavior for logging request performance.
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Starting {RequestName}", requestName);

        try
        {
            var response = await next();

            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (elapsedMs > 500) // Log slow requests
            {
                _logger.LogWarning("Slow request detected: {RequestName} took {ElapsedMs}ms",
                    requestName, elapsedMs);
            }
            else
            {
                _logger.LogDebug("Completed {RequestName} in {ElapsedMs}ms",
                    requestName, elapsedMs);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### Step 6: Domain Event Handlers

#### 6.1 Event Handlers Implementation

```csharp
namespace SnapDog.Server.Features.Events.Handlers;

/// <summary>
/// Handler for stream started events.
/// </summary>
public class StreamStartedEventHandler : INotificationHandler<StreamStartedEvent>
{
    private readonly IStateManager _stateManager;
    private readonly IMqttService _mqttService;
    private readonly ILogger<StreamStartedEventHandler> _logger;

    public StreamStartedEventHandler(
        IStateManager stateManager,
        IMqttService mqttService,
        ILogger<StreamStartedEventHandler> logger)
    {
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(StreamStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling stream started event: {StreamId}", notification.StreamId);

        try
        {
            // Update global state
            await _stateManager.UpdateStateAsync(state =>
            {
                var updatedStream = state.Streams
                    .FirstOrDefault(s => s.Id == notification.StreamId);

                if (updatedStream != null)
                {
                    return state.WithUpdatedStream(updatedStream);
                }

                return state;
            });

            // Publish MQTT notification
            if (_mqttService.IsConnected)
            {
                var mqttPayload = JsonSerializer.Serialize(new
                {
                    streamId = notification.StreamId,
                    streamName = notification.StreamName,
                    status = "started",
                    startedBy = notification.StartedBy,
                    timestamp = notification.OccurredAt
                });

                await _mqttService.PublishAsync(
                    $"SNAPDOG/STREAM/{notification.StreamId}/STATUS",
                    mqttPayload,
                    retain: true,
                    cancellationToken);

                _logger.LogDebug("Published stream started notification to MQTT");
            }

            _logger.LogInformation("Successfully handled stream started event: {StreamId}", notification.StreamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle stream started event: {StreamId}", notification.StreamId);
            // Don't rethrow - event handling should not fail the original operation
        }
    }
}

/// <summary>
/// Handler for client connected events.
/// </summary>
public class ClientConnectedEventHandler : INotificationHandler<ClientConnectedEvent>
{
    private readonly ISnapcastService _snapcastService;
    private readonly IKnxService _knxService;
    private readonly ILogger<ClientConnectedEventHandler> _logger;

    public ClientConnectedEventHandler(
        ISnapcastService snapcastService,
        IKnxService knxService,
        ILogger<ClientConnectedEventHandler> logger)
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(ClientConnectedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling client connected event: {ClientId}", notification.ClientId);

        try
        {
            // Synchronize with Snapcast server
            var snapcastClients = await _snapcastService.GetClientsAsync(cancellationToken);
            if (snapcastClients.IsSuccess)
            {
                var snapcastClient = snapcastClients.Value
                    .FirstOrDefault(c => c.Host.Mac == notification.MacAddress);

                if (snapcastClient != null)
                {
                    _logger.LogDebug("Found corresponding Snapcast client: {SnapcastId}", snapcastClient.Id);
                    // Additional synchronization logic here
                }
            }

            // Send KNX notification if enabled
            if (_knxService.IsConnected)
            {
                // Send client connected status to KNX
                var statusGA = $"1/1/{notification.ClientId}"; // Example group address
                await _knxService.WriteGroupValueAsync(statusGA, new byte[] { 1 }, cancellationToken);

                _logger.LogDebug("Sent client connected status to KNX: {GroupAddress}", statusGA);
            }

            _logger.LogInformation("Successfully handled client connected event: {ClientId}", notification.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle client connected event: {ClientId}", notification.ClientId);
        }
    }
}
```

## Expected Deliverable

### Working Console Application Output

```
[16:00:15 INF] Starting SnapDog Phase 3 - Server Layer & Business Logic
[16:00:15 INF] Configuring MediatR with pipeline behaviors
[16:00:15 INF] Registering command and query handlers
[16:00:15 INF] Setting up domain event handlers
[16:00:15 INF] === Business Logic Demonstration ===
[16:00:15 INF] Creating audio stream via command handler
[16:00:15 DBG] Validating request: CreateAudioStreamCommand
[16:00:15 DBG] Validation passed for CreateAudioStreamCommand
[16:00:15 INF] Creating audio stream: Living Room Stream
[16:00:15 INF] Successfully created audio stream: 1
[16:00:15 INF] Publishing domain event: StreamCreatedEvent
[16:00:15 INF] Starting audio stream via command handler
[16:00:15 DBG] Validating request: StartAudioStreamCommand
[16:00:15 INF] Starting audio stream: 1 by System
[16:00:15 INF] Successfully started audio stream: 1
[16:00:15 INF] Handling stream started event: 1
[16:00:15 DBG] Published stream started notification to MQTT
[16:00:15 INF] Querying system status
[16:00:15 INF] System Status: 1 active streams, 0 clients, 0 zones
[16:00:15 INF] === Phase 3 Business Logic Complete ===
```

### Test Results

```
Phase 3 Test Results:
===================
Command Handler Tests: 35/35 passed
Query Handler Tests: 20/20 passed
Validation Tests: 25/25 passed
Domain Event Tests: 15/15 passed
Pipeline Behavior Tests: 12/12 passed
Integration Tests: 18/18 passed

Total Tests: 125/125 passed
Code Coverage: 96%
```

## Quality Gates

### Code Quality Checklist

- [ ] All business operations implemented as commands/queries
- [ ] Comprehensive validation for all inputs
- [ ] Domain events published for all state changes
- [ ] Pipeline behaviors handle cross-cutting concerns
- [ ] Error handling and logging properly implemented
- [ ] 95%+ test coverage for business logic

### Architecture Validation

- [ ] MediatR CQRS pattern correctly implemented
- [ ] Business logic properly separated from infrastructure
- [ ] Domain events follow proper patterns
- [ ] Validation integrated into request pipeline
- [ ] Handler dependencies properly injected

## Next Steps

Upon successful completion of Phase 3:

1. **Validate all business logic** against success criteria
2. **Test command/query workflows** end-to-end
3. **Verify domain event publishing** and handling
4. **Prepare for Phase 4** by reviewing API layer requirements
5. **Begin Phase 4** with complete business logic foundation

Phase 3 transforms the domain and infrastructure into a fully functional business layer with comprehensive CQRS implementation.
