# 11a. SignalR Real-Time Communication

## 11a.1. SignalR Design Philosophy

The SnapDog2 SignalR implementation provides **real-time bidirectional communication** between the server and connected clients, enabling immediate notification of state changes without polling. It serves as the primary mechanism for building responsive user interfaces and real-time monitoring applications.

SignalR events directly correspond to the **Command Framework (Section 9)** status updates, ensuring consistency between different notification methods (SignalR, MQTT). When a zone's volume changes, both SignalR clients and MQTT subscribers receive notifications simultaneously.

Key design principles:

1. **Status Framework Alignment**: SignalR events map directly to status updates from the Command Framework. A `ZoneVolumeChanged` SignalR event corresponds to the `ZONE_VOLUME_STATUS` update.
2. **Real-Time State Synchronization**: Clients receive immediate notifications when system state changes, eliminating the need for polling and ensuring UI consistency.
3. **Selective Event Coverage**: Only dynamic, frequently-changing status updates are broadcast via SignalR. Static configuration data and bulk collections are excluded to optimize performance.
4. **Lightweight Payloads**: Events carry minimal, focused data to reduce bandwidth and improve responsiveness.
5. **Hub-Based Architecture**: Uses ASP.NET Core SignalR hubs for connection management and event broadcasting.

## 11a.2. Connection and Authentication

SignalR connections follow the same authentication model as the REST API:

* **API Key Authentication**: Clients **must** provide a valid API key via the `X-API-Key` header during connection negotiation. The same API keys used for REST API access are valid for SignalR connections.
* **Connection Management**: The server automatically manages connection lifecycle, including reconnection handling and cleanup.
* **Hub Endpoint**: SignalR hub is available at `/hubs/snapdog` relative to the base server URL.

Example JavaScript connection:
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/snapdog", {
        headers: { "X-API-Key": "your-api-key-here" }
    })
    .build();
```

## 11a.3. Event Categories

SignalR events are organized into three main categories corresponding to the Command Framework structure:

### 11a.3.1. Zone Events
Real-time notifications for zone state changes affecting audio playback and control.

### 11a.3.2. Client Events  
Real-time notifications for Snapcast client state changes affecting individual audio endpoints.

### 11a.3.3. System Events
Real-time notifications for system-wide status changes and error conditions.

## 11a.4. SignalR Event Specification

### 11a.4.1. Zone Events

Events related to audio zone state changes. All zone events include the affected zone index (1-based).

| Event Name | Status ID | Trigger Condition | Payload | Description |
|:-----------|:----------|:------------------|:--------|:------------|
| `ZoneVolumeChanged` | `ZONE_VOLUME_STATUS` | Zone volume level changes | `{ zoneIndex: int, volume: int }` | Volume changed (0-100) |
| `ZoneMuteChanged` | `ZONE_MUTE_STATUS` | Zone mute state changes | `{ zoneIndex: int, muted: bool }` | Mute state toggled |
| `ZonePlaybackChanged` | `ZONE_PLAYBACK_STATUS` | Playback state changes | `{ zoneIndex: int, state: string }` | Play/pause/stop state |
| `ZoneTrackMetadataChanged` | `ZONE_TRACK_METADATA_STATUS` | Track metadata updates | `{ zoneIndex: int, metadata: TrackInfo }` | Current track info |
| `ZoneProgressChanged` | `ZONE_TRACK_PROGRESS_STATUS` | Track position updates | `{ zoneIndex: int, progress: float }` | Playback progress (0.0-1.0) |
| `ZoneRepeatModeChanged` | `ZONE_REPEAT_MODE_STATUS` | Repeat mode changes | `{ zoneIndex: int, repeatMode: string }` | Repeat mode (none/track/playlist) |
| `ZoneShuffleChanged` | `ZONE_SHUFFLE_STATUS` | Shuffle mode changes | `{ zoneIndex: int, shuffled: bool }` | Shuffle enabled/disabled |
| `ZonePlaylistChanged` | `ZONE_PLAYLIST_STATUS` | Active playlist changes | `{ zoneIndex: int, playlistIndex: int }` | Current playlist index |

### 11a.4.2. Client Events

Events related to Snapcast client state changes. All client events include the affected client index.

| Event Name | Status ID | Trigger Condition | Payload | Description |
|:-----------|:----------|:------------------|:--------|:------------|
| `ClientVolumeChanged` | `CLIENT_VOLUME_STATUS` | Client volume changes | `{ clientIndex: int, volume: int }` | Client volume (0-100) |
| `ClientMuteChanged` | `CLIENT_MUTE_STATUS` | Client mute state changes | `{ clientIndex: int, muted: bool }` | Client mute toggled |
| `ClientLatencyChanged` | `CLIENT_LATENCY_STATUS` | Client latency changes | `{ clientIndex: int, latency: int }` | Network latency (ms) |
| `ClientZoneChanged` | `CLIENT_ZONE_STATUS` | Client zone assignment | `{ clientIndex: int, zoneIndex: int? }` | Zone assignment (null = unassigned) |
| `ClientConnected` | `CLIENT_CONNECTED_STATUS` | Client connection state | `{ clientIndex: int, connected: bool }` | Connection status |

### 11a.4.3. System Events

Events related to system-wide status and error conditions.

| Event Name | Status ID | Trigger Condition | Payload | Description |
|:-----------|:----------|:------------------|:--------|:------------|
| `SystemStatusChanged` | `SYSTEM_STATUS` | System status changes | `{ status: string, timestamp: string }` | System online/offline |
| `ErrorOccurred` | `ERROR_STATUS` | System errors | `{ error: string, timestamp: string, severity: string }` | Error notifications |

## 11a.5. Event Exclusions

The following status types are **explicitly excluded** from SignalR events for performance and architectural reasons:

### 11a.5.1. Static Configuration Data
- `VERSION_INFO`: Software version information (static)
- `ZONE_COUNT`: Total zone count (configuration-based)
- `CLIENT_COUNT`: Total client count (configuration-based)

### 11a.5.2. Bulk Data Collections
- `ZONE_STATES`: Complete zone list (use individual zone events)
- `CLIENT_STATES`: Complete client list (use individual client events)
- `MEDIA_PLAYLISTS`: Playlist collections (large, infrequently changed)
- `MEDIA_PLAYLIST_INFO`: Detailed playlist data (large payloads)
- `MEDIA_PLAYLIST_TRACKS`: Track listings (large collections)

### 11a.5.3. Alternative Notification Paths
- `COMMAND_STATUS`: Command processing status (has dedicated error handling)
- `SERVER_STATS`: Performance statistics (polling-appropriate)

### 11a.5.4. Media Browsing Data
- `MEDIA_TRACK_INFO`: Individual track details (on-demand retrieval)
- Media browsing endpoints (large, hierarchical data)

## 11a.6. Implementation Architecture

### 11a.6.1. Hub Structure
```csharp
public class SnapDogHub : Hub
{
    // Connection management and authentication handled by framework
    // No custom methods - server-to-client events only
}
```

### 11a.6.2. Event Broadcasting
Events are broadcast to all connected clients using the hub context:
```csharp
await _hubContext.Clients.All.SendAsync("ZoneVolumeChanged", new { 
    zoneIndex = 1, 
    volume = 75 
});
```

### 11a.6.3. Notification Integration
SignalR events are triggered by the same notification handlers that process MQTT and other integrations, ensuring consistency across all notification channels.

## 11a.7. Client Implementation Examples

### 11a.7.1. JavaScript/TypeScript
```javascript
// Connection setup
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/snapdog", {
        headers: { "X-API-Key": "your-api-key" }
    })
    .build();

