namespace SnapDog2.Tests.Fixtures.Shared;

/// <summary>
/// Test categories for organizing and filtering tests
/// </summary>
public static class TestCategories
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string Container = "Container";
    public const string Performance = "Performance";
    public const string Workflow = "Workflow";
    public const string Smoke = "Smoke";
    public const string Regression = "Regression";
}

/// <summary>
/// Test types for more granular classification
/// </summary>
public static class TestTypes
{
    public const string Service = "Service";
    public const string Controller = "Controller";
    public const string Configuration = "Configuration";
    public const string Infrastructure = "Infrastructure";
    public const string Domain = "Domain";
    public const string Api = "Api";
    public const string Database = "Database";
    public const string Network = "Network";

    // Real-world scenario test types
    public const string RealWorldScenario = "RealWorldScenario";
    public const string FaultInjection = "FaultInjection";
    public const string Performance = "Performance";
    public const string LoadTest = "LoadTest";
    public const string StressTest = "StressTest";
    public const string EndToEnd = "EndToEnd";
    public const string CrossSystem = "CrossSystem";
    public const string Recovery = "Recovery";
}

/// <summary>
/// Test requirements for dependency management
/// </summary>
public static class TestRequirements
{
    public const string Docker = "Docker";
    public const string Network = "Network";
    public const string Audio = "Audio";
    public const string FileSystem = "FileSystem";
    public const string ExternalService = "ExternalService";
    public const string Database = "Database";

    // Real-world scenario requirements
    public const string Container = "Container";
    public const string Snapcast = "Snapcast";
    public const string RealTimeSystem = "RealTimeSystem";
    public const string HighMemory = "HighMemory";
    public const string LongRunning = "LongRunning";
}

/// <summary>
/// Test execution speed categories
/// </summary>
public static class TestSpeed
{
    public const string Fast = "Fast"; // < 100ms
    public const string Medium = "Medium"; // 100ms - 1s
    public const string Slow = "Slow"; // > 1s
    public const string VerySlow = "VerySlow"; // > 10s
}

/// <summary>
/// Custom attribute for marking test execution speed
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class TestSpeedAttribute : Attribute
{
    public string Speed { get; }

    public TestSpeedAttribute(string speed)
    {
        Speed = speed;
    }
}

/// <summary>
/// Custom attribute for marking test requirements
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequiresAttribute : Attribute
{
    public string Requirement { get; }

    public RequiresAttribute(string requirement)
    {
        Requirement = requirement;
    }
}

/// <summary>
/// Custom attribute for marking flaky tests that need retry logic
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RetryAttribute : Attribute
{
    public int MaxRetries { get; }
    public int DelayMs { get; }

    public RetryAttribute(int maxRetries = 3, int delayMs = 1000)
    {
        MaxRetries = maxRetries;
        DelayMs = delayMs;
    }
}

/// <summary>
/// Custom attribute for marking tests that should only run in CI
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class CiOnlyAttribute : Attribute { }

/// <summary>
/// Custom attribute for marking tests with specific environment requirements
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class EnvironmentAttribute : Attribute
{
    public string Environment { get; }

    public EnvironmentAttribute(string environment)
    {
        Environment = environment;
    }
}

/// <summary>
/// Custom attribute for marking test execution priority
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TestPriorityAttribute : Attribute
{
    public int Priority { get; }

    public TestPriorityAttribute(int priority)
    {
        Priority = priority;
    }
}
