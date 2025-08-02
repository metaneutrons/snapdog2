namespace SnapDog2.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Test controller to discover available services.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestController"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public TestController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Lists available services.
    /// </summary>
    /// <returns>List of available services.</returns>
    [HttpGet("services")]
    public ActionResult<object> GetServices()
    {
        var services = new List<string>();
        
        // Try to find mediator-related services
        var serviceDescriptors = _serviceProvider.GetService<IServiceCollection>();
        
        return Ok(new { message = "Service discovery endpoint", timestamp = DateTime.UtcNow });
    }
}
