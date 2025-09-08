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
namespace SnapDog2.Shared.Constants;

/// <summary>
/// Strongly-typed constants for CommandId values.
/// These constants preserve blueprint validation while eliminating
/// dependencies on command classes after mediator removal.
/// </summary>
public static class CommandIds
{
    // Zone Playback Commands
    public const string Play = "PLAY";
    public const string Pause = "PAUSE";
    public const string Stop = "STOP";

    // Zone Volume Commands
    public const string Volume = "VOLUME";
    public const string VolumeUp = "VOLUME_UP";
    public const string VolumeDown = "VOLUME_DOWN";
    public const string Mute = "MUTE";
    public const string MuteToggle = "MUTE_TOGGLE";

    // Zone Track Commands
    public const string Track = "TRACK";
    public const string TrackNext = "TRACK_NEXT";
    public const string TrackPrevious = "TRACK_PREVIOUS";
    public const string TrackRepeat = "TRACK_REPEAT";
    public const string TrackRepeatToggle = "TRACK_REPEAT_TOGGLE";
    public const string TrackSeekPosition = "TRACK_POSITION";
    public const string TrackSeekProgress = "TRACK_PROGRESS";
    public const string TrackPlayByIndex = "TRACK_PLAY_INDEX";
    public const string TrackPlayFromPlaylist = "TRACK_PLAY_PLAYLIST";
    public const string TrackPlayUrl = "TRACK_PLAY_URL";

    // Zone Playlist Commands
    public const string Playlist = "PLAYLIST";
    public const string PlaylistNext = "PLAYLIST_NEXT";
    public const string PlaylistPrevious = "PLAYLIST_PREVIOUS";
    public const string PlaylistRepeat = "PLAYLIST_REPEAT";
    public const string PlaylistRepeatToggle = "PLAYLIST_REPEAT_TOGGLE";
    public const string PlaylistShuffle = "PLAYLIST_SHUFFLE";
    public const string PlaylistShuffleToggle = "PLAYLIST_SHUFFLE_TOGGLE";

    // Client Volume Commands
    public const string ClientVolume = "CLIENT_VOLUME";
    public const string ClientVolumeUp = "CLIENT_VOLUME_UP";
    public const string ClientVolumeDown = "CLIENT_VOLUME_DOWN";
    public const string ClientMute = "CLIENT_MUTE";
    public const string ClientMuteToggle = "CLIENT_MUTE_TOGGLE";

    // Client Configuration Commands
    public const string ClientLatency = "CLIENT_LATENCY";
    public const string ClientZone = "CLIENT_ZONE";
    public const string ClientName = "CLIENT_NAME";

    // Zone Configuration Commands
    public const string ZoneName = "ZONE_NAME";

    // Zone Control Commands
    public const string Control = "CONTROL";
}
