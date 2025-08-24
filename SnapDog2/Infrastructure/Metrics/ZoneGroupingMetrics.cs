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

/// <summary>
/// OpenTelemetry metrics for zone grouping operations.
/// Provides comprehensive monitoring of zone grouping health and performance.
/// </summary>
public class ZoneGroupingMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _reconciliationCounter;
    private readonly Histogram<double> _reconciliationDuration;
    private readonly Counter<long> _clientUpdatesCounter;
    private readonly Counter<long> _errorsCounter;

    public ZoneGroupingMetrics()
    {
        _meter = new Meter("SnapDog2.ZoneGrouping", "1.0.0");

        // Counters for events
        _reconciliationCounter = _meter.CreateCounter<long>(
            "zone_grouping_reconciliations_total",
            description: "Total number of zone grouping reconciliations performed"
        );

        _clientUpdatesCounter = _meter.CreateCounter<long>(
            "zone_grouping_client_updates_total",
            description: "Total number of client name updates performed"
        );

        _errorsCounter = _meter.CreateCounter<long>(
            "zone_grouping_errors_total",
            description: "Total number of zone grouping errors encountered"
        );

        // Histogram for performance
        _reconciliationDuration = _meter.CreateHistogram<double>(
            "zone_grouping_reconciliation_duration_seconds",
            unit: "s",
            description: "Duration of zone grouping reconciliation operations"
        );
    }

    /// <summary>
    /// Records a reconciliation operation with its results.
    /// </summary>
    /// <param name="durationSeconds">Duration of the reconciliation in seconds</param>
    /// <param name="success">Whether the reconciliation was successful</param>
    /// <param name="clientUpdates">Number of client updates performed</param>
    /// <param name="errorType">Type of error if unsuccessful (optional)</param>
    public void RecordReconciliation(
        double durationSeconds,
        bool success,
        int clientUpdates = 0,
        string? errorType = null
    )
    {
        var tags = new KeyValuePair<string, object?>[] { new("success", success.ToString().ToLowerInvariant()) };

        _reconciliationCounter.Add(1, tags);
        _reconciliationDuration.Record(durationSeconds, tags);

        if (clientUpdates > 0)
        {
            _clientUpdatesCounter.Add(clientUpdates);
        }

        if (!success && !string.IsNullOrEmpty(errorType))
        {
            _errorsCounter.Add(1, new KeyValuePair<string, object?>[] { new("error_type", errorType) });
        }
    }

    /// <summary>
    /// Records an error in zone grouping operations.
    /// </summary>
    /// <param name="errorType">Type of error encountered</param>
    /// <param name="operation">Operation where error occurred</param>
    public void RecordError(string errorType, string operation)
    {
        _errorsCounter.Add(
            1,
            new KeyValuePair<string, object?>[] { new("error_type", errorType), new("operation", operation) }
        );
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}
