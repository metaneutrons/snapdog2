namespace SnapDog2.Tests.Fixtures.Shared;

/// <summary>
/// Collection for pure unit tests that run in parallel without external dependencies
/// </summary>
[CollectionDefinition(TestCategories.Unit)]
public class UnitTestCollection
{
    // Unit tests can run in parallel as they have no shared state
}

/// <summary>
/// Collection for integration tests that require shared infrastructure
/// </summary>
[CollectionDefinition(TestCategories.Integration)]
public class IntegrationTestCollection : ICollectionFixture<Integration.IntegrationTestFixture>
{
    // Integration tests share the same fixture to avoid resource conflicts
}

/// <summary>
/// Collection for container-based tests using Testcontainers
/// </summary>
[CollectionDefinition(TestCategories.Container)]
public class ContainerTestCollection : ICollectionFixture<Containers.TestcontainersFixture>
{
    // Container tests share Docker resources and network
}

/// <summary>
/// Collection for performance tests that need isolated execution
/// </summary>
[CollectionDefinition(TestCategories.Performance)]
public class PerformanceTestCollection : ICollectionFixture<Integration.IntegrationTestFixture>
{
    // Performance tests run sequentially to avoid resource contention
}

/// <summary>
/// Collection for workflow tests that simulate end-to-end scenarios
/// </summary>
[CollectionDefinition(TestCategories.Workflow)]
public class WorkflowTestCollection : ICollectionFixture<Integration.IntegrationTestFixture>
{
    // Workflow tests need full application context
}

/// <summary>
/// Collection for API tests with enabled authentication and services
/// </summary>
[CollectionDefinition("ApiEnabled")]
public class ApiEnabledCollection : ICollectionFixture<WebApp.CustomWebApplicationFactory>
{
    // API tests with full application stack
}

/// <summary>
/// Collection for API tests with disabled services for configuration testing
/// </summary>
[CollectionDefinition("ApiDisabled")]
public class ApiDisabledCollection : ICollectionFixture<WebApp.ApiDisabledWebApplicationFactory>
{
    // API tests with minimal configuration
}

/// <summary>
/// Collection for smoke tests that verify basic functionality
/// </summary>
[CollectionDefinition(TestCategories.Smoke)]
public class SmokeTestCollection : ICollectionFixture<Integration.IntegrationTestFixture>
{
    // Smoke tests verify critical paths work
}

/// <summary>
/// Collection for regression tests that prevent known issues from reoccurring
/// </summary>
[CollectionDefinition(TestCategories.Regression)]
public class RegressionTestCollection : ICollectionFixture<Integration.IntegrationTestFixture>
{
    // Regression tests ensure fixed bugs stay fixed
}
