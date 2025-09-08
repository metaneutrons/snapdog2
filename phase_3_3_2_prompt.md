# SNAPDOG2 MEDIATOR REMOVAL - PHASE 3.3.2 IMPLEMENTATION PROMPT

## CURRENT PHASE: Phase 3.3 - Complete Mediator Removal (Sprint 5-6)
## CURRENT STEP: 3.3.2 - Replace ZoneManager Stub with Direct Service Calls
## LAST COMPLETED: Phase 3.3.1 - ZoneManager mediator stub replacement successful
## NEXT OBJECTIVE: Replace stub implementations with actual direct service calls

## IMPLEMENTATION STATUS

- **Files Modified**: 2 (ZoneManager.cs, replace_mediator.py)
- **Files Removed**: 0
- **Services Migrated**: ZoneManager (stub phase complete)
- **Tests Updated**: 0
- **Build Status**: PASS (0 errors, 20 warnings unchanged)

## CRITICAL PATTERNS ESTABLISHED

- **LoggerMessage**: Fully implemented across all services
- **Service Injection**: ZoneManager uses stub mediator (needs real services)
- **StateStore Events**: Not yet implemented
- **Attribute Migration**: CommandId/StatusId preserved in constants

## CURRENT ARCHITECTURE STATE

```
ZoneManager → StubMediator (temporary)
           ↓
         Need: IPlaylistManager + IHubContext<SnapDogHub>
```

## PHASE 3.3.2 OBJECTIVES

### 1. Replace Playlist Query Stubs with Direct Service Calls

**Current Pattern**:
```csharp
var mediator = new StubMediator();
var playlistResult = await mediator.SendQueryAsync<GetPlaylistQuery, Result<PlaylistWithTracks>>(query);
```

**Target Pattern**:
```csharp
var playlistResult = await this._playlistManager.GetPlaylistAsync(playlistIndex);
```

**Required Changes**:
1. Add `IPlaylistManager` and `IHubContext<SnapDogHub>` to ZoneManager constructor
2. Replace all `mediator.SendQueryAsync<GetPlaylistQuery>()` calls with direct `_playlistManager.GetPlaylistAsync()`
3. Replace all `mediator.SendQueryAsync<GetAllPlaylistsQuery>()` calls with direct `_playlistManager.GetAllPlaylistsAsync()`

### 2. Replace Notification Stubs with Direct SignalR Calls

**Current Pattern**:
```csharp
var mediator = new StubMediator();
await mediator.PublishAsync(new ZoneTrackPlayingStatusChangedNotification { ... });
```

**Target Pattern**:
```csharp
await this._hubContext.Clients.All.SendAsync("ZonePlaybackChanged", new ZonePlaybackChangedNotification(...));
```

**Required Changes**:
1. Replace all `mediator.PublishAsync()` calls with direct SignalR hub calls
2. Map notification types to appropriate SignalR method names
3. Ensure notification data structures match SignalR client expectations

### 3. Remove Stub Infrastructure

**After Direct Calls Working**:
1. Remove `IMediator` stub interface
2. Remove `StubMediator` class
3. Clean up any remaining mediator-related using statements

## IMPLEMENTATION STEPS

### Step 1: Add Required Dependencies to ZoneManager

```csharp
public partial class ZoneManager(
    ILogger<ZoneManager> logger,
    ISnapcastService snapcastService,
    ISnapcastStateRepository snapcastStateRepository,
    IMediaPlayerService mediaPlayerService,
    IServiceScopeFactory serviceScopeFactory,
    IZoneStateStore zoneStateStore,
    IClientStateStore clientStateStore,
    IStatusFactory statusFactory,
    IPlaylistManager playlistManager,           // ADD
    IHubContext<SnapDogHub> hubContext,        // ADD
    IOptions<SnapDogConfiguration> configuration
) : IZoneManager, IAsyncDisposable, IDisposable
```

### Step 2: Replace Playlist Query Calls

Find and replace patterns:
- `mediator.SendQueryAsync<GetPlaylistQuery, Result<PlaylistWithTracks>>()` → `_playlistManager.GetPlaylistAsync()`
- `mediator.SendQueryAsync<GetAllPlaylistsQuery, Result<List<PlaylistInfo>>>()` → `_playlistManager.GetAllPlaylistsAsync()`

### Step 3: Replace Notification Publishing

Map notification types to SignalR calls:
- `ZoneTrackPlayingStatusChangedNotification` → `"ZonePlaybackChanged"`
- `ZoneTrackProgressChangedNotification` → `"ZoneProgressChanged"`
- `ZoneTrackMetadataChangedNotification` → `"ZoneMetadataChanged"`
- `ZoneTrackTitleChangedNotification` → `"ZoneTitleChanged"`
- `ZoneTrackArtistChangedNotification` → `"ZoneArtistChanged"`
- `ZoneTrackAlbumChangedNotification` → `"ZoneAlbumChanged"`

### Step 4: Verify and Clean Up

1. Build verification: `dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet`
2. Remove stub infrastructure once direct calls work
3. Update DI registration if needed

## RISK MITIGATION

- **Build Breaks**: Keep stub as fallback until direct calls proven working
- **Missing Methods**: Check IPlaylistManager interface for correct method signatures
- **SignalR Issues**: Verify hub context injection and client method names
- **Performance**: Monitor for any performance regressions

## SUCCESS CRITERIA

- [ ] All playlist queries use direct IPlaylistManager calls
- [ ] All notifications use direct SignalR hub calls
- [ ] Build: 0 errors, ≤20 warnings
- [ ] No stub infrastructure remaining
- [ ] ZoneManager fully independent of mediator pattern
- [ ] Existing functionality preserved

## NEXT PHASE PREPARATION

After Phase 3.3.2 completion:
- **Phase 3.3.3**: Apply same pattern to ClientManager
- **Phase 3.3.4**: Apply same pattern to PlaylistManager
- **Phase 3.3.5**: Remove all mediator infrastructure and packages

## VALIDATION COMMANDS

```bash
# Build verification
dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet

# Check for remaining mediator references
grep -r "IMediator\|mediator\." SnapDog2/Domain/Services/ZoneManager.cs

# Verify direct service calls
grep -r "_playlistManager\|_hubContext" SnapDog2/Domain/Services/ZoneManager.cs
```
