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

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator;
using Cortex.Mediator.Commands;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using Polly;
using Polly.Retry;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Constants;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Helpers;
using SnapDog2.Core.Mappers;
using SnapDog2.Core.Models;
using SnapDog2.Core.Models.Mqtt;
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Playlist;
using SnapDog2.Server.Features.Zones.Commands.Track;
using SnapDog2.Server.Features.Zones.Commands.Volume;

/// <summary>
/// Enterprise-grade MQTT service implementation using MQTTnet v5.
/// Provides bi-directional MQTT communication with Polly resilience policies,
/// configurable topic structure, and comprehensive error handling.
/// </summary>
public sealed partial class MqttService : IMqttService, IAsyncDisposable
{
    private readonly MqttConfig _config;
    private readonly SystemConfig _systemConfig;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MqttService> _logger;
    private readonly List<ZoneConfig> _zoneConfigs;
    private readonly List<ClientConfig> _clientConfigs;
    private readonly ResiliencePipeline _connectionPolicy;
    private readonly ResiliencePipeline _operationPolicy;

    /// <summary>
    /// Cache of last published MQTT zone states to detect changes.
    /// Key: zoneIndex, Value: last published MqttZoneState
    /// </summary>
    private readonly ConcurrentDictionary<int, MqttZoneState> _lastPublishedZoneStates = new();

    /// <summary>
    /// Constructs a full MQTT topic using the configured base topic.
    /// </summary>
    /// <param name="topicSuffix">The topic suffix (e.g., "zone/1/volume")</param>
    /// <returns>Full topic with base prefix (e.g., "snapdog/zones/1/volume")</returns>
    private string BuildTopic(string topicSuffix)
    {
        var baseTopic = _config.MqttBaseTopic.TrimEnd('/');
        return $"{baseTopic}/{topicSuffix}";
    }

    /// <summary>
    /// Extracts the topic suffix by removing the configured base topic prefix.
    /// </summary>
    /// <param name="fullTopic">The full MQTT topic</param>
    /// <returns>Topic suffix without base prefix, or null if doesn't match base</returns>
    private string? ExtractTopicSuffix(string fullTopic)
    {
        var baseTopic = _config.MqttBaseTopic.TrimEnd('/');
        var expectedPrefix = $"{baseTopic}/";

        if (fullTopic.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return fullTopic[expectedPrefix.Length..];
        }

        return null;
    }

    private IMqttClient? _mqttClient;
    private bool _initialized = false;
    private bool _disposed = false;

    public MqttService(
        IOptions<SnapDogConfiguration> configOptions,
        IOptions<List<ZoneConfig>> zoneConfigOptions,
        IOptions<List<ClientConfig>> clientConfigOptions,
        IServiceProvider serviceProvider,
        ILogger<MqttService> logger
    )
    {
        this._config = configOptions.Value.Services.Mqtt;
        this._systemConfig = configOptions.Value.System;
        this._serviceProvider = serviceProvider;
        this._logger = logger;
        this._zoneConfigs = zoneConfigOptions.Value;
        this._clientConfigs = clientConfigOptions.Value;

        // Configure resilience policies
        this._connectionPolicy = this.CreateConnectionPolicy();
        this._operationPolicy = this.CreateOperationPolicy();

        this.LogServiceCreated(this._config.BrokerAddress, this._config.Port, this._config.Enabled);
    }

    #region Logging

    [LoggerMessage(
        EventId = 4401,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "MQTT service created for {BrokerAddress}:{Port}, enabled: {Enabled}"
    )]
    private partial void LogServiceCreated(string brokerAddress, int port, bool enabled);

    [LoggerMessage(
        EventId = 4402,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Initializing MQTT connection to {BrokerAddress}:{Port}"
    )]
    private partial void LogInitializing(string brokerAddress, int port);

    [LoggerMessage(
        EventId = 4403,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "MQTT connection established successfully"
    )]
    private partial void LogConnectionEstablished();

    [LoggerMessage(
        EventId = 4404,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "MQTT connection lost: {Reason}"
    )]
    private partial void LogConnectionLost(string reason);

    [LoggerMessage(
        EventId = 4405,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to initialize MQTT connection"
    )]
    private partial void LogInitializationFailed(Exception ex);

    [LoggerMessage(
        EventId = 4409,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "MQTT connection error: {ErrorMessage}"
    )]
    private partial void LogConnectionErrorMessage(string errorMessage);

