# Testing & Validation Framework

## Overview

This document defines the comprehensive testing and validation framework for SnapDog, ensuring robust quality assurance through test-driven development and AI-assisted testing strategies.

## Testing Philosophy

### Test-Driven Development (TDD)

Every feature development follows the Red-Green-Refactor cycle:

1. **Red**: Write failing test that defines desired functionality
2. **Green**: Write minimal code to make the test pass
3. **Refactor**: Improve code quality while keeping tests passing

### Testing Pyramid

```
                    /\
                   /  \
                  / E2E \ (Few, Slow, Expensive)
                 /______\
                /        \
               /Integration\ (Some, Medium Speed)
              /____________\
             /              \
            /   Unit Tests   \ (Many, Fast, Cheap)
           /________________\
```

### Quality Standards

- **Unit Tests**: 95% code coverage minimum
- **Integration Tests**: All cross-layer interactions covered
- **End-to-End Tests**: Critical user workflows validated
- **Performance Tests**: Real-time requirements verified

## Test Categories

### Unit Tests

#### Purpose and Scope

- Test individual components in complete isolation
- Verify business logic correctness
- Validate error handling and edge cases
- Ensure proper state management

#### Implementation Standards

```csharp
[TestClass]
public class AudioStreamTests
{
    [TestMethod]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var name = "Test Stream";
        var codec = AudioCodec.FLAC;
        var sampleRate = 44100;
        var bitDepth = 16;
        var channels = 2;

        // Act
        var stream = new AudioStream(name, codec, sampleRate, bitDepth, channels);

        // Assert
        stream.Name.Should().Be(name);
        stream.Codec.Should().Be(codec);
        stream.SampleRate.Should().Be(sampleRate);
        stream.BitDepth.Should().Be(bitDepth);
        stream.Channels.Should().Be(channels);
        stream.Status.Should().Be(StreamStatus.Stopped);
    }

    [TestMethod]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidName = "";
        var codec = AudioCodec.FLAC;

        // Act & Assert
        var action = () => new AudioStream(invalidName, codec, 44100, 16, 2);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Stream name cannot be empty*");
    }

    [TestMethod]
    public void Start_WhenStopped_ShouldChangeStatusToActive()
    {
        // Arrange
        var stream = CreateTestAudioStream();
        stream.Status.Should().Be(StreamStatus.Stopped);

        // Act
        var result = stream.Start();

        // Assert
        result.Should().BeSuccessful();
        stream.Status.Should().Be(StreamStatus.Active);
    }

    [TestMethod]
    public void Start_WhenAlreadyActive_ShouldReturnFailure()
    {
        // Arrange
        var stream = CreateTestAudioStream();
        stream.Start(); // First start

        // Act
        var result = stream.Start(); // Second start

        // Assert
        result.Should().BeFailure();
        result.Error.Should().Contain("already active");
    }

    private static AudioStream CreateTestAudioStream()
    {
        return new AudioStream("Test Stream", AudioCodec.FLAC, 44100, 16, 2);
    }
}
```

#### AI-Assisted Unit Test Generation

```
UNIT TEST GENERATION TEMPLATE

Context: [Include component context and business rules]

Component Under Test: [ClassName]
Method Under Test: [MethodName]

Test Scenarios to Generate:
1. Happy path with valid inputs
2. Edge cases and boundary conditions
3. Invalid input scenarios
4. Error conditions and exception handling
5. State transition validations

Requirements:
- Use AAA pattern (Arrange, Act, Assert)
- Follow naming convention: MethodName_StateUnderTest_ExpectedBehavior
- Include FluentAssertions for readable assertions
- Mock all external dependencies
- Test one behavior per test method

Generate comprehensive test class covering all scenarios.
```

### Integration Tests

#### Purpose and Scope

- Test interactions between layers
- Validate database operations
- Test external service integrations
- Verify configuration loading

#### Database Integration Tests

