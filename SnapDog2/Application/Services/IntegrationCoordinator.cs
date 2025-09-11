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
namespace SnapDog2.Application.Services;

using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Events;

/// <summary>
/// Coordinates state changes across all integration publishers.
/// </summary>
public class IntegrationCoordinator : IHostedService
{
    private readonly IEnumerable<IIntegrationPublisher> _publishers;
    private readonly ILogger<IntegrationCoordinator> _logger;
    private readonly IZoneStateStore _zoneStateStore;
    private readonly IClientStateStore _clientStateStore;

    public IntegrationCoordinator(
        IZoneStateStore zoneStateStore,
        IClientStateStore clientStateStore,
        IEnumerable<IIntegrationPublisher> publishers,
        ILogger<IntegrationCoordinator> logger)
    {
        _zoneStateStore = zoneStateStore;
        _clientStateStore = clientStateStore;
        _publishers = publishers;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Subscribe to zone state events
        _zoneStateStore.ZonePlaylistChanged += OnZonePlaylistChanged;
        _zoneStateStore.ZoneVolumeChanged += OnZoneVolumeChanged;
        _zoneStateStore.ZoneTrackChanged += OnZoneTrackChanged;
        _zoneStateStore.ZonePlaybackStateChanged += OnZonePlaybackStateChanged;

        // Subscribe to client state events
        _clientStateStore.ClientVolumeChanged += OnClientVolumeChanged;
        _clientStateStore.ClientConnectionChanged += OnClientConnectionChanged;
        _clientStateStore.ClientNameChanged += OnClientNameChanged;
        _clientStateStore.ClientLatencyChanged += OnClientLatencyChanged;

        _logger.LogInformation("IntegrationCoordinator started with {PublisherCount} publishers", _publishers.Count());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Unsubscribe from zone state events
        _zoneStateStore.ZonePlaylistChanged -= OnZonePlaylistChanged;
        _zoneStateStore.ZoneVolumeChanged -= OnZoneVolumeChanged;
        _zoneStateStore.ZoneTrackChanged -= OnZoneTrackChanged;
        _zoneStateStore.ZonePlaybackStateChanged -= OnZonePlaybackStateChanged;

        // Unsubscribe from client state events
        _clientStateStore.ClientVolumeChanged -= OnClientVolumeChanged;
        _clientStateStore.ClientConnectionChanged -= OnClientConnectionChanged;
        _clientStateStore.ClientNameChanged -= OnClientNameChanged;
        _clientStateStore.ClientLatencyChanged -= OnClientLatencyChanged;

        return Task.CompletedTask;
    }

    private async void OnZonePlaylistChanged(object? sender, ZonePlaylistChangedEventArgs e)
    {
        var tasks = _publishers
            .Where(p => p.IsEnabled)
            .Select(p => PublishWithErrorHandling(
                () => p.PublishZonePlaylistChangedAsync(e.ZoneIndex, e.NewPlaylist),
                p.Name,
                "ZonePlaylistChanged"));

        await Task.WhenAll(tasks);
    }

    private async void OnZoneVolumeChanged(object? sender, ZoneVolumeChangedEventArgs e)
    {
        var tasks = _publishers
            .Where(p => p.IsEnabled)
            .Select(p => PublishWithErrorHandling(
                () => p.PublishZoneVolumeChangedAsync(e.ZoneIndex, e.NewVolume),
                p.Name,
                "ZoneVolumeChanged"));

        await Task.WhenAll(tasks);
    }

    private async void OnZoneTrackChanged(object? sender, ZoneTrackChangedEventArgs e)
    {
        var tasks = _publishers
            .Where(p => p.IsEnabled)
            .Select(p => PublishWithErrorHandling(
                () => p.PublishZoneTrackChangedAsync(e.ZoneIndex, e.NewTrack),
                p.Name,
                "ZoneTrackChanged"));

        await Task.WhenAll(tasks);
    }

    private async void OnZonePlaybackStateChanged(object? sender, ZonePlaybackStateChangedEventArgs e)
    {
        var tasks = _publishers
            .Where(p => p.IsEnabled)
            .Select(p => PublishWithErrorHandling(
                () => p.PublishZonePlaybackStateChangedAsync(e.ZoneIndex, e.NewPlaybackState),
                p.Name,
                "ZonePlaybackStateChanged"));

        await Task.WhenAll(tasks);
    }

    private async void OnClientVolumeChanged(object? sender, ClientVolumeChangedEventArgs e)
    {
        var tasks = _publishers
            .Where(p => p.IsEnabled)
            .Select(p => PublishWithErrorHandling(
                () => p.PublishClientVolumeChangedAsync(e.ClientIndex, e.NewVolume),
                p.Name,
                "ClientVolumeChanged"));

        await Task.WhenAll(tasks);
    }

    private async void OnClientConnectionChanged(object? sender, ClientConnectionChangedEventArgs e)
    {
        var tasks = _publishers
            .Where(p => p.IsEnabled)
            .Select(p => PublishWithErrorHandling(
                () => p.PublishClientConnectionChangedAsync(e.ClientIndex, e.NewConnected),
                p.Name,
                "ClientConnectionChanged"));

        await Task.WhenAll(tasks);
    }

    private async void OnClientNameChanged(object? sender, ClientNameChangedEventArgs e)
    {
        var tasks = _publishers
            .Where(p => p.IsEnabled)
            .Select(p => PublishWithErrorHandling(
                () => p.PublishClientNameChangedAsync(e.ClientIndex, e.NewName),
                p.Name,
                "ClientNameChanged"));

        await Task.WhenAll(tasks);
    }

    private async void OnClientLatencyChanged(object? sender, ClientLatencyChangedEventArgs e)
    {
        var tasks = _publishers
            .Where(p => p.IsEnabled)
            .Select(p => PublishWithErrorHandling(
                () => p.PublishClientLatencyChangedAsync(e.ClientIndex, e.NewLatencyMs),
                p.Name,
                "ClientLatencyChanged"));

        await Task.WhenAll(tasks);
    }

    private async Task PublishWithErrorHandling(Func<Task> publishAction, string publisherName, string eventType)
    {
        try
        {
            await publishAction();
            _logger.LogDebug("Successfully published {EventType} to {Publisher}", eventType, publisherName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} to {Publisher}", eventType, publisherName);
        }
    }
}
