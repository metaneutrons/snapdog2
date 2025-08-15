# FalconSDK v6 for .NET – Developer Documentation

## Architecture Overview

Falcon SDK v6 is the official KNX .NET library that provides software access to the KNX bus, offering a high-level API for sending and receiving KNX telegrams. Falcon 6 is built on a modern asynchronous architecture – all operations are Task-based and non-blocking, in line with .NET best practices. The internal KNX stack implementation follows the KNX specification (Handbook) with only minimal deviations, ensuring consistent behavior with KNX standards. This version also introduced several improvements such as reduced resource usage (fewer threads) and corrected naming to align with KNX terminology.

Falcon 6 consolidates its components for easier deployment: the core functionality is contained in a single assembly (`Knx.Falcon.dll`) alongside its SDK assembly (e.g. `Knx.Falcon.Sdk.dll` for the public package). This simplification removes external dependencies (e.g., no more Autofac) and makes integration more straightforward. The SDK targets .NET 6 (and .NET Framework 4.8) for broad platform support. In fact, Falcon is usable on both Windows and Linux (.NET) environments; Falcon 6 provides a unified USB implementation for all OS and supports KNX RF interfaces, improving cross-platform and IoT use cases.

#### Public vs. Manufacturer SDK

There are two editions of Falcon.NET SDK. The public SDK (available via NuGet) provides the core functionality needed to interface with KNX networks (group telegram communication, basic bus access, etc.). The manufacturer SDK (available to KNX members) extends this with advanced features for device management and low-level access. For example, the manufacturer edition enables a Bus Monitor mode (raw bus traffic capture via `ConnectorMode.Busmon`) and direct local device access (`ConnectorMode.Local`) for device programming. It also adds specialized API calls for remote device management – e.g., reading or writing individual addresses in devices’ programming mode, scanning for unused addresses, reading/writing device memory and properties, and handling KNX Secure keys. These features are not available in the public SDK. In practice, enterprise developers can build robust KNX applications with the public SDK, while KNX manufacturer members can unlock deeper control over devices as needed.

Overall, Falcon SDK v6 provides a layered architecture that abstracts KNX protocol details behind clean C# APIs. Developers interact with high-level classes (like bus connections, group address objects, etc.), and Falcon handles the KNX transport (TP, IP, RF) and protocol layers internally according to the KNX standard. This design lets you focus on your application logic while Falcon ensures reliable telegram delivery, routing, and device communication on the KNX network.

## SDK Components and API Reference

FalconSDK v6 is organized into a set of classes and namespaces that represent KNX network concepts and provide various services. Below is a structured reference of the key components, classes, and their usage.

### Core Bus Access: `KnxBus` Class

The `KnxBus` (in `Knx.Falcon.Sdk`) is the central class for KNX communication. It represents a connection or interface to a KNX network (such as via an IP gateway or USB interface). Using a `KnxBus` instance, you can connect to the KNX bus, send group write/read requests, and receive incoming group telegrams.

**Key properties and methods include:**

* **Connecting/Disconnecting**: To establish a connection, create a `KnxBus` with appropriate connector parameters (see Connector Parameters below) and call `ConnectAsync()` to open the bus (or `Connect()` if using synchronous legacy call). **Always** `Disconnect()` or dispose the bus when done. After `ConnectAsync()`, check `IsConnected` to verify the connection state.
* **Sending Group Values**: Use `WriteValueAsync(GroupAddress, GroupValue, Priority)` to send a group telegram carrying a value. For example, writing a boolean ON to a group address can be done by providing a `GroupAddress` object and a `GroupValue` containing the boolean. (Falcon v6 uses async calls, so typically you would `await WriteValueAsync`.)
* **Reading Group Values**: Use `ReadValueAsync(GroupAddress)` to request a group read. This returns a task that completes with the group’s value (as a `GroupValue` or a .NET type) once the read response is received. For instance, `bool status = (bool)await bus.ReadValueAsync(new GroupAddress("1/1/2"));` will retrieve and cast a binary value. (The returned object type depends on the datapoint type of that group address; Falcon will convert it to an appropriate .NET type when possible.)
* **Event: `GroupValueReceived`**: The `KnxBus` provides an event `GroupValueReceived` that fires whenever any group telegram is received on the bus (e.g. updates from any device). The event args (`GroupValueEventArgs`) include the Group Address and the value of the telegram, allowing the application to react to KNX events in real-time. This is useful for implementing a Group Monitor or for updating application state when KNX devices report changes. Note: In the public SDK, this event will deliver group-address telegrams. (Bus monitor mode, which would deliver all bus frames including non-group messages, is only available in manufacturer mode via a special connection mode.)
* **Connector Mode**: When creating a `KnxBus`, you may specify a mode if you have manufacturer privileges. By default (and in public SDK), the bus uses the standard group communication mode. Manufacturer SDK users can open the bus in `ConnectorMode.Busmon` (exclusive bus monitor) or `ConnectorMode.Local` (local device programming) for special use cases. These modes are passed along with connector parameters when initializing the bus.
* **Discovery and Interfaces**: The `KnxBus` class also includes static methods to discover interfaces. For example, `KnxBus.DiscoverIpDevicesAsync()` scans the local network for KNXnet/IP devices (routers/interfaces) via multicast search, returning a list of available IP interface endpoints (as `IpDeviceDiscoveryResult`). Similarly, `KnxBus.GetAttachedUsbDevices()` enumerates KNX USB interfaces attached to the machine, allowing you to list and select a USB interface programmatically (see Connector Parameters below for usage). These discovery functions help applications present interface choices to users or auto-select the appropriate interface.

