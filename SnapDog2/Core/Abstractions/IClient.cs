namespace SnapDog2.Core.Abstractions;

using System.Threading.Tasks;
using SnapDog2.Core.Models;

/// <summary>
/// Represents an individual Snapcast client with control operations.
/// </summary>
public interface IClient
{
    /// <summary>
    /// Gets the client ID.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the client name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sets the volume for this client.
    /// </summary>
    /// <param name="volume">The volume level (0-100).</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetVolumeAsync(int volume);

    /// <summary>
    /// Sets the mute state for this client.
    /// </summary>
    /// <param name="mute">True to mute, false to unmute.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetMuteAsync(bool mute);

    /// <summary>
    /// Sets the latency for this client.
    /// </summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetLatencyAsync(int latencyMs);
}
