# Phase 5: Integration & Protocols

## Overview

Phase 5 integrates all external protocols and services including Snapcast, KNX, MQTT, and Subsonic. This phase connects SnapDog to the complete multi-room audio ecosystem.

**Deliverable**: Multi-protocol system with real external integrations and comprehensive protocol handling.

## Objectives

### Primary Goals

- [ ] Complete Snapcast server integration with RPC communication
- [ ] Implement KNX protocol with DPT mappings and group addresses
- [ ] Build MQTT command framework with topic management
- [ ] Integrate Subsonic API for music streaming
- [ ] Create protocol coordination and synchronization
- [ ] Implement real-time event processing

### Success Criteria

- All protocols working with real external systems
- Command framework operational across protocols
- Real-time synchronization between systems
- Error handling and reconnection logic working
- Protocol integration tests passing
- Performance meets real-time requirements

## Implementation Steps

### Step 1: Snapcast Integration

#### 1.1 Enhanced Snapcast Service

```csharp
namespace SnapDog.Infrastructure.Services;

public class SnapcastService : ISnapcastService
{
    private readonly ISnapcastClient _client;
    private readonly ILogger<SnapcastService> _logger;
    private readonly SemaphoreSlim _connectionSemaphore;
    private bool _isConnected;

    public async Task<Result> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isConnected) return Result.Success();

            await _client.ConnectAsync(cancellationToken);
            _isConnected = true;

            // Subscribe to server events
            _client.OnClientVolumeChanged += OnClientVolumeChanged;
            _client.OnClientConnected += OnClientConnected;
            _client.OnGroupStreamChanged += OnGroupStreamChanged;

            _logger.LogInformation("Connected to Snapcast server successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Snapcast server");
            return Result.Failure($"Connection failed: {ex.Message}");
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<Result> SynchronizeServerStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get server status
            var serverStatus = await _client.GetServerStatusAsync(cancellationToken);

            // Get all groups and clients
            var groups = await _client.GetGroupsAsync(cancellationToken);
            var clients = await _client.GetClientsAsync(cancellationToken);

            // Publish synchronization events
            await _mediator.Publish(new SnapcastStateSynchronizedEvent(
                serverStatus, groups, clients), cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synchronize Snapcast server state");
            return Result.Failure($"Sync failed: {ex.Message}");
        }
    }

    private async void OnClientVolumeChanged(object sender, ClientVolumeChangedEventArgs e)
    {
        try
        {
            await _mediator.Publish(new SnapcastClientVolumeChangedEvent(
                e.ClientId, e.Volume, e.Muted));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client volume change event");
        }
    }
}
```

### Step 2: KNX Protocol Implementation

#### 2.1 KNX Service with DPT Handling

```csharp
namespace SnapDog.Infrastructure.Services;

public class KnxService : IKnxService
{
    private readonly IKnxConnection _connection;
    private readonly KnxConfiguration _config;
    private readonly ILogger<KnxService> _logger;
    private readonly Dictionary<string, Func<byte[], Task>> _groupSubscriptions;

    public async Task<Result> WriteGroupValueAsync(string groupAddress, byte[] value, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Writing to KNX group address {GroupAddress}: {Value}",
                groupAddress, Convert.ToHexString(value));

            await _connection.Action(groupAddress, value, cancellationToken);

            _logger.LogInformation("Successfully wrote to KNX group address {GroupAddress}", groupAddress);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to KNX group address {GroupAddress}", groupAddress);
            return Result.Failure($"KNX write failed: {ex.Message}");
        }
    }

    public async Task<Result> SendVolumeCommand(string groupAddress, int volume, CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert volume (0-100) to DPT 5.001 (0-255)
            var knxValue = (byte)Math.Clamp(Math.Round(volume * 2.55), 0, 255);

            return await WriteGroupValueAsync(groupAddress, new[] { knxValue }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send volume command to {GroupAddress}", groupAddress);
            return Result.Failure($"Volume command failed: {ex.Message}");
        }
    }

    public async Task<Result> SendPlaybackCommand(string groupAddress, bool playing, CancellationToken cancellationToken = default)
    {
        try
        {
            // DPT 1.001 - Boolean
            var knxValue = playing ? (byte)1 : (byte)0;

            return await WriteGroupValueAsync(groupAddress, new[] { knxValue }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send playback command to {GroupAddress}", groupAddress);
            return Result.Failure($"Playback command failed: {ex.Message}");
        }
    }
}
```

### Step 3: MQTT Command Framework

#### 3.1 MQTT Service Implementation

