# 10. Infrastructure Services Implementation

This chapter details the concrete implementations of the infrastructure services responsible for bridging the gap between SnapDog2's core application logic (`/Server` layer) and the external world (libraries, protocols, servers). These services reside within the `/Infrastructure` folder and implement the abstractions defined in `/Core/Abstractions`. They encapsulate the complexities of external interactions, apply resilience patterns, handle protocol-specific details, and integrate with the application's logging, configuration, and state management systems.

## 10.1. Snapcast Integration (`/Infrastructure/Snapcast/`)

This component handles all direct communication with the Snapcast server.

### 10.1.1. `SnapcastService`

* **Implements:** `SnapDog2.Core.Abstractions.ISnapcastService`
* **Purpose:** Manages the connection to the Snapcast server's control port, wraps the underlying library calls, handles server events, and keeps the raw state repository updated.
* **Key Library:** `SnapcastClient` (v0.3.1) - Uses `SnapcastClient` for communication.
* **Dependencies:** `IOptions<SnapcastOptions>`, `IMediator`, `ISnapcastStateRepository`, `ILogger<SnapcastService>`.
* **Core Logic:**
  * **Connection Management:** Implements `InitializeAsync` to establish a connection using `SnapcastClient.ConnectAsync`. Applies the `_reconnectionPolicy` (Polly indefinite retry with backoff, Sec 7.1) for initial connection and automatic reconnection triggered by the library's `Disconnected` event. Uses `SemaphoreSlim` to prevent concurrent connection attempts.
  * **Operation Wrapping:** Implements methods defined in `ISnapcastService` (e.g., `GetStatusAsync`, `SetClientGroupAsync`, `SetClientVolumeAsync`, `SetClientNameAsync`, `CreateGroupAsync`, `DeleteGroupAsync`, etc.). Each wrapper method:
    * Checks disposal status using `ObjectDisposedException.ThrowIf`.
    * Applies the `_operationPolicy` (Polly limited retry, Sec 7.1) around the call to the corresponding `SnapcastClient.SnapcastClient` method (e.g., `_client.SetClientVolumeAsync(...)`).
    * Uses `try/catch` around the policy execution to capture final exceptions after retries are exhausted.
    * Logs operations and errors using the LoggerMessage pattern.
    * Returns `Result` or `Result<T>` indicating success or failure, converting exceptions to `Result.Failure(ex)`.
  * **Event Handling:** Subscribes to events exposed by `SnapcastClient.SnapcastClient` (e.g., `ClientConnected`, `ClientDisconnected`, `GroupChanged`, `ClientVolumeChanged`, `Disconnected`). Event handlers perform two main actions:
        1. **Update State Repository:** Call the appropriate method on the injected `ISnapcastStateRepository` to update the raw in-memory state (e.g., `_stateRepository.UpdateClient(eventArgs.Client)`).
        2. **Publish Cortex.Mediator Notification:** Publish a corresponding internal notification (defined in `/Server/Notifications`, e.g., `SnapcastClientConnectedNotification(eventArgs.Client)`) using the injected `IMediator`. These notifications carry the raw `SnapcastClient` model data received in the event.
  * **State Synchronization:** On initial successful connection (`InitializeAsync`) and potentially periodically or after reconnection, calls `_client.GetStatusAsync` to fetch the complete server state and populates the `ISnapcastStateRepository` using `_stateRepository.UpdateServerState`.
  * **Disposal:** Implements `IAsyncDisposable` to unhook event handlers, gracefully disconnect the `SnapcastClient`, and dispose resources.

