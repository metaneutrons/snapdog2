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

namespace SnapDog2.Application.Extensions;

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging.Abstractions;
using SnapDog2.Shared.Configuration;

/// <summary>
/// Extensions for creating resilient web host configurations
/// </summary>
public static partial class WebHostExtensions
{
    /// <summary>
    /// Configures Kestrel with resilient port binding and fallback logic using HttpConfig.
    /// Uses NullLogger to avoid early service provider creation issues.
    /// </summary>
    public static IWebHostBuilder UseResilientKestrel(this IWebHostBuilder builder, HttpConfig httpConfig)
    {
        return builder.UseKestrel(
            (_, options) =>
            {
                // Use NullLogger for infrastructure setup to avoid service provider issues
                var logger = NullLogger.Instance;

                if (!httpConfig.ApiEnabled)
                {
                    LogApiDisabled(logger);
                    return;
                }

                LogKestrelConfiguring(logger);
                LogPreferredHttpPort(logger, httpConfig.HttpPort);

                try
                {
                    // Configure HTTP endpoint with fallback
                    var actualHttpPort = ConfigureHttpEndpointWithFallback(options, httpConfig.HttpPort, logger);

                    LogKestrelConfigured(logger);
                    LogActualHttpPort(logger, actualHttpPort);
                }
                catch (Exception ex)
                {
                    LogKestrelConfigurationFailed(logger, ex);
                    throw new InvalidOperationException("Unable to bind to any available port", ex);
                }
            }
        );
    }

    private static int ConfigureHttpEndpointWithFallback(
        KestrelServerOptions options,
        int preferredPort,
        ILogger logger
    )
    {
        const int maxAttempts = 10;
        var currentPort = preferredPort;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (IsPortAvailable(currentPort))
                {
                    options.Listen(IPAddress.Any, currentPort);

                    if (currentPort != preferredPort)
                    {
                        LogHttpPortFallback(logger, preferredPort, currentPort);
                    }

                    return currentPort;
                }

                LogHttpPortUnavailable(logger, currentPort, attempt, maxAttempts);
                currentPort++;
            }
            catch (Exception ex)
            {
                LogHttpPortBindFailed(logger, currentPort, attempt, maxAttempts, ex.Message);
                currentPort++;
            }
        }

        throw new InvalidOperationException(
            $"Unable to find an available HTTP port after {maxAttempts} attempts starting from {preferredPort}"
        );
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    // LoggerMessage definitions for high-performance logging
    [LoggerMessage(EventId = 14000, Level = LogLevel.Information, Message = "ApiDisabled")]
    private static partial void LogApiDisabled(ILogger logger);

    [LoggerMessage(EventId = 14001, Level = LogLevel.Information, Message = "KestrelConfiguring")]
    private static partial void LogKestrelConfiguring(ILogger logger);

    [LoggerMessage(EventId = 14002, Level = LogLevel.Information, Message = "PreferredHttpPort: {HttpPort}")]
    private static partial void LogPreferredHttpPort(ILogger logger, int httpPort);

    [LoggerMessage(EventId = 14003, Level = LogLevel.Information, Message = "KestrelConfigured")]
    private static partial void LogKestrelConfigured(ILogger logger);

    [LoggerMessage(EventId = 14004, Level = LogLevel.Information, Message = "ActualHttpPort: {ActualHttpPort}")]
    private static partial void LogActualHttpPort(ILogger logger, int actualHttpPort);

    [LoggerMessage(EventId = 14005, Level = LogLevel.Warning, Message = "KestrelConfigurationFailed")]
    private static partial void LogKestrelConfigurationFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 14006, Level = LogLevel.Information, Message = "HTTP port fallback from {PreferredPort} to {CurrentPort}")]
    private static partial void LogHttpPortFallback(ILogger logger, int preferredPort, int currentPort);

    [LoggerMessage(EventId = 14007, Level = LogLevel.Information, Message = "HTTP port {CurrentPort} unavailable (attempt {Attempt}/{MaxAttempts})")]
    private static partial void LogHttpPortUnavailable(ILogger logger, int currentPort, int attempt, int maxAttempts);

    [LoggerMessage(EventId = 14008, Level = LogLevel.Warning, Message = "HTTP port bind failed for {CurrentPort} (attempt {Attempt}/{MaxAttempts}): {Error}")]
    private static partial void LogHttpPortBindFailed(ILogger logger, int currentPort, int attempt, int maxAttempts, string error);
}
