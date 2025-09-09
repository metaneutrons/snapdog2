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
/// Attribute to mark API endpoints with their blueprint status ID.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class StatusIdAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusIdAttribute"/> class.
    /// </summary>
    /// <param name="id">The status ID from the blueprint.</param>
    public StatusIdAttribute(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the status ID.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the status ID for a type using reflection.
    /// </summary>
    /// <typeparam name="T">The type to get the status ID for.</typeparam>
    /// <returns>The status ID string.</returns>
    public static string GetStatusId<T>()
    {
        var type = typeof(T);
        var attribute = type.GetCustomAttribute<StatusIdAttribute>();
        return attribute?.Id ?? string.Empty;
    }
}
