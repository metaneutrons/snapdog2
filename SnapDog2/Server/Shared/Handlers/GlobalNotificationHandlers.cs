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
namespace SnapDog2.Server.Shared.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Global.Notifications;
using SnapDog2.Shared.Attributes;

/// <summary>
/// Handles global system state change notifications to log and process status updates.
/// </summary>
public partial class GlobalStateNotificationHandler(
    IServiceProvider serviceProvider,
    ILogger<GlobalStateNotificationHandler> logger
)
    : INotificationHandler<SystemStatusChangedNotification>,
        INotificationHandler<VersionInfoChangedNotification>,
        INotificationHandler<ServerStatsChangedNotification>,
        INotificationHandler<SystemErrorNotification>,
        INotificationHandler<ZonesInfoChangedNotification>
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<GlobalStateNotificationHandler> _logger = logger;

    [LoggerMessage(
        EventId = 9700,
        Level = LogLevel.Information,
        Message = "System status changed to online: {IsOnline}"
    )]
    private partial void LogSystemStatusChange(bool isOnline);

    [LoggerMessage(
        EventId = 9701,
        Level = LogLevel.Information,
        Message = "Version info updated: {Version} (Build: {BuildDate})"
    )]
    private partial void LogVersionInfoChange(string version, DateTime buildDate);

    [LoggerMessage(
        EventId = 9702,
        Level = LogLevel.Information,
        Message = "Server stats updated - CPU: {CpuUsage:F2}%, Memory: {MemoryUsage:F2}MB"
    )]
    private partial void LogServerStatsChange(double cpuUsage, double memoryUsage);

    [LoggerMessage(
        EventId = 9703,
        Level = LogLevel.Error,
        Message = "System error occurred: {ErrorCode} - {Message}"
    )]
    private partial void LogSystemError(string errorCode, string message);

    [LoggerMessage(
        EventId = 9704,
        Level = LogLevel.Information,
        Message = "Zones info updated - Available zones: [{ZoneIndices}]"
    )]
    private partial void LogZonesInfoChange(string zoneIndices);

    public async Task Handle(SystemStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogSystemStatusChange(notification.Status.IsOnline);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<SystemStatusChangedNotification>(),
            notification.Status,
            cancellationToken
        );
    }

    public async Task Handle(VersionInfoChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogVersionInfoChange(
            notification.VersionInfo.Version,
            notification.VersionInfo.BuildDateUtc ?? DateTime.UtcNow
        );

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<VersionInfoChangedNotification>(),
            notification.VersionInfo,
            cancellationToken
        );
    }

    public async Task Handle(ServerStatsChangedNotification notification, CancellationToken cancellationToken)
    {
        this.LogServerStatsChange(notification.Stats.CpuUsagePercent, notification.Stats.MemoryUsageMb);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ServerStatsChangedNotification>(),
            notification.Stats,
            cancellationToken
        );
    }

    public async Task Handle(SystemErrorNotification notification, CancellationToken cancellationToken)
    {
        this.LogSystemError(notification.Error.ErrorCode, notification.Error.Message);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<SystemErrorNotification>(),
            notification.Error,
            cancellationToken
        );
    }

    public async Task Handle(ZonesInfoChangedNotification notification, CancellationToken cancellationToken)
    {
        var zoneIndicesStr = string.Join(", ", notification.ZoneIndices);
        this.LogZonesInfoChange(zoneIndicesStr);

        // Publish to external systems (MQTT, KNX)
        await this.PublishToExternalSystemsAsync(
            StatusIdAttribute.GetStatusId<ZonesInfoChangedNotification>(),
            notification.ZoneIndices,
            cancellationToken
        );
    }

    /// <summary>
    /// Publishes global events to external systems (MQTT, KNX) if they are enabled.
    /// </summary>
    private async Task PublishToExternalSystemsAsync<T>(
        string eventType,
        T payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Publish to MQTT if enabled
            var mqttService = this._serviceProvider.GetService<IMqttService>();
            if (mqttService != null)
            {
                await mqttService.PublishGlobalStatusAsync(eventType, payload, cancellationToken);
            }

            // Publish to KNX if enabled
            var knxService = this._serviceProvider.GetService<IKnxService>();
            if (knxService != null)
            {
                await knxService.PublishGlobalStatusAsync(eventType, payload, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishGlobalEventToExternalSystems(ex, eventType);
        }
    }
}
