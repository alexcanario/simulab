using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Simulab.Identity.Api.Authorization;

/// <summary>
/// The dynamic permission-policy pipeline (F-6, BR3-BR4). Kept in this project, never in Infrastructure:
/// these are ASP.NET Core HTTP-pipeline types, and the profile's dependency rule runs one way only
/// (Api may reference Infrastructure for DI; Infrastructure never references Api).
/// </summary>
public static class IdentityAuthorizationModule
{
    public static IServiceCollection AddIdentityAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, PermissionForbiddenResultHandler>();

        return services;
    }
}
