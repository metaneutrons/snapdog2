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
/// Collection for integration tests using unified Docker Compose infrastructure
/// </summary>
[CollectionDefinition(TestCategories.Integration)]
public class IntegrationTestCollection : ICollectionFixture<Containers.DockerComposeTestFixture>
{
    // Integration tests share the unified Docker Compose environment
}

/// <summary>
/// Collection for container-based tests using unified Docker Compose infrastructure
/// </summary>
[CollectionDefinition(TestCategories.Container)]
public class ContainerTestCollection : ICollectionFixture<Containers.DockerComposeTestFixture>
{
    // Container tests use the same unified infrastructure
}

/// <summary>
/// Collection for performance tests using unified Docker Compose infrastructure
/// </summary>
[CollectionDefinition(TestCategories.Performance)]
public class PerformanceTestCollection : ICollectionFixture<Containers.DockerComposeTestFixture>
{
    // Performance tests run with unified infrastructure
}

/// <summary>
/// Collection for workflow tests using unified Docker Compose infrastructure
/// </summary>
[CollectionDefinition(TestCategories.Workflow)]
public class WorkflowTestCollection : ICollectionFixture<Containers.DockerComposeTestFixture>
{
    // Workflow tests use unified infrastructure
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
/// Collection for smoke tests using unified Docker Compose infrastructure
/// </summary>
[CollectionDefinition(TestCategories.Smoke)]
public class SmokeTestCollection : ICollectionFixture<Containers.DockerComposeTestFixture>
{
    // Smoke tests use unified infrastructure
}

/// <summary>
/// Collection for regression tests using unified Docker Compose infrastructure
/// </summary>
[CollectionDefinition(TestCategories.Regression)]
public class RegressionTestCollection : ICollectionFixture<Containers.DockerComposeTestFixture>
{
    // Regression tests use unified infrastructure
}
