# SignalR Track Progress Event

## New Event Required: `TrackProgress`

### Event Signature
```csharp
await Clients.All.SendAsync("TrackProgress", zoneIndex, positionMs, progressPercent);
```

### Parameters
- `zoneIndex` (int): Zone index (1-based)
- `positionMs` (int): Current position in milliseconds
- `progressPercent` (float): Progress as percentage (0-100)

### Firing Rules
- **Frequency**: Maximum once per second
- **Condition**: Only when track is playing (`playbackState == "playing"`)
- **Source**: Timer-based or media player position updates

### Implementation Example
```csharp
// In your zone/media service
private async Task BroadcastTrackProgress(int zoneIndex)
{
    var zone = GetZone(zoneIndex);
    if (zone?.Track?.IsPlaying == true && zone.Track.DurationMs > 0)
    {
        var positionMs = zone.Progress?.Position ?? 0;
        var progressPercent = (float)(positionMs * 100.0 / zone.Track.DurationMs);
        
        await _hubContext.Clients.All.SendAsync("TrackProgress", 
            zoneIndex, positionMs, progressPercent);
    }
}

// Call this method every second for playing zones
```

### Frontend Integration
The frontend now listens for this event and updates the progress bar and time display in real-time without local timers.
