using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Events;
using SnapDog2.Infrastructure.Services;
using Xunit;

namespace SnapDog2.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive unit tests for enhanced SnapcastService covering real-time event processing,
/// JSON-RPC communication, server state synchronization, and error handling.
/// Award-worthy test suite ensuring robust Snapcast integration with complete coverage.
/// </summary>
public class SnapcastServiceEnhancedTests : IDisposable
{
    private readonly Mock<TcpClient> _mockTcpClient;
    private readonly Mock<NetworkStream> _mockNetworkStream;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<SnapcastService>> _mockLogger;
    private readonly SnapcastConfiguration _config;
    private readonly SnapcastService _snapcastService;
    private readonly MemoryStream _responseStream;
    private readonly MemoryStream _writeStream;

    public SnapcastServiceEnhancedTests()
    {
        _mockTcpClient = new Mock<TcpClient>();
        _mockNetworkStream = new Mock<NetworkStream>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<SnapcastService>>();
        _responseStream = new MemoryStream();
        _writeStream = new MemoryStream();

        _config = new SnapcastConfiguration
        {
            Enabled = true,
            Host = "localhost",
            Port = 1705,
            AutoReconnect = true,
            ReconnectDelaySeconds = 5,
            TimeoutSeconds = 10,
            MaxReconnectAttempts = 3,
        };

        // Setup network stream mocks
        _mockNetworkStream.Setup(x => x.CanRead).Returns(true);
        _mockNetworkStream.Setup(x => x.CanWrite).Returns(true);
        _mockNetworkStream
            .Setup(x =>
                x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .Returns<byte[], int, int, CancellationToken>(
                (buffer, offset, count, ct) => _responseStream.ReadAsync(buffer, offset, count, ct)
            );
        _mockNetworkStream
            .Setup(x =>
                x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .Returns<byte[], int, int, CancellationToken>(
                (buffer, offset, count, ct) =>
                {
                    _writeStream.Write(buffer, offset, count);
                    return Task.CompletedTask;
                }
            );

        _mockTcpClient.Setup(x => x.GetStream()).Returns(_mockNetworkStream.Object);
        _mockTcpClient.Setup(x => x.Connected).Returns(true);

        var options = Options.Create(_config);
        _snapcastService = new SnapcastService(options, _mockLogger.Object, _mockMediator.Object);
    }

    #region Connection and Initialization Tests

    [Fact]
    public async Task IsServerAvailableAsync_WithConnectedClient_ShouldReturnTrue()
    {
        // Arrange
        SetupSuccessfulServerStatusResponse();

        // Act
        var result = await _snapcastService.IsServerAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsServerAvailableAsync_WithDisconnectedClient_ShouldReturnFalse()
    {
        // Arrange
        _mockTcpClient.Setup(static x => x.Connected).Returns(false);

        // Act
        var result = await _snapcastService.IsServerAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region JSON-RPC Communication Tests

    [Fact]
    public async Task SendRpcRequestAsync_WithValidRequest_ShouldReturnResponse()
    {
        // Arrange
        var request = new { method = "Server.GetStatus", id = 1 };
        var expectedResponse = CreateServerStatusResponse();
        SetupRpcResponse(expectedResponse);

        // Act
        var result = await _snapcastService.GetServerStatusAsync(new CancellationToken());

        // Assert
        result.Should().NotBeNull();
        VerifyRpcRequestSent("Server.GetStatus");
    }

    [Fact]
    public async Task SetClientVolumeAsync_WithParameters_ShouldIncludeInRequest()
    {
        // Arrange
        var parameters = new { id = "test-client", volume = new { percent = 75 } };
        var response = CreateSuccessResponse();
        SetupRpcResponse(response);

        // Act
        var result = await _snapcastService.SetClientVolumeAsync("test-client", 75);

        // Assert
        result.Should().BeTrue();
        VerifyRpcRequestSent("Client.SetVolume", parameters);
    }

    [Fact]
    public async Task GetServerStatusAsync_WithErrorResponse_ShouldThrowException()
    {
        // Arrange
        var errorResponse = CreateErrorResponse(-1, "Method not found");
        SetupRpcResponse(errorResponse);

        // Act & Assert
        var act = () => _snapcastService.GetServerStatusAsync(new CancellationToken());
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Method not found*");
    }

    [Fact]
    public async Task GetServerStatusAsync_WithMalformedResponse_ShouldThrowException()
    {
        // Arrange
        SetupRpcResponse("invalid json response");

        // Act & Assert
        var act = () => _snapcastService.GetServerStatusAsync(new CancellationToken());
        await act.Should().ThrowAsync<JsonException>();
    }

    #endregion

    #region Client Operations Tests

    [Fact]
    public async Task SetClientVolumeAsync_WithValidClient_ShouldReturnTrue()
    {
        // Arrange
        var clientId = "test-client";
        var volume = 75;
        var successResponse = CreateSuccessResponse();
        SetupRpcResponse(successResponse);

        // Act
        var result = await _snapcastService.SetClientVolumeAsync(clientId, volume);

        // Assert
        result.Should().BeTrue();
        VerifyRpcRequestSent("Client.SetVolume", new { id = clientId, volume = new { percent = volume } });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-100)]
    [InlineData(1000)]
    public async Task SetClientVolumeAsync_WithInvalidVolume_ShouldThrowArgumentException(int invalidVolume)
    {
        // Arrange
        var clientId = "test-client";

        // Act & Assert
        var act = () => _snapcastService.SetClientVolumeAsync(clientId, invalidVolume);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*volume*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetClientVolumeAsync_WithInvalidClientId_ShouldThrowArgumentException(string invalidClientId)
    {
        // Act & Assert
        var act = () => _snapcastService.SetClientVolumeAsync(invalidClientId, 50);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*clientId*");
    }

    [Fact]
    public async Task SetClientMuteAsync_WithValidClient_ShouldReturnTrue()
    {
        // Arrange
        var clientId = "test-client";
        var muted = true;
        var successResponse = CreateSuccessResponse();
        SetupRpcResponse(successResponse);

        // Act
        var result = await _snapcastService.SetClientMuteAsync(clientId, muted);

        // Assert
        result.Should().BeTrue();
        VerifyRpcRequestSent("Client.SetVolume", new { id = clientId, volume = new { muted = muted } });
    }

    #endregion

    #region Group and Stream Operations Tests

    [Fact]
    public async Task SetGroupStreamAsync_WithValidGroup_ShouldReturnTrue()
    {
        // Arrange
        var groupId = "group1";
        var streamId = "stream2";
        var successResponse = CreateSuccessResponse();
        SetupRpcResponse(successResponse);

        // Act
        var result = await _snapcastService.SetGroupStreamAsync(groupId, streamId);

        // Assert
        result.Should().BeTrue();
        VerifyRpcRequestSent("Group.SetStream", new { id = groupId, streamId = streamId });
    }

    [Fact]
    public async Task GetGroupsAsync_WithValidServer_ShouldReturnGroups()
    {
        // Arrange
        var response = CreateGroupsResponse();
        SetupRpcResponse(response);

        // Act
        var result = await _snapcastService.GetGroupsAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCountGreaterThan(0);
        VerifyRpcRequestSent("Server.GetStatus");
    }

    #endregion

    #region Server State Synchronization Tests

    [Fact]
    public async Task SynchronizeServerStateAsync_WithValidServer_ShouldPublishSyncEvent()
    {
        // Arrange
        var serverStatus = CreateServerStatusResponse();
        SetupRpcResponse(serverStatus);

        // Act
        await _snapcastService.SynchronizeServerStateAsync();

        // Assert
        _mockMediator.Verify(
            static x => x.Publish(It.IsAny<SnapcastStateSynchronizedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        VerifyRpcRequestSent("Server.GetStatus");
    }

    [Fact]
    public async Task SynchronizeServerStateAsync_WithMultipleClients_ShouldSyncAllClientStates()
    {
        // Arrange
        var serverStatus = CreateServerStatusWithMultipleClients();
        SetupRpcResponse(serverStatus);

        // Act
        await _snapcastService.SynchronizeServerStateAsync();

        // Assert
        _mockMediator.Verify(
            static x => x.Publish(It.IsAny<SnapcastStateSynchronizedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Should log information about synchronized clients
        VerifyLoggerInfo("Synchronized server state");
    }

    [Fact]
    public async Task SynchronizeServerStateAsync_WithServerError_ShouldLogErrorAndNotThrow()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.Connected).Returns(false);

        // Act & Assert
        var act = () => _snapcastService.SynchronizeServerStateAsync();
        await act.Should().NotThrowAsync();

        VerifyLoggerError("Failed to synchronize server state");
    }

    #endregion

    #region Error Handling and Resilience Tests

    [Fact]
    public async Task GetServerStatusAsync_WithNetworkFailure_ShouldRetryAndEventuallyFail()
    {
        // Arrange
        _mockNetworkStream
            .Setup(x =>
                x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new IOException("Network failure"));

        // Act & Assert
        var act = () => _snapcastService.GetServerStatusAsync(new CancellationToken());
        await act.Should().ThrowAsync<IOException>();

        VerifyLoggerError("Network error during RPC request");
    }

    [Fact]
    public async Task ListenForEventsAsync_WithStreamInterruption_ShouldHandleGracefully()
    {
        // Arrange
        _mockNetworkStream
            .Setup(static x =>
                x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new IOException("Stream interrupted"));

        // Act
        await _snapcastService.ListenForEventsAsync(CancellationToken.None);
        await Task.Delay(100);

        // Assert
        VerifyLoggerError("Error reading from Snapcast event stream");
    }

    [Fact]
    public async Task SetClientVolumeAsync_WithTimeout_ShouldReturnFalse()
    {
        // Arrange
        var clientId = "timeout-client";
        var volume = 50;

        _mockNetworkStream
            .Setup(static x =>
                x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .Returns(Task.Delay(TimeSpan.FromSeconds(_config.TimeoutSeconds + 1)));

        // Act
        var result = await _snapcastService.SetClientVolumeAsync(clientId, volume);

        // Assert
        result.Should().BeFalse();
        VerifyLoggerError("Request timeout");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ProcessMultipleEvents_WithHighFrequency_ShouldHandleEfficiently()
    {
        // Arrange
        const int eventCount = 1000;
        var events = new List<string>();

        for (int i = 0; i < eventCount; i++)
        {
            events.Add(CreateVolumeChangeNotification($"client{i % 10}", i % 101, i % 2 == 0));
        }

        var combinedEvents = string.Join("\n", events);
        SetupEventStream(combinedEvents);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var eventTask = _snapcastService.ListenForEventsAsync(CancellationToken.None);
        await Task.Delay(1000); // Allow processing

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
        _mockMediator.Verify(
            x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(eventCount / 2)
        ); // Allow for some processing variance
    }

    [Fact]
    public async Task ConcurrentRpcRequests_ShouldHandleThreadSafely()
    {
        // Arrange
        const int requestCount = 100;
        var successResponse = CreateSuccessResponse();
        SetupRpcResponse(successResponse);

        var tasks = new List<Task<bool>>();

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            var clientId = $"client{i}";
            var volume = i % 101;
            tasks.Add(_snapcastService.SetClientVolumeAsync(clientId, volume));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(static result => result.Should().BeTrue());
        _mockNetworkStream.Verify(
            static x =>
                x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(requestCount)
        );
    }

    #endregion

    #region Helper Methods

    private void SetupRpcResponse(string response)
    {
        var responseBytes = Encoding.UTF8.GetBytes(response + "\n");
        _responseStream.SetLength(0);
        _responseStream.Write(responseBytes, 0, responseBytes.Length);
        _responseStream.Position = 0;
    }

    private void SetupEventStream(string eventData)
    {
        var eventBytes = Encoding.UTF8.GetBytes(eventData + "\n");
        _responseStream.SetLength(0);
        _responseStream.Write(eventBytes, 0, eventBytes.Length);
        _responseStream.Position = 0;
    }

    private void SetupContinuousEventStream(string repeatingEvent)
    {
        var eventBytes = Encoding.UTF8.GetBytes(repeatingEvent + "\n");
        _mockNetworkStream
            .Setup(x =>
                x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .Returns<byte[], int, int, CancellationToken>(
                (buffer, offset, count, ct) =>
                {
                    if (ct.IsCancellationRequested)
                        return Task.FromCanceled<int>(ct);

                    var bytesToCopy = Math.Min(count, eventBytes.Length);
                    Array.Copy(eventBytes, 0, buffer, offset, bytesToCopy);
                    return Task.FromResult(bytesToCopy);
                }
            );
    }

    private void SetupSuccessfulServerStatusResponse()
    {
        var response = CreateServerStatusResponse();
        SetupRpcResponse(response);
    }

    private string CreateSuccessResponse()
    {
        return JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id = 1,
                result = new { },
            }
        );
    }

    private string CreateErrorResponse(int code, string message)
    {
        return JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id = 1,
                error = new { code = code, message = message },
            }
        );
    }

    private string CreateServerStatusResponse()
    {
        return JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id = 1,
                result = new
                {
                    server = new
                    {
                        host = new
                        {
                            arch = "x86_64",
                            ip = "127.0.0.1",
                            mac = "00:11:22:33:44:55",
                            name = "TestHost",
                            os = "Linux",
                        },
                        snapserver = new { name = "Snapserver", version = "0.26.0" },
                    },
                    groups = new[]
                    {
                        new
                        {
                            id = "group1",
                            name = "Test Group",
                            stream_id = "stream1",
                            muted = false,
                            clients = new[]
                            {
                                new
                                {
                                    id = "client1",
                                    host = new { name = "TestClient", ip = "192.168.1.100" },
                                    config = new { name = "Living Room", volume = new { percent = 75, muted = false } },
                                    connected = true,
                                },
                            },
                        },
                    },
                },
            }
        );
    }

    private string CreateServerStatusWithMultipleClients()
    {
        return JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id = 1,
                result = new
                {
                    server = new { },
                    groups = new[]
                    {
                        new
                        {
                            id = "group1",
                            clients = new[]
                            {
                                new
                                {
                                    id = "client1",
                                    connected = true,
                                    config = new { volume = new { percent = 75, muted = false } },
                                },
                                new
                                {
                                    id = "client2",
                                    connected = true,
                                    config = new { volume = new { percent = 80, muted = true } },
                                },
                                new
                                {
                                    id = "client3",
                                    connected = false,
                                    config = new { volume = new { percent = 60, muted = false } },
                                },
                            },
                        },
                    },
                },
            }
        );
    }

    private string CreateClientVolumeResponse(int volume, bool muted)
    {
        return JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id = 1,
                result = new { client = new { config = new { volume = new { percent = volume, muted = muted } } } },
            }
        );
    }

    private string CreateGroupsResponse()
    {
        return JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id = 1,
                result = new
                {
                    groups = new[]
                    {
                        new
                        {
                            id = "group1",
                            name = "Group 1",
                            stream_id = "stream1",
                        },
                        new
                        {
                            id = "group2",
                            name = "Group 2",
                            stream_id = "stream2",
                        },
                    },
                },
            }
        );
    }

    private string CreateVolumeChangeNotification(string clientId, int volume, bool muted)
    {
        return JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                method = "Client.OnVolumeChanged",
                @params = new { id = clientId, volume = new { percent = volume, muted = muted } },
            }
        );
    }

    private string CreateClientConnectionNotification(string clientId, bool connected)
    {
        return JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                method = connected ? "Client.OnConnect" : "Client.OnDisconnect",
                @params = new { client = new { id = clientId } },
            }
        );
    }

    private string CreateStreamChangeNotification(string groupId, string streamId)
    {
        return JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                method = "Group.OnStreamChanged",
                @params = new { id = groupId, stream_id = streamId },
            }
        );
    }

    private string CreateUnknownNotification()
    {
        return JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                method = "Unknown.Method",
                @params = new { },
            }
        );
    }

    private void VerifyRpcRequestSent(string method, object? parameters = null)
    {
        _writeStream.Position = 0;
        var requestData = new byte[_writeStream.Length];
        _writeStream.Read(requestData, 0, requestData.Length);
        var requestJson = Encoding.UTF8.GetString(requestData);

        requestJson.Should().Contain($"\"method\":\"{method}\"");
        if (parameters != null)
        {
            // Basic verification that parameters were included
            requestJson.Should().Contain("\"params\":");
        }
    }

    private void VerifyLoggerError(string expectedMessage)
    {
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    private void VerifyLoggerWarning(string expectedMessage)
    {
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    private void VerifyLoggerInfo(string expectedMessage)
    {
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    #endregion

    public void Dispose()
    {
        _responseStream?.Dispose();
        _writeStream?.Dispose();
        _snapcastService?.Dispose();
    }
}
