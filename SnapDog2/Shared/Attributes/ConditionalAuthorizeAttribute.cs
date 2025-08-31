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

        // Skip authorization if API auth is disabled
        if (config?.Value.Http.ApiAuthEnabled == false)
        {
            return;
        }

        // Apply authorization if enabled
        var authService = context.HttpContext.RequestServices.GetService<IAuthorizationService>();
        if (authService == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
