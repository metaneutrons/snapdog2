using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SnapDog2.Infrastructure.Services;
using Xunit;

namespace SnapDog2.Tests.Functional;

public abstract class ApiTestBase : IClassFixture<TestWebApplicationFactory<Program>>
{
    protected readonly HttpClient _client;
    protected readonly Mock<IMediator> _mockMediator;
    protected readonly Mock<IKnxService> _mockKnxService;
    protected readonly Mock<IMqttService> _mockMqttService;
    protected readonly Mock<ISnapcastService> _mockSnapcastService;

    protected ApiTestBase(TestWebApplicationFactory<Program> factory)
    {
        factory.ResetMocks();
        _mockMediator = factory.MockMediator;
        _mockKnxService = factory.MockKnxService;
        _mockMqttService = factory.MockMqttService;
        _mockSnapcastService = factory.MockSnapcastService;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
    }
}
