# 20. Client Commands Implementation

This document covers the Cortex.Mediator implementation for Client Commands from Section 9.4.

## 20.1. Client Volume and Mute Commands

```csharp
// /Server/Features/Clients/Commands/ClientVolumeCommands.cs
namespace SnapDog2.Server.Features.Clients.Commands;

using Cortex.Mediator;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set the volume for a specific client.
/// </summary>
public record SetClientVolumeCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target client.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the desired volume level (0-100).
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to set client mute state.
/// </summary>
public record SetClientMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target client.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets whether to mute (true) or unmute (false) the client.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to toggle client mute state.
/// </summary>
public record ToggleClientMuteCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target client.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
```

## 20.2. Client Configuration Commands

```csharp
// /Server/Features/Clients/Commands/ClientConfigCommands.cs
namespace SnapDog2.Server.Features.Clients.Commands;

using Cortex.Mediator;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to set client latency.
/// </summary>
public record SetClientLatencyCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target client.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the latency in milliseconds.
    /// </summary>
    public required int LatencyMs { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to assign a client to a zone.
/// </summary>
public record AssignClientToZoneCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target client.
    /// </summary>
    public required int ClientIndex { get; init; }

    /// <summary>
    /// Gets the ID of the zone to assign the client to (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
```

## 20.3. Client Command Validators

```csharp
// /Server/Features/Clients/Validators/ClientCommandValidators.cs
namespace SnapDog2.Server.Features.Clients.Validators;

using FluentValidation;
using SnapDog2.Server.Features.Clients.Commands;

/// <summary>
/// Validator for the SetClientVolumeCommand.
/// </summary>
public class SetClientVolumeCommandValidator : AbstractValidator<SetClientVolumeCommand>
{
    public SetClientVolumeCommandValidator()
    {
        RuleFor(x => x.ClientIndex)
            .GreaterThan(0)
            .WithMessage("Client ID must be a positive integer.");

        RuleFor(x => x.Volume)
            .InclusiveBetween(0, 100)
            .WithMessage("Volume must be between 0 and 100.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the SetClientMuteCommand.
/// </summary>
public class SetClientMuteCommandValidator : AbstractValidator<SetClientMuteCommand>
{
    public SetClientMuteCommandValidator()
    {
        RuleFor(x => x.ClientIndex)
            .GreaterThan(0)
            .WithMessage("Client ID must be a positive integer.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the ToggleClientMuteCommand.
/// </summary>
public class ToggleClientMuteCommandValidator : AbstractValidator<ToggleClientMuteCommand>
{
    public ToggleClientMuteCommandValidator()
    {
        RuleFor(x => x.ClientIndex)
            .GreaterThan(0)
            .WithMessage("Client ID must be a positive integer.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the SetClientLatencyCommand.
/// </summary>
public class SetClientLatencyCommandValidator : AbstractValidator<SetClientLatencyCommand>
{
    public SetClientLatencyCommandValidator()
    {
        RuleFor(x => x.ClientIndex)
            .GreaterThan(0)
            .WithMessage("Client ID must be a positive integer.");

        RuleFor(x => x.LatencyMs)
            .InclusiveBetween(0, 10000)
            .WithMessage("Latency must be between 0 and 10000 milliseconds.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}

/// <summary>
/// Validator for the AssignClientToZoneCommand.
/// </summary>
public class AssignClientToZoneCommandValidator : AbstractValidator<AssignClientToZoneCommand>
{
    public AssignClientToZoneCommandValidator()
    {
        RuleFor(x => x.ClientIndex)
            .GreaterThan(0)
            .WithMessage("Client ID must be a positive integer.");

        RuleFor(x => x.ZoneIndex)
            .GreaterThan(0)
            .WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }
}
```

## 20.4. Client Command Handlers

