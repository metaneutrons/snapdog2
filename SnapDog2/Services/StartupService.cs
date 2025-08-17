using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Services;

/// <summary>
/// resilient startup service that handles port conflicts and other startup failures
/// </summary>
public partial class StartupService : IHostedService
{
    private readonly ILogger<StartupService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly SnapDogConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _environment;
    private readonly bool _isDebugLoggingEnabled;

    private const int MaxRetryAttempts = 5;
    private const int BaseDelayMs = 1000;
    private const int MaxDelayMs = 30000;
    private const int PortScanRange = 100;

    public StartupService(
        ILogger<StartupService> logger,
        IHostApplicationLifetime applicationLifetime,
        IOptions<SnapDogConfiguration> config,
        IServiceProvider serviceProvider,
        IWebHostEnvironment environment
    )
    {
        this._logger = logger;
        this._applicationLifetime = applicationLifetime;
        this._config = config.Value;
        this._serviceProvider = serviceProvider;
        this._environment = environment;

        // Determine if debug logging is enabled
        this._isDebugLoggingEnabled =
            this._config.System.LogLevel.Equals("Debug", StringComparison.OrdinalIgnoreCase)
            || this._config.System.LogLevel.Equals("Trace", StringComparison.OrdinalIgnoreCase);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this.LogStartupSequenceInitiated();

        try
        {
            await this.ExecuteWithRetryAsync(
                "Port Availability Check",
                async () => await this.ValidatePortAvailabilityAsync(cancellationToken),
                cancellationToken
            );

            await this.ExecuteWithRetryAsync(
                "Network Connectivity Check",
                async () => await this.ValidateNetworkConnectivityAsync(cancellationToken),
                cancellationToken
            );

            await this.ExecuteWithRetryAsync(
                "External Dependencies Check",
                async () => await this.ValidateExternalDependenciesAsync(cancellationToken),
                cancellationToken
            );

            this.LogStartupValidationsCompleted();
        }
        catch (StartupValidationException ex)
        {
            if (this._isDebugLoggingEnabled)
            {
                this.LogCriticalStartupFailureWithException(ex.ValidationStep, MaxRetryAttempts, ex);
            }
            else
            {
                this.LogCriticalStartupFailure(ex.ValidationStep, MaxRetryAttempts, GetCleanErrorMessage(ex));
            }

            this.LogStartupFailureDetails(ex);

            // Trigger graceful shutdown
            this._applicationLifetime.StopApplication();
            throw;
        }
        catch (Exception ex)
        {
            if (this._isDebugLoggingEnabled)
            {
                this.LogUnexpectedCriticalFailureWithException(ex);
            }
            else
            {
                this.LogUnexpectedCriticalFailure($"{ex.GetType().Name} - {ex.Message}");
            }

            this.LogUnexpectedFailureDetails(ex);

            // Trigger graceful shutdown
            this._applicationLifetime.StopApplication();
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.LogGracefulShutdownInitiated();
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
                this.LogOperationAttempt(operationName, attempt, MaxRetryAttempts);

                await operation();

                if (attempt > 1)
                {
                    this.LogOperationSucceededOnRetry(operationName, attempt);
                }

                return; // Success!
            }
            catch (Exception ex) when (attempt < MaxRetryAttempts)
            {
                // Log with or without stack trace based on debug level and exception type
                if (this._isDebugLoggingEnabled || IsUnexpectedException(ex))
                {
                    this.LogOperationFailedWithRetryAndDelayAndException(
                        operationName,
                        attempt,
                        MaxRetryAttempts,
                        delay,
                        ex.Message,
                        ex
                    );
                }
                else
                {
                    // For expected exceptions, show a clean message without stack trace
                    var errorMessage = GetCleanErrorMessage(ex);
                    this.LogOperationFailedWithRetryAndDelay(
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
                if (this._isDebugLoggingEnabled || IsUnexpectedException(ex))
                {
                    this.LogOperationFinalFailureWithException(operationName, attempt, MaxRetryAttempts, ex);
                }
                else
                {
                    this.LogOperationFinalFailure(
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
            ("Snapcast JSON-RPC", this._config.Services.Snapcast.JsonRpcPort),
            ("MQTT", this._config.Services.Mqtt.Port),
        };

        var portConflicts = new List<(string Service, int Port, string ConflictDetails)>();

        foreach (var (serviceName, port) in portsToCheck)
        {
            if (port <= 0)
            {
                continue; // Skip unconfigured ports
            }

            try
            {
                var isAvailable = await this.IsPortAvailableAsync(port, cancellationToken);

                if (!isAvailable)
                {
                    var conflictDetails = await this.GetPortConflictDetailsAsync(port, cancellationToken);
                    portConflicts.Add((serviceName, port, conflictDetails));

                    this.LogPortConflictDetected(serviceName, port, conflictDetails);

                    // Attempt to find alternative port
                    var alternativePort = await this.FindAlternativePortAsync(port, cancellationToken);
                    if (alternativePort.HasValue)
                    {
                        this.LogAlternativePortFound(serviceName, alternativePort.Value);
                    }
                }
                else
                {
                    this.LogPortAvailable(port, serviceName);
                }
            }
            catch (Exception ex)
            {
                if (this._isDebugLoggingEnabled)
                {
                    this.LogPortAvailabilityCheckFailedWithException(serviceName, port, ex);
                }
                else
                {
                    this.LogPortAvailabilityCheckFailed(serviceName, port, ex.GetType().Name, ex.Message);
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
            ("Snapcast Server", this._config.Services.Snapcast.Address, this._config.Services.Snapcast.JsonRpcPort),
            ("MQTT Broker", this._config.Services.Mqtt.BrokerAddress, this._config.Services.Mqtt.Port),
            ("KNX Gateway", this._config.Services.Knx.Gateway, this._config.Services.Knx.Port),
        };

        foreach (var (serviceName, address, port) in connectivityChecks)
        {
            if (string.IsNullOrEmpty(address) || port <= 0)
            {
                continue;
            }

            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(address, port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    this.LogConnectivityCheckTimedOut(serviceName, address, port);
                }
                else if (connectTask.IsFaulted)
                {
                    this.LogServiceNotReachable(
                        serviceName,
                        address,
                        port,
                        connectTask.Exception?.GetBaseException().Message ?? "Unknown error"
                    );
                }
                else
                {
                    this.LogConnectivityVerified(serviceName, address, port);
                }
            }
            catch (Exception ex)
            {
                if (this._isDebugLoggingEnabled)
                {
                    this.LogConnectivityVerificationFailedWithException(serviceName, address, port, ex);
                }
                else
                {
                    this.LogConnectivityVerificationFailed(serviceName, address, port, ex.Message);
                }
                // Don't throw - connectivity issues might be temporary
            }
        }
    }

    private async Task ValidateExternalDependenciesAsync(CancellationToken cancellationToken)
    {
        // Skip directory validation in test environment
        if (this._environment.EnvironmentName == "Testing")
        {
            this.LogDirectoryValidationSkipped();
            return;
        }

        // Build list of required directories based on configuration
        var requiredDirectories = new List<string>();

        // Only add log directory if SNAPDOG_SYSTEM_LOG_FILE is configured
        var systemLogFile = Environment.GetEnvironmentVariable("SNAPDOG_SYSTEM_LOG_FILE");
        if (!string.IsNullOrEmpty(systemLogFile))
        {
            var logDirectory = Path.GetDirectoryName(systemLogFile);
            if (!string.IsNullOrEmpty(logDirectory))
            {
                requiredDirectories.Add(logDirectory);
            }
        }

        // Always check temp directory
        requiredDirectories.Add("/tmp/snapdog2");

        // Add data directory if configured
        var dataPath = Environment.GetEnvironmentVariable("SNAPDOG_DATA_PATH");
        if (!string.IsNullOrEmpty(dataPath))
        {
            requiredDirectories.Add(dataPath);
        }

        foreach (var directory in requiredDirectories)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    this.LogDirectoryCreated(directory);
                }

                // Test write permissions
                var testFile = Path.Combine(directory, $"startup-test-{Guid.NewGuid()}.tmp");
                await File.WriteAllTextAsync(testFile, "test", cancellationToken);
                File.Delete(testFile);

                this.LogDirectoryAccessible(directory);
            }
            catch (Exception ex)
            {
                if (this._isDebugLoggingEnabled)
                {
                    this.LogDirectoryNotAccessible(directory, ex);
                }
                else
                {
                    this.LogDirectoryNotAccessibleWithMessage(directory, GetUserFriendlyErrorMessage(ex));
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
        for (var offset = 1; offset <= PortScanRange; offset++)
        {
            var candidatePort = preferredPort + offset;
            if (candidatePort > 65535)
            {
                break;
            }

            if (await this.IsPortAvailableAsync(candidatePort, cancellationToken))
            {
                return candidatePort;
            }
        }

        return null;
    }

    private void LogStartupFailureDetails(StartupValidationException ex)
    {
        // TODO: Check why this is called in a row
        this.LogStartupFailureAnalysisHeader();
        this.LogFailureValidationStep(ex.ValidationStep);
        this.LogFailureAttempts(ex.Attempts);
        this.LogFailureFinalError(GetCleanErrorMessage(ex));
        this.LogFailureTimestamp(DateTime.UtcNow);
        this.LogFailureMachine(Environment.MachineName);
        this.LogFailureProcessId(Environment.ProcessId);
        this.LogFailureWorkingDirectory(Environment.CurrentDirectory);

        if (ex is PortConflictException portEx)
        {
            this.LogFailurePortConflictsHeader();
            foreach (var conflict in portEx.Conflicts)
            {
                this.LogFailurePortConflict(conflict.Service, conflict.Port, conflict.ConflictDetails);
            }
        }

        // Only show detailed stack trace in debug mode
        if (this._isDebugLoggingEnabled && ex.InnerException != null)
        {
            this.LogFailureExceptionDetails(ex.InnerException.ToString());
        }
    }

    private void LogUnexpectedFailureDetails(Exception ex)
    {
        this.LogUnexpectedFailureAnalysisHeader();
        this.LogUnexpectedFailureExceptionType(ex.GetType().FullName ?? "Unknown");
        this.LogUnexpectedFailureErrorMessage(ex.Message);
        this.LogFailureTimestamp(DateTime.UtcNow);
        this.LogFailureMachine(Environment.MachineName);
        this.LogFailureProcessId(Environment.ProcessId);

        // Only show stack trace and inner exceptions in debug mode
        if (this._isDebugLoggingEnabled)
        {
            this.LogUnexpectedFailureStackTrace(ex.StackTrace ?? "No stack trace available");

            var innerEx = ex.InnerException;
            var depth = 1;
            while (innerEx != null && depth <= 5)
            {
                this.LogUnexpectedFailureInnerExceptionWithDepth(
                    depth,
                    innerEx.GetType().FullName ?? "Unknown",
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
        // TODO: do we really need this?
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

    #region Logging

    [LoggerMessage(4001, LogLevel.Information, "🛡️ Initiating startup sequence")]
    private partial void LogStartupSequenceInitiated();

    [LoggerMessage(4002, LogLevel.Information, "✅ All startup validations completed successfully")]
    private partial void LogStartupValidationsCompleted();

    [LoggerMessage(
        4003,
        LogLevel.Critical,
        "🚨 STARTUP VALIDATION FAILED: {ErrorMessage}. Application will terminate to prevent undefined behavior."
    )]
    private partial void LogStartupValidationFailed(string errorMessage);

    [LoggerMessage(
        4004,
        LogLevel.Critical,
        "🚨 STARTUP VALIDATION FAILED: {ErrorMessage}. Application will terminate to prevent undefined behavior."
    )]
    private partial void LogStartupValidationFailedWithException(string errorMessage, Exception exception);

    [LoggerMessage(
        4005,
        LogLevel.Critical,
        "🚨 UNEXPECTED STARTUP FAILURE: {ErrorMessage}. Application will terminate."
    )]
    private partial void LogUnexpectedStartupFailure(string errorMessage);

    [LoggerMessage(
        4006,
        LogLevel.Critical,
        "🚨 UNEXPECTED STARTUP FAILURE: {ErrorMessage}. Application will terminate."
    )]
    private partial void LogUnexpectedStartupFailureWithException(string errorMessage, Exception exception);

    [LoggerMessage(4007, LogLevel.Information, "🛡️ Graceful shutdown initiated")]
    private partial void LogGracefulShutdownInitiated();

    [LoggerMessage(4008, LogLevel.Information, "🔄 {ValidationStep}: Attempt {AttemptNumber}/{MaxAttempts}")]
    private partial void LogValidationAttempt(string validationStep, int attemptNumber, int maxAttempts);

    [LoggerMessage(4009, LogLevel.Information, "🔄 {ValidationStep}: Attempt {AttemptNumber}/{MaxAttempts}")]
    private partial void LogValidationAttemptWithException(
        string validationStep,
        int attemptNumber,
        int maxAttempts,
        Exception exception
    );

    [LoggerMessage(
        4010,
        LogLevel.Warning,
        "⚠️ {ValidationStep} failed (attempt {AttemptNumber}/{MaxAttempts}): {ErrorMessage}"
    )]
    private partial void LogValidationFailedWithRetry(
        string validationStep,
        int attemptNumber,
        int maxAttempts,
        string errorMessage
    );

    [LoggerMessage(
        4011,
        LogLevel.Warning,
        "⚠️ {ValidationStep} failed (attempt {AttemptNumber}/{MaxAttempts}): {ErrorMessage}"
    )]
    private partial void LogValidationFailedWithRetryAndException(
        string validationStep,
        int attemptNumber,
        int maxAttempts,
        string errorMessage,
        Exception exception
    );

    [LoggerMessage(4012, LogLevel.Error, "❌ {ValidationStep} failed after {MaxAttempts} attempts: {ErrorMessage}")]
    private partial void LogValidationFailedFinal(string validationStep, int maxAttempts, string errorMessage);

    [LoggerMessage(4013, LogLevel.Error, "❌ {ValidationStep} failed after {MaxAttempts} attempts: {ErrorMessage}")]
    private partial void LogValidationFailedFinalWithException(
        string validationStep,
        int maxAttempts,
        string errorMessage,
        Exception exception
    );

    [LoggerMessage(4014, LogLevel.Warning, "🔌 Port {Port} ({ServiceName}) is already in use by another process")]
    private partial void LogPortInUse(int port, string serviceName);

    [LoggerMessage(4015, LogLevel.Information, "🔄 Waiting {DelaySeconds}s before retry...")]
    private partial void LogRetryDelay(int delaySeconds);

    [LoggerMessage(4016, LogLevel.Debug, "✅ Port {Port} ({ServiceName}) is available")]
    private partial void LogPortAvailable(int port, string serviceName);

    [LoggerMessage(4017, LogLevel.Error, "❌ Failed to check port {Port} ({ServiceName}): {ErrorMessage}")]
    private partial void LogPortCheckFailed(int port, string serviceName, string errorMessage);

    [LoggerMessage(4018, LogLevel.Error, "❌ Failed to check port {Port} ({ServiceName}): {ErrorMessage}")]
    private partial void LogPortCheckFailedWithException(
        int port,
        string serviceName,
        string errorMessage,
        Exception exception
    );

    [LoggerMessage(4019, LogLevel.Warning, "🔌 {ServiceName} is not reachable ({Address}:{Port}): {ErrorMessage}")]
    private partial void LogServiceNotReachable(string serviceName, string address, int port, string errorMessage);

    [LoggerMessage(4020, LogLevel.Warning, "🔌 {ServiceName} is not reachable ({Address}): {ErrorMessage}")]
    private partial void LogServiceNotReachableNoPort(string serviceName, string address, string errorMessage);

    [LoggerMessage(4021, LogLevel.Debug, "✅ {ServiceName} is reachable ({Address}:{Port})")]
    private partial void LogServiceReachable(string serviceName, string address, int port);

    [LoggerMessage(
        4022,
        LogLevel.Warning,
        "⚠️ Failed to check connectivity to {ServiceName} ({Address}:{Port}): {ErrorMessage}"
    )]
    private partial void LogConnectivityCheckFailed(string serviceName, string address, int port, string errorMessage);

    [LoggerMessage(
        4023,
        LogLevel.Warning,
        "⚠️ Failed to check connectivity to {ServiceName} ({Address}:{Port}): {ErrorMessage}"
    )]
    private partial void LogConnectivityCheckFailedWithException(
        string serviceName,
        string address,
        int port,
        string errorMessage,
        Exception exception
    );

    [LoggerMessage(4024, LogLevel.Information, "📁 Created required directory: {Directory}")]
    private partial void LogDirectoryCreated(string directory);

    [LoggerMessage(4023, LogLevel.Information, "🧪 Directory validation skipped in test environment")]
    private partial void LogDirectoryValidationSkipped();

    [LoggerMessage(4025, LogLevel.Debug, "✅ Directory {Directory} is accessible and writable")]
    private partial void LogDirectoryAccessible(string directory);

    [LoggerMessage(4026, LogLevel.Error, "❌ Directory {Directory} is not accessible or writable")]
    private partial void LogDirectoryNotAccessible(string directory, Exception exception);

    [LoggerMessage(4027, LogLevel.Error, "❌ Directory {Directory} is not accessible or writable: {ErrorMessage}")]
    private partial void LogDirectoryNotAccessibleWithMessage(string directory, string errorMessage);

    [LoggerMessage(4028, LogLevel.Critical, "🚨 STARTUP FAILURE ANALYSIS:")]
    private partial void LogStartupFailureAnalysisHeader();

    [LoggerMessage(4029, LogLevel.Critical, "   Validation Step: {ValidationStep}")]
    private partial void LogFailureValidationStep(string validationStep);

    [LoggerMessage(4030, LogLevel.Critical, "   Attempts Made: {Attempts}")]
    private partial void LogFailureAttempts(int attempts);

    [LoggerMessage(4031, LogLevel.Critical, "   Final Error: {ErrorMessage}")]
    private partial void LogFailureFinalError(string errorMessage);

    [LoggerMessage(4032, LogLevel.Critical, "   Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC")]
    private partial void LogFailureTimestamp(DateTime timestamp);

    [LoggerMessage(4033, LogLevel.Critical, "   Machine: {MachineName}")]
    private partial void LogFailureMachine(string machineName);

    [LoggerMessage(4034, LogLevel.Critical, "   Process ID: {ProcessId}")]
    private partial void LogFailureProcessId(int processId);

    [LoggerMessage(4035, LogLevel.Critical, "   Working Directory: {WorkingDirectory}")]
    private partial void LogFailureWorkingDirectory(string workingDirectory);

    [LoggerMessage(4036, LogLevel.Critical, "   Port Conflicts:")]
    private partial void LogFailurePortConflictsHeader();

    [LoggerMessage(4037, LogLevel.Critical, "     - {Service} on port {Port}: {ConflictDetails}")]
    private partial void LogFailurePortConflict(string service, int port, string conflictDetails);

    [LoggerMessage(4038, LogLevel.Critical, "   Exception Details: {ExceptionDetails}")]
    private partial void LogFailureExceptionDetails(string exceptionDetails);

    [LoggerMessage(4039, LogLevel.Critical, "🚨 UNEXPECTED FAILURE ANALYSIS:")]
    private partial void LogUnexpectedFailureAnalysisHeader();

    [LoggerMessage(4040, LogLevel.Critical, "   Exception Type: {ExceptionType}")]
    private partial void LogUnexpectedFailureExceptionType(string exceptionType);

    [LoggerMessage(4041, LogLevel.Critical, "   Error Message: {ErrorMessage}")]
    private partial void LogUnexpectedFailureErrorMessage(string errorMessage);

    [LoggerMessage(4042, LogLevel.Critical, "   Stack Trace: {StackTrace}")]
    private partial void LogUnexpectedFailureStackTrace(string stackTrace);

    [LoggerMessage(4043, LogLevel.Critical, "   Inner Exception {Depth}: {InnerExceptionType} - {InnerMessage}")]
    private partial void LogUnexpectedFailureInnerExceptionWithDepth(
        int depth,
        string innerExceptionType,
        string innerMessage
    );

    [LoggerMessage(
        4044,
        LogLevel.Critical,
        "💥 CRITICAL STARTUP FAILURE: {ValidationStep} failed after {MaxAttempts} attempts. Application cannot continue safely. Initiating graceful shutdown."
    )]
    private partial void LogCriticalStartupFailureWithException(
        string validationStep,
        int maxAttempts,
        Exception exception
    );

    [LoggerMessage(
        4045,
        LogLevel.Critical,
        "💥 CRITICAL STARTUP FAILURE: {ValidationStep} failed after {MaxAttempts} attempts. Application cannot continue safely. Reason: {ErrorMessage}"
    )]
    private partial void LogCriticalStartupFailure(string validationStep, int maxAttempts, string errorMessage);

    [LoggerMessage(
        4046,
        LogLevel.Critical,
        "💥 UNEXPECTED CRITICAL FAILURE: Application encountered an unexpected error during startup. Initiating emergency shutdown."
    )]
    private partial void LogUnexpectedCriticalFailureWithException(Exception exception);

    [LoggerMessage(
        4047,
        LogLevel.Critical,
        "💥 UNEXPECTED CRITICAL FAILURE: {ErrorMessage}. Application cannot continue safely."
    )]
    private partial void LogUnexpectedCriticalFailure(string errorMessage);

    [LoggerMessage(4048, LogLevel.Information, "🔄 {OperationName}: Attempt {Attempt}/{MaxAttempts}")]
    private partial void LogOperationAttempt(string operationName, int attempt, int maxAttempts);

    [LoggerMessage(4049, LogLevel.Information, "✅ {OperationName}: Succeeded on attempt {Attempt}")]
    private partial void LogOperationSucceededOnRetry(string operationName, int attempt);

    [LoggerMessage(
        4050,
        LogLevel.Warning,
        "⚠️ {OperationName} failed (attempt {Attempt}/{MaxAttempts}): {ErrorMessage}"
    )]
    private partial void LogOperationFailedWithRetry(
        string operationName,
        int attempt,
        int maxAttempts,
        string errorMessage
    );

    [LoggerMessage(
        4051,
        LogLevel.Warning,
        "⚠️ {OperationName} failed (attempt {Attempt}/{MaxAttempts}): {ErrorMessage}"
    )]
    private partial void LogOperationFailedWithRetryAndException(
        string operationName,
        int attempt,
        int maxAttempts,
        string errorMessage,
        Exception exception
    );

    [LoggerMessage(4052, LogLevel.Error, "❌ {OperationName} failed after {MaxAttempts} attempts: {ErrorMessage}")]
    private partial void LogOperationFailedFinal(string operationName, int maxAttempts, string errorMessage);

    [LoggerMessage(4053, LogLevel.Error, "❌ {OperationName} failed after {MaxAttempts} attempts: {ErrorMessage}")]
    private partial void LogOperationFailedFinalWithException(
        string operationName,
        int maxAttempts,
        string errorMessage,
        Exception exception
    );

    [LoggerMessage(
        4054,
        LogLevel.Warning,
        "⚠️ {OperationName}: Attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelayMs} ms. Error: {ErrorMessage}"
    )]
    private partial void LogOperationFailedWithRetryAndDelay(
        string operationName,
        int attempt,
        int maxAttempts,
        int delayMs,
        string errorMessage
    );

    [LoggerMessage(
        4055,
        LogLevel.Warning,
        "⚠️ {OperationName}: Attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelayMs} ms. Error: {ErrorMessage}"
    )]
    private partial void LogOperationFailedWithRetryAndDelayAndException(
        string operationName,
        int attempt,
        int maxAttempts,
        int delayMs,
        string errorMessage,
        Exception exception
    );

    [LoggerMessage(
        4056,
        LogLevel.Error,
        "❌ {OperationName}: Final attempt {Attempt}/{MaxAttempts} failed. Operation cannot be completed."
    )]
    private partial void LogOperationFinalFailureWithException(
        string operationName,
        int attempt,
        int maxAttempts,
        Exception exception
    );

    [LoggerMessage(
        4057,
        LogLevel.Error,
        "❌ {OperationName}: Final attempt {Attempt}/{MaxAttempts} failed. Operation cannot be completed. Error: {ErrorType} - {ErrorMessage}"
    )]
    private partial void LogOperationFinalFailure(
        string operationName,
        int attempt,
        int maxAttempts,
        string errorType,
        string errorMessage
    );

    [LoggerMessage(4058, LogLevel.Error, "❌ Failed to check port availability for {ServiceName} on port {Port}")]
    private partial void LogPortAvailabilityCheckFailedWithException(string serviceName, int port, Exception exception);

    [LoggerMessage(
        4059,
        LogLevel.Error,
        "❌ Failed to check port availability for {ServiceName} on port {Port}: {ErrorType} - {ErrorMessage}"
    )]
    private partial void LogPortAvailabilityCheckFailed(
        string serviceName,
        int port,
        string errorType,
        string errorMessage
    );

    [LoggerMessage(4060, LogLevel.Warning, "❌ Failed to verify {ServiceName} connectivity ({Address}:{Port})")]
    private partial void LogConnectivityVerificationFailedWithException(
        string serviceName,
        string address,
        int port,
        Exception exception
    );

    [LoggerMessage(
        4061,
        LogLevel.Warning,
        "❌ Failed to verify {ServiceName} connectivity ({Address}:{Port}): {ErrorMessage}"
    )]
    private partial void LogConnectivityVerificationFailed(
        string serviceName,
        string address,
        int port,
        string errorMessage
    );

    [LoggerMessage(
        4062,
        LogLevel.Warning,
        "🚫 Port conflict detected: {ServiceName} port {Port} is in use. {ConflictDetails}"
    )]
    private partial void LogPortConflictDetected(string serviceName, int port, string conflictDetails);

    [LoggerMessage(4063, LogLevel.Information, "🔄 Alternative port found for {ServiceName}: {AlternativePort}")]
    private partial void LogAlternativePortFound(string serviceName, int alternativePort);

    [LoggerMessage(4064, LogLevel.Warning, "⏱️  {ServiceName} connectivity check timed out ({Address}:{Port})")]
    private partial void LogConnectivityCheckTimedOut(string serviceName, string address, int port);

    [LoggerMessage(4065, LogLevel.Debug, "✅ {ServiceName} connectivity verified ({Address}:{Port})")]
    private partial void LogConnectivityVerified(string serviceName, string address, int port);

    #endregion
}

/// <summary>
/// Exception thrown when startup validation fails
/// </summary>
public class StartupValidationException(string validationStep, int attempts, Exception innerException)
    : Exception($"Startup validation failed for '{validationStep}' after {attempts} attempts", innerException)
{
    public string ValidationStep { get; } = validationStep;
    public int Attempts { get; } = attempts;
}

/// <summary>
/// Exception thrown when port conflicts are detected
/// </summary>
public class PortConflictException(
    string message,
    IEnumerable<(string Service, int Port, string ConflictDetails)> conflicts
) : StartupValidationException("Port Availability Check", 1, new AddressInUseException(message))
{
    public IReadOnlyList<(string Service, int Port, string ConflictDetails)> Conflicts { get; } =
        conflicts.ToList().AsReadOnly();
}

/// <summary>
/// Exception thrown when directory access fails
/// </summary>
public class DirectoryAccessException(string directory, Exception innerException)
    : StartupValidationException("External Dependencies Check", 1, innerException)
{
    public string Directory { get; } = directory;
}