```csharp
[TestClass]
[TestCategory("Integration")]
public class AudioStreamRepositoryIntegrationTests
{
    private TestDbContext _dbContext;
    private AudioStreamRepository _repository;

    [TestInitialize]
    public async Task Setup()
    {
        // Use in-memory database for integration tests
        var options = new DbContextOptionsBuilder<SnapDogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _dbContext = new TestDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _repository = new AudioStreamRepository(_dbContext);
    }

    [TestMethod]
    public async Task CreateAsync_WithValidStream_ShouldPersistToDatabase()
    {
        // Arrange
        var stream = new AudioStream("Integration Test Stream", AudioCodec.FLAC, 44100, 16, 2);

        // Act
        var result = await _repository.CreateAsync(stream, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);

        // Verify persistence
        var persistedStream = await _repository.GetByIdAsync(result.Id, CancellationToken.None);
        persistedStream.Should().NotBeNull();
        persistedStream.Name.Should().Be(stream.Name);
        persistedStream.Codec.Should().Be(stream.Codec);
    }

    [TestMethod]
    public async Task GetActiveStreamsAsync_WithMixedStatuses_ShouldReturnOnlyActiveStreams()
    {
        // Arrange
        var activeStream1 = new AudioStream("Active 1", AudioCodec.FLAC, 44100, 16, 2);
        var activeStream2 = new AudioStream("Active 2", AudioCodec.FLAC, 44100, 16, 2);
        var stoppedStream = new AudioStream("Stopped", AudioCodec.FLAC, 44100, 16, 2);

        await _repository.CreateAsync(activeStream1, CancellationToken.None);
        await _repository.CreateAsync(activeStream2, CancellationToken.None);
        await _repository.CreateAsync(stoppedStream, CancellationToken.None);

        // Start only the first two streams
        activeStream1.Start();
        activeStream2.Start();

        await _repository.UpdateAsync(activeStream1, CancellationToken.None);
        await _repository.UpdateAsync(activeStream2, CancellationToken.None);

        // Act
        var activeStreams = await _repository.GetActiveStreamsAsync(CancellationToken.None);

        // Assert
        activeStreams.Should().HaveCount(2);
        activeStreams.Should().OnlyContain(s => s.Status == StreamStatus.Active);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }
}
```

#### External Service Integration Tests

```csharp
[TestClass]
[TestCategory("Integration")]
public class SnapcastServiceIntegrationTests
{
    private readonly Mock<ISnapcastClient> _mockSnapcastClient;
    private readonly SnapcastService _service;

    public SnapcastServiceIntegrationTests()
    {
        _mockSnapcastClient = new Mock<ISnapcastClient>();
        var mockLogger = new Mock<ILogger<SnapcastService>>();
        _service = new SnapcastService(_mockSnapcastClient.Object, mockLogger.Object);
    }

    [TestMethod]
    public async Task GetServerStatusAsync_WithValidConnection_ShouldReturnStatus()
    {
        // Arrange
        var expectedStatus = new ServerStatus { Version = "0.26.0", Connected = true };
        _mockSnapcastClient
            .Setup(c => c.GetServerStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        // Act
        var result = await _service.GetServerStatusAsync(CancellationToken.None);

        // Assert
        result.Should().BeSuccessful();
        result.Value.Should().BeEquivalentTo(expectedStatus);
        _mockSnapcastClient.Verify(c => c.GetServerStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetServerStatusAsync_WithConnectionFailure_ShouldReturnFailure()
    {
        // Arrange
        _mockSnapcastClient
            .Setup(c => c.GetServerStatusAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SnapcastConnectionException("Connection failed"));

        // Act
        var result = await _service.GetServerStatusAsync(CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Error.Should().Contain("Connection failed");
    }
}
```

### End-to-End Tests

#### Purpose and Scope

- Test complete user workflows
- Validate API endpoints with real integrations
- Test system behavior under realistic conditions
- Verify cross-system integration

#### API End-to-End Tests

