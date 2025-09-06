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
                    logger.LogInformation("ApiDisabled");
                    return;
                }

                logger.LogInformation("KestrelConfiguring");
                logger.LogInformation("PreferredHttpPort: {Details}", httpConfig.HttpPort);

                try
                {
                    // Configure HTTP endpoint with fallback
                    var actualHttpPort = ConfigureHttpEndpointWithFallback(options, httpConfig.HttpPort, logger);

                    logger.LogInformation("KestrelConfigured");
                    logger.LogInformation("ActualHttpPort: {Details}", actualHttpPort);
                }
                catch (Exception ex)
                {
                    logger.LogInformation("KestrelConfigurationFailed: {Details}", ex);
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
                        logger.LogInformation("HttpPortFallback: {Details}", preferredPort, currentPort);
                    }

                    return currentPort;
                }

                logger.LogInformation("HttpPortUnavailable: {Details}", currentPort, attempt, maxAttempts);
                currentPort++;
            }
            catch (Exception ex)
            {
                logger.LogInformation("HttpPortBindFailed: {Details}", ex, currentPort, attempt, maxAttempts);
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

    // LoggerMessage methods for structured logging
}
