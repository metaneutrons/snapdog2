# 14. Command Framework

## 14.1. Overview of Command Structure

The command framework provides a unified approach to controlling SnapDog2 across different communication protocols. It defines the **logical commands (actions) and status information (state)** required by the system, independent of the specific implementation (MQTT, KNX, API, Cortex.Mediator). This definition serves as the canonical specification for system interactions.

The framework is organized into three levels:

1. **Global Level**: System-wide status updates.
2. **Zone Level**: Commands and status for audio zones (which map 1:1 to Snapcast Groups).
3. **Client Level**: Commands and status for individual Snapcast clients.

For each level, this section defines:

* The conceptual **Functionality**: Listing available Commands and Status updates with their unique IDs, descriptions, essential parameters/payload types, direction (Command/Status), and relevant notes.
* The **MQTT Implementation**: Detailing how the functionality maps to MQTT topics and payloads, including default topic structures and relevant environment variable configuration suffixes.
* The **KNX Implementation**: Detailing how the functionality maps to KNX Group Addresses (GAs) and Datapoint Types (DPTs), including relevant environment variable configuration suffixes.

**Key Conventions within this Framework:**

* **Targeting:** Commands and status related to Zones or Clients logically require a `ZoneIndex` or `ClientIndex` to identify the target. This ID is often implicit in specific implementations (e.g., API URL path, method called on an object instance) but is explicitly listed in the Functionality tables as essential information.
* **Indexing:** All **Playlist and Track indices** referenced in external interfaces (MQTT, KNX, API) are **1-based**. Playlist index `1` is reserved for the configured Radio stations (see Section 10 for configuration details).
* **Internal Mapping:** The internal application logic (e.g., within `/Server` layer components like `PlaylistManager`) is responsible for mapping these 1-based external indices to 0-based internal list indices where necessary.
* **KNX Limits:** For KNX DPT 5.010 (used for Track/Playlist indices), values greater than 255 cannot be represented. In such cases, the corresponding KNX Status GA **must** report `0`.
* **Cortex.Mediator:** Conceptual commands map to Cortex.Mediator `ICommand<Result<T>>` or `IQuery<Result<T>>` objects, while status updates correspond to Cortex.Mediator `INotification` publications handled by relevant infrastructure adapters. See Section 16 for Cortex.Mediator implementation details.
* **Configuration:** MQTT Topic structures and KNX Group Addresses are configurable via environment variables detailed in Section 10. The tables below list the default relative topic paths and environment variable *suffixes*.

## 14.2. Global Commands and Status

### 14.2.1. Global Functionality (Status Only)

| Status ID         | Description                 | Essential Information / Type          | Direction        | Notes                           |
| :---------------- | :-------------------------- | :------------------------------------ | :--------------- | :------------------------------ |
| `SYSTEM_STATUS`   | System online/offline status | `IsOnline` (bool)                     | Status (Publish) | `true`=online, `false`=offline  |
| `ERROR_STATUS`    | System error information    | `ErrorDetails` object (`Core.Models`) | Status (Publish) | Published on significant errors |
| `VERSION_INFO`    | Software version information| `VersionDetails` object (`Core.Models`) | Status (Publish) | Contains version, build date etc. |
| `SERVER_STATS`    | Server performance stats    | `ServerStats` object (`Core.Models`)  | Status (Publish) | CPU, Memory, Uptime             |

