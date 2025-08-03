namespace SnapDog2.Core.Enums;

/// <summary>
/// Defines the available KNX connection types supported by Knx.Falcon.Sdk.
/// </summary>
public enum KnxConnectionType
{
    /// <summary>
    /// IP Tunneling connection - connects to KNX/IP gateway via UDP tunneling.
    /// Most common connection type for KNX installations.
    /// Uses IpTunnelingConnectorParameters.
    /// </summary>
    Tunnel,

    /// <summary>
    /// IP Routing connection - connects to KNX/IP router via UDP multicast.
    /// Used for direct access to KNX backbone without gateway.
    /// Uses IpRoutingConnectorParameters.
    /// </summary>
    Router,

    /// <summary>
    /// USB connection - connects directly to KNX USB interface.
    /// Used for direct hardware connection to KNX bus.
    /// Uses UsbConnectorParameters.
    /// </summary>
    Usb,
}
