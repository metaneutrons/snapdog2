# SNAPDOG2 MEDIATOR REMOVAL - PHASE 4.1 IMPLEMENTATION PROMPT

## CURRENT PHASE: Phase 4.1 - State Store Event-Driven Architecture
## CURRENT STEP: 4.1.1 - Enhanced State Store Interfaces with Events
## LAST COMPLETED: Phase 3.5 - Complete mediator infrastructure removal
## NEXT OBJECTIVE: Implement event-driven state management as Single Source of Truth

## IMPLEMENTATION STATUS

- **Files Modified**: ZoneManager.cs (✓ direct calls)
- **Files Removed**: ~4,000+ lines (command/handler infrastructure)
- **Services Migrated**: ZoneManager (✓), Others (stubbed)
- **Tests Updated**: 0
- **Build Status**: PASS (0 errors, ≤30 warnings)

## CRITICAL PATTERNS ESTABLISHED

- **LoggerMessage**: Fully implemented across all services
- **Service Injection**: Direct service calls proven working
- **StateStore Events**: **READY TO IMPLEMENT**
- **Attribute Migration**: CommandId/StatusId preserved in constants

## CURRENT ARCHITECTURE STATE

```
API Controllers → Direct Service Calls
Domain Services → StateStore (read/write)
StateStore → Events (MISSING - TO IMPLEMENT)
Integration Services → StateStore Events (MISSING - TO IMPLEMENT)
```

## PHASE 4.1 OBJECTIVES

### 1. Enhanced State Store Interfaces with Events

**Target Pattern**:
```csharp
// StateStore becomes Single Source of Truth with Events
StateStore.SetZoneState() → Fires Events → Integration Services React
```

**IZoneStateStore Enhancement**:
```csharp
// File: Domain/Abstractions/IZoneStateStore.cs
public interface IZoneStateStore
{
    // Specific events for granular change detection
    [StatusId(StatusIds.ZoneStateChanged)]
    event EventHandler<ZoneStateChangedEventArgs> ZoneStateChanged;

    [StatusId(StatusIds.PlaylistStatus)]
    event EventHandler<ZonePlaylistChangedEventArgs> ZonePlaylistChanged;

    [StatusId(StatusIds.VolumeStatus)]
    event EventHandler<ZoneVolumeChangedEventArgs> ZoneVolumeChanged;

    [StatusId(StatusIds.TrackStatus)]
    event EventHandler<ZoneTrackChangedEventArgs> ZoneTrackChanged;

    [StatusId(StatusIds.PlaybackState)]
    event EventHandler<ZonePlaybackStateChangedEventArgs> ZonePlaybackStateChanged;

    // Existing methods unchanged
    ZoneState? GetZoneState(int zoneIndex);
    void SetZoneState(int zoneIndex, ZoneState state);
    Dictionary<int, ZoneState> GetAllZoneStates();
    void InitializeZoneState(int zoneIndex, ZoneState defaultState);
}
```

### 2. Event Args Classes with StatusId Attributes

**Event Args Implementation**:
```csharp
// File: Shared/Events/StateChangeEventArgs.cs
public record ZoneStateChangedEventArgs(
    int ZoneIndex,
    ZoneState? OldState,
    ZoneState NewState,
    DateTime Timestamp = default
);

[StatusId(StatusIds.PlaylistStatus)]
public record ZonePlaylistChangedEventArgs(
    int ZoneIndex,
    PlaylistInfo? OldPlaylist,
    PlaylistInfo? NewPlaylist,
    DateTime Timestamp = default
) : ZoneStateChangedEventArgs(ZoneIndex, null, null, Timestamp);

[StatusId(StatusIds.VolumeStatus)]
public record ZoneVolumeChangedEventArgs(
    int ZoneIndex,
    int OldVolume,
    int NewVolume,
    DateTime Timestamp = default
) : ZoneStateChangedEventArgs(ZoneIndex, null, null, Timestamp);
```

### 3. Smart State Change Detection

