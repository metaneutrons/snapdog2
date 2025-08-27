# 🧪 SnapDog2 Enterprise Test Suite

Welcome to the **enterprise-grade test suite** for SnapDog2! This comprehensive testing framework provides **100% modern, best-practice testing** with advanced features for unit, integration, container, performance, and workflow testing.

## 🏗️ **Test Architecture**

```
SnapDog2.Tests/
├── 📁 Unit/                    # Pure unit tests (no external dependencies)
├── 📁 Integration/             # Integration tests with real services
├── 📁 Container/               # Testcontainer-based tests
├── 📁 Performance/             # Performance and load tests
├── 📁 Fixtures/                # Test fixtures and infrastructure
├── 📁 Helpers/                 # Test utilities and extensions
└── 📁 TestData/                # Test data files
```

## 🚀 **Quick Start**

### **Run All Tests**

```bash
./run-tests.sh
```

### **Run Specific Categories**

```bash
./run-tests.sh -c Unit                    # Unit tests only
./run-tests.sh -c Integration             # Integration tests
./run-tests.sh -c Container               # Container tests
./run-tests.sh -c Performance             # Performance tests
```

### **Run with Filters**

```bash
./run-tests.sh -t Service -s Fast         # Fast service tests
./run-tests.sh -c Unit --no-coverage      # Unit tests without coverage
./run-tests.sh --ci                       # CI/CD optimized execution
```

## 📊 **Test Categories**

### **🔧 Unit Tests** (`Unit/`)

- **Purpose**: Pure unit tests with no external dependencies
- **Speed**: Fast (< 100ms)
- **Isolation**: Complete isolation using mocks and stubs
- **Coverage**: High code coverage with edge case testing

**Examples:**

- Configuration validation
- Domain model behavior
- Service logic without I/O
- Utility functions

### **🔗 Integration Tests** (`Integration/`)

- **Purpose**: Test component interactions with real services
- **Speed**: Medium to Slow (100ms - 10s)
- **Dependencies**: Real MQTT, Snapcast, KNX services
- **Scope**: API endpoints, service integration, workflows

**Examples:**

- API controller integration
- Service communication
- Database operations
- Message queue processing

### **🐳 Container Tests** (`Container/`)

- **Purpose**: Test with real containerized services
- **Speed**: Slow (1s - 30s)
- **Infrastructure**: Docker containers via Testcontainers
- **Realism**: Production-like environment

**Examples:**

- Multi-room audio streaming
- MQTT broker communication
- Snapcast clients/server interaction
- Network configuration testing

### **⚡ Performance Tests** (`Performance/`)

- **Purpose**: Measure performance, throughput, and resource usage
- **Speed**: Very Slow (10s+)
- **Metrics**: Response time, throughput, memory usage
- **Load Testing**: Concurrent users, stress testing

**Examples:**

- API response time benchmarks
- Concurrent request handling
- Memory leak detection
- Throughput measurement

### **🔄 Workflow Tests** (`Integration/Workflows/`)

- **Purpose**: End-to-end business scenarios
- **Speed**: Slow (5s - 60s)
- **Scope**: Complete user journeys
- **Validation**: Business logic correctness

**Examples:**

- Zone change workflows
- Client discovery processes
- Audio streaming scenarios
- Configuration management

## 🏷️ **Test Attributes & Traits**

### **Categories**

```csharp
[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Integration)]
[Trait("Category", TestCategories.Container)]
[Trait("Category", TestCategories.Performance)]
[Trait("Category", TestCategories.Workflow)]
```

### **Types**

```csharp
[Trait("Type", TestTypes.Service)]
[Trait("Type", TestTypes.Controller)]
[Trait("Type", TestTypes.Configuration)]
[Trait("Type", TestTypes.Infrastructure)]
```

### **Speed Classifications**

```csharp
[TestSpeed(TestSpeed.Fast)]      // < 100ms
[TestSpeed(TestSpeed.Medium)]    // 100ms - 1s
[TestSpeed(TestSpeed.Slow)]      // 1s - 10s
[TestSpeed(TestSpeed.VerySlow)]  // > 10s
```

### **Requirements**

```csharp
[Requires(TestRequirements.Docker)]
[Requires(TestRequirements.Network)]
[Requires(TestRequirements.Audio)]
```

## 🛠️ **Enterprise Features**

### **🔍 Advanced Assertions**

```csharp
// HTTP Response Assertions
response.Should().BeSuccessful();
await response.Should().ContainValidJsonAsync<ApiResponse>();

// Result Pattern Assertions
result.Should().BeSuccessful();
result.Should().BeSuccessfulWithValue(expectedValue);
result.Should().BeFailureWithError("Expected error message");

// Async Collection Assertions
await collection.Should().ContainMatchingItemsWithinAsync(
    item => item.Status == "Ready",
    TimeSpan.FromSeconds(30));
```

### **📊 Performance Measurement**

```csharp
// Automatic performance logging
var result = await _output.MeasureAsync("Operation Name", async () =>
{
    return await someService.PerformOperationAsync();
});

// Manual performance tracking
_output.WritePerformance("Database Query", duration, "Additional info");
```

### **🏗️ Test Data Builders**

```csharp
// Fluent test data creation
var client = ClientConfigBuilder
    .LivingRoom()
    .WithMac("02:42:ac:11:00:10")
    .WithMqtt(mqtt => mqtt.WithBaseTopic("snapdog/clients/livingroom"))
    .Build();

var zone = ZoneConfigBuilder
    .GroundFloor()
    .WithSink("/snapsinks/zone1")
    .Build();
```

