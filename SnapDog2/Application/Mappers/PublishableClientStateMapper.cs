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

namespace SnapDog2.Application.Mappers;

using SnapDog2.Shared.Models;

/// <summary>
/// Maps internal client state models to simplified external formats for MQTT publishing and API responses.
/// Provides a consistent, user-friendly representation across all external integrations.
/// </summary>
public static class PublishableClientStateMapper
{
    /// <summary>
    /// Converts a ClientState to a simplified PublishableClientState for publishing.
    /// </summary>
    /// <param name="clientState">The full client state.</param>
    /// <returns>Simplified client state with only user-facing information.</returns>
    public static PublishableClientState ToPublishableClientState(ClientState clientState)
    {
        return new PublishableClientState
        {
            Name = clientState.Name,
            Volume = clientState.Volume,
            Mute = clientState.Mute,
            Connected = clientState.Connected,
            ZoneIndex = clientState.ZoneIndex,
            LatencyMs = clientState.LatencyMs
        };
    }

    /// <summary>
    /// Compares two PublishableClientState objects to determine if they represent a meaningful change.
    /// </summary>
    /// <param name="previous">The previous state (can be null).</param>
    /// <param name="current">The current state.</param>
    /// <returns>True if the states are different enough to warrant publishing.</returns>
    public static bool HasMeaningfulChange(PublishableClientState? previous, PublishableClientState current)
    {
        if (previous == null)
        {
            return true; // First time publishing
        }

        // Check all client properties for changes
        return previous.Name != current.Name ||
               previous.Volume != current.Volume ||
               previous.Mute != current.Mute ||
               previous.Connected != current.Connected ||
               previous.ZoneIndex != current.ZoneIndex ||
               previous.LatencyMs != current.LatencyMs;
    }
}
