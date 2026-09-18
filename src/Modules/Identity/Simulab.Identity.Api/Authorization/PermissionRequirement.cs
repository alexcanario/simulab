using Microsoft.AspNetCore.Authorization;

namespace Simulab.Identity.Api.Authorization;

/// <summary>One named permission (F-6, BR3). <see cref="PermissionPolicyProvider"/> builds one policy per name, on demand.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
