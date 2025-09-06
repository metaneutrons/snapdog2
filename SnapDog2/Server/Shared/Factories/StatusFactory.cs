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
namespace SnapDog2.Server.Shared.Factories;

using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Clients.Notifications;
using SnapDog2.Server.Global.Notifications;
using SnapDog2.Server.Zones.Notifications;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Enterprise-grade factory for creating status notifications with type safety and blueprint compliance.
/// Provides centralized, consistent status notification creation with parameter validation.
/// </summary>
public partial class StatusFactory(ILogger<StatusFactory> logger) : IStatusFactory
{
    private readonly ILogger<StatusFactory> _logger = logger;

    [LoggerMessage(EventId = 113900, Level = LogLevel.Debug, Message = "Creating {NotificationType} for {EntityType} {EntityIndex}"
)]
    private partial void LogStatusCreation(string notificationType, string entityType, int entityIndex);

    [LoggerMessage(EventId = 113901, Level = LogLevel.Debug, Message = "Creating global {NotificationType}"
)]
    private partial void LogGlobalStatusCreation(string notificationType);

    [LoggerMessage(EventId = 113902, Level = LogLevel.Warning, Message = "Invalid parameter for {NotificationType}: {ParameterName} = {Value}"
)]
    private partial void LogInvalidParameter(string notificationType, string parameterName, object value);

    #region Global Status Notifications

    /// <inheritdoc />
    public SystemStatusChangedNotification CreateSystemStatusNotification(bool isOnline)
    {
        this.LogGlobalStatusCreation(nameof(SystemStatusChangedNotification));
        return new SystemStatusChangedNotification { Status = new SystemStatus { IsOnline = isOnline } };
    }

    /// <inheritdoc />
    public VersionInfoChangedNotification CreateVersionInfoNotification(VersionDetails versionDetails)
    {
        ArgumentNullException.ThrowIfNull(versionDetails);
        this.LogGlobalStatusCreation(nameof(VersionInfoChangedNotification));
        return new VersionInfoChangedNotification { VersionInfo = versionDetails };
    }

    /// <inheritdoc />
    public ServerStatsChangedNotification CreateServerStatsNotification(ServerStats serverStats)
    {
        ArgumentNullException.ThrowIfNull(serverStats);
        this.LogGlobalStatusCreation(nameof(ServerStatsChangedNotification));
        return new ServerStatsChangedNotification { Stats = serverStats };
    }

    /// <inheritdoc />
    public SystemErrorNotification CreateSystemErrorNotification(ErrorDetails errorDetails)
    {
        ArgumentNullException.ThrowIfNull(errorDetails);
        this.LogGlobalStatusCreation(nameof(SystemErrorNotification));
        return new SystemErrorNotification { Error = errorDetails };
    }

    /// <inheritdoc />
    public ZonesInfoChangedNotification CreateZonesInfoNotification(int[] availableZones)
    {
        ArgumentNullException.ThrowIfNull(availableZones);
        this.LogGlobalStatusCreation(nameof(ZonesInfoChangedNotification));
        return new ZonesInfoChangedNotification(availableZones);
    }

    #endregion

    #region Zone Status Notifications (State Changes)

    /// <inheritdoc />
    public ZonePlaybackStateChangedNotification CreateZonePlaybackStateChangedNotification(
        int zoneIndex,
        PlaybackState playbackState
    )
    {
        this.ValidateZoneIndex(zoneIndex, nameof(ZonePlaybackStateChangedNotification));
        this.LogStatusCreation(nameof(ZonePlaybackStateChangedNotification), "Zone", zoneIndex);
        return new ZonePlaybackStateChangedNotification { ZoneIndex = zoneIndex, PlaybackState = playbackState };
    }

    /// <inheritdoc />
    public ZoneVolumeChangedNotification CreateZoneVolumeChangedNotification(int zoneIndex, int volume)
    {
        this.ValidateZoneIndex(zoneIndex, nameof(ZoneVolumeChangedNotification));
        var clampedVolume = this.ValidateAndClampVolume(volume, nameof(ZoneVolumeChangedNotification));
        this.LogStatusCreation(nameof(ZoneVolumeChangedNotification), "Zone", zoneIndex);
        return new ZoneVolumeChangedNotification { ZoneIndex = zoneIndex, Volume = clampedVolume };
    }

