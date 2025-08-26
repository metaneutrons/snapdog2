//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Infrastructure.Application;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Metrics;

/// <summary>
/// Enterprise-grade implementation of metrics service using OpenTelemetry.
/// Replaces the placeholder MetricsService with proper metrics collection.
/// </summary>
public partial class EnterpriseMetricsService : IMetricsService, IDisposable
{
    private readonly ILogger<EnterpriseMetricsService> _logger;
    private readonly IApplicationMetrics _applicationMetrics;
    private readonly Timer _systemMetricsTimer;

    private static readonly DateTime _startTime = DateTime.UtcNow;
    private long _processedRequests = 0;

    public EnterpriseMetricsService(ILogger<EnterpriseMetricsService> logger, IApplicationMetrics applicationMetrics)
    {
        _logger = logger;
        _applicationMetrics = applicationMetrics;

        // Start system metrics collection timer (every 30 seconds)
        _systemMetricsTimer = new Timer(CollectSystemMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        LogServiceInitialized(_logger);
    }

    #region IMetricsService Implementation

    public void RecordCortexMediatorRequestDuration(
        string requestType,
        string requestName,
        long durationMs,
        bool success
    )
    {
        var durationSeconds = durationMs / 1000.0;

        if (requestType.Equals("Command", StringComparison.OrdinalIgnoreCase))
        {
            _applicationMetrics.RecordCommand(requestName, durationSeconds, success);
        }
        else if (requestType.Equals("Query", StringComparison.OrdinalIgnoreCase))
        {
            _applicationMetrics.RecordQuery(requestName, durationSeconds, success);
        }

        this.LogRecordingRequestDuration(requestType, requestName, durationMs, success);
    }

    public void IncrementCounter(string name, long delta = 1, params (string Key, string Value)[] labels)
    {
        // For backwards compatibility, log the counter increment
        // In a full enterprise implementation, you might want to create dynamic counters
        this.LogCounterIncrement(name, delta, FormatLabels(labels));

        // Track processed requests for server stats
        if (name.Contains("request", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Add(ref _processedRequests, delta);
        }
    }

    public void SetGauge(string name, double value, params (string Key, string Value)[] labels)
    {
        // For backwards compatibility, log the gauge set
        // In a full enterprise implementation, you might want to create dynamic gauges
        this.LogGaugeSet(name, value, FormatLabels(labels));
    }

    public async Task<ServerStats> GetServerStatsAsync()
    {
        this.LogGettingServerStats();

        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - _startTime;

        // Get system memory info
        var totalMemoryMb = GetTotalSystemMemoryMb();
        var processMemoryMb = process.WorkingSet64 / (1024.0 * 1024.0);
        var memoryUsagePercent = totalMemoryMb > 0 ? (processMemoryMb / totalMemoryMb) * 100 : 0;

        var stats = new ServerStats
        {
            TimestampUtc = DateTime.UtcNow,
            CpuUsagePercent = await GetCpuUsageAsync(),
            MemoryUsageMb = processMemoryMb,
            TotalMemoryMb = totalMemoryMb,
            Uptime = uptime,
            ActiveConnections = GetActiveConnections(),
            ProcessedRequests = Interlocked.Read(ref _processedRequests),
        };

        return stats;
    }

    #endregion

    #region System Metrics Collection

    private void CollectSystemMetrics(object? state)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var memoryUsageMb = process.WorkingSet64 / (1024.0 * 1024.0);
            var totalMemoryMb = GetTotalSystemMemoryMb();
            var memoryUsagePercent = totalMemoryMb > 0 ? (memoryUsageMb / totalMemoryMb) * 100 : 0;

            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
            var threadPoolThreads = maxWorkerThreads - availableWorkerThreads;

            var systemMetrics = new SystemMetricsState
            {
                CpuUsagePercent = GetCpuUsageSync(),
                MemoryUsageMb = memoryUsageMb,
                MemoryUsagePercent = memoryUsagePercent,
                ActiveConnections = GetActiveConnections(),
                ThreadPoolThreads = threadPoolThreads,
            };

            _applicationMetrics.UpdateSystemMetrics(systemMetrics);
        }
        catch (Exception ex)
        {
            LogFailedToCollectSystemMetrics(_logger, ex);
        }
    }

    private async Task<double> GetCpuUsageAsync()
    {
        try
        {
            // Cross-platform CPU usage calculation
            return await GetCpuUsageCrossPlatformAsync();
        }
        catch (Exception ex)
        {
            LogFailedToGetCpuUsage(_logger, ex);
            return 0.0;
        }
    }

