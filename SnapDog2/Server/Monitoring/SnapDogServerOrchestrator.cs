using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Infrastructure.Services;

namespace SnapDog2.Server.Monitoring
{
    /// <summary>
    /// Production orchestrator for coordinating KNX, MQTT, and Snapcast infrastructure services.
    /// Manages lifecycle, health monitoring, and reconnection logic.
    /// </summary>
    public class SnapDogServerOrchestrator : BackgroundService
    {
        private readonly ILogger<SnapDogServerOrchestrator> _logger;
        private readonly IKnxService _knxService;
        private readonly IMqttService _mqttService;
        private readonly ISnapcastService _snapcastService;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromSeconds(10);

        public SnapDogServerOrchestrator(
            ILogger<SnapDogServerOrchestrator> logger,
            IKnxService knxService,
            IMqttService mqttService,
            ISnapcastService snapcastService
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
            _snapcastService = snapcastService ?? throw new ArgumentNullException(nameof(snapcastService));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SnapDogServerOrchestrator starting");

            // Initial connection attempts
            await EnsureConnectedAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorAndReconnectAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during orchestration loop");
                }

                await Task.Delay(_healthCheckInterval, stoppingToken);
            }

            _logger.LogInformation("SnapDogServerOrchestrator stopping");
        }

        private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            // KNX
            if (!await _knxService.ConnectAsync(cancellationToken))
            {
                _logger.LogWarning("KNX service failed to connect on startup");
            }

            // MQTT
            if (!await _mqttService.ConnectAsync(cancellationToken))
            {
                _logger.LogWarning("MQTT service failed to connect on startup");
            }

            // Snapcast: no explicit connect, but check health
            if (!await _snapcastService.IsServerAvailableAsync(cancellationToken))
            {
                _logger.LogWarning("Snapcast server unavailable on startup");
            }
        }

        private async Task MonitorAndReconnectAsync(CancellationToken cancellationToken)
        {
            // KNX health: try a simple operation (could be extended)
            try
            {
                // If needed, add a real health check method to IKnxService
                // Here, try a dummy read (could be improved)
                await _knxService.ReadGroupValueAsync(null, cancellationToken);
            }
            catch
            {
                _logger.LogWarning("KNX service unhealthy, attempting reconnect...");
                await _knxService.DisconnectAsync(cancellationToken);
                await _knxService.ConnectAsync(cancellationToken);
            }

            // MQTT health: try a simple publish to a test topic (could be improved)
            try
            {
                await _mqttService.PublishAsync("snapdog/healthcheck", "ping", cancellationToken);
            }
            catch
            {
                _logger.LogWarning("MQTT service unhealthy, attempting reconnect...");
                await _mqttService.DisconnectAsync(cancellationToken);
                await _mqttService.ConnectAsync(cancellationToken);
            }

            // Snapcast health
            if (!await _snapcastService.IsServerAvailableAsync(cancellationToken))
            {
                _logger.LogWarning("Snapcast server unavailable, will retry...");
                // No explicit reconnect, just log and retry next interval
            }
        }
    }
}