*(The C# Record definitions for `ErrorDetails`, `VersionDetails`, `ServerStats` are implemented in `SnapDog2.Core.Models` namespace)*

### 14.2.2. Global MQTT Implementation

Base topic: `SNAPDOG_SYSTEM_MQTT_BASE_TOPIC` (default: `snapdog`). System topics relative to base, configurable via `SNAPDOG_SYSTEM_MQTT_*_TOPIC` vars (Sec 10).

| Status ID         | Default Relative Topic | Retained | Example Payload (JSON - aligned with Records)                         |
| :---------------- | :--------------------- | :------- | :-------------------------------------------------------------------- |
| `SYSTEM_STATUS`   | `status`               | Yes      | `{"status": 1, "timestamp": "2025-04-05T20:00:00Z"}`                  |
| `ERROR_STATUS`    | `error`                | No       | `{"timestampUtc":"...","level":4,"errorCode":"KNX_WRITE_TIMEOUT",...}`  |
| `VERSION_INFO`    | `version`              | Yes      | `{"version":"1.3.0","timestampUtc":"...","buildDateUtc":"..."}`      |
| `SERVER_STATS`    | `stats`                | No       | `{"timestampUtc":"...","cpuUsagePercent":12.5,"memoryUsageMb":128.5,...}`|

### 14.2.3. Global MQTT Last Will and Testament (LWT)

* **Topic:** `{BaseTopic}/{StatusTopic}` (e.g., `snapdog/status`)
* **Payload:** `{"status": 0}`
* **Retained:** `true`
* **QoS:** `1` (AtLeastOnce)

(When online, publishes `{"status": 1}` to the same topic with retain=true).

## 14.3. Zone Commands and Status

### 14.3.1. Zone Functionality

*(Grouped by function)*

**Playback Control**

| Command/Status ID | Description            | Essential Information / Type                       | Direction        | Notes / Comments                  |
| :---------------- | :--------------------- | :------------------------------------------------- | :--------------- | :-------------------------------- |
| `PLAY`            | Start/resume playback  | `ZoneIndex` (int), Optional `TrackIndex`/`MediaUrl` | Command (Set)    | Action: Tell zone to play         |
| `PAUSE`           | Pause playback         | `ZoneIndex` (int)                                     | Command (Set)    | Action: Tell zone to pause        |
| `STOP`            | Stop playback          | `ZoneIndex` (int)                                     | Command (Set)    | Action: Tell zone to stop         |
| `PLAYBACK_STATE`  | Current playback state | `ZoneIndex` (int), `Status` (`PlaybackStatus` enum)   | Status (Publish) | State: Stopped, Playing, Paused |

**Track Management**

| Command/Status ID     | Description                | Essential Information / Type                   | Direction        | Notes / Comments                        |
| :-------------------- | :------------------------- | :--------------------------------------------- | :--------------- | :-------------------------------------- |
| `TRACK`               | Set specific track         | `ZoneIndex` (int), `TrackIndex` (int, 1-based)    | Command (Set)    | Action: Play track `N`                |
| `TRACK_INDEX`         | Current track index        | `ZoneIndex` (int), `TrackIndex` (int, 1-based)    | Status (Publish) | State: Current index is `N`, 0 for KNX if > 255 |
| `TRACK_INFO`          | Detailed track info        | `ZoneIndex` (int), `TrackInfo` (object/record)    | Status (Publish) | State: Details of track `N`         |
| `TRACK_INFO_LENGTH`   | Track length.              | `ZoneIndex` (int), `TrackLength` in ms (long)  | Status (Publish) | State: Length of track `N`         |
| `TRACK_INFO_POSITION` | Track position.            | `ZoneIndex` (int), `TrackPosition` in ms (long) | Status (Publish) | State: Position of track `N`       |
| `TRACK_INFO_TITLE`  | Track title.             | `ZoneIndex` (int), `TrackTitle` (string)        | Status (Publish) | State: Title of track `N`          |
| `TRACK_INFO_ARTIST`  | Track artist.           | `ZoneIndex` (int), `TrackArtist` (string)      | Status (Publish) | State: Artist of track `N`        |
| `TRACK_INFO_ALBUM`   | Track album.            | `ZoneIndex` (int), `TrackAlbum` (string)       | Status (Publish) | State: Album of track `N`         |
| `TRACK_NEXT`          | Play next track            | `ZoneIndex` (int)                                 | Command (Set)    | Action: Go to next track            |
| `TRACK_PREVIOUS`      | Play previous track        | `ZoneIndex` (int)                                 | Command (Set)    | Action: Go to previous track        |
| `TRACK_REPEAT`        | Set track repeat mode      | `ZoneIndex` (int), `Enabled` (bool)               | Command (Set)    | Action: Turn repeat on/off            |
| `TRACK_REPEAT_TOGGLE` | Toggle track repeat mode   | `ZoneIndex` (int)                                 | Command (Set)    | Action: Toggle repeat state           |
| `TRACK_REPEAT_STATUS` | Current track repeat state | `ZoneIndex` (int), `Enabled` (bool)               | Status (Publish) | State: Repeat is on/off               |

**Playlist Management**

| Command/Status ID         | Description                | Essential Information / Type                          | Direction        | Notes / Comments                        |
| :------------------------ | :------------------------- | :---------------------------------------------------- | :--------------- | :-------------------------------------- |
| `PLAYLIST`                | Set specific playlist      | `ZoneIndex` (int), `PlaylistIndex` (1-based) or `PlaylistIndex` | Command (Set)    | Action: Change to playlist `P`        |
| `PLAYLIST_INDEX`          | Current playlist index/ID  | `ZoneIndex` (int), `PlaylistIndex` (1-based) or `PlaylistIndex` | Status (Publish) | State: Current playlist is `P`, 0 for KNX if > 255 |
| `PLAYLIST_INFO`           | Detailed playlist info     | `ZoneIndex` (int), `PlaylistInfo` (object/record)        | Status (Publish) | State: Details of playlist `P`        |
| `PLAYLIST_NEXT`           | Play next playlist         | `ZoneIndex` (int)                                        | Command (Set)    | Action: Go to next playlist           |
| `PLAYLIST_PREVIOUS`       | Play previous playlist     | `ZoneIndex` (int)                                        | Command (Set)    | Action: Go to previous playlist       |
| `PLAYLIST_SHUFFLE`        | Set playlist shuffle mode  | `ZoneIndex` (int), `Enabled` (bool)                      | Command (Set)    | Action: Turn shuffle on/off           |
| `PLAYLIST_SHUFFLE_TOGGLE` | Toggle shuffle mode        | `ZoneIndex` (int)                                        | Command (Set)    | Action: Toggle shuffle state          |
| `PLAYLIST_SHUFFLE_STATUS` | Current shuffle state      | `ZoneIndex` (int), `Enabled` (bool)                      | Status (Publish) | State: Shuffle is on/off              |
| `PLAYLIST_REPEAT`         | Set playlist repeat mode   | `ZoneIndex` (int), `Enabled` (bool)                      | Command (Set)    | Action: Turn playlist repeat on/off   |
| `PLAYLIST_REPEAT_TOGGLE`  | Toggle playlist repeat     | `ZoneIndex` (int)                                        | Command (Set)    | Action: Toggle playlist repeat state  |
| `PLAYLIST_REPEAT_STATUS`  | Current playlist repeat    | `ZoneIndex` (int), `Enabled` (bool)                      | Status (Publish) | State: Playlist repeat is on/off      |

**Volume & Mute Control**

| Command/Status ID | Description             | Essential Information / Type                       | Direction        | Notes / Comments           |
| :---------------- | :---------------------- | :------------------------------------------------- | :--------------- | :------------------------- |
| `VOLUME`          | Set zone volume         | `ZoneIndex` (int), `Volume` (int, 0-100)              | Command (Set)    | Action: Set volume to `Volume`  |
| `VOLUME_STATUS`   | Current zone volume     | `ZoneIndex` (int), `Volume` (int, 0-100)              | Status (Publish) | State: Current volume is `Volume` |
| `VOLUME_UP`       | Increase zone volume    | `ZoneIndex` (int), Optional `Step` (int, default 5)   | Command (Set)    | Action: Increase volume    |
| `VOLUME_DOWN`     | Decrease zone volume    | `ZoneIndex` (int), Optional `Step` (int, default 5)   | Command (Set)    | Action: Decrease volume    |
| `MUTE`            | Set zone mute           | `ZoneIndex` (int), `Enabled` (bool)                   | Command (Set)    | Action: Mute/unmute zone |
| `MUTE_TOGGLE`     | Toggle zone mute        | `ZoneIndex` (int)                                     | Command (Set)    | Action: Toggle mute state  |
| `MUTE_STATUS`     | Current zone mute state | `ZoneIndex` (int), `Enabled` (bool)                   | Status (Publish) | State: Mute is on/off      |

**General Zone**

| Command/Status ID | Description         | Essential Information / Type                   | Direction        | Notes / Comments              |
| :---------------- | :------------------ | :--------------------------------------------- | :--------------- | :-------------------------- |
| `ZONE_STATE`      | Complete zone state | `ZoneIndex` (int), `ZoneState` (object/record)    | Status (Publish) | State: Full state incl. modes |

### 14.3.2. Zone MQTT Implementation

* Base topic: `SNAPDOG_ZONE_n_MQTT_BASE_TOPIC` (default: `snapdog/zones/{n}/`). **Indices are 1-based.** Relative topic paths configured via `SNAPDOG_ZONE_{n}_MQTT_{SUFFIX}` variables (Sec 10).

#### 14.3.2.1. Zone Command Topics

**Playback/Mode Control**

| Command ID                | Env Var Suffix           | Default Rel. Topic   | Example Payloads                               | Notes                            |
| :------------------------ | :----------------------- | :--------------------- | :--------------------------------------------- | :------------------------------- |
| `PLAY`/`PAUSE`/`STOP` etc.| `_CONTROL_SET_TOPIC`     | `control/set`              | `"play"`, `"pause"`, `"next"`, `"shuffle_on"` | **See 13.3.2.3 for full payload list** |

**Navigation Commands (Dedicated Topics)**

| Command ID              | Env Var Suffix             | Default Rel. Topic   | Example Payloads                   | Notes                     |
| :---------------------- | :------------------------- | :------------------- | :--------------------------------- | :------------------------ |
| `TRACK_NEXT`            | `_TRACK_NEXT_TOPIC`        | `next`               | (no payload needed)                | Next track (most common) |
| `TRACK_PREVIOUS`        | `_TRACK_PREVIOUS_TOPIC`    | `previous`           | (no payload needed)                | Previous track           |
| `PLAYLIST_NEXT`         | `_PLAYLIST_NEXT_TOPIC`     | `playlist/next`      | (no payload needed)                | Next playlist            |
| `PLAYLIST_PREVIOUS`     | `_PLAYLIST_PREVIOUS_TOPIC` | `playlist/previous`  | (no payload needed)                | Previous playlist        |

**Track Management**

| Command ID              | Env Var Suffix             | Default Rel. Topic   | Example Payloads                   | Notes                     |
| :---------------------- | :------------------------- | :------------------- | :--------------------------------- | :------------------------ |
| `TRACK`                 | `_TRACK_SET_TOPIC`         | `track/set`          | `<index>`, `"+"` , `"-"`         | **1-based** index       |
| `TRACK_REPEAT`          | `_TRACK_REPEAT_SET_TOPIC`  | `repeat/track`       | `"true"`/`"false"`, `"1"`/`"0"`  |                           |
| `TRACK_REPEAT_TOGGLE`   | `_TRACK_REPEAT_SET_TOPIC`  | `repeat/track`       | `"toggle"`                         |                           |

**Playlist Management**

| Command ID                | Env Var Suffix                 | Default Rel. Topic   | Example Payloads                   | Notes                     |
| :------------------------ | :----------------------------- | :--------------------- | :--------------------------------- | :------------------------ |
| `PLAYLIST`                | `_PLAYLIST_SET_TOPIC`        | `playlist/set`         | `<id_or_index>`, `"+"` , `"-"`   | **1=Radio**, 2+=Subsonic|
| `PLAYLIST_REPEAT`         | `_PLAYLIST_REPEAT_SET_TOPIC` | `repeat/set`           | `"true"`/`"false"`, `"1"`/`"0"`  |                           |
| `PLAYLIST_REPEAT_TOGGLE`  | `_PLAYLIST_REPEAT_SET_TOPIC` | `repeat/set`           | `"toggle"`                         |                           |
| `PLAYLIST_SHUFFLE`        | `_PLAYLIST_SHUFFLE_SET_TOPIC`| `shuffle/set`          | `"true"`/`"false"`, `"1"`/`"0"`  |                           |
| `PLAYLIST_SHUFFLE_TOGGLE` | `_PLAYLIST_SHUFFLE_SET_TOPIC`| `shuffle/set`          | `"toggle"`                         |                           |

**Volume/Mute Control**

| Command ID            | Env Var Suffix      | Default Rel. Topic | Example Payloads                     | Notes                     |
| :-------------------- | :------------------ | :----------------- | :----------------------------------- | :------------------------ |
| `VOLUME`/`UP`/`DOWN`  | `_VOLUME_SET_TOPIC` | `volume/set`       | `0`-`100`, `"+"` / `"-"`, `"+/-<step>"` |                           |
| `VOLUME_UP`           | `_VOLUME_UP_TOPIC`  | `volume/up`        | (no payload needed)                  | Dedicated volume up       |
| `VOLUME_DOWN`         | `_VOLUME_DOWN_TOPIC`| `volume/down`      | (no payload needed)                  | Dedicated volume down     |
| `MUTE`/`TOGGLE`       | `_MUTE_SET_TOPIC`   | `mute/set`         | `"true"`/`"false"`, `"1"`/`"0"`, `"toggle"` |                           |
| `MUTE_TOGGLE`         | `_MUTE_TOGGLE_TOPIC`| `mute/toggle`      | (no payload needed)                  | Dedicated mute toggle     |

#### 14.3.2.2. Zone Status Topics (Read-Only)

**Important Topic Distinction:**

* **`control`** - Publishes simple string status values for current playback state and modes (e.g., `"play"`, `"mute_on"`)
* **`state`** - Publishes complete JSON zone state object with all information (see Section 13.5.1 below)

**Playback/Mode State**

| Status ID                 | Env Var Suffix         | Default Rel. Topic | Example Payload                      | Retained | Notes                      |
| :------------------------ | :--------------------- | :----------------- | :----------------------------------- | :------- | :------------------------- |
| `PLAYBACK_STATE` + Modes  | `_CONTROL_TOPIC`       | `control`          | `"play"`, `"track_repeat_on"`, etc. | Yes      | Simple string status values    |

**Track Management**

| Status ID              | Env Var Suffix         | Default Rel. Topic | Example Payload                  | Retained | Notes                    |
| :--------------------- | :--------------------- | :----------------- | :------------------------------- | :------- | :----------------------- |
| `TRACK_INDEX`          | `_TRACK_TOPIC`         | `track`            | `1`, `3`                         | Yes      | **1-based** index      |
| `TRACK_INFO`           | `_TRACK_INFO_TOPIC`    | `track/info`       | Full JSON `TrackInfo` object     | Yes      | Structure from Core Models |
| `TRACK_INFO_LENGTH`    | `_TRACK_INFO_LENGTH`   | `track/length`     | `300000` (in ms)                  | Yes      | Length of track `N`     |
| `TRACK_INFO_POSITION`  | `_TRACK_INFO_POSITION` | `track/position`   | `10000` (in ms)                  | Yes      | Position of track `N`   |
| `TRACK_INFO_TITLE`     | `_TRACK_INFO_TITLE`    | `track/title`      | `"Song Title"`                   | Yes      | Title of track `N`      |
| `TRACK_INFO_ARTIST`    | `_TRACK_INFO_ARTIST`   | `track/artist`    | `"Artist Name"`                  | Yes      | Artist of track `N`     |
| `TRACK_INFO_ALBUM`     | `_TRACK_INFO_ALBUM`    | `track/album`     | `"Album Name"`                   | Yes      | Album of track `N`      |
| `TRACK_REPEAT_STATUS`  | `_TRACK_REPEAT_TOPIC`  | `repeat/track`     | `true` / `false` (`1`/`0`)       | Yes      | Improved hierarchy       |

**Playlist Management**

| Status ID                 | Env Var Suffix             | Default Rel. Topic | Example Payload                    | Retained | Notes                      |
| :------------------------ | :------------------------- | :------------------- | :--------------------------------- | :------- | :------------------------- |
| `PLAYLIST_INDEX`          | `_PLAYLIST_TOPIC`        | `playlist`           | `1` (Radio), `2`                 | Yes      | **1-based** index        |
| `PLAYLIST_INFO`           | `_PLAYLIST_INFO_TOPIC`   | `playlist/info`      | Full JSON `PlaylistInfo` object  | Yes      | Structure from Core Models |
| `PLAYLIST_REPEAT_STATUS`  | `_PLAYLIST_REPEAT_TOPIC` | `repeat`             | `true` / `false` (`1`/`0`)         | Yes      |                            |
| `PLAYLIST_SHUFFLE_STATUS` | `_PLAYLIST_SHUFFLE_TOPIC`| `shuffle`            | `true` / `false` (`1`/`0`)         | Yes      |                            |

**Volume/Mute Control**

| Status ID          | Env Var Suffix         | Default Rel. Topic | Example Payload              | Retained | Notes                 |
| :----------------- | :--------------------- | :----------------- | :--------------------------- | :------- | :-------------------- |
| `VOLUME_STATUS`    | `_VOLUME_TOPIC`        | `volume`           | `75`                         | Yes      |                       |
| `MUTE_STATUS`      | `_MUTE_TOPIC`          | `mute`             | `true` / `false` (`1`/`0`)   | Yes      | Explicit Mute State   |

**General**

| Status ID         | Env Var Suffix | Default Rel. Topic | Example Payload                  | Retained | Notes                 |
| :---------------- | :------------- | :----------------- | :------------------------------- | :------- | :-------------------- |
| `ZONE_STATE`      | `_STATE_TOPIC` | `state`            | **Full JSON object (see 13.5.1)** | Yes      | Includes all status |

**Response Topics (Command Acknowledgments & Errors)**

| Status ID             | Env Var Suffix     | Default Rel. Topic | Example Payload                    | Retained | Notes                      |
| :-------------------- | :----------------- | :----------------- | :--------------------------------- | :------- | :------------------------- |
| `COMMAND_STATUS`      | `_STATUS_TOPIC`    | `status`           | `"ok"`, `"processing"`, `"done"`   | No       | Command acknowledgments    |
| `COMMAND_ERROR`       | `_ERROR_TOPIC`     | `error`            | `{"error": "Invalid track", "code": 400}` | No       | Error responses            |

#### 14.3.2.3. System-Level Topics (Discovery & Global Status)

System-level topics provide discovery and global status information across all zones.

**System Discovery**

| Status ID             | Env Var Suffix           | Default Topic          | Example Payload                    | Retained | Notes                      |
| :-------------------- | :----------------------- | :--------------------- | :--------------------------------- | :------- | :------------------------- |
| `ZONES_INFO`          | `_SYSTEM_MQTT_ZONES_TOPIC` | `snapdog/system/zones` | `[1, 2, 3]`                      | Yes      | List of available zones    |

**Global System Status**

| Status ID             | Env Var Suffix           | Default Topic          | Example Payload                    | Retained | Notes                      |
| :-------------------- | :----------------------- | :--------------------- | :--------------------------------- | :------- | :------------------------- |
| `SYSTEM_STATUS`       | `_SYSTEM_MQTT_STATUS_TOPIC` | `snapdog/status`      | `{"online": true, "uptime": 3600}` | Yes      | System health status       |
| `VERSION_INFO`        | `_SYSTEM_MQTT_VERSION_TOPIC` | `snapdog/version`     | `{"version": "2.0.0", "build": "..."}` | Yes      | Version information        |
| `SERVER_STATS`        | `_SYSTEM_MQTT_STATS_TOPIC` | `snapdog/stats`        | `{"cpu": 15.2, "memory": 512}`    | Yes      | Server performance stats   |
| `ERROR_STATUS`        | `_SYSTEM_MQTT_ERROR_TOPIC` | `snapdog/error`        | `{"error": "Service unavailable"}` | No       | System-level errors        |

#### 14.3.2.4. Payloads for `{zoneBaseTopic}control/set`

This topic accepts various string payloads to control multiple aspects:

| Command Functionality     | `{zoneBaseTopic}control/set` Payload |
| :------------------------ | :----------------------------------- |
| `PLAY`                    | `play`, `play url <url>`             |
| `PAUSE`                   | `pause`                              |
| `STOP`                    | `stop`                               |
| `TRACK`                   | `track <index>` (1-based)            |
| `TRACK_NEXT`              | `next`, `track_next`, `+`            |
| `TRACK_PREVIOUS`          | `previous`, `track_previous`, `-`    |
| `TRACK_REPEAT`            | `track_repeat_on`, `track_repeat_off`|
| `TRACK_REPEAT_TOGGLE`     | `track_repeat_toggle`                |
| `PLAYLIST`                | `playlist <index_or_id>` (1=Radio)   |
| `PLAYLIST_NEXT`           | `playlist_next`                      |
| `PLAYLIST_PREVIOUS`       | `playlist_previous`                  |
| `PLAYLIST_SHUFFLE`        | `shuffle_on`, `shuffle_off`          |
| `PLAYLIST_SHUFFLE_TOGGLE` | `shuffle_toggle`                     |
| `PLAYLIST_REPEAT`         | `repeat_on`, `repeat_off`            |
| `PLAYLIST_REPEAT_TOGGLE`  | `repeat_toggle`                      |
| `MUTE`                    | `mute_on`, `mute_off`                |
| `MUTE_TOGGLE`             | `mute_toggle`                        |
| `VOLUME`                  | `volume <level>` (0-100)             |
| `VOLUME_UP`               | `volume_up`, `volume +<step>`        |
| `VOLUME_DOWN`             | `volume_down`, `volume -<step>`      |

#### 14.3.2.5. Standardized Payload Patterns

All boolean topics accept consistent payload formats for maximum compatibility:

**Boolean Values**

* **True**: `"true"`, `"1"`, `"on"`, `"yes"`
* **False**: `"false"`, `"0"`, `"off"`, `"no"`
* **Toggle**: `"toggle"`

**Examples:**

```
snapdog/zone/1/repeat/track     → "true" | "false" | "1" | "0" | "on" | "off" | "toggle"
snapdog/zone/1/shuffle/set      → "true" | "false" | "1" | "0" | "on" | "off" | "toggle"
snapdog/zone/1/mute/set         → "true" | "false" | "1" | "0" | "on" | "off" | "toggle"
```

**Numeric Values**

* **Volume**: `0`-`100` (integer)
* **Track/Playlist Index**: `1`-based integers
* **Volume Steps**: `+5`, `-3`, `+`, `-` (default step: 5)

#### 14.3.2.6. Status Values for `{zoneBaseTopic}control`

This topic publishes simple string representations for various states:

| Status Functionality    | `{zoneBaseTopic}control` Status Value         |
| :---------------------- | :-------------------------------------------- |
| `PLAYBACK_STATE`        | `play`, `pause`, `stop`                       |
| `TRACK_REPEAT_STATUS`   | `track_repeat_on`, `track_repeat_off`         |
| `PLAYLIST_SHUFFLE_STATUS`| `shuffle_on`, `shuffle_off`                   |
| `PLAYLIST_REPEAT_STATUS`| `repeat_on`, `repeat_off`   |
| `MUTE_STATUS`           | `mute_on`, `mute_off`                         |

### 14.3.3. Zone KNX Implementation

Uses `Knx.Falcon.GroupAddress`. GAs configured via `SNAPDOG_ZONE_{n}_KNX_{SUFFIX}` Env Vars (Sec 10). DPT Value Mapping follows standard KNX conventions (see Section 20 for dependencies). **Indices 1-based.** Report `0` on Status GA if > 255.

#### 14.3.3.1. KNX Zone Command Group Addresses

**Playback Control**

| Command ID        | DPT     | Env Var Suffix            | Notes                                 |
| :---------------- | :------ | :------------------------ | :------------------------------------ |
| `PLAY` / `PAUSE`  | 1.001   | `_KNX_PLAY`               | Send 1=Play, 0=Pause                  |
| `STOP`            | 1.001   | `_KNX_STOP`               | Send 1=Stop                           |

**Track Management**

| Command ID            | DPT     | Env Var Suffix            | Notes                 |
| :-------------------- | :------ | :------------------------ | :-------------------- |
| `TRACK_NEXT`          | 1.007   | `_KNX_TRACK_NEXT`         | Send 1 to activate    |
| `TRACK_PREVIOUS`      | 1.007   | `_KNX_TRACK_PREVIOUS`     | Send 1 to activate    |
| `TRACK_REPEAT`        | 1.001   | `_KNX_TRACK_REPEAT`       | Send 0=Off, 1=On      |
| `TRACK_REPEAT_TOGGLE` | 1.001   | `_KNX_TRACK_REPEAT_TOGGLE`| Send 1 to toggle      |
| `TRACK`               | 5.010   | `_KNX_TRACK`              | Send 1-based index  |

**Playlist Management**

| Command ID                | DPT     | Env Var Suffix                 | Notes                 |
| :------------------------ | :------ | :----------------------------- | :-------------------- |
| `PLAYLIST`                | 5.010   | `_KNX_PLAYLIST`                | Send 1-based index  |
| `PLAYLIST_NEXT`           | 1.007   | `_KNX_PLAYLIST_NEXT`           | Send 1 to activate    |
| `PLAYLIST_PREVIOUS`       | 1.007   | `_KNX_PLAYLIST_PREVIOUS`       | Send 1 to activate    |
| `PLAYLIST_SHUFFLE`        | 1.001   | `_KNX_PLAYLIST_SHUFFLE`        | Send 0=Off, 1=On      |
| `PLAYLIST_SHUFFLE_TOGGLE` | 1.001   | `_KNX_PLAYLIST_SHUFFLE_TOGGLE` | Send 1 to toggle      |
| `PLAYLIST_REPEAT`         | 1.001   | `_KNX_PLAYLIST_REPEAT`         | Send 0=Off, 1=On      |
| `PLAYLIST_REPEAT_TOGGLE`  | 1.001   | `_KNX_PLAYLIST_REPEAT_TOGGLE`  | Send 1 to toggle      |

**Volume/Mute Control**

| Command ID    | DPT     | Env Var Suffix      | Notes                 |
| :------------ | :------ | :------------------ | :-------------------- |
| `VOLUME`      | 5.001   | `_KNX_VOLUME`       | Send 0-100%           |
| `VOLUME_UP`   | 3.007   | `_KNX_VOLUME_DIM`   | Send Dim Up command   |
| `VOLUME_DOWN` | 3.007   | `_KNX_VOLUME_DIM`   | Send Dim Down command |
| `MUTE`        | 1.001   | `_KNX_MUTE`         | Send 0=Off, 1=On      |
| `MUTE_TOGGLE` | 1.001   | `_KNX_MUTE_TOGGLE`  | Send 1 to toggle      |

#### 14.3.3.2. KNX Zone Status Group Addresses

**Playback Control**

| Status ID        | DPT     | Env Var Suffix         | Notes                  |
| :--------------- | :------ | :--------------------- | :--------------------- |
| `PLAYBACK_STATE` | 1.001   | `_KNX_PLAYBACK_STATUS` | Send 1=Play, 0=Pause/Stop |

**Track Management**

| Status ID             | DPT     | Env Var Suffix             | Notes                  |
| :-------------------- | :------ | :------------------------- | :--------------------- |
| `TRACK_REPEAT_STATUS` | 1.001   | `_KNX_TRACK_REPEAT_STATUS` | Send 0=Off, 1=On       |
| `TRACK_INDEX`         | 5.010   | `_KNX_TRACK_STATUS`        | Send 1-based, 0 if>255 |

**Playlist Management**

| Status ID                 | DPT     | Env Var Suffix                   | Notes                  |
| :------------------------ | :------ | :------------------------------- | :--------------------- |
| `PLAYLIST_INDEX`          | 5.010   | `_KNX_PLAYLIST_STATUS`           | Send 1-based, 0 if>255 |
| `PLAYLIST_SHUFFLE_STATUS` | 1.001   | `_KNX_PLAYLIST_SHUFFLE_STATUS`   | Send 0=Off, 1=On       |
| `PLAYLIST_REPEAT_STATUS`  | 1.001   | `_KNX_PLAYLIST_REPEAT_STATUS`    | Send 0=Off, 1=On       |

**Volume/Mute Control**

| Status ID         | DPT     | Env Var Suffix         | Notes             |
| :---------------- | :------ | :--------------------- | :---------------- |
| `VOLUME_STATUS`   | 5.001   | `_KNX_VOLUME_STATUS`   | Send 0-100%       |
| `MUTE_STATUS`     | 1.001   | `_KNX_MUTE_STATUS`     | Send 0=Off, 1=On  |

## 14.4. Client Commands and Status

### 14.4.1. Client Functionality

**Volume & Mute**

| Command/Status ID    | Description             | Essential Information / Type          | Direction        | Notes                     |
| :------------------- | :---------------------- | :------------------------------------ | :--------------- | :------------------------ |
| `CLIENT_VOLUME`      | Set client volume       | `ClientIndex` (int), `Volume` (int, 0-100) | Command (Set)    | Sets individual client vol|
| `CLIENT_VOLUME_STATUS`| Current client volume    | `ClientIndex` (int), `Volume` (int, 0-100) | Status (Publish) |                           |
| `CLIENT_MUTE`        | Set client mute         | `ClientIndex` (int), `Enabled` (bool)      | Command (Set)    |                           |
| `CLIENT_MUTE_TOGGLE` | Toggle client mute      | `ClientIndex` (int)                        | Command (Set)    |                           |
| `CLIENT_MUTE_STATUS` | Current client mute state | `ClientIndex` (int), `Enabled` (bool)      | Status (Publish) |                           |

**Configuration & State**

| Command/Status ID    | Description             | Essential Information / Type          | Direction        | Notes                     |
| :------------------- | :---------------------- | :------------------------------------ | :--------------- | :------------------------ |
| `CLIENT_LATENCY`     | Set client latency      | `ClientIndex` (int), `LatencyMs` (int)   | Command (Set)    |                           |
| `CLIENT_LATENCY_STATUS`| Current client latency  | `ClientIndex` (int), `LatencyMs` (int)   | Status (Publish) |                           |
| `CLIENT_ZONE`        | Assign client to zone   | `ClientIndex` (int), `ZoneIndex` (int, 1-based)| Command (Set)    | Assigns client to group   |
| `CLIENT_ZONE_STATUS` | Current assigned zone ID| `ClientIndex` (int), `ZoneIndex` (int?, 1-based)| Status (Publish) |                           |
| `CLIENT_CONNECTED`   | Client connection status| `ClientIndex` (int), `IsConnected` (bool)  | Status (Publish) |                           |
| `CLIENT_STATE`       | Complete client state   | `ClientIndex` (int), `ClientState` object  | Status (Publish) |                           |

### 14.4.2. Client MQTT Implementation

Base topic: `SNAPDOG_CLIENT_m_MQTT_BASE_TOPIC` (default: `snapdog/clients/{m}/`).

#### 14.4.2.1. Client Command Topics (`/set`)

**Volume/Mute**

| Command ID           | Env Var Suffix         | Default Rel. Topic | Example Payloads              |
| :------------------- | :--------------------- | :--------------------- | :---------------------------- |
| `CLIENT_VOLUME`      | `_VOLUME_SET_TOPIC`    | `volume/set`           | `0`-`100`                     |
| `CLIENT_MUTE`        | `_MUTE_SET_TOPIC`      | `mute/set`             | `true` / `false`, `1` / `0`   |
| `CLIENT_MUTE_TOGGLE` | `_MUTE_SET_TOPIC`      | `mute/set`             | `toggle`                      |

**Config & State**

| Command ID        | Env Var Suffix       | Default Rel. Topic | Example Payloads              |
| :---------------- | :------------------- | :------------------- | :---------------------------- |
| `CLIENT_LATENCY`  | `_LATENCY_SET_TOPIC` | `latency/set`        | `<ms>`                        |
| `CLIENT_ZONE`     | `_ZONE_SET_TOPIC`    | `zone/set`           | `<zone_id>` (1-based)         |

#### 14.4.2.2. Client Status Topics (Read-Only)

**Volume/Mute**

| Status ID              | Env Var Suffix         | Default Rel. Topic | Example Payload           | Retained |
| :--------------------- | :--------------------- | :--------------------- | :------------------------ | :------- |
| `CLIENT_VOLUME_STATUS` | `_VOLUME_TOPIC`        | `volume`               | `80`                      | Yes      |
| `CLIENT_MUTE_STATUS`   | `_MUTE_TOPIC`          | `mute`                 | `true` / `false`          | Yes      |

**Config & State**

| Status ID               | Env Var Suffix         | Default Rel. Topic | Example Payload           | Retained |
| :---------------------- | :--------------------- | :--------------------- | :------------------------ | :------- |
| `CLIENT_CONNECTED`      | `_CONNECTED_TOPIC`     | `connected`            | `true` / `false`, `1` / `0` | Yes      |
| `CLIENT_LATENCY_STATUS` | `_LATENCY_TOPIC`       | `latency`              | `20`                      | Yes      |
| `CLIENT_ZONE_STATUS`    | `_ZONE_TOPIC`          | `zone`                 | `1`                       | Yes      |
| `CLIENT_STATE`          | `_STATE_TOPIC`         | `state`                | Full JSON object (13.5.2)  | Yes      |

### 14.4.3. Client KNX Implementation

Uses `Knx.Falcon.GroupAddress`. GAs configured via `SNAPDOG_CLIENT_{m}_KNX_{SUFFIX}` Env Vars (Sec 10). DPT Value Mapping follows standard KNX conventions (see Section 20 for dependencies). **Zone indices 1-based.** Report `0` on Status GA if index > 255.

#### 14.4.3.1. KNX Client Command Group Addresses

**Volume/Mute**

| Command ID           | DPT     | Env Var Suffix       | Notes              |
| :------------------- | :------ | :------------------- | :----------------- |
| `CLIENT_VOLUME`      | 5.001   | `_KNX_VOLUME`        | Send 0-100%        |
| `CLIENT_MUTE`        | 1.001   | `_KNX_MUTE`          | Send 0=Off, 1=On   |
| `CLIENT_MUTE_TOGGLE` | 1.001   | `_KNX_MUTE_TOGGLE`   | Send 1 to toggle   |

**Config & State**

| Command ID        | DPT     | Env Var Suffix   | Notes              |
| :---------------- | :------ | :--------------- | :----------------- |
| `CLIENT_LATENCY`  | 7.001   | `_KNX_LATENCY`   | Send ms            |
| `CLIENT_ZONE`     | 5.010   | `_KNX_ZONE`      | Send 1-based index |

#### 14.4.3.2. KNX Client Status Group Addresses

**Volume/Mute**

| Status ID              | DPT     | Env Var Suffix         | Notes             |
| :--------------------- | :------ | :--------------------- | :---------------- |
| `CLIENT_VOLUME_STATUS` | 5.001   | `_KNX_VOLUME_STATUS`   | Send 0-100%       |
| `CLIENT_MUTE_STATUS`   | 1.001   | `_KNX_MUTE_STATUS`     | Send 0=Off, 1=On  |

**Config & State**

| Status ID               | DPT     | Env Var Suffix         | Notes                 |
| :---------------------- | :------ | :--------------------- | :-------------------- |
| `CLIENT_LATENCY_STATUS` | 7.001   | `_KNX_LATENCY_STATUS`  | Send ms               |
| `CLIENT_ZONE_STATUS`    | 5.010   | `_KNX_ZONE_STATUS`     | Send 1-based, 0 if>255|
| `CLIENT_CONNECTED`      | 1.002   | `_KNX_CONNECTED_STATUS`| Send 0=Off, 1=On      |

## 14.5. Zone and Client State Objects (JSON Examples)

### 14.5.1. Complete Zone State (JSON)

Published to `{zoneBaseTopic}/state`.

```json
{
  "id": 1,
  "name": "Living Room",
  "playback_state": "play", // "play", "pause", "stop"
  "volume": 65,
  "mute": false,
  "track_repeat": false,
  "playlist_repeat": false,
  "playlist_shuffle": true,
  "snapcastGroupId": "group-uuid-1",
  "snapcastStreamId": "stream-fifo-1",
  "isSnapcastGroupMuted": false, // Raw state from Snapcast
  "playlist": {
    "id": "pl.10", // Can be "radio"
    "name": "Jazz Classics", // Can be "Radio Stations"
    "index": 2 // 1-based playlist index (1=Radio, 2=First Subsonic etc.) - Optional
  },
  "track": {
    "index": 1, // 1-based track index within the current playlist
    "id": "track_12345", // or stream URL for radio
    "title": "Take Five",
    "artist": "Dave Brubeck", // or "Radio"
    "album": "Time Out", // or Playlist Name
    "duration_sec": 325, // null for radio/streams
    "position_sec": 142, // Current playback position
    "coverArtUrl": "...",
    "source": "subsonic" // or "radio"
  },
  "clients": [ 1, 2 ], // SnapDog2 Client IDs currently in this zone
  "timestamp": "2025-04-05T21:30:00Z" // Example ISO8601 UTC
}
```

### 14.5.2. Complete Client State (JSON)

Published to `{clientBaseTopic}/state`.

```json
{
  "id": 1, // SnapDog2 internal client ID
  "snapcastId": "00:11:22:AA:BB:CC",
  "name": "Living Room Speaker", // SnapDog2 configured Name
  "mac": "00:11:22:AA:BB:CC",
  "connected": true,
  "volume": 80, // 0-100
  "mute": false,
  "latency_ms": 20,
  "zoneIndex": 1, // 1-based SnapDog2 Zone ID it's currently assigned to (null if unassigned)
  "configuredSnapcastName": "Snapclient on pi", // Name from Snapcast client config
  "lastSeen": "2025-04-05T21:25:10Z", // Example ISO8601 UTC
  "hostIpAddress": "192.168.1.50",
  "hostName": "livingroom-pi",
  "hostOs": "Raspbian GNU/Linux 11 (bullseye)",
  "hostArch": "aarch64",
  "snapClientVersion": "0.27.0",
  "snapClientProtocolVersion": 2,
  "timestamp": "2025-04-05T21:30:00Z"
}
```
