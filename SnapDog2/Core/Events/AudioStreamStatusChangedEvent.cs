using SnapDog2.Core.Models.Enums;

namespace SnapDog2.Core.Events;

/// <summary>
/// Domain event triggered when an audio stream's status changes.
/// Contains information about the stream and its previous and new status.
/// </summary>
public sealed record AudioStreamStatusChangedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the audio stream.
    /// </summary>
    public required string StreamId { get; init; }

    /// <summary>
    /// Gets the name of the audio stream.
    /// </summary>
    public required string StreamName { get; init; }

    /// <summary>
    /// Gets the previous status of the stream.
    /// </summary>
    public required StreamStatus PreviousStatus { get; init; }

    /// <summary>
    /// Gets the new status of the stream.
    /// </summary>
    public required StreamStatus NewStatus { get; init; }

    /// <summary>
    /// Gets the URL of the audio stream.
    /// </summary>
    public string? StreamUrl { get; init; }

    /// <summary>
    /// Gets additional context or reason for the status change.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStreamStatusChangedEvent"/> record.
    /// </summary>
    public AudioStreamStatusChangedEvent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStreamStatusChangedEvent"/> record with a correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    public AudioStreamStatusChangedEvent(string? correlationId)
        : base(correlationId) { }

    /// <summary>
    /// Creates a new audio stream status changed event.
    /// </summary>
    /// <param name="streamId">The unique identifier of the audio stream.</param>
    /// <param name="streamName">The name of the audio stream.</param>
    /// <param name="previousStatus">The previous status of the stream.</param>
    /// <param name="newStatus">The new status of the stream.</param>
    /// <param name="streamUrl">The URL of the audio stream.</param>
    /// <param name="reason">Additional context or reason for the status change.</param>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    /// <returns>A new <see cref="AudioStreamStatusChangedEvent"/> instance.</returns>
    public static AudioStreamStatusChangedEvent Create(
        string streamId,
        string streamName,
        StreamStatus previousStatus,
        StreamStatus newStatus,
        string? streamUrl = null,
        string? reason = null,
        string? correlationId = null
    )
    {
        return new AudioStreamStatusChangedEvent(correlationId)
        {
            StreamId = streamId,
            StreamName = streamName,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            StreamUrl = streamUrl,
            Reason = reason,
        };
    }

    /// <summary>
    /// Gets a value indicating whether the stream transitioned to a playing state.
    /// </summary>
    public bool IsTransitionToPlaying => NewStatus == StreamStatus.Playing && PreviousStatus != StreamStatus.Playing;

    /// <summary>
    /// Gets a value indicating whether the stream transitioned to a stopped state.
    /// </summary>
    public bool IsTransitionToStopped => NewStatus == StreamStatus.Stopped && PreviousStatus != StreamStatus.Stopped;

    /// <summary>
    /// Gets a value indicating whether the stream transitioned to an error state.
    /// </summary>
    public bool IsTransitionToError => NewStatus == StreamStatus.Error && PreviousStatus != StreamStatus.Error;
}
