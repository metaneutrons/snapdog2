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
namespace SnapDog2.Server.Subsonic.Handlers;

using Cortex.Mediator.Notifications;
using SnapDog2.Server.Subsonic.Notifications;

/// <summary>
/// Handles Subsonic connection established notifications.
/// </summary>
public partial class SubsonicConnectionEstablishedNotificationHandler(
    ILogger<SubsonicConnectionEstablishedNotificationHandler> logger
) : INotificationHandler<SubsonicConnectionEstablishedNotification>
{
    private readonly ILogger<SubsonicConnectionEstablishedNotificationHandler> _logger = logger;

    public Task Handle(SubsonicConnectionEstablishedNotification notification, CancellationToken cancellationToken)
    {
        LogSubsonicConnectionEstablished(this._logger, notification.ServerUrl, notification.Username);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 12700,
        Level = LogLevel.Information,
        Message = "🎵 Subsonic connection established to {ServerUrl} for user {Username}"
    )]
    private static partial void LogSubsonicConnectionEstablished(ILogger logger, string serverUrl, string username);
}

/// <summary>
/// Handles Subsonic connection lost notifications.
/// </summary>
public partial class SubsonicConnectionLostNotificationHandler(
    ILogger<SubsonicConnectionLostNotificationHandler> logger
) : INotificationHandler<SubsonicConnectionLostNotification>
{
    private readonly ILogger<SubsonicConnectionLostNotificationHandler> _logger = logger;

    public Task Handle(SubsonicConnectionLostNotification notification, CancellationToken cancellationToken)
    {
        LogSubsonicConnectionLost(this._logger, notification.ServerUrl, notification.Reason);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 12701,
        Level = LogLevel.Warning,
        Message = "⚠️ Subsonic connection lost to {ServerUrl}: {Reason}"
    )]
    private static partial void LogSubsonicConnectionLost(ILogger logger, string serverUrl, string reason);
}

/// <summary>
/// Handles Subsonic playlists retrieved notifications for analytics and caching.
/// </summary>
public partial class SubsonicPlaylistsRetrievedNotificationHandler(
    ILogger<SubsonicPlaylistsRetrievedNotificationHandler> logger
) : INotificationHandler<SubsonicPlaylistsRetrievedNotification>
{
    private readonly ILogger<SubsonicPlaylistsRetrievedNotificationHandler> _logger = logger;

    public Task Handle(SubsonicPlaylistsRetrievedNotification notification, CancellationToken cancellationToken)
    {
        LogPlaylistsRetrieved(this._logger, notification.PlaylistCount, notification.RetrievalTime.TotalMilliseconds);

        // Future: Could implement caching logic here
        // Future: Could update metrics/analytics here

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 12702,
        Level = LogLevel.Debug,
        Message = "📊 Retrieved {PlaylistCount} playlists in {RetrievalTimeMs:F2}ms"
    )]
    private static partial void LogPlaylistsRetrieved(ILogger logger, int playlistCount, double retrievalTimeMs);
}

/// <summary>
/// Handles Subsonic playlist accessed notifications for usage tracking.
/// </summary>
public partial class SubsonicPlaylistAccessedNotificationHandler(
    ILogger<SubsonicPlaylistAccessedNotificationHandler> logger
) : INotificationHandler<SubsonicPlaylistAccessedNotification>
{
    private readonly ILogger<SubsonicPlaylistAccessedNotificationHandler> _logger = logger;

    public Task Handle(SubsonicPlaylistAccessedNotification notification, CancellationToken cancellationToken)
    {
        LogPlaylistAccessed(this._logger, notification.PlaylistName, notification.TrackCount);

        // Future: Could implement usage analytics here
        // Future: Could implement playlist popularity tracking here

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 12703,
        Level = LogLevel.Debug,
        Message = "🎵 Accessed playlist '{PlaylistName}' with {TrackCount} tracks"
    )]
    private static partial void LogPlaylistAccessed(ILogger logger, string playlistName, int trackCount);
}

/// <summary>
/// Handles Subsonic stream requested notifications for usage tracking.
/// </summary>
public partial class SubsonicStreamRequestedNotificationHandler(
    ILogger<SubsonicStreamRequestedNotificationHandler> logger
) : INotificationHandler<SubsonicStreamRequestedNotification>
{
    private readonly ILogger<SubsonicStreamRequestedNotificationHandler> _logger = logger;

    public Task Handle(SubsonicStreamRequestedNotification notification, CancellationToken cancellationToken)
    {
        LogStreamRequested(this._logger, notification.TrackId);

        // Future: Could implement stream analytics here
        // Future: Could implement track popularity tracking here

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 12704,
        Level = LogLevel.Debug,
        Message = "🎧 Stream requested for track {TrackId}"
    )]
    private static partial void LogStreamRequested(ILogger logger, string trackId);
}

/// <summary>
/// Handles Subsonic service error notifications for monitoring and alerting.
/// </summary>
public partial class SubsonicServiceErrorNotificationHandler(ILogger<SubsonicServiceErrorNotificationHandler> logger)
    : INotificationHandler<SubsonicServiceErrorNotification>
{
    private readonly ILogger<SubsonicServiceErrorNotificationHandler> _logger = logger;

    public Task Handle(SubsonicServiceErrorNotification notification, CancellationToken cancellationToken)
    {
        LogServiceError(this._logger, notification.Operation, notification.ErrorMessage);

        // Future: Could implement alerting logic here
        // Future: Could implement error rate monitoring here

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 12705,
        Level = LogLevel.Error,
        Message = "❌ Subsonic service error in {Operation}: {ErrorMessage}"
    )]
    private static partial void LogServiceError(ILogger logger, string operation, string errorMessage);
}

/// <summary>
/// Handles Subsonic initialization started notifications for monitoring.
/// </summary>
public partial class SubsonicInitializationStartedNotificationHandler(
    ILogger<SubsonicInitializationStartedNotificationHandler> logger
) : INotificationHandler<SubsonicInitializationStartedNotification>
{
    private readonly ILogger<SubsonicInitializationStartedNotificationHandler> _logger = logger;

    public Task Handle(SubsonicInitializationStartedNotification notification, CancellationToken cancellationToken)
    {
        LogInitializationStarted(this._logger, notification.ServerUrl);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 12706,
        Level = LogLevel.Information,
        Message = "🚀 Subsonic service initialization started for {ServerUrl}"
    )]
    private static partial void LogInitializationStarted(ILogger logger, string serverUrl);
}

/// <summary>
/// Handles Subsonic service disposed notifications for cleanup tracking.
/// </summary>
public partial class SubsonicServiceDisposedNotificationHandler(
    ILogger<SubsonicServiceDisposedNotificationHandler> logger
) : INotificationHandler<SubsonicServiceDisposedNotification>
{
    private readonly ILogger<SubsonicServiceDisposedNotificationHandler> _logger = logger;

    public Task Handle(SubsonicServiceDisposedNotification notification, CancellationToken cancellationToken)
    {
        LogServiceDisposed(this._logger, notification.ServerUrl);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 12707,
        Level = LogLevel.Information,
        Message = "🗑️ Subsonic service disposed for {ServerUrl}"
    )]
    private static partial void LogServiceDisposed(ILogger logger, string serverUrl);
}