```csharp
// Example Snippet: /Infrastructure/Snapcast/SnapcastService.cs
namespace SnapDog2.Infrastructure.Snapcast;

using SnapcastClient;
using SnapcastClient.Models;
// ... other usings (Core Abstractions, Models, Config, Logging, Cortex.Mediator, Polly) ...

public partial class SnapcastService : ISnapcastService, IAsyncDisposable
{
    private readonly SnapcastOptions _config;
    private readonly IMediator _mediator;
    private readonly ISnapcastStateRepository _stateRepository;
    private readonly ILogger<SnapcastService> _logger;
    private readonly SnapcastClient _client;
    private readonly IAsyncPolicy _reconnectionPolicy;
    private readonly IAsyncPolicy _operationPolicy;
    private bool _disposed = false;
    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
    private CancellationTokenSource _disconnectCts = new CancellationTokenSource();

    // --- LoggerMessage Definitions ---
    [LoggerMessage(/*...*/)] private partial void LogInitializing(string host, int port);
    [LoggerMessage(/*...*/)] private partial void LogConnectAttemptFailed(Exception ex);
    // ... other loggers ...

    public SnapcastService(IOptions<SnapcastOptions> configOptions, IMediator mediator, ISnapcastStateRepository stateRepository, ILogger<SnapcastService> logger)
    {
        // ... Assign injected dependencies ...
        _stateRepository = stateRepository;
        // ... Initialize _client, policies, hook events ...
        _client.ClientVolumeChanged += OnSnapcastClientVolumeChangedHandler; // Example hook
        _client.Disconnected += OnSnapcastServerDisconnectedHandler;
    }

    public async Task<Result> InitializeAsync(CancellationToken cancellationToken) { /* ... Connect logic using _reconnectionPolicy ... */ return Result.Success();}

    // --- Wrapper Methods ---
    public async Task<Result> SetClientVolumeAsync(string snapcastClientId, int volumePercent)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var volumeData = new ClientVolume { Percent = volumePercent, Muted = volumePercent == 0 }; // Using SnapcastClient model
        var policyResult = await _operationPolicy.ExecuteAndCaptureAsync(
          async ct => await _client.SetClientVolumeAsync(snapcastClientId, volumeData, ct).ConfigureAwait(false)
        ).ConfigureAwait(false);

        if(policyResult.Outcome == OutcomeType.Failure) {
            LogOperationFailed(nameof(SetClientVolumeAsync), policyResult.FinalException!);
            return Result.Failure(policyResult.FinalException!);
        }
        return Result.Success();
    }
    // ... other wrappers (GetStatusAsync, SetClientGroupAsync etc.) ...


    // --- Event Handlers ---
    private void OnSnapcastClientVolumeChangedHandler(object? sender, ClientVolumeEventArgs e)
    {
        if (_disposed) return;
        LogSnapcastEvent("ClientVolumeChanged", e.ClientId); // Example log
        try
        {
            // 1. Update State Repository (using raw event args/models)
            var client = _stateRepository.GetClient(e.ClientId);
            if (client != null) { _stateRepository.UpdateClient(client with { Config = client.Config with { Volume = e.Volume }}); }

            // 2. Publish Cortex.Mediator Notification (using raw event args/models)
            _ = _mediator.Publish(new SnapcastClientVolumeChangedNotification(e.ClientId, e.Volume)); // Fire-and-forget publish
        } catch(Exception ex) { LogEventHandlerError("ClientVolumeChanged", ex); }
    }
     private void OnSnapcastServerDisconnectedHandler(object? sender, EventArgs e) { /* Trigger Reconnect */ }
    // ... other event handlers ...

    public async ValueTask DisposeAsync() { /* ... Implementation ... */ await ValueTask.CompletedTask; }

    // Internal logger for event handler errors
    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing Snapcast event {EventType}.")]
    private partial void LogEventHandlerError(string eventType, Exception ex);
}
```

### 10.1.2. `SnapcastStateRepository`

