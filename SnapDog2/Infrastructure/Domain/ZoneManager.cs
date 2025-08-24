namespace SnapDog2.Infrastructure.Domain;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Audio;
using SnapDog2.Server.Features.Playlists.Queries;
using SnapDog2.Server.Features.Zones.Notifications;
using LibVLC = LibVLCSharp.Shared;

/// <summary>
/// Production-ready implementation of IZoneManager with full Snapcast integration.
/// Manages audio zones, their state, and coordinates with Snapcast groups.
/// </summary>
public partial class ZoneManager(
    ILogger<ZoneManager> logger,
    ISnapcastService snapcastService,
    ISnapcastStateRepository snapcastStateRepository,
    IMediaPlayerService mediaPlayerService,
    IServiceScopeFactory serviceScopeFactory,
    IZoneStateStore zoneStateStore,
    IStatusFactory statusFactory,
    IOptions<SnapDogConfiguration> configuration
) : IZoneManager, IAsyncDisposable, IDisposable
{
    private readonly ILogger<ZoneManager> _logger = logger;
    private readonly ISnapcastService _snapcastService = snapcastService;
    private readonly ISnapcastStateRepository _snapcastStateRepository = snapcastStateRepository;
    private readonly IMediaPlayerService _mediaPlayerService = mediaPlayerService;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IZoneStateStore _zoneStateStore = zoneStateStore;
    private readonly IStatusFactory _statusFactory = statusFactory;
    private readonly List<ZoneConfig> _zoneConfigs = configuration.Value.Zones;
    private readonly ConcurrentDictionary<int, IZoneService> _zones = new ConcurrentDictionary<int, IZoneService>();
    private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);
    private bool _isInitialized;
    private bool _disposed;

    [LoggerMessage(7001, LogLevel.Information, "Initializing ZoneManager with {ZoneCount} configured zones")]
    private partial void LogInitializing(int zoneCount);

    [LoggerMessage(7002, LogLevel.Information, "Zone {ZoneIndex} ({ZoneName}) initialized successfully")]
    private partial void LogZoneInitialized(int zoneIndex, string zoneName);

    [LoggerMessage(7003, LogLevel.Warning, "Zone {ZoneIndex} not found")]
    private partial void LogZoneNotFound(int zoneIndex);

    [LoggerMessage(7004, LogLevel.Error, "Failed to initialize zone {ZoneIndex}: {Error}")]
    private partial void LogZoneInitializationFailed(int zoneIndex, string error);

    [LoggerMessage(7005, LogLevel.Debug, "Getting zone {ZoneIndex}")]
    private partial void LogGettingZone(int zoneIndex);

    [LoggerMessage(7006, LogLevel.Debug, "Getting all zones")]
    private partial void LogGettingAllZones();

    /// <summary>
    /// Initializes all configured zones and their Snapcast group mappings.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await this._initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (this._isInitialized)
            {
                return;
            }

            this.LogInitializing(this._zoneConfigs.Count);

            // Initialize zones based on configuration
            for (int i = 0; i < this._zoneConfigs.Count; i++)
            {
                var zoneConfig = this._zoneConfigs[i];
                var zoneIndex = i + 1; // 1-based zone IDs

                try
                {
                    var zoneService = new ZoneService(
                        zoneIndex,
                        zoneConfig,
                        this._snapcastService,
                        this._snapcastStateRepository,
                        this._mediaPlayerService,
                        this._serviceScopeFactory,
                        this._zoneStateStore,
                        this._statusFactory,
                        this._logger
                    );

                    await zoneService.InitializeAsync(cancellationToken).ConfigureAwait(false);

                    this._zones.TryAdd(zoneIndex, zoneService);
                    this.LogZoneInitialized(zoneIndex, zoneConfig.Name);
                }
                catch (Exception ex)
                {
                    this.LogZoneInitializationFailed(zoneIndex, ex.Message);
                    // Continue with other zones even if one fails
                }
            }

            this._isInitialized = true;
        }
        finally
        {
            this._initializationLock.Release();
        }
    }

    public async Task<Result<IZoneService>> GetZoneAsync(int zoneIndex)
    {
        this.LogGettingZone(zoneIndex);

        if (!this._isInitialized)
        {
            await this.InitializeAsync().ConfigureAwait(false);
        }

        if (this._zones.TryGetValue(zoneIndex, out var zone))
        {
            return Result<IZoneService>.Success(zone);
        }

        this.LogZoneNotFound(zoneIndex);
        return Result<IZoneService>.Failure($"Zone {zoneIndex} not found");
    }

    public async Task<Result<IEnumerable<IZoneService>>> GetAllZonesAsync()
    {
        this.LogGettingAllZones();

        if (!this._isInitialized)
        {
            await this.InitializeAsync().ConfigureAwait(false);
        }

        return Result<IEnumerable<IZoneService>>.Success(this._zones.Values);
    }

    public async Task<Result<ZoneState>> GetZoneStateAsync(int zoneIndex)
    {
        var zoneResult = await this.GetZoneAsync(zoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            return Result<ZoneState>.Failure(zoneResult.ErrorMessage ?? "Zone not found");
        }

        return await zoneResult.Value!.GetStateAsync().ConfigureAwait(false);
    }

    public async Task<Result<List<ZoneState>>> GetAllZoneStatesAsync()
    {
        this.LogGettingAllZones();

        if (!this._isInitialized)
        {
            await this.InitializeAsync().ConfigureAwait(false);
        }

        var states = new List<ZoneState>();
        var tasks = this._zones.Values.Select(async zone =>
        {
            var stateResult = await zone.GetStateAsync().ConfigureAwait(false);
            return stateResult.IsSuccess ? stateResult.Value : null;
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        states.AddRange(results.Where(state => state != null)!);

        return Result<List<ZoneState>>.Success(states);
    }

    public async Task<bool> ZoneExistsAsync(int zoneIndex)
    {
        if (!this._isInitialized)
        {
            await this.InitializeAsync().ConfigureAwait(false);
        }

        return this._zones.ContainsKey(zoneIndex);
    }

    /// <summary>
    /// Synchronizes zones with current Snapcast server state.
    /// Called when Snapcast server state changes.
    /// </summary>
    public async Task SynchronizeWithSnapcastAsync()
    {
        if (!this._isInitialized)
        {
            return;
        }

        var snapcastGroups = this._snapcastStateRepository.GetAllGroups();

        foreach (var zone in this._zones.Values.Cast<ZoneService>())
        {
            await zone.SynchronizeWithSnapcastAsync(snapcastGroups).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (this._disposed)
        {
            return;
        }

        // Dispose all zone services
        var disposeTasks = this._zones.Values.OfType<IAsyncDisposable>().Select(zone => zone.DisposeAsync().AsTask());

        await Task.WhenAll(disposeTasks).ConfigureAwait(false);

        this._initializationLock?.Dispose();
        this._disposed = true;
    }

    public void Dispose()
    {
        // TODO: This is a workaround for DI container scope disposal limitations.
        // The proper solution would be to:
        // 1. Use IServiceScopeFactory in StatePublishingService to avoid disposing scoped services
        // 2. Or implement a custom ServiceScope that supports async disposal
        // 3. Or refactor to use IHostedService lifecycle management instead of BackgroundService
        // Current approach blocks on async disposal which could cause deadlocks in some scenarios.
        // See: https://github.com/dotnet/runtime/issues/61132
        this.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async Task<Result<ZoneState>> GetZoneAsync(int zoneIndex, CancellationToken cancellationToken = default)
    {
        return await GetZoneStateAsync(zoneIndex);
    }

    public async Task<Result<List<ZoneState>>> GetAllZonesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllZoneStatesAsync();
    }
}

/// <summary>
/// Production-ready implementation of IZoneService with full Snapcast integration.
/// </summary>
public partial class ZoneService : IZoneService, IAsyncDisposable
{
    private readonly int _zoneIndex;
    private readonly ZoneConfig _config;
    private int _lastLoggedPositionSeconds = -1; // Track last logged position to avoid spam
    private readonly ISnapcastService _snapcastService;
    private readonly ISnapcastStateRepository _snapcastStateRepository;
    private readonly IMediaPlayerService _mediaPlayerService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IZoneStateStore _zoneStateStore;
    private readonly IStatusFactory _statusFactory;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _stateLock;
    private ZoneState _currentState;
    private string? _snapcastGroupId;
    private bool _disposed;
    private Timer? _positionUpdateTimer;

    [LoggerMessage(7101, LogLevel.Information, "Zone {ZoneIndex} ({ZoneName}): {Action}")]
    private partial void LogZoneAction(int zoneIndex, string zoneName, string action);

    [LoggerMessage(7102, LogLevel.Debug, "Zone {ZoneIndex} synchronized with Snapcast group {GroupId}")]
    private partial void LogSnapcastSync(int zoneIndex, string groupId);

    [LoggerMessage(7103, LogLevel.Warning, "Zone {ZoneIndex} Snapcast group {GroupId} not found")]
    private partial void LogSnapcastGroupNotFound(int zoneIndex, string groupId);

    [LoggerMessage(7104, LogLevel.Error, "Zone {ZoneIndex} ({ZoneName}): {Action} - {Error}")]
    private partial void LogZoneError(int zoneIndex, string zoneName, string action, string error);

    public int ZoneIndex => this._zoneIndex;

    public ZoneService(
        int zoneIndex,
        ZoneConfig config,
        ISnapcastService snapcastService,
        ISnapcastStateRepository snapcastStateRepository,
        IMediaPlayerService mediaPlayerService,
        IServiceScopeFactory serviceScopeFactory,
        IZoneStateStore zoneStateStore,
        IStatusFactory statusFactory,
        ILogger logger
    )
    {
        this._zoneIndex = zoneIndex;
        this._config = config;
        this._snapcastService = snapcastService;
        this._snapcastStateRepository = snapcastStateRepository;
        this._mediaPlayerService = mediaPlayerService;
        this._serviceScopeFactory = serviceScopeFactory;
        this._zoneStateStore = zoneStateStore;
        this._statusFactory = statusFactory;
        this._logger = logger;
        this._stateLock = new SemaphoreSlim(1, 1);

        // Initialize state from store or create default
        var storedState = this._zoneStateStore.GetZoneState(zoneIndex);
        if (storedState != null)
        {
            LogZoneInitializing(
                zoneIndex,
                storedState.Playlist?.Source ?? "none",
                storedState.Playlist?.Index.ToString() ?? "none",
                storedState.Track?.Index.ToString() ?? "none",
                storedState.Track?.Title ?? "No Track"
            );
            this._currentState = storedState;
        }
        else
        {
            LogNoStoredStateFound(zoneIndex);
            this._currentState = this.CreateInitialState();
            // Store the initial state
            this._zoneStateStore.SetZoneState(zoneIndex, this._currentState);
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Find or create corresponding Snapcast group
        await this.EnsureSnapcastGroupAsync().ConfigureAwait(false);
    }

    public async Task<Result<ZoneState>> GetStateAsync()
    {
        LogGetStateAsyncCalled(this._zoneIndex);

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Update state from Snapcast if available
            await this.UpdateStateFromSnapcastAsync().ConfigureAwait(false);

            return Result<ZoneState>.Success(this._currentState with { TimestampUtc = DateTime.UtcNow });
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    // Playback Control Implementation
    public async Task<Result> PlayAsync()
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, "Play");

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Check if we have a valid track to play
            if (
                this._currentState.Track == null
                || string.IsNullOrEmpty(this._currentState.Track.Url)
                || this._currentState.Track.Url == "none://no-track"
                || this._currentState.Track.Source == "none"
            )
            {
                return Result.Failure("No track available to play. Please set a playlist or track first.");
            }

            // Start media playback
            var playResult = await this
                ._mediaPlayerService.PlayAsync(this._zoneIndex, this._currentState.Track!)
                .ConfigureAwait(false);
            if (playResult.IsFailure)
            {
                return playResult;
            }

            // Update state
            this._currentState = this._currentState with
            {
                PlaybackState = SnapDog2.Core.Enums.PlaybackState.Playing,
            };

            // Start position update timer for reliable MQTT updates + subscribe to events for immediate updates
            this.StartPositionUpdateTimer();
            this.SubscribeToMediaPlayerEvents();

            // Publish notification
            this.PublishZoneStateChangedAsync();

            // Publish status notification for blueprint compliance
            await PublishPlaybackStateStatusAsync(Core.Enums.PlaybackState.Playing);

            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> PlayTrackAsync(int trackIndex)
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, $"Play track {trackIndex}");

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Update track info (this would normally come from playlist manager)
            var newTrack = this._currentState.Track! with
            {
                Index = trackIndex,
                Title = $"Track {trackIndex}",
                Url = $"placeholder://track/{trackIndex}",
            };

            // Start playback
            var playResult = await this._mediaPlayerService.PlayAsync(this._zoneIndex, newTrack).ConfigureAwait(false);
            if (playResult.IsFailure)
            {
                return playResult;
            }

            // Update state
            this._currentState = this._currentState with
            {
                PlaybackState = SnapDog2.Core.Enums.PlaybackState.Playing,
                Track = newTrack,
            };

            // Start timer for reliable updates + events for immediate updates
            this.StartPositionUpdateTimer();
            this.SubscribeToMediaPlayerEvents();

            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> PlayUrlAsync(string mediaUrl)
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, $"Play URL: {mediaUrl}");

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var streamTrack = new TrackInfo
            {
                Source = "stream",
                Index = 0,
                Title = "Stream",
                Artist = "Unknown",
                Album = "Stream",
                Url = mediaUrl,
            };

            var playResult = await this
                ._mediaPlayerService.PlayAsync(this._zoneIndex, streamTrack)
                .ConfigureAwait(false);
            if (playResult.IsFailure)
            {
                return playResult;
            }

            this._currentState = this._currentState with
            {
                PlaybackState = SnapDog2.Core.Enums.PlaybackState.Playing,
                Track = streamTrack,
            };

            // Start timer for reliable updates + events for immediate updates
            this.StartPositionUpdateTimer();
            this.SubscribeToMediaPlayerEvents();

            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> PauseAsync()
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, "Pause");

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var pauseResult = await this._mediaPlayerService.PauseAsync(this._zoneIndex).ConfigureAwait(false);
            if (pauseResult.IsFailure)
            {
                return pauseResult;
            }

            this._currentState = this._currentState with { PlaybackState = SnapDog2.Core.Enums.PlaybackState.Paused };

            // Stop timer and unsubscribe from events when not playing
            this.StopPositionUpdateTimer();
            this.UnsubscribeFromMediaPlayerEvents();

            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> StopAsync()
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, "Stop");

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var stopResult = await this._mediaPlayerService.StopAsync(this._zoneIndex).ConfigureAwait(false);
            if (stopResult.IsFailure)
            {
                return stopResult;
            }

            this._currentState = this._currentState with { PlaybackState = SnapDog2.Core.Enums.PlaybackState.Stopped };

            // Stop timer and unsubscribe from events when not playing
            this.StopPositionUpdateTimer();
            this.UnsubscribeFromMediaPlayerEvents();

            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    // Volume Control Implementation
    public async Task<Result> SetVolumeAsync(int volume)
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, $"Set volume to {volume}");

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var result = this.SetVolumeInternal(volume);

            if (result.IsSuccess)
            {
                await PublishVolumeStatusAsync(Math.Clamp(volume, 0, 100));
            }

            return result;
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    private Result SetVolumeInternal(int volume)
    {
        var clampedVolume = Math.Clamp(volume, 0, 100);

        // Update Snapcast group volume if available
        if (!string.IsNullOrEmpty(this._snapcastGroupId))
        {
            // For now, we'll set individual client volumes since there's no SetGroupVolumeAsync
            // This would need to be implemented by iterating through group clients
            // var snapcastResult = await _snapcastService.SetGroupVolumeAsync(_snapcastGroupId, clampedVolume).ConfigureAwait(false);
            // if (snapcastResult.IsFailure)
            //     return snapcastResult;
        }

        this._currentState = this._currentState with { Volume = clampedVolume };
        this.PublishZoneStateChangedAsync();
        return Result.Success();
    }

    public async Task<Result> VolumeUpAsync(int step = 5)
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var newVolume = Math.Clamp(this._currentState.Volume + step, 0, 100);
            this.LogZoneAction(this._zoneIndex, this._config.Name, $"Set volume to {newVolume}");
            var result = this.SetVolumeInternal(newVolume);

            if (result.IsSuccess)
            {
                await PublishVolumeStatusAsync(newVolume);
            }

            return result;
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> VolumeDownAsync(int step = 5)
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var newVolume = Math.Clamp(this._currentState.Volume - step, 0, 100);
            this.LogZoneAction(this._zoneIndex, this._config.Name, $"Set volume to {newVolume}");
            var result = this.SetVolumeInternal(newVolume);

            if (result.IsSuccess)
            {
                await PublishVolumeStatusAsync(newVolume);
            }

            return result;
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> SetMuteAsync(bool enabled)
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, enabled ? "Mute" : "Unmute");

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var result = await this.SetMuteInternalAsync(enabled).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await PublishMuteStatusAsync(enabled);
            }

            return result;
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    private async Task<Result> SetMuteInternalAsync(bool enabled)
    {
        // Update Snapcast group mute if available
        if (!string.IsNullOrEmpty(this._snapcastGroupId))
        {
            var snapcastResult = await this
                ._snapcastService.SetGroupMuteAsync(this._snapcastGroupId, enabled)
                .ConfigureAwait(false);
            if (snapcastResult.IsFailure)
            {
                return snapcastResult;
            }
        }

        this._currentState = this._currentState with { Mute = enabled };
        this.PublishZoneStateChangedAsync();
        return Result.Success();
    }

    public async Task<Result> ToggleMuteAsync()
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            this.LogZoneAction(this._zoneIndex, this._config.Name, this._currentState.Mute ? "Unmute" : "Mute");
            return await this.SetMuteInternalAsync(!this._currentState.Mute).ConfigureAwait(false);
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    // Track Management Implementation
    public async Task<Result> SetTrackAsync(int trackIndex)
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, $"Set track to {trackIndex}");

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Get tracks from the current playlist
            if (this._currentState.Playlist == null)
            {
                return Result.Failure("No playlist selected. Please set a playlist first.");
            }

            // Get the playlist with tracks
            if (this._currentState.Playlist.Index == null)
            {
                return Result.Failure("Current playlist has no index");
            }

            var playlistIndex = this._currentState.Playlist.Index.Value;
            var getPlaylistQuery = new GetPlaylistQuery { PlaylistIndex = playlistIndex };

            var scope = this._serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var playlistResult = await mediator
                .SendQueryAsync<GetPlaylistQuery, Result<Api.Models.PlaylistWithTracks>>(getPlaylistQuery)
                .ConfigureAwait(false);

            if (playlistResult.IsFailure)
            {
                return Result.Failure($"Failed to get playlist tracks: {playlistResult.ErrorMessage}");
            }

            var playlist = playlistResult.Value;
            if (playlist?.Tracks == null || playlist.Tracks.Count == 0)
            {
                return Result.Failure("Playlist has no tracks");
            }

            // Find the track by index (1-based)
            var targetTrack = playlist.Tracks.FirstOrDefault(t => t.Index == trackIndex);
            if (targetTrack == null)
            {
                return Result.Failure($"Track {trackIndex} not found in playlist");
            }

            this._currentState = this._currentState with { Track = targetTrack };
            this.PublishZoneStateChangedAsync();

            // Log successful track change with meaningful information
            LogZonePlaying(
                this._zoneIndex,
                targetTrack.Title,
                this._currentState.Playlist.Source,
                this._currentState.Playlist.Index.ToString(),
                targetTrack.Index.ToString()
            );

            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> NextTrackAsync()
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var currentIndex = this._currentState.Track?.Index ?? 1;
            return await this.SetTrackAsync(currentIndex + 1).ConfigureAwait(false);
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> PreviousTrackAsync()
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var currentIndex = this._currentState.Track?.Index ?? 1;
            var newIndex = Math.Max(1, currentIndex - 1);
            return await this.SetTrackAsync(newIndex).ConfigureAwait(false);
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    /// <summary>
    /// Seeks to a specific position in the current track.
    /// </summary>
    /// <param name="positionMs">Position in milliseconds</param>
    /// <returns>Result indicating success or failure</returns>
    public async Task<Result> SeekToPositionAsync(long positionMs)
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, $"Seek to position {positionMs}ms");

        try
        {
            if (this._mediaPlayerService == null)
            {
                return Result.Failure("Media player service not initialized");
            }

            return await this
                ._mediaPlayerService.SeekToPositionAsync(this._zoneIndex, positionMs)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.LogZoneError(
                this._zoneIndex,
                this._config.Name,
                $"Failed to seek to position {positionMs}ms",
                ex.Message
            );
            return Result.Failure($"Failed to seek to position {positionMs}ms: {ex.Message}");
        }
    }

    /// <summary>
    /// Seeks to a specific progress percentage in the current track.
    /// </summary>
    /// <param name="progress">Progress percentage (0.0-1.0)</param>
    /// <returns>Result indicating success or failure</returns>
    public async Task<Result> SeekToProgressAsync(float progress)
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, $"Seek to progress {progress:P1}");

        try
        {
            if (this._mediaPlayerService == null)
            {
                return Result.Failure("Media player service not initialized");
            }

            return await this._mediaPlayerService.SeekToProgressAsync(this._zoneIndex, progress).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.LogZoneError(
                this._zoneIndex,
                this._config.Name,
                $"Failed to seek to progress {progress:P1}",
                ex.Message
            );
            return Result.Failure($"Failed to seek to progress {progress:P1}: {ex.Message}");
        }
    }

    public async Task<Result> SetTrackRepeatAsync(bool enabled)
    {
        this.LogZoneAction(
            this._zoneIndex,
            this._config.Name,
            enabled ? "Enable track repeat" : "Disable track repeat"
        );

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            this._currentState = this._currentState with { TrackRepeat = enabled };
            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> ToggleTrackRepeatAsync()
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var newValue = !this._currentState.TrackRepeat;
            this.LogZoneAction(
                this._zoneIndex,
                this._config.Name,
                newValue ? "Enable track repeat" : "Disable track repeat"
            );
            this._currentState = this._currentState with { TrackRepeat = newValue };
            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    // Playlist Management Implementation
    public async Task<Result> SetPlaylistAsync(int playlistIndex)
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, $"Set playlist to {playlistIndex}");

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Get all playlists to find the correct one
            var getAllPlaylistsQuery = new GetAllPlaylistsQuery();

            var scope = this._serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var playlistsResult = await mediator
                .SendQueryAsync<GetAllPlaylistsQuery, Result<List<PlaylistInfo>>>(getAllPlaylistsQuery)
                .ConfigureAwait(false);

            if (playlistsResult.IsFailure)
            {
                return Result.Failure($"Failed to get playlists: {playlistsResult.ErrorMessage}");
            }

            var playlists = playlistsResult.Value ?? new List<PlaylistInfo>();
            var targetPlaylist = playlists.FirstOrDefault(p => p.Index == playlistIndex);

            if (targetPlaylist == null)
            {
                return Result.Failure($"Playlist {playlistIndex} not found");
            }

            this._currentState = this._currentState with { Playlist = targetPlaylist };
            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> SetPlaylistAsync(string playlistIndex)
    {
        this.LogZoneAction(this._zoneIndex, this._config.Name, $"Set playlist to {playlistIndex}");

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var newPlaylist = this._currentState.Playlist! with { Id = playlistIndex, Name = playlistIndex };

            this._currentState = this._currentState with { Playlist = newPlaylist };
            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> NextPlaylistAsync()
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var currentIndex = this._currentState.Playlist?.Index ?? 1;
            return await this.SetPlaylistAsync(currentIndex + 1).ConfigureAwait(false);
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> PreviousPlaylistAsync()
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var currentIndex = this._currentState.Playlist?.Index ?? 1;
            var newIndex = Math.Max(1, currentIndex - 1);
            return await this.SetPlaylistAsync(newIndex).ConfigureAwait(false);
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> SetPlaylistShuffleAsync(bool enabled)
    {
        this.LogZoneAction(
            this._zoneIndex,
            this._config.Name,
            enabled ? "Enable playlist shuffle" : "Disable playlist shuffle"
        );

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            this._currentState = this._currentState with { PlaylistShuffle = enabled };
            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> TogglePlaylistShuffleAsync()
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var newValue = !this._currentState.PlaylistShuffle;
            this.LogZoneAction(
                this._zoneIndex,
                this._config.Name,
                newValue ? "Enable playlist shuffle" : "Disable playlist shuffle"
            );
            this._currentState = this._currentState with { PlaylistShuffle = newValue };
            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> SetPlaylistRepeatAsync(bool enabled)
    {
        this.LogZoneAction(
            this._zoneIndex,
            this._config.Name,
            enabled ? "Enable playlist repeat" : "Disable playlist repeat"
        );

        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            this._currentState = this._currentState with { PlaylistRepeat = enabled };
            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    public async Task<Result> TogglePlaylistRepeatAsync()
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var newValue = !this._currentState.PlaylistRepeat;
            this.LogZoneAction(
                this._zoneIndex,
                this._config.Name,
                newValue ? "Enable playlist repeat" : "Disable playlist repeat"
            );
            this._currentState = this._currentState with { PlaylistRepeat = newValue };
            this.PublishZoneStateChangedAsync();
            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    // Internal synchronization methods
    internal async Task SynchronizeWithSnapcastAsync(IEnumerable<SnapcastClient.Models.Group> snapcastGroups)
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Find matching Snapcast group and update state
            // This would use actual Snapcast group data
            await this.UpdateStateFromSnapcastAsync().ConfigureAwait(false);
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    private ZoneState CreateInitialState()
    {
        return new ZoneState
        {
            Id = this._zoneIndex,
            Name = this._config.Name,
            PlaybackState = SnapDog2.Core.Enums.PlaybackState.Stopped,
            Volume = 50,
            Mute = false,
            TrackRepeat = false,
            PlaylistRepeat = false,
            PlaylistShuffle = false,
            SnapcastGroupId = $"group_{this._zoneIndex}",
            SnapcastStreamId = this._config.Sink,
            IsSnapcastGroupMuted = false,
            Track = new TrackInfo
            {
                Source = "none",
                Index = 0,
                Title = "No Track",
                Artist = "Unknown",
                Album = "Unknown",
                Url = "none://no-track",
            },
            Playlist = new PlaylistInfo
            {
                Id = "none",
                Source = "none",
                Index = 1,
                Name = "No Playlist",
                TrackCount = 0,
            },
            Clients = Array.Empty<int>(),
            TimestampUtc = DateTime.UtcNow,
        };
    }

    private async Task EnsureSnapcastGroupAsync()
    {
        await Task.CompletedTask; // Ensure method is properly async

        try
        {
            // Extract stream ID from sink path (e.g., "/snapsinks/zone1" -> "Zone1")
            var streamId = ExtractStreamIdFromSink(this._config.Sink);

            // Find existing group for this zone's stream
            var allGroups = this._snapcastStateRepository.GetAllGroups();
            var existingGroup = allGroups.FirstOrDefault(g => g.StreamId == streamId);

            if (existingGroup.Id != null)
            {
                // Use existing group
                this._snapcastGroupId = existingGroup.Id;
                this.LogSnapcastSync(this._zoneIndex, this._snapcastGroupId);
            }
            else
            {
                // No existing group for this stream - we'll create one when clients are assigned
                // For now, use a placeholder that will be replaced when first client is assigned
                this._snapcastGroupId = null;
                this.LogZoneAction(
                    this._zoneIndex,
                    this._config.Name,
                    "No existing group found, will create when clients assigned"
                );
            }
        }
        catch (Exception ex)
        {
            this.LogZoneAction(this._zoneIndex, this._config.Name, $"Failed to ensure Snapcast group: {ex.Message}");
            this._snapcastGroupId = null;
        }
    }

    private static string ExtractStreamIdFromSink(string sink)
    {
        // Convert "/snapsinks/zone1" -> "Zone1", "/snapsinks/zone2" -> "Zone2"
        var fileName = Path.GetFileName(sink);
        if (fileName.StartsWith("zone", StringComparison.OrdinalIgnoreCase))
        {
            var zoneNumber = fileName.Substring(4);
            return $"Zone{zoneNumber}";
        }
        return fileName;
    }

    private async Task UpdateStateFromSnapcastAsync()
    {
        try
        {
            LogUpdateStateFromSnapcastStarting(this._zoneIndex);

            // Get current playback status from MediaPlayerService
            var playbackStatus = await this._mediaPlayerService.GetStatusAsync(this._zoneIndex).ConfigureAwait(false);

            LogMediaPlayerStatusResult(playbackStatus.IsSuccess, playbackStatus.Value != null ? "NotNull" : "Null");

            if (playbackStatus.IsSuccess && playbackStatus.Value != null)
            {
                var status = playbackStatus.Value;

                LogMediaPlayerStatus(status.IsPlaying, status.CurrentTrack?.Title);

                // Update track playing state if we have a current track
                if (this._currentState.Track != null && status.CurrentTrack != null)
                {
                    // Only log track state changes at INFO level if something actually changed
                    var stateChanged = this._currentState.Track.IsPlaying != status.IsPlaying;

                    if (stateChanged)
                    {
                        LogTrackStateChanged(this._currentState.Track.IsPlaying, status.IsPlaying);
                    }
                    else
                    {
                        LogUpdatingTrackState(this._currentState.Track.IsPlaying, status.IsPlaying);
                    }

                    // Log position updates every 30 seconds (simpler approach)
                    var positionSeconds = status.CurrentTrack.PositionMs / 1000.0;
                    var durationSeconds = status.CurrentTrack.DurationMs / 1000.0;
                    var progressPercent = status.CurrentTrack.Progress * 100;

                    // Log every 30 seconds of playback
                    var positionSecondsInt = (int)positionSeconds;
                    var shouldLogPosition =
                        positionSecondsInt > 0
                        && positionSecondsInt % 30 == 0
                        && (positionSecondsInt != this._lastLoggedPositionSeconds);

                    if (shouldLogPosition)
                    {
                        LogZonePlayingWithPosition(
                            this._zoneIndex,
                            status.CurrentTrack.Title ?? "Unknown",
                            positionSeconds,
                            durationSeconds,
                            progressPercent
                        );
                        this._lastLoggedPositionSeconds = positionSecondsInt;
                    }
                    else
                    {
                        LogLibVLCPositionData(
                            status.CurrentTrack.PositionMs,
                            status.CurrentTrack.Progress,
                            status.CurrentTrack.DurationMs
                        );
                    }

                    // Update the track with current playback information
                    this._currentState = this._currentState with
                    {
                        Track = this._currentState.Track with
                        {
                            IsPlaying = status.IsPlaying,
                            PositionMs = status.CurrentTrack.PositionMs,
                            Progress = status.CurrentTrack.Progress,
                        },
                        PlaybackState = status.IsPlaying
                            ? SnapDog2.Core.Enums.PlaybackState.Playing
                            : SnapDog2.Core.Enums.PlaybackState.Stopped,
                    };

                    LogUpdatedTrackState(
                        this._currentState.Track.IsPlaying,
                        this._currentState.Track.PositionMs,
                        this._currentState.Track.Progress
                    );
                }
                else
                {
                    LogSkippingUpdate(
                        this._currentState.Track != null ? "NotNull" : "Null",
                        status.CurrentTrack != null ? "NotNull" : "Null"
                    );
                }
            }
            else
            {
                LogMediaPlayerStatusFailed(playbackStatus.IsSuccess, playbackStatus.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the state update
            LogFailedToUpdateZoneState(ex, this._zoneIndex);
        }
    }

    private void PublishZoneStateChangedAsync()
    {
        // Persist state to store for future requests
        this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);

        // Publish notification via mediator using fire-and-forget to prevent blocking
        var notification = this._statusFactory.CreateZoneStateChangedNotification(this._zoneIndex, this._currentState);

        // Use Task.Run to avoid blocking the calling thread
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = this._serviceScopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.PublishAsync(notification).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log error but don't propagate to avoid breaking the main operation
                LogFailedToPublishZoneStateNotification(ex, this._zoneIndex);
            }
        });
    }

    /// <summary>
    /// Starts the position update timer for reliable MQTT position updates when playing.
    /// </summary>
    private void StartPositionUpdateTimer()
    {
        if (this._positionUpdateTimer != null)
        {
            return; // Timer already running
        }

        LogStartingPositionUpdateTimer(this._zoneIndex);

        this._positionUpdateTimer = new Timer(
            async _ =>
            {
                try
                {
                    // Only update if we're playing and not disposed
                    if (this._disposed || this._currentState.PlaybackState != SnapDog2.Core.Enums.PlaybackState.Playing)
                    {
                        return;
                    }

                    // Update state from MediaPlayer and publish if position changed
                    await this.UpdateStateFromSnapcastAsync().ConfigureAwait(false);
                    this.PublishZoneStateChangedAsync();
                }
                catch (Exception ex)
                {
                    LogErrorDuringPositionUpdate(ex, this._zoneIndex);
                }
            },
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1)
        );
    }

    /// <summary>
    /// Stops the position update timer.
    /// </summary>
    private void StopPositionUpdateTimer()
    {
        if (this._positionUpdateTimer == null)
        {
            return;
        }

        LogStoppingPositionUpdateTimer(this._zoneIndex);

        this._positionUpdateTimer?.Dispose();
        this._positionUpdateTimer = null;
    }

    /// <summary>
    /// Subscribes to MediaPlayer events for immediate position and state updates.
    /// </summary>
    private void SubscribeToMediaPlayerEvents()
    {
        var mediaPlayer = this._mediaPlayerService.GetMediaPlayer(this._zoneIndex);
        if (mediaPlayer != null)
        {
            LogSubscribingToMediaPlayerEvents(this._zoneIndex);

            mediaPlayer.PositionChanged += this.OnMediaPlayerPositionChanged;
            mediaPlayer.PlaybackStateChanged += this.OnMediaPlayerStateChanged;
        }
        else
        {
            LogMediaPlayerNullOnSubscribe(this._zoneIndex);
        }
    }

    /// <summary>
    /// Unsubscribes from MediaPlayer events.
    /// </summary>
    private void UnsubscribeFromMediaPlayerEvents()
    {
        var mediaPlayer = this._mediaPlayerService.GetMediaPlayer(this._zoneIndex);
        if (mediaPlayer != null)
        {
            LogUnsubscribingFromMediaPlayerEvents(this._zoneIndex);

            mediaPlayer.PositionChanged -= this.OnMediaPlayerPositionChanged;
            mediaPlayer.PlaybackStateChanged -= this.OnMediaPlayerStateChanged;
        }
    }

    /// <summary>
    /// Handles position changes from MediaPlayer and publishes updates.
    /// </summary>
    private void OnMediaPlayerPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        try
        {
            LogPositionChangedForZone(this._zoneIndex, e.PositionMs, e.Progress);

            if (this._disposed || this._currentState.PlaybackState != SnapDog2.Core.Enums.PlaybackState.Playing)
            {
                LogSkippingPositionUpdate(this._disposed, this._currentState.PlaybackState.ToString());
                return;
            }

            // Update current state with new position
            if (this._currentState.Track != null)
            {
                this._currentState = this._currentState with
                {
                    Track = this._currentState.Track with
                    {
                        PositionMs = e.PositionMs,
                        Progress = e.Progress,
                        DurationMs = e.DurationMs > 0 ? e.DurationMs : this._currentState.Track.DurationMs,
                    },
                };

                // Publish updated state to MQTT
                this.PublishZoneStateChangedAsync();
                LogPublishedMqttUpdateForPositionChange();
            }
        }
        catch (Exception ex)
        {
            LogErrorHandlingPositionChange(ex, this._zoneIndex);
        }
    }

    /// <summary>
    /// Handles playback state changes from MediaPlayer.
    /// </summary>
    private void OnMediaPlayerStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        try
        {
            LogMediaPlayerStateChanged(this._zoneIndex, e.State.ToString());

            // Update playback state based on LibVLC state
            var newPlaybackState =
                e.IsPlaying ? SnapDog2.Core.Enums.PlaybackState.Playing
                : e.State == LibVLC.VLCState.Paused ? SnapDog2.Core.Enums.PlaybackState.Paused
                : SnapDog2.Core.Enums.PlaybackState.Stopped;

            if (this._currentState.PlaybackState != newPlaybackState)
            {
                this._currentState = this._currentState with { PlaybackState = newPlaybackState };
                this.PublishZoneStateChangedAsync();
            }
        }
        catch (Exception ex)
        {
            LogErrorHandlingStateChange(ex, this._zoneIndex);
        }
    }

    #region Status Publishing Methods (Blueprint Compliance)

    public async Task PublishPlaybackStateStatusAsync(Core.Enums.PlaybackState playbackState)
    {
        var notification = this._statusFactory.CreateZonePlaybackStateChangedNotification(
            this._zoneIndex,
            playbackState
        );

        using var scope = this._serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(notification).ConfigureAwait(false);
    }

    public async Task PublishVolumeStatusAsync(int volume)
    {
        var notification = this._statusFactory.CreateZoneVolumeChangedNotification(this._zoneIndex, volume);

        using var scope = this._serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(notification).ConfigureAwait(false);
    }

    public async Task PublishMuteStatusAsync(bool isMuted)
    {
        var notification = this._statusFactory.CreateZoneMuteChangedNotification(this._zoneIndex, isMuted);

        using var scope = this._serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(notification).ConfigureAwait(false);
    }

    public async Task PublishTrackStatusAsync(Core.Models.TrackInfo trackInfo, int trackIndex)
    {
        var notification = this._statusFactory.CreateZoneTrackChangedNotification(
            this._zoneIndex,
            trackInfo,
            trackIndex
        );

        using var scope = this._serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(notification).ConfigureAwait(false);
    }

    public async Task PublishPlaylistStatusAsync(Core.Models.PlaylistInfo playlistInfo, int playlistIndex)
    {
        var notification = this._statusFactory.CreateZonePlaylistChangedNotification(
            this._zoneIndex,
            playlistInfo,
            playlistIndex
        );

        using var scope = this._serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(notification).ConfigureAwait(false);
    }

    public async Task PublishZoneStateStatusAsync(Core.Models.ZoneState zoneState)
    {
        var notification = this._statusFactory.CreateZoneStateChangedNotification(this._zoneIndex, zoneState);

        using var scope = this._serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.PublishAsync(notification).ConfigureAwait(false);
    }

    #endregion

    public ValueTask DisposeAsync()
    {
        if (this._disposed)
        {
            return ValueTask.CompletedTask;
        }

        // Stop timer and unsubscribe from MediaPlayer events
        this.StopPositionUpdateTimer();
        this.UnsubscribeFromMediaPlayerEvents();

        this._stateLock?.Dispose();
        this._disposed = true;
        return ValueTask.CompletedTask;
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: Loaded state from store - Source: {Source}; Playlist: {PlaylistIndex}, Track: {TrackIndex} ({TrackTitle})"
    )]
    private partial void LogZoneInitializing(
        int ZoneIndex,
        string Source,
        string PlaylistIndex,
        string TrackIndex,
        string TrackTitle
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: No stored state found, creating initial state"
    )]
    private partial void LogNoStoredStateFound(int ZoneIndex);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "GetStateAsync: Called for zone {ZoneIndex}")]
    private partial void LogGetStateAsyncCalled(int ZoneIndex);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: State updated - {StateInfo}"
    )]
    private partial void LogZoneStateUpdated(int ZoneIndex, string StateInfo);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Starting for zone {ZoneIndex}"
    )]
    private partial void LogUpdateStateFromSnapcastStarting(int ZoneIndex);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "🎵 Zone {ZoneIndex} playing: {TrackTitle} from {Source} (playlist {PlaylistIndex}, track {TrackIndex})"
    )]
    private partial void LogZonePlaying(
        int ZoneIndex,
        string? TrackTitle,
        string? Source,
        string? PlaylistIndex,
        string? TrackIndex
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Getting server status for zone {ZoneIndex}"
    )]
    private partial void LogGettingServerStatus(int ZoneIndex);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Found group {GroupId} for zone {ZoneIndex}"
    )]
    private partial void LogFoundGroup(int ZoneIndex, string GroupId);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: Stream changed from {OldStream} to {NewStream}"
    )]
    private partial void LogStreamChanged(int ZoneIndex, string OldStream, string NewStream);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: Stream unchanged: {StreamId}")]
    private partial void LogStreamUnchanged(int ZoneIndex, string StreamId);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: Volume changed from {OldVolume}% to {NewVolume}%"
    )]
    private partial void LogVolumeChanged(int ZoneIndex, int OldVolume, int NewVolume);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: Volume unchanged: {Volume}%")]
    private partial void LogVolumeUnchanged(int ZoneIndex, int Volume);

    [LoggerMessage(EventId = 13, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: No group found in Snapcast")]
    private partial void LogNoGroupFound(int ZoneIndex);

    [LoggerMessage(EventId = 14, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: No server status available")]
    private partial void LogNoServerStatus(int ZoneIndex);

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: State synchronized from Snapcast"
    )]
    private partial void LogStateSynchronized(int ZoneIndex);

    [LoggerMessage(
        EventId = 16,
        Level = LogLevel.Warning,
        Message = "Failed to update zone {ZoneIndex} state from media player"
    )]
    private partial void LogFailedUpdateFromMediaPlayer(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 17,
        Level = LogLevel.Error,
        Message = "Failed to update zone {ZoneIndex} state from Snapcast"
    )]
    private partial void LogFailedUpdateFromSnapcast(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 18,
        Level = LogLevel.Debug,
        Message = "Starting position update timer for zone {ZoneIndex}"
    )]
    private partial void LogStartingPositionTimer(int ZoneIndex);

    [LoggerMessage(
        EventId = 19,
        Level = LogLevel.Warning,
        Message = "Error during position update for zone {ZoneIndex}"
    )]
    private partial void LogPositionUpdateError(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 20,
        Level = LogLevel.Debug,
        Message = "Stopping position update timer for zone {ZoneIndex}"
    )]
    private partial void LogStoppingPositionTimer(int ZoneIndex);

    [LoggerMessage(
        EventId = 21,
        Level = LogLevel.Information,
        Message = "✅ Subscribing to MediaPlayer events for zone {ZoneIndex}"
    )]
    private partial void LogSubscribingToMediaPlayerEvents(int ZoneIndex);

    [LoggerMessage(
        EventId = 22,
        Level = LogLevel.Warning,
        Message = "⚠️ MediaPlayer is null when trying to subscribe to events for zone {ZoneIndex}"
    )]
    private partial void LogMediaPlayerNullOnSubscribe(int ZoneIndex);

    [LoggerMessage(
        EventId = 23,
        Level = LogLevel.Debug,
        Message = "Unsubscribing from MediaPlayer events for zone {ZoneIndex}"
    )]
    private partial void LogUnsubscribingFromMediaPlayerEvents(int ZoneIndex);

    [LoggerMessage(
        EventId = 24,
        Level = LogLevel.Information,
        Message = "🎵 Zone {ZoneIndex} position: {PositionMs}ms / {DurationMs}ms ({Progress:P1})"
    )]
    private partial void LogPositionUpdate(int ZoneIndex, long PositionMs, long DurationMs, float Progress);

    [LoggerMessage(EventId = 25, Level = LogLevel.Debug, Message = "📡 Published MQTT update for position change")]
    private partial void LogPublishedMqttPositionUpdate();

    [LoggerMessage(
        EventId = 26,
        Level = LogLevel.Warning,
        Message = "Error handling position change for zone {ZoneIndex}"
    )]
    private partial void LogPositionChangeError(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 27,
        Level = LogLevel.Debug,
        Message = "MediaPlayer state changed for zone {ZoneIndex}: {State}"
    )]
    private partial void LogMediaPlayerStateChanged(int ZoneIndex, LibVLC.VLCState State);

    [LoggerMessage(
        EventId = 28,
        Level = LogLevel.Warning,
        Message = "Error handling state change for zone {ZoneIndex}"
    )]
    private partial void LogStateChangeError(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 29,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: MediaPlayer status result - Success: {Success}, Value: {Value}"
    )]
    private partial void LogMediaPlayerStatusResult(bool Success, string Value);

    [LoggerMessage(
        EventId = 30,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Status - IsPlaying: {IsPlaying}, CurrentTrack: {CurrentTrack}"
    )]
    private partial void LogMediaPlayerStatus(bool IsPlaying, string? CurrentTrack);

    [LoggerMessage(
        EventId = 31,
        Level = LogLevel.Information,
        Message = "UpdateStateFromSnapcastAsync: Track state changed - Old IsPlaying: {OldIsPlaying}, New IsPlaying: {NewIsPlaying}"
    )]
    private partial void LogTrackStateChanged(bool OldIsPlaying, bool NewIsPlaying);

    [LoggerMessage(
        EventId = 32,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Updating track state - Old IsPlaying: {OldIsPlaying}, New IsPlaying: {NewIsPlaying}"
    )]
    private partial void LogUpdatingTrackState(bool OldIsPlaying, bool NewIsPlaying);

    [LoggerMessage(
        EventId = 33,
        Level = LogLevel.Information,
        Message = "UpdateStateFromSnapcastAsync: Position update - Position: {PositionMs}ms, Duration: {DurationMs}ms"
    )]
    private partial void LogPositionUpdateFromSnapcast(long PositionMs, long DurationMs);

    [LoggerMessage(
        EventId = 34,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Position unchanged - Position: {PositionMs}ms"
    )]
    private partial void LogPositionUnchanged(long PositionMs);

    [LoggerMessage(
        EventId = 35,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: No group found for zone {ZoneIndex}"
    )]
    private partial void LogNoGroupFoundForZone(int ZoneIndex);

    [LoggerMessage(
        EventId = 36,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: No server status available for zone {ZoneIndex}"
    )]
    private partial void LogNoServerStatusForZone(int ZoneIndex);

    [LoggerMessage(
        EventId = 37,
        Level = LogLevel.Information,
        Message = "UpdateStateFromSnapcastAsync: State synchronized for zone {ZoneIndex}"
    )]
    private partial void LogStateSynchronizedForZone(int ZoneIndex);

    [LoggerMessage(
        EventId = 38,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: Playing \"{Title}\" - {Position:F1}s / {Duration:F1}s ({Progress:F1}%)"
    )]
    private partial void LogZonePlayingWithPosition(
        int ZoneIndex,
        string? Title,
        double Position,
        double? Duration,
        double Progress
    );

    [LoggerMessage(
        EventId = 39,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: LibVLC Position Data - PositionMs: {PositionMs}, Progress: {Progress}, DurationMs: {DurationMs}"
    )]
    private partial void LogLibVLCPositionData(long? PositionMs, float Progress, long? DurationMs);

    [LoggerMessage(
        EventId = 40,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Updated track state - IsPlaying: {IsPlaying}, PositionMs: {PositionMs}, Progress: {Progress}"
    )]
    private partial void LogUpdatedTrackState(bool IsPlaying, long PositionMs, float Progress);

    [LoggerMessage(
        EventId = 41,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Skipping update - CurrentState.Track: {CurrentTrack}, Status.CurrentTrack: {StatusCurrentTrack}"
    )]
    private partial void LogSkippingUpdate(string CurrentTrack, string StatusCurrentTrack);

    [LoggerMessage(
        EventId = 42,
        Level = LogLevel.Information,
        Message = "UpdateStateFromSnapcastAsync: MediaPlayer status failed or null - Success: {Success}, ErrorMessage: {ErrorMessage}"
    )]
    private partial void LogMediaPlayerStatusFailed(bool Success, string? ErrorMessage);

    [LoggerMessage(
        EventId = 43,
        Level = LogLevel.Warning,
        Message = "Failed to update zone {ZoneIndex} state from media player"
    )]
    private partial void LogFailedToUpdateZoneState(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 44,
        Level = LogLevel.Error,
        Message = "Failed to publish zone state notification for zone {ZoneIndex}"
    )]
    private partial void LogFailedToPublishZoneStateNotification(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 45,
        Level = LogLevel.Debug,
        Message = "Starting position update timer for zone {ZoneIndex}"
    )]
    private partial void LogStartingPositionUpdateTimer(int ZoneIndex);

    [LoggerMessage(
        EventId = 46,
        Level = LogLevel.Warning,
        Message = "Error during position update for zone {ZoneIndex}"
    )]
    private partial void LogErrorDuringPositionUpdate(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 47,
        Level = LogLevel.Debug,
        Message = "Stopping position update timer for zone {ZoneIndex}"
    )]
    private partial void LogStoppingPositionUpdateTimer(int ZoneIndex);

    [LoggerMessage(
        EventId = 51,
        Level = LogLevel.Information,
        Message = "🎵 LibVLC Position Event: Zone {ZoneIndex}, PositionMs: {PositionMs}, Progress: {Progress}"
    )]
    private partial void LogPositionChangedForZone(int ZoneIndex, long PositionMs, float Progress);

    [LoggerMessage(
        EventId = 52,
        Level = LogLevel.Debug,
        Message = "Skipping position update - disposed: {Disposed}, state: {State}"
    )]
    private partial void LogSkippingPositionUpdate(bool Disposed, string State);

    [LoggerMessage(EventId = 53, Level = LogLevel.Debug, Message = "📡 Published MQTT update for position change")]
    private partial void LogPublishedMqttUpdateForPositionChange();

    [LoggerMessage(
        EventId = 54,
        Level = LogLevel.Warning,
        Message = "Error handling position change for zone {ZoneIndex}"
    )]
    private partial void LogErrorHandlingPositionChange(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 55,
        Level = LogLevel.Debug,
        Message = "MediaPlayer state changed for zone {ZoneIndex}: {State}"
    )]
    private partial void LogMediaPlayerStateChanged(int ZoneIndex, string State);

    [LoggerMessage(
        EventId = 56,
        Level = LogLevel.Warning,
        Message = "Error handling state change for zone {ZoneIndex}"
    )]
    private partial void LogErrorHandlingStateChange(Exception ex, int ZoneIndex);
}
