using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Extensions;

/// <summary>
/// Extensions for creating resilient web host configurations
/// </summary>
public static partial class WebHostExtensions
{
    /// <summary>
    /// Configures Kestrel with resilient port binding and fallback logic using ApiConfig
    /// </summary>
    public static IWebHostBuilder UseResilientKestrel(this IWebHostBuilder builder, ApiConfig apiConfig, ILogger logger)
    {
        return builder.UseKestrel(
            (context, options) =>
            {
                if (!apiConfig.Enabled)
                {
                    LogApiDisabled(logger);
                    return;
                }

                LogKestrelConfiguring(logger);
                LogPreferredHttpPort(logger, apiConfig.Port);

                try
                {
                    // Configure HTTP endpoint with fallback
                    var actualHttpPort = ConfigureHttpEndpointWithFallback(options, apiConfig.Port, logger);

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
            using var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    [LoggerMessage(12001, LogLevel.Information, "🌐 API is disabled - Kestrel will not bind to any ports")]
    private static partial void LogApiDisabled(ILogger logger);

    [LoggerMessage(12002, LogLevel.Information, "🌐 Configuring Kestrel with resilient port binding")]
    private static partial void LogKestrelConfiguring(ILogger logger);

    [LoggerMessage(12003, LogLevel.Information, "   Preferred HTTP port: {HttpPort}")]
    private static partial void LogPreferredHttpPort(ILogger logger, int httpPort);

    [LoggerMessage(12004, LogLevel.Information, "✅ Kestrel configured successfully")]
    private static partial void LogKestrelConfigured(ILogger logger);

    [LoggerMessage(12005, LogLevel.Information, "   Actual HTTP port: {ActualHttpPort}")]
    private static partial void LogActualHttpPort(ILogger logger, int actualHttpPort);

    [LoggerMessage(12006, LogLevel.Critical, "💥 Failed to configure Kestrel endpoints. No available ports found.")]
    private static partial void LogKestrelConfigurationFailed(ILogger logger, Exception ex);

    [LoggerMessage(
        12007,
        LogLevel.Warning,
        "⚠️ HTTP port {PreferredPort} was not available. Using fallback port {ActualPort}"
    )]
    private static partial void LogHttpPortFallback(ILogger logger, int preferredPort, int actualPort);

    [LoggerMessage(12008, LogLevel.Debug, "🚫 HTTP port {Port} is not available (attempt {Attempt}/{MaxAttempts})")]
    private static partial void LogHttpPortUnavailable(ILogger logger, int port, int attempt, int maxAttempts);

    [LoggerMessage(12009, LogLevel.Warning, "❌ Failed to bind to HTTP port {Port} (attempt {Attempt}/{MaxAttempts})")]
    private static partial void LogHttpPortBindFailed(
        ILogger logger,
        Exception ex,
        int port,
        int attempt,
        int maxAttempts
    );
}

/// <summary>
/// Exception thrown when web host configuration fails
/// </summary>
public class WebHostConfigurationException : Exception
{
    public WebHostConfigurationException(string message)
        : base(message) { }

    public WebHostConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}
