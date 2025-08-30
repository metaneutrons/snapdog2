using SnapDog2.WebUi.ApiClient.Generated;

namespace SnapDog2.WebUi.ApiClient.Models;

/// <summary>
/// WebUI-friendly zone state that adapts the generated ZoneState
/// </summary>
public class WebUiZoneState
{
    private readonly ZoneState _zoneState;
    private readonly ICollection<ClientState> _clientStates;

    public WebUiZoneState(ZoneState zoneState, int index, ICollection<ClientState> clientStates)
    {
        _zoneState = zoneState;
        Index = index;
        _clientStates = clientStates;
    }

    public int Index { get; }
    public string Name => _zoneState.Name;
    public int Volume => _zoneState.Volume;
    public bool Mute => _zoneState.Mute;
    public PlaybackState PlaybackState => _zoneState.PlaybackState;
    public bool TrackRepeat => _zoneState.TrackRepeat;
    public bool PlaylistRepeat => _zoneState.PlaylistRepeat;

    public TrackInfo? CurrentTrack => _zoneState.Track;
    public bool IsPlaying => _zoneState.PlaybackState == PlaybackState._1; // Assuming 1 = Playing
    public bool IsMuted => _zoneState.Mute;

    public IEnumerable<WebUiClientState> Clients =>
        _clientStates.Where(c => _zoneState.Clients?.Contains(c.Id) == true)
                    .Select(c => new WebUiClientState(c));
}

/// <summary>
/// WebUI-friendly client state that adapts the generated ClientState
/// </summary>
public class WebUiClientState
{
    private readonly ClientState _clientState;

    public WebUiClientState(ClientState clientState)
    {
        _clientState = clientState;
    }

    public int Index => _clientState.Id;
    public string Name => _clientState.Name;
    public bool IsConnected => _clientState.Connected;
    public int Volume => _clientState.Volume;
    public bool Mute => _clientState.Mute;
    public string Mac => _clientState.Mac;
    public int ZoneIndex => _clientState.ZoneIndex;
}