    [LoggerMessage(
        EventId = 4406,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "MQTT operation {Operation} failed"
    )]
    private partial void LogOperationFailed(string operation, Exception ex);

    [LoggerMessage(
        EventId = 4407,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "MQTT service disposed"
    )]
    private partial void LogServiceDisposed();

    [LoggerMessage(
        EventId = 4408,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "MQTT service not connected for operation: {Operation}"
    )]
    private partial void LogNotConnected(string operation);

    [LoggerMessage(
        EventId = 4400,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "🚀 Attempting MQTT connection to {BrokerAddress}:{Port} (attempt {AttemptNumber}/{MaxAttempts}: {ErrorMessage})"
    )]
    private partial void LogConnectionRetryAttempt(
        string brokerAddress,
        int port,
        int attemptNumber,
        int maxAttempts,
        string errorMessage
    );

    #endregion

    #region Properties

    public bool IsConnected => this._mqttClient?.IsConnected ?? false;

    public event EventHandler? Connected;
    public event EventHandler<string>? Disconnected;
    public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;

    #endregion

    #region Helper Methods

    private async Task PublishNotificationAsync<T>(T notification)
        where T : INotification
    {
        // Avoid noisy errors during shutdown when the root provider is disposed
        if (this._disposed)
        {
            return;
        }
        try
        {
            using var scope = this._serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.PublishAsync(notification);
        }
        catch (ObjectDisposedException)
        {
            // Benign during shutdown
            return;
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("PublishNotification", ex);
        }
    }

    /// <summary>
    /// Creates resilience policy for connection operations.
    /// </summary>
    private ResiliencePipeline CreateConnectionPolicy()
    {
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Connection);

        var builder = new ResiliencePipelineBuilder();

