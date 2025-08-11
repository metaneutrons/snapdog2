# Unimplemented CommandIds and StatusIds Analysis

## Summary

After comprehensive analysis of the blueprint specification against the current codebase implementation, here are the findings:

## ✅ CommandIds - FULLY IMPLEMENTED

All **25 CommandIds** from the blueprint are implemented with `[CommandId("...")]` attributes:

### Implemented CommandIds (25/25)
- **Zone Playback (3)**: `PLAY`, `PAUSE`, `STOP`
- **Zone Volume (5)**: `VOLUME`, `VOLUME_UP`, `VOLUME_DOWN`, `MUTE`, `MUTE_TOGGLE`
- **Zone Track (5)**: `TRACK`, `TRACK_NEXT`, `TRACK_PREVIOUS`, `TRACK_REPEAT`, `TRACK_REPEAT_TOGGLE`
- **Zone Playlist (7)**: `PLAYLIST`, `PLAYLIST_NEXT`, `PLAYLIST_PREVIOUS`, `PLAYLIST_REPEAT`, `PLAYLIST_REPEAT_TOGGLE`, `PLAYLIST_SHUFFLE`, `PLAYLIST_SHUFFLE_TOGGLE`
- **Client Commands (5)**: `CLIENT_VOLUME`, `CLIENT_MUTE`, `CLIENT_MUTE_TOGGLE`, `CLIENT_LATENCY`, `CLIENT_ZONE`

### ❌ Missing CommandIds: **NONE**

## ⚠️ StatusIds - PARTIALLY IMPLEMENTED

**19/21 StatusIds** from the blueprint are implemented. **2 StatusIds are missing**.

### Implemented StatusIds (19/21)
- **Client Status (6)**: `CLIENT_VOLUME_STATUS`, `CLIENT_MUTE_STATUS`, `CLIENT_LATENCY_STATUS`, `CLIENT_CONNECTED`, `CLIENT_ZONE_STATUS`, `CLIENT_STATE`
- **Zone Status (9)**: `PLAYBACK_STATE`, `VOLUME_STATUS`, `MUTE_STATUS`, `TRACK_INDEX`, `PLAYLIST_INDEX`, `TRACK_REPEAT_STATUS`, `PLAYLIST_REPEAT_STATUS`, `PLAYLIST_SHUFFLE_STATUS`, `ZONE_STATE`
- **Global Status (4)**: `SYSTEM_STATUS`, `VERSION_INFO`, `SERVER_STATS`, `SYSTEM_ERROR`*

*Note: `SYSTEM_ERROR` is implemented instead of `ERROR_STATUS` from blueprint

### ❌ Missing StatusIds (2/21)

#### 1. `TRACK_INFO` - Zone Track Information Status
- **Blueprint Definition**: Detailed track info status
- **Type**: `ZoneIndex` (int), `TrackInfo` (object/record)
- **Direction**: Status (Publish)
- **Description**: State: Details of track N
- **MQTT Topic**: `track/info` (Full JSON TrackInfo object)
- **Required**: Yes - defined in blueprint Section 14.3.1

#### 2. `PLAYLIST_INFO` - Zone Playlist Information Status  
- **Blueprint Definition**: Detailed playlist info status
- **Type**: `ZoneIndex` (int), `PlaylistInfo` (object/record)
- **Direction**: Status (Publish)
- **Description**: State: Details of playlist P
- **MQTT Topic**: `playlist/info` (Full JSON PlaylistInfo object)
- **Required**: Yes - defined in blueprint Section 14.3.1

### 🔍 Implementation Status Details

#### StatusId Naming Discrepancy
- **Blueprint**: `ERROR_STATUS`
- **Implemented**: `SYSTEM_ERROR`
- **Impact**: Functional equivalent, just different naming convention
- **Recommendation**: Consider renaming to match blueprint exactly

## 📋 Required Implementation Tasks

### High Priority - Missing StatusIds