### **📝 Structured Logging**

```csharp
// Structured test output
_output.WriteSection("Test Section Name");
_output.WriteStep("Step Name", "Description");
_output.WriteSuccess("Operation completed successfully");
_output.WriteFailure("Operation failed");
_output.WriteJson(complexObject, "Object State");
```

## 🔧 **Configuration**

### **Test Execution Settings** (`xunit.runner.json`)

- Parallel execution optimization
- Test discovery configuration
- Timeout settings
- Reporter configuration

### **Build Configuration** (`Directory.Build.props`)

- Code coverage settings
- Performance optimization
- Warning configuration
- Global usings

### **Project Configuration** (`SnapDog2.Tests.csproj`)

- Enterprise testing packages
- Performance benchmarking tools
- Code coverage tools
- Test data management

## 📈 **Code Coverage**

### **Generate Coverage Report**

```bash
./run-tests.sh --coverage
```

### **Coverage Thresholds**

- **Minimum**: 80% line coverage
- **Branch**: 80% branch coverage
- **Method**: 80% method coverage

### **Coverage Exclusions**

- Generated code
- Third-party integrations
- Test infrastructure

## 🚀 **CI/CD Integration**

### **GitHub Actions**

```bash
./run-tests.sh --ci
```

### **Azure DevOps**

```bash
./run-tests.sh --ci -o $(Agent.TempDirectory)/TestResults
```

### **Jenkins**

```bash
./run-tests.sh --ci -o $WORKSPACE/TestResults
```

## 🐳 **Container Requirements**

### **Docker Setup**

- Docker Desktop or Docker Engine
- Minimum 4GB RAM allocated
- Network access for image pulls

### **Container Images Used**

- `eclipse-mosquitto:2.0` - MQTT broker
- `badaix/snapserver:latest` - Snapcast server
- `badaix/snapclient:latest` - Snapcast clients

## 📊 **Performance Benchmarks**

### **Response Time Targets**

- **API Endpoints**: < 100ms average
- **Service Operations**: < 50ms average
- **Database Queries**: < 25ms average

### **Throughput Targets**

- **API Requests**: > 100 req/sec
- **Message Processing**: > 1000 msg/sec
- **Concurrent Users**: > 50 users

### **Resource Usage Limits**

- **Memory Growth**: < 50% during test execution
- **CPU Usage**: < 80% sustained
- **Network Bandwidth**: Efficient usage

## 🔍 **Debugging Tests**

### **Visual Studio / Rider**

1. Set breakpoints in test code
2. Run tests in debug mode
3. Attach to test process

### **VS Code**

1. Use C# Dev Kit extension
2. Configure launch.json for test debugging
3. Set breakpoints and run

### **Command Line Debugging**

```bash
./run-tests.sh -v --no-parallel    # Verbose, sequential execution
```

## 📚 **Best Practices**

### **✅ Do's**

- Use descriptive test names that explain behavior
- Follow AAA pattern (Arrange, Act, Assert)
- Use appropriate test categories and traits
- Implement proper cleanup in fixtures
- Use builders for complex test data
- Add performance assertions for critical paths
- Use structured logging for test output

### **❌ Don'ts**

- Don't use Thread.Sleep in tests
- Don't test implementation details
- Don't create interdependent tests
- Don't ignore flaky tests
- Don't skip cleanup operations
- Don't use production data in tests

### **🏗️ Test Structure**

```csharp
[Collection(TestCategories.Integration)]
[Trait("Category", TestCategories.Integration)]
[Trait("Type", TestTypes.Service)]
[TestSpeed(TestSpeed.Medium)]
public class ServiceIntegrationTests
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ServiceIntegrationTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Operation_WithValidInput_ShouldReturnExpectedResult()
    {
        // Arrange
        _output.WriteSection("Test Description");
        var service = _fixture.GetService<IService>();
        var input = TestDataBuilder.CreateValidInput();

        // Act
        var result = await _output.MeasureAsync("Operation", async () =>
        {
            return await service.PerformOperationAsync(input);
        });

        // Assert
        result.Should().BeSuccessful();
        _output.WriteSuccess("Test completed successfully");
    }
}
```

## 🆘 **Troubleshooting**

### **Common Issues**

**Docker not available**

```bash
# Check Docker status
docker info

# Start Docker Desktop (macOS/Windows)
# Or start Docker daemon (Linux)
sudo systemctl start docker
```

**Port conflicts**

```bash
# Check for port usage
lsof -i :1883  # MQTT
lsof -i :1705  # Snapcast

# Kill conflicting processes if needed
```

**Memory issues**

```bash
# Increase Docker memory allocation
# Docker Desktop -> Settings -> Resources -> Memory
```

**Test timeouts**

```bash
# Run with increased verbosity
./run-tests.sh -v

# Run specific failing test
./run-tests.sh --filter "FullyQualifiedName~TestMethodName"
```

## 📞 **Support**

For issues with the test suite:

1. Check this documentation
2. Review test output logs
3. Check Docker container status
4. Verify network connectivity
5. Create an issue with detailed logs

---

**🎯 This enterprise test suite provides comprehensive coverage, performance insights, and maintainable test infrastructure for SnapDog2's multi-room audio platform.**
