# 25. SnapDog2 Web UI — Separate Services + AI-Optimized Stack (v2)

**Goal**: Ship a modern, reliable audio control UI using separate frontend/backend services with explicit REST calls and realtime SignalR. Optimized for AI code generation with React + TypeScript.

## 25.1. Scope & Principles

- **Separate services**: Frontend (React SPA) and backend (.NET API) as independent containers
- **Explicit HTTP only**: UI never calls aggregate/list endpoints for state. All reads/writes are explicit (e.g., PUT /zones/{zoneIndex}/volume)
- **Realtime first**: State hydration and live UX come from SignalR hub, not from "read-all" HTTP
- **AI-friendly stack**: React + TypeScript + Vite + Tailwind for optimal AI code generation
- **Standard patterns**: Conventional React structure for predictable AI output
- **Container orchestration**: Docker Compose for development and production

## 25.2. High-Level Architecture

```plaintext
Development:
[ Frontend Container (React + Vite) :5173 ]  ← Hot reload, proxy to backend
[ Backend Container (.NET API) :5000 ]       ← SignalR hub, REST endpoints
[ Caddy Reverse Proxy :8000 ]               ← Single entry point

Production:
[ Frontend Container (Nginx + React SPA) ]   ← Static files, optimized build
[ Backend Container (.NET API) ]             ← Same as development
[ Caddy Reverse Proxy :80 ]                 ← SSL termination, routing
```

## 25.3. Interaction Model

**Initial paint**:

1. UI connects to SignalR.
2. UI calls SubscribeAllZones() or (preferred) receives ZonesIndexV1([ids]), then calls SubscribeZone(id) per visible zone.
3. Server pushes a compact ZoneSnapshotV1 for each subscribed zone (only primitives and short strings).

**Live updates**: Server pushes deltas (progress, track change, control change, client status).

**User actions**: UI calls explicit REST (e.g., POST /zones/{zoneIndex}/play) and updates state optimistically. Hub echo confirms/reconciles.

## 25.4. Public Contracts

### 25.4.1. Current SignalR Implementation

**Existing Hub (SnapDogHub)**:

```csharp
public class SnapDogHub : Hub
{
    public async Task JoinZoneGroup(int zoneIndex)
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task LeaveZoneGroup(int zoneIndex)
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task JoinClientGroup(int clientIndex)
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task LeaveClientGroup(int clientIndex)
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task JoinSystemGroup()
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, "system");
    }

    public async Task LeaveSystemGroup()
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, "system");
    }
}
```

**Existing Notification Types**:

```csharp
// Zone notifications
[StatusId("TRACK_PROGRESS_STATUS")]
public record ZoneProgressChangedNotification(int ZoneIndex, long Position, float Progress) : INotification;

[StatusId("PLAYBACK_STATE")]
public record ZonePlaybackChangedNotification(int ZoneIndex, string PlaybackState) : INotification;

[StatusId("TRACK_METADATA")]
public record ZoneTrackMetadataChangedNotification(int ZoneIndex, TrackInfo Track) : INotification;

[StatusId("VOLUME_STATUS")]
public record ZoneVolumeChangedNotification(int ZoneIndex, int Volume) : INotification;

[StatusId("MUTE_STATUS")]
public record ZoneMuteChangedNotification(int ZoneIndex, bool Muted) : INotification;

[StatusId("TRACK_REPEAT_STATUS")]
public record ZoneRepeatModeChangedNotification(int ZoneIndex, bool TrackRepeat, bool PlaylistRepeat) : INotification;

[StatusId("PLAYLIST_SHUFFLE_STATUS")]
public record ZoneShuffleChangedNotification(int ZoneIndex, bool Shuffle) : INotification;

[StatusId("PLAYLIST_STATUS")]
public record ZonePlaylistChangedNotification(int ZoneIndex, PlaylistInfo? Playlist) : INotification;

// Client notifications
[StatusId("CLIENT_CONNECTED")]
public record ClientConnectedNotification(int ClientIndex, bool Connected) : INotification;

[StatusId("CLIENT_ZONE_STATUS")]
public record ClientZoneChangedNotification(int ClientIndex, int? ZoneIndex) : INotification;

[StatusId("CLIENT_VOLUME_STATUS")]
public record ClientVolumeChangedNotification(int ClientIndex, int Volume) : INotification;

[StatusId("CLIENT_MUTE_STATUS")]
public record ClientMuteChangedNotification(int ClientIndex, bool Muted) : INotification;

[StatusId("CLIENT_LATENCY_STATUS")]
public record ClientLatencyChangedNotification(int ClientIndex, int LatencyMs) : INotification;

// System notifications
[StatusId("SYSTEM_ERROR")]
public record ErrorOccurredNotification(string ErrorCode, string Message, string? Context = null) : INotification;

[StatusId("SYSTEM_STATUS")]
public record SystemStatusChangedNotification(SystemStatus Status) : INotification;
```

**TrackInfo Model**:

```csharp
public record TrackInfo
{
    public int? Index { get; init; }
    public required string Title { get; init; }
    public required string Artist { get; init; }
    public string? Album { get; init; }
    public long? DurationMs { get; init; }
    public long? PositionMs { get; init; }
    public float? Progress { get; init; }
    public string? CoverArtUrl { get; init; }
    public string? Genre { get; init; }
    public int? TrackNumber { get; init; }
    public int? Year { get; init; }
    public float? Rating { get; init; }

    public required string Source { get; init; }
    public required string Url { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
```

### 25.4.2. Required SignalR Handler Implementation

**Missing: Notification handlers that emit to SignalR clients**. Current notifications exist but aren't sent to connected clients. Need to implement:

