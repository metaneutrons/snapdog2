namespace SnapDog2.Tests.Integration.Knx;

using SnapDog2.Tests.Integration.Fixtures;
using Xunit;

/// <summary>
/// Test collection for KNX integration flow tests.
/// Ensures tests run sequentially to avoid conflicts with shared KNX/MQTT/Snapcast infrastructure.
/// </summary>
[CollectionDefinition("KnxIntegrationFlow")]
public class KnxIntegrationFlowCollection : ICollectionFixture<KnxIntegrationTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

/// <summary>
/// Test collection for KNX container-based tests (existing).
/// Maintains compatibility with existing KNX health and interaction tests.
/// </summary>
[CollectionDefinition("KnxdContainer")]
public class KnxdContainerCollection : ICollectionFixture<KnxdFixture>
{
    // Existing collection for KNX container tests
}