```csharp
namespace SnapDog.Infrastructure.Services;

public class MqttService : IMqttService
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttConfiguration _config;
    private readonly ILogger<MqttService> _logger;
    private readonly ConcurrentDictionary<string, List<Func<string, string, Task>>> _topicHandlers;

    public async Task<Result> PublishStreamStatusAsync(int streamId, string status, CancellationToken cancellationToken = default)
    {
        var topic = $"{_config.BaseTopic}/STREAM/{streamId}/STATUS";
        var payload = JsonSerializer.Serialize(new
        {
            streamId,
            status,
            timestamp = DateTime.UtcNow
        });

        return await PublishAsync(topic, payload, retain: true, cancellationToken);
    }

    public async Task<Result> PublishZoneVolumeAsync(int zoneId, int volume, CancellationToken cancellationToken = default)
    {
        var topic = $"{_config.BaseTopic}/ZONE/{zoneId}/VOLUME";
        var payload = volume.ToString();

        return await PublishAsync(topic, payload, retain: true, cancellationToken);
    }

    public async Task<Result> SubscribeToCommandsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Subscribe to all command topics
            var commandTopics = new[]
            {
                $"{_config.BaseTopic}/+/COMMAND/+",
                $"{_config.BaseTopic}/ZONE/+/+",
                $"{_config.BaseTopic}/CLIENT/+/+"
            };

            foreach (var topic in commandTopics)
            {
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build(), cancellationToken);

                _logger.LogInformation("Subscribed to MQTT topic: {Topic}", topic);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to MQTT command topics");
            return Result.Failure($"MQTT subscription failed: {ex.Message}");
        }
    }

    private async Task HandleMqttMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            _logger.LogDebug("Received MQTT message on {Topic}: {Payload}", topic, payload);

            // Parse topic and route to appropriate handler
            if (topic.Contains("/ZONE/") && topic.EndsWith("/VOLUME"))
            {
                await HandleZoneVolumeCommand(topic, payload);
            }
            else if (topic.Contains("/CLIENT/") && topic.EndsWith("/VOLUME"))
            {
                await HandleClientVolumeCommand(topic, payload);
            }
            else if (topic.Contains("/COMMAND/"))
            {
                await HandleGeneralCommand(topic, payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MQTT message on topic {Topic}", e.ApplicationMessage.Topic);
        }
    }
}
```

### Step 4: Subsonic Integration

#### 4.1 Subsonic Service

```csharp
namespace SnapDog.Infrastructure.Services;

public class SubsonicService : ISubsonicService
{
    private readonly HttpClient _httpClient;
    private readonly SubsonicConfiguration _config;
    private readonly ILogger<SubsonicService> _logger;

    public async Task<Result<IEnumerable<Playlist>>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/rest/getPlaylists?u={_config.Username}&p={_config.Password}&v=1.16.1&c=SnapDog&f=json",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var subsonicResponse = JsonSerializer.Deserialize<SubsonicResponse<PlaylistsResult>>(content);

                if (subsonicResponse?.SubsonicResponse?.Playlists?.Playlist != null)
                {
                    var playlists = subsonicResponse.SubsonicResponse.Playlists.Playlist
                        .Select(ConvertToPlaylist);

                    return Result<IEnumerable<Playlist>>.Success(playlists);
                }
            }

            return Result<IEnumerable<Playlist>>.Failure("Failed to retrieve playlists from Subsonic");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving playlists from Subsonic");
            return Result<IEnumerable<Playlist>>.Failure($"Subsonic error: {ex.Message}");
        }
    }

    public async Task<Result<Stream>> GetTrackStreamAsync(string trackId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/rest/stream?id={trackId}&u={_config.Username}&p={_config.Password}&v=1.16.1&c=SnapDog",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return Result<Stream>.Success(stream);
            }

            return Result<Stream>.Failure($"Failed to stream track {trackId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming track {TrackId}", trackId);
            return Result<Stream>.Failure($"Stream error: {ex.Message}");
        }
    }
}
```

### Step 5: Protocol Coordination

#### 5.1 Protocol Coordinator

