namespace SnapDog2.Shared.Attributes;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using SnapDog2.Shared.Configuration;

/// <summary>
/// Conditional authorization attribute that respects the API auth configuration.
/// </summary>
public class ConditionalAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var config = context.HttpContext.RequestServices.GetService<IOptions<SnapDogConfiguration>>();

        // Skip authorization completely if API auth is disabled
        if (config?.Value.Http.ApiAuthEnabled == false)
        {
            // Allow the request to proceed without any authorization checks
            return;
        }

        // Apply standard authorization if enabled
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
