# Missing Status and Command IDs Analysis

## Overview
Analysis of Status and Command IDs documented in blueprint vs. actual implementation in codebase.

## Currently Implemented Status IDs
```
CLIENT_CONNECTED
CLIENT_LATENCY_STATUS  
CLIENT_MUTE_STATUS
CLIENT_STATE
CLIENT_VOLUME_STATUS
CLIENT_ZONE_STATUS
MUTE_STATUS
PLAYBACK_STATE
PLAYLIST_INDEX
PLAYLIST_REPEAT_STATUS
PLAYLIST_SHUFFLE_STATUS
SERVER_STATS
SYSTEM_ERROR (blueprint shows ERROR_STATUS)
SYSTEM_STATUS
TRACK_INDEX
TRACK_REPEAT_STATUS
VERSION_INFO
VOLUME_STATUS
ZONE_STATE
ZONES_INFO
```

## Currently Implemented Command IDs
```
CLIENT_LATENCY
CLIENT_MUTE_TOGGLE
CLIENT_MUTE
CLIENT_VOLUME
CLIENT_ZONE
MUTE_TOGGLE
MUTE
PAUSE
PLAY
PLAYLIST_NEXT
PLAYLIST_PREVIOUS
PLAYLIST_REPEAT_TOGGLE
PLAYLIST_REPEAT
PLAYLIST_SHUFFLE_TOGGLE
PLAYLIST_SHUFFLE
PLAYLIST
STOP
TRACK_NEXT
TRACK_PREVIOUS
TRACK_REPEAT_TOGGLE
TRACK_REPEAT
TRACK
VOLUME_DOWN
VOLUME_UP
VOLUME
```

## Missing Status IDs (from Blueprint)

### Track Information Status IDs
- `TRACK_INFO` - Detailed track info object
- `TRACK_INFO_LENGTH` - Track length in ms
- `TRACK_INFO_POSITION` - Track position in ms
- `TRACK_INFO_TITLE` - Track title string
- `TRACK_INFO_ARTIST` - Track artist string
- `TRACK_INFO_ALBUM` - Track album string

### Playlist Information Status IDs
- `PLAYLIST_INFO` - Detailed playlist info object

### Command Response Status IDs
- `COMMAND_STATUS` - Command acknowledgments ("ok", "processing", "done")
- `COMMAND_ERROR` - Command error responses

### Naming Inconsistency
- Blueprint: `ERROR_STATUS` vs Implementation: `SYSTEM_ERROR`

## Missing Command IDs (from Blueprint)
All major command IDs appear to be implemented. The blueprint shows the same commands that exist in the implementation.

## Recommendations

### 1. Add Missing Track Information Notifications
Create new notification classes for detailed track information:
- `ZoneTrackInfoChangedNotification` 
- `ZoneTrackLengthChangedNotification`
- `ZoneTrackPositionChangedNotification`
- `ZoneTrackTitleChangedNotification`
- `ZoneTrackArtistChangedNotification`
- `ZoneTrackAlbumChangedNotification`

### 2. Add Missing Playlist Information Notifications
- `ZonePlaylistInfoChangedNotification`

### 3. Add Command Response Notifications
- `CommandStatusNotification`
- `CommandErrorNotification`

### 4. Fix Naming Inconsistency
Decide whether to use `ERROR_STATUS` (blueprint) or `SYSTEM_ERROR` (implementation) and update accordingly.

### 5. Update Registries
Update both `StatusIds.cs` and the blueprint documentation to be consistent.
