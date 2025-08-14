using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using SnapDog2.Tests.Integration.Fixtures;
using Xunit;

namespace SnapDog2.Tests.Integration.Api;

[Collection("AppContainer")]
public class AppHealthTests
{
    private readonly AppContainerFixture _fixture;

    public AppHealthTests(AppContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReadyEndpoint_ShouldBeReachable()
    {
        var response = await _fixture.HttpClient.GetAsync("/api/health/ready");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }
}
