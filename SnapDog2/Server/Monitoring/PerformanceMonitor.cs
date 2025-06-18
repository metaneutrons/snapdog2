using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SnapDog2.Server.Monitoring;

/// <summary>
/// In-memory implementation of the performance monitor.
/// Tracks request performance metrics and provides alerting for slow requests.
/// </summary>
public sealed class PerformanceMonitor : IPerformanceMonitor, IDisposable
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly PerformanceMonitorOptions _options;
    private readonly ConcurrentDictionary<string, RequestMetricsData> _requestMetrics;
    private readonly ConcurrentQueue<SlowRequestAlert> _slowRequestAlerts;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
    /// </summary>
    /// <param name="options">The performance monitor options.</param>
    /// <param name="logger">The logger.</param>
    public PerformanceMonitor(IOptions<PerformanceMonitorOptions> options, ILogger<PerformanceMonitor> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestMetrics = new ConcurrentDictionary<string, RequestMetricsData>();
        _slowRequestAlerts = new ConcurrentQueue<SlowRequestAlert>();

        // Setup cleanup timer
        _cleanupTimer = new Timer(CleanupOldData, null, _options.CleanupInterval, _options.CleanupInterval);

        _logger.LogDebug("PerformanceMonitor initialized with options: {@Options}", _options);
    }

    /// <inheritdoc />
    public Task RecordExecutionTimeAsync(
        string requestName,
        long executionTime,
        bool success,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(requestName);

        var metricsData = _requestMetrics.GetOrAdd(requestName, _ => new RequestMetricsData(requestName));
        metricsData.RecordExecution(executionTime, success);

        // Check if this is a slow request
        if (IsSlowRequest(requestName, executionTime))
        {
            var threshold = GetSlowRequestThreshold(requestName);
            _ = Task.Run(
                () => RecordSlowRequestAsync(requestName, executionTime, threshold, cancellationToken),
                cancellationToken
            );
        }

        _logger.LogTrace(
            "Recorded execution time for {RequestName}: {ExecutionTime}ms (Success: {Success})",
            requestName,
            executionTime,
            success
        );

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<PerformanceMetrics?> GetMetricsAsync(string requestName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(requestName);

        if (_requestMetrics.TryGetValue(requestName, out var metricsData))
        {
            var metrics = metricsData.ToPerformanceMetrics();
            return Task.FromResult<PerformanceMetrics?>(metrics);
        }

        return Task.FromResult<PerformanceMetrics?>(null);
    }

    /// <inheritdoc />
    public Task<IEnumerable<PerformanceMetrics>> GetAllMetricsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var allMetrics = _requestMetrics
            .Values.Select(data => data.ToPerformanceMetrics())
            .OrderBy(m => m.RequestName)
            .ToList();

        return Task.FromResult<IEnumerable<PerformanceMetrics>>(allMetrics);
    }

    /// <inheritdoc />
    public bool IsSlowRequest(string requestName, long executionTime)
    {
        var threshold = GetSlowRequestThreshold(requestName);
        return executionTime > threshold;
    }

    /// <inheritdoc />
    public long GetSlowRequestThreshold(string requestName)
    {
        // Get request-specific threshold or default
        return requestName.ToLowerInvariant() switch
        {
            var name when name.Contains("systemstatus") => _options.SystemStatusThreshold,
            var name when name.Contains("audiostream") => _options.AudioStreamThreshold,
            var name when name.Contains("client") => _options.ClientThreshold,
            var name when name.Contains("zone") => _options.ZoneThreshold,
            var name when name.Contains("playlist") => _options.PlaylistThreshold,
            var name when name.Contains("radiostation") => _options.RadioStationThreshold,
            var name when name.Contains("command") => _options.CommandThreshold,
            var name when name.Contains("query") => _options.QueryThreshold,
            _ => _options.DefaultThreshold,
        };
    }

    /// <inheritdoc />
    public Task RecordSlowRequestAsync(
        string requestName,
        long executionTime,
        long threshold,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        var alert = new SlowRequestAlert
        {
            RequestName = requestName,
            ExecutionTime = executionTime,
            Threshold = threshold,
            Timestamp = DateTime.UtcNow,
        };

        _slowRequestAlerts.Enqueue(alert);

        // Limit the number of stored alerts
        while (_slowRequestAlerts.Count > _options.MaxSlowRequestAlerts)
        {
            _slowRequestAlerts.TryDequeue(out _);
        }

        _logger.LogWarning(
            "Slow request detected: {RequestName} took {ExecutionTime}ms (threshold: {Threshold}ms)",
            requestName,
            executionTime,
            threshold
        );

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<SlowRequestAlert>> GetRecentSlowRequestsAsync(
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        var cutoffTime = DateTime.UtcNow - timeWindow;
        var recentAlerts = _slowRequestAlerts
            .Where(alert => alert.Timestamp >= cutoffTime)
            .OrderByDescending(alert => alert.Timestamp)
            .ToList();

        return Task.FromResult<IEnumerable<SlowRequestAlert>>(recentAlerts);
    }

    /// <inheritdoc />
    public Task CleanupOldMetricsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var cutoffTime = DateTime.UtcNow - maxAge;
        var removedCount = 0;

        foreach (var kvp in _requestMetrics.ToList())
        {
            if (kvp.Value.LastRequestAt < cutoffTime)
            {
                if (_requestMetrics.TryRemove(kvp.Key, out _))
                {
                    removedCount++;
                }
            }
        }

        if (removedCount > 0)
        {
            _logger.LogDebug("Cleaned up {Count} old performance metrics", removedCount);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SystemPerformanceStats> GetSystemStatsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var allMetrics = _requestMetrics.Values.ToList();

        var stats = new SystemPerformanceStats
        {
            TotalRequests = allMetrics.Sum(m => m.TotalRequests),
            SuccessfulRequests = allMetrics.Sum(m => m.SuccessfulRequests),
            AverageExecutionTime = allMetrics.Count > 0 ? allMetrics.Average(m => m.AverageExecutionTime) : 0.0,
            MonitoredRequestTypes = allMetrics.Count,
            TotalSlowRequests = allMetrics.Sum(m => m.SlowRequests),
            GeneratedAt = DateTime.UtcNow,
            TimeWindow = _options.MetricsRetentionPeriod,
        };

        return Task.FromResult(stats);
    }

    /// <summary>
    /// Cleans up old data periodically.
    /// </summary>
    private void CleanupOldData(object? state)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            // Clean up old metrics
            _ = Task.Run(() => CleanupOldMetricsAsync(_options.MetricsRetentionPeriod));

            // Clean up old alerts
            var alertCutoffTime = DateTime.UtcNow - _options.AlertRetentionPeriod;
            var tempAlerts = new List<SlowRequestAlert>();

            while (_slowRequestAlerts.TryDequeue(out var alert))
            {
                if (alert.Timestamp >= alertCutoffTime)
                {
                    tempAlerts.Add(alert);
                }
            }

            // Re-enqueue recent alerts
            foreach (var alert in tempAlerts)
            {
                _slowRequestAlerts.Enqueue(alert);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during performance monitor cleanup");
        }
    }

    /// <summary>
    /// Throws an exception if the monitor has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PerformanceMonitor));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer?.Dispose();
            _disposed = true;

            _logger.LogDebug("PerformanceMonitor disposed");
        }
    }
}

