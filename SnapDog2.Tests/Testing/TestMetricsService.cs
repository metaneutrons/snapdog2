namespace SnapDog2.Tests.Testing;

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;

public class TestMetricsService : IMetricsService
{
    public ConcurrentDictionary<string, long> Counters { get; } = new();
    public ConcurrentDictionary<string, double> Gauges { get; } = new();

    public void RecordCortexMediatorRequestDuration(
        string requestType,
        string requestName,
        long durationMs,
        bool success
    )
    {
        // not used in these tests
    }

    public Task<ServerStats> GetServerStatsAsync()
    {
        return Task.FromResult(
            new ServerStats
            {
                CpuUsagePercent = 0,
                MemoryUsageMb = 0,
                TotalMemoryMb = 0,
                Uptime = TimeSpan.Zero,
            }
        );
    }

    public void IncrementCounter(string name, long delta = 1, params (string Key, string Value)[] labels)
    {
        this.Counters.AddOrUpdate(name, delta, (_, v) => v + delta);
    }

    public void SetGauge(string name, double value, params (string Key, string Value)[] labels)
    {
        this.Gauges[name] = value;
    }
}