```csharp
public class SignalRNotificationHandler :
    INotificationHandler<ZoneProgressChangedNotification>,
    INotificationHandler<ZoneTrackMetadataChangedNotification>,
    INotificationHandler<ZoneVolumeChangedNotification>,
    INotificationHandler<ClientZoneChangedNotification>,
    // ... other notifications
{
    private readonly IHubContext<SnapDogHub> _hubContext;

    public async Task Handle(ZoneProgressChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
            .SendAsync("ZoneProgressChanged", notification.ZoneIndex, notification.Position, notification.Progress, cancellationToken);
    }

    // ... other handlers
}
```

### 25.4.3. Explicit REST Endpoints (UI calls only these)

**Current API endpoints from ZonesController and ClientsController**:

**Zones – transport & controls**:

- POST /api/v1/zones/{zoneIndex}/play
- POST /api/v1/zones/{zoneIndex}/pause
- POST /api/v1/zones/{zoneIndex}/stop
- POST /api/v1/zones/{zoneIndex}/next
- POST /api/v1/zones/{zoneIndex}/previous
- PUT  /api/v1/zones/{zoneIndex}/track/position (body: long positionMs)
- PUT  /api/v1/zones/{zoneIndex}/track/progress (body: float progress)
- PUT  /api/v1/zones/{zoneIndex}/volume (body: int volume)
- PUT  /api/v1/zones/{zoneIndex}/mute (body: bool muted)
- POST /api/v1/zones/{zoneIndex}/mute/toggle
- PUT  /api/v1/zones/{zoneIndex}/shuffle (body: bool enabled)
- POST /api/v1/zones/{zoneIndex}/shuffle/toggle
- PUT  /api/v1/zones/{zoneIndex}/repeat (body: bool enabled) - playlist repeat
- POST /api/v1/zones/{zoneIndex}/repeat/toggle
- PUT  /api/v1/zones/{zoneIndex}/repeat/track (body: bool enabled) - track repeat
- POST /api/v1/zones/{zoneIndex}/repeat/track/toggle
- PUT  /api/v1/zones/{zoneIndex}/playlist (body: int playlistIndex)
- PUT  /api/v1/zones/{zoneIndex}/track (body: int trackIndex)
- POST /api/v1/zones/{zoneIndex}/control (body: string command) - unified control

**Clients – assignment & basics**:

- PUT  /api/v1/clients/{clientIndex}/zone (body: int zoneIndex)
- PUT  /api/v1/clients/{clientIndex}/volume (body: int volume)
- PUT  /api/v1/clients/{clientIndex}/mute (body: bool muted)
- POST /api/v1/clients/{clientIndex}/mute/toggle
- POST /api/v1/clients/{clientIndex}/volume/up?step=5
- POST /api/v1/clients/{clientIndex}/volume/down?step=5

**Scalar reads (when UI needs specific values)**:

- GET  /api/v1/zones/count (return int ZoneCount)
- GET  /api/v1/zones/{zoneIndex}/name
- GET  /api/v1/zones/{zoneIndex}/volume
- GET  /api/v1/zones/{zoneIndex}/mute
- GET  /api/v1/zones/{zoneIndex}/shuffle
- GET  /api/v1/zones/{zoneIndex}/repeat
- GET  /api/v1/zones/{zoneIndex}/repeat/track
- GET  /api/v1/zones/{zoneIndex}/playback
- GET  /api/v1/zones/{zoneIndex}/track/title
- GET  /api/v1/zones/{zoneIndex}/track/artist
- GET  /api/v1/zones/{zoneIndex}/track/album
- GET  /api/v1/zones/{zoneIndex}/track/cover
- GET  /api/v1/zones/{zoneIndex}/track/duration
- GET  /api/v1/zones/{zoneIndex}/track/position
- GET  /api/v1/zones/{zoneIndex}/track/progress
- GET  /api/v1/zones/{zoneIndex}/track/playing
- GET  /api/v1/zones/{zoneIndex}/track/metadata (returns TrackInfo)
- GET  /api/v1/zones/{zoneIndex}/track (returns int TrackIndex)
- GET  /api/v1/zones/{zoneIndex}/playlist (returns int PlaylistIndex)
- GET  /api/v1/zones/{zoneIndex}/playlist/name
- GET  /api/v1/zones/{zoneIndex}/playlist/count
- GET  /api/v1/zones/{zoneIndex}/playlist/info (returns PlaylistInfo)
- GET  /api/v1/clients/count (returns int ClientCount)
- GET  /api/v1/clients/{clientIndex}/name
- GET  /api/v1/clients/{clientIndex}/volume
- GET  /api/v1/clients/{clientIndex}/mute
- GET  /api/v1/clients/{clientIndex}/zone

**Key differences from original blueprint**:

- Uses `zoneIndex` and `clientIndex` (1-based) instead of `zoneIndex`/`clientId`
- Separate track repeat vs playlist repeat endpoints
- Unified control endpoint for blueprint commands
- All endpoints return 202 Accepted for async operations
- Scalar GET endpoints for individual properties

## 25.5. Server Implementation (C#)

### 25.5.1. Current Hub Implementation

```csharp
// Already implemented in SnapDog2/Api/Hubs/SnapDogHub.cs
public class SnapDogHub : Hub
{
    public async Task JoinZone(int zoneIndex)
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task LeaveZone(int zoneIndex)
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"zone_{zoneIndex}");
    }

    public async Task JoinClient(int clientIndex)
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task LeaveClient(int clientIndex)
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, $"client_{clientIndex}");
    }

    public async Task JoinSystem()
    {
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, "system");
    }

    public async Task LeaveSystem()
    {
        await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, "system");
    }
}
```

### 25.5.2. SignalR Notification Handlers

**✅ Implemented**: SignalR notification handlers that emit existing notifications to connected clients:

