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
namespace SnapDog2.Shared.Enums;

/// <summary>
/// Application environment types.
/// </summary>
public enum ApplicationEnvironment
{
    /// <summary>
    /// Development environment with reduced resource usage and debug features.
    /// </summary>
    Development,

    /// <summary>
    /// Staging environment for testing production-like configurations.
    /// </summary>
    Staging,

    /// <summary>
    /// Production environment with full optimization and monitoring.
    /// </summary>
    Production
}