```csharp
// /Server/Features/Clients/Handlers/SetClientVolumeCommandHandler.cs
namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands;

/// <summary>
/// Handles the SetClientVolumeCommand.
/// </summary>
public partial class SetClientVolumeCommandHandler : ICommandHandler<SetClientVolumeCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<SetClientVolumeCommandHandler> _logger;

    [LoggerMessage(3001, LogLevel.Information, "Setting volume for Client {ClientIndex} to {Volume} from {Source}")]
    private partial void LogHandling(int clientIndex, int volume, CommandSource source);

    [LoggerMessage(3002, LogLevel.Warning, "Client {ClientIndex} not found for SetClientVolumeCommand")]
    private partial void LogClientNotFound(int clientIndex);

    public SetClientVolumeCommandHandler(
        IClientManager clientManager,
        ILogger<SetClientVolumeCommandHandler> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    public async Task<Result> Handle(SetClientVolumeCommand request, CancellationToken cancellationToken)
    {
        LogHandling(request.ClientIndex, request.Volume, request.Source);

        // Get the client
        var clientResult = await _clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value;

        // Set the volume
        var result = await client.SetVolumeAsync(request.Volume).ConfigureAwait(false);

        return result;
    }
}

// /Server/Features/Clients/Handlers/AssignClientToZoneCommandHandler.cs
namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands;

/// <summary>
/// Handles the AssignClientToZoneCommand.
/// </summary>
public partial class AssignClientToZoneCommandHandler : ICommandHandler<AssignClientToZoneCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<AssignClientToZoneCommandHandler> _logger;

    [LoggerMessage(3101, LogLevel.Information, "Assigning Client {ClientIndex} to Zone {ZoneIndex} from {Source}")]
    private partial void LogHandling(int clientIndex, int zoneIndex, CommandSource source);

    [LoggerMessage(3102, LogLevel.Warning, "Client {ClientIndex} not found for AssignClientToZoneCommand")]
    private partial void LogClientNotFound(int clientIndex);

    [LoggerMessage(3103, LogLevel.Warning, "Zone {ZoneIndex} not found for AssignClientToZoneCommand")]
    private partial void LogZoneNotFound(int zoneIndex);

    public AssignClientToZoneCommandHandler(
        IClientManager clientManager,
        IZoneManager zoneManager,
        ILogger<AssignClientToZoneCommandHandler> logger)
    {
        _clientManager = clientManager;
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignClientToZoneCommand request, CancellationToken cancellationToken)
    {
        LogHandling(request.ClientIndex, request.ZoneIndex, request.Source);

        // Validate client exists
        var clientResult = await _clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        // Validate zone exists
        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            LogZoneNotFound(request.ZoneIndex);
            return zoneResult;
        }

        // Perform the assignment
        var result = await _clientManager.AssignClientToZoneAsync(request.ClientIndex, request.ZoneIndex).ConfigureAwait(false);

        return result;
    }
}

// /Server/Features/Clients/Handlers/SetClientLatencyCommandHandler.cs
namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands;

/// <summary>
/// Handles the SetClientLatencyCommand.
/// </summary>
public partial class SetClientLatencyCommandHandler : ICommandHandler<SetClientLatencyCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<SetClientLatencyCommandHandler> _logger;

    [LoggerMessage(3201, LogLevel.Information, "Setting latency for Client {ClientIndex} to {LatencyMs}ms from {Source}")]
    private partial void LogHandling(int clientIndex, int latencyMs, CommandSource source);

    public SetClientLatencyCommandHandler(
        IClientManager clientManager,
        ILogger<SetClientLatencyCommandHandler> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    public async Task<Result> Handle(SetClientLatencyCommand request, CancellationToken cancellationToken)
    {
        LogHandling(request.ClientIndex, request.LatencyMs, request.Source);

        // Get the client
        var clientResult = await _clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            return clientResult;
        }

        var client = clientResult.Value;

        // Set the latency
        var result = await client.SetLatencyAsync(request.LatencyMs).ConfigureAwait(false);

        return result;
    }
}
```

## 20.5. Client Queries

```csharp
// /Server/Features/Clients/Queries/ClientQueries.cs
namespace SnapDog2.Server.Features.Clients.Queries;

using System.Collections.Generic;
using Cortex.Mediator;
using SnapDog2.Core.Models;

/// <summary>
/// Query to retrieve the state of all known clients.
/// </summary>
public record GetAllClientsQuery : IQuery<Result<List<ClientState>>>;

/// <summary>
/// Query to retrieve the state of a specific client.
/// </summary>
public record GetClientQuery : IQuery<Result<ClientState>>
{
    /// <summary>
    /// Gets the ID of the client to retrieve.
    /// </summary>
    public required int ClientIndex { get; init; }
}

/// <summary>
/// Query to retrieve clients assigned to a specific zone.
/// </summary>
public record GetClientsByZoneQuery : IQuery<Result<List<ClientState>>>
{
    /// <summary>
    /// Gets the ID of the zone.
    /// </summary>
    public required int ZoneIndex { get; init; }
}
```

## 20.6. Client Query Handlers

```csharp
// /Server/Features/Clients/Handlers/GetAllClientsQueryHandler.cs
namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Handles the GetAllClientsQuery.
/// </summary>
public partial class GetAllClientsQueryHandler : IQueryHandler<GetAllClientsQuery, Result<List<ClientState>>>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<GetAllClientsQueryHandler> _logger;

    [LoggerMessage(4001, LogLevel.Information, "Handling GetAllClientsQuery")]
    private partial void LogHandling();

    public GetAllClientsQueryHandler(
        IClientManager clientManager,
        ILogger<GetAllClientsQueryHandler> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    public async Task<Result<List<ClientState>>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken)
    {
        LogHandling();

        try
        {
            var clients = await _clientManager.GetAllClientsAsync().ConfigureAwait(false);
            return Result<List<ClientState>>.Success(clients);
        }
        catch (Exception ex)
        {
            return Result<List<ClientState>>.Failure(ex);
        }
    }
}

// /Server/Features/Clients/Handlers/GetClientQueryHandler.cs
namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Handles the GetClientQuery.
/// </summary>
public partial class GetClientQueryHandler : IQueryHandler<GetClientQuery, Result<ClientState>>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<GetClientQueryHandler> _logger;

    [LoggerMessage(4101, LogLevel.Information, "Handling GetClientQuery for Client {ClientIndex}")]
    private partial void LogHandling(int clientIndex);

    public GetClientQueryHandler(
        IClientManager clientManager,
        ILogger<GetClientQueryHandler> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    public async Task<Result<ClientState>> Handle(GetClientQuery request, CancellationToken cancellationToken)
    {
        LogHandling(request.ClientIndex);

        var result = await _clientManager.GetClientStateAsync(request.ClientIndex).ConfigureAwait(false);
        return result;
    }
}
```

---

**Next**: Continue with Zone Queries (6d) and Status Notifications (6e).
