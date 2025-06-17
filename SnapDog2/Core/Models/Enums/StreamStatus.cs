namespace SnapDog2.Core.Models.Enums;

/// <summary>
/// Represents the current status of an audio stream.
/// Used to track the operational state of audio streams in the SnapDog2 system.
/// </summary>
public enum StreamStatus
{
    /// <summary>
    /// The stream is stopped and not playing audio.
    /// </summary>
    Stopped,

    /// <summary>
    /// The stream is in the process of starting up.
    /// </summary>
    Starting,

    /// <summary>
    /// The stream is actively playing audio.
    /// </summary>
    Playing,

    /// <summary>
    /// The stream is paused and can be resumed.
    /// </summary>
    Paused,

    /// <summary>
    /// The stream has encountered an error and cannot play.
    /// </summary>
    Error,
}
