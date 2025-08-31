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
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging.Abstractions;
using SnapDog2.Shared.Configuration;

namespace SnapDog2.Application.Extensions;

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
            (context, options) =>
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
                else
                {
                    LogHttpPortUnavailable(logger, currentPort, attempt, maxAttempts);
                    currentPort++;
                }
            }
            catch (Exception ex)
            {
                LogHttpPortBindFailed(logger, ex, currentPort, attempt, maxAttempts);
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
    [LoggerMessage(
        EventId = 5400,
        Level = LogLevel.Information,
        Message = "🚫 API is disabled - skipping Kestrel configuration"
    )]
    private static partial void LogApiDisabled(ILogger logger);

    [LoggerMessage(EventId = 5401, Level = LogLevel.Information, Message = "🌐 Configuring Kestrel web server...")]
    private static partial void LogKestrelConfiguring(ILogger logger);

    [LoggerMessage(EventId = 5402, Level = LogLevel.Information, Message = "🎯 Preferred HTTP port: {Port}")]
    private static partial void LogPreferredHttpPort(ILogger logger, int Port);

    [LoggerMessage(EventId = 5403, Level = LogLevel.Information, Message = "✅ Kestrel configured successfully")]
    private static partial void LogKestrelConfigured(ILogger logger);

    [LoggerMessage(EventId = 5404, Level = LogLevel.Information, Message = "🌐 HTTP server listening on port: {Port}")]
    private static partial void LogActualHttpPort(ILogger logger, int Port);

    [LoggerMessage(EventId = 5405, Level = LogLevel.Error, Message = "❌ Kestrel configuration failed")]
    private static partial void LogKestrelConfigurationFailed(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 5406,
        Level = LogLevel.Warning,
        Message = "🔄 HTTP port fallback: {PreferredPort} → {ActualPort}"
    )]
    private static partial void LogHttpPortFallback(ILogger logger, int PreferredPort, int ActualPort);

    [LoggerMessage(
        EventId = 5407,
        Level = LogLevel.Warning,
        Message = "⚠️ HTTP port {Port} unavailable (attempt {Attempt}/{MaxAttempts})"
    )]
    private static partial void LogHttpPortUnavailable(ILogger logger, int Port, int Attempt, int MaxAttempts);

    [LoggerMessage(
        EventId = 5408,
        Level = LogLevel.Warning,
        Message = "⚠️ Failed to bind HTTP port {Port} (attempt {Attempt}/{MaxAttempts})"
    )]
    private static partial void LogHttpPortBindFailed(
        ILogger logger,
        Exception ex,
        int Port,
        int Attempt,
        int MaxAttempts
    );
}
