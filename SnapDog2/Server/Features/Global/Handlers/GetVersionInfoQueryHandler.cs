namespace SnapDog2.Server.Features.Global.Handlers;

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Queries;

/// <summary>
/// Handles the GetVersionInfoQuery.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GetVersionInfoQueryHandler"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public partial class GetVersionInfoQueryHandler(ILogger<GetVersionInfoQueryHandler> logger)
    : IQueryHandler<GetVersionInfoQuery, Result<VersionDetails>>
{
    private readonly ILogger<GetVersionInfoQueryHandler> _logger = logger;

    /// <inheritdoc/>
    public async Task<Result<VersionDetails>> Handle(GetVersionInfoQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling();

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var buildDate = File.GetLastWriteTime(assembly.Location);

            var versionDetails = new VersionDetails
            {
                Version = version,
                BuildDateUtc = buildDate,
                BuildConfiguration = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            };

            return await Task.FromResult(Result<VersionDetails>.Success(versionDetails)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.LogError(ex);
            return Result<VersionDetails>.Failure(ex);
        }
    }

    [LoggerMessage(1003, LogLevel.Information, "Handling GetVersionInfoQuery")]
    private partial void LogHandling();

    [LoggerMessage(1004, LogLevel.Error, "Error retrieving version information")]
    private partial void LogError(Exception ex);
}
