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
namespace SnapDog2.Infrastructure.Integrations.SignalR;

using Microsoft.AspNetCore.SignalR;
using SnapDog2.Api.Hubs;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Events;

/// <summary>
/// Direct SignalR notifier connected to state stores.
/// </summary>
public partial class SignalRStateNotifier : IHostedService
{
    private readonly IHubContext<SnapDogHub>? _hubContext;
    private readonly IZoneStateStore _zoneStateStore;
    private readonly IClientStateStore _clientStateStore;
    private readonly ILogger<SignalRStateNotifier> _logger;

    public SignalRStateNotifier(
        IServiceProvider serviceProvider,
        IZoneStateStore zoneStateStore,
        IClientStateStore clientStateStore,
        ILogger<SignalRStateNotifier> logger)
    {
        _hubContext = serviceProvider.GetService<IHubContext<SnapDogHub>>();
        _zoneStateStore = zoneStateStore;
        _clientStateStore = clientStateStore;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_hubContext == null)
        {
            LogSignalRNotAvailable();
            return Task.CompletedTask;
        }

        // Subscribe directly to state store events
        _zoneStateStore.ZonePlaylistChanged += OnZonePlaylistChanged;
        _zoneStateStore.ZoneVolumeChanged += OnZoneVolumeChanged;
        _zoneStateStore.ZoneTrackChanged += OnZoneTrackChanged;
        _zoneStateStore.ZonePlaybackStateChanged += OnZonePlaybackStateChanged;

        _clientStateStore.ClientVolumeChanged += OnClientVolumeChanged;
        _clientStateStore.ClientConnectionChanged += OnClientConnectionChanged;
        _clientStateStore.ClientNameChanged += OnClientNameChanged;
        _clientStateStore.ClientLatencyChanged += OnClientLatencyChanged;

        LogSignalRNotifierStarted();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hubContext == null)
        {
            return Task.CompletedTask;
        }

        _zoneStateStore.ZonePlaylistChanged -= OnZonePlaylistChanged;
        _zoneStateStore.ZoneVolumeChanged -= OnZoneVolumeChanged;
        _zoneStateStore.ZoneTrackChanged -= OnZoneTrackChanged;
        _zoneStateStore.ZonePlaybackStateChanged -= OnZonePlaybackStateChanged;

        _clientStateStore.ClientVolumeChanged -= OnClientVolumeChanged;
        _clientStateStore.ClientConnectionChanged -= OnClientConnectionChanged;
        _clientStateStore.ClientNameChanged -= OnClientNameChanged;
        _clientStateStore.ClientLatencyChanged -= OnClientLatencyChanged;

        return Task.CompletedTask;
    }

    private async void OnZonePlaylistChanged(object? sender, ZonePlaylistChangedEventArgs e)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ZonePlaylistChanged", e.ZoneIndex, e.NewPlaylist);
    }

    private async void OnZoneVolumeChanged(object? sender, ZoneVolumeChangedEventArgs e)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ZoneVolumeChanged", e.ZoneIndex, e.NewVolume);
    }

    private async void OnZoneTrackChanged(object? sender, ZoneTrackChangedEventArgs e)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ZoneTrackChanged", e.ZoneIndex, e.NewTrack);
    }

    private async void OnZonePlaybackStateChanged(object? sender, ZonePlaybackStateChangedEventArgs e)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ZonePlaybackStateChanged", e.ZoneIndex, e.NewPlaybackState);
    }

    private async void OnClientVolumeChanged(object? sender, ClientVolumeChangedEventArgs e)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ClientVolumeChanged", e.ClientIndex, e.NewVolume);
    }

    private async void OnClientConnectionChanged(object? sender, ClientConnectionChangedEventArgs e)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ClientConnectionChanged", e.ClientIndex, e.NewConnected);
    }

    private async void OnClientNameChanged(object? sender, ClientNameChangedEventArgs e)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ClientNameChanged", e.ClientIndex, e.NewName);
    }

    private async void OnClientLatencyChanged(object? sender, ClientLatencyChangedEventArgs e)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ClientLatencyChanged", e.ClientIndex, e.NewLatencyMs);
    }

    [LoggerMessage(EventId = 15000, Level = LogLevel.Information, Message = "SignalR state notifier started")]
    private partial void LogSignalRNotifierStarted();

    [LoggerMessage(EventId = 15001, Level = LogLevel.Warning, Message = "SignalR not available, state notifications disabled")]
    private partial void LogSignalRNotAvailable();
}