* **Implements:** `SnapDog2.Core.Abstractions.ISnapcastStateRepository`
* **Purpose:** Provides a thread-safe, in-memory store for the *latest known raw state* received from the Snapcast server. This acts as a cache reflecting the server's perspective, updated by `SnapcastService` based on events and status pulls.
* **Key Library:** Uses `System.Collections.Concurrent.ConcurrentDictionary` for storing `Sturd.SnapcastNet.Models.Client`, `Group`, `Stream`. Uses `lock` for updating the `ServerInfo` struct.
* **Dependencies:** `ILogger<SnapcastStateRepository>`.
* **Core Logic:** Implements the `ISnapcastStateRepository` interface methods (`UpdateServerState`, `UpdateClient`, `GetClient`, `GetAllClients`, etc.) using thread-safe dictionary operations (`AddOrUpdate`, `TryRemove`, `TryGetValue`). `UpdateServerState` replaces the entire known state based on a full status dump.

```csharp
// Example Snippet: /Infrastructure/Snapcast/SnapcastStateRepository.cs
namespace SnapDog2.Infrastructure.Snapcast;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using Sturd.SnapcastNet.Models; // Use models from library

/// <summary>
/// Thread-safe repository holding the last known state received from Snapcast server.
/// </summary>
public partial class SnapcastStateRepository : ISnapcastStateRepository
{
    private readonly ConcurrentDictionary<string, Client> _clients = new();
    private readonly ConcurrentDictionary<string, Group>_groups = new();
    private readonly ConcurrentDictionary<string, Stream> _streams = new();
    private ServerInfo_serverInfo;
    private readonly object _serverInfoLock = new();
    private readonly ILogger<SnapcastStateRepository>_logger;

    // Logger Messages
    [LoggerMessage(1, LogLevel.Debug, "Updating full Snapcast server state. Groups: {GroupCount}, Clients: {ClientCount}, Streams: {StreamCount}")]
    private partial void LogUpdatingServerState(int groupCount, int clientCount, int streamCount);
    [LoggerMessage(2, LogLevel.Debug, "Updating Snapcast Client {SnapcastId}")] private partial void LogUpdatingClient(string snapcastId);
    [LoggerMessage(3, LogLevel.Debug, "Removing Snapcast Client {SnapcastId}")] private partial void LogRemovingClient(string snapcastId);
    [LoggerMessage(4, LogLevel.Debug, "Updating Snapcast Group {GroupId}")] private partial void LogUpdatingGroup(string groupId);
    [LoggerMessage(5, LogLevel.Debug, "Removing Snapcast Group {GroupId}")] private partial void LogRemovingGroup(string groupId);
    // ... Loggers for Streams ...

    public SnapcastStateRepository(ILogger<SnapcastStateRepository> logger)
    {
        _logger = logger;
    }

    public void UpdateServerState(Server server)
    {
        LogUpdatingServerState(server.Groups?.Count ?? 0, server.Groups?.SelectMany(g => g.Clients).Count() ?? 0, server.Streams?.Count ?? 0);
        lock(_serverInfoLock) { _serverInfo = server.ServerInfo; }
        UpdateDictionary(_groups, server.Groups?.ToDictionary(g => g.Id, g => g) ?? new Dictionary<string, Group>());
        var allClients = server.Groups?.SelectMany(g => g.Clients).DistinctBy(c => c.Id) ?? Enumerable.Empty<Client>();
        UpdateDictionary(_clients, allClients.ToDictionary(c => c.Id, c => c));
        UpdateDictionary(_streams, server.Streams?.ToDictionary(s => s.Id, s => s) ?? new Dictionary<string, Stream>());
    }
    public void UpdateClient(Client client) { LogUpdatingClient(client.Id); _clients[client.Id] = client; }
    public void RemoveClient(string id) { LogRemovingClient(id); _clients.TryRemove(id, out _); }
    public Client? GetClient(string id) => _clients.TryGetValue(id, out var client) ? client : null;
    public IEnumerable<Client> GetAllClients() => _clients.Values.ToList(); // Return copy
    public void UpdateGroup(Group group) { LogUpdatingGroup(group.Id); _groups[group.Id] = group; foreach(var client in group.Clients) UpdateClient(client); }
    public void RemoveGroup(string id) { LogRemovingGroup(id); _groups.TryRemove(id, out _); }
    public Group? GetGroup(string id) => _groups.TryGetValue(id, out var group) ? group : null;
    public IEnumerable<Group> GetAllGroups() => _groups.Values.ToList(); // Return copy
    public ServerInfo GetServerInfo() { lock(_serverInfoLock) return _serverInfo; }
    public void UpdateStream(Stream stream) { /* Log */ _streams[stream.Id] = stream; }
    public void RemoveStream(string id) { /* Log */ _streams.TryRemove(id, out _); }
    public Stream? GetStream(string id) => _streams.TryGetValue(id, out var stream) ? stream : null;
    public IEnumerable<Stream> GetAllStreams() => _streams.Values.ToList();

    private static void UpdateDictionary<TKey, TValue>(ConcurrentDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source) where TKey : notnull
    {
        foreach (var key in target.Keys.Except(source.Keys)) { target.TryRemove(key, out _); }
        foreach (var kvp in source) { target[kvp.Key] = kvp.Value; }
    }
}
```

