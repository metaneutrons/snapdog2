using Microsoft.Extensions.Logging;

namespace SnapDog2.Worker.Services;

/// <summary>
/// High-performance LoggerMessage definitions for IntegrationServicesHostedService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class IntegrationServicesHostedService
{
    // Service Lifecycle Operations (10001-10004)
    [LoggerMessage(10001, LogLevel.Information, "Integration services initialization cancelled")]
    private partial void LogIntegrationServicesInitializationCancelled();

    [LoggerMessage(10002, LogLevel.Error, "Failed to initialize integration services")]
    private partial void LogFailedToInitializeIntegrationServices(Exception ex);

    [LoggerMessage(10003, LogLevel.Information, "Stopping integration services...")]
    private partial void LogStoppingIntegrationServices();

    [LoggerMessage(10004, LogLevel.Information, "Integration services stopped")]
    private partial void LogIntegrationServicesStopped();
}