**Usage**: In typical use, you will configure connector parameters, instantiate a `KnxBus`, subscribe to `GroupValueReceived` if needed, then connect. Once connected, you can send group write telegrams (e.g., to turn lights on/off, set values) and issue group read requests. The bus instance manages the KNX connection in the background, including any tunneling keep-alives or reconnect logic. Falcon v6 automatically handles reconnection for tunneling and USB connections if the link is temporarily lost, but it’s good practice to implement error handling (see Best Practices).

### Connector Types and Parameters

Falcon 6 supports multiple KNX interface types through connector parameter classes (namespace `Knx.Falcon.Configuration`). These parameters define how the `KnxBus` will connect to the KNX network. The main connector classes are:

* **IP Tunneling**: Use `IpTunnelingConnectorParameters` for KNXnet/IP Tunneling connections (to connect via a KNX IP interface or router in tunneling mode). This class takes the remote IP address (or hostname) of the KNX interface, the port (usually 3671), and a Boolean for NAT (Network Address Translation) mode. For example, to connect to a local KNX Virtual or an IP interface at 192.168.1.10, you might instantiate `new IpTunnelingConnectorParameters("192.168.1.10", 3671, false)`. (NAT mode should be `false` in most cases unless the KNX interface explicitly supports NAT routing.) Falcon v6’s `IpTunnelingConnectorParameters` replaces the older `KnxIpTunnelingConnectorParameters` from Falcon5, but serves the same purpose.
* **IP Routing (Multicast)**: Use `IpRoutingConnectorParameters` for KNXnet/IP Routing connections. KNX IP Routing is a multicast-based communication (usually on multicast address 224.0.23.12). This parameter class typically does not require a specific remote IP; instead it may allow specifying a local network adapter if needed. By using IP Routing, Falcon can listen to and send KNX telegrams via multicast (usually used when a KNX IP Router is present). The Falcon6 class `IpRoutingConnectorParameters` corresponds to the Falcon5 `KnxIpRoutingConnectorParameters` (same functionality). If multiple network interfaces are present on your machine, the parameters may include an option to select which interface to use for multicast.
* **Local USB Interface**: Use `UsbConnectorParameters` to connect via a KNX USB interface (e.g. a KNX USB dongle). This class can be constructed by providing identification of the USB interface. In Falcon6, it’s common to obtain a `UsbConnectorParameters` instance by discovery: for example, `UsbConnectorParameters.FromDiscovery(...)` can create the parameter object from a discovered USB device entry. Typically you would call `KnxBus.GetAttachedUsbDevices()` which returns a list of attached USB KNX interfaces, then convert one of those to `UsbConnectorParameters`. The `UsbConnectorParameters` class includes properties like the device’s USB path or serial, but Falcon6 simplified USB handling by using one unified driver for all OS and supporting automatic reconnect. This means using USB in Falcon6 is easier – just pick the device and connect.
* **IP (Unicast) Generic**: Falcon6 also introduces `IpUnicastConnectorParameters` (which replaced a general `KnxIpConnectorParameters` in Falcon5). This might be used internally or for future expansion (it’s hinted that secure connections might be configured through it). In most cases, you will use the specific Tunneling or Routing classes above, but be aware that Falcon’s API unifies certain IP connection logic under this class.

All connector parameter classes inherit from a common base (likely `ConnectorParameters`) and share some capabilities. Notably, Falcon6 supports KNX Secure connections (both IP Secure and KNX Data Secure) via these parameters. For IP Tunneling or Routing, if a secure interface is used, the parameters include fields for authentication (like the device’s KNX Secure authentication code or tool key). In Falcon6, the formerly separate secure parameter classes have been merged into the standard ones (secure tunneling info is now part of `IpTunnelingConnectorParameters`, and secure routing part of `IpRoutingConnectorParameters`). This means you can configure secure sessions by populating the relevant fields on these parameter objects (for enterprise use, ensure you manage and protect any security keys).