#### 1. Implement `TRACK_INFO` StatusId
```csharp
// Create notification class
[StatusId("TRACK_INFO", "Blueprint Section 14.3.1")]
public record ZoneTrackInfoChangedNotification : INotification
{
    public required int ZoneIndex { get; init; }
    public required TrackInfo TrackInfo { get; init; }
}

// Add to StatusIds constants
public static readonly string TrackInfo = StatusIdAttribute.GetStatusId<ZoneTrackInfoChangedNotification>();

// Add to StatusEventType enum
[Description("TRACK_INFO")]
TrackInfo,
```

#### 2. Implement `PLAYLIST_INFO` StatusId
```csharp
// Create notification class
[StatusId("PLAYLIST_INFO", "Blueprint Section 14.3.1")]
public record ZonePlaylistInfoChangedNotification : INotification
{
    public required int ZoneIndex { get; init; }
    public required PlaylistInfo PlaylistInfo { get; init; }
}

// Add to StatusIds constants
public static readonly string PlaylistInfo = StatusIdAttribute.GetStatusId<ZonePlaylistInfoChangedNotification>();

// Add to StatusEventType enum
[Description("PLAYLIST_INFO")]
PlaylistInfo,
```

### Medium Priority - Naming Consistency

#### 3. Consider Renaming `SYSTEM_ERROR` to `ERROR_STATUS`
```csharp
// Current implementation
[StatusId("SYSTEM_ERROR", "Blueprint Section 14.2.1")]
public record SystemErrorNotification : INotification

// Blueprint-compliant naming
[StatusId("ERROR_STATUS", "Blueprint Section 14.2.1")]
public record SystemErrorNotification : INotification
```

## 🎯 Implementation Impact

### After Implementing Missing StatusIds
- **Blueprint Compliance**: 100% (21/21 StatusIds)
- **CommandId Compliance**: 100% (25/25 CommandIds) ✅ Already Complete
- **Total Identifiers**: 46/46 (100% complete)
- **DRY Architecture**: Fully compliant with zero hardcoded strings

### Service Integration Requirements
Once the missing StatusIds are implemented, the following services need updates:

#### MQTT Service
- Add topic mapping for `TRACK_INFO` → `track/info`
- Add topic mapping for `PLAYLIST_INFO` → `playlist/info`
- Update `GetZoneMqttTopic()` method to handle new StatusIds

#### KNX Service  
- `TRACK_INFO` and `PLAYLIST_INFO` are information-only StatusIds
- No KNX Group Address mapping required (information objects, not control values)
- No updates needed to KNX service

#### Notification Publishers
- Implement publishers in zone management services
- Trigger notifications when track/playlist information changes
- Ensure proper JSON serialization of TrackInfo/PlaylistInfo objects

## 📊 Current Architecture Quality

### Strengths
- **CommandIds**: 100% blueprint compliance ✅
- **StatusIds**: 90.5% blueprint compliance (19/21)
- **DRY Architecture**: Fully implemented with three-layered approach
- **Type Safety**: Complete compile-time validation
- **Performance**: Optimized with enum switches and O(1) lookups

### Remaining Work
- **2 Missing StatusIds**: `TRACK_INFO`, `PLAYLIST_INFO`
- **1 Naming Inconsistency**: `SYSTEM_ERROR` vs `ERROR_STATUS`
- **Service Integration**: MQTT topic mapping updates needed

## 🚀 Next Steps

1. **Implement Missing StatusIds** (High Priority)
   - Create notification classes with StatusId attributes
   - Add to constants and enum systems
   - Update service integrations

2. **Verify Blueprint Compliance** (Medium Priority)
   - Consider renaming `SYSTEM_ERROR` to `ERROR_STATUS`
   - Ensure all naming matches blueprint exactly

3. **Complete Service Integration** (Medium Priority)
   - Update MQTT service topic mappings
   - Implement notification publishers
   - Test end-to-end functionality

4. **Documentation Updates** (Low Priority)
   - Update DRY architecture documentation
   - Add usage examples for new StatusIds
   - Update blueprint compliance metrics

After completing these tasks, SnapDog2 will achieve **100% blueprint compliance** with **46/46 identifiers** fully implemented in the comprehensive DRY architecture system.