/// <summary>
/// Thread-safe container for request metrics data.
/// </summary>
internal sealed class RequestMetricsData
{
    private readonly object _lock = new();
    private readonly List<long> _executionTimes = new();
    private long _totalRequests;
    private long _successfulRequests;
    private long _slowRequests;
    private long _minExecutionTime = long.MaxValue;
    private long _maxExecutionTime;
    private DateTime _firstRequestAt = DateTime.MaxValue;
    private DateTime _lastRequestAt = DateTime.MinValue;

    public string RequestName { get; }
    public long TotalRequests => _totalRequests;
    public long SuccessfulRequests => _successfulRequests;
    public long SlowRequests => _slowRequests;
    public DateTime FirstRequestAt => _firstRequestAt == DateTime.MaxValue ? DateTime.UtcNow : _firstRequestAt;
    public DateTime LastRequestAt => _lastRequestAt == DateTime.MinValue ? DateTime.UtcNow : _lastRequestAt;
    public double AverageExecutionTime
    {
        get
        {
            lock (_lock)
            {
                return _executionTimes.Count > 0 ? _executionTimes.Average() : 0.0;
            }
        }
    }

    public RequestMetricsData(string requestName)
    {
        RequestName = requestName ?? throw new ArgumentNullException(nameof(requestName));
    }

