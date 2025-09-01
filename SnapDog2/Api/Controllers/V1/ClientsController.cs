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
namespace SnapDog2.Api.Controllers.V1;

using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Server.Clients.Commands.Config;
using SnapDog2.Server.Clients.Commands.Volume;
using SnapDog2.Server.Clients.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Modern API controller for Snapcast client management.
/// Returns primitive values directly without wrapper objects for maximum simplicity.
/// </summary>
[ApiController]
[Route("api/v1/clients")]
[Authorize]
[Produces("application/json")]
[Tags("Clients")]
public partial class ClientsController(IMediator mediator, ILogger<ClientsController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<ClientsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // ═══════════════════════════════════════════════════════════════════════════════
    // LOGGER MESSAGE DEFINITIONS - High-performance source generators
    // ═══════════════════════════════════════════════════════════════════════════════

    [LoggerMessage(
        EventId = 5100,
        Level = LogLevel.Warning,
        Message = "Failed to get clients: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClients(string errorMessage);

    [LoggerMessage(
        EventId = 5101,
        Level = LogLevel.Warning,
        Message = "Failed to get client {ClientIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClient(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5102,
        Level = LogLevel.Warning,
        Message = "Failed to set client {ClientIndex} volume to {Volume}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetClientVolume(int clientIndex, int volume, string errorMessage);

    [LoggerMessage(
        EventId = 5103,
        Level = LogLevel.Warning,
        Message = "Failed to get client {ClientIndex} volume: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClientVolume(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5104,
        Level = LogLevel.Warning,
        Message = "Failed to get client {ClientIndex} for volume up: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClientForVolumeUp(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5105,
        Level = LogLevel.Warning,
        Message = "Failed to increase client {ClientIndex} volume: {ErrorMessage}"
    )]
    private partial void LogFailedToIncreaseClientVolume(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5106,
        Level = LogLevel.Warning,
        Message = "Failed to get client {ClientIndex} for volume down: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClientForVolumeDown(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5107,
        Level = LogLevel.Warning,
        Message = "Failed to decrease client {ClientIndex} volume: {ErrorMessage}"
    )]
    private partial void LogFailedToDecreaseClientVolume(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5108,
        Level = LogLevel.Warning,
        Message = "Failed to set client {ClientIndex} mute to {Muted}: {ErrorMessage}"
    )]
    private partial void LogFailedToSetClientMute(int clientIndex, bool muted, string errorMessage);

    [LoggerMessage(
        EventId = 5109,
        Level = LogLevel.Warning,
        Message = "Failed to get client {ClientIndex} mute state: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClientMuteState(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5110,
        Level = LogLevel.Warning,
        Message = "Failed to toggle client {ClientIndex} mute: {ErrorMessage}"
    )]
    private partial void LogFailedToToggleClientMute(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5111,
        Level = LogLevel.Warning,
        Message = "Failed to get client {ClientIndex} after mute toggle: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClientAfterMuteToggle(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5112,
        Level = LogLevel.Warning,
        Message = "Failed to set client {ClientIndex} latency to {Latency}ms: {ErrorMessage}"
    )]
    private partial void LogFailedToSetClientLatency(int clientIndex, int latency, string errorMessage);

    [LoggerMessage(
        EventId = 5113,
        Level = LogLevel.Warning,
        Message = "Failed to get client {ClientIndex} latency: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClientLatency(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5114,
        Level = LogLevel.Warning,
        Message = "Failed to assign client {ClientIndex} to zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToAssignClientToZone(int clientIndex, int zoneIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5115,
        Level = LogLevel.Warning,
        Message = "Failed to get client {ClientIndex} zone assignment: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClientZoneAssignment(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5116,
        Level = LogLevel.Information,
        Message = "Setting client {ClientIndex} name to '{Name}'"
    )]
    private partial void LogSettingClientName(int clientIndex, string name);

    [LoggerMessage(
        EventId = 5119,
        Level = LogLevel.Warning,
        Message = "Failed to set client {ClientIndex} name to '{Name}': {ErrorMessage}"
    )]
    private partial void LogFailedToSetClientName(int clientIndex, string name, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════════
    // API ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get all discovered Snapcast clients.
    /// </summary>
    /// <returns>List of all clients</returns>
    [HttpGet("count")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetClientsCount()
    {
        var query = new GetClientsCountQuery();
        var result = await this._mediator.SendQueryAsync<GetClientsCountQuery, Result<int>>(query);

        if (result.IsFailure)
        {
            return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return this.Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType<Page<ClientState>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Page<ClientState>>> GetClients([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        if (page < 1)
        {
            return this.BadRequest("Page must be greater than 0");
        }

        if (size < 1 || size > 100)
        {
            return this.BadRequest("Size must be between 1 and 100");
        }

        var query = new GetAllClientsQuery();
        var result = await this._mediator.SendQueryAsync<GetAllClientsQuery, Result<List<ClientState>>>(query);

        if (result.IsFailure)
        {
            this.LogFailedToGetClients(result.ErrorMessage ?? "Unknown error");
            return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        var clients = result.Value ?? [];
        var totalCount = clients.Count;
        var startIndex = (page - 1) * size;
        var pagedClients = clients.Skip(startIndex).Take(size).ToArray();

        var pageResult = new Page<ClientState>(pagedClients, totalCount, size, page);

        return this.Ok(pageResult);
    }

    /// <summary>
    /// Get detailed information for a specific client.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <returns>Client state information</returns>
    [HttpGet("{clientIndex:int}")]
    [ProducesResponseType<ClientState>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientState>> GetClient(int clientIndex)
    {
        var query = new GetClientQuery { ClientIndex = clientIndex };
        var result = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            this.LogFailedToGetClient(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return this.Ok(result.Value!);
    }

    /// <summary>
    /// Set client volume level.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="volume">Volume level (0-100)</param>
    /// <returns>New volume level</returns>
    [HttpPut("{clientIndex:int}/volume")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> SetVolume(int clientIndex, [FromBody] int volume)
    {
        if (volume < 0 || volume > 100)
        {
            return this.BadRequest("Volume must be between 0 and 100");
        }

        var command = new SetClientVolumeCommand { ClientIndex = clientIndex, Volume = volume };
        var result = await this._mediator.SendCommandAsync<SetClientVolumeCommand, Result>(command);

        if (result.IsFailure)
        {
            this.LogFailedToSetClientVolume(clientIndex, volume, result.ErrorMessage ?? "Unknown error");
            return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The volume change will be applied asynchronously and published via MQTT/KNX
        return this.Accepted(volume);
    }

    /// <summary>
    /// Get current client volume level.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <returns>Current volume level (0-100)</returns>
    [HttpGet("{clientIndex:int}/volume")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> GetVolume(int clientIndex)
    {
        var query = new GetClientQuery { ClientIndex = clientIndex };
        var result = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            this.LogFailedToGetClientVolume(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return this.Ok(result.Value!.Volume);
    }

    /// <summary>
    /// Increase client volume by specified step.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="step">Volume increase step (default: 5)</param>
    /// <returns>New volume level</returns>
    [HttpPost("{clientIndex:int}/volume/up")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> VolumeUp(int clientIndex, [FromQuery] int step = 5)
    {
        if (step < 1 || step > 50)
        {
            return this.BadRequest("Step must be between 1 and 50");
        }

        var command = new ClientVolumeUpCommand { ClientIndex = clientIndex, Step = step };
        var result = await this._mediator.SendCommandAsync<ClientVolumeUpCommand, Result>(command);

        if (result.IsFailure)
        {
            this.LogFailedToIncreaseClientVolume(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The volume change will be applied asynchronously and published via MQTT/KNX
        return this.Accepted(step);
    }

    /// <summary>
    /// Decrease client volume by specified step.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="step">Volume decrease step (default: 5)</param>
    /// <returns>New volume level</returns>
    [HttpPost("{clientIndex:int}/volume/down")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> VolumeDown(int clientIndex, [FromQuery] int step = 5)
    {
        if (step < 1 || step > 50)
        {
            return this.BadRequest("Step must be between 1 and 50");
        }

        var command = new ClientVolumeDownCommand { ClientIndex = clientIndex, Step = step };
        var result = await this._mediator.SendCommandAsync<ClientVolumeDownCommand, Result>(command);

        if (result.IsFailure)
        {
            this.LogFailedToDecreaseClientVolume(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The volume change will be applied asynchronously and published via MQTT/KNX
        return this.Accepted(step);
    }

    /// <summary>
    /// Set client mute state.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="muted">Mute state (true = muted, false = unmuted)</param>
    /// <returns>New mute state</returns>
    [HttpPut("{clientIndex:int}/mute")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> SetMute(int clientIndex, [FromBody] bool muted)
    {
        var command = new SetClientMuteCommand { ClientIndex = clientIndex, Enabled = muted };
        var result = await this._mediator.SendCommandAsync<SetClientMuteCommand, Result>(command);

        if (result.IsFailure)
        {
            this.LogFailedToSetClientMute(clientIndex, muted, result.ErrorMessage ?? "Unknown error");
            return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(muted);
    }

    /// <summary>
    /// Get current client mute state.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <returns>Current mute state</returns>
    [HttpGet("{clientIndex:int}/mute")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> GetMute(int clientIndex)
    {
        var query = new GetClientQuery { ClientIndex = clientIndex };
        var result = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            this.LogFailedToGetClientMuteState(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return this.Ok(result.Value!.Mute);
    }

    /// <summary>
    /// Toggle client mute state.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <returns>New mute state</returns>
    [HttpPost("{clientIndex:int}/mute/toggle")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> ToggleMute(int clientIndex)
    {
        var command = new ToggleClientMuteCommand { ClientIndex = clientIndex };
        var result = await this._mediator.SendCommandAsync<ToggleClientMuteCommand, Result>(command);

        if (result.IsFailure)
        {
            this.LogFailedToToggleClientMute(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The mute toggle will be applied asynchronously and published via MQTT/KNX
        // Client should query current state or listen to MQTT/KNX for the actual result
        return this.Accepted();
    }

    /// <summary>
    /// Set client audio latency compensation.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="latency">Latency in milliseconds</param>
    /// <returns>New latency value</returns>
    [HttpPut("{clientIndex:int}/latency")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> SetLatency(int clientIndex, [FromBody] int latency)
    {
        if (latency < -2000 || latency > 2000)
        {
            return this.BadRequest("Latency must be between -2000 and 2000 milliseconds");
        }

        var command = new SetClientLatencyCommand { ClientIndex = clientIndex, LatencyMs = latency };
        var result = await this._mediator.SendCommandAsync<SetClientLatencyCommand, Result>(command);

        if (result.IsFailure)
        {
            this.LogFailedToSetClientLatency(clientIndex, latency, result.ErrorMessage ?? "Unknown error");
            return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(latency);
    }

    /// <summary>
    /// Get current client audio latency compensation.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <returns>Current latency in milliseconds</returns>
    [HttpGet("{clientIndex:int}/latency")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> GetLatency(int clientIndex)
    {
        var query = new GetClientQuery { ClientIndex = clientIndex };
        var result = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            this.LogFailedToGetClientLatency(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return this.Ok(result.Value!.LatencyMs);
    }

    /// <summary>
    /// Assign client to a specific zone.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="zoneIndex">Zone ID (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPut("{clientIndex:int}/zone")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignToZone(int clientIndex, [FromBody] int zoneIndex)
    {
        if (zoneIndex < 1)
        {
            return this.BadRequest("Zone ID must be greater than 0");
        }

        var command = new AssignClientToZoneCommand { ClientIndex = clientIndex, ZoneIndex = zoneIndex };
        var result = await this._mediator.SendCommandAsync<AssignClientToZoneCommand, Result>(command);

        if (result.IsFailure)
        {
            this.LogFailedToAssignClientToZone(clientIndex, zoneIndex, result.ErrorMessage ?? "Unknown error");
            return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        // The zone assignment will be applied asynchronously and published via MQTT/KNX
        return this.Accepted(zoneIndex);
    }

    /// <summary>
    /// Get the zone assignment for a client.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <returns>Zone ID if assigned, null if unassigned</returns>
    [HttpGet("{clientIndex:int}/zone")]
    [ProducesResponseType<int?>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int?>> GetZoneAssignment(int clientIndex)
    {
        var query = new GetClientQuery { ClientIndex = clientIndex };
        var result = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            this.LogFailedToGetClientZoneAssignment(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return this.Ok(result.Value!.ZoneIndex);
    }

    /// <summary>
    /// Set client display name.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="name">New client name</param>
    /// <returns>Updated client name</returns>
    [HttpPut("{clientIndex:int}/name")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> SetName(int clientIndex, [FromBody] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return this.BadRequest("Name cannot be empty");
        }

        if (name.Length > 100)
        {
            return this.BadRequest("Name cannot exceed 100 characters");
        }

        var command = new SetClientNameCommand { ClientIndex = clientIndex, Name = name.Trim() };
        var result = await this._mediator.SendCommandAsync<SetClientNameCommand, Result>(command);

        if (result.IsFailure)
        {
            this.LogFailedToSetClientName(clientIndex, name, result.ErrorMessage ?? "Unknown error");
            return this.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // ✅ Command-Status Flow: Return 202 Accepted for asynchronous operation
        return this.Accepted(name.Trim());
    }

    /// <summary>
    /// Get client connection status.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <returns>Client connection status</returns>
    [HttpGet("{clientIndex:int}/connected")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> GetClientConnected(int clientIndex)
    {
        var query = new GetClientQuery { ClientIndex = clientIndex };
        var result = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            this.LogFailedToGetClientConnectionStatus(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return this.Ok(result.Value!.Connected);
    }

    /// <summary>
    /// Get client name.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <returns>Client name</returns>
    [HttpGet("{clientIndex:int}/name")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetClientName(int clientIndex)
    {
        var query = new GetClientQuery { ClientIndex = clientIndex };
        var result = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            this.LogFailedToGetClientName(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        var s = result.Value!.Name;

        return this.Ok(s);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // LOGGING METHODS FOR NEW ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [LoggerMessage(
        EventId = 5118,
        Level = LogLevel.Warning,
        Message = "Failed to get client {ClientIndex} connection status: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClientConnectionStatus(int clientIndex, string errorMessage);

    [LoggerMessage(
        EventId = 5119,
        Level = LogLevel.Warning,
        Message = "Failed to get client {ClientIndex} name: {ErrorMessage}"
    )]
    private partial void LogFailedToGetClientName(int clientIndex, string errorMessage);
}
