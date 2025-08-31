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

using System.Threading.Tasks;
using SnapDog2.Shared.Models;

/// <summary>
/// Represents an individual Snapcast client with control operations and status publishing.
/// </summary>
public interface IClient
{
    /// <summary>
    /// Gets the client Index.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the client name.
    /// </summary>
    string Name { get; }

    #region Command Operations

    /// <summary>
    /// Sets the volume for this client.
    /// </summary>
    /// <param name="volume">The volume level (0-100).</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetVolumeAsync(int volume);

    /// <summary>
    /// Sets the mute state for this client.
    /// </summary>
    /// <param name="mute">True to mute, false to unmute.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetMuteAsync(bool mute);

    /// <summary>
    /// Sets the latency for this client.
    /// </summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetLatencyAsync(int latencyMs);

    /// <summary>
    /// Sets the display name for this client.
    /// </summary>
    /// <param name="name">The new name for the client.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetNameAsync(string name);

    /// <summary>
    /// Increases the volume for this client by the specified step.
    /// </summary>
    /// <param name="step">The volume step to increase (default: 5).</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> VolumeUpAsync(int step = 5);

    /// <summary>
    /// Decreases the volume for this client by the specified step.
    /// </summary>
    /// <param name="step">The volume step to decrease (default: 5).</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> VolumeDownAsync(int step = 5);

    /// <summary>
    /// Toggles the mute state for this client.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ToggleMuteAsync();

    /// <summary>
    /// Assigns this client to the specified zone.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based) to assign the client to.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AssignToZoneAsync(int zoneIndex);

    #endregion

    #region Status Publishing

    /// <summary>
    /// Publishes a volume status notification for this client.
    /// </summary>
    /// <param name="volume">The current volume level (0-100).</param>
    Task PublishVolumeStatusAsync(int volume);

    /// <summary>
    /// Publishes a mute status notification for this client.
    /// </summary>
    /// <param name="muted">The current mute state.</param>
    Task PublishMuteStatusAsync(bool muted);

    /// <summary>
    /// Publishes a latency status notification for this client.
    /// </summary>
    /// <param name="latencyMs">The current latency in milliseconds.</param>
    Task PublishLatencyStatusAsync(int latencyMs);

    /// <summary>
    /// Publishes a zone assignment status notification for this client.
    /// </summary>
    /// <param name="zoneIndex">The current zone index (1-based), or null if unassigned.</param>
    Task PublishZoneStatusAsync(int? zoneIndex);

    /// <summary>
    /// Publishes a connection status notification for this client.
    /// </summary>
    /// <param name="isConnected">The current connection state.</param>
    Task PublishConnectionStatusAsync(bool isConnected);

    /// <summary>
    /// Publishes a complete state notification for this client.
    /// </summary>
    /// <param name="state">The current client state.</param>
    Task PublishStateAsync(ClientState state);

    #endregion
}
