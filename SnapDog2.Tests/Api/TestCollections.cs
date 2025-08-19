namespace SnapDog2.Tests.Api;

using SnapDog2.Tests.Fixtures.WebApp;
using Xunit;

/// <summary>
/// Collection for tests that require API to be enabled.
/// Tests in this collection will not run in parallel with tests in other collections.
/// </summary>
[CollectionDefinition("ApiEnabled")]
public class ApiEnabledCollection : ICollectionFixture<CustomWebApplicationFactory> { }

/// <summary>
/// Collection for tests that require API to be disabled.
/// Tests in this collection will not run in parallel with tests in other collections.
/// </summary>
[CollectionDefinition("ApiDisabled")]
public class ApiDisabledCollection : ICollectionFixture<ApiDisabledWebApplicationFactory> { }
