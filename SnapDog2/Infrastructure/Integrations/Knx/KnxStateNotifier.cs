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
namespace SnapDog2.Infrastructure.Integrations.Knx;

using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Events;

/// <summary>
/// Direct KNX notifier connected to state stores.
/// </summary>
public partial class KnxStateNotifier : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IZoneStateStore _zoneStateStore;
    private readonly ILogger<KnxStateNotifier> _logger;
    private readonly SnapDogConfiguration _configuration;

    public KnxStateNotifier(
        IServiceProvider serviceProvider,
        IZoneStateStore zoneStateStore,
        ILogger<KnxStateNotifier> logger,
        IOptions<SnapDogConfiguration> configuration)
    {
        _serviceProvider = serviceProvider;
        _zoneStateStore = zoneStateStore;
        _logger = logger;
        _configuration = configuration.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.Services.Knx.Enabled)
        {
            LogKnxNotAvailable();
            return Task.CompletedTask;
        }

        _zoneStateStore.ZoneVolumeChanged += OnZoneVolumeChanged;
        _zoneStateStore.ZonePlaybackStateChanged += OnZonePlaybackStateChanged;
        _zoneStateStore.ZonePlaylistChanged += OnZonePlaylistChanged;
        _zoneStateStore.ZoneTrackChanged += OnZoneTrackChanged;
        _zoneStateStore.ZonePositionChanged += OnZonePositionChanged;
        _zoneStateStore.ZoneStateChanged += OnZoneStateChanged;

        LogKnxNotifierStarted();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.Services.Knx.Enabled)
        {
            return Task.CompletedTask;
        }

        _zoneStateStore.ZoneVolumeChanged -= OnZoneVolumeChanged;
        _zoneStateStore.ZonePlaybackStateChanged -= OnZonePlaybackStateChanged;
        _zoneStateStore.ZonePlaylistChanged -= OnZonePlaylistChanged;
        _zoneStateStore.ZoneTrackChanged -= OnZoneTrackChanged;
        _zoneStateStore.ZonePositionChanged -= OnZonePositionChanged;
        _zoneStateStore.ZoneStateChanged -= OnZoneStateChanged;

        return Task.CompletedTask;
    }

    private async void OnZoneVolumeChanged(object? sender, ZoneVolumeChangedEventArgs e)
    {
        var zoneIndex = e.ZoneIndex - 1; // Convert to 0-based
        if (zoneIndex < 0 || zoneIndex >= _configuration.Zones.Count)
        {
            return;
        }

        var knxConfig = _configuration.Zones[zoneIndex].Knx;
        if (!knxConfig.Enabled || string.IsNullOrEmpty(knxConfig.VolumeStatus))
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetService<IKnxService>();
        if (knxService != null)
        {
            await knxService.WriteGroupValueAsync(knxConfig.VolumeStatus, e.NewVolume);
        }
    }

    private async void OnZonePlaybackStateChanged(object? sender, ZonePlaybackStateChangedEventArgs e)
    {
        var zoneIndex = e.ZoneIndex - 1; // Convert to 0-based
        if (zoneIndex < 0 || zoneIndex >= _configuration.Zones.Count)
        {
            return;
        }

        var knxConfig = _configuration.Zones[zoneIndex].Knx;
        if (!knxConfig.Enabled || string.IsNullOrEmpty(knxConfig.TrackPlayingStatus))
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetService<IKnxService>();
        if (knxService != null)
        {
            var isPlaying = e.NewPlaybackState == PlaybackState.Playing ? 1 : 0;
            await knxService.WriteGroupValueAsync(knxConfig.TrackPlayingStatus, isPlaying);
        }
    }

    private async void OnZonePlaylistChanged(object? sender, ZonePlaylistChangedEventArgs e)
    {
        var zoneIndex = e.ZoneIndex - 1; // Convert to 0-based
        if (zoneIndex < 0 || zoneIndex >= _configuration.Zones.Count)
        {
            return;
        }

        var knxConfig = _configuration.Zones[zoneIndex].Knx;
        if (!knxConfig.Enabled || string.IsNullOrEmpty(knxConfig.PlaylistStatus))
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetService<IKnxService>();
        if (knxService != null)
        {
            await knxService.WriteGroupValueAsync(knxConfig.PlaylistStatus, e.NewPlaylist?.Name ?? "");
        }
    }

    private async void OnZoneTrackChanged(object? sender, ZoneTrackChangedEventArgs e)
    {
        var zoneIndex = e.ZoneIndex - 1; // Convert to 0-based
        if (zoneIndex < 0 || zoneIndex >= _configuration.Zones.Count)
        {
            return;
        }

        var knxConfig = _configuration.Zones[zoneIndex].Knx;
        if (!knxConfig.Enabled || e.NewTrack == null)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetService<IKnxService>();
        if (knxService == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(knxConfig.TrackTitleStatus))
        {
            await knxService.WriteGroupValueAsync(knxConfig.TrackTitleStatus, e.NewTrack.Title ?? "");
        }

        if (!string.IsNullOrEmpty(knxConfig.TrackArtistStatus))
        {
            await knxService.WriteGroupValueAsync(knxConfig.TrackArtistStatus, e.NewTrack.Artist ?? "");
        }

        if (!string.IsNullOrEmpty(knxConfig.TrackAlbumStatus))
        {
            await knxService.WriteGroupValueAsync(knxConfig.TrackAlbumStatus, e.NewTrack.Album ?? "");
        }

        // Send track index/number
        if (!string.IsNullOrEmpty(knxConfig.TrackStatus))
        {
            await knxService.WriteGroupValueAsync(knxConfig.TrackStatus, e.NewTrack.Index ?? 0);
        }
    }

    private async void OnZonePositionChanged(object? sender, ZonePositionChangedEventArgs e)
    {
        var zoneIndex = e.ZoneIndex - 1;
        if (zoneIndex < 0 || zoneIndex >= _configuration.Zones.Count)
        {
            return;
        }

        var knxConfig = _configuration.Zones[zoneIndex].Knx;
        if (!knxConfig.Enabled || e.Track == null)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetService<IKnxService>();
        if (knxService == null)
        {
            return;
        }

        // Send track progress (0-100%)
        if (!string.IsNullOrEmpty(knxConfig.TrackProgressStatus) && e.Track.Progress.HasValue)
        {
            var progressPercent = (int)(e.Track.Progress.Value * 100);
            await knxService.WriteGroupValueAsync(knxConfig.TrackProgressStatus, progressPercent);
        }
    }

    private async void OnZoneStateChanged(object? sender, ZoneStateChangedEventArgs e)
    {
        var zoneIndex = e.ZoneIndex - 1;
        if (zoneIndex < 0 || zoneIndex >= _configuration.Zones.Count)
        {
            return;
        }

        var knxConfig = _configuration.Zones[zoneIndex].Knx;
        if (!knxConfig.Enabled || e.NewState == null)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var knxService = scope.ServiceProvider.GetService<IKnxService>();
        if (knxService == null)
        {
            return;
        }

        // Send mute status
        if (!string.IsNullOrEmpty(knxConfig.MuteStatus))
        {
            await knxService.WriteGroupValueAsync(knxConfig.MuteStatus, e.NewState.Mute ? 1 : 0);
        }

        // Send shuffle status
        if (!string.IsNullOrEmpty(knxConfig.ShuffleStatus))
        {
            await knxService.WriteGroupValueAsync(knxConfig.ShuffleStatus, e.NewState.PlaylistShuffle ? 1 : 0);
        }

        // Send repeat status
        if (!string.IsNullOrEmpty(knxConfig.RepeatStatus))
        {
            await knxService.WriteGroupValueAsync(knxConfig.RepeatStatus, e.NewState.PlaylistRepeat ? 1 : 0);
        }

        // Send track repeat status
        if (!string.IsNullOrEmpty(knxConfig.TrackRepeatStatus))
        {
            await knxService.WriteGroupValueAsync(knxConfig.TrackRepeatStatus, e.NewState.TrackRepeat ? 1 : 0);
        }
    }

    [LoggerMessage(EventId = 15182, Level = LogLevel.Information, Message = "KNX state notifier started")]
    private partial void LogKnxNotifierStarted();

    [LoggerMessage(EventId = 15183, Level = LogLevel.Warning, Message = "KNX not enabled, state notifications disabled")]
    private partial void LogKnxNotAvailable();
}
