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
using SnapDog2.Application.Services;

namespace SnapDog2.Infrastructure.Hosting;

/// <summary>
/// Custom host wrapper that handles startup exceptions gracefully
/// </summary>
public partial class ResilientHost(IHost innerHost, ILogger<ResilientHost> logger, bool isDebugMode) : IHost
{
    private readonly IHost _innerHost = innerHost;
    private readonly ILogger<ResilientHost> _logger = logger;
    private readonly bool _isDebugMode = isDebugMode;

    public IServiceProvider Services => this._innerHost.Services;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await this._innerHost.StartAsync(cancellationToken);
        }
        catch (StartupValidationException ex)
        {
            // Handle our custom startup validation exceptions gracefully
            if (this._isDebugMode)
            {
                LogStartupValidationFailedDebug(this._logger, ex, ex.ValidationStep);
            }
            else
            {
                LogStartupValidationFailedProduction(this._logger, ex.ValidationStep, GetCleanErrorMessage(ex));
            }

            // Don't re-throw - let the application exit gracefully
            Environment.ExitCode = 1;
        }
        catch (Exception ex) when (IsExpectedStartupException(ex))
        {
            // Handle expected startup exceptions
            if (this._isDebugMode)
            {
                LogStartupFailedDebug(this._logger, ex);
            }
            else
            {
                LogStartupFailedProduction(this._logger, ex.GetType().Name, ex.Message);
            }

            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            // Handle unexpected startup exceptions
            if (this._isDebugMode)
            {
                LogUnexpectedStartupFailureDebug(this._logger, ex);
            }
            else
            {
                LogUnexpectedStartupFailureProduction(this._logger, ex.GetType().Name, ex.Message);
            }

            Environment.ExitCode = 2;
            throw; // Re-throw unexpected exceptions
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await this._innerHost.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (this._isDebugMode)
            {
                this.LogErrorDuringHostShutdown(ex);
            }
            else
            {
                this.LogErrorDuringHostShutdownProduction(ex.GetType().Name, ex.Message);
            }
            // Don't re-throw shutdown exceptions
        }
    }

    public void Dispose()
    {
        this._innerHost.Dispose();
    }

    private static bool IsExpectedStartupException(Exception ex)
    {
        // Check if the exception contains our startup validation exceptions
        var currentEx = ex;
        while (currentEx != null)
        {
            if (currentEx is StartupValidationException or DirectoryAccessException or PortConflictException)
            {
                return true;
            }

            currentEx = currentEx.InnerException;
        }

        // Check for common startup-related exceptions
        var expectedTypes = new[]
        {
            typeof(UnauthorizedAccessException),
            typeof(DirectoryNotFoundException),
            typeof(IOException),
            typeof(System.Net.Sockets.SocketException),
            typeof(Microsoft.AspNetCore.Connections.AddressInUseException),
        };

        return expectedTypes.Contains(ex.GetType())
            || (ex.InnerException != null && expectedTypes.Contains(ex.InnerException.GetType()));
    }

    private static string GetCleanErrorMessage(Exception ex)
    {
        // For our custom exceptions, get the root cause
        if (ex is StartupValidationException sve && sve.InnerException != null)
        {
            return GetUserFriendlyErrorMessage(sve.InnerException);
        }

        return GetUserFriendlyErrorMessage(ex);
    }

    private static string GetUserFriendlyErrorMessage(Exception ex)
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
            Microsoft.AspNetCore.Connections.AddressInUseException =>
                "Port is already in use. Please stop conflicting services or change the port configuration.",
            _ => ex.Message,
        };
    }
}

/// <summary>
/// Extension methods for creating resilient hosts
/// </summary>
public static class ResilientHostExtensions
{
    /// <summary>
    /// Wraps the host with resilient exception handling
    /// </summary>
    public static IHost UseResilientStartup(this IHost host, bool isDebugMode = false)
    {
        var logger = host.Services.GetRequiredService<ILogger<ResilientHost>>();
        return new ResilientHost(host, logger, isDebugMode);
    }
}
