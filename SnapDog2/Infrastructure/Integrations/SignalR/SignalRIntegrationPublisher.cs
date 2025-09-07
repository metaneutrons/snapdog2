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
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// SignalR implementation of integration publisher.
/// </summary>
public class SignalRIntegrationPublisher : IIntegrationPublisher
{
    private readonly IHubContext<SnapDogHub>? _hubContext;

    public SignalRIntegrationPublisher(IServiceProvider serviceProvider)
    {
        _hubContext = serviceProvider.GetService<IHubContext<SnapDogHub>>();
    }

    public string Name => "SignalR";

    public bool IsEnabled => _hubContext != null;

    public async Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ZonePlaylistChanged", zoneIndex, playlist, cancellationToken);
    }

    public async Task PublishZoneVolumeChangedAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ZoneVolumeChanged", zoneIndex, volume, cancellationToken);
    }

    public async Task PublishZoneTrackChangedAsync(int zoneIndex, TrackInfo? track, CancellationToken cancellationToken = default)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ZoneTrackChanged", zoneIndex, track, cancellationToken);
    }

    public async Task PublishZonePlaybackStateChangedAsync(int zoneIndex, PlaybackState playbackState, CancellationToken cancellationToken = default)
    {
        if (_hubContext == null)
        {
            return;
        }

        await _hubContext.Clients.All.SendAsync("ZonePlaybackStateChanged", zoneIndex, playbackState, cancellationToken);
    }
}
