namespace SnapDog2.WebUi.ApiClient;

public class ZoneState
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPlaying { get; set; }
    public bool IsMuted { get; set; }
    public int Volume { get; set; }
    public TrackInfo? CurrentTrack { get; set; }
    public List<ClientState> Clients { get; set; } = new();
}

public class ClientState
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public int ZoneIndex { get; set; }
}

public class TrackInfo
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}
