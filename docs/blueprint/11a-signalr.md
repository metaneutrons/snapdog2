# 11. SignalR Real-Time Communication

## 11.1. ✅ Implementation Status: FULLY IMPLEMENTED

The SignalR real-time communication system is **fully implemented** with the following components:

- ✅ **SnapDogHub** (`/hubs/snapdog/v1`) with group subscription methods
- ✅ **SignalRNotificationHandler** that emits all domain notifications to subscribed clients
- ✅ **Group-based broadcasting** for efficient, selective event delivery
- ✅ **Auto-registration** via existing DI system
- ✅ **All 15 notification types** mapped to SignalR events

**Key Enhancement**: Implemented group-based subscriptions instead of broadcast-to-all, providing better performance and bandwidth efficiency.

## 11.2. SignalR Design Philosophy

The SnapDog2 SignalR implementation provides **real-time bidirectional communication** between the server and connected clients, enabling immediate notification of state changes without polling. It serves as the primary mechanism for building responsive user interfaces and real-time monitoring applications.

SignalR events directly correspond to the **Command Framework (Section 9)** status updates, ensuring consistency between different notification methods (SignalR, MQTT). When a zone's volume changes, both SignalR clients and MQTT subscribers receive notifications simultaneously.

Key design principles:

1. **Status Framework Alignment**: SignalR events map directly to status updates from the Command Framework. A `ZoneVolumeChanged` SignalR event corresponds to the `ZONE_VOLUME_STATUS` update.
2. **Real-Time State Synchronization**: Clients receive immediate notifications when system state changes, eliminating the need for polling and ensuring UI consistency.
3. **Selective Event Coverage**: Only dynamic, frequently-changing status updates are broadcast via SignalR. Static configuration data and bulk collections are excluded to optimize performance.
4. **Lightweight Payloads**: Events carry minimal, focused data to reduce bandwidth and improve responsiveness.
5. **Hub-Based Architecture**: Uses ASP.NET Core SignalR hubs for connection management and event broadcasting.

## 11.3. Connection and Hub Methods

SignalR connections require the same authentication as REST API endpoints:

* **Hub Endpoint**: SignalR hub is available at `/hubs/snapdog/v1`
* **Authentication Required**: Hub is protected with `[Authorize]` attribute  
* **API Key Authentication**: Use `X-API-Key` header or `apikey` query parameter
* **Group-Based Subscriptions**: Clients can subscribe to specific zones, clients, or system events
* **Selective Broadcasting**: Events are sent only to subscribed clients, reducing bandwidth

### 11.3.1. Hub Methods (Client-to-Server)

```csharp
public class SnapDogHub : Hub
{
    // Zone subscriptions
    public async Task JoinZone(int zoneIndex);
    public async Task LeaveZone(int zoneIndex);
    
    // Client subscriptions  
    public async Task JoinClient(int clientIndex);
    public async Task LeaveClient(int clientIndex);
    
    // System subscriptions
    public async Task JoinSystem();
    public async Task LeaveSystem();
}
```

### 11.3.2. JavaScript Connection Example

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/snapdog/v1", {
        headers: { "X-API-Key": "your-api-key-here" }
    })
    .withAutomaticReconnect()
    .build();

// Subscribe to zone 1 events
await connection.start();
await connection.invoke("JoinZone", 1);

