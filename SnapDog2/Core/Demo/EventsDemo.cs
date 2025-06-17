using Microsoft.Extensions.Logging;
using SnapDog2.Core.Events;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Core.State;

namespace SnapDog2.Core.Demo;

/// <summary>
/// Demonstrates domain events publishing and handling capabilities.
/// Shows event creation, publishing, correlation, and async handling patterns.
/// </summary>
public class EventsDemo
{
    private readonly ILogger<EventsDemo> _logger;
    private readonly IEventPublisher _eventPublisher;
    private readonly IStateManager _stateManager;

    public EventsDemo(ILogger<EventsDemo> logger, IEventPublisher eventPublisher, IStateManager stateManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
    }

    /// <summary>
    /// Runs the complete events demonstration.
    /// </summary>
    public async Task RunDemoAsync()
    {
        _logger.LogInformation("=== Events Demo ===");

        try
        {
            await DemonstrateBasicEventPublishingAsync();
            await DemonstrateClientEventsAsync();
            await DemonstrateVolumeEventsAsync();
            await DemonstratePlaylistEventsAsync();
            await DemonstrateStreamEventsAsync();
            await DemonstrateZoneEventsAsync();
            await DemonstrateBatchEventPublishingAsync();
            await DemonstrateEventCorrelationAsync();

            _logger.LogInformation("Events demo completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Events demo failed");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates basic event publishing patterns.
    /// </summary>
    private async Task DemonstrateBasicEventPublishingAsync()
    {
        _logger.LogInformation("--- Basic Event Publishing Demo ---");

        // Create and publish a simple client connected event
        var clientConnectedEvent = ClientConnectedEvent.Create(
            clientId: "demo-client-1",
            clientName: "Demo Living Room Speaker",
            macAddress: new MacAddress("AA:BB:CC:DD:EE:01"),
            ipAddress: new IpAddress("192.168.1.101"),
            volume: 65,
            assignedZoneId: "living-room",
            isFirstConnection: true,
            clientVersion: "SnapCast 0.26.0"
        );

        _logger.LogInformation("Publishing ClientConnectedEvent for {ClientName}", clientConnectedEvent.ClientName);
        await _eventPublisher.PublishAsync(clientConnectedEvent);

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates client-related events (connect/disconnect).
    /// </summary>
    private async Task DemonstrateClientEventsAsync()
    {
        _logger.LogInformation("--- Client Events Demo ---");

        var correlationId = Guid.NewGuid().ToString();

        // Client connection sequence
        var connectEvent = ClientConnectedEvent.Create(
            clientId: "demo-client-2",
            clientName: "Demo Kitchen Speaker",
            macAddress: new MacAddress("BB:CC:DD:EE:FF:02"),
            ipAddress: new IpAddress("192.168.1.102"),
            volume: 50,
            assignedZoneId: "kitchen",
            correlationId: correlationId
        );

        await _eventPublisher.PublishAsync(connectEvent);
        _logger.LogInformation("Client {ClientName} connected", connectEvent.ClientName);

        await Task.Delay(500);

        // Client disconnection
        var disconnectEvent = ClientDisconnectedEvent.Create(
            clientId: "demo-client-2",
            clientName: "Demo Kitchen Speaker",
            macAddress: new MacAddress("BB:CC:DD:EE:FF:02"),
            ipAddress: new IpAddress("192.168.1.102"),
            disconnectionReason: "Network timeout",
            correlationId: correlationId
        );

        await _eventPublisher.PublishAsync(disconnectEvent);
        _logger.LogInformation(
            "Client {ClientName} disconnected: {Reason}",
            disconnectEvent.ClientName,
            disconnectEvent.DisconnectionReason
        );

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates volume change events.
    /// </summary>
    private async Task DemonstrateVolumeEventsAsync()
    {
        _logger.LogInformation("--- Volume Events Demo ---");

        var volumeEvents = new[]
        {
            VolumeChangedEvent.CreateForClient(
                "demo-client-1",
                "Demo Client 1",
                65,
                75,
                changeReason: "user-adjustment"
            ),
            VolumeChangedEvent.CreateForClient(
                "demo-client-1",
                "Demo Client 1",
                75,
                0,
                false,
                true,
                changeReason: "mute-button"
            ),
            VolumeChangedEvent.CreateForClient(
                "demo-client-1",
                "Demo Client 1",
                0,
                60,
                true,
                false,
                changeReason: "unmute-restore"
            ),
        };

        foreach (var volumeEvent in volumeEvents)
        {
            await _eventPublisher.PublishAsync(volumeEvent);
            _logger.LogInformation(
                "Volume changed for {ClientId}: {OldVolume} -> {NewVolume} (Muted: {OldMute} -> {NewMute})",
                volumeEvent.EntityId,
                volumeEvent.PreviousVolume,
                volumeEvent.NewVolume,
                volumeEvent.PreviousMuteStatus,
                volumeEvent.NewMuteStatus
            );
            await Task.Delay(200);
        }
    }

    /// <summary>
    /// Demonstrates playlist-related events.
    /// </summary>
    private async Task DemonstratePlaylistEventsAsync()
    {
        _logger.LogInformation("--- Playlist Events Demo ---");

        // Create a sample playlist update event
        var playlistEvent = PlaylistUpdatedEvent.Create(
            playlistId: "jazz-classics",
            playlistName: "Jazz Classics",
            updateType: PlaylistUpdateType.TracksAdded,
            trackIds: new[] { "track-take-five" },
            addedTrackIds: new[] { "track-take-five" }
        );

        await _eventPublisher.PublishAsync(playlistEvent);
        _logger.LogInformation(
            "Playlist {PlaylistName} updated: {UpdateType}, Added tracks: {AddedCount}",
            playlistEvent.PlaylistName,
            playlistEvent.UpdateType,
            playlistEvent.AddedTrackIds.Count
        );

        // Simulate multiple playlist operations
        var operations = new[]
        {
            (PlaylistUpdateType.TracksAdded, new[] { "track-blue-in-green" }, Array.Empty<string>()),
            (PlaylistUpdateType.TracksAdded, new[] { "track-round-midnight" }, Array.Empty<string>()),
            (PlaylistUpdateType.TracksReordered, Array.Empty<string>(), Array.Empty<string>()),
            (PlaylistUpdateType.TracksRemoved, Array.Empty<string>(), new[] { "track-blue-in-green" }),
        };

        foreach (var (updateType, addedTracks, removedTracks) in operations)
        {
            var evt = PlaylistUpdatedEvent.Create(
                playlistId: "jazz-classics",
                playlistName: "Jazz Classics",
                updateType: updateType,
                addedTrackIds: addedTracks,
                removedTrackIds: removedTracks
            );

            await _eventPublisher.PublishAsync(evt);
            _logger.LogInformation("Playlist operation: {UpdateType}", updateType);
            await Task.Delay(100);
        }
    }

    /// <summary>
    /// Demonstrates audio stream status events.
    /// </summary>
    private async Task DemonstrateStreamEventsAsync()
    {
        _logger.LogInformation("--- Stream Events Demo ---");

        var streamEvents = new[]
        {
            AudioStreamStatusChangedEvent.Create(
                "jazz-stream",
                "Jazz Radio",
                StreamStatus.Stopped,
                StreamStatus.Starting,
                reason: "user-play"
            ),
            AudioStreamStatusChangedEvent.Create(
                "jazz-stream",
                "Jazz Radio",
                StreamStatus.Starting,
                StreamStatus.Playing,
                reason: "stream-ready"
            ),
            AudioStreamStatusChangedEvent.Create(
                "jazz-stream",
                "Jazz Radio",
                StreamStatus.Playing,
                StreamStatus.Paused,
                reason: "user-pause"
            ),
            AudioStreamStatusChangedEvent.Create(
                "jazz-stream",
                "Jazz Radio",
                StreamStatus.Paused,
                StreamStatus.Playing,
                reason: "user-resume"
            ),
            AudioStreamStatusChangedEvent.Create(
                "jazz-stream",
                "Jazz Radio",
                StreamStatus.Playing,
                StreamStatus.Stopped,
                reason: "user-stop"
            ),
        };

        foreach (var streamEvent in streamEvents)
        {
            await _eventPublisher.PublishAsync(streamEvent);
            _logger.LogInformation(
                "Stream {StreamName}: {PreviousStatus} -> {NewStatus} ({Reason})",
                streamEvent.StreamName,
                streamEvent.PreviousStatus,
                streamEvent.NewStatus,
                streamEvent.Reason
            );
            await Task.Delay(300);
        }
    }

    /// <summary>
    /// Demonstrates zone configuration events.
    /// </summary>
    private async Task DemonstrateZoneEventsAsync()
    {
        _logger.LogInformation("--- Zone Events Demo ---");

        var zoneEvent = ZoneConfigurationChangedEvent.Create(
            zoneId: "living-room",
            zoneName: "Living Room",
            volumeSettingsChanged: true,
            newDefaultVolume: 65,
            newMinVolume: 10,
            newMaxVolume: 90,
            changeReason: "Volume settings updated",
            changedBy: "admin"
        );

        await _eventPublisher.PublishAsync(zoneEvent);
        _logger.LogInformation("Zone {ZoneName} configuration changed: Volume settings updated", zoneEvent.ZoneName);

        // Additional zone events
        var clientAssignmentEvent = ZoneConfigurationChangedEvent.Create(
            zoneId: "living-room",
            zoneName: "Living Room",
            clientIds: new[] { "demo-client-1" },
            addedClientIds: new[] { "demo-client-1" },
            changeReason: "Client assigned",
            changedBy: "system"
        );

        await _eventPublisher.PublishAsync(clientAssignmentEvent);
        _logger.LogInformation(
            "Zone client assignment: Added {ClientCount} clients",
            clientAssignmentEvent.AddedClientIds.Count
        );

        var streamAssignmentEvent = ZoneConfigurationChangedEvent.Create(
            zoneId: "living-room",
            zoneName: "Living Room",
            currentStreamId: "jazz-stream",
            changeReason: "Stream assigned",
            changedBy: "user"
        );

        await _eventPublisher.PublishAsync(streamAssignmentEvent);
        _logger.LogInformation("Zone stream assignment: {StreamId}", streamAssignmentEvent.CurrentStreamId);

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates batch event publishing.
    /// </summary>
    private async Task DemonstrateBatchEventPublishingAsync()
    {
        _logger.LogInformation("--- Batch Event Publishing Demo ---");

        var correlationId = Guid.NewGuid().ToString();
        var batchEvents = new List<IDomainEvent>
        {
            ClientConnectedEvent.Create(
                "batch-client-1",
                "Batch Client 1",
                new MacAddress("CC:DD:EE:FF:AA:01"),
                new IpAddress("192.168.1.201"),
                correlationId: correlationId
            ),
            ClientConnectedEvent.Create(
                "batch-client-2",
                "Batch Client 2",
                new MacAddress("CC:DD:EE:FF:AA:02"),
                new IpAddress("192.168.1.202"),
                correlationId: correlationId
            ),
            VolumeChangedEvent.CreateForClient(
                "batch-client-1",
                "Batch Client 1",
                50,
                70,
                changeReason: "batch-update",
                correlationId: correlationId
            ),
            VolumeChangedEvent.CreateForClient(
                "batch-client-2",
                "Batch Client 2",
                50,
                60,
                changeReason: "batch-update",
                correlationId: correlationId
            ),
        };

        _logger.LogInformation(
            "Publishing {EventCount} events in batch with correlation ID {CorrelationId}",
            batchEvents.Count,
            correlationId
        );

        await _eventPublisher.PublishAsync(batchEvents);
        _logger.LogInformation("Batch publishing completed");

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates event correlation and tracing.
    /// </summary>
    private async Task DemonstrateEventCorrelationAsync()
    {
        _logger.LogInformation("--- Event Correlation Demo ---");

        var correlationId = $"demo-workflow-{Guid.NewGuid():N}";
        _logger.LogInformation("Starting correlated workflow with ID: {CorrelationId}", correlationId);

        // Simulate a complete workflow with correlated events
        var workflowEvents = new List<(string Description, IDomainEvent Event)>
        {
            (
                "Client connects",
                ClientConnectedEvent.Create(
                    "workflow-client",
                    "Workflow Client",
                    new MacAddress("DD:EE:FF:AA:BB:01"),
                    new IpAddress("192.168.1.201"),
                    correlationId: correlationId
                )
            ),
            (
                "Volume adjusted",
                VolumeChangedEvent.CreateForClient(
                    "workflow-client",
                    "Workflow Client",
                    50,
                    75,
                    changeReason: "initial-setup",
                    correlationId: correlationId
                )
            ),
            (
                "Stream starts",
                AudioStreamStatusChangedEvent.Create(
                    "workflow-stream",
                    "Workflow Stream",
                    StreamStatus.Stopped,
                    StreamStatus.Playing,
                    reason: "workflow-start",
                    correlationId: correlationId
                )
            ),
            (
                "Zone updated",
                ZoneConfigurationChangedEvent.Create(
                    "workflow-zone",
                    "Workflow Zone",
                    clientIds: new[] { "workflow-client" },
                    addedClientIds: new[] { "workflow-client" },
                    changeReason: "Client assigned to zone",
                    changedBy: "workflow",
                    correlationId: correlationId
                )
            ),
        };

        foreach (var (description, domainEvent) in workflowEvents)
        {
            _logger.LogInformation("Workflow step: {Description}", description);
            await _eventPublisher.PublishAndWaitAsync(domainEvent);
            await Task.Delay(200);
        }

        _logger.LogInformation("Correlated workflow completed for {CorrelationId}", correlationId);
    }
}
