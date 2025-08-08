using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KnxMonitor.Models;

/// <summary>
/// Enterprise-grade data model for Terminal.Gui TableView integration.
/// Provides observable collection with real-time updates and filtering capabilities.
/// </summary>
public class KnxMessageTableModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<KnxMessageRow> _messages = new();
    private readonly object _lock = new();
    private string? _filter;
    private int _maxMessages = 1000; // Configurable message buffer size

    /// <summary>
    /// Initializes a new instance of the <see cref="KnxMessageTableModel"/> class.
    /// </summary>
    public KnxMessageTableModel()
    {
        _messages.CollectionChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Messages)));
    }

    /// <summary>
    /// Gets the observable collection of KNX messages.
    /// </summary>
    public ObservableCollection<KnxMessageRow> Messages => _messages;

    /// <summary>
    /// Gets or sets the current filter pattern.
    /// </summary>
    public string? Filter
    {
        get => _filter;
        set
        {
            if (_filter != value)
            {
                _filter = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Filter)));
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of messages to retain.
    /// </summary>
    public int MaxMessages
    {
        get => _maxMessages;
        set
        {
            if (_maxMessages != value && value > 0)
            {
                _maxMessages = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxMessages)));
                TrimMessages();
            }
        }
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Adds a new KNX message to the table model.
    /// </summary>
    /// <param name="message">The KNX message to add.</param>
    public void AddMessage(KnxMessage message)
    {
        if (message == null)
            return;

        lock (_lock)
        {
            var row = new KnxMessageRow(message);

            // Apply filter if active
            if (!string.IsNullOrEmpty(_filter) && !MatchesFilter(message, _filter))
            {
                return;
            }

            _messages.Insert(0, row); // Add to top for newest-first display
            TrimMessages();
        }
    }

    /// <summary>
    /// Clears all messages from the table model.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
    }

    /// <summary>
    /// Gets all messages as a list for export purposes.
    /// </summary>
    /// <returns>List of all current messages.</returns>
    public List<KnxMessageRow> GetAllMessages()
    {
        lock (_lock)
        {
            return new List<KnxMessageRow>(_messages);
        }
    }

    private void ApplyFilter()
    {
        // Re-populate with filtered messages
        // This is a simplified implementation - in production, you might want to maintain
        // separate filtered and unfiltered collections for better performance
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Messages)));
    }

    private void TrimMessages()
    {
        while (_messages.Count > _maxMessages)
        {
            _messages.RemoveAt(_messages.Count - 1);
        }
    }

    private static bool MatchesFilter(KnxMessage message, string filter)
    {
        if (string.IsNullOrEmpty(filter))
            return true;

        // Support wildcard patterns like "1/2/*" or exact matches
        if (filter.EndsWith("/*"))
        {
            var prefix = filter[..^2];
            return message.GroupAddress.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return message.GroupAddress.Equals(filter, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Enterprise-grade row model for KNX message display in TableView.
/// Provides formatted display values and color coding information.
/// </summary>
public class KnxMessageRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KnxMessageRow"/> class.
    /// </summary>
    /// <param name="message">The source KNX message.</param>
    public KnxMessageRow(KnxMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));

        // Pre-calculate display values for performance
        TimeDisplay = message.Timestamp.ToString("HH:mm:ss.fff");
        TypeDisplay = FormatMessageType(message.MessageType);
        SourceDisplay = message.SourceAddress;
        GroupAddressDisplay = message.GroupAddress;
        ValueDisplay = message.DisplayValue;
        PriorityDisplay = message.Priority.ToString();
        DataDisplay = message.DataHex;

        // Calculate age-based color
        AgeColorCode = CalculateAgeColorCode(message.Timestamp);
        TypeColorCode = CalculateTypeColorCode(message.MessageType);
        PriorityColorCode = CalculatePriorityColorCode(message.Priority);
    }

    /// <summary>
    /// Gets the original KNX message.
    /// </summary>
    public KnxMessage Message { get; }

    /// <summary>
    /// Gets the formatted time display.
    /// </summary>
    public string TimeDisplay { get; }

    /// <summary>
    /// Gets the formatted message type display.
    /// </summary>
    public string TypeDisplay { get; }

    /// <summary>
    /// Gets the formatted source display.
    /// </summary>
    public string SourceDisplay { get; }

    /// <summary>
    /// Gets the formatted group address display.
    /// </summary>
    public string GroupAddressDisplay { get; }

    /// <summary>
    /// Gets the formatted value display.
    /// </summary>
    public string ValueDisplay { get; }

    /// <summary>
    /// Gets the formatted priority display.
    /// </summary>
    public string PriorityDisplay { get; }

    /// <summary>
    /// Gets the formatted raw data display.
    /// </summary>
    public string DataDisplay { get; }

    /// <summary>
    /// Gets the age-based color code for the message.
    /// </summary>
    public string AgeColorCode { get; }

    /// <summary>
    /// Gets the type-based color code for the message.
    /// </summary>
    public string TypeColorCode { get; }

    /// <summary>
    /// Gets the priority-based color code for the message.
    /// </summary>
    public string PriorityColorCode { get; }

    private static string FormatMessageType(KnxMessageType type)
    {
        return type switch
        {
            KnxMessageType.Read => "Read",
            KnxMessageType.Write => "Write",
            KnxMessageType.Response => "Response",
            _ => "Unknown",
        };
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "Empty",
            bool b => b ? "true" : "false",
            byte[] bytes => Convert.ToHexString(bytes),
            _ => value.ToString() ?? "Empty",
        };
    }

    private static string CalculateAgeColorCode(DateTime timestamp)
    {
        var age = DateTime.Now - timestamp;
        return age.TotalSeconds switch
        {
            < 1 => "\u001b[92m", // Bright Green
            < 5 => "\u001b[93m", // Bright Yellow
            < 30 => "\u001b[33m", // Yellow
            < 300 => "\u001b[37m", // White
            _ => "\u001b[90m", // Gray
        };
    }

    private static string CalculateTypeColorCode(KnxMessageType type)
    {
        return type switch
        {
            KnxMessageType.Read => "\u001b[96m", // Bright Cyan
            KnxMessageType.Write => "\u001b[92m", // Bright Green
            KnxMessageType.Response => "\u001b[93m", // Bright Yellow
            _ => "\u001b[37m", // White
        };
    }

    private static string CalculatePriorityColorCode(KnxPriority priority)
    {
        return priority switch
        {
            KnxPriority.System => "\u001b[91m", // Bright Red
            KnxPriority.Urgent => "\u001b[95m", // Bright Magenta
            KnxPriority.Normal => "\u001b[37m", // White
            KnxPriority.Low => "\u001b[90m", // Gray
            _ => "\u001b[37m", // White
        };
    }
}