    /// <inheritdoc />
    public ZoneMuteChangedNotification CreateZoneMuteChangedNotification(int zoneIndex, bool isMuted)
    {
        this.ValidateZoneIndex(zoneIndex, nameof(ZoneMuteChangedNotification));
        this.LogStatusCreation(nameof(ZoneMuteChangedNotification), "Zone", zoneIndex);
        return new ZoneMuteChangedNotification { ZoneIndex = zoneIndex, IsMuted = isMuted };
    }

    /// <inheritdoc />
    public ZoneTrackChangedNotification CreateZoneTrackChangedNotification(
        int zoneIndex,
        TrackInfo trackInfo,
        int trackIndex
    )
    {
        this.ValidateZoneIndex(zoneIndex, nameof(ZoneTrackChangedNotification));
        ArgumentNullException.ThrowIfNull(trackInfo);
        var validatedTrackIndex = this.ValidateTrackIndex(trackIndex, nameof(ZoneTrackChangedNotification));
        this.LogStatusCreation(nameof(ZoneTrackChangedNotification), "Zone", zoneIndex);
        return new ZoneTrackChangedNotification
        {
            ZoneIndex = zoneIndex,
            TrackInfo = trackInfo,
            TrackIndex = validatedTrackIndex,
        };
    }

    /// <inheritdoc />
    public ZonePlaylistChangedNotification CreateZonePlaylistChangedNotification(
        int zoneIndex,
        PlaylistInfo playlistInfo,
        int playlistIndex
    )
    {
        this.ValidateZoneIndex(zoneIndex, nameof(ZonePlaylistChangedNotification));
        ArgumentNullException.ThrowIfNull(playlistInfo);
        var validatedPlaylistIndex = this.ValidatePlaylistIndex(playlistIndex, nameof(ZonePlaylistChangedNotification));
        this.LogStatusCreation(nameof(ZonePlaylistChangedNotification), "Zone", zoneIndex);
        return new ZonePlaylistChangedNotification
        {
            ZoneIndex = zoneIndex,
            PlaylistInfo = playlistInfo,
            PlaylistIndex = validatedPlaylistIndex,
        };
    }

    #endregion

    // Note: Obsolete Zone Status Notifications (Blueprint Compliance - Status Publishing) methods removed
    // These used incorrect ZONE_* prefixed status IDs that don't match blueprint specification
    // Use the correct state change notifications above instead

    #region Client Status Notifications (State Changes)

