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
using Microsoft.Extensions.Logging;

namespace SnapDog2.Server.Shared.Handlers;

/// <summary>
/// High-performance LoggerMessage definitions for GlobalStateNotificationHandler.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class GlobalStateNotificationHandler
{
    // Global Event Publishing Operations (11101)
    [LoggerMessage(
        EventId = 4900,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to publish {EventType} to external systems"
    )]
    private partial void LogFailedToPublishGlobalEventToExternalSystems(Exception ex, string eventType);
}
