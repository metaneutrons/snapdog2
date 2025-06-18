using MediatR;
using SnapDog2.Core.Common;

namespace SnapDog2.Server.Features.AudioStreams.Commands;

/// <summary>
/// Command to start an existing audio stream.
/// This command triggers the stream to begin playing and integrates with the Snapcast service.
/// </summary>
/// <param name="StreamId">The unique identifier of the stream to start.</param>
/// <param name="RequestedBy">The user or system that requested the stream to start.</param>
public record StartAudioStreamCommand(string StreamId, string RequestedBy = "System") : IRequest<Result>
{
    /// <summary>
    /// Gets the group ID to assign the stream to (optional).
    /// If not specified, the stream will be assigned to the default group.
    /// </summary>
    public string? GroupId { get; init; }

    /// <summary>
    /// Gets a value indicating whether to force start the stream even if another stream is already playing.
    /// </summary>
    public bool ForceStart { get; init; } = false;

    /// <summary>
    /// Gets the timeout in seconds for the start operation.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Gets additional metadata or context for the start operation.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Creates a new StartAudioStreamCommand with basic parameters.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream to start.</param>
    /// <param name="requestedBy">The user or system that requested the stream to start.</param>
    /// <returns>A new StartAudioStreamCommand instance.</returns>
    public static StartAudioStreamCommand Create(string streamId, string requestedBy = "System")
    {
        return new StartAudioStreamCommand(streamId, requestedBy);
    }

    /// <summary>
    /// Creates a new StartAudioStreamCommand with all optional parameters.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream to start.</param>
    /// <param name="requestedBy">The user or system that requested the stream to start.</param>
    /// <param name="groupId">The group ID to assign the stream to.</param>
    /// <param name="forceStart">Whether to force start the stream.</param>
    /// <param name="timeoutSeconds">The timeout in seconds for the start operation.</param>
    /// <param name="reason">Additional metadata or context for the start operation.</param>
    /// <returns>A new StartAudioStreamCommand instance.</returns>
    public static StartAudioStreamCommand CreateDetailed(
        string streamId,
        string requestedBy = "System",
        string? groupId = null,
        bool forceStart = false,
        int timeoutSeconds = 30,
        string? reason = null
    )
    {
        return new StartAudioStreamCommand(streamId, requestedBy)
        {
            GroupId = groupId,
            ForceStart = forceStart,
            TimeoutSeconds = timeoutSeconds,
            Reason = reason,
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
