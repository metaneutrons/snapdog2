using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace SnapDog2.Extensions;

/// <summary>
/// Extensions for creating resilient web host configurations
/// </summary>
public static class ResilientWebHostExtensions
{
    /// <summary>
    /// Configures Kestrel with resilient port binding and fallback logic
    /// </summary>
    public static IWebHostBuilder UseResilientKestrel(this IWebHostBuilder builder, ILogger logger)
    {
        return builder.UseKestrel(
            (context, options) =>
            {
                var configuration = context.Configuration;

                // Get preferred ports from configuration or environment
                var httpPort = GetConfiguredPort(configuration, "HTTP_PORT", 5000);
                var httpsPort = GetConfiguredPort(configuration, "HTTPS_PORT", 5001);

                logger.LogInformation("🌐 Configuring Kestrel with resilient port binding");
                logger.LogInformation("   Preferred HTTP port: {HttpPort}", httpPort);
                logger.LogInformation("   Preferred HTTPS port: {HttpsPort}", httpsPort);

                try
                {
                    // Configure HTTP endpoint with fallback
                    var actualHttpPort = ConfigureHttpEndpointWithFallback(options, httpPort, logger);

                    // Configure HTTPS endpoint with fallback (if certificates are available)
                    var actualHttpsPort = ConfigureHttpsEndpointWithFallback(options, httpsPort, logger);

                    // Update environment variables with actual ports for other services
                    Environment.SetEnvironmentVariable("SNAPDOG_ACTUAL_HTTP_PORT", actualHttpPort.ToString());
                    if (actualHttpsPort.HasValue)
                    {
                        Environment.SetEnvironmentVariable("SNAPDOG_ACTUAL_HTTPS_PORT", actualHttpsPort.ToString());
                    }

                    logger.LogInformation("✅ Kestrel configured successfully");
                    logger.LogInformation("   Actual HTTP port: {ActualHttpPort}", actualHttpPort);
                    if (actualHttpsPort.HasValue)
                    {
                        logger.LogInformation("   Actual HTTPS port: {ActualHttpsPort}", actualHttpsPort);
                    }
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

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
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

    private static int? ConfigureHttpsEndpointWithFallback(
        KestrelServerOptions options,
        int preferredPort,
        ILogger logger
    )
    {
        // Check if HTTPS certificate is available
        var certPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path");
        var certPassword = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Password");

        if (string.IsNullOrEmpty(certPath))
        {
            logger.LogInformation("ℹ️  No HTTPS certificate configured. Skipping HTTPS endpoint.");
            return null;
        }

        const int maxAttempts = 10;
        var currentPort = preferredPort;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (IsPortAvailable(currentPort))
                {
                    options.Listen(
                        IPAddress.Any,
                        currentPort,
                        listenOptions =>
                        {
                            listenOptions.UseHttps(certPath, certPassword);
                        }
                    );

                    if (currentPort != preferredPort)
                    {
                        logger.LogWarning(
                            "⚠️  HTTPS port {PreferredPort} was not available. Using fallback port {ActualPort}",
                            preferredPort,
                            currentPort
                        );
                    }

                    return currentPort;
                }
                else
                {
                    logger.LogDebug(
                        "🚫 HTTPS port {Port} is not available (attempt {Attempt}/{MaxAttempts})",
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
                    "❌ Failed to bind to HTTPS port {Port} (attempt {Attempt}/{MaxAttempts})",
                    currentPort,
                    attempt,
                    maxAttempts
                );
                currentPort++;
            }
        }

        logger.LogWarning(
            "⚠️  Unable to find an available HTTPS port after {MaxAttempts} attempts starting from {PreferredPort}. HTTPS will be disabled.",
            maxAttempts,
            preferredPort
        );

        return null;
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