```csharp
namespace SnapDog.Server.Services;

public class ProtocolCoordinator : IProtocolCoordinator
{
    private readonly ISnapcastService _snapcastService;
    private readonly IMqttService _mqttService;
    private readonly IKnxService _knxService;
    private readonly IMediator _mediator;
    private readonly ILogger<ProtocolCoordinator> _logger;

    public async Task<Result> SynchronizeVolumeChangeAsync(int clientId, int volume, string source, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Synchronizing volume change for client {ClientId} to {Volume} from {Source}",
            clientId, volume, source);

        var tasks = new List<Task<Result>>();

        // Update Snapcast if not the source
        if (source != "Snapcast")
        {
            tasks.Add(_snapcastService.SetClientVolumeAsync(clientId.ToString(), volume, cancellationToken));
        }

        // Update MQTT if not the source
        if (source != "MQTT")
        {
            tasks.Add(_mqttService.PublishClientVolumeAsync(clientId, volume, cancellationToken));
        }

        // Update KNX if not the source and client has KNX configuration
        if (source != "KNX")
        {
            var client = await GetClientConfigurationAsync(clientId, cancellationToken);
            if (client != null && !string.IsNullOrEmpty(client.KnxVolumeGroupAddress))
            {
                tasks.Add(_knxService.SendVolumeCommand(client.KnxVolumeGroupAddress, volume, cancellationToken));
            }
        }

        // Execute all synchronization tasks
        var results = await Task.WhenAll(tasks);

        var failures = results.Where(r => r.IsFailure).ToList();
        if (failures.Any())
        {
            _logger.LogWarning("Some protocol synchronizations failed: {Errors}",
                string.Join(", ", failures.Select(f => f.Error)));

            return Result.Failure($"Partial sync failure: {failures.Count}/{results.Length} failed");
        }

        _logger.LogInformation("Successfully synchronized volume change across all protocols");
        return Result.Success();
    }

    public async Task<Result> HandlePlaybackCommandAsync(string command, int? streamId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling playback command: {Command} for stream {StreamId}", command, streamId);

        try
        {
            // Execute command in business layer
            var mediatrResult = command.ToUpperInvariant() switch
            {
                "PLAY" when streamId.HasValue => await _mediator.Send(new StartAudioStreamCommand(streamId.Value, "Protocol"), cancellationToken),
                "STOP" when streamId.HasValue => await _mediator.Send(new StopAudioStreamCommand(streamId.Value, "Protocol"), cancellationToken),
                "PAUSE" when streamId.HasValue => await _mediator.Send(new PauseAudioStreamCommand(streamId.Value, "Protocol"), cancellationToken),
                _ => Result.Failure($"Unknown command: {command}")
            };

            if (mediatrResult.IsFailure)
            {
                return mediatrResult;
            }

            // Broadcast status to all protocols
            var broadcastTasks = new List<Task>();

            if (streamId.HasValue)
            {
                var status = command.ToUpperInvariant() == "PLAY" ? "active" : "stopped";

                broadcastTasks.Add(_mqttService.PublishStreamStatusAsync(streamId.Value, status, cancellationToken).AsTask());

                // Broadcast to KNX if configured
                var streamConfig = await GetStreamConfigurationAsync(streamId.Value, cancellationToken);
                if (streamConfig?.KnxPlaybackGroupAddress != null)
                {
                    var playing = status == "active";
                    broadcastTasks.Add(_knxService.SendPlaybackCommand(streamConfig.KnxPlaybackGroupAddress, playing, cancellationToken).AsTask());
                }
            }

            await Task.WhenAll(broadcastTasks);

            _logger.LogInformation("Successfully handled playback command: {Command}", command);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback command: {Command}", command);
            return Result.Failure($"Command handling failed: {ex.Message}");
        }
    }
}
```

## Expected Deliverable

### Multi-Protocol Integration Status

```
SnapDog Protocol Integration Status
=================================
🟢 Snapcast Server    - Connected (localhost:1705)
   ├── Groups: 3      - Living Room, Kitchen, Bedroom
   ├── Clients: 5     - All synchronized
   └── Streams: 2     - Active and monitored

🟢 MQTT Broker       - Connected (localhost:1883)
   ├── Topics: 15     - All subscribed
   ├── Commands: ✅   - Volume, playback working
   └── Status: ✅     - Real-time updates

🟢 KNX Gateway       - Connected (192.168.1.100:3671)
   ├── Group Addr: 12 - All configured
   ├── DPT Mapping: ✅ - Volume, status, commands
   └── Events: ✅     - Real-time processing

🟢 Subsonic Server   - Connected (music.local:4040)
   ├── Playlists: 25  - All synchronized
   ├── Tracks: 5,431  - Available for streaming
   └── Auth: ✅       - API access working

Protocol Coordination: ✅ All systems synchronized
Real-time Events: ✅ Sub-second response times
Error Handling: ✅ Graceful degradation
```

### Test Results

```
Phase 5 Test Results:
===================
Snapcast Integration: 35/35 passed
KNX Protocol Tests: 28/28 passed
MQTT Framework Tests: 30/30 passed
Subsonic Integration: 20/20 passed
Protocol Coordination: 25/25 passed
End-to-End Protocol Tests: 15/15 passed

Total Tests: 153/153 passed
Code Coverage: 91%
Integration Success: 100%
```

## Quality Gates

- [ ] All external protocols working with real systems
- [ ] Command framework operational across all protocols
- [ ] Real-time synchronization under 1 second
- [ ] Error handling and reconnection working
- [ ] Protocol integration tests passing
- [ ] Performance meets real-time requirements

## Next Steps

Phase 5 completes the protocol integration, connecting SnapDog to the complete multi-room audio ecosystem. Proceed to Phase 6 for production observability.
