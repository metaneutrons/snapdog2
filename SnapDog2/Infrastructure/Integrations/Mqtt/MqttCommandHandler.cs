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
using SnapDog2.Shared.Models;

/// <summary>
/// Handles MQTT commands for blueprint compliance.
/// </summary>
public partial class MqttCommandHandler
{
    private readonly ILogger<MqttCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MqttCommandHandler(ILogger<MqttCommandHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles MQTT-only commands that are excluded from API.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <param name="zoneIndex">The zone index.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Result> HandleMqttOnlyCommandAsync(string commandId, int zoneIndex, object? parameters = null)
    {
        return commandId switch
        {
            "ZONE_NAME" => await HandleZoneName(zoneIndex, parameters),
            _ => Result.Success() // Unknown commands are ignored
        };
    }

    [MqttCommand("ZONE_NAME")]
    private async Task<Result> HandleZoneName(int zoneIndex, object? parameters)
    {
        LogMqttCommand("ZONE_NAME", zoneIndex);
        return await Task.FromResult(Result.Success());
    }

    [LoggerMessage(EventId = 120101, Level = LogLevel.Debug, Message = "MQTT command {CommandId} handled for zone {ZoneIndex}")]
    private partial void LogMqttCommand(string commandId, int zoneIndex);
}

/// <summary>
/// Attribute to mark methods as MQTT command handlers.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class MqttCommandAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MqttCommandAttribute"/> class.
    /// </summary>
    /// <param name="commandId">The command ID this handler supports.</param>
    public MqttCommandAttribute(string commandId)
    {
        CommandId = commandId;
    }

    /// <summary>
    /// Gets the command ID this handler supports.
    /// </summary>
    public string CommandId { get; }
}
