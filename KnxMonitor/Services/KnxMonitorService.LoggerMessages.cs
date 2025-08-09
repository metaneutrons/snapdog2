using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for KnxMonitorService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class KnxMonitorService
{
    // Configuration Operations (10601-10602)
    [LoggerMessage(10601, LogLevel.Debug, "KNX Value - Type: {ValueType}, DPT: {DptId}, Data: {Data}")]
    private partial void LogKnxValue(string valueType, string dptId, string data);

    [LoggerMessage(10602, LogLevel.Warning, "Error extracting value information from {ValueType}")]
    private partial void LogErrorExtractingValueInformation(Exception ex, string valueType);

    // Filter and Startup Operations (10603-10608)
    [LoggerMessage(10603, LogLevel.Warning, "Invalid filter pattern: {Pattern}")]
    private partial void LogInvalidFilterPattern(Exception ex, string pattern);

    [LoggerMessage(10604, LogLevel.Information, "Starting KNX monitoring using {ConnectionType}...")]
    private partial void LogStartingKnxMonitoring(string connectionType);

    [LoggerMessage(10605, LogLevel.Information, "KNX monitoring started successfully")]
    private partial void LogKnxMonitoringStartedSuccessfully();

    [LoggerMessage(10606, LogLevel.Error, "Failed to start KNX monitoring")]
    private partial void LogFailedToStartKnxMonitoring(Exception ex);

    // Shutdown and Disposal Operations (10607-10610)
    [LoggerMessage(10607, LogLevel.Information, "KNX monitoring stopped")]
    private partial void LogKnxMonitoringStopped();

    [LoggerMessage(10608, LogLevel.Error, "Error stopping KNX monitoring")]
    private partial void LogErrorStoppingKnxMonitoring(Exception ex);

    [LoggerMessage(10609, LogLevel.Error, "Error during async dispose")]
    private partial void LogErrorDuringAsyncDispose(Exception ex);

    [LoggerMessage(10610, LogLevel.Error, "Error disposing KNX monitor service")]
    private partial void LogErrorDisposingKnxMonitorService(Exception ex);

    // Connection Operations (10611-10616)
    [LoggerMessage(10611, LogLevel.Error, "Failed to create connector parameters")]
    private partial void LogFailedToCreateConnectorParameters(Exception ex);

    [LoggerMessage(10612, LogLevel.Debug, "Creating IP tunneling connection to {GatewayIp}:{Port}")]
    private partial void LogCreatingIpTunnelingConnection(string gatewayIp, int port);

    [LoggerMessage(10613, LogLevel.Debug, "Creating IP routing connection to multicast {MulticastAddress}")]
    private partial void LogCreatingIpRoutingConnection(string multicastAddress);

    [LoggerMessage(
        10614,
        LogLevel.Debug,
        "Creating IP routing connection from {MulticastAddress} to resolved IP {ResolvedIp}"
    )]
    private partial void LogCreatingIpRoutingConnectionWithResolvedIp(string multicastAddress, string resolvedIp);

    [LoggerMessage(10615, LogLevel.Debug, "Creating USB connection to device {DeviceName}")]
    private partial void LogCreatingUsbConnectionToDevice(string deviceName);

    // Message Operations (10616)
    [LoggerMessage(10616, LogLevel.Debug, "KNX Message: {MessageType} -> {GroupAddress}: {DisplayValue}")]
    private partial void LogKnxMessage(string messageType, string groupAddress, string displayValue);
}
