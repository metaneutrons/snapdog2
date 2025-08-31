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
namespace SnapDog2.Infrastructure.Metrics;

using System.Diagnostics.Metrics;
using SnapDog2.Domain.Abstractions;

/// <summary>
/// Enterprise-grade application metrics using OpenTelemetry.
/// Provides comprehensive monitoring across all application layers.
/// Follows Prometheus naming conventions and OpenTelemetry best practices.
/// </summary>
public partial class ApplicationMetrics : IApplicationMetrics
{
    private readonly Meter _meter;
    private readonly ILogger<ApplicationMetrics> _logger;

    // HTTP Metrics
    private readonly Counter<long> _httpRequestsTotal;
    private readonly Histogram<double> _httpRequestDuration;
    private readonly Counter<long> _httpRequestsErrors;

    // Command/Query Metrics
    private readonly Counter<long> _commandsTotal;
    private readonly Counter<long> _queriesTotal;
    private readonly Histogram<double> _commandDuration;
    private readonly Histogram<double> _queryDuration;
    private readonly Counter<long> _commandErrors;
    private readonly Counter<long> _queryErrors;

    // System Metrics
    private readonly ObservableGauge<double> _cpuUsagePercent;
    private readonly ObservableGauge<double> _memoryUsageMb;
    private readonly ObservableGauge<double> _memoryUsagePercent;
    private readonly ObservableGauge<long> _uptimeSeconds;
    private readonly ObservableGauge<int> _activeConnections;
    private readonly ObservableGauge<int> _threadPoolThreads;

    // Business Metrics
    private readonly ObservableGauge<int> _zonesTotal;
    private readonly ObservableGauge<int> _zonesActive;
    private readonly ObservableGauge<int> _clientsConnected;
    private readonly ObservableGauge<int> _tracksPlaying;
    private readonly Counter<long> _trackChangesTotal;
    private readonly Counter<long> _volumeChangesTotal;

    // Error Tracking
    private readonly Counter<long> _errorsTotal;
    private readonly Counter<long> _exceptionsTotal;

    // Current state for observable gauges
    private volatile SystemMetricsState _systemState = new();
    private volatile BusinessMetricsState _businessState = new();

    private static readonly DateTime _startTime = DateTime.UtcNow;

    public ApplicationMetrics(ILogger<ApplicationMetrics> logger)
    {
        this._logger = logger;
        this._meter = new Meter("SnapDog2.Application", "2.0.0");

        // HTTP Metrics
        this._httpRequestsTotal = this._meter.CreateCounter<long>(
            "snapdog_http_requests_total",
            description: "Total number of HTTP requests processed"
        );

        this._httpRequestDuration = this._meter.CreateHistogram<double>(
            "snapdog_http_request_duration_seconds",
            unit: "s",
            description: "Duration of HTTP requests in seconds"
        );

        this._httpRequestsErrors = this._meter.CreateCounter<long>(
            "snapdog_http_requests_errors_total",
            description: "Total number of HTTP request errors"
        );

        // Command/Query Metrics
        this._commandsTotal = this._meter.CreateCounter<long>(
            "snapdog_commands_total",
            description: "Total number of commands processed"
        );

        this._queriesTotal = this._meter.CreateCounter<long>(
            "snapdog_queries_total",
            description: "Total number of queries processed"
        );

        this._commandDuration = this._meter.CreateHistogram<double>(
            "snapdog_command_duration_seconds",
            unit: "s",
            description: "Duration of command processing in seconds"
        );

        this._queryDuration = this._meter.CreateHistogram<double>(
            "snapdog_query_duration_seconds",
            unit: "s",
            description: "Duration of query processing in seconds"
        );

        this._commandErrors = this._meter.CreateCounter<long>(
            "snapdog_command_errors_total",
            description: "Total number of command processing errors"
        );

        this._queryErrors = this._meter.CreateCounter<long>(
            "snapdog_query_errors_total",
            description: "Total number of query processing errors"
        );

        // System Metrics
        this._cpuUsagePercent = this._meter.CreateObservableGauge(
            "snapdog_system_cpu_usage_percent",
            observeValue: () => this._systemState.CpuUsagePercent,
            description: "Current CPU usage percentage"
        );

        this._memoryUsageMb = this._meter.CreateObservableGauge(
            "snapdog_system_memory_usage_mb",
            observeValue: () => this._systemState.MemoryUsageMb,
            description: "Current memory usage in megabytes"
        );

        this._memoryUsagePercent = this._meter.CreateObservableGauge(
            "snapdog_system_memory_usage_percent",
            observeValue: () => this._systemState.MemoryUsagePercent,
            description: "Current memory usage percentage"
        );

        this._uptimeSeconds = this._meter.CreateObservableGauge(
            "snapdog_system_uptime_seconds",
            observeValue: () => (long)(DateTime.UtcNow - _startTime).TotalSeconds,
            description: "Application uptime in seconds"
        );

        this._activeConnections = this._meter.CreateObservableGauge(
            "snapdog_system_connections_active",
            observeValue: () => this._systemState.ActiveConnections,
            description: "Number of active connections"
        );

        this._threadPoolThreads = this._meter.CreateObservableGauge(
            "snapdog_system_threadpool_threads",
            observeValue: () => this._systemState.ThreadPoolThreads,
            description: "Number of thread pool threads"
        );

        // Business Metrics
        this._zonesTotal = this._meter.CreateObservableGauge(
            "snapdog_zones_total",
            observeValue: () => this._businessState.ZonesTotal,
            description: "Total number of configured zones"
        );

        this._zonesActive = this._meter.CreateObservableGauge(
            "snapdog_zones_active",
            observeValue: () => this._businessState.ZonesActive,
            description: "Number of zones currently active"
        );

        this._clientsConnected = this._meter.CreateObservableGauge(
            "snapdog_clients_connected",
            observeValue: () => this._businessState.ClientsConnected,
            description: "Number of connected Snapcast clients"
        );

        this._tracksPlaying = this._meter.CreateObservableGauge(
            "snapdog_tracks_playing",
            observeValue: () => this._businessState.TracksPlaying,
            description: "Number of tracks currently playing"
        );

        this._trackChangesTotal = this._meter.CreateCounter<long>(
            "snapdog_track_changes_total",
            description: "Total number of track changes"
        );

        this._volumeChangesTotal = this._meter.CreateCounter<long>(
            "snapdog_volume_changes_total",
            description: "Total number of volume changes"
        );

        // Error Tracking
        this._errorsTotal = this._meter.CreateCounter<long>(
            "snapdog_errors_total",
            description: "Total number of application errors"
        );

        this._exceptionsTotal = this._meter.CreateCounter<long>(
            "snapdog_exceptions_total",
            description: "Total number of unhandled exceptions"
        );

        this.LogApplicationMetricsInitialized(this._meter.Name, this._meter.Version);
    }

