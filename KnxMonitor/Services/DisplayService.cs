using System.Collections.Concurrent;
using KnxMonitor.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace KnxMonitor.Services;

/// <summary>
/// Service for displaying KNX monitor output in a visually attractive manner.
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

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayService"/> class.
    /// </summary>
    /// <param name="config">Monitor configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public DisplayService(KnxMonitorConfig config, ILogger<DisplayService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _cancellationTokenSource?.Cancel();

        if (_displayTask != null)
        {
            try
            {
                await _displayTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _cancellationTokenSource?.Dispose();
    }

    /// <inheritdoc/>
    public void UpdateConnectionStatus(string status, bool isConnected)
    {
        // Status updates are handled in the display update loop
    }

    /// <inheritdoc/>
    public void DisplayMessage(KnxMessage message)
    {
        _messageQueue.Enqueue(message);
        Interlocked.Increment(ref _messageCount);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Handles message received events from the monitor service.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="message">Received message.</param>
    private void OnMessageReceived(object? sender, KnxMessage message)
    {
        DisplayMessage(message);
    }

    /// <summary>
    /// Initializes the display layout.
    /// </summary>
    private void InitializeDisplay()
    {
        // Only create visual components for interactive mode
        if (!ShouldUseLoggingMode())
        {
            // Create status table
            _statusTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .AddColumn(new TableColumn("[bold]Property[/]").Centered())
                .AddColumn(new TableColumn("[bold]Value[/]").Centered());

            // Create messages table
            _messagesTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Green)
                .AddColumn(new TableColumn("[bold]Time[/]").Width(12))
                .AddColumn(new TableColumn("[bold]Type[/]").Width(8))
                .AddColumn(new TableColumn("[bold]Source[/]").Width(10))
                .AddColumn(new TableColumn("[bold]Group Address[/]").Width(15))
                .AddColumn(new TableColumn("[bold]Value[/]").Width(20))
                .AddColumn(new TableColumn("[bold]Data[/]").Width(16))
                .AddColumn(new TableColumn("[bold]Priority[/]").Width(8));

            // Create layout
            _layout = new Layout("Root").SplitRows(new Layout("Header").Size(8), new Layout("Messages"));

            _layout["Header"].Update(_statusTable);
            _layout["Messages"].Update(_messagesTable);
        }
        else
        {
            // For logging mode, just print a simple header
            Console.WriteLine("=== KNX Monitor - Logging Mode ===");
            Console.WriteLine("Format: [Time] [Type] Source -> GroupAddress = Value (Data) [Priority]");
            Console.WriteLine("=====================================");
        }
    }

    /// <summary>
    /// Checks if we should use logging mode instead of visual display.
    /// </summary>
    private static bool ShouldUseLoggingMode()
    {
        // Check for explicit logging mode environment variable
        if (Environment.GetEnvironmentVariable("KNX_MONITOR_LOGGING_MODE") == "true")
            return true;

        // Check for Docker container environment
        return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
            || File.Exists("/.dockerenv")
            || Console.IsOutputRedirected;
    }

    /// <summary>
    /// Updates the display with current information.
    /// </summary>
    /// <param name="monitorService">Monitor service.</param>
    private void UpdateDisplay(IKnxMonitorService monitorService)
    {
        // Only update visual display if we're in an interactive terminal
        if (!ShouldUseLoggingMode())
        {
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
                    AnsiConsole.Write(_layout!);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error rendering display");
                }
            }
        }
        else
        {
            // For container/logging mode, just log new messages
            ProcessNewMessagesForLogging();
        }
    }

    /// <summary>
    /// Processes new messages for simple logging output (non-interactive mode).
    /// </summary>
    private void ProcessNewMessagesForLogging()
    {
        var newMessages = new List<KnxMessage>();
        while (_messageQueue.TryDequeue(out var message) && newMessages.Count < 50)
        {
            newMessages.Add(message);
        }

        foreach (var message in newMessages)
        {
            var typeColor = GetMessageTypeColor(message.MessageType);
            var priorityColor = GetPriorityColor(message.Priority);

            Console.WriteLine(
                $"[{message.Timestamp:HH:mm:ss.fff}] "
                    + $"[{typeColor}]{message.MessageType}[/] "
                    + $"{message.SourceAddress} -> {message.GroupAddress} "
                    + $"= {message.DisplayValue} "
                    + $"({message.DataHex}) "
                    + $"[{priorityColor}]{message.Priority}[/]"
            );
        }
    }

    /// <summary>
    /// Updates the status table with current information.
    /// </summary>
    /// <param name="monitorService">Monitor service.</param>
    private void UpdateStatusTable(IKnxMonitorService monitorService)
    {
        _statusTable!.Rows.Clear();

        var connectionColor = monitorService.IsConnected ? "green" : "red";
        var connectionIcon = monitorService.IsConnected ? "✓" : "✗";

        _statusTable.AddRow("Connection", $"[{connectionColor}]{connectionIcon} {monitorService.ConnectionStatus}[/]");
        _statusTable.AddRow("Type", GetConnectionTypeDisplay());
        _statusTable.AddRow("Gateway", _config.Gateway ?? "N/A");
        _statusTable.AddRow("Port", _config.Port.ToString());
        _statusTable.AddRow("Filter", _config.Filter ?? "None");
        _statusTable.AddRow("Messages", _messageCount.ToString());
        _statusTable.AddRow("Uptime", GetUptime());
    }

    /// <summary>
    /// Updates the messages table with recent messages.
    /// </summary>
    private void UpdateMessagesTable()
    {
        // Process new messages from queue
        var newMessages = new List<KnxMessage>();
        while (_messageQueue.TryDequeue(out var message) && newMessages.Count < 50)
        {
            newMessages.Add(message);
        }

        // Add new messages to table (keep last 20 messages)
        foreach (var message in newMessages.TakeLast(20))
        {
            var timeColor = GetTimeColor(message.Timestamp);
            var typeColor = GetMessageTypeColor(message.MessageType);
            var priorityColor = GetPriorityColor(message.Priority);

            _messagesTable!.AddRow(
                $"[{timeColor}]{message.Timestamp:HH:mm:ss.fff}[/]",
                $"[{typeColor}]{message.MessageType}[/]",
                $"[dim]{message.SourceAddress}[/]",
                $"[bold]{message.GroupAddress}[/]",
                $"[yellow]{message.DisplayValue}[/]",
                $"[dim]{message.DataHex}[/]",
                $"[{priorityColor}]{message.Priority}[/]"
            );
        }

        // Keep only the last 20 rows
        while (_messagesTable!.Rows.Count > 20)
        {
            _messagesTable.Rows.RemoveAt(0);
        }
    }

    /// <summary>
    /// Gets the display text for the connection type.
    /// </summary>
    /// <returns>Connection type display text.</returns>
    private string GetConnectionTypeDisplay()
    {
        return _config.ConnectionType switch
        {
            KnxConnectionType.Tunnel => "[cyan]IP Tunneling[/]",
            KnxConnectionType.Router => "[magenta]IP Routing[/]",
            KnxConnectionType.Usb => "[orange1]USB[/]",
            _ => "[dim]Unknown[/]",
        };
    }

    /// <summary>
    /// Gets the uptime string.
    /// </summary>
    /// <returns>Uptime string.</returns>
    private string GetUptime()
    {
        var uptime = DateTime.Now - _startTime;
        return $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
    }

    /// <summary>
    /// Gets the color for a timestamp based on age.
    /// </summary>
    /// <param name="timestamp">Message timestamp.</param>
    /// <returns>Color name.</returns>
    private static string GetTimeColor(DateTime timestamp)
    {
        var age = DateTime.Now - timestamp;
        return age.TotalSeconds switch
        {
            < 1 => "green",
            < 5 => "yellow",
            < 30 => "orange1",
            _ => "dim",
        };
    }

    /// <summary>
    /// Gets the color for a message type.
    /// </summary>
    /// <param name="messageType">Message type.</param>
    /// <returns>Color name.</returns>
    private static string GetMessageTypeColor(KnxMessageType messageType)
    {
        return messageType switch
        {
            KnxMessageType.Read => "cyan",
            KnxMessageType.Write => "green",
            KnxMessageType.Response => "yellow",
            _ => "white",
        };
    }

    /// <summary>
    /// Gets the color for a message priority.
    /// </summary>
    /// <param name="priority">Message priority.</param>
    /// <returns>Color name.</returns>
    private static string GetPriorityColor(KnxPriority priority)
    {
        return priority switch
        {
            KnxPriority.System => "red",
            KnxPriority.Urgent => "orange1",
            KnxPriority.Normal => "white",
            KnxPriority.Low => "dim",
            _ => "white",
        };
    }
}
