# MQTT Integration

## Topic Structure

snapdog/
├── system/
│   ├── status                     # online/offline (LWT)
│   ├── version                    # version info
├── zones/{index}/
│   ├── control                    # control status in one one topic (see), see 14.3.2.4. Payloads for `{zoneBaseTopic}control/set`
│   ├── control/set                # control command (play/pause/stop), see 14.3.2.4. Payloads for `{zoneBaseTopic}control/set`
│   ├── playing                    # true/false
│   ├── play                       # play command
│   ├── pause                      # pause command
│   ├── stop                       # stop command
│   ├── next                       # next track command
│   ├── previous                   # previous track command
│   ├── volume                     # 50 (current volume)
│   ├── volume/up                  # volume up (optional payload: int step)
│   ├── volume/down                # volume down (optional payload: int step)
│   ├── volume/set                 # 75 (set volume command)
│   ├── mute                       # false (current mute state)
│   ├── mute/set                   # true (set mute command)
│   ├── repeat                     # none/track/playlist
│   ├── repeat/set                 # none/track/playlist (set repeat mode)
│   ├── repeat/playlist            # repeat state: true/false
│   ├── repeat/playlist/toggle     # toggle repeat state
│   ├── repeat/playlist/set        # set repeat state: true/false
│   ├── repeat/track               # track repeat state: true/false
│   ├── repeat/track/toggle        # toggle track repeat state
│   ├── repeat/track/set           # set track repeat state: true/false
│   ├── shuffle                    # shuffle state: true/false
│   ├── shuffle/toggle             # toggle shuffle state
│   ├── shuffle/set                # shuffle state: true/false
│   ├── track                      # index of current track
│   ├── track/set                  # index of track to play
│   ├── track/                     # track information
│   │   ├── title                  # "Song Title"
│   │   ├── artist                 # "Artist Name"
│   │   ├── album                  # "Album Name"
│   │   ├── cover                  # "<cover_image_url>"
│   │   ├── duration               # duration in ms (if available, otherwise "")
│   │   ├── position               # position in ms (only when playing, otherwise "")
│   │   ├── progress               # progress in percent (only when playing, otherwise "")
│   ├── playlist                   # index current playlist
│   ├── playlist/set               # index of playlist to set
│   └── playlist/                  # current playlist information
│       ├── name                   # "Best of Keith"
└── clients/{index}/
    ├── name                       # client name
    ├── connected                  # true/false (LWT)
    ├── volume                     # 100 (current volume)
    ├── volume/set                 # 75 (set volume command)
    ├── mute                       # false
    ├── mute/set                   # true
    ├── latency                    # current latency in ms
    ├── latency/set                # set latency in ms
    ├── zone                       # 1 (current zone)
    └── zone/set                   # 2 (assign to zone)

## Status Id & Command Id Mapping

**Note:** Some Command/Status IDs from the framework are intentionally NOT mapped to MQTT topics:

- `ZONE_NAME` (Command) - Zone names are read-only via MQTT
- `CLIENT_NAME` (Command) - Client names are read-only via MQTT
- `COMMAND_STATUS` (Status) - Command success/failure inferred from state changes
- `COMMAND_ERROR` (Status) - Command success/failure inferred from state changes
- `CLIENT_ERROR` (Status) - Limited error information available from Snapcast clients
- `CLIENT_COMMAND_STATUS` (Status) - Command success/failure inferred from state changes
- `CLIENT_COMMAND_ERROR` (Status) - Command success/failure inferred from state changes

### System Level

| MQTT Topic | Command/Status ID    | Type   | Description                 |
|------------|----------------------|--------|-----------------------------|
| `system/status` | `SYSTEM_STATUS` | Status | System online/offline (LWT) |
| `system/version` | `VERSION_INFO` | Status | Version information         |
| `system/error` | `SYSTEM_ERROR`   | Status | System error information    |
| `system/stats` | `SERVER_STATS`   | Status | Server performance stats    |

### Zone Level