/// <summary>
/// Enterprise-grade connection status model for real-time display updates.
/// </summary>
public class ConnectionStatusModel : INotifyPropertyChanged
{
    private bool _isConnected;
    private string _connectionType = "Unknown";
    private string _gateway = "Unknown";
    private int _port;
    private string? _filter;
    private int _messageCount;
    private DateTime _startTime = DateTime.Now;
    private string _lastError = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the connection is active.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionStatusDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the connection type display text.
    /// </summary>
    public string ConnectionType
    {
        get => _connectionType;
        set
        {
            if (_connectionType != value)
            {
                _connectionType = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionType)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionStatusDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the gateway address.
    /// </summary>
    public string Gateway
    {
        get => _gateway;
        set
        {
            if (_gateway != value)
            {
                _gateway = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Gateway)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionStatusDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the connection port.
    /// </summary>
    public int Port
    {
        get => _port;
        set
        {
            if (_port != value)
            {
                _port = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Port)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionStatusDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the current filter pattern.
    /// </summary>
    public string? Filter
    {
        get => _filter;
        set
        {
            if (_filter != value)
            {
                _filter = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Filter)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the total message count.
    /// </summary>
    public int MessageCount
    {
        get => _messageCount;
        set
        {
            if (_messageCount != value)
            {
                _messageCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MessageCount)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the application start time.
    /// </summary>
    public DateTime StartTime
    {
        get => _startTime;
        set
        {
            if (_startTime != value)
            {
                _startTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartTime)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UptimeDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string LastError
    {
        get => _lastError;
        set
        {
            if (_lastError != value)
            {
                _lastError = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastError)));
            }
        }
    }

    /// <summary>
    /// Gets the formatted connection status display.
    /// </summary>
    public string ConnectionStatusDisplay =>
        $"{(IsConnected ? "✓ Connected" : "✗ Disconnected")} to {Gateway}:{Port} ({ConnectionType})";

    /// <summary>
    /// Gets the formatted filter display.
    /// </summary>
    public string FilterDisplay => string.IsNullOrEmpty(Filter) ? "None" : Filter;

    /// <summary>
    /// Gets the formatted uptime display.
    /// </summary>
    public string UptimeDisplay
    {
        get
        {
            var uptime = DateTime.Now - StartTime;
            return $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
        }
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}
