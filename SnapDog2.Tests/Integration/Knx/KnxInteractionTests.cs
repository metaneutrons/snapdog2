namespace SnapDog2.Tests.Integration.Knx;

using FluentAssertions;
using SnapDog2.Tests.Integration.Fixtures;

[Collection("KnxdContainer")]
public class KnxInteractionTests
{
    private readonly KnxdFixture _fixture;

    public KnxInteractionTests(KnxdFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Enable once Falcon client wiring and state assertions are finalized")]
    public async Task GroupValueWrite_VolumeSet_ShouldReflectInAppState()
    {
        // Arrange
        // TODO: Initialize Falcon client against _fixture.KnxHost:_fixture.KnxTcpPort (ETS router is on 3671; TCP control on 6720)
        // TODO: Publish GroupValueWrite to GA 1/2/1 with value 50 (DPT 5.001)
        // TODO: Assert via API endpoint (/zones/1) or MQTT status topic that volume==50

        await Task.CompletedTask;
        true.Should().BeTrue();
    }
}
