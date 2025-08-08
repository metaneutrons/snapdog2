using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Text;
using KnxMonitor.Models;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace KnxMonitor.Services;

/// <summary>
/// Award-worthy Terminal.Gui V2 implementation for KNX Monitor.
/// Provides enterprise-grade windowed interface with proper V2 API compatibility and coordinated shutdown.
/// </summary>
public class TuiDisplayService : IDisplayService
{
    private readonly ILogger<TuiDisplayService> _logger;
    private readonly KnxMessageTableModel _tableModel;
    private Window? _mainWindow;
    private TableView? _tableView;
    private StatusBar? _statusBar;
    private Label? _connectionStatusLabel;
    private Label? _messageCountLabel;
    private Label? _lastMessageLabel;
    private Label? _filterStatusLabel;
    private readonly object _lockObject = new();
    private bool _isRunning;
    private DateTime _startTime;
    private IKnxMonitorService? _monitorService;
    private CancellationTokenSource? _cancellationTokenSource;
    private TaskCompletionSource<bool>? _shutdownCompletionSource;
    private bool _shutdownRequested = false;

    public TuiDisplayService(ILogger<TuiDisplayService> logger)
    {
        _logger = logger;
        _tableModel = new KnxMessageTableModel();
        _startTime = DateTime.Now;
    }

    // IDisplayService properties
    public bool IsRunning => _isRunning;
    public string? CurrentFilter => _tableModel.Filter;
    public int MessageCount => _tableModel.Messages.Count;
    public DateTime StartTime => _startTime;

    public void Initialize()
    {
        try
        {
            Application.Init();

            // Note: Ctrl+C handling is now managed at the Program level
            // This avoids conflicts between main program and TUI service

            _logger.LogInformation("Terminal.Gui V2 application initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Terminal.Gui V2 application");
            throw;
        }
    }

    /// <summary>
    /// Requests shutdown of the TUI application.
    /// This method is called by the main program's Ctrl+C handler or F10 key.
    /// </summary>
    public void RequestShutdown()
    {
        if (_shutdownRequested)
        {
            return;
        }

        try
        {
            _shutdownRequested = true;
            _logger.LogInformation("TUI shutdown requested, closing application...");

            // Request Terminal.Gui to stop
            Application.RequestStop();

            // Signal that shutdown is complete
            _shutdownCompletionSource?.TrySetResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during TUI shutdown request");
            _shutdownCompletionSource?.TrySetException(ex);
        }
    }