```csharp
[TestClass]
[TestCategory("E2E")]
public class StreamManagementE2ETests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database with in-memory version
                    services.RemoveAll(typeof(DbContextOptions<SnapDogDbContext>));
                    services.AddDbContext<SnapDogDbContext>(options =>
                        options.UseInMemoryDatabase("E2ETestDb"));

                    // Mock external services for E2E tests
                    services.RemoveAll(typeof(ISnapcastClient));
                    services.AddScoped<ISnapcastClient, MockSnapcastClient>();
                });

                builder.UseEnvironment("Testing");
            });

        _client = _factory.CreateClient();
    }

    [TestMethod]
    public async Task CompleteStreamWorkflow_CreateStartStopDelete_ShouldSucceed()
    {
        // Arrange
        var createRequest = new CreateStreamRequest
        {
            Name = "E2E Test Stream",
            Codec = "FLAC",
            SampleRate = 44100,
            BitDepth = 16,
            Channels = 2
        };

        // Act & Assert - Create Stream
        var createResponse = await _client.PostAsJsonAsync("/api/v1/streams", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdStream = await createResponse.Content.ReadFromJsonAsync<AudioStreamDto>();
        createdStream.Should().NotBeNull();
        createdStream.Name.Should().Be(createRequest.Name);
        createdStream.Status.Should().Be("Stopped");

        // Act & Assert - Start Stream
        var startResponse = await _client.PostAsync($"/api/v1/streams/{createdStream.Id}/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify stream is running
        var statusResponse = await _client.GetAsync($"/api/v1/streams/{createdStream.Id}");
        var runningStream = await statusResponse.Content.ReadFromJsonAsync<AudioStreamDto>();
        runningStream.Status.Should().Be("Active");

        // Act & Assert - Stop Stream
        var stopResponse = await _client.PostAsync($"/api/v1/streams/{createdStream.Id}/stop", null);
        stopResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify stream is stopped
        statusResponse = await _client.GetAsync($"/api/v1/streams/{createdStream.Id}");
        var stoppedStream = await statusResponse.Content.ReadFromJsonAsync<AudioStreamDto>();
        stoppedStream.Status.Should().Be("Stopped");

        // Act & Assert - Delete Stream
        var deleteResponse = await _client.DeleteAsync($"/api/v1/streams/{createdStream.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify stream is deleted
        var notFoundResponse = await _client.GetAsync($"/api/v1/streams/{createdStream.Id}");
        notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task ConcurrentStreamOperations_MultipleClients_ShouldHandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            var request = new CreateStreamRequest
            {
                Name = $"Concurrent Stream {i}",
                Codec = "FLAC",
                SampleRate = 44100,
                BitDepth = 16,
                Channels = 2
            };

            tasks.Add(_client.PostAsJsonAsync("/api/v1/streams", request));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(response =>
            response.StatusCode.Should().Be(HttpStatusCode.Created));

        // Verify all streams were created with unique IDs
        var streamIds = new List<int>();
        foreach (var response in responses)
        {
            var stream = await response.Content.ReadFromJsonAsync<AudioStreamDto>();
            streamIds.Add(stream.Id);
        }

        streamIds.Should().OnlyHaveUniqueItems();
        streamIds.Should().HaveCount(5);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

### Performance Tests

#### Purpose and Scope

- Validate real-time audio processing requirements
- Test system performance under load
- Memory usage and leak detection
- Concurrent operation performance

#### Audio Processing Performance Tests

```csharp
[TestClass]
[TestCategory("Performance")]
public class AudioProcessingPerformanceTests
{
    [TestMethod]
    [Timeout(1000)] // 1 second maximum
    public async Task ProcessAudioBuffer_LargeBuffer_ShouldMeetLatencyRequirements()
    {
        // Arrange
        var processor = new AudioProcessor();
        var audioBuffer = CreateAudioBuffer(1024 * 1024); // 1MB buffer
        var latencyRequirement = TimeSpan.FromMilliseconds(10);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await processor.ProcessBufferAsync(audioBuffer);
        stopwatch.Stop();

        // Assert
        result.Should().BeSuccessful();
        stopwatch.Elapsed.Should().BeLessThan(latencyRequirement,
            "Audio processing must complete within 10ms for real-time requirements");
    }

    [TestMethod]
    public async Task ConcurrentStreamProcessing_MultipleStreams_ShouldMaintainPerformance()
    {
        // Arrange
        var processor = new AudioProcessor();
        var streamCount = 10;
        var bufferSize = 4096; // 4KB buffers
        var maxLatencyPerStream = TimeSpan.FromMilliseconds(10);

        var tasks = Enumerable.Range(1, streamCount)
            .Select(async i =>
            {
                var buffer = CreateAudioBuffer(bufferSize);
                var stopwatch = Stopwatch.StartNew();
                var result = await processor.ProcessBufferAsync(buffer);
                stopwatch.Stop();

                return new { StreamId = i, Elapsed = stopwatch.Elapsed, Result = result };
            })
            .ToArray();

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.Result.Should().BeSuccessful();
            r.Elapsed.Should().BeLessThan(maxLatencyPerStream,
                $"Stream {r.StreamId} exceeded latency requirement");
        });

        // Verify no significant performance degradation with multiple streams
        var avgLatency = results.Average(r => r.Elapsed.TotalMilliseconds);
        avgLatency.Should().BeLessThan(8.0,
            "Average latency should remain well below 10ms even with concurrent processing");
    }

    [TestMethod]
    public async Task MemoryUsage_ContinuousProcessing_ShouldNotLeak()
    {
        // Arrange
        var processor = new AudioProcessor();
        var iterations = 1000;
        var bufferSize = 8192;
        var initialMemory = GC.GetTotalMemory(false);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var buffer = CreateAudioBuffer(bufferSize);
            await processor.ProcessBufferAsync(buffer);

            // Force garbage collection every 100 iterations
            if (i % 100 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        // Final cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePercent = (double)memoryIncrease / initialMemory * 100;

        memoryIncreasePercent.Should().BeLessThan(10.0,
            "Memory usage should not increase significantly after continuous processing");
    }

    private static byte[] CreateAudioBuffer(int size)
    {
        var buffer = new byte[size];
        new Random().NextBytes(buffer);
        return buffer;
    }
}
```

## Test Data Management

### Test Data Builders

```csharp
public class AudioStreamBuilder
{
    private string _name = "Test Stream";
    private AudioCodec _codec = AudioCodec.FLAC;
    private int _sampleRate = 44100;
    private int _bitDepth = 16;
    private int _channels = 2;
    private StreamStatus _status = StreamStatus.Stopped;

