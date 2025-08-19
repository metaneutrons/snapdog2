using System.Net;
using System.Net.Sockets;
using SnapDog2.Tests.Fixtures.Containers;
using Xunit;

namespace SnapDog2.Tests.Integration.Attributes;

/// <summary>
/// Skips tests when UDP support is not available or when running in environments
/// with UDP limitations (e.g., Testcontainers UDP mapping issues).
/// </summary>
public class RequiresUdpSupportAttribute : FactAttribute
{
    public RequiresUdpSupportAttribute()
    {
        if (!IsUdpSupportAvailable())
        {
            this.Skip =
                "UDP support not available or Testcontainers UDP limitation detected. "
                + "KNX/IP protocol requires proper UDP port mapping which is not supported "
                + "by current Testcontainers version. See docs/investigation/FINDINGS-SUMMARY.md";
        }
    }

    private static bool IsUdpSupportAvailable()
    {
        // Check if we're running in a Testcontainers environment
        var isTestcontainers =
            Environment.GetEnvironmentVariable("TESTCONTAINERS") == "true"
            || Environment.GetEnvironmentVariable("CI") == "true"
            || IsRunningInTestcontainersContext();

        if (isTestcontainers)
        {
            // Skip KNX tests in Testcontainers due to UDP limitations
            return false;
        }

        // For local development, check if UDP is actually available
        return CanBindUdpPort();
    }

    private static bool IsRunningInTestcontainersContext()
    {
        // Detect if we're in a Testcontainers context by checking for container-related environment
        return Environment.GetEnvironmentVariable("DOCKER_HOST") != null
            || Environment.GetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED") != null
            || System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
    }

    private static bool CanBindUdpPort()
    {
        try
        {
            using var udpClient = new UdpClient(0);
            return udpClient.Client.LocalEndPoint != null;
        }
        catch
        {
            return false;
        }
    }
}
