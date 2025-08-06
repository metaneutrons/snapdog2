namespace SnapDog2.Server.Features.Subsonic.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.Logging;
using SnapDog2.Server.Notifications;

/// <summary>
/// Handles Subsonic connection established notifications.
/// </summary>
public partial class SubsonicConnectionEstablishedNotificationHandler
    : INotificationHandler<SubsonicConnectionEstablishedNotification>
{
    private readonly ILogger<SubsonicConnectionEstablishedNotificationHandler> _logger;

    public SubsonicConnectionEstablishedNotificationHandler(
        ILogger<SubsonicConnectionEstablishedNotificationHandler> logger
    )
    {
        _logger = logger;
    }

    public Task Handle(SubsonicConnectionEstablishedNotification notification, CancellationToken cancellationToken)
    {
        LogSubsonicConnectionEstablished(_logger, notification.ServerUrl, notification.Username);
        return Task.CompletedTask;
    }

    [LoggerMessage(3000, LogLevel.Information, "🎵 Subsonic connection established to {ServerUrl} for user {Username}")]
    private static partial void LogSubsonicConnectionEstablished(ILogger logger, string serverUrl, string username);
}

/// <summary>
/// Handles Subsonic connection lost notifications.
/// </summary>
public partial class SubsonicConnectionLostNotificationHandler
    : INotificationHandler<SubsonicConnectionLostNotification>
{
    private readonly ILogger<SubsonicConnectionLostNotificationHandler> _logger;

    public SubsonicConnectionLostNotificationHandler(ILogger<SubsonicConnectionLostNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SubsonicConnectionLostNotification notification, CancellationToken cancellationToken)
    {
        LogSubsonicConnectionLost(_logger, notification.ServerUrl, notification.Reason);
        return Task.CompletedTask;
    }

    [LoggerMessage(3001, LogLevel.Warning, "⚠️ Subsonic connection lost to {ServerUrl}: {Reason}")]
    private static partial void LogSubsonicConnectionLost(ILogger logger, string serverUrl, string reason);
}

/// <summary>
/// Handles Subsonic playlists retrieved notifications for analytics and caching.
/// </summary>
public partial class SubsonicPlaylistsRetrievedNotificationHandler
    : INotificationHandler<SubsonicPlaylistsRetrievedNotification>
{
    private readonly ILogger<SubsonicPlaylistsRetrievedNotificationHandler> _logger;

    public SubsonicPlaylistsRetrievedNotificationHandler(ILogger<SubsonicPlaylistsRetrievedNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SubsonicPlaylistsRetrievedNotification notification, CancellationToken cancellationToken)
    {
        LogPlaylistsRetrieved(_logger, notification.PlaylistCount, notification.RetrievalTime.TotalMilliseconds);

        // Future: Could implement caching logic here
        // Future: Could update metrics/analytics here

        return Task.CompletedTask;
    }

    [LoggerMessage(3002, LogLevel.Debug, "📊 Retrieved {PlaylistCount} playlists in {RetrievalTimeMs:F2}ms")]
    private static partial void LogPlaylistsRetrieved(ILogger logger, int playlistCount, double retrievalTimeMs);
}

/// <summary>
/// Handles Subsonic playlist accessed notifications for usage tracking.
/// </summary>
public partial class SubsonicPlaylistAccessedNotificationHandler
    : INotificationHandler<SubsonicPlaylistAccessedNotification>
{
    private readonly ILogger<SubsonicPlaylistAccessedNotificationHandler> _logger;

    public SubsonicPlaylistAccessedNotificationHandler(ILogger<SubsonicPlaylistAccessedNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SubsonicPlaylistAccessedNotification notification, CancellationToken cancellationToken)
    {
        LogPlaylistAccessed(_logger, notification.PlaylistName, notification.TrackCount);

        // Future: Could implement usage analytics here
        // Future: Could implement playlist popularity tracking here

        return Task.CompletedTask;
    }

    [LoggerMessage(3003, LogLevel.Debug, "🎵 Accessed playlist '{PlaylistName}' with {TrackCount} tracks")]
    private static partial void LogPlaylistAccessed(ILogger logger, string playlistName, int trackCount);
}

/// <summary>
/// Handles Subsonic stream requested notifications for usage tracking.
/// </summary>
public partial class SubsonicStreamRequestedNotificationHandler
    : INotificationHandler<SubsonicStreamRequestedNotification>
{
    private readonly ILogger<SubsonicStreamRequestedNotificationHandler> _logger;

    public SubsonicStreamRequestedNotificationHandler(ILogger<SubsonicStreamRequestedNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SubsonicStreamRequestedNotification notification, CancellationToken cancellationToken)
    {
        LogStreamRequested(_logger, notification.TrackId);

        // Future: Could implement stream analytics here
        // Future: Could implement track popularity tracking here

        return Task.CompletedTask;
    }

    [LoggerMessage(3004, LogLevel.Debug, "🎧 Stream requested for track {TrackId}")]
    private static partial void LogStreamRequested(ILogger logger, string trackId);
}

/// <summary>
/// Handles Subsonic service error notifications for monitoring and alerting.
/// </summary>
public partial class SubsonicServiceErrorNotificationHandler : INotificationHandler<SubsonicServiceErrorNotification>
{
    private readonly ILogger<SubsonicServiceErrorNotificationHandler> _logger;

    public SubsonicServiceErrorNotificationHandler(ILogger<SubsonicServiceErrorNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SubsonicServiceErrorNotification notification, CancellationToken cancellationToken)
    {
        LogServiceError(_logger, notification.Operation, notification.ErrorMessage);

        // Future: Could implement alerting logic here
        // Future: Could implement error rate monitoring here

        return Task.CompletedTask;
    }

    [LoggerMessage(3005, LogLevel.Error, "❌ Subsonic service error in {Operation}: {ErrorMessage}")]
    private static partial void LogServiceError(ILogger logger, string operation, string errorMessage);
}

/// <summary>
/// Handles Subsonic initialization started notifications for monitoring.
/// </summary>
public partial class SubsonicInitializationStartedNotificationHandler
    : INotificationHandler<SubsonicInitializationStartedNotification>
{
    private readonly ILogger<SubsonicInitializationStartedNotificationHandler> _logger;

    public SubsonicInitializationStartedNotificationHandler(
        ILogger<SubsonicInitializationStartedNotificationHandler> logger
    )
    {
        _logger = logger;
    }

    public Task Handle(SubsonicInitializationStartedNotification notification, CancellationToken cancellationToken)
    {
        LogInitializationStarted(_logger, notification.ServerUrl);
        return Task.CompletedTask;
    }

    [LoggerMessage(3006, LogLevel.Information, "🚀 Subsonic service initialization started for {ServerUrl}")]
    private static partial void LogInitializationStarted(ILogger logger, string serverUrl);
}

/// <summary>
/// Handles Subsonic service disposed notifications for cleanup tracking.
/// </summary>
public partial class SubsonicServiceDisposedNotificationHandler
    : INotificationHandler<SubsonicServiceDisposedNotification>
{
    private readonly ILogger<SubsonicServiceDisposedNotificationHandler> _logger;

    public SubsonicServiceDisposedNotificationHandler(ILogger<SubsonicServiceDisposedNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SubsonicServiceDisposedNotification notification, CancellationToken cancellationToken)
    {
        LogServiceDisposed(_logger, notification.ServerUrl);
        return Task.CompletedTask;
    }

    [LoggerMessage(3007, LogLevel.Information, "🗑️ Subsonic service disposed for {ServerUrl}")]
    private static partial void LogServiceDisposed(ILogger logger, string serverUrl);
}
