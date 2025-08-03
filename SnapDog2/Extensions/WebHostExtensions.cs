using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Extensions;

/// <summary>
/// Extensions for creating resilient web host configurations
/// </summary>
public static class WebHostExtensions
{
    /// <summary>
    /// Configures Kestrel with resilient port binding and fallback logic using ApiConfig
    /// </summary>
    public static IWebHostBuilder UseResilientKestrel(
        this IWebHostBuilder builder,
        ApiConfig apiConfig,
        Serilog.ILogger logger
    )
    {
        return builder.UseKestrel(
            (context, options) =>
            {
                if (!apiConfig.Enabled)
                {
                    logger.Information("🌐 API is disabled - Kestrel will not bind to any ports");
                    return;
                }

                logger.Information("🌐 Configuring Kestrel with resilient port binding");
                logger.Information("   Preferred HTTP port: {HttpPort}", apiConfig.Port);

                try
                {
                    // Configure HTTP endpoint with fallback
                    var actualHttpPort = ConfigureHttpEndpointWithFallback(options, apiConfig.Port, logger);

                    // Update environment variables with actual ports for other services
                    Environment.SetEnvironmentVariable("SNAPDOG_ACTUAL_HTTP_PORT", actualHttpPort.ToString());

                    logger.Information("✅ Kestrel configured successfully");
                    logger.Information("   Actual HTTP port: {ActualHttpPort}", actualHttpPort);
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, "💥 Failed to configure Kestrel endpoints. No available ports found.");
                    throw new InvalidOperationException("Unable to bind to any available port", ex);
                }
            }
        );
    }

    /// <summary>
    /// Configures Kestrel with resilient port binding and fallback logic (legacy method for backward compatibility)
    /// </summary>
    [Obsolete("Use UseResilientKestrel(ApiConfig, ILogger) instead to properly integrate with EnvoyConfig")]
    public static IWebHostBuilder UseKestrel(this IWebHostBuilder builder, Microsoft.Extensions.Logging.ILogger logger)
    {
        return builder.UseKestrel(
            (context, options) =>
            {
                var configuration = context.Configuration;

                // Get preferred ports from configuration or environment
                var httpPort = GetConfiguredPort(configuration, "HTTP_PORT", 5000);

                logger.LogInformation("🌐 Configuring Kestrel with resilient port binding (legacy method)");
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
        Serilog.ILogger logger
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
                        logger.Warning(
                            "⚠️  HTTP port {PreferredPort} was not available. Using fallback port {ActualPort}",
                            preferredPort,
                            currentPort
                        );
                    }

                    return currentPort;
                }
                else
                {
                    logger.Debug(
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
                logger.Warning(
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

    private static int ConfigureHttpEndpointWithFallback(
        KestrelServerOptions options,
        int preferredPort,
        Microsoft.Extensions.Logging.ILogger logger
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
