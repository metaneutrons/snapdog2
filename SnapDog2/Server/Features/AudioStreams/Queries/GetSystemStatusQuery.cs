using MediatR;
using SnapDog2.Core.Common;
using SnapDog2.Server.Models;

namespace SnapDog2.Server.Features.AudioStreams.Queries;

/// <summary>
/// Query to retrieve the overall system status including audio stream statistics and health information.
/// Provides a comprehensive overview of the system's current operational state.
/// </summary>
public sealed record GetSystemStatusQuery : IRequest<Result<SystemStatusResponse>>
{
    /// <summary>
    /// Gets a value indicating whether to include detailed stream statistics.
    /// When true, includes breakdown by codec, status, and other metrics.
    /// </summary>
    public bool IncludeStreamStatistics { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to include health check information.
    /// When true, includes status of external services and dependencies.
    /// </summary>
    public bool IncludeHealthChecks { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to include performance metrics.
    /// When true, includes system performance and resource utilization data.
    /// </summary>
    public bool IncludePerformanceMetrics { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to include historical data.
    /// When true, includes trends and historical system information.
    /// </summary>
    public bool IncludeHistoricalData { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to include detailed error information.
    /// When true, includes recent errors and diagnostic information.
    /// </summary>
    public bool IncludeErrorDetails { get; init; } = false;

    /// <summary>
    /// Gets the time window for historical data in hours.
    /// Only used when IncludeHistoricalData is true.
    /// </summary>
    public int HistoricalDataHours { get; init; } = 24;

    /// <summary>
    /// Gets a value indicating whether to include client connection information.
    /// When true, includes details about connected clients and zones.
    /// </summary>
    public bool IncludeClientInfo { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to refresh cached data before returning results.
    /// When true, forces a refresh of all cached system status information.
    /// </summary>
    public bool RefreshCache { get; init; } = false;

    /// <summary>
    /// Creates a query to get system status with default settings.
    /// </summary>
    /// <returns>A new GetSystemStatusQuery instance.</returns>
    public static GetSystemStatusQuery Create() => new();

    /// <summary>
    /// Creates a query to get basic system status with minimal information.
    /// </summary>
    /// <returns>A new GetSystemStatusQuery instance configured for basic status.</returns>
    public static GetSystemStatusQuery CreateBasic() =>
        new()
        {
            IncludeStreamStatistics = false,
            IncludeHealthChecks = false,
            IncludePerformanceMetrics = false,
            IncludeHistoricalData = false,
            IncludeErrorDetails = false,
            IncludeClientInfo = false,
        };

    /// <summary>
    /// Creates a query to get comprehensive system status with all details.
    /// </summary>
    /// <returns>A new GetSystemStatusQuery instance configured for full details.</returns>
    public static GetSystemStatusQuery CreateDetailed() =>
        new()
        {
            IncludeStreamStatistics = true,
            IncludeHealthChecks = true,
            IncludePerformanceMetrics = true,
            IncludeHistoricalData = true,
            IncludeErrorDetails = true,
            IncludeClientInfo = true,
        };

    /// <summary>
    /// Creates a query to get system status with health checks only.
    /// </summary>
    /// <returns>A new GetSystemStatusQuery instance configured for health monitoring.</returns>
    public static GetSystemStatusQuery CreateHealthCheck() =>
        new()
        {
            IncludeStreamStatistics = false,
            IncludeHealthChecks = true,
            IncludePerformanceMetrics = false,
            IncludeHistoricalData = false,
            IncludeErrorDetails = true,
            IncludeClientInfo = false,
        };

    /// <summary>
    /// Creates a query to get system status with performance focus.
    /// </summary>
    /// <returns>A new GetSystemStatusQuery instance configured for performance monitoring.</returns>
    public static GetSystemStatusQuery CreatePerformanceMonitor() =>
        new()
        {
            IncludeStreamStatistics = true,
            IncludeHealthChecks = false,
            IncludePerformanceMetrics = true,
            IncludeHistoricalData = false,
            IncludeErrorDetails = false,
            IncludeClientInfo = true,
        };

    /// <summary>
    /// Creates a query to get system status with historical analysis.
    /// </summary>
    /// <param name="hours">Number of hours of historical data to include.</param>
    /// <returns>A new GetSystemStatusQuery instance configured for historical analysis.</returns>
    public static GetSystemStatusQuery CreateHistoricalAnalysis(int hours = 24) =>
        new()
        {
            IncludeStreamStatistics = true,
            IncludeHealthChecks = true,
            IncludePerformanceMetrics = true,
            IncludeHistoricalData = true,
            IncludeErrorDetails = true,
            IncludeClientInfo = true,
            HistoricalDataHours = hours,
        };

    /// <summary>
    /// Creates a query to get fresh system status with cache refresh.
    /// </summary>
    /// <returns>A new GetSystemStatusQuery instance configured to refresh cache.</returns>
    public static GetSystemStatusQuery CreateFresh() => new() { RefreshCache = true };
}
