namespace SnapDog2.Server.Features.Shared.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;

/// <summary>
/// Notification published when a command status changes (acknowledgments).
/// </summary>
[StatusId("COMMAND_STATUS")]
public record CommandStatusNotification : INotification
{
    /// <summary>
    /// Gets the command ID that this status relates to.
    /// </summary>
    public required string CommandId { get; init; }

    /// <summary>
    /// Gets the status of the command ("ok", "processing", "done").
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the zone index if this is a zone-specific command (1-based).
    /// </summary>
    public int? ZoneIndex { get; init; }

    /// <summary>
    /// Gets the client index if this is a client-specific command (1-based).
    /// </summary>
    public int? ClientIndex { get; init; }

    /// <summary>
    /// Gets additional context or metadata about the command execution.
    /// </summary>
    public string? Context { get; init; }
}

/// <summary>
/// Notification published when a command encounters an error.
/// </summary>
[StatusId("COMMAND_ERROR")]
public record CommandErrorNotification : INotification
{
    /// <summary>
    /// Gets the command ID that encountered the error.
    /// </summary>
    public required string CommandId { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the error code (HTTP-style or custom error code).
    /// </summary>
    public required int ErrorCode { get; init; }

    /// <summary>
    /// Gets the zone index if this is a zone-specific command (1-based).
    /// </summary>
    public int? ZoneIndex { get; init; }

    /// <summary>
    /// Gets the client index if this is a client-specific command (1-based).
    /// </summary>
    public int? ClientIndex { get; init; }

    /// <summary>
    /// Gets additional error details or stack trace information.
    /// </summary>
    public string? ErrorDetails { get; init; }
}
