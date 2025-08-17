using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Abstractions;
using SnapDog2.Tests.Integration.Fixtures;
using SnapDog2.Tests.Testing;
using Xunit;

namespace SnapDog2.Tests.Integration.Api;

[Collection("IntegrationTests")] // reuse existing fixture collection if defined
public class NotificationPipelineMetricsTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public NotificationPipelineMetricsTests(IntegrationTestFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task Zone_state_changes_are_enqueued_and_processed_and_metrics_updated()
    {
        // Arrange
        var services = this._fixture.ServiceProvider;
        var metrics = (TestMetricsService)services.GetRequiredService<IMetricsService>();

        // Act: enqueue a small burst to exercise queue and background service
        var queue = services.GetRequiredService<INotificationQueue>();
        for (int i = 0; i < 5; i++)
        {
            await queue.EnqueueZoneAsync("test.status", 1, new { ok = true, i }, CancellationToken.None);
        }

        // Give background service time to process
        await Task.Delay(1500);

        // Assert: counters moved and gauge is non-negative
        metrics.Counters.TryGetValue("notifications_enqueued_total", out var enq);
        metrics.Counters.TryGetValue("notifications_dequeued_total", out var deq);
        metrics.Counters.TryGetValue("notifications_processed_total", out var processed);

        enq.Should().BeGreaterThan(0);
        deq.Should().BeGreaterThan(0);
        processed.Should().BeGreaterThan(0);

        metrics.Gauges.TryGetValue("notifications_queue_depth", out var depth);
        depth.Should().BeGreaterThanOrEqualTo(0.0);
    }
}
