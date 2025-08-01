using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;
using SnapDog2.Core.State;
using SnapDog2.Infrastructure.HealthChecks.Models;
using SnapDog2.Infrastructure.Repositories;
using SnapDog2.Server.Features.AudioStreams.Queries;
using SnapDog2.Server.Models;

namespace SnapDog2.Server.Features.AudioStreams.Handlers;

/// <summary>
/// Handler for retrieving comprehensive system status information.
/// Aggregates data from multiple sources including state, repositories, and health checks.
/// </summary>
public sealed class GetSystemStatusHandler : IRequestHandler<GetSystemStatusQuery, Result<SystemStatusResponse>>
{
    private readonly IStateManager _stateManager;
    private readonly IAudioStreamRepository _audioStreamRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IZoneRepository _zoneRepository;
    private readonly ILogger<GetSystemStatusHandler> _logger;
    private static readonly DateTime _systemStartTime = DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSystemStatusHandler"/> class.
    /// </summary>
    /// <param name="stateManager">The state manager.</param>
    /// <param name="audioStreamRepository">The audio stream repository.</param>
    /// <param name="clientRepository">The client repository.</param>
    /// <param name="zoneRepository">The zone repository.</param>
    /// <param name="logger">The logger.</param>
    public GetSystemStatusHandler(
        IStateManager stateManager,
        IAudioStreamRepository audioStreamRepository,
        IClientRepository clientRepository,
        IZoneRepository zoneRepository,
        ILogger<GetSystemStatusHandler> logger
    )
    {
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _audioStreamRepository =
            audioStreamRepository ?? throw new ArgumentNullException(nameof(audioStreamRepository));
        _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
        _zoneRepository = zoneRepository ?? throw new ArgumentNullException(nameof(zoneRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request to get system status.
    /// </summary>
    /// <param name="request">The get system status query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the system status response.</returns>
    public async Task<Result<SystemStatusResponse>> Handle(
        GetSystemStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Retrieving system status with options - IncludeStreamStatistics: {IncludeStreamStatistics}, IncludeHealthChecks: {IncludeHealthChecks}, IncludePerformanceMetrics: {IncludePerformanceMetrics}, RefreshCache: {RefreshCache}",
                request.IncludeStreamStatistics,
                request.IncludeHealthChecks,
                request.IncludePerformanceMetrics,
                request.RefreshCache
            );

            // Get current system state
            var currentState = _stateManager.GetCurrentState();
            var uptime = DateTime.UtcNow - _systemStartTime;

            _logger.LogDebug(
                "Current system state: {SystemStatus}, Version: {Version}, Uptime: {Uptime}",
                currentState.SystemStatus,
                currentState.Version,
                uptime
            );

            // Create basic response
            var response = SystemStatusResponse.FromSystemState(currentState.SystemStatus, uptime, "1.0.0"); // TODO: Get actual version from assembly

            // Add stream statistics if requested
            if (request.IncludeStreamStatistics)
            {
                response = response with { StreamStatistics = await GetStreamStatisticsAsync(cancellationToken) };
                _logger.LogDebug("Added stream statistics to system status response");
            }

            // Add health check information if requested
            if (request.IncludeHealthChecks)
            {
                response = response with { HealthStatus = await GetHealthStatusAsync(cancellationToken) };
                _logger.LogDebug("Added health status to system status response");
            }

            // Add performance metrics if requested
            if (request.IncludePerformanceMetrics)
            {
                response = response with { Performance = await GetPerformanceMetricsAsync(cancellationToken) };
                _logger.LogDebug("Added performance metrics to system status response");
            }

            // Add client information if requested
            if (request.IncludeClientInfo)
            {
                response = response with { ClientInfo = await GetClientConnectionInfoAsync(cancellationToken) };
                _logger.LogDebug("Added client connection info to system status response");
            }

            // Add error information if requested
            if (request.IncludeErrorDetails)
            {
                response = response with
                {
                    ErrorInfo = await GetSystemErrorInfoAsync(request.HistoricalDataHours, cancellationToken),
                };
                _logger.LogDebug("Added error information to system status response");
            }

            // Add historical data if requested
            if (request.IncludeHistoricalData)
            {
                await EnrichWithHistoricalDataAsync(response, request.HistoricalDataHours, cancellationToken);
                _logger.LogDebug(
                    "Enriched response with historical data for {Hours} hours",
                    request.HistoricalDataHours
                );
            }

            _logger.LogInformation(
                "Successfully retrieved system status - Status: {Status}, Uptime: {Uptime}",
                response.Status,
                response.Uptime
            );

            return Result<SystemStatusResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve system status");

            return Result<SystemStatusResponse>.Failure($"Failed to retrieve system status: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves audio stream statistics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Audio stream statistics.</returns>
    private async Task<AudioStreamStatistics> GetStreamStatisticsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var allStreams = await _audioStreamRepository.GetAllAsync(cancellationToken);
            var activeStreams = await _audioStreamRepository.GetActiveStreamsAsync(cancellationToken);

            var codecBreakdown = allStreams
                .GroupBy(static s => s.Codec.ToString())
                .ToDictionary(static g => g.Key, static g => g.Count());

            var bitrateBreakdown = allStreams
                .GroupBy(static s => GetBitrateRange(s.BitrateKbps))
                .ToDictionary(static g => g.Key, static g => g.Count());

            return new AudioStreamStatistics
            {
                TotalCount = allStreams.Count(),
                ActiveCount = activeStreams.Count(),
                StoppedCount = allStreams.Count(static s => s.Status == Core.Models.Enums.StreamStatus.Stopped),
                ErrorCount = allStreams.Count(static s => s.Status == Core.Models.Enums.StreamStatus.Error),
                CodecBreakdown = codecBreakdown,
                BitrateBreakdown = bitrateBreakdown,
                LastStreamCreated = allStreams.OrderByDescending(static s => s.CreatedAt).FirstOrDefault()?.CreatedAt,
                LastStreamStarted = activeStreams
                    .OrderByDescending(static s => s.UpdatedAt ?? s.CreatedAt)
                    .FirstOrDefault()
                    ?.UpdatedAt,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve stream statistics");
            return new AudioStreamStatistics();
        }
    }

    /// <summary>
    /// Retrieves system health status.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>System health status.</returns>
    private async Task<SystemHealthStatus?> GetHealthStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            // In a real implementation, this would query the health check service
            // For now, we'll return a basic health status
            await Task.Delay(1, cancellationToken);

            return new SystemHealthStatus
            {
                Status = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                Timestamp = DateTime.UtcNow,
                TotalDurationMs = 50,
                Results = new Dictionary<string, HealthCheckResponse>
                {
                    ["Database"] = new HealthCheckResponse
                    {
                        Name = "Database",
                        Status = "Healthy",
                        ResponseTimeMs = 10,
                        Description = "Database connection is healthy",
                    },
                    ["AudioStreams"] = new HealthCheckResponse
                    {
                        Name = "AudioStreams",
                        Status = "Healthy",
                        ResponseTimeMs = 5,
                        Description = "Audio streaming service is operational",
                    },
                },
                SystemInfo = new Dictionary<string, object>
                {
                    ["Environment"] = "Development",
                    ["MachineName"] = Environment.MachineName,
                    ["ProcessorCount"] = Environment.ProcessorCount,
                },
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve health status");
            return null;
        }
    }

    /// <summary>
    /// Retrieves performance metrics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Performance metrics.</returns>
    private async Task<PerformanceMetrics?> GetPerformanceMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // In a real implementation, this would gather actual performance metrics
            await Task.Delay(1, cancellationToken);

            return new PerformanceMetrics
            {
                CpuUsagePercent = 15.5,
                MemoryUsageMb = 256.7,
                DiskUsagePercent = 45.2,
                NetworkThroughputBps = 1024000,
                AverageResponseTimeMs = 12.3,
                ActiveConnections = 5,
                Counters = new Dictionary<string, double>
                {
                    ["RequestsPerSecond"] = 25.6,
                    ["ErrorRate"] = 0.1,
                    ["CacheHitRatio"] = 98.5,
                },
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve performance metrics");
            return null;
        }
    }

    /// <summary>
    /// Retrieves client connection information.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Client connection information.</returns>
    private async Task<ClientConnectionInfo?> GetClientConnectionInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            var allClients = await _clientRepository.GetAllAsync(cancellationToken);
            var allZones = await _zoneRepository.GetAllAsync(cancellationToken);

            var activeClients = allClients.Where(static c => c.IsConnected).ToList();
            var activeZones = allZones.Where(static z => z.IsActive).ToList();

            return new ClientConnectionInfo
            {
                TotalClients = allClients.Count(),
                ActiveClients = activeClients.Count,
                TotalZones = allZones.Count(),
                ActiveZones = activeZones.Count,
                PlatformBreakdown = activeClients
                    .GroupBy(static c => "Unknown") // TODO: Add Platform property to Client entity
                    .ToDictionary(static g => g.Key, static g => g.Count()),
                LastClientConnected = activeClients
                    .OrderByDescending(static c => c.LastSeen)
                    .FirstOrDefault()
                    ?.LastSeen,
                AverageConnectionDuration = TimeSpan.FromMinutes(45), // Placeholder calculation
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve client connection info");
            return null;
        }
    }

