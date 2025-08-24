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

namespace SnapDog2.Hosting;

/// <summary>
/// High-performance LoggerMessage definitions for ResilientHost.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class ResilientHost
{
    // Startup Validation Errors (10001-10002)
    [LoggerMessage(10001, LogLevel.Critical, "🚨 STARTUP VALIDATION FAILED: {ValidationStep}")]
    private static partial void LogStartupValidationFailedDebug(ILogger logger, Exception ex, string validationStep);

    [LoggerMessage(10002, LogLevel.Critical, "🚨 STARTUP VALIDATION FAILED: {ValidationStep} - {ErrorMessage}")]
    private static partial void LogStartupValidationFailedProduction(
        ILogger logger,
        string validationStep,
        string errorMessage
    );

    // Expected Startup Errors (10003-10004)
    [LoggerMessage(10003, LogLevel.Critical, "🚨 STARTUP FAILED: Expected startup error occurred")]
    private static partial void LogStartupFailedDebug(ILogger logger, Exception ex);

    [LoggerMessage(10004, LogLevel.Critical, "🚨 STARTUP FAILED: {ErrorType} - {ErrorMessage}")]
    private static partial void LogStartupFailedProduction(ILogger logger, string errorType, string errorMessage);

    // Unexpected Startup Errors (10005-10006)
    [LoggerMessage(10005, LogLevel.Critical, "🚨 UNEXPECTED STARTUP FAILURE: Unhandled exception during host startup")]
    private static partial void LogUnexpectedStartupFailureDebug(ILogger logger, Exception ex);

    [LoggerMessage(10006, LogLevel.Critical, "🚨 UNEXPECTED STARTUP FAILURE: {ErrorType} - {ErrorMessage}")]
    private static partial void LogUnexpectedStartupFailureProduction(
        ILogger logger,
        string errorType,
        string errorMessage
    );

    // Host Shutdown Error Operations (10501-10502)
    [LoggerMessage(10501, LogLevel.Error, "Error during host shutdown")]
    private partial void LogErrorDuringHostShutdown(Exception ex);

    [LoggerMessage(10502, LogLevel.Error, "Error during host shutdown: {ErrorType} - {ErrorMessage}")]
    private partial void LogErrorDuringHostShutdownProduction(string errorType, string errorMessage);
}
