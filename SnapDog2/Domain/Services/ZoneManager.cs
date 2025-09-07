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

using System.Collections.Concurrent;
using Cortex.Mediator;
using Microsoft.Extensions.Options;
using SnapDog2.Api.Hubs.Notifications;
using SnapDog2.Api.Models;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Infrastructure.Audio;
using SnapDog2.Infrastructure.Integrations.Snapcast.Models;
using SnapDog2.Server.Playlists.Queries;
using SnapDog2.Server.Zones.Notifications;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
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
    IClientStateStore clientStateStore,
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
    private readonly ConcurrentDictionary<int, IZoneService> _zones = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _isInitialized;
    private bool _disposed;

    [LoggerMessage(EventId = 110562, Level = LogLevel.Information, Message = "Initializing ZoneManager with {ZoneCount} configured zones"
)]
    private partial void LogInitializing(int zoneCount);

    [LoggerMessage(EventId = 110564, Level = LogLevel.Information, Message = "Zone {ZoneIndex} ({ZoneName}) initialized successfully"
)]
    private partial void LogZoneInitialized(int zoneIndex, string zoneName);

    [LoggerMessage(EventId = 110565, Level = LogLevel.Warning, Message = "Zone {ZoneIndex} not found"
)]
    private partial void LogZoneNotFound(int zoneIndex);

    [LoggerMessage(EventId = 110567, Level = LogLevel.Error, Message = "Failed → initialize zone {ZoneIndex}: {Error}"
)]
    private partial void LogZoneInitializationFailed(int zoneIndex, string error);

    [LoggerMessage(EventId = 110568, Level = LogLevel.Debug, Message = "Getting zone {ZoneIndex}"
)]
    private partial void LogGettingZone(int zoneIndex);

    [LoggerMessage(EventId = 110569, Level = LogLevel.Debug, Message = "Getting all zones"
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
                        clientStateStore,
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

        this._initializationLock.Dispose();
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
    private readonly IClientStateStore _clientStateStore;
    private readonly IStatusFactory _statusFactory;
    private readonly ILogger _logger;
    private readonly IOptions<SnapDogConfiguration> _configuration;
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
        IClientStateStore clientStateStore,
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
        this._clientStateStore = clientStateStore;
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
            }

            // Get clients assigned to this zone
            var zoneClients = Array.Empty<int>();
            if (this._clientStateStore != null)
            {
                var allClientStates = this._clientStateStore.GetAllClientStates();
                zoneClients = allClientStates
                    .Where(kvp => kvp.Value.ZoneIndex == this._zoneIndex)
                    .Select(kvp => kvp.Key)
                    .ToArray();
            }

            return Result<ZoneState>.Success(this._currentState with
            {
                Clients = zoneClients,
                TimestampUtc = DateTime.UtcNow
            });
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
                PlaybackState = PlaybackState.Playing,
            };

            // Start position update timer for reliable MQTT updates + subscribe to events for immediate updates
            this.StartPositionUpdateTimer();
            this.SubscribeToMediaPlayerEvents();

            // Publish notification
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);

            // Publish status notification for blueprint compliance
            await this.PublishPlaybackStateStatusAsync(PlaybackState.Playing);

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
            // Get track from current playlist using playlist manager
            if (this._currentState.Playlist == null || this._currentState.Playlist.Index == null)
            {
                return Result.Failure("No playlist selected. Please set a playlist first.");
            }

            var playlistIndex = this._currentState.Playlist.Index.Value;
            var getPlaylistQuery = new GetPlaylistQuery { PlaylistIndex = playlistIndex };

            using var scope = this._serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var playlistResult = await mediator
                .SendQueryAsync<GetPlaylistQuery, Result<PlaylistWithTracks>>(getPlaylistQuery)
                .ConfigureAwait(false);

            if (playlistResult.IsFailure)
            {
                return Result.Failure($"Failed to get playlist: {playlistResult.ErrorMessage}");
            }

            var playlist = playlistResult.Value;
            if (playlist?.Tracks == null || playlist.Tracks.Count == 0)
            {
                return Result.Failure("Playlist has no tracks");
            }

            var targetTrack = playlist.Tracks.FirstOrDefault(t => t.Index == trackIndex);
            if (targetTrack == null)
            {
                return Result.Failure($"Track {trackIndex} not found in playlist");
            }

            // Start playback with the actual track from playlist
            var playResult = await this._mediaPlayerService.PlayAsync(this._zoneIndex, targetTrack).ConfigureAwait(false);
            if (playResult.IsFailure)
            {
                return playResult;
            }

            // Update state
            this._currentState = this._currentState with
            {
                PlaybackState = PlaybackState.Playing,
                Track = targetTrack,
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
                PlaybackState = PlaybackState.Playing,
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

            this._currentState = this._currentState with { PlaybackState = PlaybackState.Paused };

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

            this._currentState = this._currentState with { PlaybackState = PlaybackState.Stopped };

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
            var result = await this.SetVolumeInternal(volume);

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

    private async Task<Result> SetVolumeInternal(int volume)
    {
        var clampedVolume = Math.Clamp(volume, 0, 100);

        // Update Snapcast group volume using proportional scaling like snapweb
        if (!string.IsNullOrEmpty(this._snapcastGroupId))
        {
            this.LogZoneAction(this._zoneIndex, this._config.Name, $"Setting group volume {clampedVolume} for group {_snapcastGroupId}");

            // Get group status to find all clients
            var serverStatus = await this._snapcastService.GetServerStatusAsync();
            if (serverStatus.IsSuccess)
            {
                var group = serverStatus.Value?.Groups?.FirstOrDefault(g => g.Id == this._snapcastGroupId);
                if (group?.Clients != null && group.Clients.Count > 0)
                {
                    // Calculate current group volume (average of all client volumes)
                    var currentGroupVolume = group.Clients.Average(c => (double)c.Volume);
                    var delta = clampedVolume - currentGroupVolume;

                    this.LogZoneAction(this._zoneIndex, this._config.Name, $"Current group volume: {currentGroupVolume:F1}, target: {clampedVolume}, delta: {delta:F1}");

                    // Apply proportional scaling to each client (snapweb algorithm)
                    foreach (var client in group.Clients)
                    {
                        var currentClientVolume = (double)client.Volume;
                        double newClientVolume;

                        if (delta < 0)
                        {
                            // Decreasing volume: scale down proportionally
                            var ratio = Math.Abs(delta) / currentGroupVolume;
                            newClientVolume = currentClientVolume - (ratio * currentClientVolume);
                        }
                        else
                        {
                            // Increasing volume: scale up proportionally
                            var ratio = delta / (100 - currentGroupVolume);
                            newClientVolume = currentClientVolume + (ratio * (100 - currentClientVolume));
                        }

                        var finalVolume = Math.Clamp((int)Math.Round(newClientVolume), 0, 100);

                        var clientResult = await this._snapcastService.SetClientVolumeAsync(client.Id, finalVolume);
                        if (clientResult.IsFailure)
                        {
                            this.LogZoneAction(this._zoneIndex, this._config.Name, $"Failed to set volume for client {client.Id}: {clientResult.ErrorMessage}");
                            return clientResult;
                        }
                    }
                    this.LogZoneAction(this._zoneIndex, this._config.Name, $"Successfully applied proportional volume scaling to {group.Clients.Count} clients");
                }
            }
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
            var result = await this.SetVolumeInternal(newVolume);

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
            var result = await this.SetVolumeInternal(newVolume);

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
        this.LogZoneAction(this._zoneIndex, this._config.Name, $"SetMuteInternal: enabled={enabled}, snapcastGroupId={_snapcastGroupId ?? "null"}");

        // Update Snapcast group mute if available
        if (!string.IsNullOrEmpty(this._snapcastGroupId))
        {
            this.LogZoneAction(this._zoneIndex, this._config.Name, $"Calling SetGroupMuteAsync for group {_snapcastGroupId}");
            var snapcastResult = await this
                ._snapcastService.SetGroupMuteAsync(this._snapcastGroupId, enabled)
                .ConfigureAwait(false);
            if (snapcastResult.IsFailure)
            {
                this.LogZoneAction(this._zoneIndex, this._config.Name, $"SetGroupMuteAsync failed: {snapcastResult.ErrorMessage}");
                return snapcastResult;
            }
            this.LogZoneAction(this._zoneIndex, this._config.Name, $"SetGroupMuteAsync succeeded");
        }
        else
        {
            this.LogZoneAction(this._zoneIndex, this._config.Name, "Skipping Snapcast group mute - no group ID set");
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
                .SendQueryAsync<GetPlaylistQuery, Result<PlaylistWithTracks>>(getPlaylistQuery)
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
            var wasPlaying = this._currentState.PlaybackState == PlaybackState.Playing;

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
                    PlaybackState = PlaybackState.Playing
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
            .SendQueryAsync<GetPlaylistQuery, Result<PlaylistWithTracks>>(getPlaylistQuery)
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
            var wasPlaying = this._currentState.PlaybackState == PlaybackState.Playing;

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
                    PlaybackState = PlaybackState.Playing
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
    async internal Task SynchronizeWithSnapcastAsync(IEnumerable<Group> snapcastGroups)
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
            PlaybackState = PlaybackState.Stopped,
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

    public void UpdateSnapcastGroupId(string? groupId)
    {
        this._snapcastGroupId = groupId;
        this.LogZoneAction(this._zoneIndex, this._config.Name, $"Updated Snapcast group ID to: {groupId ?? "null"}");
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
                            updatedTrack.Title,
                            positionSeconds,
                            durationSeconds,
                            progressPercent
                        );
                        this._lastLoggedPositionSeconds = positionSecondsInt;
                    }
                    else
                    {
                        this.LogLibVlcPositionData(
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
                            ? PlaybackState.Playing
                            : PlaybackState.Stopped,
                    };

                    this.LogUpdatedTrackState(
                        updatedTrack.IsPlaying,
                        updatedTrack.PositionMs ?? 0,
                        updatedTrack.Progress ?? 0
                    );

                    // Publish progress notification for real-time UI updates
                    if (updatedTrack.IsPlaying && updatedTrack.PositionMs.HasValue)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = this._serviceScopeFactory.CreateScope();
                                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                                var progressPercent = updatedTrack.DurationMs.HasValue && updatedTrack.DurationMs > 0
                                    ? (updatedTrack.Progress ?? 0) * 100
                                    : 0; // For radio streams, progress is always 0

                                var progressNotification = new ZoneProgressChangedNotification(
                                    this._zoneIndex,
                                    updatedTrack.PositionMs.Value,
                                    progressPercent
                                );

                                await mediator.PublishAsync(progressNotification).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                this.LogErrorPublishingTrackProgress(ex, this._zoneIndex);
                            }
                        });
                    }
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
                                ? PlaybackState.Playing
                                : PlaybackState.Paused
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
            async void (_) =>
            {
                try
                {
                    // Only update if we're playing and not disposed
                    if (this._disposed || this._currentState.PlaybackState != PlaybackState.Playing)
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

            if (this._disposed || this._currentState.PlaybackState != PlaybackState.Playing)
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

                        // Publish progress notification for real-time UI updates
                        var progressPercent = this._currentState.Track.DurationMs.HasValue && this._currentState.Track.DurationMs > 0
                            ? e.Progress * 100
                            : 0; // For radio streams, progress is always 0

                        var signalRProgressNotification = new ZoneProgressChangedNotification(
                            this._zoneIndex,
                            e.PositionMs,
                            progressPercent
                        );

                        await mediator.PublishAsync(signalRProgressNotification).ConfigureAwait(false);
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
                e.IsPlaying ? PlaybackState.Playing
                : e.State == LibVLC.VLCState.Paused ? PlaybackState.Paused
                : PlaybackState.Stopped;

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
                            IsPlaying = newPlaybackState == PlaybackState.Playing
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
            this.LogTrackInfoChanged(this._zoneIndex, e.TrackInfo.Title, e.TrackInfo.Artist);

            // Update the Zone's current state with the new track info
            this._currentState = this._currentState with { Track = e.TrackInfo };

            // Publish state change to notify other components
            this._zoneStateStore.SetZoneState(this._zoneIndex, this._currentState);

            this.LogTrackInfoUpdated(this._zoneIndex, e.TrackInfo.Title);
        }
        catch (Exception ex)
        {
            this.LogErrorHandlingTrackInfoChange(ex, this._zoneIndex);
        }
    }

    #region Status Publishing Methods (Blueprint Compliance)

    public async Task PublishPlaybackStateStatusAsync(PlaybackState playbackState)
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

        this._stateLock.Dispose();
        this._disposed = true;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Publishes individual track metadata notifications for KNX integration
    /// </summary>
    private async Task PublishTrackMetadataNotificationsAsync(TrackInfo track)
    {
        try
        {
            using var scope = this._serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Publish complete metadata notification
            var metadataNotification = new SnapDog2.Server.Zones.Notifications.ZoneTrackMetadataChangedNotification
            {
                ZoneIndex = this._zoneIndex,
                TrackInfo = new TrackInfo
                {
                    Index = track.Index,
                    Title = track.Title,
                    Artist = track.Artist,
                    Album = track.Album ?? "Unknown", // TODO: Why can Album be null?
                    Source = track.Source,
                    Url = track.Url,
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

    [LoggerMessage(EventId = 110506, Level = LogLevel.Information, Message = "Zone {ZoneIndex} ({ZoneName}): {Action}"
)]
    private partial void LogZoneAction(int zoneIndex, string zoneName, string action);

    [LoggerMessage(EventId = 110507, Level = LogLevel.Debug, Message = "Zone {ZoneIndex} synchronized with Snapcast group {GroupId}"
)]
    private partial void LogSnapcastSync(int zoneIndex, string groupId);

    [LoggerMessage(EventId = 110508, Level = LogLevel.Warning, Message = "Zone {ZoneIndex} Snapcast group {GroupId} not found"
)]
    private partial void LogSnapcastGroupNotFound(int zoneIndex, string groupId);

    [LoggerMessage(EventId = 110509, Level = LogLevel.Error, Message = "Zone {ZoneIndex} ({ZoneName}): {Action} - {Error}"
)]
    private partial void LogZoneError(int zoneIndex, string zoneName, string action, string error);

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(EventId = 110510, Level = LogLevel.Information, Message = "Zone {ZoneIndex}: Loaded state from store - Source: {Source}; Playlist: {PlaylistIndex}, Track: {TrackIndex} ({TrackTitle})"
)]
    private partial void LogZoneInitializing(
        int zoneIndex,
        string source,
        string playlistIndex,
        string trackIndex,
        string trackTitle
    );

    [LoggerMessage(EventId = 110511, Level = LogLevel.Information, Message = "Zone {ZoneIndex}: No stored state found, creating initial state"
)]
    private partial void LogNoStoredStateFound(int zoneIndex);

    [LoggerMessage(EventId = 110512, Level = LogLevel.Debug, Message = "GetStateAsync: Called for zone {ZoneIndex}")]
    private partial void LogGetStateAsyncCalled(int zoneIndex);

    [LoggerMessage(EventId = 110514, Level = LogLevel.Warning, Message = "GetStateAsync: Timeout for zone {ZoneIndex} - returning cached state")]
    private partial void LogGetStateAsyncTimeout(int zoneIndex);

    [LoggerMessage(EventId = 110514, Level = LogLevel.Information, Message = "Zone {ZoneIndex}: State updated - {StateInfo}"
)]
    private partial void LogZoneStateUpdated(int zoneIndex, string stateInfo);

    [LoggerMessage(EventId = 110515, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: Starting for zone {ZoneIndex}"
)]
    private partial void LogUpdateStateFromSnapcastStarting(int zoneIndex);

    [LoggerMessage(EventId = 110516, Level = LogLevel.Information, Message = "Zone {ZoneIndex} playing: {TrackTitle} from {Source} (playlist {PlaylistIndex}, track {TrackIndex})"
)]
    private partial void LogZonePlaying(
        int zoneIndex,
        string? trackTitle,
        string? source,
        string? playlistIndex,
        string? trackIndex
    );

    [LoggerMessage(EventId = 110517, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: Getting server status for zone {ZoneIndex}"
)]
    private partial void LogGettingServerStatus(int zoneIndex);

    [LoggerMessage(EventId = 110518, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: Found group {GroupId} for zone {ZoneIndex}"
)]
    private partial void LogFoundGroup(int zoneIndex, string groupId);

    [LoggerMessage(EventId = 110519, Level = LogLevel.Information, Message = "Zone {ZoneIndex}: Stream changed from {OldStream} → {NewStream}"
)]
    private partial void LogStreamChanged(int zoneIndex, string oldStream, string newStream);

    [LoggerMessage(EventId = 110520, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: Stream unchanged: {StreamId}")]
    private partial void LogStreamUnchanged(int zoneIndex, string streamId);

    [LoggerMessage(EventId = 110521, Level = LogLevel.Information, Message = "Zone {ZoneIndex}: Volume changed from {OldVolume}% → {NewVolume}%"
)]
    private partial void LogVolumeChanged(int zoneIndex, int oldVolume, int newVolume);

    [LoggerMessage(EventId = 110522, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: Volume unchanged: {Volume}%")]
    private partial void LogVolumeUnchanged(int zoneIndex, int volume);

    [LoggerMessage(EventId = 110523, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: No group found in Snapcast")]
    private partial void LogNoGroupFound(int zoneIndex);

    [LoggerMessage(EventId = 110524, Level = LogLevel.Debug, Message = "Zone {ZoneIndex}: No server status available")]
    private partial void LogNoServerStatus(int zoneIndex);

    [LoggerMessage(EventId = 110525, Level = LogLevel.Information, Message = "Zone {ZoneIndex}: State synchronized from Snapcast"
)]
    private partial void LogStateSynchronized(int zoneIndex);

    [LoggerMessage(EventId = 110526, Level = LogLevel.Warning, Message = "Failed → update zone {ZoneIndex} state from media player"
)]
    private partial void LogFailedUpdateFromMediaPlayer(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110527, Level = LogLevel.Error, Message = "Failed → update zone {ZoneIndex} state from Snapcast"
)]
    private partial void LogFailedUpdateFromSnapcast(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110528, Level = LogLevel.Debug, Message = "Starting position update timer for zone {ZoneIndex}"
)]
    private partial void LogStartingPositionTimer(int zoneIndex);

    [LoggerMessage(EventId = 110529, Level = LogLevel.Warning, Message = "Error during position update for zone {ZoneIndex}"
)]
    private partial void LogPositionUpdateError(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110530, Level = LogLevel.Debug, Message = "Stopping position update timer for zone {ZoneIndex}"
)]
    private partial void LogStoppingPositionTimer(int zoneIndex);

    [LoggerMessage(EventId = 110531, Level = LogLevel.Information, Message = "✅ Subscribing → MediaPlayer events for zone {ZoneIndex}"
)]
    private partial void LogSubscribingToMediaPlayerEvents(int zoneIndex);

    [LoggerMessage(EventId = 110532, Level = LogLevel.Warning, Message = "⚠️ MediaPlayer is null when trying → subscribe → events for zone {ZoneIndex}"
)]
    private partial void LogMediaPlayerNullOnSubscribe(int zoneIndex);

    [LoggerMessage(EventId = 110533, Level = LogLevel.Debug, Message = "Unsubscribing from MediaPlayer events for zone {ZoneIndex}"
)]
    private partial void LogUnsubscribingFromMediaPlayerEvents(int zoneIndex);

    [LoggerMessage(EventId = 110534, Level = LogLevel.Information, Message = "Zone {ZoneIndex} position: {PositionMs}ms / {DurationMs}ms ({Progress:P1})"
)]
    private partial void LogPositionUpdate(int zoneIndex, long positionMs, long durationMs, float progress);

    [LoggerMessage(EventId = 110535, Level = LogLevel.Debug, Message = "Published MQTT update for position change")]
    private partial void LogPublishedMqttPositionUpdate();

    [LoggerMessage(EventId = 110536, Level = LogLevel.Warning, Message = "Error handling position change for zone {ZoneIndex}"
)]
    private partial void LogPositionChangeError(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110537, Level = LogLevel.Debug, Message = "MediaPlayer state changed for zone {ZoneIndex}: {State}"
)]
    private partial void LogMediaPlayerStateChanged(int zoneIndex, LibVLC.VLCState state);

    [LoggerMessage(EventId = 110538, Level = LogLevel.Warning, Message = "Error handling state change for zone {ZoneIndex}"
)]
    private partial void LogStateChangeError(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110539, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: MediaPlayer status result - Success: {Success}, Value: {Value}"
)]
    private partial void LogMediaPlayerStatusResult(bool success, string value);

    [LoggerMessage(EventId = 110540, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: Status - IsPlaying: {IsPlaying}, CurrentTrack: {CurrentTrack}"
)]
    private partial void LogMediaPlayerStatus(bool isPlaying, string? currentTrack);

    [LoggerMessage(EventId = 110541, Level = LogLevel.Information, Message = "UpdateStateFromSnapcastAsync: Track state changed - Old IsPlaying: {OldIsPlaying}, New IsPlaying: {NewIsPlaying}"
)]
    private partial void LogTrackStateChanged(bool oldIsPlaying, bool newIsPlaying);

    [LoggerMessage(EventId = 110542, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: Updating track state - Old IsPlaying: {OldIsPlaying}, New IsPlaying: {NewIsPlaying}"
)]
    private partial void LogUpdatingTrackState(bool oldIsPlaying, bool newIsPlaying);

    [LoggerMessage(EventId = 110543, Level = LogLevel.Information, Message = "UpdateStateFromSnapcastAsync: Position update - Position: {PositionMs}ms, Duration: {DurationMs}ms"
)]
    private partial void LogPositionUpdateFromSnapcast(long positionMs, long durationMs);

    [LoggerMessage(EventId = 110544, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: Position unchanged - Position: {PositionMs}ms"
)]
    private partial void LogPositionUnchanged(long positionMs);

    [LoggerMessage(EventId = 110545, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: No group found for zone {ZoneIndex}"
)]
    private partial void LogNoGroupFoundForZone(int zoneIndex);

    [LoggerMessage(EventId = 110546, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: No server status available for zone {ZoneIndex}"
)]
    private partial void LogNoServerStatusForZone(int zoneIndex);

    [LoggerMessage(EventId = 110547, Level = LogLevel.Information, Message = "UpdateStateFromSnapcastAsync: State synchronized for zone {ZoneIndex}"
)]
    private partial void LogStateSynchronizedForZone(int zoneIndex);

    [LoggerMessage(EventId = 110548, Level = LogLevel.Information, Message = "Zone {ZoneIndex}: Playing \"{Title}\" - {Position:F1}s / {Duration:F1}s ({Progress:F1}%)"
)]
    private partial void LogZonePlayingWithPosition(
        int zoneIndex,
        string? title,
        double position,
        double? duration,
        double progress
    );

    [LoggerMessage(EventId = 110549, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: LibVLC Position Data - PositionMs: {PositionMs}, Progress: {Progress}, DurationMs: {DurationMs}"
)]
    private partial void LogLibVlcPositionData(long? positionMs, float progress, long? durationMs);

    [LoggerMessage(EventId = 110550, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: Updated track state - IsPlaying: {IsPlaying}, PositionMs: {PositionMs}, Progress: {Progress}"
)]
    private partial void LogUpdatedTrackState(bool isPlaying, long positionMs, float progress);

    [LoggerMessage(EventId = 110551, Level = LogLevel.Debug, Message = "UpdateStateFromSnapcastAsync: Skipping update - CurrentState.Track: {CurrentTrack}, Status.CurrentTrack: {StatusCurrentTrack}"
)]
    private partial void LogSkippingUpdate(string currentTrack, string statusCurrentTrack);

    [LoggerMessage(EventId = 110552, Level = LogLevel.Information, Message = "UpdateStateFromSnapcastAsync: MediaPlayer status failed or null - Success: {Success}, ErrorMessage: {ErrorMessage}"
)]
    private partial void LogMediaPlayerStatusFailed(bool success, string? errorMessage);

    [LoggerMessage(EventId = 110553, Level = LogLevel.Warning, Message = "Failed → update zone {ZoneIndex} state from media player"
)]
    private partial void LogFailedToUpdateZoneState(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110554, Level = LogLevel.Debug, Message = "Starting position update timer for zone {ZoneIndex}"
)]
    private partial void LogStartingPositionUpdateTimer(int zoneIndex);

    [LoggerMessage(EventId = 110555, Level = LogLevel.Warning, Message = "Error during position update for zone {ZoneIndex}"
)]
    private partial void LogErrorDuringPositionUpdate(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110556, Level = LogLevel.Debug, Message = "Stopping position update timer for zone {ZoneIndex}"
)]
    private partial void LogStoppingPositionUpdateTimer(int zoneIndex);

    [LoggerMessage(EventId = 110557, Level = LogLevel.Information, Message = "LibVLC Position Event: Zone {ZoneIndex}, PositionMs: {PositionMs}, Progress: {Progress}"
)]
    private partial void LogPositionChangedForZone(int zoneIndex, long positionMs, float progress);

    [LoggerMessage(EventId = 110558, Level = LogLevel.Debug, Message = "Skipping position update - disposed: {Disposed}, state: {State}"
)]
    private partial void LogSkippingPositionUpdate(bool disposed, string state);

    [LoggerMessage(EventId = 110559, Level = LogLevel.Debug, Message = "Published MQTT update for position change")]
    private partial void LogPublishedMqttUpdateForPositionChange();

    [LoggerMessage(EventId = 110560, Level = LogLevel.Warning, Message = "Error handling position change for zone {ZoneIndex}"
)]
    private partial void LogErrorHandlingPositionChange(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110561, Level = LogLevel.Debug, Message = "MediaPlayer state changed for zone {ZoneIndex}: {State}"
)]
    private partial void LogMediaPlayerStateChanged(int zoneIndex, string state);

    [LoggerMessage(EventId = 110562, Level = LogLevel.Warning, Message = "Error publishing track metadata notifications for zone {ZoneIndex}"
)]
    private partial void LogErrorPublishingTrackMetadata(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110563, Level = LogLevel.Warning, Message = "Error handling state change for zone {ZoneIndex}"
)]
    private partial void LogErrorHandlingStateChange(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110564, Level = LogLevel.Warning, Message = "Error publishing track playing status notification for zone {ZoneIndex}"
)]
    private partial void LogErrorPublishingTrackPlayingStatus(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110565, Level = LogLevel.Warning, Message = "Error publishing track progress notification for zone {ZoneIndex}"
)]
    private partial void LogErrorPublishingTrackProgress(Exception ex, int zoneIndex);

    [LoggerMessage(EventId = 110567, Level = LogLevel.Debug, Message = "Publishing SignalR progress: Zone {ZoneIndex}, Position: {PositionMs}ms, Progress: {ProgressPercent}%"
)]
    private partial void LogPublishingSignalRProgress(int zoneIndex, long positionMs, float progressPercent);

    [LoggerMessage(EventId = 110567, Level = LogLevel.Debug, Message = "Track info changed for zone {ZoneIndex}: '{Title}' by '{Artist}'"
)]
    private partial void LogTrackInfoChanged(int zoneIndex, string title, string artist);

    [LoggerMessage(EventId = 110568, Level = LogLevel.Information, Message = "✅ Track info updated for zone {ZoneIndex}: '{Title}'"
)]
    private partial void LogTrackInfoUpdated(int zoneIndex, string title);

    [LoggerMessage(EventId = 110569, Level = LogLevel.Warning, Message = "Error handling track info change for zone {ZoneIndex}"
)]
    private partial void LogErrorHandlingTrackInfoChange(Exception ex, int zoneIndex);
}
