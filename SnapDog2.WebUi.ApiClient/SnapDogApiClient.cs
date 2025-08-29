using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using SnapDog2.WebUi.ApiClient.Generated;

namespace SnapDog2.WebUi.ApiClient;

/// <summary>
/// Enterprise API client implementation with resilience, logging, and business logic.
/// Wraps the generated transport client with enterprise patterns.
/// </summary>
public partial class SnapDogApiClient : ISnapDogApiClient
{
    private readonly IGeneratedSnapDogClient _generatedClient;
    private readonly ILogger<SnapDogApiClient> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public SnapDogApiClient(
        IGeneratedSnapDogClient generatedClient,
        ILogger<SnapDogApiClient> logger)
    {
        _generatedClient = generatedClient;
        _logger = logger;
        _retryPolicy = CreateRetryPolicy();
    }

    public async Task<PublishableZoneState[]> GetAllZonesAsync(CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            LogFetchingZones();
            var zonesPage = await _generatedClient.ZonesAsync(null, null, cancellationToken);
            LogRetrievedZones(zonesPage.Items?.Count ?? 0);
            return zonesPage.Items?.ToArray() ?? Array.Empty<PublishableZoneState>();
        });
    }

    public async Task<ClientState[]> GetAllClientsAsync(CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            LogFetchingClients();
            var clients = await _generatedClient.ClientsAllAsync(cancellationToken);
            LogRetrievedClients(clients?.Count ?? 0);
            return clients?.ToArray() ?? Array.Empty<ClientState>();
        });
    }

    public async Task<SystemStatusApiResponse> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            LogFetchingSystemStatus();
            var status = await _generatedClient.StatusAsync(cancellationToken);
            LogRetrievedSystemStatus();
            return status;
        });
    }

    public async Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default)
    {
        // Business validation
        if (clientIndex < 1)
        {
            throw new ArgumentException("Client index must be >= 1", nameof(clientIndex));
        }

        if (zoneIndex < 1)
        {
            throw new ArgumentException("Zone index must be >= 1", nameof(zoneIndex));
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            LogAssigningClient(clientIndex, zoneIndex);

            try
            {
                await _generatedClient.ZonePUTAsync(clientIndex, zoneIndex, cancellationToken);
                LogClientAssigned(clientIndex, zoneIndex);
            }
            catch (ApiException ex) when (ex.StatusCode == (int)HttpStatusCode.NotFound)
            {
                LogClientOrZoneNotFound(clientIndex, zoneIndex);
                throw new InvalidOperationException($"Client {clientIndex} or zone {zoneIndex} not found", ex);
            }
        });
    }

    public async Task PlayZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        if (zoneIndex < 1)
        {
            throw new ArgumentException("Zone index must be >= 1", nameof(zoneIndex));
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            LogPlayingZone(zoneIndex);
            await _generatedClient.PlayAsync(zoneIndex, cancellationToken);
            LogZonePlayed(zoneIndex);
        });
    }

    public async Task PauseZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        if (zoneIndex < 1)
        {
            throw new ArgumentException("Zone index must be >= 1", nameof(zoneIndex));
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            LogPausingZone(zoneIndex);
            await _generatedClient.PauseAsync(zoneIndex, cancellationToken);
            LogZonePaused(zoneIndex);
        });
    }

    public async Task StopZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        if (zoneIndex < 1)
        {
            throw new ArgumentException("Zone index must be >= 1", nameof(zoneIndex));
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            LogStoppingZone(zoneIndex);
            await _generatedClient.StopAsync(zoneIndex, cancellationToken);
            LogZoneStopped(zoneIndex);
        });
    }

    public async Task<int> NextTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        if (zoneIndex < 1)
        {
            throw new ArgumentException("Zone index must be >= 1", nameof(zoneIndex));
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            LogSkippingToNext(zoneIndex);
            var trackIndex = await _generatedClient.NextAsync(zoneIndex, cancellationToken);
            LogSkippedToNext(zoneIndex, trackIndex);
            return trackIndex;
        });
    }

    public async Task<int> PreviousTrackAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        if (zoneIndex < 1)
        {
            throw new ArgumentException("Zone index must be >= 1", nameof(zoneIndex));
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            LogSkippingToPrevious(zoneIndex);
            var trackIndex = await _generatedClient.PreviousAsync(zoneIndex, cancellationToken);
            LogSkippedToPrevious(zoneIndex, trackIndex);
            return trackIndex;
        });
    }

    public async Task<bool> ToggleMuteAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        if (zoneIndex < 1)
        {
            throw new ArgumentException("Zone index must be >= 1", nameof(zoneIndex));
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            LogTogglingMute(zoneIndex);
            var muteState = await _generatedClient.Toggle2Async(zoneIndex, cancellationToken);
            LogMuteToggled(zoneIndex, muteState);
            return muteState;
        });
    }

    public async Task<int> SetVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default)
    {
        if (zoneIndex < 1)
        {
            throw new ArgumentException("Zone index must be >= 1", nameof(zoneIndex));
        }

        if (volume < 0 || volume > 100)
        {
            throw new ArgumentException("Volume must be between 0 and 100", nameof(volume));
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            LogSettingVolume(zoneIndex, volume);
            var actualVolume = await _generatedClient.VolumePUT2Async(zoneIndex, volume, cancellationToken);
            LogVolumeSet(zoneIndex, actualVolume);
            return actualVolume;
        });
    }

    private static IAsyncPolicy CreateRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Fetching all zones")]
    private partial void LogFetchingZones();

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Retrieved {ZoneCount} zones")]
    private partial void LogRetrievedZones(int ZoneCount);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Assigning client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogAssigningClient(int ClientIndex, int ZoneIndex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Successfully assigned client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogClientAssigned(int ClientIndex, int ZoneIndex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Client {ClientIndex} or zone {ZoneIndex} not found")]
    private partial void LogClientOrZoneNotFound(int ClientIndex, int ZoneIndex);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Playing zone {ZoneIndex}")]
    private partial void LogPlayingZone(int ZoneIndex);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Zone {ZoneIndex} started playing")]
    private partial void LogZonePlayed(int ZoneIndex);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Pausing zone {ZoneIndex}")]
    private partial void LogPausingZone(int ZoneIndex);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Zone {ZoneIndex} paused")]
    private partial void LogZonePaused(int ZoneIndex);

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Stopping zone {ZoneIndex}")]
    private partial void LogStoppingZone(int ZoneIndex);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Zone {ZoneIndex} stopped")]
    private partial void LogZoneStopped(int ZoneIndex);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "Skipping to next track in zone {ZoneIndex}")]
    private partial void LogSkippingToNext(int ZoneIndex);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "Zone {ZoneIndex} skipped to track {TrackIndex}")]
    private partial void LogSkippedToNext(int ZoneIndex, int TrackIndex);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "Skipping to previous track in zone {ZoneIndex}")]
    private partial void LogSkippingToPrevious(int ZoneIndex);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "Zone {ZoneIndex} skipped to track {TrackIndex}")]
    private partial void LogSkippedToPrevious(int ZoneIndex, int TrackIndex);

    [LoggerMessage(EventId = 16, Level = LogLevel.Information, Message = "Toggling mute for zone {ZoneIndex}")]
    private partial void LogTogglingMute(int ZoneIndex);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = "Zone {ZoneIndex} mute state: {MuteState}")]
    private partial void LogMuteToggled(int ZoneIndex, bool MuteState);

    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = "Setting volume for zone {ZoneIndex} to {Volume}")]
    private partial void LogSettingVolume(int ZoneIndex, int Volume);

    [LoggerMessage(EventId = 19, Level = LogLevel.Information, Message = "Zone {ZoneIndex} volume set to {Volume}")]
    private partial void LogVolumeSet(int ZoneIndex, int Volume);

    [LoggerMessage(EventId = 20, Level = LogLevel.Debug, Message = "Fetching all clients")]
    private partial void LogFetchingClients();

    [LoggerMessage(EventId = 21, Level = LogLevel.Debug, Message = "Retrieved {ClientCount} clients")]
    private partial void LogRetrievedClients(int ClientCount);

    [LoggerMessage(EventId = 22, Level = LogLevel.Debug, Message = "Fetching system status")]
    private partial void LogFetchingSystemStatus();

    [LoggerMessage(EventId = 23, Level = LogLevel.Debug, Message = "Retrieved system status")]
    private partial void LogRetrievedSystemStatus();
}
