# ADR-001: Async Disposal Workaround in Scoped Services

## Status
Accepted (Temporary)

## Context
The .NET dependency injection container's `ServiceScope.Dispose()` method only supports synchronous disposal, but some of our services (`ZoneManager`, `MediaPlayerService`) implement only `IAsyncDisposable` for proper resource cleanup.

When `StatePublishingService` (a `BackgroundService`) creates scopes to access scoped services like `IMediator`, the scope disposal fails with:
```
'ServiceName' type only implements IAsyncDisposable. Use DisposeAsync to dispose the container.
```

## Decision
**Temporary Solution:** Add synchronous `IDisposable` implementations to async-only services that call `DisposeAsync().GetAwaiter().GetResult()`.

**Services Modified:**
- `ZoneManager` - Now implements both `IDisposable` and `IAsyncDisposable`
- `MediaPlayerService` - Now implements both `IDisposable` and `IAsyncDisposable`

## Consequences

### Positive
- ✅ Application starts and runs without critical errors
- ✅ Proper resource cleanup still occurs
- ✅ Maintains existing async disposal benefits where possible
- ✅ Minimal code changes required

### Negative
- ❌ **Risk of deadlocks** in complex async contexts
- ❌ Blocks calling thread during disposal
- ❌ Not architecturally pure - mixing sync/async patterns
- ❌ Technical debt that needs future resolution

## Future Improvements
1. **Custom ServiceScope**: Implement async-aware service scope
2. **Lifecycle Refactoring**: Use `IHostedService` instead of `BackgroundService` for state publishing
3. **Scope Avoidance**: Restructure to avoid disposing scoped services in background services
4. **Framework Updates**: Monitor .NET runtime improvements for native async scope disposal

## References
- [.NET Runtime Issue #61132](https://github.com/dotnet/runtime/issues/61132)
- [Microsoft Docs: IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.iasyncdisposable)
- [Background Services Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)

## Review Date
Target: Q2 2025 (when .NET 10 async DI improvements may be available)
