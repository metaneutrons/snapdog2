# 25. SnapDog2 Web UI — Explicit REST + Realtime Hub (v1)

**Goal**: Ship a modern, reliable audio control UI with only explicit REST calls (no bulky /zones or /clients lists) and server-push updates via SignalR. Develop like a normal TS app; publish as a single .NET artifact with embedded assets.

## 25.1. Scope & Principles

- **Explicit HTTP only**: UI never calls aggregate/list endpoints for state. All reads/writes are explicit (e.g., PUT /zones/{zoneIndex}/volume).
- **Realtime first**: State hydration and live UX come from the hub, not from "read-all" HTTP.
- **DX vs Ops**: Dev in TypeScript (Vite/HMR). At build time, emit static assets → embed into a .NET Assets project. No Node in production.
- **Single page, minimal routes**: Focused control surface; no complex router needed.
- **No external assets**: Fonts, CSS, icons are embedded as resources.

## 25.2. High-Level Architecture

```plaintext
[ SnapDog2 (monolith, .NET 9) ]
   ├─ REST (explicit actions & scalars)      ← UI uses only small endpoints
   ├─ SignalR Hub (/hubs/snapdog)            ← UI subscribes, gets snapshots+deltas
   ├─ Static Files (embedded)                 ← Built TS assets (index.html, JS, CSS, fonts)
   └─ Audio Engine                            ← Emits events to Hub (throttled)

[ Web UI (TypeScript, React/Vite in dev) ]
   ├─ fetch-wrapper.ts  (only explicit endpoints)
   ├─ signalr.ts        (hub connection, handlers)
   ├─ store.ts          (reducer/query cache with optimistic updates)
   ├─ components/       (ZoneCard, ClientChip, Transport, Volume)
   └─ index.html        (mounts app)
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

[StatusId("ZONE_MUTE_STATUS")]
public record ZoneMuteChangedNotification(int ZoneIndex, bool Muted) : INotification;

[StatusId("TRACK_REPEAT_STATUS")]
public record ZoneRepeatModeChangedNotification(int ZoneIndex, bool TrackRepeat, bool PlaylistRepeat) : INotification;

[StatusId("ZONE_SHUFFLE_STATUS")]
public record ZoneShuffleChangedNotification(int ZoneIndex, bool Shuffled) : INotification;

[StatusId("ZONE_PLAYLIST_STATUS")]
public record ZonePlaylistChangedNotification(int ZoneIndex, int PlaylistIndex, string PlaylistName) : INotification;

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
public record ClientLatencyChangedNotification(int ClientIndex, int Latency) : INotification;

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
    public bool IsPlaying { get; init; }
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

- Uses `zoneIndex` and `clientIndex` (1-based) instead of `zoneId`/`clientId`
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

### 5.3 Current Program.cs Configuration

```csharp
// Already implemented in Program.cs
builder.Services.AddSignalR();

// Hub mapping (line 640)
app.MapHub<SnapDogHub>("/hubs/snapdog/v1");

// Serve embedded static files
var assetsAssembly = typeof(AssetsMarker).Assembly;
var embedded = new ManifestEmbeddedFileProvider(assetsAssembly, "EmbeddedWebRoot");
app.UseStaticFiles(new StaticFileOptions { FileProvider = embedded });

// Map SPA index (hash-router or simple "/")
app.MapGet("/", async ctx =>
{
    var file = embedded.GetFileInfo("index.html");
    ctx.Response.ContentType = "text/html; charset=utf-8";
    await using var s = file.CreateReadStream();
    await s.CopyToAsync(ctx.Response.Body);
});

// Map REST (your existing controllers) …

// Map hub
app.MapHub<SnapDogHub>("/hubs/snapdog");
```

## 25.6. Client Implementation (TypeScript)

### 25.6.1. Project Layout

```plaintext
apps/webui/
  src/
    main.tsx
    signalr.ts
    fetch-wrapper.ts
    store.ts
    components/
      ZoneCard.tsx
      ClientChip.tsx
      Transport.tsx
      Volume.tsx
  index.html
  vite.config.ts       → outDir: ../SnapDog2.WebUi.Assets/EmbeddedWebRoot
```

### 25.6.2. SignalR client

```typescript
// src/signalr.ts
import { HubConnectionBuilder, LogLevel, HubConnectionState } from "@microsoft/signalr";
import { store } from "./store";

