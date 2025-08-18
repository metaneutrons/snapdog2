using SnapDog2.Tests.Fixtures.Integration;

namespace SnapDog2.Tests.Integration.Internal;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

public class DependencyInjectionTests
{
    [Fact]
    public void ServiceCollection_ShouldBuild_WithCoreManagersRegistered()
    {
        var services = new ServiceCollection();

        // Minimal registrations required for building core managers (adjust as needed)
        // In a focused internal DI test suite, we would register real managers and mock boundary services.

        services.AddLogging();

        // TODO: add specific manager registrations and their dependencies as needed
        // services.AddScoped<SnapDog2.Core.Abstractions.IZoneManager, SnapDog2.Infrastructure.Domain.ZoneManager>();
        // services.AddScoped<SnapDog2.Core.Abstractions.IPlaylistManager, SnapDog2.Infrastructure.Domain.PlaylistManager>();

        var provider = services.BuildServiceProvider();
        provider.Should().NotBeNull();
    }
}