**Connection Strings**: To simplify storage of connection configurations (e.g., in config files or databases), Falcon provides methods to serialize connector parameters. You can call `ConnectorParameters.ToConnectionString()` to get a string representation of any connector setup, and later use `ConnectorParameters.FromConnectionString(string)` to rehydrate it. This is handy for enterprise applications that allow dynamic configuration of interfaces – you might let a user input an interface, then save the connection string for automatic reconnection next time. Using these methods is the recommended way to persist connections (the older binary serialization approach was removed in Falcon6).

### Addressing and Data Types

Working with KNX means working with Group Addresses, Individual Addresses, and various data point types. Falcon SDK provides classes to handle these concepts:

* **`GroupAddress` Class**: Represents a KNX Group Address (used for group communication, e.g., sensors and actuators listening on group addresses). A `GroupAddress` can be constructed from its string notation ("x/y/z") or from a 16-bit address value. For convenience, Falcon allows creating a `GroupAddress` simply by passing the usual textual format (e.g., `new GroupAddress("2/0/15")`). This class provides properties to retrieve the main/sub/group parts and overrides `toString` for easy display. `GroupAddress` objects are used throughout the Falcon API whenever a group address needs to be specified (for reading, writing, or subscribing to events). Internally, Falcon uses group addresses to route telegrams to the correct devices. Note: Falcon does not require you to manually map group addresses to datapoint types – you can send raw values, but for correct interpretation of values (especially on receive) you should know the DPT (see `GroupValue` below).
* **`IndividualAddress` Class**: Represents a KNX Individual Address (the unique address of a KNX device, in the format “Area.Line.Device”, e.g., “1.1.12”). `IndividualAddress` is used primarily for device management and addressing specific devices (e.g., for programming or direct device queries). You may encounter `IndividualAddress` in advanced operations like reading device memory or setting device addresses. Construct it similarly with a string "1.1.12" or via three-level components. This class ensures addresses are within valid KNX ranges and provides formatting.
* **`GroupValue` and Datapoint Types**: KNX group telegrams carry values formatted according to Datapoint Types (DPTs). Falcon abstracts the telegram payload with the `GroupValue` class (found in `Knx.Falcon` namespace). A `GroupValue` holds a value of a specific KNX DPT and knows how to serialize/deserialize it to the KNX telegram format. In many cases, you can create a `GroupValue` by passing a standard .NET value, and Falcon will infer the appropriate DPT encoding if unambiguous (for example, `new GroupValue(true)` will create a 1-bit `GroupValue` for DPT1 Boolean). For more complex types, you might use explicit factory methods or provide the DPT ID. Falcon 6 expanded the built-in Datapoint Type converters, meaning it supports a wide range of DPTs out of the box (from simple 1-bit up to 14-byte types, floats, time, etc.). It will automatically convert incoming payloads to human-readable values in event callbacks (the `GroupValueEventArgs.Value` will be a .NET type like `bool`, `int`, `float`, etc., if the DPT is known). For sending, you can either use `GroupValue` or directly pass a .NET type to the `WriteValue` method (Falcon will internally wrap it in a `GroupValue` of the correct type if it can determine the DPT, possibly from the project’s KNX master data). If needed, you can also manually use the DPT converter utilities to transform raw data bytes to typed values and vice versa.
* **KNX Property Data (PDT)**: Beyond group communication, devices have properties accessible via device management services. Falcon 6 introduced Property Data Type (PDT) converters. These are used in manufacturer-level features when reading or writing device object properties. They ensure that property values (which can be structures, etc.) are correctly interpreted. If you use methods like `KnxDevice.WriteControlPropertyValueAsync`, the SDK will handle PDT conversion for you given the property type (e.g., turning a desired value into the correct byte sequence for that property).

### Network Management: `KnxNetwork` Functions

The `KnxNetwork` functionality in Falcon (available primarily in the manufacturer SDK) deals with network-wide operations, particularly around individual address management and device discovery. It provides methods that operate at the KNX network level, beyond the simple group communications. Key capabilities include:

* **Individual Address Programming**: Methods like `ReadIndividualAddressesByProgrammingModeAsync` and `WriteIndividualAddressByProgrammingModeAsync` allow reading or assigning KNX individual addresses of devices that are in programming mode (i.e., devices with their programming button pressed). For example, an installer tool can scan for any device in programming mode and automatically assign it an address using these calls. Falcon will handle the lower-level protocol (propagation of programming mode search, etc.) and give you the results or status.
* **Individual Address by Serial Number**: You can directly target a device by its KNX Serial Number (unique hardware ID) if known. The method `KnxNetwork.ReadIndividualAddressBySerialNumberAsync(string serial)` finds a device on the bus by its serial and returns its individual address. Conversely, `WriteIndividualAddressBySerialNumberAsync` will assign a new individual address to a device specified by serial (this requires the device’s programming mode or certain permissions). These are powerful for automated commissioning in enterprise scenarios where device serials might be known ahead of time.
* **Domain Address Management**: For KNX RF and certain secure applications, devices have a domain address (used in KNX RF multi and in secure installations). The `KnxNetwork.ReadDomainAddress...` and `WriteDomainAddress...` methods allow reading/writing a device’s domain address either by having it in programming mode or by serial. This is a specialized feature likely used in advanced installation setup (manufacturer SDK).
* **Scanning for Free Addresses**: The method `ScanIndividualAddressRangeAsync` (and a faster variant `ScanIndividualAddressRangeFastAsync`) can probe a range of individual addresses to see which are in use. This is useful to find free addresses or detect conflicts on the bus. For example, you could scan 1.1.1 through 1.1.255 to build a list of used addresses on line 1.1. The “fast” version uses an optimized KNX network scan (e.g., sending multiple parallel requests or using a dedicated KNX service) to speed up the process. This feature is great for tools that need to suggest an unused address or audit the network.
* **Network Parameter and Diagnostics**: `ReadNetworkParameterAsync` / `WriteNetworkParameterAsync` let you interact with low-level network parameters (like individual address, routing configs, etc.) on devices. There are also “System” variants for system-specific parameters. These calls are typically used for advanced configuration (for example, reading the backbone key or unlocking certain system flags on devices). They are synchronous to the device’s memory/management protocol in effect (even though the API is async in Falcon, the underlying operation is a direct device communication).
* **Device Discovery**: Falcon’s network functions, in combination with the bus, allow discovering devices in various ways. Aside from scanning addresses, you can use `ReadSerialNumbersAsync` to get the serial numbers of all devices in a certain address range or line. This can help map out the devices physically present. Additionally, a method `IsIndividualAddressFreeAsync` (moved to the bus object in v6) can quickly check if a specific address responds (useful before assigning it).

In Falcon6, these network-level methods might be accessed through a `KnxNetwork` class or via the `KnxBus` object directly. Typically, the `KnxBus` has a property or associated object for network management. For example, you might call `bus.Network.ScanIndividualAddressRangeAsync(...)` to perform a scan, or simply `bus.IsIndividualAddressFreeAsync(address)` for a quick check (this was moved to bus in v6). Note: All these operations require a connected `KnxBus` and most require the manufacturer SDK, as they utilize device management telegrams not available in the public API.

### Device Management: `KnxDevice` Operations

For direct device access and management, Falcon provides a concept of a `KnxDevice` (in manufacturer SDK). A `KnxDevice` represents an individual KNX device on the bus, allowing actions that target that single device’s configuration or state. You typically obtain a `KnxDevice` instance via a network operation (e.g., open a device in local mode, or perhaps through discovery by individual address). Once you have a `KnxDevice`, you can perform operations such as:

* **Memory Operations**: Use `KnxDevice.WriteMemoryAsync` (and related read methods) to write to a device’s memory addresses. This is a low-level operation that can configure device memory or read arbitrary memory (often used for device configuration, like legacy programming models or reading diagnostic info). It requires knowledge of the device’s memory map and is generally manufacturer-specific. In practice, this is rarely used except by device configuration tools or for custom device actions not covered by standard properties.
* **Property Value Access**: KNX devices expose standardized properties (interface object properties). With Falcon, you can write or read these via methods like `WriteControlPropertyValueAsync` or possibly a read counterpart (though not explicitly listed, likely exists). The Control properties (`PDT_CONTROL`) are a special type that can trigger device functions. For example, enabling programming mode or resetting certain counters might be done via property writes. Falcon’s converter will ensure you pass the correct data type for the property.
* **Function Properties**: Some devices support functional properties (a KNX concept for calling functions on devices, introduced in KNX spec). Falcon exposes this via `KnxDevice.FunctionPropertyCommandAsync`, which allows invoking such functions (for example, to trigger self-test, etc., if the device supports it).
* **Device Reset/Restart**: You can remotely restart a KNX device using `KnxDevice.RestartAsync`. This is equivalent to power-cycling the device (if it supports the restart mechanism). This can be useful in maintenance scenarios to apply configurations or recover unresponsive devices.
* **Setting Security Keys**: For KNX Secure devices, `KnxDevice.WriteKeyAsync` is provided to set or update the device’s security access keys (like the authentication key for secure communication). This is a critical operation typically done during commissioning. The method returns the level of access granted by the device in response, so you know if the key was accepted. This is exclusively manufacturer-level functionality.

Using `KnxDevice` usually involves first opening the device in programming mode or direct mode. In Falcon manufacturer SDK, you might call something like `bus.OpenDevice(ConnectorMode.Local, individualAddress)` to get a `KnxDevice` object for a device on the bus (this uses the Local Device connection mode which grants exclusive access for device management). Once you have that, the above methods become available. Always ensure the bus is in the correct mode (`Local`) before performing device writes, and close the device (or switch the bus back to normal mode) after completing the operations, so the bus can resume normal traffic.