// Subscribe to system events
await connection.invoke("JoinSystem");
```

## 11.4. Event Categories

SignalR events are organized into three main categories corresponding to the Command Framework structure:

### 11.4.1. Zone Events

Real-time notifications for zone state changes affecting audio playback and control.

### 11.4.2. Client Events

Real-time notifications for Snapcast client state changes affecting individual audio endpoints.

### 11.4.3. System Events

Real-time notifications for system-wide status changes and error conditions.

## 11.5. SignalR Event Specification

### 11.5.1. Zone Events

Events sent to clients subscribed to specific zones via `JoinZone(zoneIndex)`.

| Event Name | Status ID | Trigger Condition | Parameters | Description |
|:-----------|:----------|:------------------|:-----------|:------------|
| `ZoneVolumeChanged` | `VOLUME_STATUS` | Zone volume level changes | `(int zoneIndex, int volume)` | Volume changed (0-100) |
| `ZoneMuteChanged` | `ZONE_MUTE_STATUS` | Zone mute state changes | `(int zoneIndex, bool muted)` | Mute state toggled |
| `ZonePlaybackChanged` | `PLAYBACK_STATE` | Playback state changes | `(int zoneIndex, string playbackState)` | Play/pause/stop state |
| `ZoneTrackMetadataChanged` | `TRACK_METADATA` | Track metadata updates | `(int zoneIndex, TrackInfo track)` | Current track info |
| `ZoneProgressChanged` | `TRACK_PROGRESS_STATUS` | Track position updates | `(int zoneIndex, long position, float progress)` | Position (ms) and progress (0.0-1.0) |
| `ZoneRepeatModeChanged` | `TRACK_REPEAT_STATUS` | Repeat mode changes | `(int zoneIndex, bool trackRepeat, bool playlistRepeat)` | Repeat modes |
| `ZoneShuffleChanged` | `ZONE_SHUFFLE_STATUS` | Shuffle mode changes | `(int zoneIndex, bool shuffled)` | Shuffle enabled/disabled |
| `ZonePlaylistChanged` | `ZONE_PLAYLIST_STATUS` | Active playlist changes | `(int zoneIndex, int playlistIndex, string playlistName)` | Current playlist |

### 11.5.2. Client Events

Events sent to clients subscribed to specific clients via `JoinClient(clientIndex)`.

| Event Name | Status ID | Trigger Condition | Parameters | Description |
|:-----------|:----------|:------------------|:-----------|:------------|
| `ClientVolumeChanged` | `CLIENT_VOLUME_STATUS` | Client volume changes | `(int clientIndex, int volume)` | Client volume (0-100) |
| `ClientMuteChanged` | `CLIENT_MUTE_STATUS` | Client mute state changes | `(int clientIndex, bool muted)` | Client mute toggled |
| `ClientLatencyChanged` | `CLIENT_LATENCY_STATUS` | Client latency changes | `(int clientIndex, int latency)` | Network latency (ms) |
| `ClientZoneChanged` | `CLIENT_ZONE_STATUS` | Client zone assignment | `(int clientIndex, int? zoneIndex)` | Zone assignment (null = unassigned) |
| `ClientConnected` | `CLIENT_CONNECTED` | Client connection state | `(int clientIndex, bool connected)` | Connection status |

### 11.5.3. System Events

Events sent to clients subscribed to system events via `JoinSystem()`.

| Event Name | Status ID | Trigger Condition | Parameters | Description |
|:-----------|:----------|:------------------|:-----------|:------------|
| `SystemStatusChanged` | `SYSTEM_STATUS` | System status changes | `(SystemStatus status)` | System status object |
| `ErrorOccurred` | `SYSTEM_ERROR` | System errors | `(string errorCode, string message, string? context)` | Error notifications |

## 11.6. Event Exclusions

The following status types are **explicitly excluded** from SignalR events for performance and architectural reasons:

### 11.6.1. Static Configuration Data

* `VERSION_INFO`: Software version information (static)

* `ZONE_COUNT`: Total zone count (configuration-based)
* `CLIENT_COUNT`: Total client count (configuration-based)

### 11.6.2. Bulk Data Collections

* `ZONE_STATES`: Complete zone list (use individual zone events)

* `CLIENT_STATES`: Complete client list (use individual client events)
* `MEDIA_PLAYLISTS`: Playlist collections (large, infrequently changed)
* `MEDIA_PLAYLIST_INFO`: Detailed playlist data (large payloads)
* `MEDIA_PLAYLIST_TRACKS`: Track listings (large collections)

### 11.6.3. Alternative Notification Paths

* `COMMAND_STATUS`: Command processing status (has dedicated error handling)

* `SERVER_STATS`: Performance statistics (polling-appropriate)

### 11.6.4. Media Browsing Data

* `MEDIA_TRACK_INFO`: Individual track details (on-demand retrieval)

* Media browsing endpoints (large, hierarchical data)

## 11.7. Implementation Architecture

### 11.7.1. Hub Structure

```csharp
// ✅ Implemented in SnapDog2/Api/Hubs/SnapDogHub.cs
public class SnapDogHub : Hub
{
    public async Task JoinZone(int zoneIndex)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task LeaveZone(int zoneIndex)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"zone_{zoneIndex}");
    }

    // ... other subscription methods
}
```

### 11.7.2. Event Broadcasting

Events are broadcast to specific groups using the hub context:

```csharp
// ✅ Implemented in SnapDog2/Api/Hubs/Handlers/SignalRNotificationHandler.cs
public async Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
{
    await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
        .SendAsync("ZoneVolumeChanged", notification.ZoneIndex, notification.Volume, cancellationToken);
}
```

### 11.7.3. Notification Integration

✅ **Fully Implemented**: SignalR events are triggered by domain notification handlers that process the same events as MQTT and other integrations, ensuring consistency across all notification channels.

## 11.8. Client Implementation Examples

### 11.8.1. JavaScript/TypeScript

```javascript
// Connection setup
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/snapdog/v1")
    .withAutomaticReconnect()
    .build();

