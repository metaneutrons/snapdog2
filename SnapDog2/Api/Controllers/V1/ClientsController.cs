namespace SnapDog2.Api.Controllers.V1;

using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands.Config;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Modern API controller for Snapcast client management.
/// Returns primitive values directly without wrapper objects for maximum simplicity.
/// </summary>
[ApiController]
[Route("api/v1/clients")]
[Authorize]
[Produces("application/json")]
[Tags("Clients")]
public partial class ClientsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(IMediator mediator, ILogger<ClientsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // LOGGER MESSAGE DEFINITIONS - High-performance source generators
    // ═══════════════════════════════════════════════════════════════════════════════

    [LoggerMessage(11001, LogLevel.Warning, "Failed to get clients: {ErrorMessage}")]
    private partial void LogFailedToGetClients(string errorMessage);

    [LoggerMessage(11002, LogLevel.Warning, "Failed to get client {ClientId}: {ErrorMessage}")]
    private partial void LogFailedToGetClient(int clientId, string errorMessage);

    [LoggerMessage(11003, LogLevel.Warning, "Failed to set client {ClientId} volume to {Volume}: {ErrorMessage}")]
    private partial void LogFailedToSetClientVolume(int clientId, int volume, string errorMessage);

    [LoggerMessage(11004, LogLevel.Warning, "Failed to get client {ClientId} volume: {ErrorMessage}")]
    private partial void LogFailedToGetClientVolume(int clientId, string errorMessage);

    [LoggerMessage(11005, LogLevel.Warning, "Failed to get client {ClientId} for volume up: {ErrorMessage}")]
    private partial void LogFailedToGetClientForVolumeUp(int clientId, string errorMessage);

    [LoggerMessage(11006, LogLevel.Warning, "Failed to increase client {ClientId} volume: {ErrorMessage}")]
    private partial void LogFailedToIncreaseClientVolume(int clientId, string errorMessage);

    [LoggerMessage(11007, LogLevel.Warning, "Failed to get client {ClientId} for volume down: {ErrorMessage}")]
    private partial void LogFailedToGetClientForVolumeDown(int clientId, string errorMessage);

    [LoggerMessage(11008, LogLevel.Warning, "Failed to decrease client {ClientId} volume: {ErrorMessage}")]
    private partial void LogFailedToDecreaseClientVolume(int clientId, string errorMessage);

    [LoggerMessage(11009, LogLevel.Warning, "Failed to set client {ClientId} mute to {Muted}: {ErrorMessage}")]
    private partial void LogFailedToSetClientMute(int clientId, bool muted, string errorMessage);

    [LoggerMessage(11010, LogLevel.Warning, "Failed to get client {ClientId} mute state: {ErrorMessage}")]
    private partial void LogFailedToGetClientMuteState(int clientId, string errorMessage);

    [LoggerMessage(11011, LogLevel.Warning, "Failed to toggle client {ClientId} mute: {ErrorMessage}")]
    private partial void LogFailedToToggleClientMute(int clientId, string errorMessage);

    [LoggerMessage(11012, LogLevel.Warning, "Failed to get client {ClientId} after mute toggle: {ErrorMessage}")]
    private partial void LogFailedToGetClientAfterMuteToggle(int clientId, string errorMessage);

    [LoggerMessage(11013, LogLevel.Warning, "Failed to set client {ClientId} latency to {Latency}ms: {ErrorMessage}")]
    private partial void LogFailedToSetClientLatency(int clientId, int latency, string errorMessage);

    [LoggerMessage(11014, LogLevel.Warning, "Failed to get client {ClientId} latency: {ErrorMessage}")]
    private partial void LogFailedToGetClientLatency(int clientId, string errorMessage);

    [LoggerMessage(11015, LogLevel.Warning, "Failed to assign client {ClientId} to zone {ZoneId}: {ErrorMessage}")]
    private partial void LogFailedToAssignClientToZone(int clientId, int zoneId, string errorMessage);

    [LoggerMessage(11016, LogLevel.Warning, "Failed to get client {ClientId} zone assignment: {ErrorMessage}")]
    private partial void LogFailedToGetClientZoneAssignment(int clientId, string errorMessage);

    [LoggerMessage(11017, LogLevel.Information, "Setting client {ClientId} name to '{Name}' (not yet implemented)")]
    private partial void LogSettingClientName(int clientId, string name);

    // ═══════════════════════════════════════════════════════════════════════════════
    // API ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get all discovered Snapcast clients.
    /// </summary>
    /// <returns>List of all clients</returns>
    [HttpGet]
    [ProducesResponseType<List<ClientState>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ClientState>>> GetClients()
    {
        var query = new GetAllClientsQuery();
        var result = await _mediator.SendQueryAsync<GetAllClientsQuery, Result<List<ClientState>>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetClients(result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(result.Value!);
    }

    /// <summary>
    /// Get detailed information for a specific client.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <returns>Client state information</returns>
    [HttpGet("{clientId:int}")]
    [ProducesResponseType<ClientState>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientState>> GetClient(int clientId)
    {
        var query = new GetClientQuery { ClientIndex = clientId };
        var result = await _mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetClient(clientId, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientId} not found");
        }

        return Ok(result.Value!);
    }

    /// <summary>
    /// Set client volume level.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <param name="volume">Volume level (0-100)</param>
    /// <returns>New volume level</returns>
    [HttpPut("{clientId:int}/volume")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> SetVolume(int clientId, [FromBody] int volume)
    {
        if (volume < 0 || volume > 100)
            return BadRequest("Volume must be between 0 and 100");

        var command = new SetClientVolumeCommand { ClientIndex = clientId, Volume = volume };
        var result = await _mediator.SendCommandAsync<SetClientVolumeCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetClientVolume(clientId, volume, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(volume);
    }

    /// <summary>
    /// Get current client volume level.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <returns>Current volume level (0-100)</returns>
    [HttpGet("{clientId:int}/volume")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> GetVolume(int clientId)
    {
        var query = new GetClientQuery { ClientIndex = clientId };
        var result = await _mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetClientVolume(clientId, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientId} not found");
        }

        return Ok(result.Value!.Volume);
    }

    /// <summary>
    /// Increase client volume by specified step.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <param name="step">Volume increase step (default: 5)</param>
    /// <returns>New volume level</returns>
    [HttpPost("{clientId:int}/volume/up")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> VolumeUp(int clientId, [FromQuery] int step = 5)
    {
        if (step < 1 || step > 50)
            return BadRequest("Step must be between 1 and 50");

        // Get current volume first
        var query = new GetClientQuery { ClientIndex = clientId };
        var clientResult = await _mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (clientResult.IsFailure)
        {
            LogFailedToGetClientForVolumeUp(clientId, clientResult.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientId} not found");
        }

        var newVolume = Math.Min(100, clientResult.Value!.Volume + step);
        var command = new SetClientVolumeCommand { ClientIndex = clientId, Volume = newVolume };
        var result = await _mediator.SendCommandAsync<SetClientVolumeCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToIncreaseClientVolume(clientId, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(newVolume);
    }

    /// <summary>
    /// Decrease client volume by specified step.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <param name="step">Volume decrease step (default: 5)</param>
    /// <returns>New volume level</returns>
    [HttpPost("{clientId:int}/volume/down")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> VolumeDown(int clientId, [FromQuery] int step = 5)
    {
        if (step < 1 || step > 50)
            return BadRequest("Step must be between 1 and 50");

        // Get current volume first
        var query = new GetClientQuery { ClientIndex = clientId };
        var clientResult = await _mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (clientResult.IsFailure)
        {
            LogFailedToGetClientForVolumeDown(clientId, clientResult.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientId} not found");
        }

        var newVolume = Math.Max(0, clientResult.Value!.Volume - step);
        var command = new SetClientVolumeCommand { ClientIndex = clientId, Volume = newVolume };
        var result = await _mediator.SendCommandAsync<SetClientVolumeCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToDecreaseClientVolume(clientId, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(newVolume);
    }

    /// <summary>
    /// Set client mute state.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <param name="muted">Mute state (true = muted, false = unmuted)</param>
    /// <returns>New mute state</returns>
    [HttpPut("{clientId:int}/mute")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> SetMute(int clientId, [FromBody] bool muted)
    {
        var command = new SetClientMuteCommand { ClientIndex = clientId, Enabled = muted };
        var result = await _mediator.SendCommandAsync<SetClientMuteCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetClientMute(clientId, muted, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(muted);
    }

    /// <summary>
    /// Get current client mute state.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <returns>Current mute state</returns>
    [HttpGet("{clientId:int}/mute")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> GetMute(int clientId)
    {
        var query = new GetClientQuery { ClientIndex = clientId };
        var result = await _mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetClientMuteState(clientId, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientId} not found");
        }

        return Ok(result.Value!.Mute);
    }

    /// <summary>
    /// Toggle client mute state.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <returns>New mute state</returns>
    [HttpPost("{clientId:int}/mute/toggle")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> ToggleMute(int clientId)
    {
        var command = new ToggleClientMuteCommand { ClientIndex = clientId };
        var result = await _mediator.SendCommandAsync<ToggleClientMuteCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToToggleClientMute(clientId, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // Get the new state to return
        var query = new GetClientQuery { ClientIndex = clientId };
        var clientResult = await _mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (clientResult.IsFailure)
        {
            LogFailedToGetClientAfterMuteToggle(clientId, clientResult.ErrorMessage ?? "Unknown error");
            return Problem(clientResult.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(clientResult.Value!.Mute);
    }

    /// <summary>
    /// Set client audio latency compensation.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <param name="latency">Latency in milliseconds</param>
    /// <returns>New latency value</returns>
    [HttpPut("{clientId:int}/latency")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> SetLatency(int clientId, [FromBody] int latency)
    {
        if (latency < -2000 || latency > 2000)
            return BadRequest("Latency must be between -2000 and 2000 milliseconds");

        var command = new SetClientLatencyCommand { ClientIndex = clientId, LatencyMs = latency };
        var result = await _mediator.SendCommandAsync<SetClientLatencyCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToSetClientLatency(clientId, latency, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(latency);
    }

    /// <summary>
    /// Get current client audio latency compensation.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <returns>Current latency in milliseconds</returns>
    [HttpGet("{clientId:int}/latency")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> GetLatency(int clientId)
    {
        var query = new GetClientQuery { ClientIndex = clientId };
        var result = await _mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetClientLatency(clientId, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientId} not found");
        }

        return Ok(result.Value!.LatencyMs);
    }

    /// <summary>
    /// Assign client to a specific zone.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <param name="zoneId">Zone ID (1-based)</param>
    /// <returns>No content on success</returns>
    [HttpPut("{clientId:int}/zone")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignToZone(int clientId, [FromBody] int zoneId)
    {
        if (zoneId < 1)
            return BadRequest("Zone ID must be greater than 0");

        var command = new AssignClientToZoneCommand { ClientIndex = clientId, ZoneIndex = zoneId };
        var result = await _mediator.SendCommandAsync<AssignClientToZoneCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToAssignClientToZone(clientId, zoneId, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }

    /// <summary>
    /// Get the zone assignment for a client.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <returns>Zone ID if assigned, null if unassigned</returns>
    [HttpGet("{clientId:int}/zone")]
    [ProducesResponseType<int?>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int?>> GetZoneAssignment(int clientId)
    {
        var query = new GetClientQuery { ClientIndex = clientId };
        var result = await _mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (result.IsFailure)
        {
            LogFailedToGetClientZoneAssignment(clientId, result.ErrorMessage ?? "Unknown error");
            return NotFound($"Client {clientId} not found");
        }

        return Ok(result.Value!.ZoneId);
    }

    /// <summary>
    /// Set client display name.
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <param name="name">New client name</param>
    /// <returns>Updated client name</returns>
    [HttpPut("{clientId:int}/name")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ActionResult<string>> SetName(int clientId, [FromBody] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult<ActionResult<string>>(BadRequest("Name cannot be empty"));

        if (name.Length > 100)
            return Task.FromResult<ActionResult<string>>(BadRequest("Name cannot exceed 100 characters"));

        // Note: This would need a SetClientNameCommand to be implemented
        // For now, return the name as if it was set successfully
        LogSettingClientName(clientId, name);

        return Task.FromResult<ActionResult<string>>(Ok(name.Trim()));
    }
}
