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
using Serilog.Core;
using Serilog.Events;

namespace SnapDog2.Infrastructure.Logging;

/// <summary>
/// Serilog enricher that suppresses stack traces for expected exceptions in non-debug mode
/// </summary>
public class StackTraceSuppressionEnricher(bool isDebugMode) : ILogEventEnricher
{
    private readonly bool _isDebugMode = isDebugMode;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Only suppress stack traces in non-debug mode
        if (this._isDebugMode || logEvent.Exception == null)
        {
            return;
        }

        // Check if this is an expected exception type
        if (IsExpectedException(logEvent.Exception))
        {
            // Create a new exception without stack trace
            var cleanException = new Exception(GetCleanExceptionMessage(logEvent.Exception));

            // Replace the exception property
            var exceptionProperty = propertyFactory.CreateProperty("Exception", cleanException);
            logEvent.AddPropertyIfAbsent(exceptionProperty);
        }
    }

    private static bool IsExpectedException(Exception ex)
    {
        // Unwrap our custom exceptions to check the root cause
        var rootException = ex;
        while (
            rootException.InnerException != null
            && (
                rootException.GetType().Name.Contains("StartupValidation")
                || rootException.GetType().Name.Contains("DirectoryAccess")
            )
        )
        {
            rootException = rootException.InnerException;
        }

        var expectedTypes = new[]
        {
            typeof(UnauthorizedAccessException),
            typeof(DirectoryNotFoundException),
            typeof(IOException),
            typeof(System.Net.Sockets.SocketException),
            typeof(TimeoutException),
            typeof(ArgumentException),
            typeof(InvalidOperationException),
        };

        return expectedTypes.Contains(rootException.GetType());
    }

    private static string GetCleanExceptionMessage(Exception ex)
    {
        // For our custom exceptions, get a clean message
        if (ex.GetType().Name.Contains("StartupValidation") && ex.InnerException != null)
        {
            return GetUserFriendlyMessage(ex.InnerException);
        }

        return GetUserFriendlyMessage(ex);
    }

    private static string GetUserFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException =>
                "Permission denied. Please check file/directory permissions or run with appropriate privileges.",
            DirectoryNotFoundException => "Directory not found. Please ensure the path exists.",
            IOException ioEx when ioEx.Message.Contains("Permission denied") =>
                "Permission denied. Please check file/directory permissions.",
            IOException ioEx when ioEx.Message.Contains("No space left") => "Insufficient disk space available.",
            System.Net.Sockets.SocketException =>
                "Network connection failed. Please check network connectivity and firewall settings.",
            TimeoutException => "Operation timed out. The service may be overloaded or unreachable.",
            _ => ex.Message,
        };
    }
}
