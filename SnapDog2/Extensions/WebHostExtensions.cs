using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace SnapDog2.Extensions;

/// <summary>
/// Extensions for creating resilient web host configurations
/// </summary>
public static class WebHostExtensions
{
    /// <summary>
    /// Configures Kestrel with resilient port binding and fallback logic
    /// </summary>
    public static IWebHostBuilder UseKestrel(this IWebHostBuilder builder, ILogger logger)
    {
        return builder.UseKestrel(
            (context, options) =>
            {
                var configuration = context.Configuration;

                // Get preferred ports from configuration or environment
                var httpPort = GetConfiguredPort(configuration, "HTTP_PORT", 5000);

                logger.LogInformation("🌐 Configuring Kestrel with resilient port binding");
                logger.LogInformation("   Preferred HTTP port: {HttpPort}", httpPort);

                try
                {
                    // Configure HTTP endpoint with fallback
                    var actualHttpPort = ConfigureHttpEndpointWithFallback(options, httpPort, logger);

                    // Update environment variables with actual ports for other services
                    Environment.SetEnvironmentVariable("SNAPDOG_ACTUAL_HTTP_PORT", actualHttpPort.ToString());

                    logger.LogInformation("✅ Kestrel configured successfully");
                    logger.LogInformation("   Actual HTTP port: {ActualHttpPort}", actualHttpPort);
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "💥 Failed to configure Kestrel endpoints. No available ports found.");
                    throw new InvalidOperationException("Unable to bind to any available port", ex);
                }
            }
        );
    }

    private static int GetConfiguredPort(IConfiguration configuration, string key, int defaultPort)
    {
        var portString = configuration[key] ?? Environment.GetEnvironmentVariable(key) ?? defaultPort.ToString();

        return int.TryParse(portString, out var port) ? port : defaultPort;
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
                        logger.LogWarning(
                            "⚠️  HTTP port {PreferredPort} was not available. Using fallback port {ActualPort}",
                            preferredPort,
                            currentPort
                        );
                    }

                    return currentPort;
                }
                else
                {
                    logger.LogDebug(
                        "🚫 HTTP port {Port} is not available (attempt {Attempt}/{MaxAttempts})",
                        currentPort,
                        attempt,
                        maxAttempts
                    );
                    currentPort++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "❌ Failed to bind to HTTP port {Port} (attempt {Attempt}/{MaxAttempts})",
                    currentPort,
                    attempt,
                    maxAttempts
                );
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
