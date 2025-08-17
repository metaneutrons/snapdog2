namespace SnapDog2.Tests.Unit.Handlers.Clients;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Clients.Handlers;
using Xunit;

/// <summary>
/// Unit tests for client volume command handlers.
/// Tests the new ClientVolumeUpCommandHandler and ClientVolumeDownCommandHandler.
/// </summary>
public class ClientVolumeCommandHandlerTests
{
    private readonly Mock<IClientManager> _mockClientManager;
    private readonly Mock<IClient> _mockClient;
    private readonly Mock<ILogger<ClientVolumeUpCommandHandler>> _mockLoggerUp;
    private readonly Mock<ILogger<ClientVolumeDownCommandHandler>> _mockLoggerDown;

    public ClientVolumeCommandHandlerTests()
    {
        _mockClientManager = new Mock<IClientManager>();
        _mockClient = new Mock<IClient>();
        _mockLoggerUp = new Mock<ILogger<ClientVolumeUpCommandHandler>>();
        _mockLoggerDown = new Mock<ILogger<ClientVolumeDownCommandHandler>>();
    }

    #region ClientVolumeUpCommandHandler Tests

    [Fact]
    public async Task ClientVolumeUpCommandHandler_Should_IncreaseVolume_Successfully()
    {
        // Arrange
        const int clientIndex = 1;
        const int currentVolume = 50;
        const int step = 10;
        const int expectedVolume = 60;

        var command = new ClientVolumeUpCommand
        {
            ClientIndex = clientIndex,
            Step = step,
            Source = CommandSource.Api,
        };

        var clientState = new ClientState
        {
            Id = clientIndex,
            SnapcastId = $"client-{clientIndex}",
            Name = $"Test Client {clientIndex}",
            Mac = "00:11:22:33:44:55",
            Connected = true,
            Volume = currentVolume,
            Mute = false,
            LatencyMs = 0,
        };

        _mockClientManager
            .Setup(x => x.GetClientStateAsync(clientIndex))
            .ReturnsAsync(Result<ClientState>.Success(clientState));

        _mockClientManager
            .Setup(x => x.GetClientAsync(clientIndex))
            .ReturnsAsync(Result<IClient>.Success(_mockClient.Object));

        _mockClient.Setup(x => x.SetVolumeAsync(expectedVolume)).ReturnsAsync(Result.Success());

        var handler = new ClientVolumeUpCommandHandler(_mockClientManager.Object, _mockLoggerUp.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockClient.Verify(x => x.SetVolumeAsync(expectedVolume), Times.Once);
    }

    [Fact]
    public async Task ClientVolumeUpCommandHandler_Should_CapVolumeAt100()
    {
        // Arrange
        const int clientIndex = 1;
        const int currentVolume = 95;
        const int step = 10;
        const int expectedVolume = 100; // Capped at 100

        var command = new ClientVolumeUpCommand
        {
            ClientIndex = clientIndex,
            Step = step,
            Source = CommandSource.Mqtt,
        };

        var clientState = new ClientState
        {
            Id = clientIndex,
            SnapcastId = $"client-{clientIndex}",
            Name = $"Test Client {clientIndex}",
            Mac = "00:11:22:33:44:55",
            Connected = true,
            Volume = currentVolume,
            Mute = false,
            LatencyMs = 0,
        };

        _mockClientManager
            .Setup(x => x.GetClientStateAsync(clientIndex))
            .ReturnsAsync(Result<ClientState>.Success(clientState));

        _mockClientManager
            .Setup(x => x.GetClientAsync(clientIndex))
            .ReturnsAsync(Result<IClient>.Success(_mockClient.Object));

        _mockClient.Setup(x => x.SetVolumeAsync(expectedVolume)).ReturnsAsync(Result.Success());

        var handler = new ClientVolumeUpCommandHandler(_mockClientManager.Object, _mockLoggerUp.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockClient.Verify(x => x.SetVolumeAsync(expectedVolume), Times.Once);
    }

    [Fact]
    public async Task ClientVolumeUpCommandHandler_Should_ReturnFailure_WhenClientNotFound()
    {
        // Arrange
        const int clientIndex = 999;
        var command = new ClientVolumeUpCommand
        {
            ClientIndex = clientIndex,
            Step = 5,
            Source = CommandSource.Internal,
        };

        _mockClientManager
            .Setup(x => x.GetClientStateAsync(clientIndex))
            .ReturnsAsync(Result<ClientState>.Failure("Client not found"));

        var handler = new ClientVolumeUpCommandHandler(_mockClientManager.Object, _mockLoggerUp.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Client not found");
    }

    #endregion

    #region ClientVolumeDownCommandHandler Tests

    [Fact]
    public async Task ClientVolumeDownCommandHandler_Should_DecreaseVolume_Successfully()
    {
        // Arrange
        const int clientIndex = 2;
        const int currentVolume = 60;
        const int step = 15;
        const int expectedVolume = 45;

        var command = new ClientVolumeDownCommand
        {
            ClientIndex = clientIndex,
            Step = step,
            Source = CommandSource.Knx,
        };

        var clientState = new ClientState
        {
            Id = clientIndex,
            SnapcastId = $"client-{clientIndex}",
            Name = $"Test Client {clientIndex}",
            Mac = "00:11:22:33:44:55",
            Connected = true,
            Volume = currentVolume,
            Mute = false,
            LatencyMs = 0,
        };

        _mockClientManager
            .Setup(x => x.GetClientStateAsync(clientIndex))
            .ReturnsAsync(Result<ClientState>.Success(clientState));

        _mockClientManager
            .Setup(x => x.GetClientAsync(clientIndex))
            .ReturnsAsync(Result<IClient>.Success(_mockClient.Object));

        _mockClient.Setup(x => x.SetVolumeAsync(expectedVolume)).ReturnsAsync(Result.Success());

        var handler = new ClientVolumeDownCommandHandler(_mockClientManager.Object, _mockLoggerDown.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockClient.Verify(x => x.SetVolumeAsync(expectedVolume), Times.Once);
    }

    [Fact]
    public async Task ClientVolumeDownCommandHandler_Should_CapVolumeAt0()
    {
        // Arrange
        const int clientIndex = 2;
        const int currentVolume = 5;
        const int step = 10;
        const int expectedVolume = 0; // Capped at 0

        var command = new ClientVolumeDownCommand
        {
            ClientIndex = clientIndex,
            Step = step,
            Source = CommandSource.Api,
        };

        var clientState = new ClientState
        {
            Id = clientIndex,
            SnapcastId = $"client-{clientIndex}",
            Name = $"Test Client {clientIndex}",
            Mac = "00:11:22:33:44:55",
            Connected = true,
            Volume = currentVolume,
            Mute = false,
            LatencyMs = 0,
        };

        _mockClientManager
            .Setup(x => x.GetClientStateAsync(clientIndex))
            .ReturnsAsync(Result<ClientState>.Success(clientState));

        _mockClientManager
            .Setup(x => x.GetClientAsync(clientIndex))
            .ReturnsAsync(Result<IClient>.Success(_mockClient.Object));

        _mockClient.Setup(x => x.SetVolumeAsync(expectedVolume)).ReturnsAsync(Result.Success());

        var handler = new ClientVolumeDownCommandHandler(_mockClientManager.Object, _mockLoggerDown.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockClient.Verify(x => x.SetVolumeAsync(expectedVolume), Times.Once);
    }

    [Fact]
    public async Task ClientVolumeDownCommandHandler_Should_ReturnFailure_WhenClientNotFound()
    {
        // Arrange
        const int clientIndex = 999;
        var command = new ClientVolumeDownCommand
        {
            ClientIndex = clientIndex,
            Step = 5,
            Source = CommandSource.Mqtt,
        };

        _mockClientManager
            .Setup(x => x.GetClientStateAsync(clientIndex))
            .ReturnsAsync(Result<ClientState>.Failure("Client not found"));

        var handler = new ClientVolumeDownCommandHandler(_mockClientManager.Object, _mockLoggerDown.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Client not found");
    }

    #endregion
}
