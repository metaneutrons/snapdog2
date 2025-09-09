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

[KnxStatusNotifier("VOLUME_STATUS")]
public class VolumeStatusKnxNotifier
{
}

[KnxStatusNotifier("MUTE_STATUS")]
public class MuteStatusKnxNotifier
{
}

[KnxStatusNotifier("TRACK_STATUS")]
public class TrackStatusKnxNotifier
{
}

[KnxStatusNotifier("TRACK_PROGRESS_STATUS")]
public class TrackProgressStatusKnxNotifier
{
}

[KnxStatusNotifier("TRACK_REPEAT_STATUS")]
public class TrackRepeatStatusKnxNotifier
{
}

[KnxStatusNotifier("PLAYLIST_STATUS")]
public class PlaylistStatusKnxNotifier
{
}

[KnxStatusNotifier("PLAYLIST_SHUFFLE_STATUS")]
public class PlaylistShuffleStatusKnxNotifier
{
}

[KnxStatusNotifier("CLIENT_MUTE_STATUS")]
public class ClientMuteStatusKnxNotifier
{
}

[KnxStatusNotifier("CLIENT_LATENCY_STATUS")]
public class ClientLatencyStatusKnxNotifier
{
}

[KnxStatusNotifier("CLIENT_ZONE_STATUS")]
public class ClientZoneStatusKnxNotifier
{
}

[KnxStatusNotifier("CLIENT_VOLUME_STATUS")]
public class ClientVolumeStatusKnxNotifier
{
}

[KnxStatusNotifier("CLIENT_CONNECTED")]
public class ClientConnectedKnxNotifier
{
}

[KnxStatusNotifier("SYSTEM_ERROR")]
public class SystemErrorKnxNotifier
{
}

[KnxStatusNotifier("ZONE_COUNT")]
public class ZoneCountKnxNotifier
{
}

[KnxStatusNotifier("CLIENT_COUNT")]
public class ClientCountKnxNotifier
{
}

[KnxStatusNotifier("VERSION_INFO")]
public class VersionInfoKnxNotifier
{
}

[KnxStatusNotifier("SERVER_STATS")]
public class ServerStatsKnxNotifier
{
}

[KnxStatusNotifier("ZONE_NAME_STATUS")]
public class ZoneNameStatusKnxNotifier
{
}

[KnxStatusNotifier("TRACK_METADATA_TITLE")]
public class TrackMetadataTitleKnxNotifier
{
}

[KnxStatusNotifier("TRACK_METADATA_ARTIST")]
public class TrackMetadataArtistKnxNotifier
{
}

[KnxStatusNotifier("TRACK_METADATA_ALBUM")]
public class TrackMetadataAlbumKnxNotifier
{
}

[KnxStatusNotifier("TRACK_PLAYING_STATUS")]
public class TrackPlayingStatusKnxNotifier
{
}

[KnxStatusNotifier("PLAYLIST_INFO")]
public class PlaylistInfoKnxNotifier
{
}

[KnxStatusNotifier("PLAYLIST_NAME_STATUS")]
public class PlaylistNameStatusKnxNotifier
{
}

[KnxStatusNotifier("PLAYLIST_COUNT_STATUS")]
public class PlaylistCountStatusKnxNotifier
{
}

[KnxStatusNotifier("PLAYLIST_REPEAT_STATUS")]
public class PlaylistRepeatStatusKnxNotifier
{
}

[KnxStatusNotifier("CLIENT_NAME_STATUS")]
public class ClientNameStatusKnxNotifier
{
}

[KnxStatusNotifier("CLIENT_STATE")]
public class ClientStateKnxNotifier
{
}

[KnxStatusNotifier("CONTROL_STATUS")]
public class ControlStatusKnxNotifier
{
}

[KnxStatusNotifier("COMMAND_ERROR")]
public class CommandErrorKnxNotifier
{
}