    public AudioStreamBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AudioStreamBuilder WithCodec(AudioCodec codec)
    {
        _codec = codec;
        return this;
    }

    public AudioStreamBuilder WithSampleRate(int sampleRate)
    {
        _sampleRate = sampleRate;
        return this;
    }

    public AudioStreamBuilder WithStatus(StreamStatus status)
    {
        _status = status;
        return this;
    }

    public AudioStream Build()
    {
        var stream = new AudioStream(_name, _codec, _sampleRate, _bitDepth, _channels);

        // Set status if not default
        if (_status == StreamStatus.Active)
        {
            stream.Start();
        }

        return stream;
    }

    public static AudioStreamBuilder Create() => new AudioStreamBuilder();
}
```

### Test Data Factories

```csharp
public static class TestDataFactory
{
    public static AudioStream CreateValidAudioStream(string name = "Test Stream")
    {
        return AudioStreamBuilder.Create()
            .WithName(name)
            .WithCodec(AudioCodec.FLAC)
            .WithSampleRate(44100)
            .Build();
    }

    public static List<AudioStream> CreateMultipleAudioStreams(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateValidAudioStream($"Test Stream {i}"))
            .ToList();
    }

    public static Client CreateTestClient(string name = "Test Client")
    {
        return new Client(
            name: name,
            macAddress: "00:11:22:33:44:55",
            ipAddress: "192.168.1.100",
            connected: true
        );
    }
}
```

## AI-Assisted Testing

### Test Generation Templates

#### Comprehensive Test Suite Generation

```
TEST SUITE GENERATION

Context: [Include component and business logic context]

Component: [ClassName]
Business Rules: [List key business rules and constraints]
Dependencies: [List external dependencies to mock]

Generate comprehensive test suite including:

1. Unit Tests:
   - Constructor validation tests
   - Method behavior tests (happy path)
   - Edge case and boundary tests
   - Error condition tests
   - State transition tests

2. Integration Tests:
   - Cross-layer interaction tests
   - Database operation tests
   - External service integration tests

3. Performance Tests:
   - Latency requirement validation
   - Memory usage tests
   - Concurrent operation tests

Requirements:
- Use AAA pattern consistently
- Include FluentAssertions for assertions
- Mock all external dependencies
- Follow naming convention: MethodName_StateUnderTest_ExpectedBehavior
- Include test data builders where appropriate
- Cover all public methods and properties
- Test both success and failure scenarios
```

#### Test Review and Enhancement

```
TEST REVIEW AND ENHANCEMENT

Existing Test Code: [Include current test code]

Review Focus:
1. Test coverage completeness
2. Test quality and readability
3. Proper mocking and isolation
4. Performance test adequacy
5. Error scenario coverage

Enhancement Suggestions:
- Identify missing test scenarios
- Improve test readability and maintainability
- Add performance validations where needed
- Enhance error testing coverage
- Suggest better test data setup patterns

Provide specific recommendations for test improvements.
```

## Continuous Testing

### Automated Test Execution

```yaml
# Test execution pipeline
stages:
  - fast-tests
  - integration-tests
  - performance-tests
  - e2e-tests

fast-tests:
  script:
    - dotnet test --filter "TestCategory=Unit" --collect:"XPlat Code Coverage"
  timeout: 5 minutes

integration-tests:
  script:
    - dotnet test --filter "TestCategory=Integration"
  timeout: 10 minutes
  dependencies:
    - fast-tests

performance-tests:
  script:
    - dotnet test --filter "TestCategory=Performance"
  timeout: 15 minutes
  dependencies:
    - integration-tests

e2e-tests:
  script:
    - dotnet test --filter "TestCategory=E2E"
  timeout: 20 minutes
  dependencies:
    - performance-tests
```

### Test Metrics and Reporting

- **Coverage Reports**: Detailed code coverage analysis
- **Performance Trends**: Historical performance tracking
- **Test Reliability**: Flaky test identification
- **Execution Time**: Test suite performance monitoring

## Validation Criteria

### Phase Completion Validation

Each phase must pass comprehensive testing validation:

```yaml
Phase Validation Requirements:
  Unit Test Coverage: >= 95%
  Integration Test Coverage: >= 80%
  Performance Tests: All passing within requirements
  E2E Tests: Critical workflows validated
  No Failing Tests: 100% pass rate required
  Code Quality: Meets all quality gates
```

This comprehensive testing and validation framework ensures SnapDog meets the highest quality standards while supporting efficient AI-assisted development.
