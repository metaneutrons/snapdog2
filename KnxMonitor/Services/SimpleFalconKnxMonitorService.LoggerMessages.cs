using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for KnxMonitorService.
/// </summary>
public partial class KnxMonitorService
{
    // Connection Operations (12001-12010)
    [LoggerMessage(12001, LogLevel.Information, "Starting KNX monitoring with connection type: {ConnectionType}")]
    private partial void LogStartingMonitoring(string connectionType);

    [LoggerMessage(12002, LogLevel.Information, "KNX monitoring started successfully")]
    private partial void LogMonitoringStartedSuccessfully();

    [LoggerMessage(12003, LogLevel.Information, "KNX monitoring stopped")]
    private partial void LogMonitoringStopped();

    [LoggerMessage(12004, LogLevel.Error, "Failed to start KNX monitoring")]
    private partial void LogFailedToStartMonitoring(Exception ex);

    [LoggerMessage(12005, LogLevel.Error, "Error stopping KNX monitoring")]
    private partial void LogErrorStoppingMonitoring(Exception ex);

    // Connection Type Operations (12011-12020)
    [LoggerMessage(12011, LogLevel.Debug, "Creating IP tunneling connection")]
    private partial void LogCreatingIpTunnelingConnection();

    [LoggerMessage(12012, LogLevel.Debug, "Creating IP routing connection")]
    private partial void LogCreatingIpRoutingConnection();

    [LoggerMessage(12013, LogLevel.Debug, "Creating USB connection")]
    private partial void LogCreatingUsbConnection();

    [LoggerMessage(12014, LogLevel.Error, "Failed to create connector parameters")]
    private partial void LogFailedToCreateConnectorParameters();

    // Message Processing (12021-12030)
    [LoggerMessage(12021, LogLevel.Debug, "Received KNX message: {GroupAddress} ({MessageType})")]
    private partial void LogReceivedKnxMessage(string groupAddress, string messageType);

    [LoggerMessage(12022, LogLevel.Error, "Error processing group message")]
    private partial void LogErrorProcessingGroupMessage(Exception ex);

    // Filter Operations (12031-12040)
    [LoggerMessage(12031, LogLevel.Warning, "Invalid filter pattern: {Pattern}")]
    private partial void LogInvalidFilterPattern(Exception ex, string pattern);

    // Disposal Operations (12041-12050)
    [LoggerMessage(12041, LogLevel.Error, "Error during async dispose")]
    private partial void LogErrorDuringAsyncDispose(Exception ex);
}