    public void RecordExecution(long executionTime, bool success)
    {
        lock (_lock)
        {
            _totalRequests++;
            if (success)
            {
                _successfulRequests++;
            }

            _executionTimes.Add(executionTime);

            // Update min/max
            if (executionTime < _minExecutionTime)
            {
                _minExecutionTime = executionTime;
            }
            if (executionTime > _maxExecutionTime)
            {
                _maxExecutionTime = executionTime;
            }

            // Update timestamps
            var now = DateTime.UtcNow;
            if (_firstRequestAt == DateTime.MaxValue)
            {
                _firstRequestAt = now;
            }
            _lastRequestAt = now;

            // Limit stored execution times to prevent memory growth
            if (_executionTimes.Count > 10000)
            {
                _executionTimes.RemoveAt(0);
            }
        }
    }

    public void RecordSlowRequest()
    {
        Interlocked.Increment(ref _slowRequests);
    }

    public PerformanceMetrics ToPerformanceMetrics()
    {
        lock (_lock)
        {
            var sortedTimes = _executionTimes.OrderBy(t => t).ToList();

            return new PerformanceMetrics
            {
                RequestName = RequestName,
                TotalRequests = _totalRequests,
                SuccessfulRequests = _successfulRequests,
                FailedRequests = _totalRequests - _successfulRequests,
                AverageExecutionTime = AverageExecutionTime,
                MinExecutionTime = _minExecutionTime == long.MaxValue ? 0 : _minExecutionTime,
                MaxExecutionTime = _maxExecutionTime,
                P95ExecutionTime = CalculatePercentile(sortedTimes, 0.95),
                P99ExecutionTime = CalculatePercentile(sortedTimes, 0.99),
                SlowRequests = _slowRequests,
                FirstRequestAt = FirstRequestAt,
                LastRequestAt = LastRequestAt,
            };
        }
    }

    private static long CalculatePercentile(List<long> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(sortedValues.Count * percentile) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }
}

/// <summary>
/// Configuration options for the performance monitor.
/// </summary>
public sealed class PerformanceMonitorOptions
{
    /// <summary>
    /// Gets or sets the default threshold for slow requests in milliseconds.
    /// </summary>
    public long DefaultThreshold { get; set; } = 1000; // 1 second

    /// <summary>
    /// Gets or sets the threshold for system status requests in milliseconds.
    /// </summary>
    public long SystemStatusThreshold { get; set; } = 500; // 500ms

    /// <summary>
    /// Gets or sets the threshold for audio stream requests in milliseconds.
    /// </summary>
    public long AudioStreamThreshold { get; set; } = 2000; // 2 seconds

    /// <summary>
    /// Gets or sets the threshold for client requests in milliseconds.
    /// </summary>
    public long ClientThreshold { get; set; } = 1000; // 1 second

    /// <summary>
    /// Gets or sets the threshold for zone requests in milliseconds.
    /// </summary>
    public long ZoneThreshold { get; set; } = 1500; // 1.5 seconds

    /// <summary>
    /// Gets or sets the threshold for playlist requests in milliseconds.
    /// </summary>
    public long PlaylistThreshold { get; set; } = 1000; // 1 second

    /// <summary>
    /// Gets or sets the threshold for radio station requests in milliseconds.
    /// </summary>
    public long RadioStationThreshold { get; set; } = 3000; // 3 seconds

    /// <summary>
    /// Gets or sets the threshold for command requests in milliseconds.
    /// </summary>
    public long CommandThreshold { get; set; } = 2000; // 2 seconds

    /// <summary>
    /// Gets or sets the threshold for query requests in milliseconds.
    /// </summary>
    public long QueryThreshold { get; set; } = 1000; // 1 second

    /// <summary>
    /// Gets or sets the maximum number of slow request alerts to keep in memory.
    /// </summary>
    public int MaxSlowRequestAlerts { get; set; } = 1000;

    /// <summary>
    /// Gets or sets how long to retain performance metrics.
    /// </summary>
    public TimeSpan MetricsRetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets how long to retain slow request alerts.
    /// </summary>
    public TimeSpan AlertRetentionPeriod { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Gets or sets the interval for cleanup operations.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