### Summary of Namespaces and Assemblies

To integrate FalconSDK v6 in your project, you will typically:

* Reference the NuGet package `Knx.Falcon.Sdk` (for public SDK) – this brings in `Knx.Falcon.Sdk.dll` and its dependencies. It will also include `Knx.Falcon.dll` (core library) and possibly `Knx.Falcon.UsbAccess.dll` if needed for USB support. All required assemblies will be in your build output after install. Ensure that `knx_master.xml` (KNX Master data) and `knx_interfaces.xml` (KNX USB interface definitions) are present on the system if ETS is not installed – the SDK will use these files to know about datapoint types and USB hardware (place them in `C:\ProgramData\KNX\Falcon` on Windows, or include in your app folder).
* Use the `Knx.Falcon.Sdk` namespace in your code for high-level classes like `KnxBus` and network functions. The base KNX types (`GroupAddress`, `IndividualAddress`, `GroupValue`, etc.) might be in `Knx.Falcon` or sub-namespaces like `Knx.Falcon.Data`. Connector parameter classes reside in `Knx.Falcon.Configuration`. If using manufacturer features, `KnxDevice` and others may be in `Knx.Falcon.Sdk` as well.

Having the correct versions of .NET (≥ .NET 6 or .NET Framework 4.8) is required to use Falcon v6. The SDK is designed to work in long-running enterprise services, with thread-safe async operations and detailed exceptions for troubleshooting. In the next sections, we outline best practices for using Falcon in enterprise applications and provide code examples for common tasks.

## Best Practices for Enterprise Use

When integrating FalconSDK v6 into enterprise software (or advanced AI-driven building management systems), consider the following guidelines to ensure reliability, performance, and maintainability:

* **Leverage Asynchronous Calls**: Falcon 6’s API is fully async; use the `async/await` pattern throughout your code. Avoid blocking calls on the main/UI thread. For example, prefer `await bus.ConnectAsync()` over calling a synchronous method. This ensures your application remains responsive and can handle many KNX operations concurrently. It also allows Falcon to internally optimize I/O without hogging threads.
* **Proper Resource Management**: **Always** disconnect and dispose of the `KnxBus` when finished. The best practice is to use a `using` statement or `try/finally` to encapsulate the bus lifetime. This makes sure the underlying socket or USB handle is released. In continuous services, if the KNX connection should be long-lived, handle application shutdown events to cleanly disconnect. Also, avoid creating multiple bus instances for the same interface; reuse one connection if possible, as each connection may consume KNX interface resources.
* **Error Handling and Diagnostics**: Wrap KNX operations in `try/catch` blocks to handle exceptions. Falcon v6 provides detailed exception messages and diagnostic data when errors occur (e.g. if a connect fails due to IP routing issues, the exception will clarify the cause). Inspect exception properties for additional info – for example, a `KnxException` might include a `Diagnostic` or `Statistics` object with counts of retries, etc. In enterprise scenarios, log these details; they are invaluable for troubleshooting connectivity issues or bus errors. If a group read times out or a device doesn’t respond, Falcon’s exception will differentiate the cause (timeout vs. address not found, etc.).
* **Thread Safety and Concurrency**: The Falcon SDK is generally thread-safe for multiple operations, but it’s best to funnel KNX operations through a dedicated context (for example, a single background task or a message queue). This avoids sending too many telegrams at once which could flood the bus. If you need to perform many actions (like setting 100 group values), introduce slight delays or ensure they await one after the other to respect bus bandwidth and rate limits. Falcon will queue telegrams internally as needed, but KNX has a finite throughput.
* **Use Auto-Reconnect Features**: In KNX IP Tunneling, connections may drop (due to network issues or interface resets). Falcon 6 has built-in auto-reconnect for tunneling and USB. You can generally rely on this for short outages – the SDK will try to re-establish the connection automatically. However, monitor the `IsConnected` state and subscribe to any future event (if provided) for disconnects. For critical systems, implement a watchdog that logs and alerts if the bus connection is down for too long. When using IP Routing (multicast), reconnection is less of an issue, but ensure your network interface remains active (Wi-Fi going down, etc., would stop traffic).
* **Interface Discovery and Selection**: Use the Falcon discovery methods rather than hard-coding interface details in config. For example, if your application runs on a server with multiple NICs, call `DiscoverIpDevicesAsync` to find the actual KNX IP Router’s address at runtime instead of assuming it. Similarly, for USB, use `GetAttachedUsbDevices()` to get the correct `UsbConnectorParameters`. This makes your solution more robust to environment changes – if the KNX interface IP or USB port changes, your app can adapt dynamically.
* **Avoid Simultaneous Multiple Connections to Same Interface**: KNX interfaces (especially USB or tunneling interfaces) often support only a limited number of simultaneous connections (e.g., a tunneling interface might allow 5 concurrent clients). In an enterprise environment with multiple services, coordinate so you don’t exceed these. If your application only needs one connection, reuse one `KnxBus`. If you need to separate concerns (for example, one connection for monitoring and one for control), ensure your hardware supports it or consider using KNX IP Routing which can feed multiple listeners.
* **Master Data and XML Files**: If your application runs on a machine without ETS, include the KNX master data file (`knx_master.xml`) and interface definitions (`knx_interfaces.xml`) in the expected directory or your application folder. These files contain definitions for datapoint types and USB interface specifics. Having them ensures Falcon can recognize all DPTs (for value conversion) and any USB devices. Falcon will use the versions of these XML files with the highest version number it finds (either in `ProgramData\KNX\Falcon` or provided in your app directory). Keep them updated when new KNX specifications are released (KNX Association updates these with new datapoints, etc.).
* **Logging and Monitoring**: In an enterprise context, enabling logging for KNX traffic and Falcon operations is useful. While Falcon itself might not have an internal logger exposed, you can log all group telegrams via the `GroupValueReceived` event (e.g., log each telegram to a file or database for audit). Also log connect/disconnect events and any use of device management functions (for traceability). If using manufacturer functions, be cautious – changes like writing individual addresses or keys should be securely logged and controlled.
* **Security Practices**: Treat KNX interface credentials (if any) and KNX Secure keys as secrets. Falcon’s `WriteKeyAsync` and secure connection parameters involve sensitive data – ensure these are stored encrypted in configuration and never exposed in logs. If your system uses KNX IP Secure, regularly rotate keys as per organizational policy and use Falcon’s methods to update device keys (`WriteKeyAsync`) accordingly. Also, consider running your KNX integration service on a secure, isolated network segment, as KNX traffic is typically not encrypted unless using KNX Secure.
* **Testing and Simulation**: Use KNX Virtual or a test setup to simulate your KNX network during development. FalconSDK can communicate with KNX Virtual (just treat it as an IP Tunneling server at `127.0.0.1:3671`). Keep in mind to disable NAT mode in parameters for KNX Virtual (set NAT flag to `false`). Automated tests can utilize KNX Virtual to send/receive dummy telegrams. This is particularly useful if you have AI logic that needs sensor inputs; you can simulate those via virtual devices and see how your software reacts.
* **Migration from Falcon5**: If upgrading legacy code, be aware of renamed APIs in Falcon6. Many classes moved namespaces (e.g., connector parameters under `Knx.Falcon.Configuration` instead of `Knx.Bus.Common.Configuration`). Also all asynchronous methods now return `Task` and often have “Async” suffix. Review the Falcon6 vs Falcon5 changes reference for a full list of API changes. Adjust your code accordingly and retest all critical functionality. The improvements in Falcon6 (like better exception clarity and fewer thread issues) should overall benefit enterprise reliability.

By following these practices, you can ensure that your KNX integration using FalconSDK v6 is robust, maintainable, and ready for production deployment.

## Code Examples

Below are code examples demonstrating core tasks with FalconSDK v6. These examples assume you have added the Falcon SDK to your project (via NuGet) and have the appropriate `using` directives (`Knx.Falcon.Sdk`, `Knx.Falcon.Configuration`, etc.). For brevity, minimal error handling is shown – in production, wrap calls in `try/catch` and follow best practices as above.

### 1. Initializing the SDK and Connecting to a KNX Network

#### Example 1 – Connect via KNX IP (Tunneling)

This example connects to a KNX IP interface in tunneling mode using its IP address. It then sends a test write and disconnects.

```csharp
using Knx.Falcon.Sdk;
using Knx.Falcon.Configuration;

async Task ConnectViaIpTunneling()
{
    // Define IP tunneling parameters for the KNX interface (replace with your interface IP)
    var options = new IpTunnelingConnectorParameters("192.168.1.10", 3671, false);

    using (KnxBus bus = new KnxBus(options))
    {
        await bus.ConnectAsync();  // Establish connection to KNX IP interface
        Console.WriteLine($"Connected: {bus.IsConnected}");

        // Write a GroupValue (e.g., turn on light at group address 1/0/5)
        var ga = new GroupAddress("1/0/5");
        await bus.WriteValueAsync(ga, new GroupValue(true));  // send boolean 'true'
        Console.WriteLine("Sent ON command to 1/0/5");

        // (Optional) Read back a status group address
        var statusGa = new GroupAddress("1/0/6");
        object result = await bus.ReadValueAsync(statusGa);
        Console.WriteLine($"Status 1/0/6 = {result}");

        // Always disconnect when done
        await bus.DisconnectAsync();
    } // using will also dispose the bus
}
```

