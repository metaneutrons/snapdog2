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
namespace SnapDog2.Domain.Abstractions;

using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Interface for publishing state changes to integration services.
/// </summary>
public interface IIntegrationPublisher
{
    /// <summary>
    /// Gets the name of the integration publisher.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether the integration publisher is enabled and available.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Publishes zone playlist change.
    /// </summary>
    Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes zone volume change.
    /// </summary>
    Task PublishZoneVolumeChangedAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes zone track change.
    /// </summary>
    Task PublishZoneTrackChangedAsync(int zoneIndex, TrackInfo? track, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes zone playback state change.
    /// </summary>
    Task PublishZonePlaybackStateChangedAsync(int zoneIndex, PlaybackState playbackState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes client volume change.
    /// </summary>
    Task PublishClientVolumeChangedAsync(int clientIndex, int volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes client connection change.
    /// </summary>
    Task PublishClientConnectionChangedAsync(int clientIndex, bool connected, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes client name change.
    /// </summary>
    Task PublishClientNameChangedAsync(int clientIndex, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes client latency change.
    /// </summary>
    Task PublishClientLatencyChangedAsync(int clientIndex, int latencyMs, CancellationToken cancellationToken = default);
}
