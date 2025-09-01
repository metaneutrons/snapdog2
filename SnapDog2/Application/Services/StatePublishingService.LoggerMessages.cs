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

/// <summary>
/// High-performance LoggerMessage definitions for StatePublishingService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class StatePublishingService
{
    // State Publishing Operations (10701)
    [LoggerMessage(
        EventId = 6200,
        Level = LogLevel.Information,
        Message = "State publishing cancelled during shutdown"
    )]
    private partial void LogStatePublishingCancelledDuringShutdown();
}
