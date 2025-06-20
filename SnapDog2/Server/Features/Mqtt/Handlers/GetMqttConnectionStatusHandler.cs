using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Services;
using SnapDog2.Server.Features.Mqtt.Queries;

namespace SnapDog2.Server.Features.Mqtt.Handlers;

/// <summary>
/// Handler for getting MQTT connection status information.
/// </summary>
public class GetMqttConnectionStatusHandler
    : IRequestHandler<GetMqttConnectionStatusQuery, MqttConnectionStatusResponse>
{
    private readonly IMqttService _mqttService;
    private readonly MqttConfiguration _config;
    private readonly ILogger<GetMqttConnectionStatusHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMqttConnectionStatusHandler"/> class.
    /// </summary>
    /// <param name="mqttService">The MQTT service.</param>
    /// <param name="config">The MQTT configuration.</param>
    /// <param name="logger">The logger.</param>
    public GetMqttConnectionStatusHandler(
        IMqttService mqttService,
        IOptions<MqttConfiguration> config,
        ILogger<GetMqttConnectionStatusHandler> logger
    )
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the get MQTT connection status query.
    /// </summary>
    /// <param name="request">The connection status query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The MQTT connection status response.</returns>
    public async Task<MqttConnectionStatusResponse> Handle(
        GetMqttConnectionStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug("Getting MQTT connection status");

        try
        {
            // For now, we'll create a basic status response
            // In a more advanced implementation, we could track connection state, statistics, etc.
            var response = new MqttConnectionStatusResponse
            {
                BrokerHost = _config.Broker,
                BrokerPort = _config.Port,
                ClientId = _config.ClientId,
                IsConnected = false, // We'll try to determine this
                LastConnectionAttempt = DateTime.UtcNow,
            };

            // Try to connect to check status
            try
            {
                response.IsConnected = await _mqttService.ConnectAsync(cancellationToken);
                if (response.IsConnected)
                {
                    response.ConnectedAt = DateTime.UtcNow;
                    _logger.LogDebug("MQTT connection status check: Connected");
                }
                else
                {
                    response.LastError = "Failed to connect to MQTT broker";
                    _logger.LogDebug("MQTT connection status check: Not connected");
                }
            }
            catch (Exception ex)
            {
                response.IsConnected = false;
                response.LastError = ex.Message;
                _logger.LogWarning(ex, "Error checking MQTT connection status");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting MQTT connection status");

            // Return a basic error response
            return new MqttConnectionStatusResponse
            {
                BrokerHost = _config.Broker,
                BrokerPort = _config.Port,
                ClientId = _config.ClientId,
                IsConnected = false,
                LastError = ex.Message,
                LastConnectionAttempt = DateTime.UtcNow,
            };
        }
    }
}
