namespace SnapDog2.Core.Models.Enums;

/// <summary>
/// Represents the current status of a Snapcast client.
/// Used to track the operational state of clients in the SnapDog2 system.
/// </summary>
public enum ClientStatus
{
    /// <summary>
    /// The client is connected and operational.
    /// </summary>
    Connected,

    /// <summary>
    /// The client is disconnected from the server.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The client is connected but muted.
    /// </summary>
    Muted,

    /// <summary>
    /// The client has encountered an error.
    /// </summary>
    Error,
}
