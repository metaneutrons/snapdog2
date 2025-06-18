using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;
using SnapDog2.Core.Events;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Infrastructure.Repositories;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.AudioStreams.Commands;

namespace SnapDog2.Server.Features.AudioStreams.Handlers;

/// <summary>
/// Handler for starting audio streams in the system.
/// Integrates with Snapcast service and publishes domain events.
/// </summary>
public sealed class StartAudioStreamHandler : IRequestHandler<StartAudioStreamCommand, Result>
{
    private readonly IAudioStreamRepository _audioStreamRepository;
    private readonly ISnapcastService _snapcastService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<StartAudioStreamHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartAudioStreamHandler"/> class.
    /// </summary>
    /// <param name="audioStreamRepository">The audio stream repository.</param>
    /// <param name="snapcastService">The Snapcast service.</param>
    /// <param name="eventPublisher">The event publisher.</param>
    /// <param name="logger">The logger.</param>
    public StartAudioStreamHandler(
        IAudioStreamRepository audioStreamRepository,
        ISnapcastService snapcastService,
        IEventPublisher eventPublisher,
        ILogger<StartAudioStreamHandler> logger
    )
    {
        _audioStreamRepository =
            audioStreamRepository ?? throw new ArgumentNullException(nameof(audioStreamRepository));
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the starting of an audio stream.
    /// </summary>
    /// <param name="request">The start audio stream command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> Handle(StartAudioStreamCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Starting audio stream: {StreamId} requested by: {RequestedBy}",
                request.StreamId,
                request.RequestedBy
            );

            // Validate command
            if (!request.IsValid())
            {
                return Result.Failure("Invalid start stream command parameters.");
            }

            // Retrieve the stream
            var audioStream = await _audioStreamRepository.GetByIdAsync(request.StreamId, cancellationToken);
            if (audioStream == null)
            {
                _logger.LogWarning("Audio stream not found: {StreamId}", request.StreamId);
                return Result.Failure($"Audio stream with ID '{request.StreamId}' not found.");
            }

            // Check if stream is already playing
            if (audioStream.IsPlaying && !request.ForceStart)
            {
                _logger.LogInformation("Audio stream {StreamId} is already playing", request.StreamId);
                return Result.Failure($"Audio stream '{audioStream.Name}' is already playing.");
            }

            // Check Snapcast service availability
            var isSnapcastAvailable = await _snapcastService.IsServerAvailableAsync(cancellationToken);
            if (!isSnapcastAvailable)
            {
                _logger.LogError("Snapcast service is not available");
                return Result.Failure("Snapcast service is not available. Cannot start audio stream.");
            }

            // Update stream status to starting
            var startingStream = audioStream.WithStatus(StreamStatus.Starting);
            await _audioStreamRepository.UpdateAsync(startingStream, cancellationToken);

            // Publish status change event
            await PublishStatusChangeEventAsync(
                audioStream,
                StreamStatus.Starting,
                request.RequestedBy,
                cancellationToken
            );

