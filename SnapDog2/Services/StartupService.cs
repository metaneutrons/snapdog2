using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Services;

/// <summary>
/// resilient startup service that handles port conflicts and other startup failures
/// </summary>
public class StartupService : IHostedService
{
    private readonly ILogger<StartupService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly SnapDogConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _isDebugLoggingEnabled;

    private const int MaxRetryAttempts = 5;
    private const int BaseDelayMs = 1000;
    private const int MaxDelayMs = 30000;
    private const int PortScanRange = 100;

    public StartupService(
        ILogger<StartupService> logger,
        IHostApplicationLifetime applicationLifetime,
        IOptions<SnapDogConfiguration> config,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _config = config.Value;
        _serviceProvider = serviceProvider;

        // Determine if debug logging is enabled
        _isDebugLoggingEnabled =
            _config.System.LogLevel.Equals("Debug", StringComparison.OrdinalIgnoreCase)
            || _config.System.LogLevel.Equals("Trace", StringComparison.OrdinalIgnoreCase);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛡️ Initiating startup sequence");

        try
        {
            await ExecuteWithRetryAsync(
                "Port Availability Check",
                async () => await ValidatePortAvailabilityAsync(cancellationToken),
                cancellationToken
            );

            await ExecuteWithRetryAsync(
                "Network Connectivity Check",
                async () => await ValidateNetworkConnectivityAsync(cancellationToken),
                cancellationToken
            );

            await ExecuteWithRetryAsync(
                "External Dependencies Check",
                async () => await ValidateExternalDependenciesAsync(cancellationToken),
                cancellationToken
            );

            _logger.LogInformation("✅ All startup validations completed successfully");
        }
        catch (StartupValidationException ex)
        {
            if (_isDebugLoggingEnabled)
            {
                _logger.LogCritical(
                    ex,
                    "💥 CRITICAL STARTUP FAILURE: {ValidationStep} failed after {MaxAttempts} attempts. "
                        + "Application cannot continue safely. Initiating graceful shutdown.",
                    ex.ValidationStep,
                    MaxRetryAttempts
                );
            }
            else
            {
                _logger.LogCritical(
                    "💥 CRITICAL STARTUP FAILURE: {ValidationStep} failed after {MaxAttempts} attempts. "
                        + "Application cannot continue safely. Reason: {ErrorMessage}",
                    ex.ValidationStep,
                    MaxRetryAttempts,
                    GetCleanErrorMessage(ex)
                );
            }

            LogStartupFailureDetails(ex);

            // Trigger graceful shutdown
            _applicationLifetime.StopApplication();
            throw;
        }
        catch (Exception ex)
        {
            if (_isDebugLoggingEnabled)
            {
                _logger.LogCritical(
                    ex,
                    "💥 UNEXPECTED STARTUP FAILURE: Unhandled exception during startup validation. "
                        + "Application state is unknown. Initiating emergency shutdown."
                );
            }
            else
            {
                _logger.LogCritical(
                    "💥 UNEXPECTED STARTUP FAILURE: Unhandled exception during startup validation. "
                        + "Application state is unknown. Error: {ErrorType} - {ErrorMessage}",
                    ex.GetType().Name,
                    ex.Message
                );
            }

            LogUnexpectedFailureDetails(ex);

            // Trigger graceful shutdown
            _applicationLifetime.StopApplication();
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛡️ Graceful shutdown initiated");
        return Task.CompletedTask;
    }

    private async Task ExecuteWithRetryAsync(
        string operationName,
        Func<Task> operation,
        CancellationToken cancellationToken
    )
    {
        var attempt = 0;
        var delay = BaseDelayMs;

        while (attempt < MaxRetryAttempts)
        {
            attempt++;

            try
            {
                _logger.LogInformation(
                    "🔄 {OperationName}: Attempt {Attempt}/{MaxAttempts}",
                    operationName,
                    attempt,
                    MaxRetryAttempts
                );

                await operation();

                if (attempt > 1)
                {
                    _logger.LogInformation("✅ {OperationName}: Succeeded on attempt {Attempt}", operationName, attempt);
                }

                return; // Success!
            }
            catch (Exception ex) when (attempt < MaxRetryAttempts)
            {
                // Log with or without stack trace based on debug level and exception type
                if (_isDebugLoggingEnabled || IsUnexpectedException(ex))
                {
                    _logger.LogWarning(
                        ex,
                        "⚠️  {OperationName}: Attempt {Attempt}/{MaxAttempts} failed. "
                            + "Retrying in {DelayMs} ms. Error: {ErrorMessage}",
                        operationName,
                        attempt,
                        MaxRetryAttempts,
                        delay,
                        ex.Message
                    );
                }
                else
                {
                    // For expected exceptions, show a clean message without stack trace
                    var errorMessage = GetCleanErrorMessage(ex);
                    _logger.LogWarning(
                        "⚠️  {OperationName}: Attempt {Attempt}/{MaxAttempts} failed. "
                            + "Retrying in {DelayMs} ms. Error: {ErrorMessage}",
                        operationName,
                        attempt,
                        MaxRetryAttempts,
                        delay,
                        errorMessage
                    );
                }

                await Task.Delay(delay, cancellationToken);

                // Exponential backoff with jitter
                delay = Math.Min(delay * 2 + Random.Shared.Next(0, 1000), MaxDelayMs);
            }
            catch (Exception ex)
            {
                // Final attempt failed - always log with more detail but conditionally include stack trace
                if (_isDebugLoggingEnabled || IsUnexpectedException(ex))
                {
                    _logger.LogError(
                        ex,
                        "❌ {OperationName}: Final attempt {Attempt}/{MaxAttempts} failed. "
                            + "Operation cannot be completed.",
                        operationName,
                        attempt,
                        MaxRetryAttempts
                    );
                }
                else
                {
                    _logger.LogError(
                        "❌ {OperationName}: Final attempt {Attempt}/{MaxAttempts} failed. "
                            + "Operation cannot be completed. Error: {ErrorType} - {ErrorMessage}",
                        operationName,
                        attempt,
                        MaxRetryAttempts,
                        ex.GetType().Name,
                        ex.Message
                    );
                }

                throw new StartupValidationException(operationName, attempt, ex);
            }
        }
    }

    private async Task ValidatePortAvailabilityAsync(CancellationToken cancellationToken)
    {
        var portsToCheck = new[]
        {
            ("Snapcast JSON-RPC", _config.Services.Snapcast.JsonRpcPort),
            ("MQTT", _config.Services.Mqtt.Port),
            ("Prometheus", _config.Telemetry.Prometheus.Port),
        };

        var portConflicts = new List<(string Service, int Port, string ConflictDetails)>();

        foreach (var (serviceName, port) in portsToCheck)
        {
            if (port <= 0)
                continue; // Skip unconfigured ports

            try
            {
                var isAvailable = await IsPortAvailableAsync(port, cancellationToken);

                if (!isAvailable)
                {
                    var conflictDetails = await GetPortConflictDetailsAsync(port, cancellationToken);
                    portConflicts.Add((serviceName, port, conflictDetails));

                    _logger.LogWarning(
                        "🚫 Port conflict detected: {ServiceName} port {Port} is in use. {ConflictDetails}",
                        serviceName,
                        port,
                        conflictDetails
                    );

                    // Attempt to find alternative port
                    var alternativePort = await FindAlternativePortAsync(port, cancellationToken);
                    if (alternativePort.HasValue)
                    {
                        _logger.LogInformation(
                            "🔄 Alternative port found for {ServiceName}: {AlternativePort}",
                            serviceName,
                            alternativePort.Value
                        );
                    }
                }
                else
                {
                    _logger.LogDebug("✅ Port {Port} ({ServiceName}) is available", port, serviceName);
                }
            }
            catch (Exception ex)
            {
                if (_isDebugLoggingEnabled)
                {
                    _logger.LogError(
                        ex,
                        "❌ Failed to check port availability for {ServiceName} on port {Port}",
                        serviceName,
                        port
                    );
                }
                else
                {
                    _logger.LogError(
                        "❌ Failed to check port availability for {ServiceName} on port {Port}: {ErrorType} - {ErrorMessage}",
                        serviceName,
                        port,
                        ex.GetType().Name,
                        ex.Message
                    );
                }
                throw;
            }
        }

        if (portConflicts.Any())
        {
            var conflictSummary = string.Join(", ", portConflicts.Select(c => $"{c.Service}:{c.Port}"));

            throw new PortConflictException(
                $"Port conflicts detected: {conflictSummary}. "
                    + "Please stop conflicting services or update SnapDog2 configuration to use different ports.",
                portConflicts
            );
        }
    }

    private async Task ValidateNetworkConnectivityAsync(CancellationToken cancellationToken)
    {
        var connectivityChecks = new[]
        {
            ("Snapcast Server", _config.Services.Snapcast.Address, _config.Services.Snapcast.JsonRpcPort),
            ("MQTT Broker", _config.Services.Mqtt.BrokerAddress, _config.Services.Mqtt.Port),
            ("KNX Gateway", _config.Services.Knx.Gateway, _config.Services.Knx.Port),
        };

        foreach (var (serviceName, address, port) in connectivityChecks)
        {
            if (string.IsNullOrEmpty(address) || port <= 0)
                continue;

            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(address, port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning(
                        "⏱️  {ServiceName} connectivity check timed out ({Address}:{Port})",
                        serviceName,
                        address,
                        port
                    );
                }
                else if (connectTask.IsFaulted)
                {
                    _logger.LogWarning(
                        "🔌 {ServiceName} is not reachable ({Address}:{Port}): {Error}",
                        serviceName,
                        address,
                        port,
                        connectTask.Exception?.GetBaseException().Message
                    );
                }
                else
                {
                    _logger.LogDebug(
                        "✅ {ServiceName} connectivity verified ({Address}:{Port})",
                        serviceName,
                        address,
                        port
                    );
                }
            }
            catch (Exception ex)
            {
                if (_isDebugLoggingEnabled)
                {
                    _logger.LogWarning(
                        ex,
                        "❌ Failed to verify {ServiceName} connectivity ({Address}:{Port})",
                        serviceName,
                        address,
                        port
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "❌ Failed to verify {ServiceName} connectivity ({Address}:{Port}): {ErrorMessage}",
                        serviceName,
                        address,
                        port,
                        ex.Message
                    );
                }
                // Don't throw - connectivity issues might be temporary
            }
        }
    }

