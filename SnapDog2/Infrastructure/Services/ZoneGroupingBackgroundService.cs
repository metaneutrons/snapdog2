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

namespace SnapDog2.Infrastructure.Services;

using System.Diagnostics;
using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Metrics;
using SnapDog2.Shared.Configuration;

/// <summary>
/// Simple background service that runs periodic zone grouping checks.
/// </summary>
public class ZoneGroupingBackgroundService : BackgroundService
{
    private readonly ILogger<ZoneGroupingBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ZoneGroupingMetrics _metrics;
    private readonly TimeSpan _reconciliationInterval;

    public ZoneGroupingBackgroundService(
        ILogger<ZoneGroupingBackgroundService> logger,
        IServiceProvider serviceProvider,
        ZoneGroupingMetrics metrics,
        IOptions<SnapcastConfig> config
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _reconciliationInterval = TimeSpan.FromMilliseconds(config.Value.ZoneGroupingIntervalMs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 Zone grouping service starting with interval {IntervalMs}ms", _reconciliationInterval.TotalMilliseconds);

        // Wait for services to be ready
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Start periodic checks
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformPeriodicCheckAsync(stoppingToken);
                await Task.Delay(_reconciliationInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("⏹️ Zone grouping service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error during periodic zone grouping check");
                await Task.Delay(_reconciliationInterval, stoppingToken);
            }
        }
    }

    private async Task PerformPeriodicCheckAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var zoneGroupingService = scope.ServiceProvider.GetRequiredService<IZoneGroupingService>();

            var result = await zoneGroupingService.EnsureZoneGroupingAsync(cancellationToken);

            if (result.IsSuccess)
            {
                _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, true);
                _logger.LogDebug("✅ Zone grouping check completed successfully");
            }
            else
            {
                _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, false);
                _logger.LogWarning("⚠️ Zone grouping check failed");
            }
        }
        catch (Exception ex)
        {
            _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, false);
            _logger.LogError(ex, "💥 Error during periodic zone grouping check");
        }
    }
}
