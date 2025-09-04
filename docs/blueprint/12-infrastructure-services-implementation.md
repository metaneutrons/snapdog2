# 12. Infrastructure Services Implementation

This chapter details the concrete implementations of the infrastructure services responsible for bridging the gap between SnapDog2's core application logic (`/Server` layer) and the external world (libraries, protocols, servers). These services reside within the `/Infrastructure` folder and implement the abstractions defined in `/Core/Abstractions`. They encapsulate the complexities of external interactions, apply resilience patterns, handle protocol-specific details, and integrate with the application's logging, configuration, and state management systems.

## 12.1. Snapcast Integration (`/Infrastructure/Snapcast/`)

This component handles all direct communication with the Snapcast server using a custom JSON-RPC WebSocket implementation that provides real-time audio streaming control and notification processing.

### 12.1.1. Architecture Overview

The Snapcast integration uses a custom implementation that communicates directly with the Snapcast server's JSON-RPC WebSocket interface, eliminating third-party library dependencies and providing full control over the communication protocol.

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   SnapDog API   │───▶│ CustomSnapcast   │───▶│ Snapcast Server │
│                 │    │     Service      │    │   (JSON-RPC)    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │                         │
                              ▼                         │
                       ┌──────────────────┐             │
                       │ SnapcastState    │             │
                       │   Repository     │             │
                       └──────────────────┘             │
                              │                         │
                              ▼                         │
                       ┌──────────────────┐             │
                       │   ClientManager  │◀────────────┘
                       │  (MAC Mapping)   │
                       └──────────────────┘
```

### 12.1.2. `CustomSnapcastService`

* **Implements:** `SnapDog2.Core.Abstractions.ISnapcastService`
* **Purpose:** Main service implementing ISnapcastService interface with direct JSON-RPC WebSocket communication to Snapcast server.
* **Key Features:** WebSocket connection management, real-time notification processing, client MAC address to ID mapping, automatic state repository synchronization.
* **Dependencies:** `SnapcastJsonRpcClient`, `SnapcastStateRepository`, `IServiceProvider`, `ILogger<CustomSnapcastService>`.

**Core Logic:**
* **Connection Management:** Establishes persistent WebSocket connection to Snapcast server on port 1705. Implements automatic reconnection with exponential backoff strategy.
* **State Initialization:** Calls `RefreshServerState()` during service startup to populate the state repository with current server state via `Server.GetStatus` command.
* **Real-time Notifications:** Processes incoming JSON-RPC notifications (`Client.OnVolumeChanged`, `Client.OnConnect`, `Server.OnUpdate`) and publishes corresponding Cortex.Mediator events.
* **Client Mapping:** Resolves Snapcast client IDs to SnapDog clients using MAC address mapping through the ClientManager.
* **Operation Wrapping:** Implements all ISnapcastService methods by sending appropriate JSON-RPC commands to the Snapcast server.

```csharp
// Core Implementation: /Infrastructure/Snapcast/CustomSnapcastService.cs
public partial class CustomSnapcastService : ISnapcastService, IDisposable
{
    private readonly SnapcastJsonRpcClient _jsonRpcClient;
    private readonly SnapcastStateRepository _stateRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomSnapcastService> _logger;

    public async Task<Result> InitializeAsync()
    {
        try
        {
            await _jsonRpcClient.ConnectAsync();
            
            // Initialize state repository with current server state
            await RefreshServerState();
            
            _logger.LogInformation("Custom Snapcast service initialized successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize custom Snapcast service");
            return Result.Failure(ex);
        }
    }

