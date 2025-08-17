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
public partial class ClientsController(IMediator mediator, ILogger<ClientsController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<ClientsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // ═══════════════════════════════════════════════════════════════════════════════
    // LOGGER MESSAGE DEFINITIONS - High-performance source generators
    // ═══════════════════════════════════════════════════════════════════════════════

    [LoggerMessage(11001, LogLevel.Warning, "Failed to get clients: {ErrorMessage}")]
    private partial void LogFailedToGetClients(string errorMessage);

    [LoggerMessage(11002, LogLevel.Warning, "Failed to get client {ClientIndex}: {ErrorMessage}")]
    private partial void LogFailedToGetClient(int clientIndex, string errorMessage);

    [LoggerMessage(11003, LogLevel.Warning, "Failed to set client {ClientIndex} volume to {Volume}: {ErrorMessage}")]
    private partial void LogFailedToSetClientVolume(int clientIndex, int volume, string errorMessage);

    [LoggerMessage(11004, LogLevel.Warning, "Failed to get client {ClientIndex} volume: {ErrorMessage}")]
    private partial void LogFailedToGetClientVolume(int clientIndex, string errorMessage);

    [LoggerMessage(11005, LogLevel.Warning, "Failed to get client {ClientIndex} for volume up: {ErrorMessage}")]
    private partial void LogFailedToGetClientForVolumeUp(int clientIndex, string errorMessage);

    [LoggerMessage(11006, LogLevel.Warning, "Failed to increase client {ClientIndex} volume: {ErrorMessage}")]
    private partial void LogFailedToIncreaseClientVolume(int clientIndex, string errorMessage);

    [LoggerMessage(11007, LogLevel.Warning, "Failed to get client {ClientIndex} for volume down: {ErrorMessage}")]
    private partial void LogFailedToGetClientForVolumeDown(int clientIndex, string errorMessage);

    [LoggerMessage(11008, LogLevel.Warning, "Failed to decrease client {ClientIndex} volume: {ErrorMessage}")]
    private partial void LogFailedToDecreaseClientVolume(int clientIndex, string errorMessage);

    [LoggerMessage(11009, LogLevel.Warning, "Failed to set client {ClientIndex} mute to {Muted}: {ErrorMessage}")]
    private partial void LogFailedToSetClientMute(int clientIndex, bool muted, string errorMessage);

    [LoggerMessage(11010, LogLevel.Warning, "Failed to get client {ClientIndex} mute state: {ErrorMessage}")]
    private partial void LogFailedToGetClientMuteState(int clientIndex, string errorMessage);

    [LoggerMessage(11011, LogLevel.Warning, "Failed to toggle client {ClientIndex} mute: {ErrorMessage}")]
    private partial void LogFailedToToggleClientMute(int clientIndex, string errorMessage);

    [LoggerMessage(11012, LogLevel.Warning, "Failed to get client {ClientIndex} after mute toggle: {ErrorMessage}")]
    private partial void LogFailedToGetClientAfterMuteToggle(int clientIndex, string errorMessage);

    [LoggerMessage(
        11013,
        LogLevel.Warning,
        "Failed to set client {ClientIndex} latency to {Latency}ms: {ErrorMessage}"
    )]
    private partial void LogFailedToSetClientLatency(int clientIndex, int latency, string errorMessage);

    [LoggerMessage(11014, LogLevel.Warning, "Failed to get client {ClientIndex} latency: {ErrorMessage}")]
    private partial void LogFailedToGetClientLatency(int clientIndex, string errorMessage);

    [LoggerMessage(
        11015,
        LogLevel.Warning,
        "Failed to assign client {ClientIndex} to zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogFailedToAssignClientToZone(int clientIndex, int zoneIndex, string errorMessage);

    [LoggerMessage(11016, LogLevel.Warning, "Failed to get client {ClientIndex} zone assignment: {ErrorMessage}")]
    private partial void LogFailedToGetClientZoneAssignment(int clientIndex, string errorMessage);

    [LoggerMessage(11017, LogLevel.Information, "Setting client {ClientIndex} name to '{Name}'")]
    private partial void LogSettingClientName(int clientIndex, string name);

    [LoggerMessage(11019, LogLevel.Warning, "Failed to set client {ClientIndex} name to '{Name}': {ErrorMessage}")]
    private partial void LogFailedToSetClientName(int clientIndex, string name, string errorMessage);

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
        var result = await this._mediator.SendQueryAsync<GetAllClientsQuery, Result<List<ClientState>>>(query);

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
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToGetClient(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!);
    }

    /// <summary>
    /// Set client volume level.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToSetClientVolume(clientIndex, volume, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return this.Ok(volume);
    }

    /// <summary>
    /// Get current client volume level.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToGetClientVolume(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.Volume);
    }

    /// <summary>
    /// Increase client volume by specified step.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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

        // Get current volume first
        var query = new GetClientQuery { ClientIndex = clientIndex };
        var clientResult = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (clientResult.IsFailure)
        {
            LogFailedToGetClientForVolumeUp(clientIndex, clientResult.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        var newVolume = Math.Min(100, clientResult.Value!.Volume + step);
        var command = new SetClientVolumeCommand { ClientIndex = clientIndex, Volume = newVolume };
        var result = await this._mediator.SendCommandAsync<SetClientVolumeCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToIncreaseClientVolume(clientIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(newVolume);
    }

    /// <summary>
    /// Decrease client volume by specified step.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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

        // Get current volume first
        var query = new GetClientQuery { ClientIndex = clientIndex };
        var clientResult = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (clientResult.IsFailure)
        {
            LogFailedToGetClientForVolumeDown(clientIndex, clientResult.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        var newVolume = Math.Max(0, clientResult.Value!.Volume - step);
        var command = new SetClientVolumeCommand { ClientIndex = clientIndex, Volume = newVolume };
        var result = await this._mediator.SendCommandAsync<SetClientVolumeCommand, Result>(command);

        if (result.IsFailure)
        {
            LogFailedToDecreaseClientVolume(clientIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(newVolume);
    }

    /// <summary>
    /// Set client mute state.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToSetClientMute(clientIndex, muted, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return this.Ok(muted);
    }

    /// <summary>
    /// Get current client mute state.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToGetClientMuteState(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.Mute);
    }

    /// <summary>
    /// Toggle client mute state.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToToggleClientMute(clientIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        // Get the new state to return
        var query = new GetClientQuery { ClientIndex = clientIndex };
        var clientResult = await this._mediator.SendQueryAsync<GetClientQuery, Result<ClientState>>(query);

        if (clientResult.IsFailure)
        {
            LogFailedToGetClientAfterMuteToggle(clientIndex, clientResult.ErrorMessage ?? "Unknown error");
            return Problem(clientResult.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(clientResult.Value!.Mute);
    }

    /// <summary>
    /// Set client audio latency compensation.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToSetClientLatency(clientIndex, latency, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return this.Ok(latency);
    }

    /// <summary>
    /// Get current client audio latency compensation.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToGetClientLatency(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.LatencyMs);
    }

    /// <summary>
    /// Assign client to a specific zone.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToAssignClientToZone(clientIndex, zoneIndex, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return this.NoContent();
    }

    /// <summary>
    /// Get the zone assignment for a client.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToGetClientZoneAssignment(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.ZoneIndex);
    }

    /// <summary>
    /// Set client display name.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToSetClientName(clientIndex, name, result.ErrorMessage ?? "Unknown error");
            return Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError);
        }

        return this.Ok(name.Trim());
    }

    /// <summary>
    /// Get client connection status.
    /// </summary>
    /// <param name="clientIndex">Client ID</param>
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
            LogFailedToGetClientConnectionStatus(clientIndex, result.ErrorMessage ?? "Unknown error");
            return this.NotFound($"Client {clientIndex} not found");
        }

        return Ok(result.Value!.Connected);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // LOGGING METHODS FOR NEW ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [LoggerMessage(11018, LogLevel.Warning, "Failed to get client {ClientIndex} connection status: {ErrorMessage}")]
    private partial void LogFailedToGetClientConnectionStatus(int clientIndex, string errorMessage);
}