> **Explanation**: We create `IpTunnelingConnectorParameters` with the interface’s IP and port. The third parameter `false` indicates NAT is not used (which is typical). We instantiate `KnxBus` with these options and call `ConnectAsync()`. After connecting, we use `WriteValueAsync` to send a boolean `true` to group address 1/0/5 (this might correspond to turning on a light, depending on the KNX installation). We then use `ReadValueAsync` on 1/0/6 to perhaps get a status (the returned result is an object – likely a `bool` or number depending on that group’s DPT, which we simply print). Finally, we disconnect. All calls are awaited to ensure each KNX operation completes before moving on.

#### Example 2 – Connect via KNX USB interface

This example scans for a USB interface and connects to it.

```csharp
using Knx.Falcon.Sdk;
using Knx.Falcon.Configuration;
using System.Linq;

async Task ConnectViaUsb()
{
    // Discover attached KNX USB interfaces
    var usbDevices = KnxBus.GetAttachedUsbDevices();
    if (usbDevices.Length == 0)
    {
        Console.WriteLine("No KNX USB interfaces found.");
        return;
    }
    // Take the first found device (you can select by device info as needed)
    var usbParams = UsbConnectorParameters.FromDiscovery(usbDevices[0]);

    using (KnxBus bus = new KnxBus(usbParams))
    {
        await bus.ConnectAsync();
        Console.WriteLine("USB Interface connected. Address: " + bus.IndividualAddress);

        // Example: listen for any group telegrams on the bus
        bus.GroupValueReceived += (sender, e) =>
        {
            Console.WriteLine($"[KNX Event] Group {e.Address} = {e.Value}");
        };

        // Send a test telegram (e.g., dimming value 50% to group 2/0/1)
        await bus.WriteValueAsync(new GroupAddress("2/0/1"), new GroupValue(50));
        Console.WriteLine("Dimming command sent on 2/0/1.");

        // Keep the connection open to receive events (in a real app, run indefinitely or until a condition)
        await Task.Delay(10000);  // wait 10 seconds to collect events as an example

        await bus.DisconnectAsync();
    }
}
```

> **Explanation**: We call `KnxBus.GetAttachedUsbDevices()` which returns an array of discovered USB interfaces (if multiple, you could list them). We use the first device found and convert it to a `UsbConnectorParameters` via `FromDiscovery`. We then connect the bus. We output the `bus.IndividualAddress` which typically is the local device address the interface is using on the KNX line (usually 0.0.0 or a configured address). We also subscribe to the `GroupValueReceived` event before sending any telegrams, so we can log incoming telegrams. The event handler simply prints any group address and value it sees. We then send a sample dimming command (50% level as an integer) to group address 2/0/1. After waiting 10 seconds (to simulate the app running and possibly receiving events), we disconnect. In a real service, you would likely not disconnect immediately but keep running and handling events continuously. (The `Task.Delay` here is just to allow some asynchronous event activity in the example.)

### 2. Reading and Writing Group Values

The following snippet shows a focused example of reading and writing group values, including error handling. This would typically be part of a larger logic (for instance, reading a sensor and then commanding an actuator).

```csharp
using Knx.Falcon.Sdk;
using Knx.Falcon.Configuration;

async Task ReadAndWriteExample(KnxBus bus)
{
    // Assume bus is already connected at this point.
    var target = new GroupAddress("3/2/1");    // e.g., a temperature setpoint
    var source = new GroupAddress("3/2/2");    // e.g., a temperature sensor

    try
    {
        // Read current sensor value
        object currentValue = await bus.ReadValueAsync(source);
        Console.WriteLine($"Sensor 3/2/2 value = {currentValue}");

        // Write a new setpoint value (if sensor value too low/high) - example logic
        if (currentValue is double temperature && temperature < 20.0)
        {
            double newSetpoint = 22.0;
            await bus.WriteValueAsync(target, new GroupValue(newSetpoint));
            Console.WriteLine($"Setpoint {target} adjusted to {newSetpoint} °C");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"KNX communication error: {ex.Message}");
        // Additional logging or recovery...
    }
}
```

> **Explanation**: In this example, we assume the bus is already connected. We define a group address `source` (e.g., a temperature sensor group) and `target` (e.g., a thermostat setpoint group). We use `ReadValueAsync` to get the current sensor reading. Falcon will return a `double` if the group is configured as a temperature (e.g., DPT 9.001 for °C) – here we check if `currentValue` is a `double`. We then have some simple logic: if the temperature is below 20, we send a new setpoint of 22°C to the thermostat via `WriteValueAsync`. The value is wrapped in a `GroupValue` – Falcon will understand a `double` likely as a DPT9 float and send it accordingly. We wrap these calls in a `try/catch` to handle any exceptions (e.g., if the read times out because no device responded on 3/2/2, or if the bus isn’t connected). In case of error, we log it. This pattern can be expanded with specific exception types (e.g., catching a specific Falcon exception class) if needed for finer control.