// Subscribe to zone 1 events
await connection.start();
await connection.invoke("JoinZone", 1);
await connection.invoke("JoinSystem");

// Event handlers - note parameter format (not objects)
connection.on("ZoneVolumeChanged", (zoneIndex, volume) => {
    console.log(`Zone ${zoneIndex} volume: ${volume}`);
    updateVolumeSlider(zoneIndex, volume);
});

connection.on("ZonePlaybackChanged", (zoneIndex, playbackState) => {
    console.log(`Zone ${zoneIndex} playback: ${playbackState}`);
    updatePlayButton(zoneIndex, playbackState);
});

connection.on("ZoneProgressChanged", (zoneIndex, position, progress) => {
    updateProgressBar(zoneIndex, position, progress);
});
```

### 11.8.2. C# Client

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5555/hubs/snapdog/v1")
    .Build();

// Subscribe to events
await connection.StartAsync();
await connection.InvokeAsync("JoinZone", 1);

// Event handlers
connection.On<int, int>("ZoneVolumeChanged", (zoneIndex, volume) => {
    // Handle volume change
});

connection.On<int, long, float>("ZoneProgressChanged", (zoneIndex, position, progress) => {
    // Handle progress update
});
```

## 11.9. Performance Considerations

### 11.9.1. Event Frequency

* **High Frequency**: Track progress updates (every second during playback)

* **Medium Frequency**: Volume changes, playback state changes
* **Low Frequency**: Zone assignments, client connections, system status

### 11.9.2. Bandwidth Optimization

* Minimal payload sizes (only essential data)

* No redundant information in events
* Efficient JSON serialization

### 11.9.3. Connection Management

* Automatic reconnection handling

* Connection cleanup on client disconnect
* Scalable to multiple concurrent clients

## 11.10. Error Handling

### 11.10.1. Connection Errors

* Authentication failures return 401 Unauthorized

* Network issues trigger automatic reconnection
* Hub exceptions are logged but don't disconnect clients

### 11.10.2. Event Delivery

* Events are fire-and-forget (no acknowledgment required)

* Failed deliveries are logged but don't block other clients
* No event queuing for disconnected clients

## 11.11. Future Enhancements

### 11.11.1. Selective Subscriptions

Future versions may support client-specific event subscriptions to reduce bandwidth for clients interested in specific zones or event types.

### 11.11.2. Event History

Potential addition of recent event history for newly connected clients to synchronize state without full API calls.

### 11.11.3. Bidirectional Commands

While currently server-to-client only, future versions may support client-to-server command execution via SignalR methods.
