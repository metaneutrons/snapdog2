# Snapcast Group to Zone Mapping Analysis

## Current Snapcast Server Status

### Groups (4 total)
1. **Group ID**: `0921235a-da42-3715-8b46-e918405c49f9`
   - **Stream**: `Zone1`
   - **Clients**: `kitchen` (MAC: 02:42:ac:11:00:11)
   - **Status**: Connected, Volume 100%, Not muted

2. **Group ID**: `9e7e3bc2-bb67-7aaa-cee7-265500d6bd60`
   - **Stream**: `Zone1`
   - **Clients**: `living-room` (MAC: 02:42:ac:11:00:10)
   - **Status**: Connected, Volume 100%, Not muted

3. **Group ID**: `07df6340-a853-e367-a030-0db577bbcd70`
   - **Stream**: `Zone1`
   - **Clients**: `bedroom` (MAC: 02:42:ac:11:00:12)
   - **Status**: Connected, Volume 100%, Not muted

4. **Group ID**: `6794179a-46c7-86c8-b41b-f8ca31301339`
   - **Stream**: `Zone1`
   - **Clients**: `0b8d98b1-1a42-4d28-9239-b3b662ed9310` (Snapweb client)
   - **Status**: Disconnected

### Streams (2 total)
1. **Zone1**: `pipe:///snapsinks/zone1` (Status: idle)
2. **Zone2**: `pipe:///snapsinks/zone2` (Status: idle)

## Current Zone Assignment Issue

### Expected Zone Assignment (from logs)
- **Client 1** (Living Room): Zone 1 ✅
- **Client 2** (Kitchen): Zone 1 ✅  
- **Client 3** (Bedroom): Zone 2 ❌ (Should be Zone2 stream, but on Zone1)

### Actual Snapcast Configuration
- **ALL clients are on Zone1 stream** ❌
- **Each client is in its own separate group** ❌
- **Zone2 stream has no clients** ❌

## The Mapping Problem

### Current Implementation Issues

1. **No Zone-to-Group Mapping**: 
   - ZoneManager creates `_snapcastGroupId = $"group_{_zoneIndex}"` (hardcoded)
   - But Snapcast uses UUIDs like `0921235a-da42-3715-8b46-e918405c49f9`

2. **No Client Zone Assignment**:
   - ClientManager shows `ZoneIndex => this._config.DefaultZone` (static config)
   - No actual Snapcast group movement when zone assignment changes

3. **Stream Assignment Missing**:
   - All clients default to Zone1 stream
   - No mechanism to move clients to Zone2 stream

### Expected Behavior

#### Zone 1 (Ground Floor)
- **Stream**: `Zone1` (`/snapsinks/zone1`)
- **Clients**: Living Room, Kitchen
- **Group**: Single group containing both clients

#### Zone 2 (1st Floor)  
- **Stream**: `Zone2` (`/snapsinks/zone2`)
- **Clients**: Bedroom
- **Group**: Single group containing bedroom client

## Missing Implementation Components

### 1. Zone-to-Stream Mapping
```csharp
// In ZoneManager
private async Task EnsureSnapcastGroupAsync()
{
    // Find existing group for this zone's stream
    var streamId = _config.Sink; // e.g., "/snapsinks/zone1" -> "Zone1"
    var groups = _snapcastStateRepository.GetAllGroups();
    var zoneGroup = groups.FirstOrDefault(g => g.StreamId == streamId);
    
    if (zoneGroup == null)
    {
        // Create new group for this zone
        _snapcastGroupId = await _snapcastService.CreateGroupAsync([], cancellationToken);
        await _snapcastService.SetGroupStreamAsync(_snapcastGroupId, streamId);
    }
    else
    {
        _snapcastGroupId = zoneGroup.Id;
    }
}
```

### 2. Client Zone Assignment Implementation
```csharp
// In ClientManager
public async Task<Result> AssignClientToZoneAsync(int clientIndex, int zoneIndex)
{
    // Get target zone's Snapcast group
    var zoneManager = _serviceProvider.GetRequiredService<IZoneManager>();
    var targetZone = await zoneManager.GetZoneAsync(zoneIndex);
    var targetGroupId = targetZone.SnapcastGroupId;
    
    // Get client's Snapcast ID
    var client = _clients[clientIndex - 1];
    var snapcastClientId = client.SnapcastClient.Id;
    
    // Move client to target group
    return await _snapcastService.SetClientGroupAsync(snapcastClientId, targetGroupId);
}
```

### 3. Stream-to-Zone Configuration Mapping
```csharp
// Zone configuration should map to stream IDs
public class ZoneConfig
{
    public string Sink { get; set; } // "/snapsinks/zone1"
    
    // Derived property
    public string StreamId => Path.GetFileName(Sink).Replace("zone", "Zone"); // "Zone1"
}
```

## Current Workaround Status

### What's Working
- ✅ Individual client control (volume, mute, latency)
- ✅ Snapcast connection and event handling
- ✅ Zone state management (conceptual)

### What's Missing
- ❌ Actual zone-to-group mapping
- ❌ Client zone assignment functionality  
- ❌ Stream assignment based on zone
- ❌ Group creation/management for zones

## Next Steps

1. **Implement Zone-to-Group Mapping**:
   - Map zone configurations to Snapcast stream IDs
   - Find or create groups for each zone's stream

2. **Implement Client Zone Assignment**:
   - Move clients between Snapcast groups when zone changes
   - Update ClientManager to actually perform group assignments

3. **Fix Initial Client Placement**:
   - Ensure clients start in correct zones based on DefaultZone config
   - Move Bedroom client to Zone2 stream/group

4. **Test Zone Switching**:
   - Verify clients can be moved between zones
   - Ensure audio streams correctly to assigned zones