| MQTT Topic | Command/Status ID | Type | Description |
|------------|------------------|------|-------------|
| `zones/{index}/name` | `ZONE_NAME_STATUS` | Status | Name of the zone |
| `zones/{index}/control` | `multiple` | Status | Control status in one topic(see framework 14.3.2.4)|
| `zones/{index}/control/set` | `multiple` | Command | Control commands (see framework 14.3.2.4) |
| `zones/{index}/state` | `ZONE_STATE` | Status | Complete zone state |
| `zones/{index}/playing` | `TRACK_PLAYING_STATUS` | Status | Track playing state |
| `zones/{index}/play` | `PLAY` | Command | Play command |
| `zones/{index}/pause` | `PAUSE` | Command | Pause command |
| `zones/{index}/stop` | `STOP` | Command | Stop command |
| `zones/{index}/next` | `TRACK_NEXT` | Command | Next track command |
| `zones/{index}/previous` | `TRACK_PREVIOUS` | Command | Previous track command |
| `zones/{index}/volume` | `VOLUME_STATUS` | Status | Current volume |
| `zones/{index}/volume/up` | `VOLUME_UP` | Command | Volume up |
| `zones/{index}/volume/down` | `VOLUME_DOWN` | Command | Volume down |
| `zones/{index}/volume/set` | `VOLUME` | Command | Set volume |
| `zones/{index}/mute` | `MUTE_STATUS` | Status | Current mute state |
| `zones/{index}/mute/set` | `MUTE` | Command | Set mute |
| `zones/{index}/mute/toggle` | `MUTE_TOGGLE` | Command | Toggle mute |
| `zones/{index}/repeat` | `PLAYLIST_REPEAT_STATUS` | Status | Playlist repeat state |
| `zones/{index}/repeat/set` | `PLAYLIST_REPEAT` | Command | Set playlist repeat |
| `zones/{index}/repeat/playlist` | `PLAYLIST_REPEAT_STATUS` | Status | Playlist repeat state |
| `zones/{index}/repeat/playlist/toggle` | `PLAYLIST_REPEAT_TOGGLE` | Command | Toggle playlist repeat |
| `zones/{index}/repeat/playlist/set` | `PLAYLIST_REPEAT` | Command | Set playlist repeat |
| `zones/{index}/repeat/track` | `TRACK_REPEAT_STATUS` | Status | Track repeat state |
| `zones/{index}/repeat/track/toggle` | `TRACK_REPEAT_TOGGLE` | Command | Toggle track repeat |
| `zones/{index}/repeat/track/set` | `TRACK_REPEAT` | Command | Set track repeat |
| `zones/{index}/shuffle` | `PLAYLIST_SHUFFLE_STATUS` | Status | Shuffle state |
| `zones/{index}/shuffle/toggle` | `PLAYLIST_SHUFFLE_TOGGLE` | Command | Toggle shuffle |
| `zones/{index}/shuffle/set` | `PLAYLIST_SHUFFLE` | Command | Set shuffle |
| `zones/{index}/track` | `TRACK_STATUS` | Status | Current track index |
| `zones/{index}/track/set` | `TRACK` | Command | Set track |
| `zones/{index}/track/metadata` | `TRACK_METADATA` | Status | Complete track metadata |
| `zones/{index}/track/playing` | `TRACK_PLAYING_STATUS` | Status | Track playing state |
| `zones/{index}/track/position` | `TRACK_POSITION_STATUS` | Status | Track position |
| `zones/{index}/track/position/set` | `TRACK_POSITION` | Command | Seek to position |
| `zones/{index}/track/progress` | `TRACK_PROGRESS_STATUS` | Status | Track progress |
| `zones/{index}/track/progress/set` | `TRACK_PROGRESS` | Command | Seek to progress |
| `zones/{index}/track/title` | `TRACK_METADATA_TITLE` | Status | Track title |
| `zones/{index}/track/artist` | `TRACK_METADATA_ARTIST` | Status | Track artist |
| `zones/{index}/track/album` | `TRACK_METADATA_ALBUM` | Status | Track album |
| `zones/{index}/track/cover` | `TRACK_METADATA_COVER` | Status | Track cover art URL |
| `zones/{index}/track/duration` | `TRACK_METADATA_DURATION` | Status | Track duration |
| `zones/{index}/playlist` | `PLAYLIST_STATUS` | Status | Current playlist index |
| `zones/{index}/playlist/set` | `PLAYLIST` | Command | Set playlist |
| `zones/{index}/playlist/next` | `PLAYLIST_NEXT` | Command | Next playlist |
| `zones/{index}/playlist/previous` | `PLAYLIST_PREVIOUS` | Command | Previous playlist |
| `zones/{index}/playlist/name` | `PLAYLIST_NAME_STATUS` | Status | Playlist name |
| `zones/{index}/playlist/count` | `PLAYLIST_COUNT_STATUS` | Status | Playlist track count |
| `zones/{index}/play/track` | `TRACK_PLAY_INDEX` | Command | Play specific track |
| `zones/{index}/play/url` | `TRACK_PLAY_URL` | Command | Play direct URL stream |

### Client Level

| MQTT Topic | Command/Status ID | Type | Description |
|------------|------------------|------|-------------|
| `clients/{index}/name` | `CLIENT_NAME_STATUS` | Status | Current client name |
| `clients/{index}/connected` | `CLIENT_CONNECTED` | Status | Client connection status |
| `clients/{index}/state` | `CLIENT_STATE` | Status | Complete client state |
| `clients/{index}/volume` | `CLIENT_VOLUME_STATUS` | Status | Current client volume |
| `clients/{index}/volume/set` | `CLIENT_VOLUME` | Command | Set client volume |
| `clients/{index}/volume/up` | `CLIENT_VOLUME_UP` | Command | Client volume up |
| `clients/{index}/volume/down` | `CLIENT_VOLUME_DOWN` | Command | Client volume down |
| `clients/{index}/mute` | `CLIENT_MUTE_STATUS` | Status | Client mute state |
| `clients/{index}/mute/set` | `CLIENT_MUTE` | Command | Set client mute |
| `clients/{index}/mute/toggle` | `CLIENT_MUTE_TOGGLE` | Command | Toggle client mute |
| `clients/{index}/latency` | `CLIENT_LATENCY_STATUS` | Status | Current client latency |
| `clients/{index}/latency/set` | `CLIENT_LATENCY` | Command | Set client latency |
| `clients/{index}/zone` | `CLIENT_ZONE_STATUS` | Status | Current assigned zone |
| `clients/{index}/zone/set` | `CLIENT_ZONE` | Command | Assign client to zone |