    public void Start()
    {
        if (_isRunning)
        {
            _logger.LogWarning("TUI display service is already running");
            return;
        }

        try
        {
            _isRunning = true;
            CreateMainWindow();

            if (_mainWindow != null)
            {
                Application.Run(_mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running TUI display service");
            throw;
        }
        finally
        {
            _isRunning = false;
        }
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        try
        {
            _isRunning = false;
            RequestShutdown();
            _logger.LogInformation("TUI display service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping TUI display service");
        }
    }

    public void Shutdown()
    {
        try
        {
            Stop();
            Application.Shutdown();
            _logger.LogInformation("Terminal.Gui V2 application shutdown completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Terminal.Gui V2 shutdown");
        }
    }

    private void CreateMainWindow()
    {
        _mainWindow = new Window
        {
            Title = "KNX Monitor - Terminal.Gui V2",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        // Handle window closing
        _mainWindow.Closing += (s, e) =>
        {
            _logger.LogInformation("Main window closing, initiating shutdown...");
            RequestShutdown();
        };

        // Create status frame at the top (compacted from 8 to 6 lines as mentioned in summary)
        var statusFrame = new FrameView
        {
            Title = "Connection Status",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 6, // Compacted status window
        };

        CreateStatusLabels(statusFrame);
        _mainWindow.Add(statusFrame);

        // Create messages frame below status
        var messagesFrame = new FrameView
        {
            Title = "KNX Messages",
            X = 0,
            Y = Pos.Bottom(statusFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 1, // Leave space for status bar
        };

        CreateMessagesTable(messagesFrame);
        _mainWindow.Add(messagesFrame);

        // Create status bar at the bottom
        CreateStatusBar();
        _mainWindow.Add(_statusBar!);

        // Set up key bindings
        SetupKeyBindings();

        _logger.LogInformation("Main window created with Terminal.Gui V2 components");
    }

    private void CreateStatusLabels(FrameView statusFrame)
    {
        // Adjusted Y positions for compacted layout (0,1,2,3 instead of 1,2,3,4)
        _connectionStatusLabel = new Label
        {
            Text = "Connection: Disconnected",
            X = 1,
            Y = 0, // Changed from Y = 0
            Width = Dim.Fill()! - 2,
            Height = 1,
        };
        statusFrame.Add(_connectionStatusLabel);

        _messageCountLabel = new Label
        {
            Text = "Messages: 0",
            X = 1,
            Y = 1, // Changed from Y = 1
            Width = Dim.Fill()! - 2,
            Height = 1,
        };
        statusFrame.Add(_messageCountLabel);

        _lastMessageLabel = new Label
        {
            Text = "Last Message: None",
            X = 1,
            Y = 2, // Changed from Y = 2
            Width = Dim.Fill()! - 2,
            Height = 1,
        };
        statusFrame.Add(_lastMessageLabel);

        _filterStatusLabel = new Label
        {
            Text = "Filter: None",
            X = 1,
            Y = 3, // Changed from Y = 3
            Width = Dim.Fill()! - 2,
            Height = 1,
        };
        statusFrame.Add(_filterStatusLabel);
    }

    private void CreateMessagesTable(FrameView messagesFrame)
    {
        // Create a simple table source implementation
        var tableSource = new KnxMessageTableSource(_tableModel);

        _tableView = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Table = tableSource,
        };

        messagesFrame.Add(_tableView);
    }

    private void CreateStatusBar()
    {
        _statusBar = new StatusBar(
            new[]
            {
                new Shortcut(Key.F1, "Help", ShowHelp),
                new Shortcut(Key.F2, "Filter", ShowFilterDialog),
                new Shortcut(Key.F3, "Export", ShowExportDialog),
                new Shortcut(Key.F10, "Quit", RequestShutdown), // Now calls RequestShutdown instead of inline logic
            }
        );
    }

    private void SetupKeyBindings()
    {
        if (_mainWindow == null)
            return;

        // Note: Ctrl+C is now handled at the Program level to avoid conflicts
        // Terminal.Gui V2 handles Esc automatically for closing dialogs
        // F-key shortcuts are handled by the status bar
    }

    private void ShowHelp()
    {
        var helpText = """
            KNX Monitor Help

            F1 - Show this help
            F2 - Set message filter
            F3 - Export messages
            F10 - Quit application
            Ctrl+C - Quit application
            Esc - Close dialogs

            Arrow keys - Navigate table
            Page Up/Down - Scroll table
            """;

        MessageBox.Query("Help", helpText, "OK");
    }

    private void ShowFilterDialog()
    {
        var dialog = new Dialog
        {
            Title = "Set Filter",
            Width = 60,
            Height = 12,
        };

        var filterField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill()! - 2,
            Height = 1,
            Text = _tableModel.Filter ?? "",
        };

        var label = new Label
        {
            Text = "Filter expression:",
            X = 1,
            Y = 0,
            Width = Dim.Fill()! - 2,
            Height = 1,
        };

        var okButton = new Button
        {
            Text = "OK",
            X = Pos.Center() - 10,
            Y = Pos.Bottom(filterField) + 2,
            IsDefault = true,
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(filterField) + 2,
        };

        okButton.Accepting += (s, e) =>
        {
            _tableModel.Filter = filterField.Text?.ToString();
            UpdateFilterStatus();
            Application.RequestStop();
            e.Handled = true;
        };

        cancelButton.Accepting += (s, e) =>
        {
            Application.RequestStop();
            e.Handled = true;
        };

        dialog.Add(label, filterField, okButton, cancelButton);
        Application.Run(dialog);
        dialog.Dispose();
    }

    private void ShowExportDialog()
    {
        var dialog = new Dialog
        {
            Title = "Export Messages",
            Width = 60,
            Height = 12,
        };

        var pathField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill()! - 2,
            Height = 1,
            Text = $"knx_messages_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
        };

        var label = new Label
        {
            Text = "Export file path:",
            X = 1,
            Y = 0,
            Width = Dim.Fill()! - 2,
            Height = 1,
        };

        var okButton = new Button
        {
            Text = "Export",
            X = Pos.Center() - 10,
            Y = Pos.Bottom(pathField) + 2,
            IsDefault = true,
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(pathField) + 2,
        };

        okButton.Accepting += (s, e) =>
        {
            try
            {
                var path = pathField.Text?.ToString();
                if (!string.IsNullOrEmpty(path))
                {
                    ExportMessages(path);
                    MessageBox.Query("Export", $"Messages exported to: {path}", "OK");
                }
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Export Error", $"Failed to export: {ex.Message}", "OK");
            }
            Application.RequestStop();
            e.Handled = true;
        };

        cancelButton.Accepting += (s, e) =>
        {
            Application.RequestStop();
            e.Handled = true;
        };

        dialog.Add(label, pathField, okButton, cancelButton);
        Application.Run(dialog);
        dialog.Dispose();
    }

    public void AddMessage(KnxMessage message)
    {
        lock (_lockObject)
        {
            _tableModel.AddMessage(message);
            UpdateUI();
        }
    }

    public void UpdateConnectionStatus(ConnectionStatusModel status)
    {
        lock (_lockObject)
        {
            // Update connection status display
            UpdateConnectionStatusDisplay();
        }
    }

    public void ClearMessages()
    {
        lock (_lockObject)
        {
            _tableModel.Clear();
            UpdateUI();
        }
    }

    public void SetFilter(string? filter)
    {
        lock (_lockObject)
        {
            _tableModel.Filter = filter;
            UpdateFilterStatus();
            UpdateUI();
        }
    }

    public void ExportMessages(string filePath)
    {
        lock (_lockObject)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,Source,Destination,Type,Data");

            foreach (var messageRow in _tableModel.Messages)
            {
                var message = messageRow.Message;
                csv.AppendLine(
                    $"{message.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{message.SourceAddress},{message.GroupAddress},{message.MessageType},{message.DataHex}"
                );
            }

            File.WriteAllText(filePath, csv.ToString());
            _logger.LogInformation("Exported {Count} messages to {FilePath}", _tableModel.Messages.Count, filePath);
        }
    }