**InMemoryZoneStateStore Enhancement**:
```csharp
// File: Infrastructure/Storage/InMemoryZoneStateStore.cs
public class InMemoryZoneStateStore : IZoneStateStore
{
    [StatusId(StatusIds.ZoneStateChanged)]
    public event EventHandler<ZoneStateChangedEventArgs>? ZoneStateChanged;

    [StatusId(StatusIds.PlaylistStatus)]
    public event EventHandler<ZonePlaylistChangedEventArgs>? ZonePlaylistChanged;

    public void SetZoneState(int zoneIndex, ZoneState newState)
    {
        var oldState = GetZoneState(zoneIndex);
        _states[zoneIndex] = newState;

        // Detect specific changes and fire targeted events
        DetectAndPublishChanges(zoneIndex, oldState, newState);

        // Always fire general state change
        ZoneStateChanged?.Invoke(this, new ZoneStateChangedEventArgs(
            zoneIndex, oldState, newState));
    }

    private void DetectAndPublishChanges(int zoneIndex, ZoneState? oldState, ZoneState newState)
    {
        // Playlist changes
        if (oldState?.Playlist?.Index != newState.Playlist?.Index)
        {
            ZonePlaylistChanged?.Invoke(this, new ZonePlaylistChangedEventArgs(
                zoneIndex, oldState?.Playlist, newState.Playlist));
        }

        // Volume changes
        if (oldState?.Volume != newState.Volume)
        {
            ZoneVolumeChanged?.Invoke(this, new ZoneVolumeChangedEventArgs(
                zoneIndex, oldState?.Volume ?? 0, newState.Volume));
        }
        
        // Additional change detection...
    }
}
```

### 4. Client State Store Events

**IClientStateStore Enhancement**:
```csharp
// File: Domain/Abstractions/IClientStateStore.cs
public interface IClientStateStore
{
    [StatusId(StatusIds.ClientStateChanged)]
    event EventHandler<ClientStateChangedEventArgs> ClientStateChanged;

    [StatusId(StatusIds.ClientVolumeStatus)]
    event EventHandler<ClientVolumeChangedEventArgs> ClientVolumeChanged;

    [StatusId(StatusIds.ClientConnected)]
    event EventHandler<ClientConnectionChangedEventArgs> ClientConnectionChanged;

    // Existing methods unchanged...
}
```

## IMPLEMENTATION STEPS

### Step 1: Create Event Args Classes

1. Create `Shared/Events/StateChangeEventArgs.cs`
2. Implement all event args with StatusId attributes
3. Ensure proper inheritance hierarchy

### Step 2: Enhance State Store Interfaces

1. Add event declarations to `IZoneStateStore`
2. Add event declarations to `IClientStateStore`
3. Add StatusId attributes to all events

### Step 3: Implement Smart Change Detection

1. Update `InMemoryZoneStateStore.SetZoneState()`
2. Add `DetectAndPublishChanges()` method
3. Implement granular change detection logic

### Step 4: Update Client State Store

1. Update `InMemoryClientStateStore` with events
2. Implement client-specific change detection
3. Add proper event firing

### Step 5: Build and Test

1. Verify compilation
2. Test event firing with unit tests
3. Validate StatusId attributes

## RISK MITIGATION

- **Performance Impact**: Monitor event firing overhead (<5ms per change)
- **Event Flooding**: Implement debouncing if needed
- **Memory Leaks**: Ensure proper event unsubscription
- **Thread Safety**: Use concurrent collections if needed

## SUCCESS CRITERIA

- [ ] All state store interfaces enhanced with events
- [ ] Event args classes implement StatusId attributes
- [ ] Smart change detection working (no false positives)
- [ ] Build: 0 errors, ≤30 warnings
- [ ] Performance impact <5ms per state change
- [ ] Blueprint tests validate StatusId attributes on events
- [ ] Unit tests verify event firing

## VALIDATION COMMANDS

```bash
# Build verification
dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet

# Check event implementations
grep -r "event EventHandler" SnapDog2/Domain/Abstractions/
grep -r "StatusId.*event" SnapDog2/Domain/Abstractions/

# Verify event args classes
find SnapDog2/Shared/Events -name "*EventArgs.cs"
```

## NEXT PHASE PREPARATION

After Phase 4.1 completion:
- **Phase 4.2**: Integration Coordinator (replaces StatePublishingService)
- **Phase 4.3**: Integration Publisher Abstraction
- **Phase 4.4**: Wire Integration Services to State Events

## ARCHITECTURE ACHIEVEMENT

This phase establishes the **Single Source of Truth** pattern:

```
Before: Services → Direct SignalR/MQTT/KNX calls
After:  Services → StateStore → Events → Integration Coordinator → All Integrations
```

**Benefits**:
- **Consistency**: All integrations receive same state changes
- **Decoupling**: Services don't know about integrations
- **Reliability**: State changes guaranteed to reach all integrations
- **Debugging**: Clear event flow for troubleshooting