```csharp
// Implemented in SnapDog2/Api/Hubs/Handlers/SignalRNotificationHandler.cs
public partial class SignalRNotificationHandler :
    INotificationHandler<ZoneProgressChangedNotification>,
    INotificationHandler<ZoneTrackMetadataChangedNotification>,
    INotificationHandler<ZoneVolumeChangedNotification>,
    INotificationHandler<ZonePlaybackChangedNotification>,
    INotificationHandler<ClientZoneChangedNotification>,
    INotificationHandler<ClientConnectedNotification>,
    INotificationHandler<ErrorOccurredNotification>
    // ... and all other notification types
{
    public async Task Handle(ZoneProgressChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
            .SendAsync("ZoneProgressChanged", notification.ZoneIndex, notification.Position, notification.Progress, cancellationToken);
    }

    // ... other handlers emit to appropriate groups
}
```

**Auto-registration**: Handlers are automatically discovered and registered via `AddCommandProcessing()` in Program.cs.

### 25.5.3. Current Program.cs Configuration

```csharp
// Already implemented in Program.cs
builder.Services.AddSignalR();

// Hub mapping
app.MapHub<SnapDogHub>("/hubs/snapdog/v1");

// CORS for development (frontend container)
if (app.Environment.IsDevelopment())
{
    app.UseCors(policy => policy
        .WithOrigins("http://localhost:5173", "http://frontend:5173")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Map REST controllers
app.MapControllers();
```

## 25.6. Frontend Implementation (React + TypeScript)

### 25.6.1. Project Layout (AI-Optimized)

```plaintext
SnapDog2.WebUI/                    # Frontend service (separate container)
  src/
    components/
      ZoneCard.tsx                 # Zone control component
      ClientChip.tsx               # Draggable client component
      VolumeSlider.tsx             # Volume control
      TransportControls.tsx        # Play/pause/next/prev
      PlaylistSelector.tsx         # Playlist dropdown
    hooks/
      useSignalR.ts               # SignalR connection hook
      useZoneState.ts             # Zone state management
      useClientState.ts           # Client state management
    services/
      api.ts                      # REST API wrapper
      signalr.ts                  # SignalR service
    store/
      index.ts                    # Zustand store
      types.ts                    # TypeScript interfaces
    App.tsx                       # Main app component
    main.tsx                      # React entry point
  public/
    fonts/                        # Local fonts
    icons/                        # SVG icons
  package.json                    # Dependencies
  vite.config.ts                  # Standard Vite config
  tailwind.config.js              # Tailwind CSS config
  tsconfig.json                   # TypeScript config
  Dockerfile                      # Production container
```

### 25.6.2. SignalR Hook (React Pattern)

```typescript
// src/hooks/useSignalR.ts
import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { useAppStore } from '../store';

export function useSignalR(baseUrl: string = '') {
  const connectionRef = useRef<HubConnection | null>(null);
  const {
    updateZoneProgress,
    updateZoneTrack,
    updateZoneVolume,
    updateZonePlayback,
    updateZoneMute,
    updateZoneRepeat,
    updateZoneShuffle,
    updateZonePlaylist,
    updateClientConnection,
    updateClientZone,
    updateClientVolume,
    updateClientMute,
    updateClientLatency,
  } = useAppStore();

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/snapdog/v1`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    // Zone event handlers
    connection.on('ZoneProgressChanged', (zoneIndex: number, position: number, progress: number) => {
      updateZoneProgress(zoneIndex, { position, progress });
    });

    connection.on('ZoneTrackMetadataChanged', (zoneIndex: number, track: any) => {
      updateZoneTrack(zoneIndex, track);
    });

    connection.on('ZoneVolumeChanged', (zoneIndex: number, volume: number) => {
      updateZoneVolume(zoneIndex, volume);
    });

    connection.on('ZonePlaybackChanged', (zoneIndex: number, playbackState: string) => {
      updateZonePlayback(zoneIndex, playbackState);
    });

    connection.on('ZoneMuteChanged', (zoneIndex: number, muted: boolean) => {
      updateZoneMute(zoneIndex, muted);
    });

    connection.on('ZoneRepeatModeChanged', (zoneIndex: number, trackRepeat: boolean, playlistRepeat: boolean) => {
      updateZoneRepeat(zoneIndex, trackRepeat, playlistRepeat);
    });

    connection.on('ZoneShuffleChanged', (zoneIndex: number, shuffle: boolean) => {
      updateZoneShuffle(zoneIndex, shuffle);
    });

    connection.on('ZonePlaylistChanged', (zoneIndex: number, playlistIndex: number, playlistName: string) => {
      updateZonePlaylist(zoneIndex, playlistIndex, playlistName);
    });

    // Client event handlers
    connection.on('ClientConnected', (clientIndex: number, connected: boolean) => {
      updateClientConnection(clientIndex, connected);
    });

    connection.on('ClientZoneChanged', (clientIndex: number, zoneIndex?: number) => {
      updateClientZone(clientIndex, zoneIndex);
    });

    connection.on('ClientVolumeChanged', (clientIndex: number, volume: number) => {
      updateClientVolume(clientIndex, volume);
    });

    connection.on('ClientMuteChanged', (clientIndex: number, muted: boolean) => {
      updateClientMute(clientIndex, muted);
    });

    connection.on('ClientLatencyChanged', (clientIndex: number, latency: number) => {
      updateClientLatency(clientIndex, latency);
    });

    // System event handlers
    connection.on('ErrorOccurred', (errorCode: string, message: string, context?: string) => {
      console.error(`System error ${errorCode}: ${message}`, context);
    });

    connection.on('SystemStatusChanged', (status: any) => {
      console.log('System status changed:', status);
    });

    const startConnection = async () => {
      try {
        await connection.start();
        await connection.invoke('JoinSystem');
        console.log('SignalR connected');
      } catch (error) {
        console.error('SignalR connection failed:', error);
        setTimeout(startConnection, 5000);
      }
    };

    startConnection();
    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [baseUrl, updateZoneProgress, updateZoneTrack, updateZoneVolume, updateZonePlayback]);

  return {
    joinZone: (zoneIndex: number) => connectionRef.current?.invoke('JoinZone', zoneIndex),
    leaveZone: (zoneIndex: number) => connectionRef.current?.invoke('LeaveZone', zoneIndex),
    joinClient: (clientIndex: number) => connectionRef.current?.invoke('JoinClient', clientIndex),
    leaveClient: (clientIndex: number) => connectionRef.current?.invoke('LeaveClient', clientIndex),
    joinSystem: () => connectionRef.current?.invoke('JoinSystem'),
    leaveSystem: () => connectionRef.current?.invoke('LeaveSystem'),
  };
}
```

### 25.6.3. API Service (Explicit REST)

```typescript
// src/services/api.ts
const BASE_URL = '/api/v1';

