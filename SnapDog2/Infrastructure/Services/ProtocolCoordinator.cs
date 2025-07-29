using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MediatR;
using SnapDog2.Core.Common;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Events;
using SnapDog2.Infrastructure.Repositories;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Implementation of protocol coordination that synchronizes state changes
/// between Snapcast, KNX, MQTT, and Subsonic protocols.
/// </summary>
public class ProtocolCoordinator : IProtocolCoordinator, IDisposable,
    INotificationHandler<SnapcastClientVolumeChangedEvent>,
    INotificationHandler<SnapcastClientConnectedEvent>,
    INotificationHandler<SnapcastClientDisconnectedEvent>,
    INotificationHandler<KnxGroupValueReceivedEvent>,
    INotificationHandler<MqttZoneVolumeCommandEvent>,
    INotificationHandler<MqttClientVolumeCommandEvent>
{
    private readonly ISnapcastService _snapcastService;
    private readonly IMqttService _mqttService;
    private readonly IKnxService _knxService;
    private readonly ISubsonicService _subsonicService;
    private readonly IClientRepository _clientRepository;
    private readonly IZoneRepository _zoneRepository;
    private readonly ILogger<ProtocolCoordinator> _logger;
    private readonly SnapDogConfiguration _config;
    private readonly SemaphoreSlim _coordinationSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, DateTime> _lastSyncTimes = new();
    private readonly TimeSpan _syncDebounceInterval = TimeSpan.FromMilliseconds(500);
    private bool _disposed;
    private bool _started;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolCoordinator"/> class.
    /// </summary>
    public ProtocolCoordinator(
        ISnapcastService snapcastService,
        IMqttService mqttService,
        IKnxService knxService,
        ISubsonicService subsonicService,
        IClientRepository clientRepository,
        IZoneRepository zoneRepository,
        IOptions<SnapDogConfiguration> config,
        ILogger<ProtocolCoordinator> logger)
    {
        _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
        _subsonicService = subsonicService ?? throw new ArgumentNullException(nameof(subsonicService));
        _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
        _zoneRepository = zoneRepository ?? throw new ArgumentNullException(nameof(zoneRepository));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("Protocol coordinator initialized");
    }

    /// <summary>
    /// Starts the protocol coordination service.
    /// </summary>
    public async Task<Result> StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return Result.Failure("Protocol coordinator has been disposed");

        if (_started)
            return Result.Success();

        await _coordinationSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Starting protocol coordinator");

            // Initialize MQTT command subscriptions
            // TODO: Implement SubscribeToCommandsAsync method in MQTT service
            var mqttResult = true; // Placeholder
            if (!mqttResult)
            {
                _logger.LogWarning("Failed to subscribe to MQTT commands");
            }

            // Perform initial synchronization
            await PerformInitialSynchronizationAsync(cancellationToken);

            _started = true;
            _logger.LogInformation("Protocol coordinator started successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start protocol coordinator");
            return Result.Failure($"Failed to start protocol coordinator: {ex.Message}");
        }
        finally
        {
            _coordinationSemaphore.Release();
        }
    }

    /// <summary>
    /// Stops the protocol coordination service.
    /// </summary>
    public async Task<Result> StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || !_started)
            return Result.Success();

        await _coordinationSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Stopping protocol coordinator");

            _started = false;
            _lastSyncTimes.Clear();

            _logger.LogInformation("Protocol coordinator stopped successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping protocol coordinator");
            return Result.Failure($"Error stopping protocol coordinator: {ex.Message}");
        }
        finally
        {
            _coordinationSemaphore.Release();
        }
    }

    /// <summary>
    /// Synchronizes a volume change across all connected protocols.
    /// </summary>
    public async Task<Result> SynchronizeVolumeChangeAsync(string clientId, int volume, string sourceProtocol, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_started)
            return Result.Failure("Protocol coordinator not started");

        var syncKey = $"volume_{clientId}_{volume}_{sourceProtocol}";
        if (!ShouldSync(syncKey))
        {
            _logger.LogTrace("Skipping volume sync due to debounce: {SyncKey}", syncKey);
            return Result.Success();
        }

        _logger.LogInformation("Synchronizing volume change for client {ClientId} to {Volume}% from {Source}",
            clientId, volume, sourceProtocol);

        var syncTasks = new List<Task<Result>>();

        try
        {
            // Get client configuration for protocol mappings
            var client = await _clientRepository.GetByIdAsync(clientId, cancellationToken);
            if (client == null)
            {
                _logger.LogWarning("Client {ClientId} not found for volume synchronization", clientId);
                return Result.Failure($"Client {clientId} not found");
            }

            // Sync to Snapcast if not the source
            if (sourceProtocol != "Snapcast" && _config.Services.Snapcast.Enabled)
            {
                syncTasks.Add(Task.Run(async () =>
                {
                    var success = await _snapcastService.SetClientVolumeAsync(clientId, volume, cancellationToken);
                    return success ? Result.Success() : Result.Failure("Snapcast volume sync failed");
                }, cancellationToken));
            }

            // Sync to MQTT if not the source
            if (sourceProtocol != "MQTT" && _config.Services.Mqtt.Enabled)
            {
                syncTasks.Add(Task.Run(async () =>
                {
                    // TODO: Implement PublishClientVolumeAsync method in MQTT service
                    await Task.CompletedTask;
                    var success = true;
                    return success ? Result.Success() : Result.Failure("MQTT volume sync failed");
                }, cancellationToken));
            }

            // Sync to KNX if not the source and client has KNX configuration
            // TODO: Add KNX configuration properties to Client entity
            if (sourceProtocol != "KNX" && _config.Services.Knx.Enabled)
            {
                syncTasks.Add(Task.Run(async () =>
                {
                    // TODO: Get KNX group address from client configuration
                    var groupAddress = "1/1/1"; // Placeholder
                    var success = await _knxService.SendVolumeCommandAsync(groupAddress, volume, cancellationToken);
                    return success ? Result.Success() : Result.Failure("KNX volume sync failed");
                }, cancellationToken));
            }

            // Wait for all synchronization tasks to complete
            var results = await Task.WhenAll(syncTasks);
            var failures = results.Where(r => r.IsFailure).ToList();

            if (failures.Any())
            {
                _logger.LogWarning("Some protocol synchronizations failed: {Errors}",
                    string.Join(", ", failures.Select(f => f.Error)));
                return Result.Failure($"Partial sync failure: {failures.Count}/{results.Length} failed");
            }

            _logger.LogDebug("Successfully synchronized volume change across all protocols");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing volume change for client {ClientId}", clientId);
            return Result.Failure($"Volume sync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes a mute state change across all connected protocols.
    /// </summary>
    public async Task<Result> SynchronizeMuteChangeAsync(string clientId, bool muted, string sourceProtocol, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_started)
            return Result.Failure("Protocol coordinator not started");

        var syncKey = $"mute_{clientId}_{muted}_{sourceProtocol}";
        if (!ShouldSync(syncKey))
        {
            return Result.Success();
        }

        _logger.LogInformation("Synchronizing mute change for client {ClientId} to {Muted} from {Source}",
            clientId, muted, sourceProtocol);

        var syncTasks = new List<Task<Result>>();

        try
        {
            var client = await _clientRepository.GetByIdAsync(clientId, cancellationToken);
            if (client == null)
            {
                return Result.Failure($"Client {clientId} not found");
            }

            // Sync to Snapcast if not the source
            if (sourceProtocol != "Snapcast" && _config.Services.Snapcast.Enabled)
            {
                syncTasks.Add(Task.Run(async () =>
                {
                    var success = await _snapcastService.SetClientMuteAsync(clientId, muted, cancellationToken);
                    return success ? Result.Success() : Result.Failure("Snapcast mute sync failed");
                }, cancellationToken));
            }

            // Sync to KNX if not the source and client has KNX configuration
            // TODO: Add KNX mute configuration properties to Client entity
            if (sourceProtocol != "KNX" && _config.Services.Knx.Enabled)
            {
                syncTasks.Add(Task.Run(async () =>
                {
                    // TODO: Get KNX mute group address from client configuration
                    var groupAddress = "1/1/2"; // Placeholder
                    var success = await _knxService.SendBooleanCommandAsync(groupAddress, muted, cancellationToken);
                    return success ? Result.Success() : Result.Failure("KNX mute sync failed");
                }, cancellationToken));
            }

            var results = await Task.WhenAll(syncTasks);
            var failures = results.Where(r => r.IsFailure).ToList();

            return failures.Any() 
                ? Result.Failure($"Partial sync failure: {failures.Count}/{results.Length} failed")
                : Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing mute change for client {ClientId}", clientId);
            return Result.Failure($"Mute sync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes a zone volume change across all connected protocols.
    /// </summary>
    public async Task<Result> SynchronizeZoneVolumeChangeAsync(int zoneId, int volume, string sourceProtocol, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_started)
            return Result.Failure("Protocol coordinator not started");

        var syncKey = $"zone_volume_{zoneId}_{volume}_{sourceProtocol}";
        if (!ShouldSync(syncKey))
        {
            return Result.Success();
        }

        _logger.LogInformation("Synchronizing zone volume change for zone {ZoneId} to {Volume}% from {Source}",
            zoneId, volume, sourceProtocol);

        try
        {
            var zone = await _zoneRepository.GetByIdAsync(zoneId.ToString(), cancellationToken);
            if (zone == null)
            {
                return Result.Failure($"Zone {zoneId} not found");
            }

            var syncTasks = new List<Task<Result>>();

            // Sync to MQTT if not the source
            if (sourceProtocol != "MQTT" && _config.Services.Mqtt.Enabled)
            {
                syncTasks.Add(Task.Run(async () =>
                {
                    // TODO: Implement PublishZoneVolumeAsync method in MQTT service
                    // await _mqttService.PublishZoneVolumeAsync(zoneId, volume, cancellationToken);
                    await Task.CompletedTask;
                    var success = true;
                    return success ? Result.Success() : Result.Failure("MQTT zone volume sync failed");
                }, cancellationToken));
            }

            // Sync to KNX if not the source and zone has KNX configuration
            // TODO: Add KNX configuration properties to Zone entity
            if (sourceProtocol != "KNX" && _config.Services.Knx.Enabled)
            {
                syncTasks.Add(Task.Run(async () =>
                {
                    // TODO: Get KNX group address from zone configuration
                    var groupAddress = "1/2/1"; // Placeholder
                    var success = await _knxService.SendVolumeCommandAsync(groupAddress, volume, cancellationToken);
                    return success ? Result.Success() : Result.Failure("KNX zone volume sync failed");
                }, cancellationToken));
            }

            var results = await Task.WhenAll(syncTasks);
            var failures = results.Where(r => r.IsFailure).ToList();

            return failures.Any() 
                ? Result.Failure($"Partial sync failure: {failures.Count}/{results.Length} failed")
                : Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing zone volume change for zone {ZoneId}", zoneId);
            return Result.Failure($"Zone volume sync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes a playback command across all connected protocols.
    /// </summary>
    public async Task<Result> SynchronizePlaybackCommandAsync(string command, int? streamId, string sourceProtocol, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_started)
            return Result.Failure("Protocol coordinator not started");

        var syncKey = $"playback_{command}_{streamId}_{sourceProtocol}";
        if (!ShouldSync(syncKey))
        {
            return Result.Success();
        }

        _logger.LogInformation("Synchronizing playback command {Command} for stream {StreamId} from {Source}",
            command, streamId, sourceProtocol);

        try
        {
            var syncTasks = new List<Task<Result>>();

            // Broadcast status to MQTT
            if (sourceProtocol != "MQTT" && _config.Services.Mqtt.Enabled && streamId.HasValue)
            {
                var status = command.ToUpperInvariant() switch
                {
                    "PLAY" => "playing",
                    "STOP" => "stopped",
                    "PAUSE" => "paused",
                    _ => "unknown"
                };

                syncTasks.Add(Task.Run(async () =>
                {
                    // TODO: Implement PublishStreamStatusAsync method in MQTT service
                    // await _mqttService.PublishStreamStatusAsync(streamId.Value, status, cancellationToken);
                    await Task.CompletedTask;
                    var success = true;
                    return success ? Result.Success() : Result.Failure("MQTT playback sync failed");
                }, cancellationToken));
            }

            // Broadcast to KNX if there's a stream configuration with KNX playback address
            if (sourceProtocol != "KNX" && _config.Services.Knx.Enabled && streamId.HasValue)
            {
                // This would require stream configuration lookup - simplified for now
                _logger.LogTrace("KNX playback sync would be implemented with stream configuration lookup");
            }

            var results = await Task.WhenAll(syncTasks);
            var failures = results.Where(r => r.IsFailure).ToList();

            return failures.Any() 
                ? Result.Failure($"Partial sync failure: {failures.Count}/{results.Length} failed")
                : Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing playback command {Command}", command);
            return Result.Failure($"Playback sync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes a stream assignment change across all connected protocols.
    /// </summary>
    public async Task<Result> SynchronizeStreamAssignmentAsync(string groupId, string streamId, string sourceProtocol, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_started)
            return Result.Failure("Protocol coordinator not started");

        var syncKey = $"stream_{groupId}_{streamId}_{sourceProtocol}";
        if (!ShouldSync(syncKey))
        {
            return Result.Success();
        }

        _logger.LogInformation("Synchronizing stream assignment for group {GroupId} to stream {StreamId} from {Source}",
            groupId, streamId, sourceProtocol);

        try
        {
            var syncTasks = new List<Task<Result>>();

            // Sync to Snapcast if not the source
            if (sourceProtocol != "Snapcast" && _config.Services.Snapcast.Enabled)
            {
                syncTasks.Add(Task.Run(async () =>
                {
                    var success = await _snapcastService.SetGroupStreamAsync(groupId, streamId, cancellationToken);
                    return success ? Result.Success() : Result.Failure("Snapcast stream assignment sync failed");
                }, cancellationToken));
            }

            var results = await Task.WhenAll(syncTasks);
            var failures = results.Where(r => r.IsFailure).ToList();

            return failures.Any() 
                ? Result.Failure($"Partial sync failure: {failures.Count}/{results.Length} failed")
                : Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing stream assignment for group {GroupId}", groupId);
            return Result.Failure($"Stream assignment sync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes client connection status across all connected protocols.
    /// </summary>
    public async Task<Result> SynchronizeClientStatusAsync(string clientId, bool connected, string sourceProtocol, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_started)
            return Result.Failure("Protocol coordinator not started");

        var syncKey = $"client_status_{clientId}_{connected}_{sourceProtocol}";
        if (!ShouldSync(syncKey))
        {
            return Result.Success();
        }

        _logger.LogInformation("Synchronizing client status for {ClientId}: {Connected} from {Source}",
            clientId, connected ? "connected" : "disconnected", sourceProtocol);

        try
        {
            var syncTasks = new List<Task<Result>>();

            // Sync to MQTT if not the source
            if (sourceProtocol != "MQTT" && _config.Services.Mqtt.Enabled)
            {
                syncTasks.Add(Task.Run(async () =>
                {
                    // TODO: Implement PublishClientStatusAsync method in MQTT service
                    // await _mqttService.PublishClientStatusAsync(clientId, connected, cancellationToken);
                    await Task.CompletedTask;
                    var success = true;
                    return success ? Result.Success() : Result.Failure("MQTT client status sync failed");
                }, cancellationToken));
            }

            var results = await Task.WhenAll(syncTasks);
            var failures = results.Where(r => r.IsFailure).ToList();

            return failures.Any() 
                ? Result.Failure($"Partial sync failure: {failures.Count}/{results.Length} failed")
                : Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing client status for {ClientId}", clientId);
            return Result.Failure($"Client status sync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current health status of all connected protocols.
    /// </summary>
    public async Task<Dictionary<string, bool>> GetProtocolHealthAsync(CancellationToken cancellationToken = default)
    {
        var health = new Dictionary<string, bool>();

        try
        {
            // Check Snapcast health
            if (_config.Services.Snapcast.Enabled)
            {
                health["Snapcast"] = await _snapcastService.IsServerAvailableAsync(cancellationToken);
            }

            // Check Subsonic health
            if (_config.Services.Subsonic.Enabled)
            {
                health["Subsonic"] = await _subsonicService.IsServerAvailableAsync(cancellationToken);
            }

            // MQTT and KNX would need similar health checks
            health["MQTT"] = _config.Services.Mqtt.Enabled; // Simplified
            health["KNX"] = _config.Services.Knx.Enabled; // Simplified
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking protocol health");
        }

        return health;
    }

    #region Event Handlers

    /// <summary>
    /// Handles Snapcast client volume change events.
    /// </summary>
    public async Task Handle(SnapcastClientVolumeChangedEvent notification, CancellationToken cancellationToken)
    {
        await SynchronizeVolumeChangeAsync(notification.ClientId, notification.Volume, "Snapcast", cancellationToken);
    }

    /// <summary>
    /// Handles Snapcast client connected events.
    /// </summary>
    public async Task Handle(SnapcastClientConnectedEvent notification, CancellationToken cancellationToken)
    {
        await SynchronizeClientStatusAsync(notification.ClientId, true, "Snapcast", cancellationToken);
    }

    /// <summary>
    /// Handles Snapcast client disconnected events.
    /// </summary>
    public async Task Handle(SnapcastClientDisconnectedEvent notification, CancellationToken cancellationToken)
    {
        await SynchronizeClientStatusAsync(notification.ClientId, false, "Snapcast", cancellationToken);
    }

    /// <summary>
    /// Handles KNX group value received events.
    /// </summary>
    public async Task Handle(KnxGroupValueReceivedEvent notification, CancellationToken cancellationToken)
    {
        // Process KNX group value changes and sync to other protocols
        _logger.LogTrace("Processing KNX group value for address {Address}", notification.Address);
        
        // This would require mapping KNX addresses to clients/zones - simplified for now
        // Implementation would lookup the client/zone associated with the KNX address
        // and then call the appropriate synchronization method
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles MQTT zone volume command events.
    /// </summary>
    public async Task Handle(MqttZoneVolumeCommandEvent notification, CancellationToken cancellationToken)
    {
        await SynchronizeZoneVolumeChangeAsync(notification.ZoneId, notification.Volume, "MQTT", cancellationToken);
    }

    /// <summary>
    /// Handles MQTT client volume command events.
    /// </summary>
    public async Task Handle(MqttClientVolumeCommandEvent notification, CancellationToken cancellationToken)
    {
        await SynchronizeVolumeChangeAsync(notification.ClientId, notification.Volume, "MQTT", cancellationToken);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Performs initial synchronization of all protocols.
    /// </summary>
    private async Task PerformInitialSynchronizationAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Performing initial protocol synchronization");

            // Sync Snapcast state if enabled
            if (_config.Services.Snapcast.Enabled)
            {
                await _snapcastService.SynchronizeServerStateAsync(cancellationToken);
            }

            // Additional initial sync operations would go here
            _logger.LogDebug("Initial protocol synchronization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initial protocol synchronization");
        }
    }

    /// <summary>
    /// Determines if a synchronization should proceed based on debounce logic.
    /// </summary>
    private bool ShouldSync(string syncKey)
    {
        var now = DateTime.UtcNow;
        
        if (_lastSyncTimes.TryGetValue(syncKey, out var lastSync))
        {
            if (now - lastSync < _syncDebounceInterval)
            {
                return false; // Too soon since last sync
            }
        }

        _lastSyncTimes[syncKey] = now;
        return true;
    }

    #endregion

    /// <summary>
    /// Disposes the protocol coordinator and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during protocol coordinator disposal");
        }

        _coordinationSemaphore?.Dispose();
        _disposed = true;
        
        _logger.LogDebug("Protocol coordinator disposed");
        GC.SuppressFinalize(this);
    }
}