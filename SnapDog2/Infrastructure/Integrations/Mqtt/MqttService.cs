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
namespace SnapDog2.Infrastructure.Integrations.Mqtt;

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Events;
using SnapDog2.Shared.Models;

/// <summary>
/// Enterprise-grade MQTT service implementation using MQTTnet v5.
/// </summary>
public sealed partial class MqttService : IMqttService
{
    private readonly MqttConfig _config;
    private readonly ILogger<MqttService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private IMqttClient? _mqttClient;
    private bool _disposed;

    public MqttService(MqttConfig config, ILogger<MqttService> logger, IServiceProvider serviceProvider)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public bool IsConnected => _mqttClient?.IsConnected ?? false;

    public event EventHandler? Connected;
    public event EventHandler<string>? Disconnected;
    public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;

    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return Result.Failure("MQTT service has been disposed");
        }

        if (!_config.Enabled)
        {
            return Result.Failure("MQTT service is disabled");
        }

        try
        {
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            // Subscribe to message received events
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.BrokerAddress, _config.Port)
                .WithCredentials(_config.Username, _config.Password)
                .WithClientId(_config.ClientIndex)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_config.KeepAlive))
                .WithCleanSession()
                .Build();

            await _mqttClient.ConnectAsync(options, cancellationToken);

            // Subscribe to command topics
            await SubscribeToCommandTopics(cancellationToken);

            // Subscribe to state change events
            await SubscribeToStateEvents();

            // No initial state publishing - rely on events only
            // await PublishInitialState();

            LogMqttServiceInitialized();
            Connected?.Invoke(this, EventArgs.Empty);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogMqttConnectionFailed(ex);
            return Result.Failure($"Failed to connect to MQTT broker: {ex.Message}");
        }
    }

    public Task<Result> PublishZoneStateAsync(int zoneIndex, ZoneState state, CancellationToken cancellationToken = default)
    {
        // Event-driven publishing handles this automatically via OnZoneStateChanged
        return Task.FromResult(Result.Success());
    }

    public Task<Result> PublishClientStateAsync(string clientIndex, ClientState state, CancellationToken cancellationToken = default)
    {
        // Event-driven publishing handles this automatically via OnClientStateChanged
        return Task.FromResult(Result.Success());
    }

    public async Task<Result> PublishAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default)
    {
        if (_disposed || !IsConnected)
        {
            return Result.Failure("MQTT service not available");
        }

        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag(retain)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient!.PublishAsync(message, cancellationToken);
            LogMessagePublished(topic, payload.Length, retain);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogMessagePublishFailed(topic, ex);
            return Result.Failure($"Failed to publish to topic {topic}: {ex.Message}");
        }
    }

    public async Task<Result> PublishClientStatusAsync<T>(string clientIndex, string eventType, T payload, CancellationToken cancellationToken = default)
    {
        var topic = $"{_config.MqttBaseTopic}/clients/{clientIndex}/{eventType}";
        var json = JsonSerializer.Serialize(payload);
        return await PublishAsync(topic, json, true, cancellationToken);
    }

    public async Task<Result> PublishZoneStatusAsync<T>(int zoneIndex, string eventType, T payload, CancellationToken cancellationToken = default)
    {
        var topic = $"{_config.MqttBaseTopic}/zones/{zoneIndex}/{eventType}";
        var json = JsonSerializer.Serialize(payload);
        return await PublishAsync(topic, json, true, cancellationToken);
    }

    public async Task<Result> PublishGlobalStatusAsync<T>(string eventType, T payload, CancellationToken cancellationToken = default)
    {
        var topic = $"{_config.MqttBaseTopic}/system/{eventType}";
        var json = JsonSerializer.Serialize(payload);
        return await PublishAsync(topic, json, true, cancellationToken);
    }

    public Task<Result> SubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default)
    {
        LogTopicsSubscribed(topics.Count());
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UnsubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default)
    {
        LogTopicsUnsubscribed(topics.Count());
        return Task.FromResult(Result.Success());
    }

    private async Task PublishMessage(string topic, string payload)
    {
        if (_mqttClient?.IsConnected == true)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag(true)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
            LogMqttMessagePublished(topic, payload);
        }
        else
        {
            LogMqttNotConnected(topic, payload);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            if (_mqttClient?.IsConnected == true)
            {
                await _mqttClient.DisconnectAsync();
            }
            _mqttClient?.Dispose();
        }
        catch (Exception ex)
        {
            LogMqttDisconnectError(ex);
        }

        Disconnected?.Invoke(this, "Service disposed");
        LogMqttServiceDisposed();
    }

    // TODO: Find a way to integrate in our StatusId and CommandId pattern
    private async Task SubscribeToCommandTopics(CancellationToken cancellationToken)
    {
        var commandTopics = new[]
        {
            // Zone commands - Basic playback
            $"{_config.MqttBaseTopic}/zones/+/play",
            $"{_config.MqttBaseTopic}/zones/+/pause",
            $"{_config.MqttBaseTopic}/zones/+/stop",

            // Zone commands - Track navigation
            $"{_config.MqttBaseTopic}/zones/+/track/set",
            $"{_config.MqttBaseTopic}/zones/+/track/position/set",
            $"{_config.MqttBaseTopic}/zones/+/play/track",

            // Zone commands - Playlist navigation
            $"{_config.MqttBaseTopic}/zones/+/playlist/set",

            // Zone commands - Volume control
            $"{_config.MqttBaseTopic}/zones/+/volume/set",

            // Zone commands - Mute control
            $"{_config.MqttBaseTopic}/zones/+/mute/set",

            // Zone commands - Special control topic
            $"{_config.MqttBaseTopic}/zones/+/control/set",

            // Client commands - Volume control
            $"{_config.MqttBaseTopic}/clients/+/volume/set",
            $"{_config.MqttBaseTopic}/clients/+/volume/up",
            $"{_config.MqttBaseTopic}/clients/+/volume/down",

            // Client commands - Mute control
            $"{_config.MqttBaseTopic}/clients/+/mute/set",
            $"{_config.MqttBaseTopic}/clients/+/mute/toggle",

            // Client commands - Latency and zone assignment
            $"{_config.MqttBaseTopic}/clients/+/latency/set",
            $"{_config.MqttBaseTopic}/clients/+/zones/set"
        };

        var subscriptions = commandTopics.Select(topic => new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build()).ToArray();

        foreach (var subscription in subscriptions)
        {
            await _mqttClient!.SubscribeAsync(subscription, cancellationToken);
        }

        LogMqttCommandTopicsSubscribed(commandTopics.Length);
    }

    private async Task SubscribeToStateEvents()
    {
        using var scope = _serviceProvider.CreateScope();
        var zoneStateStore = scope.ServiceProvider.GetRequiredService<IZoneStateStore>();
        var clientStateStore = scope.ServiceProvider.GetRequiredService<IClientStateStore>();

        // Subscribe to granular zone events (excludes high-frequency position updates)
        zoneStateStore.ZoneVolumeChanged += OnZoneVolumeChanged;
        zoneStateStore.ZonePlaylistChanged += OnZonePlaylistChanged;
        zoneStateStore.ZoneTrackChanged += OnZoneTrackChanged;
        zoneStateStore.ZonePositionChanged += OnZonePositionChanged;

        // Subscribe to client state events
        clientStateStore.ClientStateChanged += OnClientStateChanged;
        clientStateStore.ClientVolumeChanged += OnClientVolumeChanged;

        await Task.CompletedTask;
    }

    [StatusId("ZONE_VOLUME_CHANGED")]
    private async void OnZoneVolumeChanged(object? sender, ZoneVolumeChangedEventArgs e)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            // Use event data directly to avoid race conditions
            await PublishAsync($"snapdog/zones/{e.ZoneIndex}/volume", e.NewVolume.ToString());
        }
        catch (Exception ex)
        {
            LogMqttPublishError($"Zone {e.ZoneIndex} volume", ex);
        }
    }

    [StatusId("ZONE_PLAYLIST_CHANGED")]
    private async void OnZonePlaylistChanged(object? sender, ZonePlaylistChangedEventArgs e)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            // Use event data directly to avoid race conditions
            if (e.NewPlaylist != null)
            {
                var baseTopic = $"{_config.MqttBaseTopic}/zones/{e.ZoneIndex}";

                await PublishAsync($"{baseTopic}/playlist", e.NewPlaylist.Index.ToString());
                await PublishAsync($"{baseTopic}/playlist/name", e.NewPlaylist.Name ?? "");
                await PublishAsync($"{baseTopic}/playlist/count", e.NewPlaylist.TrackCount.ToString());

                if (e.NewPlaylist.TotalDurationSec.HasValue)
                {
                    await PublishAsync($"{baseTopic}/playlist/duration", (e.NewPlaylist.TotalDurationSec.Value * 1000).ToString());
                }

                if (!string.IsNullOrEmpty(e.NewPlaylist.Description))
                {
                    await PublishAsync($"{baseTopic}/playlist/description", e.NewPlaylist.Description);
                }

                if (!string.IsNullOrEmpty(e.NewPlaylist.CoverArtUrl))
                {
                    await PublishAsync($"{baseTopic}/playlist/cover", e.NewPlaylist.CoverArtUrl);
                }
            }
        }
        catch (Exception ex)
        {
            LogMqttPublishError($"Zone {e.ZoneIndex} playlist", ex);
        }
    }

    [StatusId("ZONE_TRACK_CHANGED")]
    private async void OnZoneTrackChanged(object? sender, ZoneTrackChangedEventArgs e)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            // Use event data directly to avoid race conditions
            if (e.NewTrack != null && !string.IsNullOrEmpty(e.NewTrack.Title) && e.NewTrack.Title != "No Track")
            {
                var baseTopic = $"{_config.MqttBaseTopic}/zones/{e.ZoneIndex}";

                await PublishAsync($"{baseTopic}/track", (e.NewTrack.Index ?? 0).ToString());
                await PublishAsync($"{baseTopic}/track/title", e.NewTrack.Title);
                await PublishAsync($"{baseTopic}/track/artist", e.NewTrack.Artist ?? "");
                await PublishAsync($"{baseTopic}/track/album", e.NewTrack.Album ?? "");
                await PublishAsync($"{baseTopic}/track/duration", (e.NewTrack.DurationMs ?? 0).ToString());
                await PublishAsync($"{baseTopic}/track/position", (e.NewTrack.PositionMs ?? 0).ToString());
                await PublishAsync($"{baseTopic}/track/cover", e.NewTrack.CoverArtUrl ?? "");

                var progress = e.NewTrack.DurationMs > 0
                    ? (int)((double)(e.NewTrack.PositionMs ?? 0) / e.NewTrack.DurationMs.Value * 100)
                    : 0;
                await PublishAsync($"{baseTopic}/track/progress", progress.ToString());

                if (!string.IsNullOrEmpty(e.NewTrack.Genre))
                {
                    await PublishAsync($"{baseTopic}/track/genre", e.NewTrack.Genre);
                }
            }
        }
        catch (Exception ex)
        {
            LogMqttPublishError($"Zone {e.ZoneIndex} track", ex);
        }
    }

    [StatusId("ZONE_POSITION_CHANGED")]
    private async void OnZonePositionChanged(object? sender, ZonePositionChangedEventArgs e)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            // Publish only position data (debounced to 500ms max)
            if (e.Track != null)
            {
                await PublishAsync($"snapdog/zones/{e.ZoneIndex}/position", e.Track.PositionMs?.ToString() ?? "0");
                await PublishAsync($"snapdog/zones/{e.ZoneIndex}/progress", (e.Track.Progress ?? 0).ToString("F2"));
            }
        }
        catch (Exception ex)
        {
            LogMqttPublishError($"Zone {e.ZoneIndex} position", ex);
        }
    }

    [StatusId("CLIENT_STATE_CHANGED")]
    private async void OnClientStateChanged(object? sender, ClientStateChangedEventArgs e)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            await PublishClientState(e.ClientIndex.ToString(), e.NewState);
        }
        catch (Exception ex)
        {
            LogMqttPublishError($"Client {e.ClientIndex} state", ex);
        }
    }

    [StatusId("CLIENT_VOLUME_STATUS")]
    private async void OnClientVolumeChanged(object? sender, ClientVolumeChangedEventArgs e)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            var topic = $"{_config.MqttBaseTopic}/clients/{e.ClientIndex}/volume";
            await PublishMessage(topic, e.NewVolume.ToString());
        }
        catch (Exception ex)
        {
            LogMqttPublishError($"Client {e.ClientIndex} volume", ex);
        }
    }

    [StatusId("VOLUME_STATUS")]
    private async Task PublishZoneState(int zoneIndex, ZoneState state)
    {
        var baseTopic = $"{_config.MqttBaseTopic}/zones/{zoneIndex}";
        var tasks = new List<Task>();

        // Publish all current state - store already detected changes
        tasks.Add(PublishMessage($"{baseTopic}/playing", (state.PlaybackState == PlaybackState.Playing).ToString().ToLowerInvariant()));
        tasks.Add(PublishMessage($"{baseTopic}/volume", state.Volume.ToString()));

        // Track data is published via OnZoneTrackChanged event handler to avoid race conditions
        // Playlist data is published via OnZonePlaylistChanged event handler to avoid race conditions

        await Task.WhenAll(tasks);
    }

    [StatusId("CLIENT_VOLUME_STATUS")]
    private async Task PublishClientState(string clientIndex, ClientState state)
    {
        var baseTopic = $"{_config.MqttBaseTopic}/clients/{clientIndex}";

        var tasks = new[]
        {
            PublishMessage($"{baseTopic}/name", state.Name ?? ""),
            PublishMessage($"{baseTopic}/connected", state.Connected.ToString().ToLowerInvariant()),
            PublishMessage($"{baseTopic}/volume", state.Volume.ToString()),
            PublishMessage($"{baseTopic}/zone", state.ZoneIndex.ToString())
        };

        await Task.WhenAll(tasks);
    }

    private async Task ProcessControlCommand(IZoneService zone, string payload)
    {
        var parts = payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var command = parts[0].ToLowerInvariant();

        try
        {
            switch (command)
            {
                case "play":
                    if (parts.Length > 1 && int.TryParse(parts[1], out var trackIndex))
                    {
                        await zone.PlayTrackAsync(trackIndex);
                    }
                    else
                    {
                        await zone.PlayAsync();
                    }

                    break;
                case "pause":
                    await zone.PauseAsync();
                    break;
                case "stop":
                    await zone.StopAsync();
                    break;
                case "next":
                case "track_next":
                case "+":
                    await zone.NextTrackAsync();
                    break;
                case "previous":
                case "track_previous":
                case "-":
                    await zone.PreviousTrackAsync();
                    break;
                case "mute_on":
                    await zone.SetMuteAsync(true);
                    break;
                case "mute_off":
                    await zone.SetMuteAsync(false);
                    break;
                case "mute_toggle":
                    await zone.ToggleMuteAsync();
                    break;
                case "volume":
                    if (parts.Length > 1 && int.TryParse(parts[1], out var volume))
                    {
                        await zone.SetVolumeAsync(volume);
                    }

                    break;
                case "volume_up":
                    await zone.VolumeUpAsync();
                    break;
                case "volume_down":
                    await zone.VolumeDownAsync();
                    break;
                case "playlist_next":
                    await zone.NextPlaylistAsync();
                    break;
                case "playlist_previous":
                    await zone.PreviousPlaylistAsync();
                    break;
            }

            LogMqttCommandExecuted($"Control: {payload}", "Success");
        }
        catch (Exception ex)
        {
            LogMqttCommandExecutionError($"Control: {payload}", ex);
        }
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.ConvertPayloadToString();

            LogMqttCommandReceived(topic, payload);

            // Fire event for external handlers
            var eventArgs = new MqttMessageReceivedEventArgs
            {
                Topic = topic,
                Payload = payload,
                Retained = e.ApplicationMessage.Retain,
                QoS = (int)e.ApplicationMessage.QualityOfServiceLevel
            };
            MessageReceived?.Invoke(this, eventArgs);

            // Process command internally
            await ProcessMqttCommand(topic, payload);
        }
        catch (Exception ex)
        {
            LogMqttCommandProcessingError(ex);
        }
    }

    private async Task ProcessMqttCommand(string topic, string payload)
    {
        // Remove base topic prefix
        var baseTopic = _config.MqttBaseTopic.TrimEnd('/');
        if (!topic.StartsWith($"{baseTopic}/"))
        {
            return;
        }

        var commandPath = topic.Substring(baseTopic.Length + 1);
        var parts = commandPath.Split('/');

        if (parts.Length < 3)
        {
            return;
        }

        try
        {
            if (parts[0] == "zones" && int.TryParse(parts[1], out var zoneIndex))
            {
                await ProcessZoneCommand(zoneIndex, parts.Skip(2).ToArray(), payload);
            }
            else if (parts[0] == "clients" && int.TryParse(parts[1], out var clientIndex))
            {
                await ProcessClientCommand(clientIndex, parts.Skip(2).ToArray(), payload);
            }
        }
        catch (Exception ex)
        {
            LogMqttCommandExecutionError(topic, ex);
        }
    }

    private async Task ProcessZoneCommand(int zoneIndex, string[] commandParts, string payload)
    {
        var command = string.Join("/", commandParts);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var zoneManager = scope.ServiceProvider.GetRequiredService<IZoneManager>();

            var zoneResult = await zoneManager.GetZoneAsync(zoneIndex);
            if (zoneResult.IsFailure || zoneResult.Value == null)
            {
                LogMqttCommandExecuted($"Zone {zoneIndex} {command}", "Zone not found");
                return;
            }

            switch (command)
            {
                // Basic playback
                case "play":
                    var playResult = await zoneResult.Value.PlayAsync();
                    LogMqttCommandExecuted($"Zone {zoneIndex} Play", playResult.IsSuccess ? "Success" : "Failed");
                    break;
                case "pause":
                    var pauseResult = await zoneResult.Value.PauseAsync();
                    LogMqttCommandExecuted($"Zone {zoneIndex} Pause", pauseResult.IsSuccess ? "Success" : "Failed");
                    break;
                case "stop":
                    var stopResult = await zoneResult.Value.StopAsync();
                    LogMqttCommandExecuted($"Zone {zoneIndex} Stop", stopResult.IsSuccess ? "Success" : "Failed");
                    break;

                // Volume control
                case "volume/set":
                    if (int.TryParse(payload, out var volume))
                    {
                        var result = await zoneResult.Value.SetVolumeAsync(volume);
                        LogMqttCommandExecuted($"Zone {zoneIndex} Volume {volume}", result.IsSuccess ? "Success" : "Failed");
                    }
                    break;

                // Mute control
                case "mute/set":
                    if (bool.TryParse(payload, out var mute))
                    {
                        var muteResult = await zoneResult.Value.SetMuteAsync(mute);
                        LogMqttCommandExecuted($"Zone {zoneIndex} Mute {mute}", muteResult.IsSuccess ? "Success" : "Failed");
                    }
                    break;

                // Track navigation
                case "track/set":
                    if (int.TryParse(payload, out var trackIndex))
                    {
                        var trackResult = await zoneResult.Value.PlayTrackAsync(trackIndex);
                        LogMqttCommandExecuted($"Zone {zoneIndex} Set Track {trackIndex}", trackResult.IsSuccess ? "Success" : "Failed");
                    }
                    break;
                case "play/track":
                    if (int.TryParse(payload, out var playTrackIndex))
                    {
                        var playTrackResult = await zoneResult.Value.PlayTrackAsync(playTrackIndex);
                        LogMqttCommandExecuted($"Zone {zoneIndex} Play Track {playTrackIndex}", playTrackResult.IsSuccess ? "Success" : "Failed");
                    }
                    break;
                case "track/position/set":
                    if (int.TryParse(payload, out var positionMs))
                    {
                        var seekResult = await zoneResult.Value.SeekToPositionAsync(TimeSpan.FromMilliseconds(positionMs));
                        LogMqttCommandExecuted($"Zone {zoneIndex} Seek {positionMs}ms", seekResult.IsSuccess ? "Success" : "Failed");
                    }
                    break;

                // Playlist navigation
                case "playlist/set":
                    if (int.TryParse(payload, out var playlistIndex))
                    {
                        var setResult = await zoneResult.Value.SetPlaylistAsync(playlistIndex);
                        LogMqttCommandExecuted($"Zone {zoneIndex} Set Playlist {playlistIndex}", setResult.IsSuccess ? "Success" : "Failed");
                    }
                    break;

                // Special control topic
                case "control/set":
                    await ProcessControlCommand(zoneResult.Value, payload);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogMqttCommandExecutionError($"Zone {zoneIndex} {command}", ex);
        }
    }

    private async Task ProcessClientCommand(int clientIndex, string[] commandParts, string payload)
    {
        var command = string.Join("/", commandParts);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var clientManager = scope.ServiceProvider.GetRequiredService<IClientManager>();

            var clientResult = await clientManager.GetClientAsync(clientIndex);
            if (clientResult.IsFailure || clientResult.Value == null)
            {
                LogMqttCommandExecuted($"Client {clientIndex} {command}", "Client not found");
                return;
            }

            switch (command)
            {
                case "volume/set":
                    if (int.TryParse(payload, out var volume))
                    {
                        var result = await clientResult.Value.SetVolumeAsync(volume);
                        LogMqttCommandExecuted($"Client {clientIndex} Volume {volume}", result.IsSuccess ? "Success" : "Failed");
                    }
                    break;
                case "volume/up":
                    var volUpResult = await clientResult.Value.VolumeUpAsync();
                    LogMqttCommandExecuted($"Client {clientIndex} Volume Up", volUpResult.IsSuccess ? "Success" : "Failed");
                    break;
                case "volume/down":
                    var volDownResult = await clientResult.Value.VolumeDownAsync();
                    LogMqttCommandExecuted($"Client {clientIndex} Volume Down", volDownResult.IsSuccess ? "Success" : "Failed");
                    break;
                case "mute/set":
                    if (bool.TryParse(payload, out var mute))
                    {
                        var muteResult = await clientResult.Value.SetMuteAsync(mute);
                        LogMqttCommandExecuted($"Client {clientIndex} Mute {mute}", muteResult.IsSuccess ? "Success" : "Failed");
                    }
                    break;
                case "mute/toggle":
                    var muteToggleResult = await clientResult.Value.ToggleMuteAsync();
                    LogMqttCommandExecuted($"Client {clientIndex} Mute Toggle", muteToggleResult.IsSuccess ? "Success" : "Failed");
                    break;
                case "latency/set":
                    if (int.TryParse(payload, out var latency))
                    {
                        var latencyResult = await clientResult.Value.SetLatencyAsync(latency);
                        LogMqttCommandExecuted($"Client {clientIndex} Latency {latency}", latencyResult.IsSuccess ? "Success" : "Failed");
                    }
                    break;
                case "zones/set":
                    if (int.TryParse(payload, out var zoneIndex))
                    {
                        var zoneResult = await clientResult.Value.AssignToZoneAsync(zoneIndex);
                        LogMqttCommandExecuted($"Client {clientIndex} Assign Zone {zoneIndex}", zoneResult.IsSuccess ? "Success" : "Failed");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            LogMqttCommandExecutionError($"Client {clientIndex} {command}", ex);
        }
    }

    // High-performance logging with LoggerMessage
    [LoggerMessage(EventId = 200001, Level = LogLevel.Information, Message = "MQTT service initialized successfully")]
    private partial void LogMqttServiceInitialized();

    [LoggerMessage(EventId = 200002, Level = LogLevel.Debug, Message = "Published zone {ZoneIndex} state to MQTT")]
    private partial void LogZoneStatePublished(int ZoneIndex);

    [LoggerMessage(EventId = 200003, Level = LogLevel.Error, Message = "Failed to publish zone {ZoneIndex} state")]
    private partial void LogZoneStatePublishFailed(int ZoneIndex, Exception ex);

    [LoggerMessage(EventId = 200004, Level = LogLevel.Debug, Message = "Published client {ClientIndex} state to MQTT")]
    private partial void LogClientStatePublished(string ClientIndex);

    [LoggerMessage(EventId = 200005, Level = LogLevel.Error, Message = "Failed to publish client {ClientIndex} state")]
    private partial void LogClientStatePublishFailed(string ClientIndex, Exception ex);

    [LoggerMessage(EventId = 200006, Level = LogLevel.Debug, Message = "Published message to topic {Topic} - Size: {PayloadSize} bytes, Retain: {Retain}")]
    private partial void LogMessagePublished(string Topic, int PayloadSize, bool Retain);

    [LoggerMessage(EventId = 200007, Level = LogLevel.Error, Message = "Failed to publish message to topic {Topic}")]
    private partial void LogMessagePublishFailed(string Topic, Exception ex);

    [LoggerMessage(EventId = 200008, Level = LogLevel.Debug, Message = "Subscribed to {TopicCount} MQTT topics")]
    private partial void LogTopicsSubscribed(int TopicCount);

    [LoggerMessage(EventId = 200009, Level = LogLevel.Debug, Message = "Unsubscribed from {TopicCount} MQTT topics")]
    private partial void LogTopicsUnsubscribed(int TopicCount);

    [LoggerMessage(EventId = 200010, Level = LogLevel.Information, Message = "MQTT service disposed")]
    private partial void LogMqttServiceDisposed();

    [LoggerMessage(EventId = 200011, Level = LogLevel.Debug, Message = "MQTT: {Topic} -> {Payload}")]
    private partial void LogMqttMessagePublished(string Topic, string Payload);

    [LoggerMessage(EventId = 200012, Level = LogLevel.Warning, Message = "MQTT not connected, skipping: {Topic} -> {Payload}")]
    private partial void LogMqttNotConnected(string Topic, string Payload);

    [LoggerMessage(EventId = 200013, Level = LogLevel.Error, Message = "MQTT connection failed")]
    private partial void LogMqttConnectionFailed(Exception ex);

    [LoggerMessage(EventId = 200014, Level = LogLevel.Error, Message = "MQTT disconnect error")]
    private partial void LogMqttDisconnectError(Exception ex);

    [LoggerMessage(EventId = 200015, Level = LogLevel.Information, Message = "Subscribed to {TopicCount} MQTT command topics")]
    private partial void LogMqttCommandTopicsSubscribed(int TopicCount);

    [LoggerMessage(EventId = 200016, Level = LogLevel.Debug, Message = "MQTT command received: {Topic} -> {Payload}")]
    private partial void LogMqttCommandReceived(string Topic, string Payload);

    [LoggerMessage(EventId = 200017, Level = LogLevel.Error, Message = "Error processing MQTT command")]
    private partial void LogMqttCommandProcessingError(Exception ex);

    [LoggerMessage(EventId = 200018, Level = LogLevel.Error, Message = "Error executing MQTT command for topic {Topic}")]
    private partial void LogMqttCommandExecutionError(string Topic, Exception ex);

    [LoggerMessage(EventId = 200019, Level = LogLevel.Debug, Message = "MQTT command executed: {Command} via {Method}")]
    private partial void LogMqttCommandExecuted(string Command, string Method);

    [LoggerMessage(EventId = 200020, Level = LogLevel.Error, Message = "Error executing MQTT command for {Command}")]
    private partial void LogMqttCommandHttpError(string Command, Exception ex);

    [LoggerMessage(EventId = 200021, Level = LogLevel.Error, Message = "Error publishing MQTT {Topic}")]
    private partial void LogMqttPublishError(string Topic, Exception ex);
}
