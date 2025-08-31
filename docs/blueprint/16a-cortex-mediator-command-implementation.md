# 19. Cortex.Mediator Command Framework Implementation

This document provides the concrete Cortex.Mediator implementation of the Command Framework defined in Section 9. It translates the logical commands and status updates into specific C# classes using Cortex.Mediator interfaces.

## 19.1. Implementation Overview

The implementation follows these key principles:

1. **Command Separation**: Commands that change state implement `ICommand<Result>` or `ICommand<Result<T>>`
2. **Query Separation**: Queries that retrieve data implement `IQuery<Result<T>>`
3. **Notification Events**: Status updates are published as `INotification` objects
4. **Validation**: All commands include FluentValidation validators
5. **Handler Organization**: Handlers are organized by domain area (Global, Zone, Client)

## 19.2. Project Structure

```
/Server/Features/
├── Global/
│   ├── Commands/
│   ├── Queries/
│   └── Handlers/
├── Zones/
│   ├── Commands/
│   ├── Queries/
│   ├── Handlers/
│   └── Validators/
├── Clients/
│   ├── Commands/
│   ├── Queries/
│   ├── Handlers/
│   └── Validators/
└── Shared/
    └── Notifications/
```

## 19.3. Base Interfaces and Common Types

### 19.3.1. Command Result Types

```csharp
// Already defined in Core.Models - referenced here for clarity
namespace SnapDog2.Core.Models;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
public class Result : IResult
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string ErrorMessage { get; }

    // Implementation details...
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
public class Result<T> : Result
{
    public T Value { get; }

    // Implementation details...
}
```

### 19.3.2. Common Enums

```csharp
namespace SnapDog2.Core.Enums;

/// <summary>
/// Represents the playback state of a zone.
/// </summary>
public enum PlaybackStatus
{
    Stopped,
    Playing,
    Paused
}

/// <summary>
/// Represents the source of a command.
/// </summary>
public enum CommandSource
{
    Internal,
    Api,
    Mqtt,
    Knx,
    WebSocket
}
```

## 19.4. Global Commands and Queries

### 19.4.1. Global Status Queries

```csharp
// /Server/Features/Global/Queries/GetSystemStatusQuery.cs
namespace SnapDog2.Server.Features.Global.Queries;

using Cortex.Mediator;
using SnapDog2.Core.Models;

/// <summary>
/// Query to get the current system status.
/// </summary>
public record GetSystemStatusQuery : IQuery<Result<SystemStatus>>;

/// <summary>
/// Query to get system version information.
/// </summary>
public record GetVersionInfoQuery : IQuery<Result<VersionDetails>>;

/// <summary>
/// Query to get server performance statistics.
/// </summary>
public record GetServerStatsQuery : IQuery<Result<ServerStats>>;
```

### 19.4.2. Global Query Handlers

```csharp
// /Server/Features/Global/Handlers/GetSystemStatusQueryHandler.cs
namespace SnapDog2.Server.Features.Global.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Queries;

/// <summary>
/// Handles the GetSystemStatusQuery.
/// </summary>
public partial class GetSystemStatusQueryHandler : IQueryHandler<GetSystemStatusQuery, Result<SystemStatus>>
{
    private readonly IAppStatusService _systemStatusService;
    private readonly ILogger<GetSystemStatusQueryHandler> _logger;

    [LoggerMessage(1001, LogLevel.Information, "Handling GetSystemStatusQuery")]
    private partial void LogHandling();

    public GetSystemStatusQueryHandler(
        IAppStatusService systemStatusService,
        ILogger<GetSystemStatusQueryHandler> logger)
    {
        _systemStatusService = systemStatusService;
        _logger = logger;
    }

    public async Task<Result<SystemStatus>> Handle(GetSystemStatusQuery request, CancellationToken cancellationToken)
    {
        LogHandling();

        try
        {
            var status = await _systemStatusService.GetCurrentStatusAsync().ConfigureAwait(false);
            return Result<SystemStatus>.Success(status);
        }
        catch (Exception ex)
        {
            return Result<SystemStatus>.Failure(ex);
        }
    }
}
```

## 19.5. Zone Commands

This section will be expanded in the next parts of this document.

### 19.5.1. Playback Control Commands

```csharp
// /Server/Features/Zones/Commands/PlayCommand.cs
namespace SnapDog2.Server.Features.Zones.Commands;

using Cortex.Mediator;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Command to start or resume playback in a zone.
/// </summary>
public record PlayCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the optional track index to play (1-based).
    /// </summary>
    public int? TrackIndex { get; init; }

    /// <summary>
    /// Gets the optional media URL to play.
    /// </summary>
    public string? MediaUrl { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to pause playback in a zone.
/// </summary>
public record PauseCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}

/// <summary>
/// Command to stop playback in a zone.
/// </summary>
public record StopCommand : ICommand<Result>
{
    /// <summary>
    /// Gets the ID of the target zone.
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the source that initiated the command.
    /// </summary>
    public CommandSource Source { get; init; } = CommandSource.Internal;
}
```

## 19.6. Status Notifications

### 19.6.1. Global Status Notifications

```csharp
// /Server/Features/Shared/Notifications/SystemStatusChangedNotification.cs
namespace SnapDog2.Server.Features.Shared.Notifications;

using System;
using Cortex.Mediator;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when the system status changes.
/// </summary>
public record SystemStatusChangedNotification : INotification
{
    /// <summary>
    /// Gets the new system status.
    /// </summary>
    public required SystemStatus Status { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the status changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a system error occurs.
/// </summary>
public record SystemErrorNotification : INotification
{
    /// <summary>
    /// Gets the error details.
    /// </summary>
    public required ErrorDetails Error { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the error occurred.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
```

## 19.7. Command Validators

### 19.7.1. Zone Command Validators

```csharp
// /Server/Features/Zones/Validators/PlayCommandValidator.cs
namespace SnapDog2.Server.Features.Zones.Validators;

using FluentValidation;
using SnapDog2.Server.Features.Zones.Commands;

/// <summary>
/// Validator for the PlayCommand.
/// </summary>
public class PlayCommandValidator : AbstractValidator<PlayCommand>
{
    public PlayCommandValidator()
    {
        RuleFor(x => x.ZoneIndex)
            .GreaterThan(0)
            .WithMessage("Zone ID must be a positive integer.");

        RuleFor(x => x.TrackIndex)
            .GreaterThan(0)
            .When(x => x.TrackIndex.HasValue)
            .WithMessage("Track index must be a positive integer when specified.");

        RuleFor(x => x.MediaUrl)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.MediaUrl))
            .WithMessage("Media URL must be a valid URL when specified.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid command source specified.");
    }

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
```

---

**Note**: This document will be continued in subsequent sections covering:

- Complete Zone Commands and Handlers (6a.8)
- Client Commands and Handlers (6a.9)
- Query Implementations (6a.10)
- Integration with Infrastructure Adapters (6a.11)
- Testing Strategies (6a.12)

The implementation follows the patterns established in Section 6 (Cortex.Mediator Implementation) and provides concrete classes for all commands and queries defined in Section 9 (Command Framework).