export function startHub(baseUrl: string) {
  const conn = new HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/snapdog/v1`)  // Current endpoint
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build();

  // Listen for current notification events (when handlers are implemented)
  conn.on("ZoneProgressChanged", (zoneIndex, position, progress) =>
    store.dispatch({ t:"zone/progress", zoneIndex, position, progress }));

  conn.on("ZoneTrackMetadataChanged", (zoneIndex, track) =>
    store.dispatch({ t:"zone/track", zoneIndex, track }));

  conn.on("ZoneVolumeChanged", (zoneIndex, volume) =>
    store.dispatch({ t:"zone/volume", zoneIndex, volume }));

  conn.on("ZonePlaybackChanged", (zoneIndex, playbackState) =>
    store.dispatch({ t:"zone/playback", zoneIndex, playbackState }));

  conn.on("ClientZoneChanged", (clientIndex, zoneIndex) =>
    store.dispatch({ t:"client/zone", clientIndex, zoneIndex }));

  conn.on("ClientConnected", (clientIndex, connected) =>
    store.dispatch({ t:"client/connected", clientIndex, connected }));

  async function start() {
    if (conn.state === HubConnectionState.Disconnected) {
      try {
        await conn.start();
        // Join groups for zones/clients you want to monitor
        await conn.invoke("JoinSystem");
      } catch {
        setTimeout(start, 1500);
      }
    }
  }
  start();

  return {
    joinZone: (zoneIndex: number) => conn.invoke("JoinZone", zoneIndex),
    leaveZone: (zoneIndex: number) => conn.invoke("LeaveZone", zoneIndex),
    joinClient: (clientIndex: number) => conn.invoke("JoinClient", clientIndex),
    leaveClient: (clientIndex: number) => conn.invoke("LeaveClient", clientIndex),
    joinSystem: () => conn.invoke("JoinSystem"),
    leaveSystem: () => conn.invoke("LeaveSystem")
  };
}
```

### 25.6.3. Explicit REST wrapper

```typescript
// src/fetch-wrapper.ts
const BASE = "/api/v1";

async function req(method: string, path: string, body?: unknown) {
  const r = await fetch(`${BASE}${path}`, {
    method,
    headers: body ? { "Content-Type": "application/json" } : undefined,
    body: body ? JSON.stringify(body) : undefined,
    credentials: "same-origin",
  });
  if (!r.ok) throw new Error(`${method} ${path} -> ${r.status}`);
  return r;
}

export const api = {
  zones: {
    play: (id: number) => req("POST", `/zones/${zoneIndex}/play`),
    pause: (id: number) => req("POST", `/zones/${zoneIndex}/pause`),
    next: (id: number) => req("POST", `/zones/${zoneIndex}/next`),
    previous: (id: number) => req("POST", `/zones/${zoneIndex}/previous`),
    seek: (id: number, positionMs: number) => req("PUT", `/zones/${zoneIndex}/track/position`, { positionMs }),
    volume: (id: number, volume: number) => req("PUT", `/zones/${zoneIndex}/volume`, { volume }),
    toggleMute: (id: number) => req("PUT", `/zones/${zoneIndex}/mute/toggle`),
    toggleShuffle: (id: number) => req("PUT", `/zones/${zoneIndex}/shuffle/toggle`),
    repeat: (id: number, mode: "Off"|"One"|"All") => req("PUT", `/zones/${zoneIndex}/repeat`, { mode }),
    setPlaylist: (id: number, playlistIndex: number) => req("PUT", `/zones/${zoneIndex}/playlist`, { playlistIndex }),
  },
  clients: {
    assign: (clientIndex: number, zoneId: number, uiActionId?: string) =>
      req("PUT", `/clients/${clientIndex}/zone`, { zoneId, uiActionId }),
  }
};
```

### 25.6.4. Store (minimal reducer)

```typescript
// src/store.ts
type State = {
  zones: Record<number, {
    now?: { title:string; artist:string; album:string; durationMs:number; coverUrl?:string };
    controls: { volume:number; mute:boolean; shuffle:boolean; repeat:"Off"|"One"|"All" };
    clients: number[];
    progress?: { pos:number; dur:number };
  }>;
  clients: Record<number, { connected:boolean; zoneId:number; volume?:number; mute?:boolean }>;
};

export const store = (() => {
  let state: State = { zones:{}, clients:{} };
  const listeners = new Set<() => void>();
  const emit = () => listeners.forEach(fn => fn());
  const get = () => state;
  const on = (fn: () => void) => (listeners.add(fn), () => listeners.delete(fn));

  function dispatch(a: any) {
    switch (a.t) {
      case "zones/index":
        a.ids.forEach((id:number) => state.zones[id] ??= { controls:{volume:0,mute:false,shuffle:false,repeat:"Off"}, clients:[] });
        break;
      case "zone/snapshot":
        state.zones[a.snap.zoneId] = {
          now: a.snap.nowPlaying ?? undefined,
          controls: a.snap.controls,
          clients: [...a.snap.clients],
          progress: state.zones[a.snap.zoneId]?.progress
        };
        break;
      case "zone/progress":
        (state.zones[a.zoneId] ??= {controls:{volume:0,mute:false,shuffle:false,repeat:"Off"}, clients:[]}).progress = { pos:a.pos, dur:a.dur };
        break;
      case "zone/nowPlaying":
        (state.zones[a.zoneId] ??= {controls:{volume:0,mute:false,shuffle:false,repeat:"Off"}, clients:[]}).now = a.np;
        break;
      case "zone/controls":
        (state.zones[a.zoneId] ??= {controls:{volume:0,mute:false,shuffle:false,repeat:"Off"}, clients:[]}).controls = a.c;
        break;
      case "client/status":
        state.clients[a.i] = { connected:a.connected, zoneId:a.zoneId, volume:a.volume, mute:a.mute };
        // Move client in zone list:
        Object.values(state.zones).forEach(z => z.clients = z.clients.filter(c => c !== a.i));
        (state.zones[a.zoneId] ??= {controls:{volume:0,mute:false,shuffle:false,repeat:"Off"}, clients:[]}).clients.push(a.i);
        break;
    }
    emit();
  }

  return { get, on, dispatch };
})();
```

### 25.6.5. Components (sketch)

- **ZoneCard** renders name, now playing, progress, controls, and ClientChip list.
- **ClientChip** is draggable (@dnd-kit) and on drop calls api.clients.assign.

## 25.7. Packaging (embed everything)

### 25.7.1. Assets project

```plaintext
SnapDog2.WebUi.Assets/
  EmbeddedWebRoot/
    index.html
    assets/...(JS/CSS)
    css/app.css
    fonts/Orbitron-Regular.woff2
    icons/...
  AssetsMarker.cs
  SnapDog2.WebUi.Assets.csproj
```

**SnapDog2.WebUi.Assets.csproj**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
    <EnableDefaultItems>false</EnableDefaultItems>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="EmbeddedWebRoot/**/*" />
    <Compile Include="AssetsMarker.cs" />
  </ItemGroup>
</Project>
```

**Local font (no CDN) in css/app.css**:

```css
@font-face {
  font-family: "OrbitronLocal";
  src: url("/fonts/Orbitron-Regular.woff2") format("woff2");
  font-weight: 400;
  font-style: normal;
  font-display: swap;
}
:root {
  --font-orbitron: "OrbitronLocal", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  --font-system: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;

  /* Light tokens */
  --color-primary:#2563eb; --bg:#fff; --surface:#f8fafc;
  --text:#1e293b; --text-muted:#64748b; --border:#e2e8f0;

  /* Semantic */
  --success:#10b981; --warn:#f59e0b; --error:#ef4444; --info:#06b6d4;
}
[data-theme="dark"] {
  --color-primary:#3b82f6; --bg:#0f172a; --surface:#1e293b;
  --text:#f1f5f9; --text-muted:#94a3b8; --border:#334155;
}
```

### 25.7.2. Vite output

**apps/webui/vite.config.ts**:

```typescript
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: "../SnapDog2.WebUi.Assets/EmbeddedWebRoot",
    emptyOutDir: true,
    sourcemap: false
  },
  server: {
    port: 5173,
    proxy: {
      "/api": "http://localhost:5000",
      "/hubs": { target: "http://localhost:5000", ws: true }
    }
  }
});
```

## 25.8. Dev & Build Commands

```bash
# From solution root: create assets project (once)
dotnet new classlib -n SnapDog2.WebUi.Assets -o SnapDog2.WebUi.Assets
dotnet sln add SnapDog2.WebUi.Assets/SnapDog2.WebUi.Assets.csproj
dotnet add SnapDog2/SnapDog2.csproj reference SnapDog2.WebUi.Assets/SnapDog2.WebUi.Assets.csproj

