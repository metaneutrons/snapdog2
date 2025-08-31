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
namespace SnapDog2.Shared.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// KNX configuration for a zone.
/// </summary>
public class ZoneKnxConfig
{
    /// <summary>
    /// Whether KNX is enabled for this zone.
    /// Maps to: SNAPDOG_ZONE_X_KNX_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    // Control addresses
    [Env(Key = "PLAY")]
    public string? Play { get; set; }

    [Env(Key = "PAUSE")]
    public string? Pause { get; set; }

    [Env(Key = "STOP")]
    public string? Stop { get; set; }

    // Removed CONTROL_STATUS - replaced with more granular TRACK_PLAYING_STATUS
    // [Env(Key = "CONTROL_STATUS")]
    // public string? ControlStatus { get; set; }

    // Track addresses
    [Env(Key = "TRACK_NEXT")]
    public string? TrackNext { get; set; }

    [Env(Key = "TRACK_PREVIOUS")]
    public string? TrackPrevious { get; set; }

    [Env(Key = "TRACK")]
    public string? Track { get; set; }

    [Env(Key = "TRACK_STATUS")]
    public string? TrackStatus { get; set; }

    [Env(Key = "TRACK_REPEAT")]
    public string? TrackRepeat { get; set; }

    [Env(Key = "TRACK_REPEAT_STATUS")]
    public string? TrackRepeatStatus { get; set; }

    [Env(Key = "TRACK_REPEAT_TOGGLE")]
    public string? TrackRepeatToggle { get; set; }

    // Playlist addresses
    [Env(Key = "PLAYLIST")]
    public string? Playlist { get; set; }

    [Env(Key = "PLAYLIST_STATUS")]
    public string? PlaylistStatus { get; set; }

    [Env(Key = "PLAYLIST_NEXT")]
    public string? PlaylistNext { get; set; }

    [Env(Key = "PLAYLIST_PREVIOUS")]
    public string? PlaylistPrevious { get; set; }

    [Env(Key = "SHUFFLE")]
    public string? Shuffle { get; set; }

    [Env(Key = "SHUFFLE_TOGGLE")]
    public string? ShuffleToggle { get; set; }

    [Env(Key = "SHUFFLE_STATUS")]
    public string? ShuffleStatus { get; set; }

    [Env(Key = "REPEAT")]
    public string? Repeat { get; set; }

    [Env(Key = "REPEAT_TOGGLE")]
    public string? RepeatToggle { get; set; }

    [Env(Key = "REPEAT_STATUS")]
    public string? RepeatStatus { get; set; }

    // Volume addresses
    [Env(Key = "VOLUME")]
    public string? Volume { get; set; }

    [Env(Key = "VOLUME_UP")]
    public string? VolumeUp { get; set; }

    [Env(Key = "VOLUME_DOWN")]
    public string? VolumeDown { get; set; }

    [Env(Key = "VOLUME_STATUS")]
    public string? VolumeStatus { get; set; }

    [Env(Key = "MUTE")]
    public string? Mute { get; set; }

    [Env(Key = "MUTE_TOGGLE")]
    public string? MuteToggle { get; set; }

    [Env(Key = "MUTE_STATUS")]
    public string? MuteStatus { get; set; }

    // Track metadata status addresses (KNX DPT 16.001 - 14-byte strings)
    [Env(Key = "TRACK_TITLE_STATUS")]
    public string? TrackTitleStatus { get; set; }

    [Env(Key = "TRACK_ARTIST_STATUS")]
    public string? TrackArtistStatus { get; set; }

    [Env(Key = "TRACK_ALBUM_STATUS")]
    public string? TrackAlbumStatus { get; set; }

    // Track playback status addresses
    [Env(Key = "TRACK_PROGRESS_STATUS")]
    public string? TrackProgressStatus { get; set; }

    [Env(Key = "TRACK_PLAYING_STATUS")]
    public string? TrackPlayingStatus { get; set; }
}
