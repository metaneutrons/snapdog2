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
namespace SnapDog2.Server.Behaviors;

using System.Diagnostics;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Application;

/// <summary>
/// Enhanced pipeline behavior that measures command execution performance and records metrics.
/// Replaces the basic PerformanceCommandBehavior with enterprise-grade metrics collection.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public partial class PerformanceCommandBehavior<TCommand, TResponse>(
    ILogger<PerformanceCommandBehavior<TCommand, TResponse>> logger,
    EnterpriseMetricsService metricsService
) : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<PerformanceCommandBehavior<TCommand, TResponse>> _logger = logger;
    private readonly EnterpriseMetricsService _metricsService = metricsService;
    private const int SlowOperationThresholdMs = 500;

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            success = response.IsSuccess;
            var durationSeconds = stopwatch.ElapsedMilliseconds / 1000.0;

            // Record metrics
            _metricsService.RecordCortexMediatorRequestDuration(
                "Command",
                commandName,
                stopwatch.ElapsedMilliseconds,
                success
            );

            // Log slow operations
            if (stopwatch.ElapsedMilliseconds > SlowOperationThresholdMs)
            {
                this.LogSlowCommand(commandName, stopwatch.ElapsedMilliseconds);
            }

            // Record specific command metrics based on command type
            RecordCommandSpecificMetrics(command, response, durationSeconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            success = false;

            // Record error metrics
            _metricsService.RecordCortexMediatorRequestDuration(
                "Command",
                commandName,
                stopwatch.ElapsedMilliseconds,
                false
            );
            _metricsService.RecordException(ex, "CommandPipeline", commandName);

            this.LogCommandException(commandName, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    /// <summary>
    /// Records command-specific metrics based on the command type.
    /// </summary>
    private void RecordCommandSpecificMetrics(TCommand command, TResponse response, double durationSeconds)
    {
        try
        {
            var commandName = typeof(TCommand).Name;

            // Track volume changes
            if (commandName.Contains("Volume", StringComparison.OrdinalIgnoreCase))
            {
                // Extract zone/client info if available through reflection or known patterns
                var targetId = ExtractTargetId(command);
                var targetType = commandName.Contains("Zone") ? "zone" : "client";

                if (!string.IsNullOrEmpty(targetId))
                {
                    // For volume commands, we'd need the old/new values
                    // This is a simplified version - in practice you'd extract actual values
                    _metricsService.RecordVolumeChange(targetId, targetType, 0, 0);
                }
            }

            // Track track changes
            if (
                commandName.Contains("Track", StringComparison.OrdinalIgnoreCase)
                || commandName.Contains("Play", StringComparison.OrdinalIgnoreCase)
            )
            {
                var zoneId = ExtractZoneId(command);
                if (!string.IsNullOrEmpty(zoneId))
                {
                    _metricsService.RecordTrackChange(zoneId, null, null);
                }
            }
        }
        catch (Exception ex)
        {
            // Don't let metrics recording failures affect command execution
            LogMetricsRecordingFailed(_logger, ex, typeof(TCommand).Name);
        }
    }

    /// <summary>
    /// Extracts target ID from command using reflection or known patterns.
    /// </summary>
    private static string? ExtractTargetId(TCommand command)
    {
        try
        {
            // Try to get ZoneIndex property
            var zoneIndexProperty = typeof(TCommand).GetProperty("ZoneIndex");
            if (zoneIndexProperty != null)
            {
                var zoneIndex = zoneIndexProperty.GetValue(command);
                return zoneIndex?.ToString();
            }

            // Try to get ClientId property
            var clientIdProperty = typeof(TCommand).GetProperty("ClientId");
            if (clientIdProperty != null)
            {
                var clientId = clientIdProperty.GetValue(command);
                return clientId?.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts zone ID from command using reflection or known patterns.
    /// </summary>
    private static string? ExtractZoneId(TCommand command)
    {
        try
        {
            var zoneIndexProperty = typeof(TCommand).GetProperty("ZoneIndex");
            if (zoneIndexProperty != null)
            {
                var zoneIndex = zoneIndexProperty.GetValue(command);
                return zoneIndex?.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    [LoggerMessage(
        EventId = 7000,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Slow command detected: {CommandName} took {ElapsedMilliseconds}ms"
    )]
    private partial void LogSlowCommand(string commandName, long elapsedMilliseconds);

    [LoggerMessage(
        EventId = 7001,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Command {CommandName} threw exception after {ElapsedMilliseconds}ms"
    )]
    private partial void LogCommandException(string commandName, long elapsedMilliseconds, Exception ex);

    [LoggerMessage(
        EventId = 7002,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Failed to record command-specific metrics for {CommandName}"
    )]
    private static partial void LogMetricsRecordingFailed(ILogger logger, Exception ex, string commandName);
}
