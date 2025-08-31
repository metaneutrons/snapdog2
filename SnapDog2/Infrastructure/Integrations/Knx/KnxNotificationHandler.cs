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
namespace SnapDog2.Infrastructure.Integrations.Knx;

using Cortex.Mediator.Notifications;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Shared.Notifications;

/// <summary>
/// Dedicated notification handler that forwards StatusChangedNotification to KNX service.
/// This wrapper ensures proper MediatR registration and notification routing.
/// </summary>
public partial class KnxNotificationHandler : INotificationHandler<StatusChangedNotification>
{
    private readonly IKnxService _knxService;
    private readonly ILogger<KnxNotificationHandler> _logger;

    public KnxNotificationHandler(IKnxService knxService, ILogger<KnxNotificationHandler> logger)
    {
        _knxService = knxService;
        _logger = logger;
    }

    public async Task Handle(StatusChangedNotification notification, CancellationToken cancellationToken)
    {
        // Debug log to verify we're receiving notifications
        LogKnxNotificationReceived(notification.StatusType, notification.TargetIndex, notification.Value?.ToString() ?? "null");

        // Forward to KNX service using its actual interface methods
        try
        {
            await _knxService.SendStatusAsync(
                notification.StatusType,
                notification.TargetIndex,
                notification.Value ?? string.Empty,
                cancellationToken);

            LogKnxNotificationForwarded(notification.StatusType, notification.TargetIndex);
        }
        catch (Exception ex)
        {
            LogKnxNotificationFailed(notification.StatusType, notification.TargetIndex, ex.Message);
        }
    }

    [LoggerMessage(
        EventId = 3300,
        Level = LogLevel.Debug,
        Message = "🔔 KNX notification handler received: {StatusType} for target {TargetIndex} with value {Value}"
    )]
    private partial void LogKnxNotificationReceived(string statusType, int targetIndex, string value);

    [LoggerMessage(
        EventId = 3301,
        Level = LogLevel.Debug,
        Message = "✅ KNX notification forwarded: {StatusType} for target {TargetIndex}"
    )]
    private partial void LogKnxNotificationForwarded(string statusType, int targetIndex);

    [LoggerMessage(
        EventId = 3302,
        Level = LogLevel.Error,
        Message = "❌ KNX notification failed: {StatusType} for target {TargetIndex} - {Error}"
    )]
    private partial void LogKnxNotificationFailed(string statusType, int targetIndex, string error);
}