### 3. Subscribing to Group Events (Receiving Telegrams)

One of the powerful features of Falcon is reacting to bus events. The following example shows how to subscribe to group telegram events to, for instance, update an in-memory state or trigger logic when any device sends a telegram.

```csharp
using Knx.Falcon.Sdk;

void SetupGroupMonitoring(KnxBus bus)
{
    bus.GroupValueReceived += (sender, e) =>
    {
        // e.Address is of type GroupAddress, e.Value is the payload value
        Console.WriteLine($"Telegram received: Group {e.Address} -> Value: {e.Value}");

        // Example: if Group 4/0/1 (Alarm) is true, perform some action
        if (e.Address.ToString() == "4/0/1" && e.Value is bool alarmOn && alarmOn)
        {
            TriggerAlarmProcedure();
        }
    };
}
```

> **Explanation**: We attach an event handler to `bus.GroupValueReceived`. Now, whenever any group telegram is received by our interface, the lambda will execute. We print the group address and value. Then we illustrate a conditional: if the telegram was on address 4/0/1 and carried a boolean `true` (perhaps meaning an alarm triggered), we call a hypothetical `TriggerAlarmProcedure()` in our application. This pattern allows your software to listen to KNX events proactively. For instance, an AI system might increase ventilation if a humidity sensor group value exceeds a threshold – you’d catch that in this event and then issue commands accordingly. In an enterprise setting, you might maintain an in-memory cache or database of all group addresses and their last known values; you can update that cache in this event handler for real-time state tracking.
>
> **Important**: The `GroupValueReceived` event only works when the bus is connected in normal mode (not busmonitor mode), and it will receive group communications. You do not have to individually subscribe to specific group addresses – the interface will report all, and you filter in code as shown. If you only care about specific addresses, you can add checks or even ignore others to reduce processing. The overhead of the event for all telegrams is usually fine, but heavy processing inside the event should be avoided (offload to another task if needed, to not block the KNX handling thread).

### 4. Managing KNX Devices (Advanced Device Operations)

Finally, an example utilizing manufacturer-level features. This scenario: we have a device’s serial number and we want to find its current individual address on the bus, then perhaps reset the device.

```csharp
using Knx.Falcon.Sdk;
using Knx.Falcon.Configuration;

async Task FindAndRestartDevice(KnxBus bus, string deviceSerial)
{
    // Ensure using manufacturer Falcon SDK and bus opened with appropriate rights (ConnectorMode.Local if needed)
    var network = bus.Network;  // assume this property provides KnxNetwork functionality

    try
    {
        // Find the individual address of the device by its KNX Serial Number
        IndividualAddress address = await network.ReadIndividualAddressBySerialNumberAsync(deviceSerial);
        Console.WriteLine($"Device {deviceSerial} is at address {address} on the bus.");

        // If address found, open the device (local mode) and send a restart
        if (address != null)
        {
            // (Pseudo-step) possibly open device in programming mode if required
            // e.g., await bus.DisconnectAsync(); bus.Connect(ConnectorMode.Local, ...); in a real case

            // Restart the device
            await KnxDevice.RestartAsync(bus, address);  // illustrative, actual usage may differ
            Console.WriteLine($"Restart command sent to device {address}.");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error managing device: {ex.Message}");
    }
}
```

> **Explanation**: In this pseudo-code (for illustration, as it uses manufacturer-only methods), we use the `bus.Network.ReadIndividualAddressBySerialNumberAsync` to get a device’s address knowing its serial. We then print the result. If we got an address, we proceed to send a restart. How to send a restart can vary – here we show a static `KnxDevice.RestartAsync(bus, address)` call for simplicity (assuming Falcon provides a way to target the device; in reality it might require obtaining a `KnxDevice` object via a local connection). We included a comment that, if needed, one might re-open the bus in local programming mode to gain exclusive access before performing device operations. In Falcon, some device management tasks might require the `ConnectorMode.Local`. The `try/catch` captures any error (for example, if the serial was not found on the network, the read might throw or return null, depending on implementation).
>
> This example demonstrates how an enterprise tool might inventory devices: given a list of serial numbers, find their addresses and perform actions (like reset or update keys). Falcon’s manufacturer API enables building such management software. Always remember these operations should be done with care – for instance, restarting a device will briefly take it off the bus, and writing addresses can conflict if not done in a controlled environment (usually when other bus activity is low).

---

These examples cover initialization, basic group communication, event-driven updates, and advanced device management. By combining these patterns, developers can create full-featured KNX integration applications – from simple monitoring services to complex AI-driven control systems – using the FalconSDK v6 for .NET.
