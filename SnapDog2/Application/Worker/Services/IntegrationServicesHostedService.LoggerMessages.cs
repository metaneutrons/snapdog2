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

namespace SnapDog2.Application.Worker.Services;

/// <summary>
/// High-performance LoggerMessage definitions for IntegrationServicesHostedService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class IntegrationServicesHostedService
{
    // Service Lifecycle Operations (10001-10004)
    [LoggerMessage(EventId = 114600, Level = LogLevel.Information, Message = "Integration services initialization cancelled"
)]
    private partial void LogIntegrationServicesInitializationCancelled();

    [LoggerMessage(EventId = 114601, Level = LogLevel.Error, Message = "Failed → initialize integration services"
)]
    private partial void LogFailedToInitializeIntegrationServices(Exception ex);

    [LoggerMessage(EventId = 114602, Level = LogLevel.Information, Message = "Stopping integration services..."
)]
    private partial void LogStoppingIntegrationServices();

    [LoggerMessage(EventId = 114603, Level = LogLevel.Information, Message = "Integration services stopped"
)]
    private partial void LogIntegrationServicesStopped();
}