    private async Task ValidateExternalDependenciesAsync(CancellationToken cancellationToken)
    {
        // Check if required directories exist and are writable
        var requiredDirectories = new[]
        {
            "/var/log/snapdog2",
            "/tmp/snapdog2",
            Environment.GetEnvironmentVariable("SNAPDOG_DATA_PATH") ?? "/data/snapdog2",
        };

        foreach (var directory in requiredDirectories)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation("📁 Created required directory: {Directory}", directory);
                }

                // Test write permissions
                var testFile = Path.Combine(directory, $"startup-test-{Guid.NewGuid()}.tmp");
                await File.WriteAllTextAsync(testFile, "test", cancellationToken);
                File.Delete(testFile);

                _logger.LogDebug("✅ Directory {Directory} is accessible and writable", directory);
            }
            catch (Exception ex)
            {
                if (_isDebugLoggingEnabled)
                {
                    _logger.LogError(ex, "❌ Directory {Directory} is not accessible or writable", directory);
                }
                else
                {
                    _logger.LogError(
                        "❌ Directory {Directory} is not accessible or writable: {ErrorMessage}",
                        directory,
                        GetUserFriendlyErrorMessage(ex)
                    );
                }
                throw new DirectoryAccessException(directory, ex);
            }
        }
    }

    private Task<bool> IsPortAvailableAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return Task.FromResult(true);
        }
        catch (SocketException)
        {
            return Task.FromResult(false);
        }
    }

    private Task<string> GetPortConflictDetailsAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            // This is a simplified version - in production you might want to use platform-specific APIs
            var processes = System.Diagnostics.Process.GetProcesses().Where(p => !p.HasExited).ToList();

            return Task.FromResult($"Port {port} is occupied by another process");
        }
        catch
        {
            return Task.FromResult($"Port {port} is in use (details unavailable)");
        }
    }

    private async Task<int?> FindAlternativePortAsync(int preferredPort, CancellationToken cancellationToken)
    {
        for (int offset = 1; offset <= PortScanRange; offset++)
        {
            var candidatePort = preferredPort + offset;
            if (candidatePort > 65535)
                break;

            if (await IsPortAvailableAsync(candidatePort, cancellationToken))
            {
                return candidatePort;
            }
        }

        return null;
    }

    private void LogStartupFailureDetails(StartupValidationException ex)
    {
        _logger.LogCritical("🚨 STARTUP FAILURE ANALYSIS:");
        _logger.LogCritical("   Validation Step: {ValidationStep}", ex.ValidationStep);
        _logger.LogCritical("   Attempts Made: {Attempts}", ex.Attempts);
        _logger.LogCritical("   Final Error: {ErrorMessage}", GetCleanErrorMessage(ex));
        _logger.LogCritical("   Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC", DateTime.UtcNow);
        _logger.LogCritical("   Machine: {MachineName}", Environment.MachineName);
        _logger.LogCritical("   Process ID: {ProcessId}", Environment.ProcessId);
        _logger.LogCritical("   Working Directory: {WorkingDirectory}", Environment.CurrentDirectory);

        if (ex is PortConflictException portEx)
        {
            _logger.LogCritical("   Port Conflicts:");
            foreach (var conflict in portEx.Conflicts)
            {
                _logger.LogCritical(
                    "     - {Service} on port {Port}: {Details}",
                    conflict.Service,
                    conflict.Port,
                    conflict.ConflictDetails
                );
            }
        }

        // Only show detailed stack trace in debug mode
        if (_isDebugLoggingEnabled && ex.InnerException != null)
        {
            _logger.LogCritical("   Exception Details: {ExceptionDetails}", ex.InnerException.ToString());
        }
    }

    private void LogUnexpectedFailureDetails(Exception ex)
    {
        _logger.LogCritical("🚨 UNEXPECTED FAILURE ANALYSIS:");
        _logger.LogCritical("   Exception Type: {ExceptionType}", ex.GetType().FullName);
        _logger.LogCritical("   Error Message: {ErrorMessage}", ex.Message);
        _logger.LogCritical("   Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC", DateTime.UtcNow);
        _logger.LogCritical("   Machine: {MachineName}", Environment.MachineName);
        _logger.LogCritical("   Process ID: {ProcessId}", Environment.ProcessId);

        // Only show stack trace and inner exceptions in debug mode
        if (_isDebugLoggingEnabled)
        {
            _logger.LogCritical("   Stack Trace: {StackTrace}", ex.StackTrace);

            var innerEx = ex.InnerException;
            var depth = 1;
            while (innerEx != null && depth <= 5)
            {
                _logger.LogCritical(
                    "   Inner Exception {Depth}: {InnerExceptionType} - {InnerMessage}",
                    depth,
                    innerEx.GetType().FullName,
                    innerEx.Message
                );
                innerEx = innerEx.InnerException;
                depth++;
            }
        }
    }

    /// <summary>
    /// Gets a clean error message without stack traces, handling wrapped exceptions
    /// </summary>
    private static string GetCleanErrorMessage(Exception ex)
    {
        // For our custom exceptions, get the root cause
        if (ex is StartupValidationException sve && sve.InnerException != null)
        {
            return GetUserFriendlyErrorMessage(sve.InnerException);
        }

        return GetUserFriendlyErrorMessage(ex);
    }

    /// <summary>
    /// Determines if an exception is unexpected and should always include stack trace
    /// </summary>
    private static bool IsUnexpectedException(Exception ex)
    {
        // For our custom exceptions, check the inner exception
        if (ex is StartupValidationException sve && sve.InnerException != null)
        {
            ex = sve.InnerException;
        }

        // These are expected exceptions that don't need stack traces in production
        var expectedExceptionTypes = new[]
        {
            typeof(UnauthorizedAccessException),
            typeof(DirectoryNotFoundException),
            typeof(IOException),
            typeof(SocketException),
            typeof(TimeoutException),
            typeof(ArgumentException),
            typeof(InvalidOperationException),
            typeof(AddressInUseException),
        };

        return !expectedExceptionTypes.Contains(ex.GetType());
    }

    /// <summary>
    /// Gets a user-friendly error message for common exceptions
    /// </summary>
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
            SocketException => "Network connection failed. Please check network connectivity and firewall settings.",
            TimeoutException => "Operation timed out. The service may be overloaded or unreachable.",
            _ => ex.Message,
        };
    }
}

/// <summary>
/// Exception thrown when startup validation fails
/// </summary>
public class StartupValidationException : Exception
{
    public string ValidationStep { get; }
    public int Attempts { get; }

    public StartupValidationException(string validationStep, int attempts, Exception innerException)
        : base($"Startup validation failed for '{validationStep}' after {attempts} attempts", innerException)
    {
        ValidationStep = validationStep;
        Attempts = attempts;
    }
}

/// <summary>
/// Exception thrown when port conflicts are detected
/// </summary>
public class PortConflictException : StartupValidationException
{
    public IReadOnlyList<(string Service, int Port, string ConflictDetails)> Conflicts { get; }

    public PortConflictException(
        string message,
        IEnumerable<(string Service, int Port, string ConflictDetails)> conflicts
    )
        : base("Port Availability Check", 1, new AddressInUseException(message))
    {
        Conflicts = conflicts.ToList().AsReadOnly();
    }
}

/// <summary>
/// Exception thrown when directory access fails
/// </summary>
public class DirectoryAccessException : StartupValidationException
{
    public string Directory { get; }

    public DirectoryAccessException(string directory, Exception innerException)
        : base("External Dependencies Check", 1, innerException)
    {
        Directory = directory;
    }
}
