namespace SnapDog2.Core.Constants;

using SnapDog2.Core.Attributes;
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Playlist;
using SnapDog2.Server.Features.Zones.Commands.Track;
using SnapDog2.Server.Features.Zones.Commands.Volume;

/// <summary>
/// Strongly-typed constants for CommandId values.
/// These are derived from the CommandIdAttribute on command classes,
/// ensuring compile-time safety and eliminating hardcoded strings.
/// </summary>
public static class CommandIds
{
    // Zone Playback Commands
    public static readonly string Play = CommandIdAttribute.GetCommandId<PlayCommand>();
    public static readonly string Pause = CommandIdAttribute.GetCommandId<PauseCommand>();
    public static readonly string Stop = CommandIdAttribute.GetCommandId<StopCommand>();

    // Zone Volume Commands
    public static readonly string Volume = CommandIdAttribute.GetCommandId<SetZoneVolumeCommand>();
    public static readonly string VolumeUp = CommandIdAttribute.GetCommandId<VolumeUpCommand>();
    public static readonly string VolumeDown = CommandIdAttribute.GetCommandId<VolumeDownCommand>();
    public static readonly string Mute = CommandIdAttribute.GetCommandId<SetZoneMuteCommand>();
    public static readonly string MuteToggle = CommandIdAttribute.GetCommandId<ToggleZoneMuteCommand>();

    // Zone Track Commands
    public static readonly string Track = CommandIdAttribute.GetCommandId<SetTrackCommand>();
    public static readonly string TrackNext = CommandIdAttribute.GetCommandId<NextTrackCommand>();
    public static readonly string TrackPrevious = CommandIdAttribute.GetCommandId<PreviousTrackCommand>();
    public static readonly string TrackRepeat = CommandIdAttribute.GetCommandId<SetTrackRepeatCommand>();
    public static readonly string TrackRepeatToggle = CommandIdAttribute.GetCommandId<ToggleTrackRepeatCommand>();

    // Zone Playlist Commands
    public static readonly string Playlist = CommandIdAttribute.GetCommandId<SetPlaylistCommand>();
    public static readonly string PlaylistNext = CommandIdAttribute.GetCommandId<NextPlaylistCommand>();
    public static readonly string PlaylistPrevious = CommandIdAttribute.GetCommandId<PreviousPlaylistCommand>();
    public static readonly string PlaylistRepeat = CommandIdAttribute.GetCommandId<SetPlaylistRepeatCommand>();
    public static readonly string PlaylistRepeatToggle = CommandIdAttribute.GetCommandId<TogglePlaylistRepeatCommand>();
    public static readonly string PlaylistShuffle = CommandIdAttribute.GetCommandId<SetPlaylistShuffleCommand>();
    public static readonly string PlaylistShuffleToggle =
        CommandIdAttribute.GetCommandId<TogglePlaylistShuffleCommand>();

    // Client Volume Commands
    public static readonly string ClientVolume = CommandIdAttribute.GetCommandId<SetClientVolumeCommand>();
    public static readonly string ClientMute = CommandIdAttribute.GetCommandId<SetClientMuteCommand>();
    public static readonly string ClientMuteToggle = CommandIdAttribute.GetCommandId<ToggleClientMuteCommand>();

    // Client Configuration Commands
    public static readonly string ClientLatency = CommandIdAttribute.GetCommandId<SetClientLatencyCommand>();
    public static readonly string ClientZone = CommandIdAttribute.GetCommandId<AssignClientToZoneCommand>();
}