    /// <inheritdoc />
    public ClientVolumeChangedNotification CreateClientVolumeChangedNotification(int clientIndex, int volume)
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientVolumeChangedNotification));
        var clampedVolume = this.ValidateAndClampVolume(volume, nameof(ClientVolumeChangedNotification));
        this.LogStatusCreation(nameof(ClientVolumeChangedNotification), "Client", clientIndex);
        return new ClientVolumeChangedNotification { ClientIndex = clientIndex, Volume = clampedVolume };
    }

    /// <inheritdoc />
    public ClientMuteChangedNotification CreateClientMuteChangedNotification(int clientIndex, bool isMuted)
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientMuteChangedNotification));
        this.LogStatusCreation(nameof(ClientMuteChangedNotification), "Client", clientIndex);
        return new ClientMuteChangedNotification { ClientIndex = clientIndex, IsMuted = isMuted };
    }

    /// <inheritdoc />
    public ClientLatencyChangedNotification CreateClientLatencyChangedNotification(int clientIndex, int latencyMs)
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientLatencyChangedNotification));
        var validatedLatency = this.ValidateLatency(latencyMs, nameof(ClientLatencyChangedNotification));
        this.LogStatusCreation(nameof(ClientLatencyChangedNotification), "Client", clientIndex);
        return new ClientLatencyChangedNotification { ClientIndex = clientIndex, LatencyMs = validatedLatency };
    }

    /// <inheritdoc />
    public ClientConnectionChangedNotification CreateClientConnectionChangedNotification(
        int clientIndex,
        bool isConnected
    )
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientConnectionChangedNotification));
        this.LogStatusCreation(nameof(ClientConnectionChangedNotification), "Client", clientIndex);
        return new ClientConnectionChangedNotification { ClientIndex = clientIndex, IsConnected = isConnected };
    }

    /// <inheritdoc />
    public ClientZoneAssignmentChangedNotification CreateClientZoneAssignmentChangedNotification(
        int clientIndex,
        int? zoneIndex
    )
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientZoneAssignmentChangedNotification));
        if (zoneIndex.HasValue)
        {
            this.ValidateZoneIndex(zoneIndex.Value, nameof(ClientZoneAssignmentChangedNotification));
        }
        this.LogStatusCreation(nameof(ClientZoneAssignmentChangedNotification), "Client", clientIndex);
        return new ClientZoneAssignmentChangedNotification { ClientIndex = clientIndex, ZoneIndex = zoneIndex };
    }

    /// <inheritdoc />
    public ClientNameChangedNotification CreateClientNameChangedNotification(int clientIndex, string name)
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientNameChangedNotification));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        this.LogStatusCreation(nameof(ClientNameChangedNotification), "Client", clientIndex);
        return new ClientNameChangedNotification { ClientIndex = clientIndex, Name = name };
    }

    /// <inheritdoc />
    public ClientStateChangedNotification CreateClientStateChangedNotification(int clientIndex, ClientState clientState)
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientStateChangedNotification));
        ArgumentNullException.ThrowIfNull(clientState);
        this.LogStatusCreation(nameof(ClientStateChangedNotification), "Client", clientIndex);
        return new ClientStateChangedNotification { ClientIndex = clientIndex, ClientState = clientState };
    }

    #endregion

    #region Client Status Notifications (Blueprint Compliance - Status Publishing)

    /// <inheritdoc />
    public ClientVolumeStatusNotification CreateClientVolumeStatusNotification(int clientIndex, int volume)
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientVolumeStatusNotification));
        var clampedVolume = this.ValidateAndClampVolume(volume, nameof(ClientVolumeStatusNotification));
        this.LogStatusCreation(nameof(ClientVolumeStatusNotification), "Client", clientIndex);
        return new ClientVolumeStatusNotification(clientIndex, clampedVolume);
    }

    /// <inheritdoc />
    public ClientMuteStatusNotification CreateClientMuteStatusNotification(int clientIndex, bool isMuted)
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientMuteStatusNotification));
        this.LogStatusCreation(nameof(ClientMuteStatusNotification), "Client", clientIndex);
        return new ClientMuteStatusNotification(clientIndex, isMuted);
    }

    /// <inheritdoc />
    public ClientLatencyStatusNotification CreateClientLatencyStatusNotification(int clientIndex, int latencyMs)
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientLatencyStatusNotification));
        var validatedLatency = this.ValidateLatency(latencyMs, nameof(ClientLatencyStatusNotification));
        this.LogStatusCreation(nameof(ClientLatencyStatusNotification), "Client", clientIndex);
        return new ClientLatencyStatusNotification(clientIndex, validatedLatency);
    }

    /// <inheritdoc />
    public ClientZoneStatusNotification CreateClientZoneStatusNotification(int clientIndex, int? zoneIndex)
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientZoneStatusNotification));
        if (zoneIndex.HasValue)
        {
            this.ValidateZoneIndex(zoneIndex.Value, nameof(ClientZoneStatusNotification));
        }
        this.LogStatusCreation(nameof(ClientZoneStatusNotification), "Client", clientIndex);
        return new ClientZoneStatusNotification(clientIndex, zoneIndex);
    }

    /// <inheritdoc />
    public ClientConnectionStatusNotification CreateClientConnectionStatusNotification(
        int clientIndex,
        bool isConnected
    )
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientConnectionStatusNotification));
        this.LogStatusCreation(nameof(ClientConnectionStatusNotification), "Client", clientIndex);
        return new ClientConnectionStatusNotification(clientIndex, isConnected);
    }

    /// <inheritdoc />
    public ClientStateNotification CreateClientStateStatusNotification(int clientIndex, ClientState clientState)
    {
        this.ValidateClientIndex(clientIndex, nameof(ClientStateNotification));
        ArgumentNullException.ThrowIfNull(clientState);
        this.LogStatusCreation(nameof(ClientStateNotification), "Client", clientIndex);
        return new ClientStateNotification(clientIndex, clientState);
    }

    #endregion

    #region Command Response Status Notifications

    /// <inheritdoc />
    public CommandStatusNotification CreateCommandStatusNotification(
        string commandId,
        string status,
        string? message = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        this.LogGlobalStatusCreation(nameof(CommandStatusNotification));
        return new CommandStatusNotification
        {
            CommandId = commandId,
            Status = status,
            Context = message,
        };
    }

    /// <inheritdoc />
    public CommandErrorNotification CreateCommandErrorNotification(
        string commandId,
        string errorCode,
        string errorMessage
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        // Try to parse errorCode as int, default to 500 if not parseable
        var errorCodeInt = int.TryParse(errorCode, out var code) ? code : 500;

        this.LogGlobalStatusCreation(nameof(CommandErrorNotification));
        return new CommandErrorNotification
        {
            CommandId = commandId,
            ErrorCode = errorCodeInt,
            ErrorMessage = errorMessage,
        };
    }

    #endregion

    #region Validation Methods

    /// <summary>
    /// Validates zone index is within acceptable range (1-based).
    /// </summary>
    private void ValidateZoneIndex(int zoneIndex, string notificationType)
    {
        if (zoneIndex < 1)
        {
            this.LogInvalidParameter(notificationType, nameof(zoneIndex), zoneIndex);
            throw new ArgumentOutOfRangeException(
                nameof(zoneIndex),
                zoneIndex,
                "Zone index must be 1-based and greater than 0"
            );
        }
    }

    /// <summary>
    /// Validates client index is within acceptable range (1-based).
    /// </summary>
    private void ValidateClientIndex(int clientIndex, string notificationType)
    {
        if (clientIndex < 1)
        {
            this.LogInvalidParameter(notificationType, nameof(clientIndex), clientIndex);
            throw new ArgumentOutOfRangeException(
                nameof(clientIndex),
                clientIndex,
                "Client index must be 1-based and greater than 0"
            );
        }
    }

    /// <summary>
    /// Validates and clamps volume to acceptable range (0-100).
    /// </summary>
    private int ValidateAndClampVolume(int volume, string notificationType)
    {
        var clampedVolume = Math.Clamp(volume, 0, 100);
        if (clampedVolume != volume)
        {
            this.LogInvalidParameter(notificationType, nameof(volume), volume);
        }
        return clampedVolume;
    }

    /// <summary>
    /// Validates track index is within acceptable range (1-based).
    /// </summary>
    private int ValidateTrackIndex(int trackIndex, string notificationType)
    {
        if (trackIndex < 1)
        {
            this.LogInvalidParameter(notificationType, nameof(trackIndex), trackIndex);
            throw new ArgumentOutOfRangeException(
                nameof(trackIndex),
                trackIndex,
                "Track index must be 1-based and greater than 0"
            );
        }
        return trackIndex;
    }

    /// <summary>
    /// Validates playlist index is within acceptable range (1-based).
    /// </summary>
    private int ValidatePlaylistIndex(int playlistIndex, string notificationType)
    {
        if (playlistIndex < 1)
        {
            this.LogInvalidParameter(notificationType, nameof(playlistIndex), playlistIndex);
            throw new ArgumentOutOfRangeException(
                nameof(playlistIndex),
                playlistIndex,
                "Playlist index must be 1-based and greater than 0"
            );
        }
        return playlistIndex;
    }

    /// <summary>
    /// Validates latency is within acceptable range (non-negative).
    /// </summary>
    private int ValidateLatency(int latencyMs, string notificationType)
    {
        if (latencyMs < 0)
        {
            this.LogInvalidParameter(notificationType, nameof(latencyMs), latencyMs);
            throw new ArgumentOutOfRangeException(nameof(latencyMs), latencyMs, "Latency must be non-negative");
        }
        return latencyMs;
    }

    #endregion
}
