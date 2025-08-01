using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Configuration; // for ApiConfiguration

namespace SnapDog2.Api.Authentication
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddSnapDogApiKeyAuthentication(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            var authSection = configuration.GetSection("ApiAuthentication");
#pragma warning disable CS8601 // Possible null reference assignment
            var authConfig =
                authSection.Get<ApiConfiguration.ApiAuthSettings>() ?? new ApiConfiguration.ApiAuthSettings();
#pragma warning restore CS8601

            if (authConfig.Enabled && authConfig.ApiKeys?.Any() == true)
            {
                services.AddSingleton(authConfig);
                services
                    .AddAuthentication(static options =>
                    {
                        options.DefaultAuthenticateScheme = ApiKeyAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = ApiKeyAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                        "ApiKey",
                        static options => { }
                    );

                services.AddAuthorization(static options =>
                {
                    options.AddPolicy(
                        "ApiKeyPolicy",
                        static policy =>
                        {
                            policy.AddAuthenticationSchemes("ApiKey");
                            policy.RequireAuthenticatedUser();
                        }
                    );
                    options.DefaultPolicy = options.GetPolicy("ApiKeyPolicy")!; // non-null asserted
                    options.FallbackPolicy = options.GetPolicy("ApiKeyPolicy")!; // non-null asserted
                });
            }

            return services;
        }
    }
}