class ApiService {
  private async request(method: string, path: string, body?: unknown) {
    const response = await fetch(`${BASE_URL}${path}`, {
      method,
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined,
      credentials: 'same-origin',
    });

    if (!response.ok) {
      throw new Error(`${method} ${path} -> ${response.status}`);
    }

    return response;
  }

  // Zone controls
  zones = {
    play: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/play`),
    pause: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/pause`),
    stop: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/stop`),
    next: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/next`),
    previous: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/previous`),
    setVolume: (zoneIndex: number, volume: number) =>
      this.request('PUT', `/zones/${zoneIndex}/volume`, { volume }),
    toggleMute: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/mute/toggle`),
    toggleShuffle: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/shuffle/toggle`),
    setRepeat: (zoneIndex: number, enabled: boolean) =>
      this.request('PUT', `/zones/${zoneIndex}/repeat`, { enabled }),
    setTrackRepeat: (zoneIndex: number, enabled: boolean) =>
      this.request('PUT', `/zones/${zoneIndex}/repeat/track`, { enabled }),
    setPlaylist: (zoneIndex: number, playlistIndex: number) =>
      this.request('PUT', `/zones/${zoneIndex}/playlist`, { playlistIndex }),
    setTrack: (zoneIndex: number, trackIndex: number) =>
      this.request('PUT', `/zones/${zoneIndex}/track`, { trackIndex }),
    seekPosition: (zoneIndex: number, positionMs: number) =>
      this.request('PUT', `/zones/${zoneIndex}/track/position`, { positionMs }),
  };

  // Client controls
  clients = {
    assignZone: (clientIndex: number, zoneIndex: number) =>
      this.request('PUT', `/clients/${clientIndex}/zone`, { zoneIndex }),
    setVolume: (clientIndex: number, volume: number) =>
      this.request('PUT', `/clients/${clientIndex}/volume`, { volume }),
    toggleMute: (clientIndex: number) => this.request('POST', `/clients/${clientIndex}/mute/toggle`),
    volumeUp: (clientIndex: number, step = 5) =>
      this.request('POST', `/clients/${clientIndex}/volume/up?step=${step}`),
    volumeDown: (clientIndex: number, step = 5) =>
      this.request('POST', `/clients/${clientIndex}/volume/down?step=${step}`),
  };

  // Scalar reads (when needed)
  get = {
    zoneCount: () => this.request('GET', '/zones/count').then(r => r.json()),
    clientCount: () => this.request('GET', '/clients/count').then(r => r.json()),
    zoneMetadata: (zoneIndex: number) =>
      this.request('GET', `/zones/${zoneIndex}/track/metadata`).then(r => r.json()),
    zonePlaylist: (zoneIndex: number) =>
      this.request('GET', `/zones/${zoneIndex}/playlist/info`).then(r => r.json()),
  };
}

export const api = new ApiService();
```

### 25.6.4. State Management (Zustand)