// Event handlers
connection.on("ZoneVolumeChanged", (data) => {
    console.log(`Zone ${data.zoneIndex} volume: ${data.volume}`);
    updateVolumeSlider(data.zoneIndex, data.volume);
});

connection.on("ZonePlaybackChanged", (data) => {
    console.log(`Zone ${data.zoneIndex} playback: ${data.state}`);
    updatePlayButton(data.zoneIndex, data.state);
});

// Start connection
await connection.start();
```

### 11a.7.2. C# Client
```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/snapdog", options => {
        options.Headers.Add("X-API-Key", "your-api-key");
    })
    .Build();

connection.On<object>("ZoneVolumeChanged", (data) => {
    // Handle volume change
});

await connection.StartAsync();
```

## 11a.8. Performance Considerations

### 11a.8.1. Event Frequency
- **High Frequency**: Track progress updates (every second during playback)
- **Medium Frequency**: Volume changes, playback state changes
- **Low Frequency**: Zone assignments, client connections, system status

### 11a.8.2. Bandwidth Optimization
- Minimal payload sizes (only essential data)
- No redundant information in events
- Efficient JSON serialization

### 11a.8.3. Connection Management
- Automatic reconnection handling
- Connection cleanup on client disconnect
- Scalable to multiple concurrent clients

## 11a.9. Error Handling

### 11a.9.1. Connection Errors
- Authentication failures return 401 Unauthorized
- Network issues trigger automatic reconnection
- Hub exceptions are logged but don't disconnect clients

### 11a.9.2. Event Delivery
- Events are fire-and-forget (no acknowledgment required)
- Failed deliveries are logged but don't block other clients
- No event queuing for disconnected clients

## 11a.10. Future Enhancements

### 11a.10.1. Selective Subscriptions
Future versions may support client-specific event subscriptions to reduce bandwidth for clients interested in specific zones or event types.

### 11a.10.2. Event History
Potential addition of recent event history for newly connected clients to synchronize state without full API calls.

### 11a.10.3. Bidirectional Commands
While currently server-to-client only, future versions may support client-to-server command execution via SignalR methods.