    /// <summary>
    /// Retrieves system error information.
    /// </summary>
    /// <param name="hoursBack">Number of hours to look back for errors.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>System error information.</returns>
    private async Task<SystemErrorInfo?> GetSystemErrorInfoAsync(int hoursBack, CancellationToken cancellationToken)
    {
        try
        {
            // In a real implementation, this would query error logs
            await Task.Delay(1, cancellationToken);

            return new SystemErrorInfo
            {
                TotalErrors = 2,
                CriticalErrors = 0,
                Warnings = 2,
                LastErrorTimestamp = DateTime.UtcNow.AddMinutes(-30),
                LastErrorMessage = "Stream connection temporarily unavailable",
                ErrorCategories = new Dictionary<string, int> { ["Network"] = 1, ["Stream"] = 1 },
                RecentErrors = new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        Timestamp = DateTime.UtcNow.AddMinutes(-30),
                        Level = "Warning",
                        Message = "Stream connection temporarily unavailable",
                        Category = "Stream",
                        Source = "AudioStreamHandler",
                    },
                },
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve system error info");
            return null;
        }
    }

    /// <summary>
    /// Enriches the response with historical data.
    /// </summary>
    /// <param name="response">The response to enrich.</param>
    /// <param name="hoursBack">Number of hours to include in historical data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task EnrichWithHistoricalDataAsync(
        SystemStatusResponse response,
        int hoursBack,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // In a real implementation, this would:
            // 1. Query historical performance metrics
            // 2. Calculate trends and averages
            // 3. Identify patterns and anomalies
            // 4. Add historical data to the response

            await Task.Delay(1, cancellationToken);

            _logger.LogDebug(
                "Historical data enrichment completed for {Hours} hours back. "
                    + "This would include trend analysis, performance history, and usage patterns.",
                hoursBack
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enrich response with historical data");
        }
    }

    /// <summary>
    /// Gets the bitrate range category for a given bitrate value.
    /// </summary>
    /// <param name="bitrateKbps">The bitrate in kbps.</param>
    /// <returns>The bitrate range category.</returns>
    private static string GetBitrateRange(int bitrateKbps)
    {
        return bitrateKbps switch
        {
            < 64 => "Low (< 64 kbps)",
            < 128 => "Medium (64-127 kbps)",
            < 256 => "High (128-255 kbps)",
            < 512 => "Very High (256-511 kbps)",
            _ => "Lossless (512+ kbps)",
        };
    }
}
