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

/// <summary>
/// High-performance LoggerMessage definitions for KnxService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class KnxService
{
    // Status Publishing Operations (10101-10102)
    [LoggerMessage(EventId = 115100, Level = LogLevel.Debug, Message = "KNX global status publishing not implemented for event type {EventType}"
)]
    private partial void LogKnxGlobalStatusPublishingNotImplemented(string eventType);

    [LoggerMessage(EventId = 115101, Level = LogLevel.Debug, Message = "{Message}"
)]
    private partial void LogKnxDebugMessage(string message);

    // Configuration and Error Operations (10103-10104)
    [LoggerMessage(EventId = 115102, Level = LogLevel.Debug, Message = "No KNX group address configured for status {StatusId} on {TargetDescription} - skipping (this is normal)"
)]
    private partial void LogNoKnxGroupAddressConfigured(string statusId, string targetDescription);

    [LoggerMessage(EventId = 115103, Level = LogLevel.Error, Message = "Error sending KNX status {StatusId} → {TargetDescription}"
)]
    private partial void LogErrorSendingKnxStatus(Exception exception, string statusId, string targetDescription);
}