## 10.2. KNX Integration (`/Infrastructure/Knx/KnxService.cs`)

Implements `IKnxService` using **`Knx.Falcon.Sdk` (6.3.x)**.

* **Library:** `Knx.Falcon.Sdk`.
* **Dependencies:** `IOptions<KnxOptions>`, `List<ZoneConfig>`, `List<ClientConfig>`, `IMediator`, `ILogger<KnxService>`, `ISnapcastStateRepository` (or direct `IZoneManager`/`IClientManager` for state reads).
* **Core Logic:**
  * **Connection/Discovery:** Implements **Option B** logic from Sec 13.2.1 using `KnxIpDeviceDiscovery`, `UsbDeviceDiscovery`, `KnxIpTunnelingConnectorParameters`, `KnxIpRoutingConnectorParameters`, `KnxUsbConnectorParameters`, `KnxBus`, `ConnectAsync`. Uses Polly for connection resilience and a Timer for discovery retries.
  * **Configuration:** Uses `KnxOptions`, `KnxZoneConfig`, `KnxClientConfig`. Performs robust parsing of GAs using `Knx.Falcon.GroupAddress.TryParse` during config loading/validation (handled by Sec 11.3 Validator).
  * **Event Handling:**
    * `OnGroupValueReceived`: Parses incoming `GroupValueEventArgs`, uses helper `MapGroupAddressToCommand` to convert the GA and value (using DPT knowledge) to a Cortex.Mediator command (`IRequest<Result>`), then calls `_mediator.Send`.
    * `OnGroupReadReceived`: Parses incoming `GroupEventArgs`, uses helper `GetStatusInfoFromGroupAddress` to find the corresponding Status ID & DPT. Fetches the current value (via `FetchCurrentValueAsync` which uses `ISnapcastStateRepository` or Cortex.Mediator queries) and sends a response using `SendKnxResponseAsync` (which calls appropriate `KnxBus.WriteXyzAsync` based on DPT).
    * `OnConnectionStateChanged`: Triggers reconnection logic (`InitializeAsync`) if state is `Lost`.
  * **Status Publishing:** Implements `INotificationHandler<StatusChangedNotification>`. The `Handle` method calls `SendStatusAsync`.
  * `SendStatusAsync`: Takes Status ID, Target ID, and value. Uses helper `GetStatusGroupAddress` to find the configured GA string. Parses GA string to `Knx.Falcon.GroupAddress`. Uses helper `WriteToKnxAsync` to convert the value based on expected DPT and call the correct `KnxBus.WriteXyzAsync` method, wrapped in Polly `_operationPolicy`. Handles the 1-based indexing and >255 reporting rule for relevant DPTs.
  * **Disposal:** Implements `IAsyncDisposable`.

## 10.3. MQTT Integration (`/Infrastructure/Mqtt/MqttService.cs`)

Implements `IMqttService` using **`MQTTnet` v5 (5.0.1+)**.

