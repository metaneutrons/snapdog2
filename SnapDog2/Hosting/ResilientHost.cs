using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Services;

namespace SnapDog2.Hosting;

/// <summary>
/// Custom host wrapper that handles startup exceptions gracefully
/// </summary>
public class ResilientHost : IHost
{
    private readonly IHost _innerHost;
    private readonly ILogger<ResilientHost> _logger;
    private readonly bool _isDebugMode;

    public ResilientHost(IHost innerHost, ILogger<ResilientHost> logger, bool isDebugMode)
    {
        _innerHost = innerHost;
        _logger = logger;
        _isDebugMode = isDebugMode;
    }

    public IServiceProvider Services => _innerHost.Services;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _innerHost.StartAsync(cancellationToken);
        }
        catch (StartupValidationException ex)
        {
            // Handle our custom startup validation exceptions gracefully
            if (_isDebugMode)
            {
                _logger.LogCritical(ex, "🚨 STARTUP VALIDATION FAILED: {ValidationStep}", ex.ValidationStep);
            }
            else
            {
                _logger.LogCritical(
                    "🚨 STARTUP VALIDATION FAILED: {ValidationStep} - {ErrorMessage}",
                    ex.ValidationStep,
                    GetCleanErrorMessage(ex)
                );
            }

            // Don't re-throw - let the application exit gracefully
            Environment.ExitCode = 1;
            return;
        }
        catch (Exception ex) when (IsExpectedStartupException(ex))
        {
            // Handle expected startup exceptions
            if (_isDebugMode)
            {
                _logger.LogCritical(ex, "🚨 STARTUP FAILED: Expected startup error occurred");
            }
            else
            {
                _logger.LogCritical("🚨 STARTUP FAILED: {ErrorType} - {ErrorMessage}", ex.GetType().Name, ex.Message);
            }

            Environment.ExitCode = 1;
            return;
        }
        catch (Exception ex)
        {
            // Handle unexpected startup exceptions
            if (_isDebugMode)
            {
                _logger.LogCritical(ex, "🚨 UNEXPECTED STARTUP FAILURE: Unhandled exception during host startup");
            }
            else
            {
                _logger.LogCritical(
                    "🚨 UNEXPECTED STARTUP FAILURE: {ErrorType} - {ErrorMessage}",
                    ex.GetType().Name,
                    ex.Message
                );
            }

            Environment.ExitCode = 2;
            throw; // Re-throw unexpected exceptions
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _innerHost.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (_isDebugMode)
            {
                _logger.LogError(ex, "Error during host shutdown");
            }
            else
            {
                _logger.LogError(
                    "Error during host shutdown: {ErrorType} - {ErrorMessage}",
                    ex.GetType().Name,
                    ex.Message
                );
            }
            // Don't re-throw shutdown exceptions
        }
    }

    public void Dispose()
    {
        _innerHost?.Dispose();
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