```typescript
// src/store/types.ts
export interface TrackInfo {
  index?: number;
  title: string;
  artist: string;
  album?: string;
  durationMs?: number;
  positionMs?: number;
  progress?: number;
  coverArtUrl?: string;

  source: string;
  url: string;
}

export interface ZoneState {
  volume: number;
  muted: boolean;
  shuffle: boolean;
  trackRepeat: boolean;
  playlistRepeat: boolean;
  playbackState: 'playing' | 'paused' | 'stopped';
  currentTrack?: TrackInfo;
  progress?: { position: number; progress: number };
  playlistIndex?: number;
  playlistName?: string;
  clients: number[];
}

export interface ClientState {
  connected: boolean;
  zoneIndex?: number;
  volume: number;
  muted: boolean;
  latency?: number;
}

// src/store/index.ts
import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import type { TrackInfo, ZoneState, ClientState } from './types';

interface AppState {
  zones: Record<number, ZoneState>;
  clients: Record<number, ClientState>;

  // Zone actions
  updateZoneProgress: (zoneIndex: number, progress: { position: number; progress: number }) => void;
  updateZoneTrack: (zoneIndex: number, track: TrackInfo) => void;
  updateZoneVolume: (zoneIndex: number, volume: number) => void;
  updateZonePlayback: (zoneIndex: number, playbackState: string) => void;
  updateZoneMute: (zoneIndex: number, muted: boolean) => void;
  updateZoneRepeat: (zoneIndex: number, trackRepeat: boolean, playlistRepeat: boolean) => void;
  updateZoneShuffle: (zoneIndex: number, shuffle: boolean) => void;
  updateZonePlaylist: (zoneIndex: number, playlistIndex: number, playlistName: string) => void;

  // Client actions
  updateClientConnection: (clientIndex: number, connected: boolean) => void;
  updateClientZone: (clientIndex: number, zoneIndex?: number) => void;
  updateClientVolume: (clientIndex: number, volume: number) => void;
  updateClientMute: (clientIndex: number, muted: boolean) => void;
  updateClientLatency: (clientIndex: number, latency: number) => void;

  // Utility actions
  initializeZone: (zoneIndex: number) => void;
  initializeClient: (clientIndex: number) => void;
}

const defaultZoneState: ZoneState = {
  volume: 0,
  muted: false,
  shuffle: false,
  trackRepeat: false,
  playlistRepeat: false,
  playbackState: 'stopped',
  clients: [],
};

const defaultClientState: ClientState = {
  connected: false,
  volume: 0,
  muted: false,
};

export const useAppStore = create<AppState>()(
  devtools(
    (set, get) => ({
      zones: {},
      clients: {},

      // Zone actions
      updateZoneProgress: (zoneIndex, progress) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              progress
            },
          },
        }), false, 'updateZoneProgress'),

      updateZoneTrack: (zoneIndex, track) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              currentTrack: track
            },
          },
        }), false, 'updateZoneTrack'),

      updateZoneVolume: (zoneIndex, volume) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              volume
            },
          },
        }), false, 'updateZoneVolume'),

      updateZonePlayback: (zoneIndex, playbackState) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              playbackState: playbackState as any
            },
          },
        }), false, 'updateZonePlayback'),

      updateZoneMute: (zoneIndex, muted) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              muted
            },
          },
        }), false, 'updateZoneMute'),

      updateZoneRepeat: (zoneIndex, trackRepeat, playlistRepeat) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              trackRepeat,
              playlistRepeat
            },
          },
        }), false, 'updateZoneRepeat'),

      updateZoneShuffle: (zoneIndex, shuffle) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              shuffle
            },
          },
        }), false, 'updateZoneShuffle'),

      updateZonePlaylist: (zoneIndex, playlistIndex, playlistName) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              playlistIndex,
              playlistName
            },
          },
        }), false, 'updateZonePlaylist'),

      // Client actions
      updateClientConnection: (clientIndex, connected) =>
        set((state) => ({
          clients: {
            ...state.clients,
            [clientIndex]: {
              ...state.clients[clientIndex] || defaultClientState,
              connected
            },
          },
        }), false, 'updateClientConnection'),

      updateClientZone: (clientIndex, zoneIndex) => {
        set((state) => {
          // Remove client from all zones
          const updatedZones = { ...state.zones };
          Object.keys(updatedZones).forEach(zId => {
            updatedZones[parseInt(zId)] = {
              ...updatedZones[parseInt(zId)],
              clients: updatedZones[parseInt(zId)].clients.filter(c => c !== clientIndex)
            };
          });

          // Add client to new zone if specified
          if (zoneIndex !== undefined) {
            updatedZones[zoneIndex] = {
              ...updatedZones[zoneIndex] || defaultZoneState,
              clients: [...(updatedZones[zoneIndex]?.clients || []), clientIndex]
            };
          }

          return {
            zones: updatedZones,
            clients: {
              ...state.clients,
              [clientIndex]: {
                ...state.clients[clientIndex] || defaultClientState,
                zoneIndex
              },
            },
          };
        }, false, 'updateClientZone');
      },

      updateClientVolume: (clientIndex, volume) =>
        set((state) => ({
          clients: {
            ...state.clients,
            [clientIndex]: {
              ...state.clients[clientIndex] || defaultClientState,
              volume
            },
          },
        }), false, 'updateClientVolume'),

      updateClientMute: (clientIndex, muted) =>
        set((state) => ({
          clients: {
            ...state.clients,
            [clientIndex]: {
              ...state.clients[clientIndex] || defaultClientState,
              muted
            },
          },
        }), false, 'updateClientMute'),

      updateClientLatency: (clientIndex, latency) =>
        set((state) => ({
          clients: {
            ...state.clients,
            [clientIndex]: {
              ...state.clients[clientIndex] || defaultClientState,
              latency
            },
          },
        }), false, 'updateClientLatency'),

      // Utility actions
      initializeZone: (zoneIndex) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: state.zones[zoneIndex] || defaultZoneState,
          },
        }), false, 'initializeZone'),

      initializeClient: (clientIndex) =>
        set((state) => ({
          clients: {
            ...state.clients,
            [clientIndex]: state.clients[clientIndex] || defaultClientState,
          },
        }), false, 'initializeClient'),
    }),
    { name: 'snapdog-store' }
  )
);

// Convenience selectors
export const useZone = (zoneIndex: number) =>
  useAppStore((state) => state.zones[zoneIndex]);

export const useClient = (clientIndex: number) =>
  useAppStore((state) => state.clients[clientIndex]);

export const useZoneClients = (zoneIndex: number) =>
  useAppStore((state) => state.zones[zoneIndex]?.clients || []);
```

### 25.6.5. React Components (Enterprise-Grade)

