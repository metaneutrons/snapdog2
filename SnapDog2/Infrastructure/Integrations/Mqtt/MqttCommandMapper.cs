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

using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// Modern MQTT command mapper using direct service calls.
/// Eliminates CommandFactory and provides direct service integration.
/// </summary>
public partial class MqttCommandMapper(
    ILogger<MqttCommandMapper> logger,
    MqttConfig mqttConfig,
    IZoneService zoneService,
    IClientService clientService)
{
    private readonly ILogger<MqttCommandMapper> _logger = logger;
    private readonly MqttConfig _mqttConfig = mqttConfig;
    private readonly IZoneService _zoneService = zoneService;
    private readonly IClientService _clientService = clientService;

    [LoggerMessage(EventId = 115400, Level = LogLevel.Debug, Message = "Mapping MQTT command: {Topic} → {Payload}")]
    private partial void LogMappingCommand(string topic, string payload);

    [LoggerMessage(EventId = 115401, Level = LogLevel.Warning, Message = "Failed → map MQTT topic: {Topic}")]
    private partial void LogMappingFailed(string topic);

    [LoggerMessage(EventId = 115402, Level = LogLevel.Error, Message = "Error mapping MQTT command for topic {Topic}: {Error}")]
    private partial void LogMappingError(string topic, string error);

    /// <summary>
    /// Maps MQTT topics to direct service calls using the new architecture.
    /// </summary>
    public async Task<Result> ExecuteCommandFromTopicAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            LogMappingCommand(topic, payload);

            // Use the strategy to execute command directly
            var result = await MqttCommandMappingStrategy.ExecuteFromMqttTopicAsync(
                topic, payload, _zoneService, _clientService, cancellationToken);

            if (result.IsFailure)
            {
                LogMappingFailed(topic);
            }

            return result;
        }
        catch (Exception ex)
        {
            LogMappingError(topic, ex.Message);
            return Result.Failure($"Error executing MQTT command: {ex.Message}");
        }
    }
}
