//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Infrastructure.Services;

using System.Reflection;
using Makaretu.Dns;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Shared.Configuration;

/// <summary>
/// Zeroconf/Bonjour service advertisement for network discovery.
/// </summary>
public partial class ZeroconfService : IHostedService, IDisposable
{
    private readonly ZeroconfConfig _config;
    private readonly SnapDogConfiguration _snapDogConfig;
    private readonly ILogger<ZeroconfService> _logger;
    private ServiceDiscovery? _serviceDiscovery;
    private bool _disposed;

    public ZeroconfService(
        IOptions<SnapDogConfiguration> snapDogConfig,
        ILogger<ZeroconfService> logger)
    {
        _snapDogConfig = snapDogConfig.Value;
        _config = _snapDogConfig.Zeroconf;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            this.LogZeroconfDisabled();
            return Task.CompletedTask;
        }

        try
        {
            _serviceDiscovery = new ServiceDiscovery();
            var instanceName = GetInstanceName();

            if (_config.AdvertiseApi)
            {
                var apiService = CreateApiService(instanceName);
                _serviceDiscovery.Advertise(apiService);
                this.LogServiceAdvertised("API", "_snapdog._tcp", _snapDogConfig.Http.HttpPort);
            }

            if (_config.AdvertiseWebUI)
            {
                var webuiService = CreateWebUIService(instanceName);
                _serviceDiscovery.Advertise(webuiService);
                this.LogServiceAdvertised("WebUI", "_http._tcp", _snapDogConfig.Http.HttpPort);
            }

            this.LogZeroconfStarted(instanceName);
        }
        catch (Exception ex)
        {
            this.LogZeroconfStartFailed(ex);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_serviceDiscovery != null)
        {
            _serviceDiscovery.Dispose();
            _serviceDiscovery = null;
            this.LogZeroconfStopped();
        }

        return Task.CompletedTask;
    }

    private string GetInstanceName()
    {
        if (!string.IsNullOrEmpty(_config.InstanceName))
        {
            return _config.InstanceName;
        }

        var hostname = Environment.MachineName;
        return $"SnapDog2-{hostname}";
    }

    private ServiceProfile CreateApiService(string instanceName)
    {
        return new ServiceProfile(instanceName, "_snapdog._tcp", (ushort)_snapDogConfig.Http.HttpPort);
    }

    private ServiceProfile CreateWebUIService(string instanceName)
    {
        return new ServiceProfile($"{instanceName} WebUI", "_http._tcp", (ushort)_snapDogConfig.Http.HttpPort);
    }

    private static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString(3) ?? "2.0.0";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _serviceDiscovery?.Dispose();
            _disposed = true;
        }
    }

    #region LoggerMessage Methods

    [LoggerMessage(EventId = 14188, Level = LogLevel.Information, Message = "Zeroconf service advertisement disabled")]
    private partial void LogZeroconfDisabled();

    [LoggerMessage(EventId = 14189, Level = LogLevel.Information, Message = "Zeroconf started - advertising as '{InstanceName}'")]
    private partial void LogZeroconfStarted(string instanceName);

    [LoggerMessage(EventId = 14190, Level = LogLevel.Information, Message = "Advertising {ServiceType} service '{ServiceName}' on port {Port}")]
    private partial void LogServiceAdvertised(string serviceType, string serviceName, int port);

    [LoggerMessage(EventId = 14191, Level = LogLevel.Information, Message = "Zeroconf service advertisement stopped")]
    private partial void LogZeroconfStopped();

    [LoggerMessage(EventId = 14192, Level = LogLevel.Error, Message = "Failed to start Zeroconf service advertisement")]
    private partial void LogZeroconfStartFailed(Exception ex);

    #endregion
}
