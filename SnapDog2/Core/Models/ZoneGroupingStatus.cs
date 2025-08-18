namespace SnapDog2.Core.Models;

/// <summary>
/// Represents the current status of zone-based client grouping.
/// </summary>
public record ZoneGroupingStatus
{
    /// <summary>
    /// Overall health status of zone grouping.
    /// </summary>
    public ZoneGroupingHealth OverallHealth { get; init; } = ZoneGroupingHealth.Unknown;

    /// <summary>
    /// Total number of zones configured.
    /// </summary>
    public int TotalZones { get; init; }

    /// <summary>
    /// Number of zones with correct grouping.
    /// </summary>
    public int HealthyZones { get; init; }

    /// <summary>
    /// Number of zones with grouping issues.
    /// </summary>
    public int UnhealthyZones { get; init; }

    /// <summary>
    /// Total number of clients across all zones.
    /// </summary>
    public int TotalClients { get; init; }

    /// <summary>
    /// Number of clients correctly grouped.
    /// </summary>
    public int CorrectlyGroupedClients { get; init; }

    /// <summary>
    /// Detailed status for each zone.
    /// </summary>
    public IReadOnlyList<ZoneGroupingDetail> ZoneDetails { get; init; } = Array.Empty<ZoneGroupingDetail>();

    /// <summary>
    /// Any issues detected during status collection.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Timestamp when this status was collected.
    /// </summary>
    public DateTimeOffset CollectedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Health status of zone grouping.
/// </summary>
public enum ZoneGroupingHealth
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy,
}

/// <summary>
/// Detailed grouping information for a specific zone.
/// </summary>
public record ZoneGroupingDetail
{
    /// <summary>
    /// Zone identifier.
    /// </summary>
    public int ZoneId { get; init; }

    /// <summary>
    /// Zone name.
    /// </summary>
    public string ZoneName { get; init; } = string.Empty;

    /// <summary>
    /// Expected Snapcast group ID for this zone.
    /// </summary>
    public string? ExpectedGroupId { get; init; }

    /// <summary>
    /// Actual Snapcast group ID(s) where zone clients are located.
    /// </summary>
    public IReadOnlyList<string> ActualGroupIds { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Clients that should be in this zone's group.
    /// </summary>
    public IReadOnlyList<ZoneClientDetail> ExpectedClients { get; init; } = Array.Empty<ZoneClientDetail>();

    /// <summary>
    /// Current grouping status for this zone.
    /// </summary>
    public ZoneGroupingHealth Health { get; init; } = ZoneGroupingHealth.Unknown;

    /// <summary>
    /// Issues specific to this zone's grouping.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Information about a client within a zone grouping context.
/// </summary>
public record ZoneClientDetail
{
    /// <summary>
    /// Client identifier.
    /// </summary>
    public int ClientId { get; init; }

    /// <summary>
    /// Client name.
    /// </summary>
    public string ClientName { get; init; } = string.Empty;

    /// <summary>
    /// Snapcast client ID.
    /// </summary>
    public string SnapcastClientId { get; init; } = string.Empty;

    /// <summary>
    /// Current Snapcast group ID where this client is located.
    /// </summary>
    public string? CurrentGroupId { get; init; }

    /// <summary>
    /// Whether the client is connected to Snapcast.
    /// </summary>
    public bool IsConnected { get; init; }

    /// <summary>
    /// Whether the client is in the correct group for its zone.
    /// </summary>
    public bool IsCorrectlyGrouped { get; init; }
}

/// <summary>
/// Result of a zone grouping reconciliation operation.
/// </summary>
public record ZoneGroupingReconciliationResult
{
    /// <summary>
    /// Number of zones that were reconciled.
    /// </summary>
    public int ZonesReconciled { get; init; }

    /// <summary>
    /// Number of clients that were moved to correct groups.
    /// </summary>
    public int ClientsMoved { get; init; }

    /// <summary>
    /// Number of new groups created.
    /// </summary>
    public int GroupsCreated { get; init; }

    /// <summary>
    /// Number of empty groups removed.
    /// </summary>
    public int GroupsRemoved { get; init; }

    /// <summary>
    /// Detailed actions taken during reconciliation.
    /// </summary>
    public IReadOnlyList<string> Actions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Any errors encountered during reconciliation.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Duration of the reconciliation operation.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Represents the result of a client name synchronization operation.
/// </summary>
public record ClientNameSyncResult
{
    /// <summary>
    /// When the synchronization started.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// When the synchronization completed.
    /// </summary>
    public DateTime EndTime { get; init; }

    /// <summary>
    /// Duration of the synchronization operation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Total number of clients processed.
    /// </summary>
    public int TotalClients { get; set; }

    /// <summary>
    /// Number of clients that were updated with new names.
    /// </summary>
    public int UpdatedClients { get; set; }

    /// <summary>
    /// Number of clients that already had the correct name.
    /// </summary>
    public int AlreadyCorrect { get; set; }

    /// <summary>
    /// Number of clients that were skipped (not found in Snapcast).
    /// </summary>
    public int SkippedClients { get; set; }

    /// <summary>
    /// Number of clients that failed to update.
    /// </summary>
    public int FailedClients { get; set; }

    /// <summary>
    /// Details of all client name updates that were performed.
    /// </summary>
    public List<ClientNameUpdate> UpdatedClientNames { get; init; } = new();

    /// <summary>
    /// Whether the synchronization was successful overall.
    /// </summary>
    public bool IsSuccessful => FailedClients == 0;

    /// <summary>
    /// Summary of actions taken during synchronization.
    /// </summary>
    public List<string> Actions
    {
        get
        {
            var actions = new List<string>();
            if (UpdatedClients > 0)
                actions.Add($"Updated {UpdatedClients} client names");
            if (AlreadyCorrect > 0)
                actions.Add($"{AlreadyCorrect} clients already had correct names");
            if (SkippedClients > 0)
                actions.Add($"Skipped {SkippedClients} clients (not found)");
            if (FailedClients > 0)
                actions.Add($"Failed to update {FailedClients} clients");
            return actions;
        }
    }
}

/// <summary>
/// Represents a single client name update operation.
/// </summary>
public record ClientNameUpdate
{
    /// <summary>
    /// The Snapcast client ID.
    /// </summary>
    public required string SnapcastId { get; init; }

    /// <summary>
    /// The old name (usually MAC address or empty).
    /// </summary>
    public required string OldName { get; init; }

    /// <summary>
    /// The new friendly name from SnapDog configuration.
    /// </summary>
    public required string NewName { get; init; }
}
