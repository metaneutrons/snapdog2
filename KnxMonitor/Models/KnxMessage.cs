namespace KnxMonitor.Models;

/// <summary>
/// Represents a KNX bus message for display purposes.
/// </summary>
public class KnxMessage
{
    /// <summary>
    /// Gets or sets the timestamp when the message was received.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the source address of the message.
    /// </summary>
    public string SourceAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination group address.
    /// </summary>
    public string GroupAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type (Read, Write, Response).
    /// </summary>
    public KnxMessageType MessageType { get; set; }

    /// <summary>
    /// Gets or sets the raw data payload.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the interpreted value (if available).
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the data point type (if known).
    /// </summary>
    public string? DataPointType { get; set; }

    /// <summary>
    /// Gets or sets the message priority.
    /// </summary>
    public KnxPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this message was repeated.
    /// </summary>
    public bool IsRepeated { get; set; }

    /// <summary>
    /// Gets the data as a hexadecimal string.
    /// </summary>
    public string DataHex => Convert.ToHexString(Data);

    /// <summary>
    /// Gets a formatted display value.
    /// </summary>
    public string DisplayValue
    {
        get
        {
            if (Value != null)
            {
                return Value.ToString() ?? DataHex;
            }

            if (Data.Length == 0)
            {
                return "Empty";
            }

            if (Data.Length == 1)
            {
                return $"{Data[0]} (0x{Data[0]:X2})";
            }

            return DataHex;
        }
    }
}

/// <summary>
/// KNX message types.
/// </summary>
public enum KnxMessageType
{
    /// <summary>
    /// Read request.
    /// </summary>
    Read,

    /// <summary>
    /// Write request.
    /// </summary>
    Write,

    /// <summary>
    /// Response to read request.
    /// </summary>
    Response,
}

/// <summary>
/// KNX message priorities.
/// </summary>
public enum KnxPriority
{
    /// <summary>
    /// System priority.
    /// </summary>
    System,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal,

    /// <summary>
    /// Urgent priority.
    /// </summary>
    Urgent,

    /// <summary>
    /// Low priority.
    /// </summary>
    Low,
}
