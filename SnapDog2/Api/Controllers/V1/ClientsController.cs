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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Constants;
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
public partial class ClientsController(IClientService clientService, ILogger<ClientsController> logger) : ControllerBase
{
    private readonly IClientService _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
    private readonly ILogger<ClientsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // ═══════════════════════════════════════════════════════════════════════════════
    // LOGGER MESSAGE DEFINITIONS - High-performance source generators
    // ═══════════════════════════════════════════════════════════════════════════════

    [LoggerMessage(EventId = 113100, Level = LogLevel.Warning, Message = "Failed → get clients: {ErrorMessage}")]
    private partial void LogFailedToGetClients(string errorMessage);

    [LoggerMessage(EventId = 113101, Level = LogLevel.Warning, Message = "Failed → get client {ClientIndex}: {ErrorMessage}")]
    private partial void LogFailedToGetClient(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113102, Level = LogLevel.Warning, Message = "Failed → set client {ClientIndex} volume → {Volume}: {ErrorMessage}")]
    private partial void LogFailedToSetClientVolume(int clientIndex, int volume, string errorMessage);

    [LoggerMessage(EventId = 113103, Level = LogLevel.Warning, Message = "Failed → get client {ClientIndex} volume: {ErrorMessage}")]
    private partial void LogFailedToGetClientVolume(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113104, Level = LogLevel.Warning, Message = "Failed → get client {ClientIndex} for volume up: {ErrorMessage}")]
    private partial void LogFailedToGetClientForVolumeUp(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113105, Level = LogLevel.Warning, Message = "Failed → increase client {ClientIndex} volume: {ErrorMessage}")]
    private partial void LogFailedToIncreaseClientVolume(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113106, Level = LogLevel.Warning, Message = "Failed → get client {ClientIndex} for volume down: {ErrorMessage}")]
    private partial void LogFailedToGetClientForVolumeDown(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113107, Level = LogLevel.Warning, Message = "Failed → decrease client {ClientIndex} volume: {ErrorMessage}")]
    private partial void LogFailedToDecreaseClientVolume(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113108, Level = LogLevel.Warning, Message = "Failed → set client {ClientIndex} mute → {Muted}: {ErrorMessage}")]
    private partial void LogFailedToSetClientMute(int clientIndex, bool muted, string errorMessage);

    [LoggerMessage(EventId = 113109, Level = LogLevel.Warning, Message = "Failed → get client {ClientIndex} mute state: {ErrorMessage}")]
    private partial void LogFailedToGetClientMuteState(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113110, Level = LogLevel.Warning, Message = "Failed → toggle client {ClientIndex} mute: {ErrorMessage}")]
    private partial void LogFailedToToggleClientMute(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113111, Level = LogLevel.Warning, Message = "Failed → get client {ClientIndex} after mute toggle: {ErrorMessage}")]
    private partial void LogFailedToGetClientAfterMuteToggle(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113112, Level = LogLevel.Warning, Message = "Failed → set client {ClientIndex} latency → {Latency}ms: {ErrorMessage}")]
    private partial void LogFailedToSetClientLatency(int clientIndex, int latency, string errorMessage);

    [LoggerMessage(EventId = 113113, Level = LogLevel.Warning, Message = "Failed → get client {ClientIndex} latency: {ErrorMessage}")]
    private partial void LogFailedToGetClientLatency(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113114, Level = LogLevel.Warning, Message = "Failed → assign client {ClientIndex} → zone {ZoneIndex}: {ErrorMessage}")]
    private partial void LogFailedToAssignClientToZone(int clientIndex, int zoneIndex, string errorMessage);

    [LoggerMessage(EventId = 113115, Level = LogLevel.Warning, Message = "Failed → get client {ClientIndex} zone assignment: {ErrorMessage}")]
    private partial void LogFailedToGetClientZoneAssignment(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113116, Level = LogLevel.Information, Message = "Setting client {ClientIndex} name → '{Name}'")]
    private partial void LogSettingClientName(int clientIndex, string name);

    [LoggerMessage(EventId = 113117, Level = LogLevel.Warning, Message = "Failed → set client {ClientIndex} name → '{Name}': {ErrorMessage}")]
    private partial void LogFailedToSetClientName(int clientIndex, string name, string errorMessage);

    [LoggerMessage(EventId = 113118, Level = LogLevel.Warning, Message = "Failed → get client {ClientIndex} connection status: {ErrorMessage}")]
    private partial void LogFailedToGetClientConnectionStatus(int clientIndex, string errorMessage);

    [LoggerMessage(EventId = 113119, Level = LogLevel.Warning, Message = "Failed → get client {ClientIndex} name: {ErrorMessage}")]
    private partial void LogFailedToGetClientName(int clientIndex, string errorMessage);

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
        var result = await _clientService.GetClientsCountAsync();

        if (result.IsFailure)
        {
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType<Page<ClientState>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Page<ClientState>>> GetClients([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        if (page < 1)
        {
            return BadRequest("Page must be greater than 0");
        }

        if (size < 1 || size > 100)
        {
            return BadRequest("Size must be between 1 and 100");
        }

        var result = await _clientService.GetAllClientsAsync();

        if (result.IsFailure)
        {
            LogFailedToGetClients(result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        var clients = result.Value ?? [];
        var totalCount = clients.Count;
        var startIndex = (page - 1) * size;
        var pagedClients = clients.Skip(startIndex).Take(size).ToArray();

        var pageResult = new Page<ClientState>(pagedClients, totalCount, size, page);

        return Ok(pageResult);
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
        var result = await _clientService.GetClientAsync(clientIndex);

        if (result.IsFailure)
        {
            LogFailedToGetClient(clientIndex, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!);
    }

    /// <summary>
    /// Set client volume level.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="volume">Volume level (0-100)</param>
    /// <returns>New volume level</returns>
    [HttpPut("{clientIndex:int}/volume")]
    [CommandId(CommandIds.ClientVolume)]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> SetVolume(int clientIndex, [FromBody] int volume)
    {
        if (volume < 0 || volume > 100)
        {
            return BadRequest("Volume must be between 0 and 100");
        }

        var result = await _clientService.SetVolumeAsync(clientIndex, volume);

        if (result.IsFailure)
        {
            LogFailedToSetClientVolume(clientIndex, volume, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Accepted(volume);
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
        var result = await _clientService.GetClientAsync(clientIndex);

        if (result.IsFailure)
        {
            LogFailedToGetClientVolume(clientIndex, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.Volume);
    }

    /// <summary>
    /// Increase client volume by specified step.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="step">Volume increase step (default: 5)</param>
    /// <returns>New volume level</returns>
    [HttpPost("{clientIndex:int}/volume/up")]
    [CommandId(CommandIds.ClientVolumeUp)]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> VolumeUp(int clientIndex, [FromQuery] int step = 5)
    {
        if (step < 1 || step > 50)
        {
            return BadRequest("Step must be between 1 and 50");
        }

        var result = await _clientService.VolumeUpAsync(clientIndex, step);

        if (result.IsFailure)
        {
            LogFailedToIncreaseClientVolume(clientIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Accepted(step);
    }

    /// <summary>
    /// Decrease client volume by specified step.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="step">Volume decrease step (default: 5)</param>
    /// <returns>New volume level</returns>
    [HttpPost("{clientIndex:int}/volume/down")]
    [CommandId(CommandIds.ClientVolumeDown)]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> VolumeDown(int clientIndex, [FromQuery] int step = 5)
    {
        if (step < 1 || step > 50)
        {
            return BadRequest("Step must be between 1 and 50");
        }

        var result = await _clientService.VolumeDownAsync(clientIndex, step);

        if (result.IsFailure)
        {
            LogFailedToDecreaseClientVolume(clientIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Accepted(step);
    }

    /// <summary>
    /// Set client mute state.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="muted">Mute state (true = muted, false = unmuted)</param>
    /// <returns>New mute state</returns>
    [HttpPut("{clientIndex:int}/mute")]
    [CommandId(CommandIds.ClientMute)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> SetMute(int clientIndex, [FromBody] bool muted)
    {
        var result = await _clientService.SetMuteAsync(clientIndex, muted);

        if (result.IsFailure)
        {
            LogFailedToSetClientMute(clientIndex, muted, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Accepted(muted);
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
        var result = await _clientService.GetClientAsync(clientIndex);

        if (result.IsFailure)
        {
            LogFailedToGetClientMuteState(clientIndex, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.Mute);
    }

    /// <summary>
    /// Toggle client mute state.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <returns>New mute state</returns>
    [HttpPost("{clientIndex:int}/mute/toggle")]
    [CommandId(CommandIds.ClientMuteToggle)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> ToggleMute(int clientIndex)
    {
        var result = await _clientService.ToggleMuteAsync(clientIndex);

        if (result.IsFailure)
        {
            LogFailedToToggleClientMute(clientIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Accepted();
    }

    /// <summary>
    /// Set client audio latency compensation.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="latency">Latency in milliseconds</param>
    /// <returns>New latency value</returns>
    [HttpPut("{clientIndex:int}/latency")]
    [CommandId(CommandIds.ClientLatency)]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> SetLatency(int clientIndex, [FromBody] int latency)
    {
        if (latency < -2000 || latency > 2000)
        {
            return BadRequest("Latency must be between -2000 and 2000 milliseconds");
        }

        var result = await _clientService.SetLatencyAsync(clientIndex, latency);

        if (result.IsFailure)
        {
            LogFailedToSetClientLatency(clientIndex, latency, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Accepted(latency);
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
        var result = await _clientService.GetClientAsync(clientIndex);

        if (result.IsFailure)
        {
            LogFailedToGetClientLatency(clientIndex, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.LatencyMs);
    }

    /// <summary>
    /// Assign client to a specific zone.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="zoneIndex">Zone ID (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPut("{clientIndex:int}/zone")]
    [CommandId(CommandIds.ClientZone)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignToZone(int clientIndex, [FromBody] int zoneIndex)
    {
        if (zoneIndex < 1)
        {
            return BadRequest("Zone ID must be greater than 0");
        }

        var result = await _clientService.AssignToZoneAsync(clientIndex, zoneIndex);

        if (result.IsFailure)
        {
            LogFailedToAssignClientToZone(clientIndex, zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Accepted(zoneIndex);
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
        var result = await _clientService.GetClientAsync(clientIndex);

        if (result.IsFailure)
        {
            LogFailedToGetClientZoneAssignment(clientIndex, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.ZoneIndex);
    }

    /// <summary>
    /// Set client display name.
    /// </summary>
    /// <param name="clientIndex">Client Index</param>
    /// <param name="name">New client name</param>
    /// <returns>Updated client name</returns>
    [HttpPut("{clientIndex:int}/name")]
    [CommandId(CommandIds.ClientName)]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> SetName(int clientIndex, [FromBody] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name cannot be empty");
        }

        if (name.Length > 100)
        {
            return BadRequest("Name cannot exceed 100 characters");
        }

        LogSettingClientName(clientIndex, name.Trim());
        var result = await _clientService.SetNameAsync(clientIndex, name.Trim());

        if (result.IsFailure)
        {
            LogFailedToSetClientName(clientIndex, name, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Accepted(name.Trim());
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
        var result = await _clientService.GetClientAsync(clientIndex);

        if (result.IsFailure)
        {
            LogFailedToGetClientConnectionStatus(clientIndex, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.Connected);
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
        var result = await _clientService.GetClientAsync(clientIndex);

        if (result.IsFailure)
        {
            LogFailedToGetClientName(clientIndex, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.Name);
    }
}
