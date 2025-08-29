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
namespace SnapDog2.Core.Abstractions;

using System;
using System.Threading;
using System.Threading.Tasks;
using SnapDog2.Core.Models;

/// <summary>
/// MQTT service interface for bi-directional communication.
/// Handles both incoming command processing and outgoing state publishing.
/// </summary>
public interface IMqttService : IAsyncDisposable
{
    /// <summary>
    /// Initializes the MQTT connection and subscribes to all configured topics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure with detailed error information.</returns>
    Task<Result> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes zone state to configured MQTT topics.
    /// Publishes to both individual status topics and comprehensive state topic.
    /// </summary>
    /// <param name="zoneIndex">Zone identifier (1-based).</param>
    /// <param name="state">Zone state to publish.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> PublishZoneStateAsync(int zoneIndex, ZoneState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes client state to configured MQTT topics.
    /// Publishes to both individual status topics and comprehensive state topic.
    /// </summary>
    /// <param name="clientIndex">Client index.</param>
    /// <param name="state">Client state to publish.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> PublishClientStateAsync(
        string clientIndex,
        ClientState state,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Publishes a custom message to a specific topic.
    /// </summary>
    /// <param name="topic">MQTT topic to publish to.</param>
    /// <param name="payload">Message payload.</param>
    /// <param name="retain">Whether to retain the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> PublishAsync(
        string topic,
        string payload,
        bool retain = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Subscribes to additional MQTT topics at runtime.
    /// </summary>
    /// <param name="topics">Topics to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from MQTT topics.
    /// </summary>
    /// <param name="topics">Topics to unsubscribe from.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UnsubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    /// <returns>True if connected to MQTT broker, false otherwise.</returns>
    bool IsConnected { get; }

    /// <summary>
    /// Publishes client status updates to MQTT topics.
    /// </summary>
    /// <param name="clientIndex">Client index.</param>
    /// <param name="eventType">Type of event (use StatusIds constants like StatusIds.ClientVolumeStatus).</param>
    /// <param name="payload">Event payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> PublishClientStatusAsync<T>(
        string clientIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Publishes zone status updates to MQTT topics.
    /// </summary>
    /// <param name="zoneIndex">Zone identifier.</param>
    /// <param name="eventType">Type of event (use StatusIds constants like StatusIds.VolumeStatus).</param>
    /// <param name="payload">Event payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> PublishZoneStatusAsync<T>(
        int zoneIndex,
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Publishes global system status updates to MQTT topics.
    /// </summary>
    /// <typeparam name="T">Type of the payload.</typeparam>
    /// <param name="eventType">Type of event (use StatusIds constants like StatusIds.SystemStatus).</param>
    /// <param name="payload">Event payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> PublishGlobalStatusAsync<T>(
        string eventType,
        T payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Event fired when MQTT connection is established.
    /// </summary>
    event EventHandler? Connected;

    /// <summary>
    /// Event fired when MQTT connection is lost.
    /// </summary>
    event EventHandler<string>? Disconnected;

    /// <summary>
    /// Event fired when a message is received on subscribed topics.
    /// </summary>
    event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;
}

/// <summary>
/// Event arguments for MQTT message received events.
/// </summary>
public class MqttMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the topic the message was received on.
    /// </summary>
    public required string Topic { get; init; }

    /// <summary>
    /// Gets the message payload as string.
    /// </summary>
    public required string Payload { get; init; }

    /// <summary>
    /// Gets whether the message was retained.
    /// </summary>
    public bool Retained { get; init; }

    /// <summary>
    /// Gets the QoS level of the message.
    /// </summary>
    public int QoS { get; init; }
}