```typescript
// src/components/ZoneCard.tsx
import React, { useEffect } from 'react';
import { useZone, useZoneClients, useAppStore } from '../store';
import { useSignalR } from '../hooks/useSignalR';
import { api } from '../services/api';
import { TransportControls } from './TransportControls';
import { VolumeSlider } from './VolumeSlider';
import { ClientList } from './ClientList';
import { PlaylistSelector } from './PlaylistSelector';

interface ZoneCardProps {
  zoneIndex: number;
  className?: string;
}

export function ZoneCard({ zoneIndex, className = '' }: ZoneCardProps) {
  const zone = useZone(zoneIndex);
  const clients = useZoneClients(zoneIndex);
  const initializeZone = useAppStore((state) => state.initializeZone);
  const { joinZone } = useSignalR();

  useEffect(() => {
    initializeZone(zoneIndex);
    joinZone(zoneIndex);
  }, [zoneIndex, initializeZone, joinZone]);

  const handleVolumeChange = async (volume: number) => {
    try {
      await api.zones.setVolume(zoneIndex, volume);
    } catch (error) {
      console.error('Failed to set zone volume:', error);
    }
  };

  const handleMuteToggle = async () => {
    try {
      await api.zones.toggleMute(zoneIndex);
    } catch (error) {
      console.error('Failed to toggle zone mute:', error);
    }
  };

  if (!zone) {
    return (
      <div className={`bg-gray-100 rounded-lg p-6 animate-pulse ${className}`}>
        <div className="h-6 bg-gray-300 rounded mb-4"></div>
        <div className="h-4 bg-gray-300 rounded mb-2"></div>
        <div className="h-4 bg-gray-300 rounded w-3/4"></div>
      </div>
    );
  }

  return (
    <div className={`bg-white rounded-lg shadow-md p-6 border border-gray-200 ${className}`}>
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900">Zone {zoneIndex}</h3>
        <div className="flex items-center space-x-2">
          <div className={`w-2 h-2 rounded-full ${zone.playbackState === 'playing' ? 'bg-green-500' : 'bg-gray-400'}`} />
          <span className="text-sm text-gray-500 capitalize">{zone.playbackState}</span>
        </div>
      </div>

      {zone.currentTrack && (
        <div className="mb-6 p-4 bg-gray-50 rounded-lg">
          <div className="flex items-start space-x-4">
            {zone.currentTrack.coverArtUrl && (
              <img
                src={zone.currentTrack.coverArtUrl}
                alt="Album cover"
                className="w-16 h-16 rounded-md object-cover"
              />
            )}
            <div className="flex-1 min-w-0">
              <p className="font-medium text-gray-900 truncate">{zone.currentTrack.title}</p>
              <p className="text-gray-600 truncate">{zone.currentTrack.artist}</p>
              {zone.currentTrack.album && (
                <p className="text-sm text-gray-500 truncate">{zone.currentTrack.album}</p>
              )}
            </div>
          </div>

          {zone.progress && zone.currentTrack.durationMs && (
            <div className="mt-3">
              <div className="flex justify-between text-xs text-gray-500 mb-1">
                <span>{formatTime(zone.progress.position)}</span>
                <span>{formatTime(zone.currentTrack.durationMs)}</span>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-1">
                <div
                  className="bg-blue-600 h-1 rounded-full transition-all duration-300"
                  style={{ width: `${zone.progress.progress * 100}%` }}
                />
              </div>
            </div>
          )}
        </div>
      )}

      <div className="space-y-4">
        <TransportControls zoneIndex={zoneIndex} />

        <div className="flex items-center space-x-4">
          <VolumeSlider
            value={zone.volume}
            muted={zone.muted}
            onChange={handleVolumeChange}
            onMuteToggle={handleMuteToggle}
            className="flex-1"
          />
        </div>

        <PlaylistSelector
          zoneIndex={zoneIndex}
          currentPlaylistIndex={zone.playlistIndex}
          currentPlaylistName={zone.playlistName}
        />

        <div>
          <h4 className="text-sm font-medium text-gray-700 mb-2">
            Clients ({clients.length})
          </h4>
          <ClientList zoneIndex={zoneIndex} clientIndices={clients} />
        </div>
      </div>
    </div>
  );
}

// src/components/ClientChip.tsx
import React from 'react';
import { useClient, useAppStore } from '../store';
import { api } from '../services/api';
import { VolumeSlider } from './VolumeSlider';

interface ClientChipProps {
  clientIndex: number;
  isDragging?: boolean;
  onDragStart?: (clientIndex: number) => void;
  onDragEnd?: () => void;
  className?: string;
}

export function ClientChip({
  clientIndex,
  isDragging = false,
  onDragStart,
  onDragEnd,
  className = ''
}: ClientChipProps) {
  const client = useClient(clientIndex);
  const initializeClient = useAppStore((state) => state.initializeClient);

  React.useEffect(() => {
    initializeClient(clientIndex);
  }, [clientIndex, initializeClient]);

  const handleVolumeChange = async (volume: number) => {
    try {
      await api.clients.setVolume(clientIndex, volume);
    } catch (error) {
      console.error('Failed to set client volume:', error);
    }
  };

  const handleMuteToggle = async () => {
    try {
      await api.clients.toggleMute(clientIndex);
    } catch (error) {
      console.error('Failed to toggle client mute:', error);
    }
  };

  const handleDragStart = (e: React.DragEvent) => {
    e.dataTransfer.setData('text/plain', clientIndex.toString());
    e.dataTransfer.effectAllowed = 'move';
    onDragStart?.(clientIndex);
  };

  if (!client) {
    return (
      <div className={`bg-gray-200 rounded-lg p-3 animate-pulse ${className}`}>
        <div className="h-4 bg-gray-300 rounded"></div>
      </div>
    );
  }

  return (
    <div
      className={`
        bg-white border-2 rounded-lg p-3 cursor-move transition-all duration-200
        ${client.connected ? 'border-green-200 bg-green-50' : 'border-gray-200 bg-gray-50'}
        ${isDragging ? 'opacity-50 scale-95' : 'hover:shadow-md'}
        ${className}
      `}
      draggable
      onDragStart={handleDragStart}
      onDragEnd={onDragEnd}
    >
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center space-x-2">
          <div className={`w-2 h-2 rounded-full ${client.connected ? 'bg-green-500' : 'bg-red-500'}`} />
          <span className="text-sm font-medium text-gray-900">Client {clientIndex}</span>
        </div>
        {client.latency !== undefined && (
          <span className="text-xs text-gray-500">{client.latency}ms</span>
        )}
      </div>

      <VolumeSlider
        value={client.volume}
        muted={client.muted}
        onChange={handleVolumeChange}
        onMuteToggle={handleMuteToggle}
        size="sm"
        showLabel={false}
      />
    </div>
  );
}

// src/components/VolumeSlider.tsx
import React from 'react';
import { VolumeX, Volume2 } from 'lucide-react';

interface VolumeSliderProps {
  value: number;
  muted: boolean;
  onChange: (value: number) => void;
  onMuteToggle: () => void;
  size?: 'sm' | 'md' | 'lg';
  showLabel?: boolean;
  className?: string;
}

export function VolumeSlider({
  value,
  muted,
  onChange,
  onMuteToggle,
  size = 'md',
  showLabel = true,
  className = ''
}: VolumeSliderProps) {
  const sizeClasses = {
    sm: 'h-1',
    md: 'h-2',
    lg: 'h-3'
  };

  const iconSizes = {
    sm: 16,
    md: 20,
    lg: 24
  };

  return (
    <div className={`flex items-center space-x-3 ${className}`}>
      <button
        onClick={onMuteToggle}
        className="p-1 rounded-md hover:bg-gray-100 transition-colors"
        aria-label={muted ? 'Unmute' : 'Mute'}
      >
        {muted ? (
          <VolumeX size={iconSizes[size]} className="text-red-500" />
        ) : (
          <Volume2 size={iconSizes[size]} className="text-gray-600" />
        )}
      </button>

      <div className="flex-1 flex items-center space-x-2">
        <input
          type="range"
          min="0"
          max="100"
          value={muted ? 0 : value}
          onChange={(e) => onChange(parseInt(e.target.value))}
          disabled={muted}
          className={`
            flex-1 appearance-none bg-gray-200 rounded-full ${sizeClasses[size]}
            disabled:opacity-50 cursor-pointer
            [&::-webkit-slider-thumb]:appearance-none
            [&::-webkit-slider-thumb]:w-4
            [&::-webkit-slider-thumb]:h-4
            [&::-webkit-slider-thumb]:rounded-full
            [&::-webkit-slider-thumb]:bg-blue-600
            [&::-webkit-slider-thumb]:cursor-pointer
            [&::-moz-range-thumb]:w-4
            [&::-moz-range-thumb]:h-4
            [&::-moz-range-thumb]:rounded-full
            [&::-moz-range-thumb]:bg-blue-600
            [&::-moz-range-thumb]:border-0
            [&::-moz-range-thumb]:cursor-pointer
          `}
        />

        {showLabel && (
          <span className="text-sm text-gray-600 w-8 text-right">
            {muted ? '0' : value}
          </span>
        )}
      </div>
    </div>
  );
}

// Utility function
function formatTime(ms: number): string {
  const seconds = Math.floor(ms / 1000);
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
}
```

