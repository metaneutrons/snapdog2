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
namespace SnapDog2.Shared.Attributes;

using System.Reflection;

/// <summary>
/// Attribute to mark notification classes with their corresponding status ID.
/// Used for outbound status events to external systems (MQTT, KNX).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Method)]
public class StatusIdAttribute(string id) : Attribute
{
    /// <summary>
    /// The status identifier used in external systems.
    /// </summary>
    public string Id { get; } = id ?? throw new ArgumentNullException(nameof(id));

    /// <summary>
    /// Gets the status ID for a notification type.
    /// </summary>
    /// <typeparam name="T">The notification type.</typeparam>
    /// <returns>The status ID string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no StatusId attribute is found.</exception>
    public static string GetStatusId<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<StatusIdAttribute>();
        return attribute?.Id
            ?? throw new InvalidOperationException(
                $"No StatusId attribute found on {typeof(T).Name}. " + $"Add [StatusId(\"STATUS_NAME\")] to the class."
            );
    }

    /// <summary>
    /// Gets the status ID for a notification type, returning null if not found.
    /// </summary>
    /// <typeparam name="T">The notification type.</typeparam>
    /// <returns>The status ID string or null if not found.</returns>
    public static string? TryGetStatusId<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<StatusIdAttribute>();
        return attribute?.Id;
    }
}
