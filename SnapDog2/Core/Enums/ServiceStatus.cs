namespace SnapDog2.Core.Enums;

/// <summary>
/// Represents the status of a service.
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// Service is stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// Service is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// Service is running normally.
    /// </summary>
    Running,

    /// <summary>
    /// Service is stopping.
    /// </summary>
    Stopping,

    /// <summary>
    /// Service is in an error state.
    /// </summary>
    Error,

    /// <summary>
    /// Service is disabled.
    /// </summary>
    Disabled,
}
