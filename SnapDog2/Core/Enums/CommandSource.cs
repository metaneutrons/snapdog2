namespace SnapDog2.Core.Enums;

/// <summary>
/// Represents the source of a command.
/// </summary>
public enum CommandSource
{
    /// <summary>
    /// Command originated internally within the application.
    /// </summary>
    Internal,

    /// <summary>
    /// Command originated from the REST API.
    /// </summary>
    Api,

    /// <summary>
    /// Command originated from MQTT.
    /// </summary>
    Mqtt,

    /// <summary>
    /// Command originated from KNX.
    /// </summary>
    Knx,

    /// <summary>
    /// Command originated from WebSocket connection.
    /// </summary>
    WebSocket,
}