    private async Task RefreshServerState()
    {
        try
        {
            var response = await _jsonRpcClient.SendRequestAsync<ServerGetStatusResponse>("Server.GetStatus");
            var server = ConvertToServer(response.Server);
            _stateRepository.UpdateServerState(server);
            _logger.LogDebug("Server state refreshed with {GroupCount} groups", response.Server.Groups.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh server state");
        }
    }

    // Notification handlers
    private async Task HandleClientVolumeChanged(ClientOnVolumeChangedNotification notification)
    {
        _logger.LogDebug("🔊 Client volume changed: {ClientId} -> {Volume}% (muted: {Muted})", 
            notification.Id, notification.Volume.Percent, notification.Volume.Muted);

        var (client, clientIndex) = await GetClientBySnapcastIdAsync(notification.Id);
        if (client == null)
        {
            _logger.LogDebug("Ignoring volume change for unconfigured client: {ClientId}", notification.Id);
            return;
        }

        // Publish SnapDog notifications using 1-based client index
        var volumeNotification = new SnapcastClientVolumeChangedNotification(
            clientIndex.ToString(), 
            new Models.ClientVolume { Muted = notification.Volume.Muted, Percent = notification.Volume.Percent });
        
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(volumeNotification);
    }
}
```

### 12.1.3. `SnapcastJsonRpcClient`

* **Purpose:** Low-level JSON-RPC WebSocket communication with Snapcast server.
* **Key Features:** Persistent WebSocket connection management, request/response correlation with unique IDs, automatic reconnection with exponential backoff, comprehensive error handling.
* **Dependencies:** `ILogger<SnapcastJsonRpcClient>`, `IOptions<SnapcastOptions>`.

**Core Logic:**
* **WebSocket Management:** Maintains persistent `ClientWebSocket` connection to `ws://localhost:1705/jsonrpc`.
* **Request Correlation:** Uses `ConcurrentDictionary<string, TaskCompletionSource<JsonElement>>` to correlate requests with responses using unique IDs.
* **Message Processing:** Handles incoming JSON-RPC messages, distinguishing between responses (with `id`) and notifications (without `id`).
* **Error Handling:** Comprehensive error handling for connection failures, message parsing errors, and protocol violations.

```csharp
public class SnapcastJsonRpcClient : IDisposable
{
    private ClientWebSocket _webSocket;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests;

    public async Task<T> SendRequestAsync<T>(string method, object parameters = null)
    {
        var id = Guid.NewGuid().ToString();
        var request = new { id, jsonrpc = "2.0", method, @params = parameters };
        
        var tcs = new TaskCompletionSource<JsonElement>();
        _pendingRequests[id] = tcs;
        
        var json = JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        
        var response = await tcs.Task;
        return JsonSerializer.Deserialize<T>(response);
    }

    public event Func<string, JsonElement, Task> NotificationReceived;
    
    private async Task HandleIncomingMessages()
    {
        var buffer = new byte[4096];
        while (_webSocket.State == WebSocketState.Open)
        {
            var result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var message = JsonDocument.Parse(json);
            
            if (message.RootElement.TryGetProperty("id", out var idProperty))
            {
                // Response - complete pending request
                var id = idProperty.GetString();
                if (_pendingRequests.TryRemove(id, out var tcs))
                {
                    tcs.SetResult(message.RootElement.GetProperty("result"));
                }
            }
            else
            {
                // Notification - publish event
                var method = message.RootElement.GetProperty("method").GetString();
                var parameters = message.RootElement.GetProperty("params");
                await NotificationReceived?.Invoke(method, parameters);
            }
        }
    }
}
```

### 12.1.4. Client MAC Address Mapping System

**Purpose:** Maps Snapcast client IDs to SnapDog client configurations using MAC addresses as the authoritative identifier.

**Problem Solved:** Snapcast clients may have dynamic names but consistent MAC addresses. SnapDog configuration uses MAC addresses to identify clients, while Snapcast notifications use client IDs (names). This system bridges the gap.

**Mapping Flow:**
1. Snapcast notification contains client ID (e.g., "kitchen")
2. Query SnapcastStateRepository for client details
3. Extract MAC address from client host information  
4. Match MAC address to SnapDog client configuration
5. Return mapped SnapDog client with 1-based index

```csharp
public async Task<(IClient? Client, int ClientIndex)> GetClientBySnapcastIdAsync(string snapcastClientId)
{
    // Get all Snapcast clients from state repository
    var allSnapcastClients = this._snapcastStateRepository.GetAllClients();

    // Find the Snapcast client by ID
    var snapcastClient = allSnapcastClients.FirstOrDefault(c => c.Id == snapcastClientId);
    if (string.IsNullOrEmpty(snapcastClient.Id))
    {
        this.LogSnapcastClientNotFound(snapcastClientId);
        return (null, 0);
    }

    // Get the MAC address from the Snapcast client
    var macAddress = snapcastClient.Host.Mac;
    if (string.IsNullOrEmpty(macAddress))
    {
        this.LogMacAddressNotFound(snapcastClientId);
        return (null, 0);
    }

    // Find the corresponding client index by MAC address in our configuration
    var clientIndex = this._clientConfigs.FindIndex(config =>
        string.Equals(config.Mac, macAddress, StringComparison.OrdinalIgnoreCase)) + 1; // 1-based index

    if (clientIndex == 0) // Not found (-1 + 1 = 0)
    {
        this.LogClientConfigNotFoundByMac(macAddress);
        return (null, 0);
    }

    // Create and return the IClient wrapper
    var client = new SnapDogClient(clientIndex, snapcastClient, this._clientConfigs[clientIndex - 1]);
    return (client, clientIndex);
}
```

### 12.1.5. Protocol Implementation

**JSON-RPC Message Formats:**

**Request Structure:**
```json
{
  "id": 1,
  "jsonrpc": "2.0", 
  "method": "Client.SetVolume",
  "params": {
    "id": "kitchen",
    "volume": {
      "muted": false,
      "percent": 75
    }
  }
}
```

**Notification Structure:**
```json
{
  "jsonrpc": "2.0",
  "method": "Client.OnVolumeChanged", 
  "params": {
    "id": "kitchen",
    "volume": {
      "muted": false,
      "percent": 75
    }
  }
}
```

**Supported Commands:**
- `Client.SetVolume` - Set client volume and mute state
- `Client.SetLatency` - Adjust client audio latency  
- `Client.SetName` - Update client display name
- `Group.SetClients` - Assign clients to groups (zones)
- `Group.SetMute` - Mute/unmute entire group
- `Group.SetStream` - Change group audio stream
- `Server.GetStatus` - Retrieve complete server state

**Critical Notifications:**
- `Client.OnVolumeChanged` - Volume/mute state changes
- `Client.OnConnect` - Client connection events
- `Client.OnDisconnect` - Client disconnection events
- `Server.OnUpdate` - Complete server state synchronization

### 12.1.6. `SnapcastStateRepository`

* **Implements:** `SnapDog2.Core.Abstractions.ISnapcastStateRepository`
* **Purpose:** Thread-safe, in-memory store for the latest known raw state received from the Snapcast server. Acts as a cache reflecting the server's perspective, updated by CustomSnapcastService based on events and status pulls.
* **Key Features:** Thread-safe operations using `ConcurrentDictionary`, complete state synchronization, MAC address-based client lookup.
* **Dependencies:** `ILogger<SnapcastStateRepository>`.

**Core Logic:** 
* Implements thread-safe dictionary operations for storing `SnapClient`, `Group`, `Stream` objects
* `UpdateServerState` replaces entire known state based on full status dump from `Server.GetStatus`
* Provides efficient lookup methods for client MAC address resolution
* Maintains comprehensive logging for all state changes

```csharp
public partial class SnapcastStateRepository : ISnapcastStateRepository
{
    private readonly ConcurrentDictionary<string, SnapClient> _clients = new();
    private readonly ConcurrentDictionary<string, Group> _groups = new();
    private readonly ConcurrentDictionary<string, Stream> _streams = new();
    private Server _serverInfo;
    private readonly object _serverInfoLock = new();

    public void UpdateServerState(Server server)
    {
        var allClients = server.Groups?.SelectMany(g => g.Clients).DistinctBy(c => c.Id) ?? Enumerable.Empty<SnapClient>();
        var clientCount = allClients.Count();
        var groupCount = server.Groups?.Length ?? 0;
        var streamCount = server.Streams?.Length ?? 0;

        LogUpdatingServerState(groupCount, clientCount, streamCount);
        
        lock (_serverInfoLock) { _serverInfo = server; }
        
        UpdateDictionary(_groups, server.Groups?.ToDictionary(g => g.Id, g => g) ?? new Dictionary<string, Group>());
        UpdateDictionary(_clients, allClients.ToDictionary(c => c.Id, c => c));
        UpdateDictionary(_streams, server.Streams?.ToDictionary(s => s.Id, s => s) ?? new Dictionary<string, Stream>());
    }

    public SnapClient? GetClientByMac(string macAddress)
    {
        return _clients.Values.FirstOrDefault(c => 
            string.Equals(c.Host.Mac, macAddress, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<SnapClient> GetAllClients() => _clients.Values.ToList();
}
```

### 12.1.7. Integration Points

**Event Publishing Flow:**
1. Snapcast sends `Client.OnVolumeChanged` notification via WebSocket
2. `CustomSnapcastService` receives and processes notification  
3. MAC address mapping resolves SnapDog client using `ClientManager`
4. `SnapcastClientVolumeChangedNotification` published via Cortex.Mediator
5. Multiple handlers process the event:
   - Storage update in `SnapcastEventNotificationHandler`
   - MQTT publishing via `IntegrationPublishingHandlers`
   - KNX integration via `KnxIntegrationHandler`
   - SignalR real-time updates via `SignalRNotificationHandler`

**Service Registration:**
```csharp
// Program.cs - Infrastructure Services
services.AddSingleton<SnapcastJsonRpcClient>();
services.AddSingleton<ISnapcastService, CustomSnapcastService>();
services.AddSingleton<SnapcastStateRepository>();
```

**Configuration:**
```json
{
  "Snapcast": {
    "Host": "localhost",
    "Port": 1705,
    "ConnectionTimeout": "00:00:30",
    "ReconnectDelay": "00:00:05", 
    "MaxReconnectAttempts": 10
  }
}
```

## 12.2. KNX Integration (`/Infrastructure/Knx/KnxService.cs`)

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

## 12.3. MQTT Integration (`/Infrastructure/Mqtt/MqttService.cs`)

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

## 12.4. Subsonic Integration (`/Infrastructure/Subsonic/SubsonicService.cs`)

Implements `ISubsonicService` using **`SubSonicMedia` (1.0.5)**.

* **Library:** `SubSonicMedia`.
* **Dependencies:** `HttpClient` (from factory), `IOptions<SubsonicOptions>`, `ILogger<SubsonicService>`. **No `ICacheService`**.
* **Core Logic:**
  * Initializes `SubSonicMedia.SubsonicClient`, passing the resilient `HttpClient`.
  * Implements interface methods (`GetPlaylistsAsync`, `GetPlaylistAsync`, `GetStreamUrlAsync`, etc.) by calling corresponding methods on the `_subsonicClient`.
  * Performs **mapping** from `SubSonicMedia.Models` (e.g., `Playlist`, `Song`) to `SnapDog2.Core.Models` (`PlaylistInfo`, `TrackInfo`, `PlaylistWithTracks`). This mapping logic resides within this service.
  * Wraps library calls in `try/catch`, returns `Result`/`Result<T>`. Resilience is handled by the injected `HttpClient`.

## 12.5. Media Playback (`/Infrastructure/Audio/MediaPlayerService.cs`)

Implements `IMediaPlayerService` using **LibVLCSharp**, a cross-platform .NET wrapper for the powerful VLC media framework.

* **Library:** `LibVLCSharp` (3.8.5) - Cross-platform .NET bindings for LibVLC with comprehensive multimedia support.
* **Dependencies:** `IOptions<AudioConfig>`, `ILogger<MediaPlayerService>`, `ILoggerFactory`, `IMediator` (for publishing playback events), `IEnumerable<ZoneConfig>`.
* **Core Logic:**
  * Uses **streamlined audio configuration** (`AudioConfig`) that provides a single source of truth for audio settings across both Snapcast and LibVLC.
  * Creates and manages a `ConcurrentDictionary<int, MediaPlayer>` (one per ZoneIndex) for concurrent multi-zone audio streaming.
  * **Automatic MAX_STREAMS calculation** - Maximum concurrent streams equals the number of configured zones (`_zoneConfigs.Count()`), eliminating manual configuration.
  * Implements `PlayAsync(int zoneIndex, TrackInfo trackInfo)`:
    * Validates zone limits using automatic MAX_STREAMS calculation.
    * Creates `MediaPlayer` instance with `AudioProcessingContext` containing:
      * `LibVLC` instance with hardcoded arguments (`--no-video`, `--quiet`) for consistency.
      * `MetadataManager` for comprehensive metadata extraction using `Media.Parse()`.
      * Audio processing pipeline configured for raw output to Snapcast sinks.
    * Builds LibVLC media options for transcoding to raw audio format matching global audio configuration.
    * Starts real-time audio streaming with automatic format detection and processing.
    * Publishes `TrackPlaybackStartedNotification` via Cortex.Mediator.
  * Implements `StopAsync`, `PauseAsync` by stopping LibVLC playback and cleaning up resources.
  * Subscribes to LibVLC media events and publishes corresponding Cortex.Mediator notifications.
  * Implements `IAsyncDisposable` to stop all players and dispose LibVLC resources properly.
  * **Audio Format Support:** Comprehensive format support via LibVLC (MP3, AAC, FLAC, WAV, OGG, HLS, DASH, and many more).
  * **Cross-Platform Streaming:** Native HTTP/HTTPS streaming with configurable timeout and robust error handling.
  * **Metadata Extraction:** Rich metadata extraction including technical details, artwork URLs, and comprehensive track information.

### 12.5.1. Streamlined Audio Configuration Integration

The media playback system uses the unified `AudioConfig` class that eliminates configuration duplication:

```csharp
// Global audio configuration (single source of truth)
public class AudioConfig
{
    // User-configurable settings
    public int SampleRate { get; set; } = 48000;
    public int BitDepth { get; set; } = 16;
    public int Channels { get; set; } = 2;
    public string Codec { get; set; } = "flac";
    public int BufferMs { get; set; } = 20;

    // Computed properties (not configurable)
    public string SnapcastSampleFormat => $"{SampleRate}:{BitDepth}:{Channels}";
    public string[] LibVLCArgs => new[] { "--no-video", "--quiet" };
    public string OutputFormat => "raw";
    public string TempDirectory => "/tmp/snapdog_audio";
}
```

**Key Benefits:**

* **Single Source of Truth**: Audio format defined once, used by both Snapcast and LibVLC
* **Automatic Consistency**: Impossible to configure mismatched audio settings
* **Smart Automation**: MAX_STREAMS calculated from actual zone configuration
* **Simplified Management**: Only essential settings are user-configurable
