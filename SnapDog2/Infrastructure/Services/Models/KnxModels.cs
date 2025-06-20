using SnapDog2.Core.Configuration;

namespace SnapDog2.Infrastructure.Services.Models;

/// <summary>
/// Represents the connection status of the KNX service.
/// </summary>
public class KnxConnectionStatus
{
    /// <summary>
    /// Gets or sets whether the KNX service is connected.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Gets or sets the KNX gateway address.
    /// </summary>
    public string Gateway { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the KNX gateway port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the connection state description.
    /// </summary>
    public string ConnectionState { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last connection attempt timestamp.
    /// </summary>
    public DateTime? LastConnectionAttempt { get; set; }

    /// <summary>
    /// Gets or sets the last successful connection timestamp.
    /// </summary>
    public DateTime? LastSuccessfulConnection { get; set; }

    /// <summary>
    /// Gets or sets any error message from the last connection attempt.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of subscribed group addresses.
    /// </summary>
    public int SubscribedAddressCount { get; set; }
}

/// <summary>
/// Represents a KNX device discovered on the network.
/// </summary>
public class KnxDeviceInfo
{
    /// <summary>
    /// Gets or sets the individual address of the device.
    /// </summary>
    public string IndividualAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serial number of the device.
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Gets or sets the manufacturer ID.
    /// </summary>
    public int? ManufacturerId { get; set; }

    /// <summary>
    /// Gets or sets the device type.
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// Gets or sets whether the device is in programming mode.
    /// </summary>
    public bool IsProgrammingMode { get; set; }

    /// <summary>
    /// Gets or sets when the device was discovered.
    /// </summary>
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a KNX group value read/write operation.
/// </summary>
public class KnxGroupValueOperation
{
    /// <summary>
    /// Gets or sets the group address.
    /// </summary>
    public KnxAddress Address { get; set; }

    /// <summary>
    /// Gets or sets the value as byte array.
    /// </summary>
    public byte[] Value { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the operation type (Read/Write).
    /// </summary>
    public KnxOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the operation.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets any error message from the operation.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents the type of KNX operation.
/// </summary>
public enum KnxOperationType
{
    /// <summary>
    /// Read operation.
    /// </summary>
    Read,

    /// <summary>
    /// Write operation.
    /// </summary>
    Write,

    /// <summary>
    /// Response operation.
    /// </summary>
    Response,
}

/// <summary>
/// Represents a KNX data type for group communication.
/// </summary>
public class KnxDataType
{
    /// <summary>
    /// Gets or sets the data point type (DPT) identifier.
    /// </summary>
    public string DptId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data size in bits.
    /// </summary>
    public int SizeInBits { get; set; }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public object? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public object? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the unit of measurement.
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Gets or sets the description of the data type.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Represents a KNX group address configuration.
/// </summary>
public class KnxGroupAddressConfig
{
    /// <summary>
    /// Gets or sets the group address.
    /// </summary>
    public KnxAddress Address { get; set; }

    /// <summary>
    /// Gets or sets the name/description of the group address.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data point type.
    /// </summary>
    public string DataPointType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this address should be monitored.
    /// </summary>
    public bool IsMonitored { get; set; }

    /// <summary>
    /// Gets or sets the function (e.g., "Lighting", "Heating", "Security").
    /// </summary>
    public string Function { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the room or zone this address belongs to.
    /// </summary>
    public string? Room { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// --- API Request/Response DTOs for KnxController ---

/// <summary>
/// Request model for writing a value to a KNX group address.
/// </summary>
public record WriteKnxValueRequest
{
    /// <summary>
    /// The KNX group address (e.g., "1/1/1").
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// The value to write, as a Base64 encoded string.
    /// Max 14 bytes raw.
    /// </summary>
    public required string Value { get; init; } // Base64 encoded bytes

    /// <summary>
    /// Optional description for the operation.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Request model for reading a value from a KNX group address.
/// </summary>
public record ReadKnxValueRequest
{
    /// <summary>
    /// The KNX group address (e.g., "1/1/1").
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Optional description for the operation.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Request model for subscribing to a KNX group address.
/// </summary>
public record SubscribeKnxRequest
{
    /// <summary>
    /// The KNX group address (e.g., "1/1/1").
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Optional description for the operation.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Request model for unsubscribing from a KNX group address.
/// </summary>
public record UnsubscribeKnxRequest
{
    /// <summary>
    /// The KNX group address (e.g., "1/1/1").
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Optional description for the operation.
    /// </summary>
    public string? Description { get; init; }
}
