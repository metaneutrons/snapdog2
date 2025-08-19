# Solution for ContinuousMonitoring_ShouldDetectAndCorrectManualChanges Test

## Root Cause Analysis

### Issue 1: Timing Race Condition
- Containers start successfully but Snapcast clients need time to connect
- Test runs before clients establish connections with server
- SnapDog2 application starts immediately but clients aren't ready

### Issue 2: Stream Configuration Mismatch  
- Snapcast server configured with: `Zone1`, `Zone2`, `default`
- SnapDog2 expects: `/snapsinks/zone1`, `/snapsinks/zone2`
- Prevents proper zone-to-stream mapping

### Issue 3: No Connection Validation
- Test assumes clients are ready immediately after container startup
- No validation that clients have actually connected to server

## Proposed Solution

### 1. Fix Stream Configuration
Update TestcontainersFixture to use correct stream names:

```csharp
.WithCommand(
    "--tcp.enabled=true",
    "--http.enabled=true", 
    $"--http.port={httpPort}",
    $"--tcp.port={jsonRpcPort}",
    // Use correct stream names that match SnapDog2 configuration
    "--source=pipe:///tmp/zone1.fifo?name=/snapsinks/zone1",
    "--source=pipe:///tmp/zone2.fifo?name=/snapsinks/zone2",
    "--source=pipe:///tmp/default.fifo?name=default"
)
```

### 2. Add Client Connection Validation
Add helper method to wait for clients to connect:

```csharp
private async Task<bool> WaitForClientsToConnectAsync(TimeSpan timeout)
{
    var startTime = DateTime.UtcNow;
    
    while (DateTime.UtcNow - startTime < timeout)
    {
        try
        {
            var serverStatus = await GetSnapcastServerStatusAsync();
            var connectedClients = serverStatus.Groups
                .SelectMany(g => g.Clients)
                .Count(c => c.Connected);
                
            if (connectedClients >= 3) // All 3 clients connected
            {
                _output.WriteLine($"✅ All {connectedClients} clients connected after {(DateTime.UtcNow - startTime).TotalSeconds:F1}s");
                return true;
            }
            
            _output.WriteLine($"⏳ Waiting for clients to connect... ({connectedClients}/3 connected)");
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"⚠️ Error checking client connections: {ex.Message}");
            await Task.Delay(2000);
        }
    }
    
    return false;
}
```

### 3. Update Test Method
Modify the test to wait for proper setup:

```csharp
[Fact]
[TestPriority(2)]
public async Task ContinuousMonitoring_ShouldDetectAndCorrectManualChanges()
{
    // Arrange
    _output.WriteLine("🧪 Testing continuous monitoring - automatic correction of manual changes");

    // CRITICAL: Wait for clients to connect before starting test
    _output.WriteLine("⏳ Waiting for Snapcast clients to connect...");
    var clientsConnected = await WaitForClientsToConnectAsync(TimeSpan.FromSeconds(30));
    clientsConnected.Should().BeTrue("All Snapcast clients should connect within 30 seconds");

    // Additional wait for SnapDog2 to detect and process the clients
    _output.WriteLine("⏳ Allowing time for SnapDog2 to detect clients...");
    await Task.Delay(5000);

    using var scope = _integrationFixture.ServiceProvider.CreateScope();
    var zoneGroupingService = scope.ServiceProvider.GetRequiredService<IZoneGroupingService>();

    // Verify we have a good initial state with clients
    var initialStatus = await zoneGroupingService.GetZoneGroupingStatusAsync();
    initialStatus.Should().BeSuccessful();
    initialStatus.Value!.TotalClients.Should().BeGreaterThan(0, "Should have detected clients");
    
    _output.WriteLine($"📊 Initial state: {initialStatus.Value.TotalClients} clients, {initialStatus.Value.HealthyZones} healthy zones");

    // Rest of test continues...
}
```

### 4. Improve Error Handling
Add better diagnostics when test fails:

```csharp
private async Task LogSnapcastStatusAsync()
{
    try
    {
        var status = await GetSnapcastServerStatusAsync();
        _output.WriteLine($"📊 Snapcast Status:");
        _output.WriteLine($"   Groups: {status.Groups?.Count ?? 0}");
        
        foreach (var group in status.Groups ?? Enumerable.Empty<SnapcastGroupInfo>())
        {
            _output.WriteLine($"   Group {group.Id}: {group.Clients?.Count ?? 0} clients, Stream: {group.StreamId}");
            foreach (var client in group.Clients ?? Enumerable.Empty<SnapcastClientInfo>())
            {
                _output.WriteLine($"     Client {client.Id}: Connected={client.Connected}");
            }
        }
    }
    catch (Exception ex)
    {
        _output.WriteLine($"❌ Failed to get Snapcast status: {ex.Message}");
    }
}
```

### 5. Alternative: Mock-Based Approach
If container timing continues to be problematic, consider a mock-based test:

```csharp
[Fact]
public async Task ContinuousMonitoring_MockedScenario_ShouldDetectAndCorrectManualChanges()
{
    // Use mocked Snapcast service with predictable behavior
    // This eliminates container timing issues while still testing the core logic
}
```

## Implementation Priority

1. **High Priority**: Fix stream configuration (quick win)
2. **High Priority**: Add client connection validation 
3. **Medium Priority**: Improve error diagnostics
4. **Low Priority**: Consider mock-based alternative if issues persist

## Expected Outcome

After implementing these fixes:
- ✅ Clients will connect reliably to Snapcast server
- ✅ Stream names will match SnapDog2 expectations  
- ✅ Test will wait for proper setup before executing
- ✅ Better diagnostics will help debug future issues
- ✅ Integration test will pass consistently
