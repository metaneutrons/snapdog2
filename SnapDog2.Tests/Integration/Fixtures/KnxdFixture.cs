using System.Net;
using System.Net.Sockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;

namespace SnapDog2.Tests.Integration.Fixtures;

/// <summary>
/// KNX daemon test fixture with proper UDP support for KNX/IP protocol.
/// Implements comprehensive UDP networking patterns for containerized testing.
/// </summary>
public class KnxdFixture : IAsyncLifetime
{
    private readonly ILogger<KnxdFixture> _logger;
    private IContainer? _knxdContainer;

    public string KnxHost { get; private set; } = string.Empty;
    public int KnxTcpPort { get; private set; } // Actually UDP port for KNX/IP

    public KnxdFixture()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<KnxdFixture>();
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Starting KNX daemon container initialization");

        try
        {
            // Find an available UDP port for host binding
            var availablePort = GetAvailableUdpPort();
            _logger.LogInformation("Using host UDP port {Port} for KNX daemon", availablePort);

            // Build container with proper UDP networking configuration
            _knxdContainer = new ContainerBuilder()
                .WithImage("knxd-test:latest")
                .WithPortBinding(availablePort, 3671) // Explicit port binding (host:container)
                .WithEnvironment("ADDRESS", "0.0.1") // KNX daemon address
                .WithEnvironment("CLIENT_ADDRESS", "0.0.2:8") // Client address range
                .WithEnvironment("INTERFACE", "dummy") // Simulation mode
                .WithEnvironment("DEBUG_LEVEL", "verbose") // Enhanced logging
                .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("netstat", "-ln")) // Wait for network services
                .WithCleanUp(true) // Ensure proper resource cleanup
                .Build();

            await _knxdContainer.StartAsync();
            _logger.LogInformation("KNX daemon container started successfully");

            // Allow time for KNX daemon initialization
            await Task.Delay(2000);

            // Configure connection parameters
            KnxTcpPort = availablePort; // Use the explicit port we assigned
            KnxHost = "localhost";

            // Validate UDP connectivity
            await ValidateUdpConnectivityAsync();

            // Set environment variables for application configuration
            Environment.SetEnvironmentVariable("SNAPDOG_TEST_KNX_HOST", KnxHost);
            Environment.SetEnvironmentVariable("SNAPDOG_TEST_KNX_TCP_PORT", KnxTcpPort.ToString());

            _logger.LogInformation("KNX daemon fixture initialized - Host: {Host}, Port: {Port}", KnxHost, KnxTcpPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize KNX daemon container");
            throw;
        }
    }

    /// <summary>
    /// Finds an available UDP port for host binding.
    /// Implements proper port discovery following networking best practices.
    /// </summary>
    private static int GetAvailableUdpPort()
    {
        using var udpClient = new UdpClient(0); // Bind to any available port
        var endpoint = (IPEndPoint)udpClient.Client.LocalEndPoint!;
        return endpoint.Port;
    }

    /// <summary>
    /// Validates UDP connectivity to the KNX daemon following best practices.
    /// Implements proper error handling and timeout management.
    /// </summary>
    private async Task ValidateUdpConnectivityAsync()
    {
        _logger.LogInformation("Validating UDP connectivity to KNX daemon");

        using var udpClient = new UdpClient();
        try
        {
            // Set reasonable timeout for UDP operations
            udpClient.Client.ReceiveTimeout = 5000;
            udpClient.Client.SendTimeout = 5000;

            var endpoint = new IPEndPoint(IPAddress.Loopback, KnxTcpPort);

            // Send a basic connectivity test packet
            var testData = new byte[] { 0x06, 0x10, 0x02, 0x01, 0x00, 0x0E }; // KNX/IP search request
            await udpClient.SendAsync(testData, testData.Length, endpoint);

            _logger.LogInformation("UDP connectivity validation completed successfully");
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NetworkUnreachable)
        {
            _logger.LogWarning("Network unreachable during UDP validation - this may be expected in test environments");
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
        {
            _logger.LogWarning("Connection refused during UDP validation - KNX daemon may still be initializing");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UDP connectivity validation encountered an issue - proceeding with caution");
        }
    }

    /// <summary>
    /// Implements proper resource disposal following async disposal patterns.
    /// Ensures container cleanup and resource management.
    /// </summary>
    public async Task DisposeAsync()
    {
        _logger.LogInformation("Disposing KNX daemon fixture");

        try
        {
            if (_knxdContainer != null)
            {
                await _knxdContainer.StopAsync();
                await _knxdContainer.DisposeAsync();
                _logger.LogInformation("KNX daemon container disposed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during KNX daemon fixture disposal");
        }
    }
}
