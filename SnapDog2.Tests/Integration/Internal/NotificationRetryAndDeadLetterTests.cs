using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SnapDog2.Core.Abstractions;
using SnapDog2.Tests.Testing;
using Xunit;

namespace SnapDog2.Tests.Integration.Internal;

public class NotificationRetryAndDeadLetterTests
{
    [Fact]
    public async Task Notifications_retry_and_dead_letter_when_publish_fails()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // WebApplicationFactory provides IWebHostBuilder; set environment via configuration
            builder.ConfigureAppConfiguration((ctx, cfg) => { ctx.HostingEnvironment.EnvironmentName = "Testing"; });
            builder.ConfigureServices(services =>
            {
                // Replace MQTT with failing implementation
                var mqttDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMqttService));
                if (mqttDescriptor != null)
                {
                    services.Remove(mqttDescriptor);
                }
                services.AddSingleton<IMqttService, TestMqttServiceFailing>();

                // Replace metrics with capture
                var metricsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMetricsService));
                if (metricsDescriptor != null)
                {
                    services.Remove(metricsDescriptor);
                }
                services.AddSingleton<IMetricsService, TestMetricsService>();

                // Configure notification processing for fast retries
                services.Configure<SnapDog2.Core.Configuration.NotificationProcessingOptions>(o =>
                {
                    o.MaxRetryAttempts = 2; // quicker
                    o.RetryBaseDelayMs = 50;
                    o.RetryMaxDelayMs = 200;
                });
            });
        });

        var sp = factory.Services;
        var metrics = (TestMetricsService)sp.GetRequiredService<IMetricsService>();
        var queue = sp.GetRequiredService<INotificationQueue>();

        // enqueue a dummy event
        await queue.EnqueueZoneAsync("test.status", 1, new { ok = true }, CancellationToken.None);

        // allow background service to retry and dead-letter
        await Task.Delay(1000);

        // assertions: retried and then dead-letter incremented
        metrics.Counters.TryGetValue("notifications_retried_total", out var retried);
        metrics.Counters.TryGetValue("notifications_dead_letter_total", out var dead);
        retried.Should().BeGreaterThan(0);
        dead.Should().BeGreaterThan(0);
    }
}

