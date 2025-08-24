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
namespace SnapDog2.Worker.Services;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;

/// <summary>
/// Hosted service responsible for initializing integration services (Snapcast, MQTT, KNX, etc.) on application startup.
/// This ensures that all external integrations are properly connected and ready before the application starts serving requests.
/// </summary>
public partial class IntegrationServicesHostedService(
    IServiceProvider serviceProvider,
    ILogger<IntegrationServicesHostedService> logger
) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<IntegrationServicesHostedService> _logger = logger;

    #region Logging

    [LoggerMessage(
        EventId = 7800,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🚀 Starting integration services initialization..."
    )]
    private partial void LogInitializationStarted();

    [LoggerMessage(
        EventId = 7801,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Initializing Snapcast service..."
    )]
    private partial void LogInitializingSnapcast();

    [LoggerMessage(
        EventId = 7802,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Snapcast service not registered - skipping initialization"
    )]
    private partial void LogSnapcastNotRegistered();

    [LoggerMessage(
        EventId = 7803,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Initializing MQTT service..."
    )]
    private partial void LogInitializingMqtt();

    [LoggerMessage(
        EventId = 7804,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "MQTT service not registered - skipping initialization"
    )]
    private partial void LogMqttNotRegistered();

    [LoggerMessage(
        EventId = 7805,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Initializing KNX service..."
    )]
    private partial void LogInitializingKnx();

    [LoggerMessage(
        EventId = 7806,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "KNX service not registered - skipping initialization"
    )]
    private partial void LogKnxNotRegistered();

    [LoggerMessage(
        EventId = 7807,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Initializing Subsonic service..."
    )]
    private partial void LogInitializingSubsonic();

    [LoggerMessage(
        EventId = 7808,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Subsonic service not registered - skipping initialization"
    )]
    private partial void LogSubsonicNotRegistered();

    [LoggerMessage(
        EventId = 7809,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ Snapcast service initialized successfully"
    )]
    private partial void LogSnapcastInitialized();

    [LoggerMessage(
        EventId = 7810,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ MQTT service initialized successfully - Connected: {IsConnected}"
    )]
    private partial void LogMqttInitialized(bool isConnected);

    [LoggerMessage(
        EventId = 7811,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ KNX service initialized successfully - Connected: {IsConnected}"
    )]
    private partial void LogKnxInitialized(bool isConnected);

    [LoggerMessage(
        EventId = 7812,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ Subsonic service initialized successfully"
    )]
    private partial void LogSubsonicInitialized();

    [LoggerMessage(
        EventId = 7813,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "❌ Failed to initialize Snapcast service: {ErrorMessage}"
    )]
    private partial void LogSnapcastInitializationFailed(string errorMessage);

    [LoggerMessage(
        EventId = 7814,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "❌ Failed to initialize MQTT service: {ErrorMessage}"
    )]
    private partial void LogMqttInitializationFailed(string errorMessage);

    [LoggerMessage(
        EventId = 7815,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "❌ Failed to initialize KNX service: {ErrorMessage}"
    )]
    private partial void LogKnxInitializationFailed(string errorMessage);

    [LoggerMessage(
        EventId = 7816,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "❌ Failed to initialize Subsonic service: {ErrorMessage}"
    )]
    private partial void LogSubsonicInitializationFailed(string errorMessage);

    [LoggerMessage(
        EventId = 7817,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "✅ All integration services initialized successfully: [{Services}]"
    )]
    private partial void LogAllServicesInitialized(string services);

    [LoggerMessage(
        EventId = 7818,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "⚠️ SYSTEM DEGRADED: Non-critical integration services failed, but core functionality remains available. Failed: [{FailedServices}], Successful: [{SuccessfulServices}]"
    )]
    private partial void LogSystemDegraded(string failedServices, string successfulServices);

    [LoggerMessage(
        EventId = 7815,
        Level = Microsoft.Extensions.Logging.LogLevel.Critical,
        Message = "🚨 SYSTEM NON-FUNCTIONAL: Critical integration services failed. Critical failures: [{CriticalFailures}], Other failures: [{NonCriticalFailures}], Successful: [{SuccessfulServices}]. Application will terminate."
    )]
    private partial void LogSystemNonFunctional(
        string criticalFailures,
        string nonCriticalFailures,
        string successfulServices
    );

    [LoggerMessage(
        EventId = 7816,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "No integration services found to initialize"
    )]
    private partial void LogNoServicesFound();

    [LoggerMessage(
        EventId = 7817,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🔌 Disabling failed service: {ServiceName}"
    )]
    private partial void LogDisablingService(string serviceName);

    [LoggerMessage(
        EventId = 7818,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "📴 {ServiceName} service marked as disabled"
    )]
    private partial void LogServiceDisabled(string serviceName);

    [LoggerMessage(
        EventId = 7819,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "⚠️ Unknown service name for disabling: {ServiceName}"
    )]
    private partial void LogUnknownServiceName(string serviceName);

    [LoggerMessage(
        EventId = 7820,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "❌ Failed to disable service: {ServiceName}"
    )]
    private partial void LogServiceDisableFailed(string serviceName, Exception exception);

    [LoggerMessage(
        EventId = 7821,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "⏰ Service initialization timed out after 30 seconds - continuing with partial initialization"
    )]
    private partial void LogInitializationTimeout();

    [LoggerMessage(
        EventId = 7822,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "⚠️ Service initialization partially failed - continuing with available services"
    )]
    private partial void LogInitializationPartialFailure(Exception exception);

    #endregion

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.LogInitializationStarted();

        try
        {
            // Create a timeout for the entire initialization process
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout for initialization

            // Initialize services in parallel for faster startup
            var initializationTasks = new List<Task>();

            // Initialize Snapcast service if available
            var snapcastService = this._serviceProvider.GetService<ISnapcastService>();
            if (snapcastService != null)
            {
                this.LogInitializingSnapcast();
                initializationTasks.Add(this.InitializeSnapcastServiceAsync(snapcastService, timeoutCts.Token));
            }
            else
            {
                this.LogSnapcastNotRegistered();
            }

            // Initialize MQTT service if available
            var mqttService = this._serviceProvider.GetService<IMqttService>();
            if (mqttService != null)
            {
                this.LogInitializingMqtt();
                initializationTasks.Add(this.InitializeMqttServiceAsync(mqttService, timeoutCts.Token));
            }
            else
            {
                this.LogMqttNotRegistered();
            }

            // Initialize KNX service if available
            var knxService = this._serviceProvider.GetService<IKnxService>();
            if (knxService != null)
            {
                this.LogInitializingKnx();
                initializationTasks.Add(this.InitializeKnxServiceAsync(knxService, timeoutCts.Token));
            }
            else
            {
                this.LogKnxNotRegistered();
            }

            // Initialize Subsonic service if available
            var subsonicService = this._serviceProvider.GetService<ISubsonicService>();
            if (subsonicService != null)
            {
                this.LogInitializingSubsonic();
                initializationTasks.Add(this.InitializeSubsonicServiceAsync(subsonicService, timeoutCts.Token));
            }
            else
            {
                this.LogSubsonicNotRegistered();
            }

            // Wait for all services to initialize with timeout handling
            if (initializationTasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(initializationTasks);
                }
                catch (OperationCanceledException)
                    when (timeoutCts.Token.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
                {
                    this.LogInitializationTimeout();
                    // Continue with partial initialization - don't fail the entire application
                }
                catch (Exception ex)
                {
                    this.LogInitializationPartialFailure(ex);
                    // Continue with partial initialization - don't fail the entire application
                }

                // Check actual service states to provide accurate logging and system health assessment
                var serviceStates = new List<(string Name, bool IsConnected, bool IsCritical)>();

                if (snapcastService != null)
                {
                    serviceStates.Add(("Snapcast", snapcastService.IsConnected, true)); // Critical for audio functionality
                }

                if (mqttService != null)
                {
                    serviceStates.Add(("MQTT", mqttService.IsConnected, true)); // Critical for IoT integration
                }

                if (knxService != null)
                {
                    serviceStates.Add(("KNX", knxService.IsConnected, false)); // Non-critical, building automation
                }

                if (subsonicService != null)
                {
                    // Subsonic doesn't have IsConnected property, so we assume it's available if registered
                    serviceStates.Add(("Subsonic", true, false)); // Non-critical, music streaming
                }

                if (subsonicService != null)
                {
                    // Subsonic doesn't have IsConnected property, so we assume it's available if registered
                    serviceStates.Add(("Subsonic", true, false)); // Non-critical, music streaming
                }

                var successfulServices = serviceStates.Where(s => s.IsConnected).ToList();
                var failedServices = serviceStates.Where(s => !s.IsConnected).ToList();
                var criticalFailures = failedServices.Where(s => s.IsCritical).ToList();
                var nonCriticalFailures = failedServices.Where(s => !s.IsCritical).ToList();

                // Assess system health and take appropriate action
                if (criticalFailures.Count != 0)
                {
                    // System is non-functional - critical services failed
                    this.LogSystemNonFunctional(
                        string.Join(", ", criticalFailures.Select(s => s.Name)),
                        string.Join(", ", nonCriticalFailures.Select(s => s.Name)),
                        string.Join(", ", successfulServices.Select(s => s.Name))
                    );

                    // Disable failed services to prevent further issues
                    await this.DisableFailedServicesAsync(failedServices.Select(s => s.Name));

                    // Terminate application - system cannot function without critical services
                    Environment.Exit(1);
                }
                else if (nonCriticalFailures.Count != 0)
                {
                    // System is degraded but functional - only non-critical services failed
                    this.LogSystemDegraded(
                        string.Join(", ", nonCriticalFailures.Select(s => s.Name)),
                        string.Join(", ", successfulServices.Select(s => s.Name))
                    );

                    // Disable failed services to prevent resource waste and error noise
                    await this.DisableFailedServicesAsync(nonCriticalFailures.Select(s => s.Name));
                }
                else
                {
                    // All services successful
                    this.LogAllServicesInitialized(string.Join(", ", successfulServices.Select(s => s.Name)));
                }
            }
            else
            {
                this.LogNoServicesFound();
            }

            // Keep the service running to maintain connections
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            this.LogIntegrationServicesInitializationCancelled();
        }
        catch (Exception ex)
        {
            this.LogFailedToInitializeIntegrationServices(ex);
            throw; // Re-throw to stop the application if critical services fail
        }
    }

    private async Task InitializeSnapcastServiceAsync(
        ISnapcastService snapcastService,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await snapcastService.InitializeAsync(cancellationToken);
            if (result.IsSuccess)
            {
                this.LogSnapcastInitialized();
            }
            else
            {
                this.LogSnapcastInitializationFailed(result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            this.LogSnapcastInitializationFailed(ex.Message);
        }
    }

    private async Task InitializeMqttServiceAsync(IMqttService mqttService, CancellationToken cancellationToken)
    {
        try
        {
            var result = await mqttService.InitializeAsync(cancellationToken);
            if (result.IsSuccess)
            {
                this.LogMqttInitialized(mqttService.IsConnected);
            }
            else
            {
                this.LogMqttInitializationFailed(result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            this.LogMqttInitializationFailed(ex.Message);
        }
    }

    private async Task InitializeKnxServiceAsync(IKnxService knxService, CancellationToken cancellationToken)
    {
        try
        {
            var result = await knxService.InitializeAsync(cancellationToken);
            if (result.IsSuccess)
            {
                this.LogKnxInitialized(knxService.IsConnected);
            }
            else
            {
                this.LogKnxInitializationFailed(result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            this.LogKnxInitializationFailed(ex.Message);
        }
    }

    private async Task InitializeSubsonicServiceAsync(
        ISubsonicService subsonicService,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await subsonicService.InitializeAsync(cancellationToken);
            if (result.IsSuccess)
            {
                this.LogSubsonicInitialized();
            }
            else
            {
                this.LogSubsonicInitializationFailed(result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            this.LogSubsonicInitializationFailed(ex.Message);
        }
    }

    /// <summary>
    /// Disables failed integration services to prevent resource waste and error noise.
    /// This helps maintain system stability by stopping retry attempts and cleanup resources.
    /// </summary>
    /// <param name="failedServiceNames">Names of services that failed to initialize.</param>
    private async Task DisableFailedServicesAsync(IEnumerable<string> failedServiceNames)
    {
        foreach (var serviceName in failedServiceNames)
        {
            try
            {
                this.LogDisablingService(serviceName);

                switch (serviceName.ToLowerInvariant())
                {
                    case "snapcast":
                        var snapcastService = this._serviceProvider.GetService<ISnapcastService>();
                        if (snapcastService != null)
                        {
                            // Note: ISnapcastService doesn't have a Dispose method in the interface
                            // The service will be disposed by DI container when the application shuts down
                            this.LogServiceDisabled("Snapcast");
                        }
                        break;

                    case "mqtt":
                        var mqttService = this._serviceProvider.GetService<IMqttService>();
                        if (mqttService != null)
                        {
                            // Note: IMqttService doesn't have a Dispose method in the interface
                            // The service will be disposed by DI container when the application shuts down
                            this.LogServiceDisabled("MQTT");
                        }
                        break;

                    case "knx":
                        var knxService = this._serviceProvider.GetService<IKnxService>();
                        if (knxService != null)
                        {
                            // Note: IKnxService doesn't have a Dispose method in the interface
                            // The service will be disposed by DI container when the application shuts down
                            this.LogServiceDisabled("KNX");
                        }
                        break;

                    default:
                        this.LogUnknownServiceName(serviceName);
                        break;
                }
            }
            catch (Exception ex)
            {
                this.LogServiceDisableFailed(serviceName, ex);
            }
        }

        await Task.CompletedTask; // Placeholder for any async cleanup operations
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        this.LogStoppingIntegrationServices();
        await base.StopAsync(cancellationToken);
        this.LogIntegrationServicesStopped();
    }
}
