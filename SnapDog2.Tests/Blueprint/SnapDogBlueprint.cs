//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Tests.Blueprint;

/// <summary>
/// Complete SnapDog2 system specification as fluent blueprint.
/// Single source of truth for all commands, status, protocols, and implementation requirements.
/// </summary>
public static class SnapDogBlueprint
{
    public static readonly Blueprint Spec = Blueprint
        .Define()
        // === ZONE PLAYBACK COMMANDS ===
        //
        .Command("PLAY")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/play")
        .Knx()
        .Post("/api/v1/zones/{zoneIndex:int}/play")
        .Description("Start playback in a zone")
        //
        .Command("PAUSE")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/pause")
        .Knx()
        .Post("/api/v1/zones/{zoneIndex:int}/pause")
        .Description("Pause playback in a zone")
        //
        .Command("STOP")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/stop")
        .Knx()
        .Post("/api/v1/zones/{zoneIndex:int}/stop")
        .Description("Stop playback in a zone")
        //
        // === VOLUME COMMANDS ===
        //
        .Command("VOLUME")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/volume/set")
        .Put("/api/v1/zones/{zoneIndex:int}/volume")
        .Description("Set zone volume to specific level")
        .Exclude(Protocol.Knx, "Handled by dedicated KNX volume actuators")
        //
        .Command("VOLUME_UP")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/volume/up")
        .Post("/api/v1/zones/{zoneIndex:int}/volume/up")
        .Description("Increase zone volume")
        .Exclude(Protocol.Knx, "Handled by dedicated KNX volume actuators")
        //
        .Command("VOLUME_DOWN")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/volume/down")
        .Post("/api/v1/zones/{zoneIndex:int}/volume/down")
        .Description("Decrease zone volume")
        .Exclude(Protocol.Knx, "Handled by dedicated KNX volume actuators")
        //
        // === MUTE COMMANDS ===
        //
        .Command("MUTE")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/mute/set")
        .Put("/api/v1/zones/{zoneIndex:int}/mute")
        .Description("Set zone mute state")
        .Exclude(Protocol.Knx, "Handled by dedicated KNX mute actuators")
        //
        .Command("MUTE_TOGGLE")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/mute/toggle")
        .Post("/api/v1/zones/{zoneIndex:int}/mute/toggle")
        .Description("Toggle zone mute state")
        .Exclude(Protocol.Knx, "Toggle commands require state synchronization")
        //
        // === TRACK COMMANDS ===
        //
        .Command("TRACK")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/set")
        .Put("/api/v1/zones/{zoneIndex:int}/track")
        .Description("Set current track by index")
        .Exclude(Protocol.Knx, "Complex track navigation not suitable for building automation")
        //
        .Command("TRACK_NEXT")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/next")
        .Post("/api/v1/zones/{zoneIndex:int}/next")
        .Description("Skip to next track")
        .Exclude(Protocol.Knx, "Complex track navigation not suitable for building automation")
        //
        .Command("TRACK_PREVIOUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/previous")
        .Post("/api/v1/zones/{zoneIndex:int}/previous")
        .Description("Skip to previous track")
        .Exclude(Protocol.Knx, "Complex track navigation not suitable for building automation")
        //
        .Command("TRACK_PLAY_INDEX")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/play/track")
        .Knx()
        .Post("/api/v1/zones/{zoneIndex:int}/play/track")
        .Description("Play specific track by index")
        //
        .Command("TRACK_PLAY_URL")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/play/url")
        .Post("/api/v1/zones/{zoneIndex:int}/play/url")
        .Description("Play direct URL stream")
        .Exclude(Protocol.Knx, "KNX cannot transmit URL strings effectively")
        //
        .Command("TRACK_POSITION")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/position/set")
        .Put("/api/v1/zones/{zoneIndex:int}/track/position")
        .Description("Seek to position in track")
        .Exclude(Protocol.Knx, "KNX lacks precision for millisecond-based seeking")
        //
        .Command("TRACK_PROGRESS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/progress/set")
        .Put("/api/v1/zones/{zoneIndex:int}/track/progress")
        .Description("Seek to progress percentage")
        .Exclude(Protocol.Knx, "KNX lacks precision for percentage-based seeking")
        //
        .Command("TRACK_REPEAT")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/repeat/track/set")
        .Knx()
        .Put("/api/v1/zones/{zoneIndex:int}/repeat/track")
        .Description("Set track repeat mode")
        //
        .Command("TRACK_REPEAT_TOGGLE")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/repeat/track/toggle")
        .Knx()
        .Post("/api/v1/zones/{zoneIndex:int}/repeat/track/toggle")
        .Description("Toggle track repeat mode")
        //
        // === PLAYLIST COMMANDS ===
        //
        .Command("PLAYLIST")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/playlist/set")
        .Put("/api/v1/zones/{zoneIndex:int}/playlist")
        .Description("Set current playlist")
        //
        .Command("PLAYLIST_NEXT")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/next/playlist")
        .Post("/api/v1/zones/{zoneIndex:int}/next/playlist")
        .Description("Switch to next playlist")
        .Exclude(Protocol.Knx, "Complex playlist navigation not suitable for building automation")
        //
        .Command("PLAYLIST_PREVIOUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/previous/playlist")
        .Post("/api/v1/zones/{zoneIndex:int}/previous/playlist")
        .Description("Switch to previous playlist")
        .Exclude(Protocol.Knx, "Complex playlist navigation not suitable for building automation")
        //
        .Command("PLAYLIST_SHUFFLE")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/shuffle/set")
        .Put("/api/v1/zones/{zoneIndex:int}/shuffle")
        .Description("Set playlist shuffle mode")
        .Exclude(Protocol.Knx, "Complex state management not suitable for KNX")
        //
        .Command("PLAYLIST_SHUFFLE_TOGGLE")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/shuffle/toggle")
        .Post("/api/v1/zones/{zoneIndex:int}/shuffle/toggle")
        .Description("Toggle playlist shuffle mode")
        .Exclude(Protocol.Knx, "Toggle commands require state synchronization")
        //
        .Command("PLAYLIST_REPEAT")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/repeat/playlist/set")
        .Knx()
        .Put("/api/v1/zones/{zoneIndex:int}/repeat")
        .Description("Set playlist repeat mode")
        //
        .Command("PLAYLIST_REPEAT_TOGGLE")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/repeat/playlist/toggle")
        .Knx()
        .Post("/api/v1/zones/{zoneIndex:int}/repeat/toggle")
        .Description("Toggle playlist repeat mode")
        //
        // === CLIENT COMMANDS ===
        //
        .Command("CLIENT_VOLUME")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/volume/set")
        .Put("/api/v1/clients/{clientIndex:int}/volume")
        .Description("Set client volume level")
        .Exclude(Protocol.Knx, "Client-specific network settings not suitable for building automation")
        //
        .Command("CLIENT_VOLUME_UP")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/volume/up")
        .Post("/api/v1/clients/{clientIndex:int}/volume/up")
        .Description("Increase client volume")
        .Exclude(Protocol.Knx, "Client-specific network settings not suitable for building automation")
        //
        .Command("CLIENT_VOLUME_DOWN")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/volume/down")
        .Post("/api/v1/clients/{clientIndex:int}/volume/down")
        .Description("Decrease client volume")
        .Exclude(Protocol.Knx, "Client-specific network settings not suitable for building automation")
        //
        .Command("CLIENT_MUTE")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/mute/set")
        .Put("/api/v1/clients/{clientIndex:int}/mute")
        .Description("Set client mute state")
        .Exclude(Protocol.Knx, "Client-specific network settings not suitable for building automation")
        //
        .Command("CLIENT_MUTE_TOGGLE")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/mute/toggle")
        .Post("/api/v1/clients/{clientIndex:int}/mute/toggle")
        .Description("Toggle client mute state")
        .Exclude(Protocol.Knx, "Client-specific network settings not suitable for building automation")
        //
        .Command("CLIENT_LATENCY")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/latency/set")
        .Put("/api/v1/clients/{clientIndex:int}/latency")
        .Description("Set client audio latency")
        .Exclude(Protocol.Knx, "Network-specific setting not suitable for building automation")
        //
        .Command("CLIENT_NAME")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/name/set")
        .Put("/api/v1/clients/{clientIndex:int}/name")
        .Description("Set client name")
        .Exclude(Protocol.Knx, "Client-specific network settings not suitable for building automation")
        //
        .Command("CLIENT_ZONE")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/zone/set")
        .Put("/api/v1/clients/{clientIndex:int}/zone")
        .Description("Assign client to zone")
        .Exclude(Protocol.Knx, "Client-specific network settings not suitable for building automation")
        //
        // === CONTROL COMMANDS ===
        //
        .Command("CONTROL")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/control/set")
        .Post("/api/v1/zones/{zoneIndex:int}/control")
        .Description("General zone control command")
        //
        .Command("ZONE_NAME")
        .Zone()
        .Description("Set zone name")
        .Exclude(Protocol.Knx, "KNX cannot transmit string names effectively")
        .Exclude(Protocol.Mqtt, "MQTT should not change zone name")
        .Exclude(Protocol.Api, "API should not change zone name")
        //
        // === SYSTEM STATUS ===
        //
        .Status("SYSTEM_STATUS")
        .Global()
        .Mqtt("snapdog/system/status")
        .Get("/api/v1/system/status")
        .Description("Overall system health and status")
        .Exclude(Protocol.Knx, "Read-only system information not actionable via KNX")
        //
        .Status("VERSION_INFO")
        .Global()
        .Mqtt("snapdog/system/version")
        .Get("/api/v1/system/version")
        .Description("System version information")
        //
        .Status("SERVER_STATS")
        .Global()
        .Mqtt("snapdog/system/stats")
        .Get("/api/v1/system/stats")
        .Description("Server performance statistics")
        //
        // === HEALTH ENDPOINTS ===
        //
        .Status("HEALTH_STATUS")
        .Global()
        .Get("/api/health")
        .Description("Overall health status")
        .Exclude(Protocol.Mqtt, "Infrastructure endpoint not suitable for MQTT")
        .Exclude(Protocol.Knx, "Infrastructure endpoint not suitable for KNX")
        //
        .Status("HEALTH_READY")
        .Global()
        .Get("/api/health/ready")
        .Description("Readiness probe for container orchestration")
        .Exclude(Protocol.Mqtt, "Infrastructure endpoint not suitable for MQTT")
        .Exclude(Protocol.Knx, "Infrastructure endpoint not suitable for KNX")
        //
        .Status("HEALTH_LIVE")
        .Global()
        .Get("/api/health/live")
        .Description("Liveness probe for container orchestration")
        .Exclude(Protocol.Mqtt, "Infrastructure endpoint not suitable for MQTT")
        .Exclude(Protocol.Knx, "Infrastructure endpoint not suitable for KNX")
        //
        .Status("ZONES_INFO")
        .Global()
        .Get("/api/v1/zones")
        .Description("Information about all zones")
        .Exclude(Protocol.Knx, "Read-only system information not actionable via KNX")
        .Exclude(Protocol.Mqtt, "No single MQTT topic for all zones info")
        //
        // === MEDIA ENDPOINTS ===
        //
        .Status("MEDIA_PLAYLISTS")
        .Global()
        .Get("/api/v1/media/playlists")
        .Description("List of all available playlists")
        .Exclude(Protocol.Mqtt, "Media browsing not suitable for MQTT")
        .Exclude(Protocol.Knx, "Media browsing not suitable for KNX")
        //
        .Status("MEDIA_PLAYLIST_INFO")
        .Global()
        .Get("/api/v1/media/playlists/{playlistIndex}")
        .Description("Detailed information about a specific playlist")
        .Exclude(Protocol.Mqtt, "Media browsing not suitable for MQTT")
        .Exclude(Protocol.Knx, "Media browsing not suitable for KNX")
        //
        .Status("MEDIA_PLAYLIST_TRACKS")
        .Global()
        .Get("/api/v1/media/playlists/{playlistIndex}/tracks")
        .Description("List of tracks in a specific playlist")
        .Exclude(Protocol.Mqtt, "Media browsing not suitable for MQTT")
        .Exclude(Protocol.Knx, "Media browsing not suitable for KNX")
        //
        .Status("MEDIA_PLAYLIST_TRACK_INFO")
        .Global()
        .Get("/api/v1/media/playlists/{playlistIndex}/tracks/{trackIndex}")
        .Description("Information about a specific track in a playlist")
        .Exclude(Protocol.Mqtt, "Media browsing not suitable for MQTT")
        .Exclude(Protocol.Knx, "Media browsing not suitable for KNX")
        //
        .Status("MEDIA_TRACK_INFO")
        .Global()
        .Get("/api/v1/media/tracks/{trackIndex}")
        .Description("Information about a specific track")
        .Exclude(Protocol.Mqtt, "Media browsing not suitable for MQTT")
        .Exclude(Protocol.Knx, "Media browsing not suitable for KNX")
        //
        .Status("CLIENTS_INFO")
        .Global()
        .Get("/api/v1/clients")
        .Description("Information about all clients")
        .Exclude(Protocol.Knx, "Read-only system information not actionable via KNX")
        .Exclude(Protocol.Mqtt, "No single MQTT topic for all clients info")
        .Exclude(Protocol.Mqtt, "No single MQTT topic for all clients info")
        //
        // === ZONE STATUS ===
        //
        .Status("ZONE_STATE")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/state")
        .Get("/api/v1/zones/{zoneIndex:int}")
        .Description("Complete zone state information")
        //
        .Status("ZONE_NAME_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/name")
        .Get("/api/v1/zones/{zoneIndex:int}/name")
        .Description("Zone name")
        .Exclude(Protocol.Knx, "Read-only string information not actionable via KNX")
        //
        .Status("VOLUME_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/volume")
        .Get("/api/v1/zones/{zoneIndex:int}/volume")
        .Description("Current zone volume level")
        //
        .Status("MUTE_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/mute")
        .Get("/api/v1/zones/{zoneIndex:int}/mute")
        .Description("Current zone mute state")
        //
        .Status("PLAYBACK_STATE")
        .Zone()
        .Get("/api/v1/zones/{zoneIndex:int}/playback")
        .Exclude(Protocol.Knx, "string-based playback state not actionable via KNX")
        .Exclude(Protocol.Mqtt, "string-based playback state not actionable via MQTT")
        .Description("Current playback state")
        //
        // === TRACK STATUS ===
        //
        .Status("TRACK_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track")
        .Knx()
        .Get("/api/v1/zones/{zoneIndex:int}/track")
        .Description("Current track information")
        //
        .Status("TRACK_METADATA")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/metadata")
        .Get("/api/v1/zones/{zoneIndex:int}/track/metadata")
        .Description("Track metadata (title, artist, album, etc.)")
        .Exclude(Protocol.Knx, "Read-only metadata not actionable via KNX")
        //
        .Status("TRACK_METADATA_DURATION")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/duration")
        .Get("/api/v1/zones/{zoneIndex:int}/track/duration")
        .Description("Track duration in milliseconds")
        .Exclude(Protocol.Knx, "Read-only metadata not actionable via KNX")
        //
        .Status("TRACK_METADATA_TITLE")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/title")
        .Knx()
        .Get("/api/v1/zones/{zoneIndex:int}/track/title")
        .Description("Track title")
        //
        .Status("TRACK_METADATA_ARTIST")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/artist")
        .Knx()
        .Get("/api/v1/zones/{zoneIndex:int}/track/artist")
        .Description("Track artist")
        //
        .Status("TRACK_METADATA_ALBUM")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/album")
        .Knx()
        .Get("/api/v1/zones/{zoneIndex:int}/track/album")
        .Description("Track album")
        //
        .Status("TRACK_METADATA_COVER")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/cover")
        .Get("/api/v1/zones/{zoneIndex:int}/track/cover")
        .Description("Track cover art URL")
        .Exclude(Protocol.Knx, "KNX cannot transmit URL strings effectively")
        //
        .Status("TRACK_PLAYING_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/playing")
        .Knx()
        .Get("/api/v1/zones/{zoneIndex:int}/track/playing")
        .Description("Current playing state")
        //
        .Status("TRACK_POSITION_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/position")
        .Get("/api/v1/zones/{zoneIndex:int}/track/position")
        .Description("Current track position")
        //
        .Status("TRACK_PROGRESS_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/track/progress")
        .Knx()
        .Get("/api/v1/zones/{zoneIndex:int}/track/progress")
        .Description("Track progress percentage")
        //
        .Status("TRACK_REPEAT_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/repeat/track")
        .Knx()
        .Get("/api/v1/zones/{zoneIndex:int}/repeat/track")
        .Description("Track repeat mode status")
        //
        // === PLAYLIST STATUS ===
        //
        .Status("PLAYLIST_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/playlist")
        .Get("/api/v1/zones/{zoneIndex:int}/playlist")
        .Description("Current playlist information")
        //
        .Status("PLAYLIST_INFO")
        .Zone()
        .Get("/api/v1/zones/{zoneIndex:int}/playlist/info")
        .Exclude(Protocol.Mqtt, "Playlist info available via individual playlist/* topics")
        .Description("Detailed playlist information")
        //
        .Status("PLAYLIST_NAME_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/playlist/name")
        .Get("/api/v1/zones/{zoneIndex:int}/playlist/name")
        .Description("Current playlist name")
        .RecentlyAdded()
        //
        .Status("PLAYLIST_COUNT_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/playlist/count")
        .Get("/api/v1/zones/{zoneIndex:int}/playlist/count")
        .Description("Number of tracks in current playlist")
        .RecentlyAdded()
        //
        .Status("PLAYLIST_SHUFFLE_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/shuffle")
        .Get("/api/v1/zones/{zoneIndex:int}/shuffle")
        .Description("Playlist shuffle mode status")
        //
        .Status("PLAYLIST_REPEAT_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/repeat/playlist")
        .Get("/api/v1/zones/{zoneIndex:int}/repeat")
        .Description("Playlist repeat mode status")
        //
        // === CLIENT STATUS ===
        //
        .Status("CLIENT_STATE")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/state")
        .Get("/api/v1/clients/{clientIndex:int}")
        .Description("Complete client state information")
        //
        .Status("CLIENT_VOLUME_STATUS")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/volume")
        .Get("/api/v1/clients/{clientIndex:int}/volume")
        .Description("Current client volume level")
        //
        .Status("CLIENT_MUTE_STATUS")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/mute")
        .Get("/api/v1/clients/{clientIndex:int}/mute")
        .Description("Current client mute state")
        //
        .Status("CLIENT_LATENCY_STATUS")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/latency")
        .Get("/api/v1/clients/{clientIndex:int}/latency")
        .Description("Current client latency setting")
        //
        .Status("CLIENT_ZONE_STATUS")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/zone")
        .Get("/api/v1/clients/{clientIndex:int}/zone")
        .Description("Client zone assignment")
        //
        .Status("CLIENT_NAME_STATUS")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/name")
        .Get("/api/v1/clients/{clientIndex:int}/name")
        .Description("Client name")
        .RecentlyAdded()
        //
        .Status("CLIENT_CONNECTED")
        .Client()
        .Mqtt("snapdog/client/{clientIndex}/connected")
        .Get("/api/v1/clients/{clientIndex:int}/connected")
        .Description("Client connection status")
        //
        // === MQTT-ONLY STATUS ===
        //
        .Status("CONTROL_STATUS")
        .Zone()
        .Mqtt("snapdog/zone/{zoneIndex}/control")
        .Description("Control command execution status (MQTT notifications only)")
        //
        // === COMMAND RESPONSE STATUS ===
        //
        .Status("COMMAND_STATUS")
        .Global()
        .Get("/api/v1/system/commands/status")
        .Exclude(Protocol.Mqtt, "Command success/failure inferred from state changes")
        .Description("Command execution status")
        //
        .Status("COMMAND_ERROR")
        .Global()
        .Get("/api/v1/system/commands/errors")
        .Exclude(Protocol.Mqtt, "Command success/failure inferred from state changes")
        .Description("Command execution errors")
        //
        .Status("SYSTEM_ERROR")
        .Global()
        .Mqtt("snapdog/system/error")
        .Get("/api/v1/system/errors")
        .Description("System-level errors")
        .Build();
}