    #region HTTP Metrics

    public void RecordHttpRequest(string method, string endpoint, int statusCode, double durationSeconds)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("method", method),
            new("endpoint", endpoint),
            new("status_code", statusCode.ToString()),
        };

        this._httpRequestsTotal.Add(1, tags);
        this._httpRequestDuration.Record(durationSeconds, tags);

        if (statusCode >= 400)
        {
            this._httpRequestsErrors.Add(1, tags);
        }
    }

    #endregion

    #region Command/Query Metrics

    public void RecordCommand(string commandName, double durationSeconds, bool success)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("command", commandName),
            new("success", success.ToString().ToLowerInvariant()),
        };

        this._commandsTotal.Add(1, tags);
        this._commandDuration.Record(durationSeconds, tags);

        if (!success)
        {
            this._commandErrors.Add(1, tags);
        }
    }

    public void RecordQuery(string queryName, double durationSeconds, bool success)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("query", queryName),
            new("success", success.ToString().ToLowerInvariant()),
        };

        this._queriesTotal.Add(1, tags);
        this._queryDuration.Record(durationSeconds, tags);

        if (!success)
        {
            this._queryErrors.Add(1, tags);
        }
    }

    #endregion

    #region System Metrics

    public void UpdateSystemMetrics(SystemMetricsState state)
    {
        this._systemState = state;
    }

    #endregion

    #region Business Metrics

    public void UpdateBusinessMetrics(BusinessMetricsState state)
    {
        this._businessState = state;
    }

    public void RecordTrackChange(string zoneIndex, string? fromTrack, string? toTrack)
    {
        var tags = new KeyValuePair<string, object?>[] { new("zone_id", zoneIndex) };

        this._trackChangesTotal.Add(1, tags);
    }

    public void RecordVolumeChange(string targetId, string targetType, int fromVolume, int toVolume)
    {
        var tags = new KeyValuePair<string, object?>[] { new("target_id", targetId), new("target_type", targetType) };

        this._volumeChangesTotal.Add(1, tags);
    }

    #endregion

    #region Error Tracking

    public void RecordError(string errorType, string component, string? operation = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("error_type", errorType),
            new("component", component),
        };

        if (!string.IsNullOrEmpty(operation))
        {
            tags.Add(new("operation", operation));
        }

        this._errorsTotal.Add(1, tags.ToArray());
    }

    public void RecordException(Exception exception, string component, string? operation = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("exception_type", exception.GetType().Name),
            new("component", component),
        };

        if (!string.IsNullOrEmpty(operation))
        {
            tags.Add(new("operation", operation));
        }

        this._exceptionsTotal.Add(1, tags.ToArray());
    }

    #endregion

    public void Dispose()
    {
        this._meter?.Dispose();
    }

    [LoggerMessage(
        EventId = 6700,
        Level = LogLevel.Information,
        Message = "ApplicationMetrics initialized with {MeterName} v{MeterVersion}"
    )]
    private partial void LogApplicationMetricsInitialized(string? meterName, string? meterVersion);
}

/// <summary>
/// Current system metrics state for observable gauges.
/// </summary>
public record SystemMetricsState
{
    public double CpuUsagePercent { get; init; }
    public double MemoryUsageMb { get; init; }
    public double MemoryUsagePercent { get; init; }
    public int ActiveConnections { get; init; }
    public int ThreadPoolThreads { get; init; }
}

/// <summary>
/// Current business metrics state for observable gauges.
/// </summary>
public record BusinessMetricsState
{
    public int ZonesTotal { get; init; }
    public int ZonesActive { get; init; }
    public int ClientsConnected { get; init; }
    public int TracksPlaying { get; init; }
}
