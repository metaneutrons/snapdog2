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
namespace SnapDog2.Shared.Enums;

using System.ComponentModel;

/// <summary>
/// Enumeration of all command event types in the system.
/// Maps to CommandId strings through Description attributes.
/// Enables compile-time safe switching and type-safe command processing.
/// </summary>
public enum CommandEventType
{
    // Zone Playback Commands
    [Description("PLAY")]
    Play,

    [Description("PAUSE")]
    Pause,

    [Description("STOP")]
    Stop,

    // Zone Volume Commands
    [Description("VOLUME")]
    Volume,

    [Description("VOLUME_UP")]
    VolumeUp,

    [Description("VOLUME_DOWN")]
    VolumeDown,

    [Description("MUTE")]
    Mute,

    [Description("MUTE_TOGGLE")]
    MuteToggle,

    // Zone Track Commands
    [Description("TRACK")]
    Track,

    [Description("TRACK_NEXT")]
    TrackNext,

    [Description("TRACK_PREVIOUS")]
    TrackPrevious,

    [Description("TRACK_PLAY_INDEX")]
    TrackPlayIndex,

    [Description("TRACK_PLAY_URL")]
    TrackPlayUrl,

    [Description("TRACK_POSITION")]
    TrackPosition,

    [Description("TRACK_PROGRESS")]
    TrackProgress,

    [Description("TRACK_REPEAT")]
    TrackRepeat,

    [Description("TRACK_REPEAT_TOGGLE")]
    TrackRepeatToggle,

    // Zone Playlist Commands
    [Description("PLAYLIST")]
    Playlist,

    [Description("PLAYLIST_NEXT")]
    PlaylistNext,

    [Description("PLAYLIST_PREVIOUS")]
    PlaylistPrevious,

    [Description("PLAYLIST_REPEAT")]
    PlaylistRepeat,

    [Description("PLAYLIST_REPEAT_TOGGLE")]
    PlaylistRepeatToggle,

    [Description("PLAYLIST_SHUFFLE")]
    PlaylistShuffle,

    [Description("PLAYLIST_SHUFFLE_TOGGLE")]
    PlaylistShuffleToggle,

    // Client Volume Commands
    [Description("CLIENT_VOLUME")]
    ClientVolume,

    [Description("CLIENT_VOLUME_UP")]
    ClientVolumeUp,

    [Description("CLIENT_VOLUME_DOWN")]
    ClientVolumeDown,

    [Description("CLIENT_MUTE")]
    ClientMute,

    [Description("CLIENT_MUTE_TOGGLE")]
    ClientMuteToggle,

    // Client Configuration Commands
    [Description("CLIENT_NAME")]
    ClientName,

    [Description("CLIENT_LATENCY")]
    ClientLatency,

    [Description("CLIENT_ZONE")]
    ClientZone,
}

/// <summary>
/// Extension methods for CommandEventType enum.
/// Provides conversion between enum values and command ID strings.
/// </summary>
public static class CommandEventTypeExtensions
{
    /// <summary>
    /// Converts a CommandEventType enum value to its corresponding command ID string.
    /// </summary>
    /// <param name="eventType">The command event type.</param>
    /// <returns>The command ID string.</returns>
    public static string ToCommandString(this CommandEventType eventType)
    {
        var field = eventType.GetType().GetField(eventType.ToString());
        var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field!, typeof(DescriptionAttribute));
        return attribute?.Description ?? eventType.ToString();
    }

    /// <summary>
    /// Converts a command ID string to its corresponding CommandEventType enum value.
    /// </summary>
    /// <param name="commandString">The command ID string.</param>
    /// <returns>The CommandEventType enum value, or null if no match is found.</returns>
    public static CommandEventType? FromCommandString(string commandString)
    {
        foreach (var eventType in Enum.GetValues<CommandEventType>())
        {
            if (eventType.ToCommandString().Equals(commandString, StringComparison.OrdinalIgnoreCase))
            {
                return eventType;
            }
        }
        return null;
    }
}