        // Add retry policy with logging
        if (validatedConfig.MaxRetries > 0)
        {
            builder.AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = validatedConfig.MaxRetries,
                    Delay = TimeSpan.FromMilliseconds(validatedConfig.RetryDelayMs),
                    BackoffType = validatedConfig.BackoffType?.ToLowerInvariant() switch
                    {
                        "linear" => DelayBackoffType.Linear,
                        "constant" => DelayBackoffType.Constant,
                        _ => DelayBackoffType.Exponential,
                    },
                    UseJitter = validatedConfig.UseJitter,
                    // Explicitly handle all exceptions - MQTT connection issues should be retried
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    OnRetry = args =>
                    {
                        this.LogConnectionRetryAttempt(
                            this._config.BrokerAddress,
                            this._config.Port,
                            args.AttemptNumber + 1,
                            validatedConfig.MaxRetries + 1,
                            args.Outcome.Exception?.Message ?? "Unknown error"
                        );
                        return ValueTask.CompletedTask;
                    },
                }
            );
        }

        // Add timeout policy
        if (validatedConfig.TimeoutSeconds > 0)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(validatedConfig.TimeoutSeconds));
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates resilience policy for operation calls.
    /// </summary>
    private ResiliencePipeline CreateOperationPolicy()
    {
        var validatedConfig = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Operation);
        return ResiliencePolicyFactory.CreatePipeline(validatedConfig, "MQTT-Operation");
    }

    #endregion

    #region Interface Implementation

    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (this._disposed)
        {
            return Result.Failure("Service has been disposed");
        }

        if (this._initialized)
        {
            return Result.Success();
        }

        if (!this._config.Enabled)
        {
            this.LogMqttIntegrationIsDisabled();
            return Result.Success();
        }

        try
        {
            this.LogInitializing(this._config.BrokerAddress, this._config.Port);

            // Log first attempt before Polly execution
            var config = ResiliencePolicyFactory.ValidateAndNormalize(this._config.Resilience.Connection);
            this.LogConnectionRetryAttempt(
                this._config.BrokerAddress,
                this._config.Port,
                1,
                config.MaxRetries + 1,
                "Initial attempt"
            );

            var result = await this._connectionPolicy.ExecuteAsync(
                async (ct) =>
                {
                    // Create MQTT client using v5 API
                    var factory = new MqttClientFactory();
                    this._mqttClient = factory.CreateMqttClient();

                    // Configure client options
                    var optionsBuilder = new MqttClientOptionsBuilder()
                        .WithTcpServer(this._config.BrokerAddress, this._config.Port)
                        .WithClientId(this._config.ClientIndex)
                        .WithKeepAlivePeriod(TimeSpan.FromSeconds(this._config.KeepAlive))
                        .WithCleanSession(true);

                    // Add authentication if configured
                    if (!string.IsNullOrEmpty(this._config.Username))
                    {
                        optionsBuilder.WithCredentials(this._config.Username, this._config.Password);
                    }

                    var options = optionsBuilder.Build();

                    // Set up event handlers
                    this._mqttClient.ConnectedAsync += this.OnConnectedAsync;
                    this._mqttClient.DisconnectedAsync += this.OnDisconnectedAsync;
                    this._mqttClient.ApplicationMessageReceivedAsync += this.OnApplicationMessageReceivedAsync;

                    // Connect to broker
                    await this._mqttClient.ConnectAsync(options, ct);

                    this._initialized = true;
                    this.LogConnectionEstablished();

                    return Result.Success();
                },
                cancellationToken
            );

            if (result.IsSuccess)
            {
                // Publish connection established notification
                await this.PublishNotificationAsync(new MqttConnectionEstablishedNotification());
            }

            return result;
        }
        catch (Exception ex)
        {
            // For common MQTT connection errors, only log the message without stack trace to reduce noise
            if (
                ex is System.Net.Sockets.SocketException
                || ex is TimeoutException
                || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("broker", StringComparison.OrdinalIgnoreCase)
            )
            {
                this.LogConnectionErrorMessage(ex.Message);
            }
            else
            {
                this.LogInitializationFailed(ex);
            }
            return Result.Failure($"Failed to initialize MQTT connection: {ex.Message}");
        }
    }

    public async Task<Result> PublishZoneStateAsync(
        int zoneIndex,
        ZoneState state,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            return Result.Failure("MQTT client is not connected");
        }

        // Get zone config (1-based indexing)
        var zoneConfigIndex = zoneIndex - 1;
        if (zoneConfigIndex < 0 || zoneConfigIndex >= this._zoneConfigs.Count)
        {
            return Result.Failure($"Zone {zoneIndex} not configured for MQTT");
        }

        try
        {
            // Convert to simplified MQTT format
            var mqttZoneState = MqttStateMapper.ToMqttZoneState(state);

            // Check if this represents a meaningful change
            var lastPublished = _lastPublishedZoneStates.GetValueOrDefault(zoneIndex);
            if (!MqttStateMapper.HasMeaningfulChange(lastPublished, mqttZoneState))
            {
                // No meaningful change, skip publishing
                return Result.Success();
            }

            // Use simple zone state topic that matches blueprint pattern
            var stateTopic = BuildTopic($"zone/{zoneIndex}/state");

            // Publish simplified state as JSON
            var stateJson = JsonSerializer.Serialize(
                mqttZoneState,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
            await this.PublishAsync(stateTopic, stateJson, true, cancellationToken);

            // Cache the published state for future change detection
            _lastPublishedZoneStates[zoneIndex] = mqttZoneState;

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("PublishZoneState", ex);
            return Result.Failure($"Failed to publish zone state: {ex.Message}");
        }
    }

    public async Task<Result> PublishClientStateAsync(
        string clientIndex,
        ClientState state,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            return Result.Failure("MQTT client is not connected");
        }

        // Parse client ID - now expecting integer format (1, 2, 3, etc.)
        if (!int.TryParse(clientIndex, out var parsedClientIndex))
        {
            return Result.Failure($"Invalid client ID format: {clientIndex}. Expected integer.");
        }

        // Convert to 0-based index for configuration array
        var configIndex = parsedClientIndex - 1;
        if (configIndex < 0 || configIndex >= this._clientConfigs.Count)
        {
            return Result.Failure($"Client {clientIndex} not configured for MQTT");
        }

        try
        {
            // Use simple client state topic that matches blueprint pattern
            var clientStateTopic = BuildTopic($"client/{parsedClientIndex}/state");

            // Publish comprehensive state as JSON
            var stateJson = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
            await this.PublishAsync(clientStateTopic, stateJson, true, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("PublishClientState", ex);
            return Result.Failure($"Failed to publish client state: {ex.Message}");
        }
    }

    public async Task<Result> PublishAsync(
        string topic,
        string payload,
        bool retain = false,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            this.LogNotConnected(nameof(this.PublishAsync));
            return Result.Failure("MQTT client is not connected");
        }

        try
        {
            return await this._operationPolicy.ExecuteAsync(
                async (ct) =>
                {
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(payload)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithRetainFlag(retain)
                        .Build();

                    await this._mqttClient!.PublishAsync(message, ct);
                    return Result.Success();
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("Publish", ex);
            return Result.Failure($"Failed to publish message: {ex.Message}");
        }
    }

    public async Task<Result> SubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default)
    {
        if (!this.IsConnected)
        {
            return Result.Failure("MQTT client is not connected");
        }

        try
        {
            foreach (var topic in topics)
            {
                await this._mqttClient!.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("Subscribe", ex);
            return Result.Failure($"Failed to subscribe to topics: {ex.Message}");
        }
    }

    public async Task<Result> UnsubscribeAsync(
        IEnumerable<string> topics,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.IsConnected)
        {
            return Result.Failure("MQTT client is not connected");
        }

        try
        {
            foreach (var topic in topics)
            {
                await this._mqttClient!.UnsubscribeAsync(topic, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("Unsubscribe", ex);
            return Result.Failure($"Failed to unsubscribe from topics: {ex.Message}");
        }
    }

    #endregion

    #region Event Handlers

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        this.Connected?.Invoke(this, EventArgs.Empty);
        await this.PublishNotificationAsync(new MqttConnectionEstablishedNotification());
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        var reason = args.Reason.ToString();
        this.LogConnectionLost(reason);
        this.Disconnected?.Invoke(this, reason);
        await this.PublishNotificationAsync(new MqttConnectionLostNotification(reason));
    }

    private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var payload =
                args.ApplicationMessage.Payload.Length > 0
                    ? Encoding.UTF8.GetString(
                        args.ApplicationMessage.Payload.IsSingleSegment
                            ? args.ApplicationMessage.Payload.FirstSpan
                            : args.ApplicationMessage.Payload.ToArray()
                    )
                    : string.Empty;

            var eventArgs = new MqttMessageReceivedEventArgs
            {
                Topic = topic,
                Payload = payload,
                Retained = args.ApplicationMessage.Retain,
                QoS = (int)args.ApplicationMessage.QualityOfServiceLevel,
            };
            this.MessageReceived?.Invoke(this, eventArgs);

            // Process the message and convert to command (placeholder for future implementation)
            await this.ProcessIncomingMessageAsync(topic, payload);
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("ProcessMessage", ex);
        }
    }

    private async Task ProcessIncomingMessageAsync(string topic, string payload)
    {
        try
        {
            this.LogProcessingMqttMessageOnTopic(topic, payload);

            // Map topic to command and execute via Mediator
            var command = this.MapTopicToCommand(topic, payload);
            if (command != null)
            {
                using var scope = this._serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<Cortex.Mediator.IMediator>();

                // Send the command - the mediator will handle the type resolution
                switch (command)
                {
                    case SetZoneVolumeCommand zoneVolumeCmd:
                        await mediator.SendCommandAsync<SetZoneVolumeCommand, Result>(zoneVolumeCmd);
                        break;
                    case SetZoneMuteCommand zoneMuteCmd:
                        await mediator.SendCommandAsync<SetZoneMuteCommand, Result>(zoneMuteCmd);
                        break;
                    case SetClientVolumeCommand clientVolumeCmd:
                        await mediator.SendCommandAsync<SetClientVolumeCommand, Result>(clientVolumeCmd);
                        break;
                    case SetClientMuteCommand clientMuteCmd:
                        await mediator.SendCommandAsync<SetClientMuteCommand, Result>(clientMuteCmd);
                        break;
                    case SetClientLatencyCommand clientLatencyCmd:
                        await mediator.SendCommandAsync<SetClientLatencyCommand, Result>(clientLatencyCmd);
                        break;
                    default:
                        this.LogUnknownCommandType(command.GetType().Name);
                        break;
                }

                this.LogSuccessfullyProcessedMqttCommandForTopic(topic);
            }
            else
            {
                this.LogNoCommandMappingFoundForTopic(topic);
            }
        }
        catch (Exception ex)
        {
            this.LogFailedToProcessMqttMessageOnTopic(ex, topic);
        }
    }

    /// <summary>
    /// Maps MQTT topics to Cortex.Mediator commands based on the topic structure.
    /// Implements complete command set as specified in blueprint Section 15.
    /// </summary>
    private ICommand<Result>? MapTopicToCommand(string topic, string payload)
    {
        try
        {
            // Extract topic suffix and parse: {zone|client}/{index}/{command}
            var topicSuffix = ExtractTopicSuffix(topic);
            if (topicSuffix == null)
            {
                this.LogNoCommandMappingFoundForTopic(topic);
                return null;
            }

            var topicParts = topicSuffix.Split('/');
            if (topicParts.Length < 3)
            {
                return null;
            }

            var entityType = topicParts[0].ToLowerInvariant();
            var entityId = topicParts[1];
            var command = topicParts[2].ToLowerInvariant();

            return entityType switch
            {
                "zone" when int.TryParse(entityId, out var zoneIndex) => this.MapZoneCommand(
                    zoneIndex,
                    command,
                    payload
                ),
                "client" when int.TryParse(entityId, out var clientIndex) => MapClientCommand(
                    clientIndex,
                    command,
                    payload
                ),
                _ => null,
            };
        }
        catch (Exception ex)
        {
            this.LogFailedToMapTopicToCommand(ex, topic);
            return null;
        }
    }

    /// <summary>
    /// Maps zone-specific MQTT commands to Mediator commands.
    /// Now includes registry validation to prevent hardcoded command usage.
    /// </summary>
    private ICommand<Result>? MapZoneCommand(int zoneIndex, string command, string payload)
    {
        // Map MQTT command strings to registry command IDs for validation
        var commandId = command switch
        {
            "play" => "PLAY",
            "pause" => "PAUSE",
            "stop" => "STOP",
            "volume" => "ZONE_VOLUME",
            "mute" => "ZONE_MUTE",
            "track" => "TRACK_SET",
            "next" => "TRACK_NEXT",
            "previous" => "TRACK_PREVIOUS",
            "playlist" => "PLAYLIST_SET",
            _ => null,
        };

        // Validate command ID against registry
        if (commandId != null && !CommandIdRegistry.IsRegistered(commandId))
        {
            this.LogUnknownMqttCommand(command, commandId);
            return null;
        }
        else if (commandId == null)
        {
            // Command not mapped - this is normal for unsupported commands
            return null;
        }

        return command switch
        {
            // Playback Control Commands (Section 14.3.1)
            "play" => CreatePlayCommand(zoneIndex, payload),
            "pause" => new PauseCommand { ZoneIndex = zoneIndex, Source = CommandSource.Mqtt },
            "stop" => new StopCommand { ZoneIndex = zoneIndex, Source = CommandSource.Mqtt },

            // Volume Control Commands
            "volume" when TryParseVolume(payload, out var volume) => new SetZoneVolumeCommand
            {
                ZoneIndex = zoneIndex,
                Volume = volume,
                Source = CommandSource.Mqtt,
            },
            "volume" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => new VolumeUpCommand
            {
                ZoneIndex = zoneIndex,
                Step = 5, // Default step
                Source = CommandSource.Mqtt,
            },
            "volume" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => new VolumeDownCommand
            {
                ZoneIndex = zoneIndex,
                Step = 5, // Default step
                Source = CommandSource.Mqtt,
            },
            "volume" when TryParseVolumeStep(payload, out var step, out var direction) => direction > 0
                ? new VolumeUpCommand
                {
                    ZoneIndex = zoneIndex,
                    Step = step,
                    Source = CommandSource.Mqtt,
                }
                : new VolumeDownCommand
                {
                    ZoneIndex = zoneIndex,
                    Step = step,
                    Source = CommandSource.Mqtt,
                },

            // Mute Control Commands
            "mute" when TryParseBool(payload, out var mute) => new SetZoneMuteCommand
            {
                ZoneIndex = zoneIndex,
                Enabled = mute,
                Source = CommandSource.Mqtt,
            },
            "mute" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) => new ToggleZoneMuteCommand
            {
                ZoneIndex = zoneIndex,
                Source = CommandSource.Mqtt,
            },

            // Track Management Commands
            "track" when int.TryParse(payload, out var trackIndex) && trackIndex > 0 => new SetTrackCommand
            {
                ZoneIndex = zoneIndex,
                TrackIndex = trackIndex,
                Source = CommandSource.Mqtt,
            },
            "track" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => new NextTrackCommand
            {
                ZoneIndex = zoneIndex,
                Source = CommandSource.Mqtt,
            },
            "track" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => new PreviousTrackCommand
            {
                ZoneIndex = zoneIndex,
                Source = CommandSource.Mqtt,
            },
            "next" => new NextTrackCommand { ZoneIndex = zoneIndex, Source = CommandSource.Mqtt },
            "previous" => new PreviousTrackCommand { ZoneIndex = zoneIndex, Source = CommandSource.Mqtt },

            // Track Repeat Commands
            "track_repeat" when TryParseBool(payload, out var repeat) => new SetTrackRepeatCommand
            {
                ZoneIndex = zoneIndex,
                Enabled = repeat,
                Source = CommandSource.Mqtt,
            },
            "track_repeat" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                new ToggleTrackRepeatCommand { ZoneIndex = zoneIndex, Source = CommandSource.Mqtt },

            // Playlist Management Commands
            "playlist" when int.TryParse(payload, out var playlistIndex) && playlistIndex > 0 => new SetPlaylistCommand
            {
                ZoneIndex = zoneIndex,
                PlaylistIndex = playlistIndex,
                Source = CommandSource.Mqtt,
            },
            "playlist" when payload.Equals("+", StringComparison.OrdinalIgnoreCase) => new NextPlaylistCommand
            {
                ZoneIndex = zoneIndex,
                Source = CommandSource.Mqtt,
            },
            "playlist" when payload.Equals("-", StringComparison.OrdinalIgnoreCase) => new PreviousPlaylistCommand
            {
                ZoneIndex = zoneIndex,
                Source = CommandSource.Mqtt,
            },

            // Playlist Mode Commands
            "playlist_shuffle" when TryParseBool(payload, out var shuffle) => new SetPlaylistShuffleCommand
            {
                ZoneIndex = zoneIndex,
                Enabled = shuffle,
                Source = CommandSource.Mqtt,
            },
            "playlist_shuffle" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                new TogglePlaylistShuffleCommand { ZoneIndex = zoneIndex, Source = CommandSource.Mqtt },
            "playlist_repeat" when TryParseBool(payload, out var playlistRepeat) => new SetPlaylistRepeatCommand
            {
                ZoneIndex = zoneIndex,
                Enabled = playlistRepeat,
                Source = CommandSource.Mqtt,
            },
            "playlist_repeat" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) =>
                new TogglePlaylistRepeatCommand { ZoneIndex = zoneIndex, Source = CommandSource.Mqtt },

            _ => null,
        };
    }

    /// <summary>
    /// Maps client-specific MQTT commands to Mediator commands.
    /// </summary>
    private static ICommand<Result>? MapClientCommand(int clientIndex, string command, string payload)
    {
        return command switch
        {
            // Volume Control Commands
            "volume" when TryParseVolume(payload, out var volume) => new SetClientVolumeCommand
            {
                ClientIndex = clientIndex,
                Volume = volume,
                Source = CommandSource.Mqtt,
            },

            // Mute Control Commands
            "mute" when TryParseBool(payload, out var mute) => new SetClientMuteCommand
            {
                ClientIndex = clientIndex,
                Enabled = mute,
                Source = CommandSource.Mqtt,
            },
            "mute" when payload.Equals("toggle", StringComparison.OrdinalIgnoreCase) => new ToggleClientMuteCommand
            {
                ClientIndex = clientIndex,
                Source = CommandSource.Mqtt,
            },

            // Configuration Commands
            "latency" when int.TryParse(payload, out var latency) && latency >= 0 => new SetClientLatencyCommand
            {
                ClientIndex = clientIndex,
                LatencyMs = latency,
                Source = CommandSource.Mqtt,
            },
            "zone" when int.TryParse(payload, out var zoneIndex) && zoneIndex > 0 => new AssignClientToZoneCommand
            {
                ClientIndex = clientIndex,
                ZoneIndex = zoneIndex,
                Source = CommandSource.Mqtt,
            },

            _ => null,
        };
    }

    /// <summary>
    /// Creates a PlayCommand with optional parameters based on payload.
    /// Supports different play command formats specification.
    /// </summary>
    private static PlayCommand CreatePlayCommand(int zoneIndex, string payload)
    {
        // Handle different play command formats:
        // "play" - simple play
        // "play url <url>" - play specific URL
        // "play track <index>" - play specific track

        if (string.IsNullOrWhiteSpace(payload) || payload.Equals("play", StringComparison.OrdinalIgnoreCase))
        {
            return new PlayCommand { ZoneIndex = zoneIndex, Source = CommandSource.Mqtt };
        }

        var parts = payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return parts[0].ToLowerInvariant() switch
            {
                "url" => new PlayCommand
                {
                    ZoneIndex = zoneIndex,
                    MediaUrl = parts[1],
                    Source = CommandSource.Mqtt,
                },
                "track" when int.TryParse(parts[1], out var trackIndex) && trackIndex > 0 => new PlayCommand
                {
                    ZoneIndex = zoneIndex,
                    TrackIndex = trackIndex,
                    Source = CommandSource.Mqtt,
                },
                _ => new PlayCommand { ZoneIndex = zoneIndex, Source = CommandSource.Mqtt },
            };
        }

        return new PlayCommand { ZoneIndex = zoneIndex, Source = CommandSource.Mqtt };
    }

    /// <summary>
    /// Tries to parse volume value (0-100).
    /// </summary>
    private static bool TryParseVolume(string payload, out int volume)
    {
        volume = 0;
        return int.TryParse(payload, out volume) && volume >= 0 && volume <= 100;
    }

    /// <summary>
    /// Tries to parse boolean value from various formats.
    /// </summary>
    private static bool TryParseBool(string payload, out bool value)
    {
        value = false;
        return payload.ToLowerInvariant() switch
        {
            "true" or "1" or "on" or "yes" => (value = true) == true,
            "false" or "0" or "off" or "no" => (value = false) == false,
            _ => false,
        };
    }

    /// <summary>
    /// Tries to parse volume step with direction (+5, -10, etc.).
    /// </summary>
    private static bool TryParseVolumeStep(string payload, out int step, out int direction)
    {
        step = 5; // Default step
        direction = 0;

        if (payload.StartsWith('+'))
        {
            direction = 1;
            var stepStr = payload[1..];
            return string.IsNullOrEmpty(stepStr) || (int.TryParse(stepStr, out step) && step > 0 && step <= 50);
        }

        if (payload.StartsWith('-'))
        {
            direction = -1;
            var stepStr = payload[1..];
            return string.IsNullOrEmpty(stepStr) || (int.TryParse(stepStr, out step) && step > 0 && step <= 50);
        }

        return false;
    }

    /// <summary>
    /// Publishes client status updates to MQTT topics.
    /// </summary>
    public async Task<Result> PublishClientStatusAsync<T>(
        string clientIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (!this.IsConnected)
            {
                return Result.Failure("MQTT client is not connected");
            }

            // Parse client ID - now expecting integer format (1, 2, 3, etc.)
            if (!int.TryParse(clientIndex, out var parsedClientIndex))
            {
                this.LogInvalidClientIndexFormat(clientIndex);
                return Result.Success();
            }

            // Convert to 0-based index for configuration array
            var configIndex = parsedClientIndex - 1;
            if (configIndex < 0 || configIndex >= this._clientConfigs.Count)
            {
                this.LogNoMqttConfigurationForClient(parsedClientIndex.ToString());
                return Result.Success();
            }

            var clientConfig = this._clientConfigs[configIndex];

            // Get the appropriate MQTT topic for this event type using simple patterns
            var topic = this.GetClientMqttTopic(eventType, parsedClientIndex);

            if (topic == null)
            {
                this.LogNoMqttTopicMappingForEventType(eventType);
                return Result.Success();
            }

            // Serialize payload
            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

            // Publish to MQTT
            return await this.PublishAsync(topic, jsonPayload, retain: true, cancellationToken);
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishClientStatus(ex, eventType, clientIndex);
            return Result.Failure($"Failed to publish client status: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the MQTT topic for a client status event type using simple patterns that match blueprint.
    /// </summary>
    /// <param name="eventType">The status event type.</param>
    /// <param name="clientIndex">The client index (1-based).</param>
    /// <returns>The MQTT topic string, or null if no mapping exists.</returns>
    private string? GetClientMqttTopic(string eventType, int clientIndex)
    {
        // Use simple topic patterns that match the blueprint
        return eventType.ToUpperInvariant() switch
        {
            "CLIENT_VOLUME_STATUS" => BuildTopic($"client/{clientIndex}/volume"),
            "CLIENT_MUTE_STATUS" => BuildTopic($"client/{clientIndex}/mute"),
            "CLIENT_LATENCY_STATUS" => BuildTopic($"client/{clientIndex}/latency"),
            "CLIENT_CONNECTED" => BuildTopic($"client/{clientIndex}/connected"),
            "CLIENT_ZONE_STATUS" => BuildTopic($"client/{clientIndex}/zone"),
            "CLIENT_STATE" => BuildTopic($"client/{clientIndex}/state"),
            _ => null,
        };
    }

    /// <summary>
    /// Gets the MQTT topic for a zone status event type using simple patterns that match blueprint.
    /// </summary>
    /// <param name="eventType">The status event type.</param>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <returns>The MQTT topic string, or null if no mapping exists.</returns>
    private string? GetZoneMqttTopic(string eventType, int zoneIndex)
    {
        // Use simple topic patterns that match the blueprint
        return eventType.ToUpperInvariant() switch
        {
            "VOLUME_STATUS" => BuildTopic($"zone/{zoneIndex}/volume"),
            "MUTE_STATUS" => BuildTopic($"zone/{zoneIndex}/mute"),
            "PLAYBACK_STATE" => BuildTopic($"zone/{zoneIndex}/playing"),
            "ZONE_STATE" => BuildTopic($"zone/{zoneIndex}/state"),
            _ => null,
        };
    }

    /// <summary>
    /// Publishes zone status updates to MQTT topics.
    /// </summary>
    public async Task<Result> PublishZoneStatusAsync<T>(
        int zoneIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (!this.IsConnected)
            {
                return Result.Failure("MQTT client is not connected");
            }

            // Get zone config (1-based indexing)
            var zoneConfigIndex = zoneIndex - 1;
            if (zoneConfigIndex < 0 || zoneConfigIndex >= this._zoneConfigs.Count)
            {
                this.LogNoMqttConfigurationForZone(zoneIndex);
                return Result.Success();
            }

            // Get the appropriate MQTT topic for this event type from blueprint
            var topic = this.GetZoneMqttTopic(eventType, zoneIndex);

            if (topic == null)
            {
                this.LogNoMqttTopicMappingForEventType(eventType);
                return Result.Success();
            }

            // Serialize payload
            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

            // Publish to MQTT
            return await this.PublishAsync(topic, jsonPayload, retain: true, cancellationToken);
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishZoneStatus(ex, eventType, zoneIndex);
            return Result.Failure($"Failed to publish zone status: {ex.Message}");
        }
    }

    /// <summary>
    /// Publishes global system status updates to MQTT topics.
    /// </summary>
    public async Task<Result> PublishGlobalStatusAsync<T>(
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (!this.IsConnected)
            {
                return Result.Failure("MQTT client is not connected");
            }

            // Use simple topic patterns that match the blueprint
            var topic = eventType.ToUpperInvariant() switch
            {
                "SYSTEM_STATUS" => BuildTopic("system/status"),
                "VERSION_INFO" => BuildTopic("system/version"),
                "SERVER_STATS" => BuildTopic("system/stats"),
                _ => null,
            };

            if (topic == null)
            {
                this.LogNoMqttTopicMappingForGlobalEventType(eventType);
                return Result.Success();
            }

            // Serialize payload
            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

            // Publish to MQTT with retain flag for status topics
            var retain = !eventType.Equals("ERROR_STATUS", StringComparison.InvariantCultureIgnoreCase); // Don't retain error messages
            return await this.PublishAsync(topic, jsonPayload, retain, cancellationToken);
        }
        catch (Exception ex)
        {
            this.LogFailedToPublishGlobalStatus(ex, eventType);
            return Result.Failure($"Failed to publish global status: {ex.Message}");
        }
    }

    #endregion

    #region Disposal

    public async ValueTask DisposeAsync()
    {
        if (this._disposed)
        {
            return;
        }

        try
        {
            if (this._mqttClient != null)
            {
                if (this._mqttClient.IsConnected)
                {
                    await this._mqttClient.DisconnectAsync();
                }

                this._mqttClient.ConnectedAsync -= this.OnConnectedAsync;
                this._mqttClient.DisconnectedAsync -= this.OnDisconnectedAsync;
                this._mqttClient.ApplicationMessageReceivedAsync -= this.OnApplicationMessageReceivedAsync;

                this._mqttClient.Dispose();
            }

            this.LogServiceDisposed();
        }
        catch (Exception ex)
        {
            this.LogOperationFailed("Dispose", ex);
        }
        finally
        {
            this._disposed = true;
        }
    }

    #endregion
}

/// <summary>
/// Notification published when MQTT connection is established.
/// </summary>
public record MqttConnectionEstablishedNotification : INotification;

/// <summary>
/// Notification published when MQTT connection is lost.
/// </summary>
/// <param name="Reason">The reason for connection loss.</param>
public record MqttConnectionLostNotification(string Reason) : INotification;