            try
            {
                // Attempt to start the stream via Snapcast
                var success = await StartStreamViaSnapcastAsync(request, startingStream, cancellationToken);

                if (success)
                {
                    // Update stream status to playing
                    var playingStream = startingStream.WithStatus(StreamStatus.Playing);
                    await _audioStreamRepository.UpdateAsync(playingStream, cancellationToken);

                    // Publish playing status change event
                    await PublishStatusChangeEventAsync(
                        startingStream,
                        StreamStatus.Playing,
                        request.RequestedBy,
                        cancellationToken
                    );

                    _logger.LogInformation(
                        "Successfully started audio stream: {StreamId} - {StreamName}",
                        audioStream.Id,
                        audioStream.Name
                    );

                    return Result.Success();
                }
                else
                {
                    // Revert to stopped status on failure
                    var stoppedStream = startingStream.WithStatus(StreamStatus.Stopped);
                    await _audioStreamRepository.UpdateAsync(stoppedStream, cancellationToken);

                    await PublishStatusChangeEventAsync(
                        startingStream,
                        StreamStatus.Stopped,
                        request.RequestedBy,
                        cancellationToken
                    );

                    return Result.Failure("Failed to start stream via Snapcast service.");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Start stream operation was cancelled for stream: {StreamId}", request.StreamId);

                // Revert to stopped status on cancellation
                var stoppedStream = startingStream.WithStatus(StreamStatus.Stopped);
                await _audioStreamRepository.UpdateAsync(stoppedStream, cancellationToken);

                return Result.Failure("Start stream operation was cancelled.");
            }
            catch (TimeoutException)
            {
                _logger.LogError("Timeout occurred while starting stream: {StreamId}", request.StreamId);

                // Update to error status on timeout
                var errorStream = startingStream.WithStatus(StreamStatus.Error);
                await _audioStreamRepository.UpdateAsync(errorStream, cancellationToken);

                await PublishStatusChangeEventAsync(
                    startingStream,
                    StreamStatus.Error,
                    request.RequestedBy,
                    cancellationToken
                );

                return Result.Failure(
                    $"Timeout occurred while starting stream. Operation took longer than {request.TimeoutSeconds} seconds."
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audio stream: {StreamId}", request.StreamId);

            return Result.Failure($"Failed to start audio stream: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts the stream via the Snapcast service.
    /// </summary>
    /// <param name="request">The start stream command.</param>
    /// <param name="audioStream">The audio stream to start.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    private async Task<bool> StartStreamViaSnapcastAsync(
        StartAudioStreamCommand request,
        Core.Models.Entities.AudioStream audioStream,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Create a timeout cancellation token
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));

            // Get available groups if no specific group is requested
            var targetGroupId = request.GroupId;
            if (string.IsNullOrEmpty(targetGroupId))
            {
                var groups = await _snapcastService.GetGroupsAsync(timeoutCts.Token);
                targetGroupId = groups.FirstOrDefault();

                if (string.IsNullOrEmpty(targetGroupId))
                {
                    _logger.LogError("No available groups found in Snapcast server");
                    return false;
                }
            }

            // Assign the stream to the group
            var success = await _snapcastService.SetGroupStreamAsync(targetGroupId, audioStream.Id, timeoutCts.Token);

            if (success)
            {
                _logger.LogDebug(
                    "Successfully assigned stream {StreamId} to group {GroupId}",
                    audioStream.Id,
                    targetGroupId
                );
            }
            else
            {
                _logger.LogError(
                    "Failed to assign stream {StreamId} to group {GroupId}",
                    audioStream.Id,
                    targetGroupId
                );
            }

            return success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while starting stream {StreamId} via Snapcast", audioStream.Id);
            return false;
        }
    }

    /// <summary>
    /// Publishes an audio stream status change event.
    /// </summary>
    /// <param name="audioStream">The audio stream.</param>
    /// <param name="newStatus">The new status.</param>
    /// <param name="requestedBy">The user who requested the change.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task PublishStatusChangeEventAsync(
        Core.Models.Entities.AudioStream audioStream,
        StreamStatus newStatus,
        string requestedBy,
        CancellationToken cancellationToken
    )
    {
        var statusChangeEvent = AudioStreamStatusChangedEvent.Create(
            audioStream.Id,
            audioStream.Name,
            audioStream.Status,
            newStatus,
            audioStream.Url.ToString() ?? string.Empty,
            $"Stream start requested by {requestedBy}"
        );

        await _eventPublisher.PublishAsync(statusChangeEvent, cancellationToken);

        _logger.LogDebug(
            "Published AudioStreamStatusChangedEvent for stream: {StreamId}, Status: {PreviousStatus} -> {NewStatus}",
            audioStream.Id,
            audioStream.Status,
            newStatus
        );
    }
}
