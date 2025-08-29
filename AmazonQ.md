# Amazon Q Development Patterns for SnapDog2

## MCP Graph Access Patterns

### When to Read from Graph

- **Session start**: Read existing patterns and context to understand current state
- **Context lost**: Retrieve previous insights and established patterns
- **Before major changes**: Check for existing solutions and known issues
- **Pattern recognition**: Search for similar problems and their solutions

### When to Update Graph

- **New insights discovered**: Document successful patterns and approaches
- **Problem-solution pairs**: Record issues encountered and their fixes
- **Code patterns established**: Save reusable templates and structures
- **Performance optimizations**: Track what works and what doesn't
- **Build/compilation fixes**: Document error patterns and solutions

### Graph Operations

```bash
# Read context at start
semantic_search("SnapDog2 logging patterns")
semantic_search("compilation errors solutions")

# Update with new insights
create_entities([{
  "name": "LoggerMessage Pattern",
  "entityType": "CodePattern",
  "observations": ["Template matching requires exact case", "Nullable params prevent CS8604"]
}])
```

## LoggerMessage Pattern (High-Performance Logging)

### Required Structure

```csharp
public partial class MyService
{
    private readonly ILogger<MyService> _logger;

    // LoggerMessage methods at end of class
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Template with {Param1}")]
    private partial void LogMethodName(string Param1);
}
```

### Conversion Rules

1. **Template matching**: `{ZoneId}` requires parameter named `ZoneId` (exact case)
2. **EventId**: Sequential per class (1, 2, 3...)
3. **Nullable parameters**: Use `string?` for nullable values
4. **LogLevel conflicts**: Use `Microsoft.Extensions.Logging.LogLevel`

### Pattern Examples

```csharp
// Before
_logger.LogInformation("Zone {ZoneId} failed: {Error}", zoneIndex, error);

// After
[LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Zone {ZoneId} failed: {Error}")]
private partial void LogZoneFailed(int ZoneId, string? Error);

LogZoneFailed(zoneIndex, error);
```

### Build Verification

```bash
dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet
# Must show: 0 Warnung(en), 0 Fehler
```