## 25.7. Container Setup

### 25.7.1. Frontend Dockerfile

```dockerfile
# SnapDog2.WebUI/Dockerfile
FROM node:20-alpine AS build

WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production

COPY . .
RUN npm run build

# Production stage
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### 25.7.2. Development Docker Compose

```yaml
# docker-compose.dev.yml (updated section)
services:
  backend:
    build:
      context: .
      dockerfile: Dockerfile
      target: development
    ports:
      - "5000:5000"
    volumes:
      - .:/app:cached
    environment:
      - ASPNETCORE_ENVIRONMENT=Development

  frontend:
    build:
      context: ./SnapDog2.WebUI
      dockerfile: Dockerfile.dev
    ports:
      - "5173:5173"
    volumes:
      - ./SnapDog2.WebUI:/app:cached
      - /app/node_modules
    environment:
      - NODE_ENV=development
    command: npm run dev -- --host 0.0.0.0

  caddy:
    image: caddy:2-alpine
    ports:
      - "8000:80"
    volumes:
      - ./Caddyfile.dev:/etc/caddy/Caddyfile
    depends_on:
      - backend
      - frontend
```

### 25.7.3. Production Docker Compose

```yaml
# docker-compose.prod.yml
services:
  backend:
    build:
      context: .
      dockerfile: Dockerfile
      target: production
    environment:
      - ASPNETCORE_ENVIRONMENT=Production

  frontend:
    build:
      context: ./SnapDog2.WebUI
      dockerfile: Dockerfile

  caddy:
    image: caddy:2-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile.prod:/etc/caddy/Caddyfile
      - caddy_data:/data
      - caddy_config:/config
```

## 25.8. Development & Build Commands

### 25.8.1. Frontend Package.json

```json
{
  "name": "snapdog2-webui",
  "private": true,
  "version": "0.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc && vite build",
    "preview": "vite preview",
    "lint": "eslint . --ext ts,tsx --report-unused-disable-directives --max-warnings 0",
    "type-check": "tsc --noEmit"
  },
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "@microsoft/signalr": "^8.0.0",
    "zustand": "^4.4.0",
    "lucide-react": "^0.294.0"
  },
  "devDependencies": {
    "@types/react": "^18.2.0",
    "@types/react-dom": "^18.2.0",
    "@vitejs/plugin-react": "^4.0.0",
    "typescript": "^5.0.0",
    "vite": "^5.0.0",
    "tailwindcss": "^3.3.0",
    "autoprefixer": "^10.4.0",
    "postcss": "^8.4.0",
    "eslint": "^8.0.0",
    "@typescript-eslint/eslint-plugin": "^6.0.0",
    "@typescript-eslint/parser": "^6.0.0"
  }
}
```

### 25.8.2. Vite Configuration

```typescript
// SnapDog2.WebUI/vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://backend:5000',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://backend:5000',
        changeOrigin: true,
        ws: true,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom'],
          signalr: ['@microsoft/signalr'],
        },
      },
    },
  },
});
```

### 25.8.3. Development Workflow

```bash
# Start all services
docker compose -f docker-compose.dev.yml up -d

# View logs
docker compose -f docker-compose.dev.yml logs -f

# Frontend development (local)
cd SnapDog2.WebUI
npm install
npm run dev

# Backend development (local)
dotnet watch --project SnapDog2/SnapDog2.csproj run