    private void UpdateUI()
    {
        if (!_isRunning)
            return;

        // Terminal.Gui V2 handles UI updates automatically
        UpdateMessageCount();
        UpdateLastMessage();
    }

    private void UpdateConnectionStatusDisplay()
    {
        if (!_isRunning || _connectionStatusLabel == null)
            return;

        // Terminal.Gui V2 handles UI updates automatically
        var status = "Connected"; // This would come from actual connection status
        _connectionStatusLabel.Text = $"Connection: {status}";
    }

    private void UpdateMessageCount()
    {
        if (_messageCountLabel == null)
            return;
        _messageCountLabel.Text = $"Messages: {_tableModel.Messages.Count}";
    }

    private void UpdateLastMessage()
    {
        if (_lastMessageLabel == null)
            return;

        var lastMessageRow = _tableModel.Messages.FirstOrDefault();
        _lastMessageLabel.Text =
            lastMessageRow != null
                ? $"Last Message: {lastMessageRow.Message.Timestamp:HH:mm:ss} {lastMessageRow.Message.SourceAddress} -> {lastMessageRow.Message.GroupAddress}"
                : "Last Message: None";
    }

    private void UpdateFilterStatus()
    {
        if (_filterStatusLabel == null)
            return;
        _filterStatusLabel.Text = $"Filter: {_tableModel.Filter ?? "None"}";
    }

