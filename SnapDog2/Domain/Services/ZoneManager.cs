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
namespace SnapDog2.Domain.Services;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Audio;
using SnapDog2.Server.Playlists.Queries;
using SnapDog2.Server.Zones.Notifications;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;
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
    private readonly IOptions<SnapDogConfiguration> _configuration = configuration;
    private readonly List<ZoneConfig> _zoneConfigs = configuration.Value.Zones;
    private readonly ConcurrentDictionary<int, IZoneService> _zones = new ConcurrentDictionary<int, IZoneService>();
    private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);
    private bool _isInitialized;
    private bool _disposed;

    [LoggerMessage(
        EventId = 6553,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Initializing ZoneManager with {ZoneCount} configured zones"
    )]
    private partial void LogInitializing(int zoneCount);

    [LoggerMessage(
        EventId = 6554,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} ({ZoneName}) initialized successfully"
    )]
    private partial void LogZoneInitialized(int zoneIndex, string zoneName);

    [LoggerMessage(
        EventId = 6555,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Zone {ZoneIndex} not found"
    )]
    private partial void LogZoneNotFound(int zoneIndex);

    [LoggerMessage(
        EventId = 6556,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to initialize zone {ZoneIndex}: {Error}"
    )]
    private partial void LogZoneInitializationFailed(int zoneIndex, string error);

    [LoggerMessage(
        EventId = 6557,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting zone {ZoneIndex}"
    )]
    private partial void LogGettingZone(int zoneIndex);

    [LoggerMessage(
        EventId = 6558,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Getting all zones"
    )]
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
            for (var i = 0; i < this._zoneConfigs.Count; i++)
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
                        this._logger,
                        this._configuration
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
        return await this.GetZoneStateAsync(zoneIndex);
    }

    public async Task<Result<List<ZoneState>>> GetAllZonesAsync(CancellationToken cancellationToken = default)
    {
        return await this.GetAllZoneStatesAsync();
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
    private readonly IOptions<SnapDogConfiguration> _configuration;
    private readonly string _instanceId = Guid.NewGuid().ToString("N")[..8]; // Short instance ID for debugging
    private readonly SemaphoreSlim _stateLock;
    private ZoneState _currentState;
    private string? _snapcastGroupId;
    private bool _disposed;
    private Timer? _positionUpdateTimer;

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
        ILogger logger,
        IOptions<SnapDogConfiguration> configuration
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
        this._configuration = configuration;
        this._stateLock = new SemaphoreSlim(1, 1);

        // Initialize state from store or create default
        var storedState = this._zoneStateStore.GetZoneState(zoneIndex);
        if (storedState != null)
        {
            this.LogZoneInitializing(
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
            this.LogNoStoredStateFound(zoneIndex);
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
        this.LogGetStateAsyncCalled(this._zoneIndex);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            await this._stateLock.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Result<ZoneState>.Failure($"Zone {this._zoneIndex} state retrieval timed out waiting for lock");
        }

        try
        {
            // Update state from Snapcast if available with timeout
            var updateTask = this.UpdateStateFromSnapcastAsync();
            var completedTask = await Task.WhenAny(updateTask, Task.Delay(3000, cts.Token)).ConfigureAwait(false);

            if (completedTask != updateTask)
            {
                this.LogGetStateAsyncTimeout(this._zoneIndex);
                // Return current state without update if timeout
                return Result<ZoneState>.Success(this._currentState with { TimestampUtc = DateTime.UtcNow });
            }

            return Result<ZoneState>.Success(this._currentState with { TimestampUtc = DateTime.UtcNow });
        }
        catch (OperationCanceledException)
        {
            return Result<ZoneState>.Failure($"Zone {this._zoneIndex} state retrieval timed out");
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
                PlaybackState = SnapDog2.Shared.Enums.PlaybackState.Playing,
            };

            // Start position update timer for reliable MQTT updates + subscribe to events for immediate updates
            this.StartPositionUpdateTimer();
            this.SubscribeToMediaPlayerEvents();

            // Publish notification
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);

            // Publish status notification for blueprint compliance
            await this.PublishPlaybackStateStatusAsync(SnapDog2.Shared.Enums.PlaybackState.Playing);

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
                PlaybackState = SnapDog2.Shared.Enums.PlaybackState.Playing,
                Track = newTrack,
            };

            // Start timer for reliable updates + events for immediate updates
            this.StartPositionUpdateTimer();
            this.SubscribeToMediaPlayerEvents();

            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
                PlaybackState = SnapDog2.Shared.Enums.PlaybackState.Playing,
                Track = streamTrack,
            };

            // Start timer for reliable updates + events for immediate updates
            this.StartPositionUpdateTimer();
            this.SubscribeToMediaPlayerEvents();

            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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

            this._currentState = this._currentState with { PlaybackState = SnapDog2.Shared.Enums.PlaybackState.Paused };

            // Stop timer and unsubscribe from events when not playing
            this.StopPositionUpdateTimer();
            this.UnsubscribeFromMediaPlayerEvents();

            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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

            this._currentState = this._currentState with { PlaybackState = SnapDog2.Shared.Enums.PlaybackState.Stopped };

            // Stop timer and unsubscribe from events when not playing
            this.StopPositionUpdateTimer();
            this.UnsubscribeFromMediaPlayerEvents();

            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
                await this.PublishVolumeStatusAsync(Math.Clamp(volume, 0, 100));
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
        this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
                await this.PublishVolumeStatusAsync(newVolume);
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
                await this.PublishVolumeStatusAsync(newVolume);
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
                await this.PublishMuteStatusAsync(enabled);
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
        this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);

            // Log successful track change with meaningful information
            this.LogZonePlaying(
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
            var nextIndex = currentIndex + 1;

            // Check if we're currently playing
            var wasPlaying = this._currentState.PlaybackState == SnapDog2.Shared.Enums.PlaybackState.Playing;

            // Set the track (inline implementation to avoid nested lock)
            var setResult = await this.SetTrackInternalAsync(nextIndex).ConfigureAwait(false);
            if (setResult.IsFailure)
            {
                return setResult;
            }

            // If we were playing, start playing the new track (inline implementation to avoid nested lock)
            if (wasPlaying)
            {
                var targetTrack = this._currentState.Track!;
                var playResult = await this._mediaPlayerService.PlayAsync(this._zoneIndex, targetTrack).ConfigureAwait(false);
                if (playResult.IsFailure)
                {
                    return playResult;
                }

                // Update state to playing
                this._currentState = this._currentState with
                {
                    PlaybackState = SnapDog2.Shared.Enums.PlaybackState.Playing
                };

                // Start timer for reliable updates + events for immediate updates
                this.StartPositionUpdateTimer();
                this.SubscribeToMediaPlayerEvents();

                this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
            }

            return Result.Success();
        }
        finally
        {
            this._stateLock.Release();
        }
    }

    /// <summary>
    /// Internal method to set track without acquiring lock (assumes lock is already held)
    /// </summary>
    private async Task<Result> SetTrackInternalAsync(int trackIndex)
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
        this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);

        // Publish individual track metadata notifications for KNX integration
        await this.PublishTrackMetadataNotificationsAsync(targetTrack).ConfigureAwait(false);

        // Log successful track change with meaningful information
        this.LogZonePlaying(
            this._zoneIndex,
            targetTrack.Title,
            this._currentState.Playlist.Source,
            this._currentState.Playlist.Index.ToString(),
            targetTrack.Index.ToString()
        );

        return Result.Success();
    }

    public async Task<Result> PreviousTrackAsync()
    {
        await this._stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var currentIndex = this._currentState.Track?.Index ?? 1;
            var previousIndex = Math.Max(1, currentIndex - 1);

            // Check if we're currently playing
            var wasPlaying = this._currentState.PlaybackState == SnapDog2.Shared.Enums.PlaybackState.Playing;

            // Set the track (inline implementation to avoid nested lock)
            var setResult = await this.SetTrackInternalAsync(previousIndex).ConfigureAwait(false);
            if (setResult.IsFailure)
            {
                return setResult;
            }

            // If we were playing, start playing the new track (inline implementation to avoid nested lock)
            if (wasPlaying)
            {
                var targetTrack = this._currentState.Track!;
                var playResult = await this._mediaPlayerService.PlayAsync(this._zoneIndex, targetTrack).ConfigureAwait(false);
                if (playResult.IsFailure)
                {
                    return playResult;
                }

                // Update state to playing
                this._currentState = this._currentState with
                {
                    PlaybackState = SnapDog2.Shared.Enums.PlaybackState.Playing
                };

                // Start timer for reliable updates + events for immediate updates
                this.StartPositionUpdateTimer();
                this.SubscribeToMediaPlayerEvents();

                this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
            }

            return Result.Success();
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
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
            var newPlaylist = this._currentState.Playlist! with { SubsonicPlaylistId = playlistIndex, Name = playlistIndex };

            this._currentState = this._currentState with { Playlist = newPlaylist };
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
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
            Name = this._config.Name,
            PlaybackState = SnapDog2.Shared.Enums.PlaybackState.Stopped,
            Volume = 50,
            Mute = false,
            TrackRepeat = false,
            PlaylistRepeat = false,
            PlaylistShuffle = false,
            SnapcastGroupId = $"group_{this._zoneIndex}",
            SnapcastStreamId = this._config.Sink,

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
                SubsonicPlaylistId = "none",
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

                // Update current state with real group ID
                this._currentState = this._currentState with { SnapcastGroupId = existingGroup.Id };
                this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);

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
            this.LogUpdateStateFromSnapcastStarting(this._zoneIndex);

            // Get current playback status from MediaPlayerService
            var playbackStatus = await this._mediaPlayerService.GetStatusAsync(this._zoneIndex).ConfigureAwait(false);

            this.LogMediaPlayerStatusResult(playbackStatus.IsSuccess, playbackStatus.Value != null ? "NotNull" : "Null");

            if (playbackStatus.IsSuccess && playbackStatus.Value != null)
            {
                var status = playbackStatus.Value;

                this.LogMediaPlayerStatus(status.IsPlaying, status.CurrentTrack?.Title);

                // Check for playing state changes regardless of whether CurrentTrack is null
                // This handles cases where MediaPlayer returns null CurrentTrack when paused
                if (this._currentState.Track != null)
                {
                    var playingStateChanged = this._currentState.Track.IsPlaying != status.IsPlaying;
                    if (playingStateChanged)
                    {
                        this.LogTrackStateChanged(this._currentState.Track.IsPlaying, status.IsPlaying);

                        // Publish track playing status notification for KNX integration
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = this._serviceScopeFactory.CreateScope();
                                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                                var playingStatusNotification = new ZoneTrackPlayingStatusChangedNotification
                                {
                                    ZoneIndex = this._zoneIndex,
                                    IsPlaying = status.IsPlaying
                                };
                                await mediator.PublishAsync(playingStatusNotification).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                this.LogErrorPublishingTrackPlayingStatus(ex, this._zoneIndex);
                            }
                        });
                    }
                }

                // Update track playing state - handle both existing tracks and new tracks from MediaPlayer
                if (status.CurrentTrack != null)
                {
                    TrackInfo updatedTrack;

                    if (this._currentState.Track != null)
                    {
                        // We have an existing track - preserve its rich metadata and update only playback info
                        var stateChanged = this._currentState.Track.IsPlaying != status.IsPlaying;

                        if (stateChanged)
                        {
                            this.LogTrackStateChanged(this._currentState.Track.IsPlaying, status.IsPlaying);

                            // Publish track playing status notification for KNX integration
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using var scope = this._serviceScopeFactory.CreateScope();
                                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                                    var playingStatusNotification = new ZoneTrackPlayingStatusChangedNotification
                                    {
                                        ZoneIndex = this._zoneIndex,
                                        IsPlaying = status.IsPlaying
                                    };
                                    await mediator.PublishAsync(playingStatusNotification).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    this.LogErrorPublishingTrackPlayingStatus(ex, this._zoneIndex);
                                }
                            });
                        }
                        else
                        {
                            this.LogUpdatingTrackState(this._currentState.Track.IsPlaying, status.IsPlaying);
                        }

                        // Preserve ALL existing metadata, only update playback status and position
                        // This ensures playlist metadata (artist, album, etc.) is not lost
                        updatedTrack = this._currentState.Track with
                        {
                            IsPlaying = status.IsPlaying,
                            PositionMs = status.CurrentTrack.PositionMs,
                            Progress = status.CurrentTrack.Progress,
                            // Only update duration if MediaPlayer provides it and we don't have it
                            DurationMs = status.CurrentTrack.DurationMs ?? this._currentState.Track.DurationMs,
                            // Preserve all other metadata: Title, Artist, Album, Source, Index, etc.
                        };
                    }
                    else
                    {
                        // No existing track - use MediaPlayer track as-is (this handles stream URLs, etc.)
                        this.LogTrackStateChanged(false, status.IsPlaying);

                        // Publish track playing status notification for KNX integration (new track)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = this._serviceScopeFactory.CreateScope();
                                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                                var playingStatusNotification = new ZoneTrackPlayingStatusChangedNotification
                                {
                                    ZoneIndex = this._zoneIndex,
                                    IsPlaying = status.IsPlaying
                                };
                                await mediator.PublishAsync(playingStatusNotification).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                this.LogErrorPublishingTrackPlayingStatus(ex, this._zoneIndex);
                            }
                        });

                        updatedTrack = status.CurrentTrack with
                        {
                            IsPlaying = status.IsPlaying,
                        };
                    }

                    // Log position updates every 30 seconds (simpler approach)
                    var positionSeconds = (status.CurrentTrack.PositionMs ?? 0) / 1000.0;
                    var durationSeconds = (updatedTrack.DurationMs ?? 0) / 1000.0;
                    var progressPercent = (status.CurrentTrack.Progress ?? 0) * 100;

                    // Log every 30 seconds of playback
                    var positionSecondsInt = (int)positionSeconds;
                    var shouldLogPosition =
                        positionSecondsInt > 0
                        && positionSecondsInt % 30 == 0
                        && (positionSecondsInt != this._lastLoggedPositionSeconds);

                    if (shouldLogPosition)
                    {
                        this.LogZonePlayingWithPosition(
                            this._zoneIndex,
                            updatedTrack.Title ?? "Unknown",
                            positionSeconds,
                            durationSeconds,
                            progressPercent
                        );
                        this._lastLoggedPositionSeconds = positionSecondsInt;
                    }
                    else
                    {
                        this.LogLibVLCPositionData(
                            status.CurrentTrack.PositionMs,
                            status.CurrentTrack.Progress ?? 0,
                            updatedTrack.DurationMs
                        );
                    }

                    // Update the zone state with the updated track
                    this._currentState = this._currentState with
                    {
                        Track = updatedTrack,
                        PlaybackState = status.IsPlaying
                            ? SnapDog2.Shared.Enums.PlaybackState.Playing
                            : SnapDog2.Shared.Enums.PlaybackState.Stopped,
                    };

                    this.LogUpdatedTrackState(
                        updatedTrack.IsPlaying,
                        updatedTrack.PositionMs ?? 0,
                        updatedTrack.Progress ?? 0
                    );
                }
                else
                {
                    this.LogSkippingUpdate(
                        this._currentState.Track != null ? "NotNull" : "Null",
                        "Null"
                    );

                    // Even when CurrentTrack is null, update the playing state of existing track
                    if (this._currentState.Track != null)
                    {
                        var updatedTrack = this._currentState.Track with
                        {
                            IsPlaying = status.IsPlaying
                        };

                        this._currentState = this._currentState with
                        {
                            Track = updatedTrack,
                            PlaybackState = status.IsPlaying
                                ? SnapDog2.Shared.Enums.PlaybackState.Playing
                                : SnapDog2.Shared.Enums.PlaybackState.Paused
                        };
                    }
                }
            }
            else
            {
                this.LogMediaPlayerStatusFailed(playbackStatus.IsSuccess, playbackStatus.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the state update
            this.LogFailedToUpdateZoneState(ex, this._zoneIndex);
        }
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

        this.LogStartingPositionUpdateTimer(this._zoneIndex);

        var intervalMs = this._configuration.Value.System.ProgressUpdateIntervalMs;
        var interval = TimeSpan.FromMilliseconds(intervalMs);

        this._positionUpdateTimer = new Timer(
            async _ =>
            {
                try
                {
                    // Only update if we're playing and not disposed
                    if (this._disposed || this._currentState.PlaybackState != SnapDog2.Shared.Enums.PlaybackState.Playing)
                    {
                        return;
                    }

                    // Update state from MediaPlayer and publish if position changed
                    await this.UpdateStateFromSnapcastAsync().ConfigureAwait(false);
                    this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
                }
                catch (Exception ex)
                {
                    this.LogErrorDuringPositionUpdate(ex, this._zoneIndex);
                }
            },
            null,
            interval,
            interval
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

        this.LogStoppingPositionUpdateTimer(this._zoneIndex);

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
            this.LogSubscribingToMediaPlayerEvents(this._zoneIndex);

            mediaPlayer.PositionChanged += this.OnMediaPlayerPositionChanged;
            mediaPlayer.PlaybackStateChanged += this.OnMediaPlayerStateChanged;
            mediaPlayer.TrackInfoChanged += this.OnMediaPlayerTrackInfoChanged;
        }
        else
        {
            this.LogMediaPlayerNullOnSubscribe(this._zoneIndex);
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
            this.LogUnsubscribingFromMediaPlayerEvents(this._zoneIndex);

            mediaPlayer.PositionChanged -= this.OnMediaPlayerPositionChanged;
            mediaPlayer.PlaybackStateChanged -= this.OnMediaPlayerStateChanged;
            mediaPlayer.TrackInfoChanged -= this.OnMediaPlayerTrackInfoChanged;
        }
    }

    /// <summary>
    /// Handles position changes from MediaPlayer and publishes updates.
    /// </summary>
    private void OnMediaPlayerPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        try
        {
            this.LogPositionChangedForZone(this._zoneIndex, e.PositionMs, e.Progress);

            if (this._disposed || this._currentState.PlaybackState != SnapDog2.Shared.Enums.PlaybackState.Playing)
            {
                this.LogSkippingPositionUpdate(this._disposed, this._currentState.PlaybackState.ToString());
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
                this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);
                this.LogPublishedMqttUpdateForPositionChange();

                // Publish track progress notification for KNX integration (throttled)
                Task.Run(async () =>
                {
                    try
                    {
                        using var scope = this._serviceScopeFactory.CreateScope();
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                        var progressNotification = new ZoneTrackProgressChangedNotification
                        {
                            ZoneIndex = this._zoneIndex,
                            Progress = e.Progress
                        };
                        await mediator.PublishAsync(progressNotification).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.LogErrorPublishingTrackProgress(ex, this._zoneIndex);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            this.LogErrorHandlingPositionChange(ex, this._zoneIndex);
        }
    }

    /// <summary>
    /// Handles playback state changes from MediaPlayer.
    /// </summary>
    private void OnMediaPlayerStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        try
        {
            this.LogMediaPlayerStateChanged(this._zoneIndex, e.State.ToString());

            // Update playback state based on LibVLC state
            var newPlaybackState =
                e.IsPlaying ? SnapDog2.Shared.Enums.PlaybackState.Playing
                : e.State == LibVLC.VLCState.Paused ? SnapDog2.Shared.Enums.PlaybackState.Paused
                : SnapDog2.Shared.Enums.PlaybackState.Stopped;

            if (this._currentState.PlaybackState != newPlaybackState)
            {
                this._currentState = this._currentState with { PlaybackState = newPlaybackState };
                this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);

                // Publish track playing status notification for KNX integration
                Task.Run(async () =>
                {
                    try
                    {
                        using var scope = this._serviceScopeFactory.CreateScope();
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                        var playingStatusNotification = new ZoneTrackPlayingStatusChangedNotification
                        {
                            ZoneIndex = this._zoneIndex,
                            IsPlaying = newPlaybackState == SnapDog2.Shared.Enums.PlaybackState.Playing
                        };
                        await mediator.PublishAsync(playingStatusNotification).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.LogErrorPublishingTrackPlayingStatus(ex, this._zoneIndex);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            this.LogErrorHandlingStateChange(ex, this._zoneIndex);
        }
    }

    /// <summary>
    /// Handles track info changes from MediaPlayer and updates Zone state.
    /// </summary>
    private void OnMediaPlayerTrackInfoChanged(object? sender, TrackInfoChangedEventArgs e)
    {
        try
        {
            this.LogTrackInfoChanged(this._zoneIndex, e.TrackInfo.Title ?? "Unknown", e.TrackInfo.Artist ?? "Unknown");

            // Update the Zone's current state with the new track info
            this._currentState = this._currentState with { Track = e.TrackInfo };

            // Publish state change to notify other components
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);

            this.LogTrackInfoUpdated(this._zoneIndex, e.TrackInfo.Title ?? "Unknown");
        }
        catch (Exception ex)
        {
            this.LogErrorHandlingTrackInfoChange(ex, this._zoneIndex);
        }
    }

    #region Status Publishing Methods (Blueprint Compliance)

    public async Task PublishPlaybackStateStatusAsync(SnapDog2.Shared.Enums.PlaybackState playbackState)
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

    public async Task PublishTrackStatusAsync(SnapDog2.Shared.Models.TrackInfo trackInfo, int trackIndex)
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

    public async Task PublishPlaylistStatusAsync(SnapDog2.Shared.Models.PlaylistInfo playlistInfo, int playlistIndex)
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

    /// <summary>
    /// Publishes individual track metadata notifications for KNX integration
    /// </summary>
    private async Task PublishTrackMetadataNotificationsAsync(SnapDog2.Shared.Models.TrackInfo track)
    {
        try
        {
            using var scope = this._serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Publish complete metadata notification
            var metadataNotification = new ZoneTrackMetadataChangedNotification
            {
                ZoneIndex = this._zoneIndex,
                TrackInfo = new SnapDog2.Shared.Models.TrackInfo
                {
                    Index = track.Index,
                    Title = track.Title ?? "Unknown",
                    Artist = track.Artist ?? "Unknown",
                    Album = track.Album ?? "Unknown",
                    Source = track.Source ?? "unknown",
                    Url = track.Url ?? "unknown://no-url",
                    DurationMs = track.DurationMs,
                    PositionMs = track.PositionMs,
                    Progress = track.Progress,
                    IsPlaying = track.IsPlaying,
                    CoverArtUrl = track.CoverArtUrl,
                    Genre = track.Genre,
                    TrackNumber = track.TrackNumber,
                    Year = track.Year,
                    Rating = track.Rating,
                    TimestampUtc = track.TimestampUtc
                }
            };
            await mediator.PublishAsync(metadataNotification).ConfigureAwait(false);

            // Publish individual field notifications
            if (!string.IsNullOrEmpty(track.Title))
            {
                var titleNotification = new ZoneTrackTitleChangedNotification
                {
                    ZoneIndex = this._zoneIndex,
                    Title = track.Title
                };
                await mediator.PublishAsync(titleNotification).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(track.Artist))
            {
                var artistNotification = new ZoneTrackArtistChangedNotification
                {
                    ZoneIndex = this._zoneIndex,
                    Artist = track.Artist
                };
                await mediator.PublishAsync(artistNotification).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(track.Album))
            {
                var albumNotification = new ZoneTrackAlbumChangedNotification
                {
                    ZoneIndex = this._zoneIndex,
                    Album = track.Album
                };
                await mediator.PublishAsync(albumNotification).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.LogErrorPublishingTrackMetadata(ex, this._zoneIndex);
        }
    }

    [LoggerMessage(
        EventId = 6559,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Zone {ZoneIndex} ({ZoneName}): {Action}"
    )]
    private partial void LogZoneAction(int zoneIndex, string zoneName, string action);

    [LoggerMessage(
        EventId = 6560,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Zone {ZoneIndex} synchronized with Snapcast group {GroupId}"
    )]
    private partial void LogSnapcastSync(int zoneIndex, string groupId);

    [LoggerMessage(
        EventId = 6561,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Zone {ZoneIndex} Snapcast group {GroupId} not found"
    )]
    private partial void LogSnapcastGroupNotFound(int zoneIndex, string groupId);

    [LoggerMessage(
        EventId = 6562,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Zone {ZoneIndex} ({ZoneName}): {Action} - {Error}"
    )]
    private partial void LogZoneError(int zoneIndex, string zoneName, string action, string error);

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(
        EventId = 6500,
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
        EventId = 6501,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: No stored state found, creating initial state"
    )]
    private partial void LogNoStoredStateFound(int ZoneIndex);

    [LoggerMessage(EventId = 6502, Level = LogLevel.Debug, Message = "GetStateAsync: Called for zone {ZoneIndex}")]
    private partial void LogGetStateAsyncCalled(int ZoneIndex);

    [LoggerMessage(EventId = 6503, Level = LogLevel.Warning, Message = "GetStateAsync: Timeout for zone {ZoneIndex} - returning cached state")]
    private partial void LogGetStateAsyncTimeout(int ZoneIndex);

    [LoggerMessage(
        EventId = 6503,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: State updated - {StateInfo}"
    )]
    private partial void LogZoneStateUpdated(int ZoneIndex, string StateInfo);

    [LoggerMessage(
        EventId = 6504,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Starting for zone {ZoneIndex}"
    )]
    private partial void LogUpdateStateFromSnapcastStarting(int ZoneIndex);

    [LoggerMessage(
        EventId = 6505,
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
        EventId = 6506,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Getting server status for zone {ZoneIndex}"
    )]
    private partial void LogGettingServerStatus(int ZoneIndex);

    [LoggerMessage(
        EventId = 6507,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Found group {GroupId} for zone {ZoneIndex}"
    )]
    private partial void LogFoundGroup(int ZoneIndex, string GroupId);

    [LoggerMessage(
        EventId = 6508,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: Stream changed from {OldStream} to {NewStream}"
    )]
    private partial void LogStreamChanged(int ZoneIndex, string OldStream, string NewStream);

    [LoggerMessage(EventId = 6509, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: Stream unchanged: {StreamId}")]
    private partial void LogStreamUnchanged(int ZoneIndex, string StreamId);

    [LoggerMessage(
        EventId = 6510,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: Volume changed from {OldVolume}% to {NewVolume}%"
    )]
    private partial void LogVolumeChanged(int ZoneIndex, int OldVolume, int NewVolume);

    [LoggerMessage(EventId = 6511, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: Volume unchanged: {Volume}%")]
    private partial void LogVolumeUnchanged(int ZoneIndex, int Volume);

    [LoggerMessage(EventId = 6512, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: No group found in Snapcast")]
    private partial void LogNoGroupFound(int ZoneIndex);

    [LoggerMessage(EventId = 6513, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: No server status available")]
    private partial void LogNoServerStatus(int ZoneIndex);

    [LoggerMessage(
        EventId = 6514,
        Level = LogLevel.Information,
        Message = "Zone {ZoneIndex}: State synchronized from Snapcast"
    )]
    private partial void LogStateSynchronized(int ZoneIndex);

    [LoggerMessage(
        EventId = 6515,
        Level = LogLevel.Warning,
        Message = "Failed to update zone {ZoneIndex} state from media player"
    )]
    private partial void LogFailedUpdateFromMediaPlayer(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6516,
        Level = LogLevel.Error,
        Message = "Failed to update zone {ZoneIndex} state from Snapcast"
    )]
    private partial void LogFailedUpdateFromSnapcast(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6517,
        Level = LogLevel.Debug,
        Message = "Starting position update timer for zone {ZoneIndex}"
    )]
    private partial void LogStartingPositionTimer(int ZoneIndex);

    [LoggerMessage(
        EventId = 6518,
        Level = LogLevel.Warning,
        Message = "Error during position update for zone {ZoneIndex}"
    )]
    private partial void LogPositionUpdateError(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6519,
        Level = LogLevel.Debug,
        Message = "Stopping position update timer for zone {ZoneIndex}"
    )]
    private partial void LogStoppingPositionTimer(int ZoneIndex);

    [LoggerMessage(
        EventId = 6520,
        Level = LogLevel.Information,
        Message = "✅ Subscribing to MediaPlayer events for zone {ZoneIndex}"
    )]
    private partial void LogSubscribingToMediaPlayerEvents(int ZoneIndex);

    [LoggerMessage(
        EventId = 6521,
        Level = LogLevel.Warning,
        Message = "⚠️ MediaPlayer is null when trying to subscribe to events for zone {ZoneIndex}"
    )]
    private partial void LogMediaPlayerNullOnSubscribe(int ZoneIndex);

    [LoggerMessage(
        EventId = 6522,
        Level = LogLevel.Debug,
        Message = "Unsubscribing from MediaPlayer events for zone {ZoneIndex}"
    )]
    private partial void LogUnsubscribingFromMediaPlayerEvents(int ZoneIndex);

    [LoggerMessage(
        EventId = 6523,
        Level = LogLevel.Information,
        Message = "🎵 Zone {ZoneIndex} position: {PositionMs}ms / {DurationMs}ms ({Progress:P1})"
    )]
    private partial void LogPositionUpdate(int ZoneIndex, long PositionMs, long DurationMs, float Progress);

    [LoggerMessage(EventId = 6524, Level = LogLevel.Debug, Message = "📡 Published MQTT update for position change")]
    private partial void LogPublishedMqttPositionUpdate();

    [LoggerMessage(
        EventId = 6525,
        Level = LogLevel.Warning,
        Message = "Error handling position change for zone {ZoneIndex}"
    )]
    private partial void LogPositionChangeError(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6526,
        Level = LogLevel.Debug,
        Message = "MediaPlayer state changed for zone {ZoneIndex}: {State}"
    )]
    private partial void LogMediaPlayerStateChanged(int ZoneIndex, LibVLC.VLCState State);

    [LoggerMessage(
        EventId = 6527,
        Level = LogLevel.Warning,
        Message = "Error handling state change for zone {ZoneIndex}"
    )]
    private partial void LogStateChangeError(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6528,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: MediaPlayer status result - Success: {Success}, Value: {Value}"
    )]
    private partial void LogMediaPlayerStatusResult(bool Success, string Value);

    [LoggerMessage(
        EventId = 6529,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Status - IsPlaying: {IsPlaying}, CurrentTrack: {CurrentTrack}"
    )]
    private partial void LogMediaPlayerStatus(bool IsPlaying, string? CurrentTrack);

    [LoggerMessage(
        EventId = 6530,
        Level = LogLevel.Information,
        Message = "UpdateStateFromSnapcastAsync: Track state changed - Old IsPlaying: {OldIsPlaying}, New IsPlaying: {NewIsPlaying}"
    )]
    private partial void LogTrackStateChanged(bool OldIsPlaying, bool NewIsPlaying);

    [LoggerMessage(
        EventId = 6531,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Updating track state - Old IsPlaying: {OldIsPlaying}, New IsPlaying: {NewIsPlaying}"
    )]
    private partial void LogUpdatingTrackState(bool OldIsPlaying, bool NewIsPlaying);

    [LoggerMessage(
        EventId = 6532,
        Level = LogLevel.Information,
        Message = "UpdateStateFromSnapcastAsync: Position update - Position: {PositionMs}ms, Duration: {DurationMs}ms"
    )]
    private partial void LogPositionUpdateFromSnapcast(long PositionMs, long DurationMs);

    [LoggerMessage(
        EventId = 6533,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Position unchanged - Position: {PositionMs}ms"
    )]
    private partial void LogPositionUnchanged(long PositionMs);

    [LoggerMessage(
        EventId = 6534,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: No group found for zone {ZoneIndex}"
    )]
    private partial void LogNoGroupFoundForZone(int ZoneIndex);

    [LoggerMessage(
        EventId = 6535,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: No server status available for zone {ZoneIndex}"
    )]
    private partial void LogNoServerStatusForZone(int ZoneIndex);

    [LoggerMessage(
        EventId = 6536,
        Level = LogLevel.Information,
        Message = "UpdateStateFromSnapcastAsync: State synchronized for zone {ZoneIndex}"
    )]
    private partial void LogStateSynchronizedForZone(int ZoneIndex);

    [LoggerMessage(
        EventId = 6537,
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
        EventId = 6538,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: LibVLC Position Data - PositionMs: {PositionMs}, Progress: {Progress}, DurationMs: {DurationMs}"
    )]
    private partial void LogLibVLCPositionData(long? PositionMs, float Progress, long? DurationMs);

    [LoggerMessage(
        EventId = 6539,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Updated track state - IsPlaying: {IsPlaying}, PositionMs: {PositionMs}, Progress: {Progress}"
    )]
    private partial void LogUpdatedTrackState(bool IsPlaying, long PositionMs, float Progress);

    [LoggerMessage(
        EventId = 6540,
        Level = LogLevel.Debug,
        Message = "UpdateStateFromSnapcastAsync: Skipping update - CurrentState.Track: {CurrentTrack}, Status.CurrentTrack: {StatusCurrentTrack}"
    )]
    private partial void LogSkippingUpdate(string CurrentTrack, string StatusCurrentTrack);

    [LoggerMessage(
        EventId = 6541,
        Level = LogLevel.Information,
        Message = "UpdateStateFromSnapcastAsync: MediaPlayer status failed or null - Success: {Success}, ErrorMessage: {ErrorMessage}"
    )]
    private partial void LogMediaPlayerStatusFailed(bool Success, string? ErrorMessage);

    [LoggerMessage(
        EventId = 6542,
        Level = LogLevel.Warning,
        Message = "Failed to update zone {ZoneIndex} state from media player"
    )]
    private partial void LogFailedToUpdateZoneState(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6544,
        Level = LogLevel.Debug,
        Message = "Starting position update timer for zone {ZoneIndex}"
    )]
    private partial void LogStartingPositionUpdateTimer(int ZoneIndex);

    [LoggerMessage(
        EventId = 6545,
        Level = LogLevel.Warning,
        Message = "Error during position update for zone {ZoneIndex}"
    )]
    private partial void LogErrorDuringPositionUpdate(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6546,
        Level = LogLevel.Debug,
        Message = "Stopping position update timer for zone {ZoneIndex}"
    )]
    private partial void LogStoppingPositionUpdateTimer(int ZoneIndex);

    [LoggerMessage(
        EventId = 6547,
        Level = LogLevel.Information,
        Message = "🎵 LibVLC Position Event: Zone {ZoneIndex}, PositionMs: {PositionMs}, Progress: {Progress}"
    )]
    private partial void LogPositionChangedForZone(int ZoneIndex, long PositionMs, float Progress);

    [LoggerMessage(
        EventId = 6548,
        Level = LogLevel.Debug,
        Message = "Skipping position update - disposed: {Disposed}, state: {State}"
    )]
    private partial void LogSkippingPositionUpdate(bool Disposed, string State);

    [LoggerMessage(EventId = 6549, Level = LogLevel.Debug, Message = "📡 Published MQTT update for position change")]
    private partial void LogPublishedMqttUpdateForPositionChange();

    [LoggerMessage(
        EventId = 6550,
        Level = LogLevel.Warning,
        Message = "Error handling position change for zone {ZoneIndex}"
    )]
    private partial void LogErrorHandlingPositionChange(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6551,
        Level = LogLevel.Debug,
        Message = "MediaPlayer state changed for zone {ZoneIndex}: {State}"
    )]
    private partial void LogMediaPlayerStateChanged(int ZoneIndex, string State);

    [LoggerMessage(
        EventId = 6553,
        Level = LogLevel.Warning,
        Message = "Error publishing track metadata notifications for zone {ZoneIndex}"
    )]
    private partial void LogErrorPublishingTrackMetadata(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6552,
        Level = LogLevel.Warning,
        Message = "Error handling state change for zone {ZoneIndex}"
    )]
    private partial void LogErrorHandlingStateChange(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6554,
        Level = LogLevel.Warning,
        Message = "Error publishing track playing status notification for zone {ZoneIndex}"
    )]
    private partial void LogErrorPublishingTrackPlayingStatus(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6555,
        Level = LogLevel.Warning,
        Message = "Error publishing track progress notification for zone {ZoneIndex}"
    )]
    private partial void LogErrorPublishingTrackProgress(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 6556,
        Level = LogLevel.Debug,
        Message = "🎵 Track info changed for zone {ZoneIndex}: '{Title}' by '{Artist}'"
    )]
    private partial void LogTrackInfoChanged(int ZoneIndex, string Title, string Artist);

    [LoggerMessage(
        EventId = 6557,
        Level = LogLevel.Information,
        Message = "✅ Track info updated for zone {ZoneIndex}: '{Title}'"
    )]
    private partial void LogTrackInfoUpdated(int ZoneIndex, string Title);

    [LoggerMessage(
        EventId = 6558,
        Level = LogLevel.Warning,
        Message = "Error handling track info change for zone {ZoneIndex}"
    )]
    private partial void LogErrorHandlingTrackInfoChange(Exception ex, int ZoneIndex);
}
