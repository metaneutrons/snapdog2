using MediatR;
using SnapDog2.Core.Common;

namespace SnapDog2.Server.Features.AudioStreams.Commands;

/// <summary>
/// Command to delete an audio stream from the system.
/// This command removes the stream permanently and ensures it's not actively playing.
/// </summary>
/// <param name="StreamId">The unique identifier of the stream to delete.</param>
/// <param name="RequestedBy">The user or system that requested the stream deletion.</param>
public record DeleteAudioStreamCommand(string StreamId, string RequestedBy = "System") : IRequest<Result>
{
    /// <summary>
    /// Gets a value indicating whether to force delete the stream even if it's currently active.
    /// If false, the command will fail if the stream is playing.
    /// </summary>
    public bool ForceDelete { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to create a backup before deletion.
    /// </summary>
    public bool CreateBackup { get; init; } = false;

    /// <summary>
    /// Gets additional metadata or context for the deletion operation.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets the timeout in seconds for the deletion operation.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Creates a new DeleteAudioStreamCommand with basic parameters.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream to delete.</param>
    /// <param name="requestedBy">The user or system that requested the stream deletion.</param>
    /// <returns>A new DeleteAudioStreamCommand instance.</returns>
    public static DeleteAudioStreamCommand Create(string streamId, string requestedBy = "System")
    {
        return new DeleteAudioStreamCommand(streamId, requestedBy);
    }

    /// <summary>
    /// Creates a new DeleteAudioStreamCommand with all optional parameters.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream to delete.</param>
    /// <param name="requestedBy">The user or system that requested the stream deletion.</param>
    /// <param name="forceDelete">Whether to force delete the stream even if it's active.</param>
    /// <param name="createBackup">Whether to create a backup before deletion.</param>
    /// <param name="reason">Additional metadata or context for the deletion operation.</param>
    /// <param name="timeoutSeconds">The timeout in seconds for the deletion operation.</param>
    /// <returns>A new DeleteAudioStreamCommand instance.</returns>
    public static DeleteAudioStreamCommand CreateDetailed(
        string streamId,
        string requestedBy = "System",
        bool forceDelete = false,
        bool createBackup = false,
        string? reason = null,
        int timeoutSeconds = 30
    )
    {
        return new DeleteAudioStreamCommand(streamId, requestedBy)
        {
            ForceDelete = forceDelete,
            CreateBackup = createBackup,
            Reason = reason,
            TimeoutSeconds = timeoutSeconds,
        };
    }

    /// <summary>
    /// Creates a forced delete command for administrative purposes.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream to delete.</param>
    /// <param name="requestedBy">The user or system that requested the stream deletion.</param>
    /// <param name="reason">The reason for the forced deletion.</param>
    /// <returns>A new DeleteAudioStreamCommand instance configured for forced deletion.</returns>
    public static DeleteAudioStreamCommand CreateForced(
        string streamId,
        string requestedBy = "System",
        string? reason = "Administrative deletion"
    )
    {
        return new DeleteAudioStreamCommand(streamId, requestedBy)
        {
            ForceDelete = true,
            CreateBackup = true,
            Reason = reason,
            TimeoutSeconds = 60,
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