* **Library:** `MQTTnet`, `MQTTnet.Extensions.ManagedClient`.
* **Dependencies:** `IOptions<MqttOptions>`, `List<ZoneConfig>`, `List<ClientConfig>`, `IMediator`, `ILogger<MqttService>`.
* **Core Logic:**
  * Uses `MqttClientFactory` and `MqttClientOptionsBuilder` for setup (TLS, Credentials, LWT, Auto-Reconnect).
  * Uses Polly for initial `ConnectAsync`. Relies on MQTTnet internal reconnect thereafter.
  * Handles `ConnectedAsync`, `DisconnectedAsync`, `ApplicationMessageReceivedAsync` events.
  * `ApplicationMessageReceivedAsync` parses `args.ApplicationMessage`, uses helper `MapTopicToCommand` to convert topic/payload to Cortex.Mediator command, calls `_mediator.Send`. Handles user-preferred detailed topic structure and `control/set` payloads. Handles 1-based indexing.
  * Implements `INotificationHandler<StatusChangedNotification>`. `Handle` method calls `PublishAsync`.
  * `PublishAsync` builds `MqttApplicationMessage` and uses `_mqttClient.PublishAsync`. Publishes to specific status topics AND the comprehensive `state` topic. Handles 1-based indexing.
  * Implements `SubscribeAsync`/`UnsubscribeAsync`.
  * Implements `DisposeAsync`.

## 10.4. Subsonic Integration (`/Infrastructure/Subsonic/SubsonicService.cs`)

Implements `ISubsonicService` using **`SubSonicMedia` (1.0.4-beta.1)**.

* **Library:** `SubSonicMedia`.
* **Dependencies:** `HttpClient` (from factory), `IOptions<SubsonicOptions>`, `ILogger<SubsonicService>`. **No `ICacheService`**.
* **Core Logic:**
  * Initializes `SubSonicMedia.SubsonicClient`, passing the resilient `HttpClient`.
  * Implements interface methods (`GetPlaylistsAsync`, `GetPlaylistAsync`, `GetStreamUrlAsync`, etc.) by calling corresponding methods on the `_subsonicClient`.
  * Performs **mapping** from `SubSonicMedia.Models` (e.g., `Playlist`, `Song`) to `SnapDog2.Core.Models` (`PlaylistInfo`, `TrackInfo`, `PlaylistWithTracks`). This mapping logic resides within this service.
  * Wraps library calls in `try/catch`, returns `Result`/`Result<T>`. Resilience is handled by the injected `HttpClient`.

## 10.5. Media Playback (`/Infrastructure/Media/MediaPlayerService.cs`)

Implements `IMediaPlayerService` using **`LibVLCSharp` (3.8.2)**.

* **Library:** `LibVLCSharp`. Requires native LibVLC installed (handled by Dockerfile).
* **Dependencies:** `IEnumerable<ZoneConfig>`, `ILogger<MediaPlayerService>`, `IMediator` (for publishing playback events).
* **Core Logic:**
  * Initializes LibVLC Core (`Core.Initialize()`) once.
  * Creates and manages a `Dictionary<int, LibVLC>` and `Dictionary<int, MediaPlayer>` (one per ZoneId).
  * Implements `PlayAsync(int zoneId, TrackInfo trackInfo)`:
    * Retrieves `MediaPlayer` for the zone.
    * Creates `LibVLCSharp.Shared.Media` using `trackInfo.Id` (which contains the stream URL) via `FromType.FromLocation`.
    * Adds `:sout=#file{dst=...}` option to the `Media` object, using the `zoneConfig.SnapcastSink` path.
    * Calls `mediaPlayer.Play(media)`.
    * Handles potential errors and returns `Result`.
  * Implements `StopAsync`, `PauseAsync` by calling corresponding `mediaPlayer` methods.
  * Subscribes to `MediaPlayer` events (`EndReached`, `EncounteredError`). Event handlers publish Cortex.Mediator notifications (e.g., `TrackEndedNotification`, `PlaybackErrorNotification`).
  * Implements `IAsyncDisposable` to stop all players and dispose `MediaPlayer` and `LibVLC` instances.
