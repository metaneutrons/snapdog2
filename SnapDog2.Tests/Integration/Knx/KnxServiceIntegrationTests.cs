using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapDog2.Core.Common;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Services;
using Xunit;

namespace SnapDog2.Tests.Integration.Knx;

[Trait("Category", "Integration")]
public class KnxServiceIntegrationTests : IClassFixture<KnxTestContainer>
{
    private readonly KnxTestContainer _knxTestContainer;
    private readonly KnxService _knxService;
    private readonly Mock<IMediator> _mockMediator;

    public KnxServiceIntegrationTests(KnxTestContainer knxTestContainer)
    {
        _knxTestContainer = knxTestContainer;
        _mockMediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<KnxService>>().Object;
        var config = new KnxConfiguration { Gateway = _knxTestContainer.ConnectionString };
        var options = Options.Create(config);
        _knxService = new KnxService(options, logger);
    }

    [Fact]
    public async Task WriteGroupValueAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var address = KnxAddress.Parse("1/1/1");
        var value = new byte[] { 0x01 };

        // Act
        var result = await _knxService.WriteGroupValueAsync(address, value);

        // Assert
        Assert.True(result);
    }
}
