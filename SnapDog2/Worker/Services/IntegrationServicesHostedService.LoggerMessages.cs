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

namespace SnapDog2.Worker.Services;

/// <summary>
/// High-performance LoggerMessage definitions for IntegrationServicesHostedService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class IntegrationServicesHostedService
{
    // Service Lifecycle Operations (10001-10004)
    [LoggerMessage(10001, LogLevel.Information, "Integration services initialization cancelled")]
    private partial void LogIntegrationServicesInitializationCancelled();

    [LoggerMessage(10002, LogLevel.Error, "Failed to initialize integration services")]
    private partial void LogFailedToInitializeIntegrationServices(Exception ex);

    [LoggerMessage(10003, LogLevel.Information, "Stopping integration services...")]
    private partial void LogStoppingIntegrationServices();

    [LoggerMessage(10004, LogLevel.Information, "Integration services stopped")]
    private partial void LogIntegrationServicesStopped();
}