    private double GetCpuUsageSync()
    {
        try
        {
            // For synchronous calls, return 0 to avoid blocking
            // In a production system, you might cache the last async result
            return 0.0;
        }
        catch (Exception ex)
        {
            LogFailedToGetCpuUsageSync(_logger, ex);
            return 0.0;
        }
    }

    private static async Task<double> GetCpuUsageCrossPlatformAsync()
    {
        // Cross-platform CPU usage calculation using Process.TotalProcessorTime
        var startTime = DateTime.UtcNow;
        var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

        await Task.Delay(100); // Small delay to measure CPU usage

        var endTime = DateTime.UtcNow;
        var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

        return cpuUsageTotal * 100;
    }

    private static double GetTotalSystemMemoryMb()
    {
        try
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            if (gcMemoryInfo.TotalAvailableMemoryBytes > 0)
            {
                return gcMemoryInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0);
            }

            // Fallback: estimate based on current process memory
            var process = Process.GetCurrentProcess();
            return process.WorkingSet64 / (1024.0 * 1024.0) * 4; // Rough estimate
        }
        catch
        {
            return 0.0;
        }
    }

    private static int GetActiveConnections()
    {
        try
        {
            // In a real implementation, you would track actual HTTP connections
            // For now, return the number of active threads as a proxy
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out _);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);
            return maxWorkerThreads - availableWorkerThreads;
        }
        catch
        {
            return 0;
        }
    }

    #endregion

    #region Public Metrics Methods

    public void RecordHttpRequest(string method, string endpoint, int statusCode, double durationSeconds)
    {
        _applicationMetrics.RecordHttpRequest(method, endpoint, statusCode, durationSeconds);
        Interlocked.Increment(ref _processedRequests);
    }

    public void RecordError(string errorType, string component, string? operation = null)
    {
        _applicationMetrics.RecordError(errorType, component, operation);
    }

    public void RecordException(Exception exception, string component, string? operation = null)
    {
        _applicationMetrics.RecordException(exception, component, operation);
    }

    public void UpdateBusinessMetrics(BusinessMetricsState businessMetrics)
    {
        _applicationMetrics.UpdateBusinessMetrics(businessMetrics);
    }

    public void RecordTrackChange(string zoneId, string? fromTrack, string? toTrack)
    {
        _applicationMetrics.RecordTrackChange(zoneId, fromTrack, toTrack);
    }

    public void RecordVolumeChange(string targetId, string targetType, int fromVolume, int toVolume)
    {
        _applicationMetrics.RecordVolumeChange(targetId, targetType, fromVolume, toVolume);
    }

    #endregion

    #region Logging

    [LoggerMessage(
        EventId = 6001,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Recording {RequestType} request '{RequestName}' duration: {DurationMs}ms, Success: {Success}"
    )]
    private partial void LogRecordingRequestDuration(
        string requestType,
        string requestName,
        long durationMs,
        bool success
    );

    [LoggerMessage(
        EventId = 6002,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting server statistics"
    )]
    private partial void LogGettingServerStats();

    [LoggerMessage(
        EventId = 6003,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Metric counter increment: {Name} += {Delta} {Labels}"
    )]
    private partial void LogCounterIncrement(string name, long delta, string labels);

    [LoggerMessage(
        EventId = 6004,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Metric gauge set: {Name} = {Value} {Labels}"
    )]
    private partial void LogGaugeSet(string name, double value, string labels);

    [LoggerMessage(
        EventId = 6000,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "EnterpriseMetricsService initialized with system metrics collection"
    )]
    private static partial void LogServiceInitialized(ILogger logger);

    [LoggerMessage(
        EventId = 6001,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to collect system metrics"
    )]
    private static partial void LogFailedToCollectSystemMetrics(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 6002,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Failed to get CPU usage"
    )]
    private static partial void LogFailedToGetCpuUsage(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 6003,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Failed to get CPU usage synchronously"
    )]
    private static partial void LogFailedToGetCpuUsageSync(ILogger logger, Exception ex);

    private static string FormatLabels((string Key, string Value)[] labels)
    {
        if (labels == null || labels.Length == 0)
        {
            return string.Empty;
        }

        return "[" + string.Join(", ", labels.Select(l => $"{l.Key}={l.Value}")) + "]";
    }

    #endregion

    public void Dispose()
    {
        _systemMetricsTimer?.Dispose();
        _applicationMetrics?.Dispose();
    }
}