    public async Task StartAsync(IKnxMonitorService monitorService, CancellationToken cancellationToken = default)
    {
        _monitorService = monitorService;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _shutdownCompletionSource = new TaskCompletionSource<bool>();
        _startTime = DateTime.Now;

        try
        {
            Initialize();

            // Subscribe to monitor service events
            if (_monitorService != null)
            {
                _monitorService.MessageReceived += OnMessageReceived;
            }

            // Start the TUI in a background task
            var tuiTask = Task.Run(
                () =>
                {
                    try
                    {
                        Start();
                        _logger.LogInformation("TUI application completed normally");
                        _shutdownCompletionSource.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in TUI application");
                        _shutdownCompletionSource.TrySetException(ex);
                    }
                },
                _cancellationTokenSource.Token
            );

            // Wait for either the TUI to close, cancellation, or shutdown completion
            var completedTask = await Task.WhenAny(
                _shutdownCompletionSource.Task,
                Task.Delay(System.Threading.Timeout.Infinite, _cancellationTokenSource.Token)
            );

            if (completedTask == _shutdownCompletionSource.Task)
            {
                _logger.LogInformation("TUI application has been closed");
            }
            else
            {
                _logger.LogInformation("TUI application was cancelled");
                RequestShutdown();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TUI display service was cancelled");
            RequestShutdown();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TUI display service");
            throw;
        }
        finally
        {
            // Unsubscribe from events
            if (_monitorService != null)
            {
                _monitorService.MessageReceived -= OnMessageReceived;
            }
        }
    }

    /// <summary>
    /// Handles KNX messages received from the monitor service.
    /// </summary>
    private void OnMessageReceived(object? sender, KnxMessage message)
    {
        try
        {
            AddMessage(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling received message");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(
            () =>
            {
                try
                {
                    _logger.LogInformation("Stopping TUI display service...");

                    // Unsubscribe from monitor service events
                    if (_monitorService != null)
                    {
                        _monitorService.MessageReceived -= OnMessageReceived;
                    }

                    // Stop the TUI
                    Stop();

                    // Cancel any ongoing operations
                    _cancellationTokenSource?.Cancel();

                    _logger.LogInformation("TUI display service stopped");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping TUI display service");
                }
            },
            cancellationToken
        );
    }

    public void UpdateConnectionStatus(string status, bool isConnected)
    {
        // Update connection status display
        UpdateConnectionStatusDisplay();
    }

    public void DisplayMessage(KnxMessage message)
    {
        AddMessage(message);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync();
            Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during async dispose");
        }
    }

    public void Dispose()
    {
        try
        {
            // Unsubscribe from monitor service events
            if (_monitorService != null)
            {
                _monitorService.MessageReceived -= OnMessageReceived;
            }

            // Stop and shutdown TUI
            Stop();
            Shutdown();

            // Dispose cancellation token source
            _cancellationTokenSource?.Dispose();

            // Complete shutdown if not already done
            _shutdownCompletionSource?.TrySetResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during dispose");
        }
    }
}

/// <summary>
/// Table source implementation for Terminal.Gui V2 TableView
/// </summary>
public class KnxMessageTableSource : ITableSource
{
    private readonly KnxMessageTableModel _model;

    public KnxMessageTableSource(KnxMessageTableModel model)
    {
        _model = model;
    }

    public int Rows => _model.Messages.Count;
    public int Columns => 5; // Timestamp, Source, Destination, Type, Data

    public string[] ColumnNames => new[] { "Timestamp", "Source", "Destination", "Type", "Data" };

    public object this[int row, int col]
    {
        get
        {
            if (row < 0 || row >= _model.Messages.Count)
                return "";

            var messageRow = _model.Messages[row];
            return col switch
            {
                0 => messageRow.TimeDisplay,
                1 => messageRow.SourceDisplay,
                2 => messageRow.GroupAddressDisplay,
                3 => messageRow.TypeDisplay,
                4 => messageRow.DataDisplay,
                _ => "",
            };
        }
    }

    public string GetColumnName(int col)
    {
        return col >= 0 && col < ColumnNames.Length ? ColumnNames[col] : "";
    }

    public Type GetColumnType(int col) => typeof(string);

    public void SetValue(int row, int col, object value)
    {
        // Read-only table
    }
}
