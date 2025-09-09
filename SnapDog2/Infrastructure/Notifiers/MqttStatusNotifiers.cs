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
namespace SnapDog2.Infrastructure.Notifiers;

using SnapDog2.Shared.Attributes;

[MqttStatusNotifier("SYSTEM_STATUS")]
public class SystemStatusMqttNotifier
{
}

[MqttStatusNotifier("VOLUME_STATUS")]
public class VolumeStatusMqttNotifier
{
}

[MqttStatusNotifier("MUTE_STATUS")]
public class MuteStatusMqttNotifier
{
}

[MqttStatusNotifier("TRACK_STATUS")]
public class TrackStatusMqttNotifier
{
}

[MqttStatusNotifier("TRACK_METADATA")]
public class TrackMetadataMqttNotifier
{
}

[MqttStatusNotifier("TRACK_PROGRESS_STATUS")]
public class TrackProgressStatusMqttNotifier
{
}

[MqttStatusNotifier("TRACK_REPEAT_STATUS")]
public class TrackRepeatStatusMqttNotifier
{
}

[MqttStatusNotifier("PLAYLIST_STATUS")]
public class PlaylistStatusMqttNotifier
{
}

[MqttStatusNotifier("PLAYLIST_SHUFFLE_STATUS")]
public class PlaylistShuffleStatusMqttNotifier
{
}

[MqttStatusNotifier("CLIENT_MUTE_STATUS")]
public class ClientMuteStatusMqttNotifier
{
}

[MqttStatusNotifier("CLIENT_LATENCY_STATUS")]
public class ClientLatencyStatusMqttNotifier
{
}

[MqttStatusNotifier("CLIENT_ZONE_STATUS")]
public class ClientZoneStatusMqttNotifier
{
}

[MqttStatusNotifier("CLIENT_VOLUME_STATUS")]
public class ClientVolumeStatusMqttNotifier
{
}

[MqttStatusNotifier("CLIENT_CONNECTED")]
public class ClientConnectedMqttNotifier
{
}

[MqttStatusNotifier("SYSTEM_ERROR")]
public class SystemErrorMqttNotifier
{
}

[MqttStatusNotifier("ZONE_COUNT")]
public class ZoneCountMqttNotifier
{
}

[MqttStatusNotifier("CLIENT_COUNT")]
public class ClientCountMqttNotifier
{
}

[MqttStatusNotifier("VERSION_INFO")]
public class VersionInfoMqttNotifier
{
}

[MqttStatusNotifier("SERVER_STATS")]
public class ServerStatsMqttNotifier
{
}

[MqttStatusNotifier("ZONE_NAME_STATUS")]
public class ZoneNameStatusMqttNotifier
{
}

[MqttStatusNotifier("TRACK_METADATA_DURATION")]
public class TrackMetadataDurationMqttNotifier
{
}

[MqttStatusNotifier("TRACK_METADATA_TITLE")]
public class TrackMetadataTitleMqttNotifier
{
}

[MqttStatusNotifier("TRACK_METADATA_ARTIST")]
public class TrackMetadataArtistMqttNotifier
{
}

[MqttStatusNotifier("TRACK_METADATA_ALBUM")]
public class TrackMetadataAlbumMqttNotifier
{
}

[MqttStatusNotifier("TRACK_METADATA_COVER")]
public class TrackMetadataCoverMqttNotifier
{
}

[MqttStatusNotifier("TRACK_PLAYING_STATUS")]
public class TrackPlayingStatusMqttNotifier
{
}

[MqttStatusNotifier("TRACK_POSITION_STATUS")]
public class TrackPositionStatusMqttNotifier
{
}

[MqttStatusNotifier("PLAYLIST_NAME_STATUS")]
public class PlaylistNameStatusMqttNotifier
{
}

[MqttStatusNotifier("PLAYLIST_COUNT_STATUS")]
public class PlaylistCountStatusMqttNotifier
{
}

[MqttStatusNotifier("PLAYLIST_REPEAT_STATUS")]
public class PlaylistRepeatStatusMqttNotifier
{
}

[MqttStatusNotifier("CLIENT_NAME_STATUS")]
public class ClientNameStatusMqttNotifier
{
}

[MqttStatusNotifier("CLIENT_STATE")]
public class ClientStateMqttNotifier
{
}

[MqttStatusNotifier("CONTROL_STATUS")]
public class ControlStatusMqttNotifier
{
}
