using Xunit;
using Xunit.Abstractions;

namespace SnapDog2.Tests.Integration;

/// <summary>
/// Simple integration test to verify test infrastructure works
/// </summary>
public class SimpleIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public SimpleIntegrationTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Simple_Test_Should_Pass()
    {
        _output.WriteLine("Simple test is running");
        Assert.True(true);
    }
}