# Access application
open http://localhost:8000
```

### 25.8.4. Production Build & Deploy

```bash
# Build frontend
cd SnapDog2.WebUI && npm run build

# Build and deploy all services
docker compose -f docker-compose.prod.yml up -d --build

# Health check
curl http://localhost/api/health
```

## 25.9. Testing

**Server unit/integration**:

- Verify hub emits ZoneSnapshotV1 on subscribe.
- Throttle logic: assert ≤ 5 progress events/sec.
- REST → hub echo (e.g., calling volume emits ZoneControlsChangedV1).

**E2E (Playwright)**:

- Drag client chip between zones → server receives PUT; UI reconciles on ClientStatusChangedV1.
- Progress bar advances; play/pause changes icons instantly.
- A11y: @axe-core/playwright for violations; tab order & ARIA roles for controls.

## 25.10. Performance & Reliability

- **Backpressure**: coalesce multiple changes within a 50–100 ms window; last-write-wins per zone tick.
- **Reconnect**: on hub reconnect, re-issue SubscribeZone(id) (or SubscribeAllZones) and re-paint from snapshots.
- **Fallback** (only if hub down): poll one or two cheap scalars for the currently visible zone (e.g., /track/position, /playing) every 3–5s. Do not poll lists.

## 25.11. Security & Ops

- **Same-origin serving**: no CORS needed in prod. Dev proxy handles cross-origin during HMR.
- **CSRF for REST**: same-site cookies or antiforgery tokens if authenticated.
- **Auth**: if needed, pass access token to hub via headers/query; validate in OnConnectedAsync.
- **Compression**: enable response compression; long-poll fallback disabled (WebSocket preferred).
- **Observability**: OpenTelemetry traces for REST + custom hub meters (events/sec, connected clients, dropped updates).

## 25.12. Accessibility & UX Notes

- Keyboard action bindings for transport & volume.
- Respect prefers-reduced-motion.
- Progress bar with ARIA slider; announce time changes on seek.
- Color contrast meets WCAG AA in both themes.

## 25.13. Rollout Plan (phased)

1. **P0**: Add SnapDogHub, implement SubscribeZone, fake progress emitter for 1 zone, show in a tiny TS page.
2. **P1**: Real progress/track change emitters; implement ZoneSnapshotV1 and deltas; play/pause/next endpoints wired.
3. **P2**: DnD clients (@dnd-kit) → PUT /clients/{i}/zone; reconcile via ClientStatusChangedV1.
4. **P3**: CI: pnpm build → embed → dotnet publish /p:PublishSingleFile=true.
5. **P4**: E2E + a11y tests; add optional playlist change events if needed.

## 25.14. Validation Checklist

### 25.14.1. Architecture Validation

- ✅ Frontend and backend run as separate containers
- ✅ No UI calls to /zones or /clients aggregates
- ✅ All state updates via SignalR realtime events
- ✅ Explicit REST calls for user actions only
- ✅ Single port access via Caddy reverse proxy

### 25.14.2. Performance Validation

- ✅ TrackProgress events ≤ 5/sec per zone with smooth progress bar
- ✅ Controls update via SignalR deltas (no polling)
- ✅ Optimistic UI updates with server reconciliation
- ✅ Frontend bundle size < 500KB gzipped
- ✅ Initial page load < 2 seconds

### 25.14.3. Functionality Validation

- ✅ Drag & drop client reassignment triggers PUT /clients/{i}/zone
- ✅ Hub echo confirms all user actions
- ✅ SignalR reconnection preserves state
- ✅ All transport controls (play/pause/next/prev) functional
- ✅ Volume and mute controls for zones and clients
- ✅ Playlist selection and track navigation

### 25.14.4. Development Experience

- ✅ Hot reload for both frontend (Vite) and backend (dotnet watch)
- ✅ TypeScript strict mode with zero errors
- ✅ ESLint with zero warnings
- ✅ Zustand DevTools integration
- ✅ Container logs accessible via docker compose logs

### 25.14.5. Production Readiness

- ✅ Frontend builds to optimized static files
- ✅ Backend runs in production container
- ✅ Health check endpoints respond correctly
- ✅ CORS configured for development only
- ✅ Error boundaries handle SignalR disconnections
- ✅ Graceful degradation when services unavailable

## 25.15. Implementation Summary

### 25.15.1. Backend (Already Implemented ✅)

- **SignalR Hub**: SnapDogHub with group management
- **Notification Handlers**: SignalRNotificationHandler emits to clients
- **REST Controllers**: ZonesController, ClientsController with explicit endpoints
- **Program.cs**: Hub mapping, CORS, health checks

### 25.15.2. Frontend (To Implement)

- **React Components**: ZoneCard, ClientChip, VolumeSlider, TransportControls
- **State Management**: Zustand store with TypeScript interfaces
- **SignalR Integration**: useSignalR hook with automatic reconnection
- **API Service**: Explicit REST wrapper with error handling
- **Styling**: Tailwind CSS with responsive design

### 25.15.3. Infrastructure (To Implement)

- **Frontend Dockerfile**: Multi-stage build with Nginx
- **Docker Compose**: Development and production configurations
- **Caddy Configuration**: Reverse proxy with WebSocket support
- **CI/CD Pipeline**: Automated build and deployment

### 25.15.4. Key Files to Create

```plaintext
SnapDog2.WebUI/
├── src/
│   ├── components/ZoneCard.tsx
│   ├── components/ClientChip.tsx
│   ├── components/VolumeSlider.tsx
│   ├── hooks/useSignalR.ts
│   ├── services/api.ts
│   ├── store/index.ts
│   └── store/types.ts
├── package.json
├── vite.config.ts
├── tailwind.config.js
└── Dockerfile

docker-compose.dev.yml    # Development services
docker-compose.prod.yml   # Production services
Caddyfile.dev            # Development proxy
Caddyfile.prod           # Production proxy
```
