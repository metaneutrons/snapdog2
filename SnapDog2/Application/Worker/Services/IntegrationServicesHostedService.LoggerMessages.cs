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

namespace SnapDog2.Application.Worker.Services;

/// <summary>
/// High-performance LoggerMessage definitions for IntegrationServicesHostedService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class IntegrationServicesHostedService
{
    // Service Lifecycle Operations (10001-10004)
    [LoggerMessage(
        EventId = 5200,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Integration services initialization cancelled"
    )]
    private partial void LogIntegrationServicesInitializationCancelled();

    [LoggerMessage(
        EventId = 5201,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to initialize integration services"
    )]
    private partial void LogFailedToInitializeIntegrationServices(Exception ex);

    [LoggerMessage(
        EventId = 5202,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Stopping integration services..."
    )]
    private partial void LogStoppingIntegrationServices();

    [LoggerMessage(
        EventId = 5203,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Integration services stopped"
    )]
    private partial void LogIntegrationServicesStopped();
}
