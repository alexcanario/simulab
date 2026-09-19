using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Api.Authorization;

/// <summary>
/// Builds a policy on demand for any name starting with <see cref="PermissionPolicy.Prefix"/> (F-6,
/// BR3), so a new permission never needs a hand-written policy registration. Falls back to the default
/// provider for every other policy name (F-5's endpoints use none, but a future module might).
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PermissionPolicy.Prefix, StringComparison.Ordinal))
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var permission = policyName[PermissionPolicy.Prefix.Length..];
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