# Frontend
cd apps/webui
pnpm i
pnpm dev            # HMR dev server (proxy to backend)
pnpm build          # emits to EmbeddedWebRoot

# Backend
dotnet run -p SnapDog2
```

**CI**: run `pnpm build` before `dotnet publish`. The publish then includes the embedded files automatically. No Node in prod.

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

- No UI call to /zones or /clients aggregates.
- ZoneSnapshotV1 arrives on subscribe and renders first paint.
- TrackProgressV1 ≤ 5/sec per zone; smooth progress bar.
- Controls update via deltas (no polling).
- DnD reassign triggers explicit PUT, hub echo confirms.
- All static assets (index.html, js, css, fonts) are embedded (check .deps.json).
- Dark/Light theme tokens applied; no external font/CDN.
- Single-file publish runs with no external webroot.

## 25.15. Minimal Code You'll Actually Add (summary)

- **Server**: SnapDogHub, ZoneEvents emitters, IZoneReadModel, Program.cs mappings.
- **Client**: signalr.ts, fetch-wrapper.ts, store.ts, 2–3 components, vite.config.ts.
- **Assets**: SnapDog2.WebUi.Assets with EmbeddedWebRoot + CSS + (optional) local font.
- **CI**: pnpm build before dotnet publish.

## 25.16. Appendix A — Assets Marker

```csharp
// SnapDog2.WebUi.Assets/AssetsMarker.cs
namespace SnapDog2.WebUi.Assets
{
    public sealed class AssetsMarker { }
}
```

## 25.17. Appendix B — Example index.html

```html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8"/>
  <meta name="viewport" content="width=device-width,initial-scale=1"/>
  <title>SnapDog2</title>
  <link rel="stylesheet" href="/css/app.css"/>
</head>
<body>
  <div id="root"></div>
  <script type="module" src="/assets/main.js"></script>
</body>
</html>
```
