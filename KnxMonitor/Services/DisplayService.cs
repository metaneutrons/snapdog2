using System.Collections.Concurrent;
using KnxMonitor.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace KnxMonitor.Services;

/// <summary>
/// Service for displaying KNX monitor output in console logging mode.
/// Used when output is redirected or in containerized environments.
/// </summary>
public class DisplayService : IDisplayService
{
    private readonly KnxMonitorConfig _config;
    private readonly ILogger<DisplayService> _logger;
    private readonly ConcurrentQueue<KnxMessage> _messageQueue = new();
    private readonly object _displayLock = new();

    private Table? _statusTable;
    private Table? _messagesTable;
    private Layout? _layout;
    private bool _isRunning;
    private Task? _displayTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _messageCount;
    private DateTime _startTime;
    private string? _currentFilter;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayService"/> class.
    /// </summary>
    /// <param name="config">Monitor configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public DisplayService(KnxMonitorConfig config, ILogger<DisplayService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentFilter = config.Filter;
    }

    /// <inheritdoc/>
    public bool IsRunning => _isRunning;

    /// <inheritdoc/>
    public string? CurrentFilter => _currentFilter;

    /// <inheritdoc/>
    public int MessageCount => _messageCount;

    /// <inheritdoc/>
    public DateTime StartTime => _startTime;

    /// <inheritdoc/>
    public async Task StartAsync(IKnxMonitorService monitorService, CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            return;
        }

        _startTime = DateTime.Now;
        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();

        // Subscribe to monitor events
        monitorService.MessageReceived += OnMessageReceived;

        // Initialize display
        InitializeDisplay();

        // Start display update task
        _displayTask = Task.Run(
            async () =>
            {
                // Log initial connection status for container mode
                if (ShouldUseLoggingMode())
                {
                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss.fff}] KNX Monitor started - {monitorService.ConnectionStatus}"
                    );
                }

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        UpdateDisplay(monitorService);

                        // Use different update intervals based on output mode
                        var delay = ShouldUseLoggingMode() ? 1000 : 500; // 1s for logs, 500ms for interactive (much less flickering)
                        await Task.Delay(delay, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating display");
                    }
                }
            },
            _cancellationTokenSource.Token
        );

        // CRITICAL FIX: Wait for the background task to complete instead of returning immediately
        // This ensures the StartAsync method doesn't complete until the service is actually stopped
        try
        {
            await _displayTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;

        try
        {
            _cancellationTokenSource?.Cancel();

            if (_displayTask != null)
            {
                await _displayTask;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping display service");
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <inheritdoc/>
    public void UpdateConnectionStatus(string status, bool isConnected)
    {
        // Implementation for logging mode
        if (ShouldUseLoggingMode())
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Connection status: {status} (Connected: {isConnected})");
        }
    }

    /// <inheritdoc/>
    public void DisplayMessage(KnxMessage message)
    {
        _messageQueue.Enqueue(message);
        Interlocked.Increment(ref _messageCount);

        // In logging mode, immediately output the message
        if (ShouldUseLoggingMode())
        {
            var filterMatch = string.IsNullOrEmpty(_currentFilter) || MatchesFilter(message, _currentFilter);
            if (filterMatch)
            {
                Console.WriteLine(
                    $"[{message.Timestamp:HH:mm:ss.fff}] {FormatMessageType(message.MessageType)} "
                        + $"{message.SourceAddress} -> {message.GroupAddress} = {message.DisplayValue} "
                        + $"({message.Priority})"
                );
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }

    private void OnMessageReceived(object? sender, KnxMessage message)
    {
        DisplayMessage(message);
    }

    private void InitializeDisplay()
    {
        if (ShouldUseLoggingMode())
        {
            return; // No visual initialization needed for logging mode
        }

        // Initialize Spectre.Console display for interactive mode
        _statusTable = new Table()
            .AddColumn("Property")
            .AddColumn("Value")
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue);

        _messagesTable = new Table()
            .AddColumn("Time")
            .AddColumn("Type")
            .AddColumn("Source")
            .AddColumn("Group Address")
            .AddColumn("Value")
            .AddColumn("Priority")
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green);

        _layout = new Layout("Root").SplitRows(new Layout("Header").Size(8), new Layout("Messages"));

        _layout["Header"].Update(CreateHeaderPanel());
        _layout["Messages"].Update(_messagesTable);
    }

    private void UpdateDisplay(IKnxMonitorService monitorService)
    {
        if (ShouldUseLoggingMode())
        {
            return; // No visual updates needed for logging mode
        }

        lock (_displayLock)
        {
            try
            {
                // Update status table
                UpdateStatusTable(monitorService);

                // Update messages table
                UpdateMessagesTable();

                // Render the layout
                AnsiConsole.Clear();
                if (_layout != null)
                {
                    AnsiConsole.Write(_layout);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating visual display");
            }
        }
    }

    private void UpdateStatusTable(IKnxMonitorService monitorService)
    {
        if (_statusTable == null)
            return;

        _statusTable.Rows.Clear();

        var connectionStatus = monitorService.IsConnected ? "✓ Connected" : "✗ Disconnected";
        var connectionColor = monitorService.IsConnected ? Color.Green : Color.Red;

        _statusTable.AddRow("Connection", $"[{connectionColor}]{connectionStatus}[/]");
        _statusTable.AddRow("Type", FormatConnectionType(_config.ConnectionType));
        _statusTable.AddRow("Gateway", _config.Gateway ?? "Unknown");
        _statusTable.AddRow("Port", _config.Port.ToString());
        _statusTable.AddRow("Filter", _currentFilter ?? "None");
        _statusTable.AddRow("Messages", _messageCount.ToString());
        _statusTable.AddRow("Uptime", FormatUptime());
    }

    private void UpdateMessagesTable()
    {
        if (_messagesTable == null)
            return;

        _messagesTable.Rows.Clear();

        var messages = new List<KnxMessage>();
        while (_messageQueue.TryDequeue(out var message) && messages.Count < 20)
        {
            if (string.IsNullOrEmpty(_currentFilter) || MatchesFilter(message, _currentFilter))
            {
                messages.Add(message);
            }
        }

        foreach (var message in messages.Take(20).Reverse())
        {
            var ageColor = CalculateAgeColor(message.Timestamp);
            var typeColor = CalculateTypeColor(message.MessageType);

            _messagesTable.AddRow(
                $"[{ageColor}]{message.Timestamp:HH:mm:ss.fff}[/]",
                $"[{typeColor}]{FormatMessageType(message.MessageType)}[/]",
                message.SourceAddress,
                message.GroupAddress,
                message.DisplayValue,
                message.Priority.ToString()
            );
        }
    }

    private Panel CreateHeaderPanel()
    {
        return new Panel(_statusTable ?? new Table()).Header("KNX Monitor").BorderColor(Color.Blue).Padding(1, 0);
    }

    /// <summary>
    /// Determines whether to use logging mode based on output redirection.
    /// </summary>
    /// <returns>True if logging mode should be used, false for interactive mode.</returns>
    private static bool ShouldUseLoggingMode()
    {
        // Check if output is redirected or if we're in a container environment
        return Console.IsOutputRedirected
            || !Console.IsInputRedirected
            || Environment.GetEnvironmentVariable("KNX_MONITOR_LOGGING_MODE") == "true"
            || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
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

    private static string FormatConnectionType(KnxConnectionType connectionType)
    {
        return connectionType switch
        {
            KnxConnectionType.Tunnel => "IP Tunneling",
            KnxConnectionType.Router => "IP Routing",
            KnxConnectionType.Usb => "USB",
            _ => "Unknown",
        };
    }

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

    private string FormatUptime()
    {
        var uptime = DateTime.Now - _startTime;
        return $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
    }

    private static string CalculateAgeColor(DateTime timestamp)
    {
        var age = DateTime.Now - timestamp;
        return age.TotalSeconds switch
        {
            < 1 => "green",
            < 5 => "yellow",
            < 30 => "orange1",
            _ => "white",
        };
    }

    private static string CalculateTypeColor(KnxMessageType type)
    {
        return type switch
        {
            KnxMessageType.Read => "cyan",
            KnxMessageType.Write => "green",
            KnxMessageType.Response => "yellow",
            _ => "white",
        };
    }
}
