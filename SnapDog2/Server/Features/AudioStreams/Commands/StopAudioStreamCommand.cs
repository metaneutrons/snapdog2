using MediatR;
using SnapDog2.Core.Common;

namespace SnapDog2.Server.Features.AudioStreams.Commands;

/// <summary>
/// Command to stop an active audio stream.
/// This command triggers the stream to stop playing and updates the stream status.
/// </summary>
/// <param name="StreamId">The unique identifier of the stream to stop.</param>
/// <param name="RequestedBy">The user or system that requested the stream to stop.</param>
public record StopAudioStreamCommand(string StreamId, string RequestedBy = "System") : IRequest<Result>
{
    /// <summary>
    /// Gets a value indicating whether to force stop the stream immediately.
    /// If false, the stream will be stopped gracefully.
    /// </summary>
    public bool ForceStop { get; init; } = false;

    /// <summary>
    /// Gets the timeout in seconds for the stop operation.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 15;

    /// <summary>
    /// Gets additional metadata or context for the stop operation.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets a value indicating whether to remove the stream from all groups after stopping.
    /// </summary>
    public bool RemoveFromGroups { get; init; } = false;

    /// <summary>
    /// Creates a new StopAudioStreamCommand with basic parameters.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream to stop.</param>
    /// <param name="requestedBy">The user or system that requested the stream to stop.</param>
    /// <returns>A new StopAudioStreamCommand instance.</returns>
    public static StopAudioStreamCommand Create(string streamId, string requestedBy = "System")
    {
        return new StopAudioStreamCommand(streamId, requestedBy);
    }

    /// <summary>
    /// Creates a new StopAudioStreamCommand with all optional parameters.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream to stop.</param>
    /// <param name="requestedBy">The user or system that requested the stream to stop.</param>
    /// <param name="forceStop">Whether to force stop the stream immediately.</param>
    /// <param name="timeoutSeconds">The timeout in seconds for the stop operation.</param>
    /// <param name="reason">Additional metadata or context for the stop operation.</param>
    /// <param name="removeFromGroups">Whether to remove the stream from all groups after stopping.</param>
    /// <returns>A new StopAudioStreamCommand instance.</returns>
    public static StopAudioStreamCommand CreateDetailed(
        string streamId,
        string requestedBy = "System",
        bool forceStop = false,
        int timeoutSeconds = 15,
        string? reason = null,
        bool removeFromGroups = false
    )
    {
        return new StopAudioStreamCommand(streamId, requestedBy)
        {
            ForceStop = forceStop,
            TimeoutSeconds = timeoutSeconds,
            Reason = reason,
            RemoveFromGroups = removeFromGroups,
        };
    }

    /// <summary>
    /// Creates a forced stop command for emergency situations.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream to stop.</param>
    /// <param name="requestedBy">The user or system that requested the stream to stop.</param>
    /// <param name="reason">The reason for the forced stop.</param>
    /// <returns>A new StopAudioStreamCommand instance configured for forced stop.</returns>
    public static StopAudioStreamCommand CreateForced(
        string streamId,
        string requestedBy = "System",
        string? reason = "Emergency stop"
    )
    {
        return new StopAudioStreamCommand(streamId, requestedBy)
        {
            ForceStop = true,
            TimeoutSeconds = 5,
            Reason = reason,
            RemoveFromGroups = true,
        };
    }

    /// <summary>
    /// Validates the command parameters.
    /// </summary>
    /// <returns>True if the command is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(StreamId) && !string.IsNullOrWhiteSpace(RequestedBy) && TimeoutSeconds > 0;
    }
}
