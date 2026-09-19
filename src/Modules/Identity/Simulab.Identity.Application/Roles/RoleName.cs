using Simulab.Identity.Contracts;

namespace Simulab.Identity.Application.Roles;

/// <summary>F-9, BR3: a role name is trimmed and must then be 2 to 50 characters long.</summary>
public static class RoleName
{
    /// <summary>The trimmed name when it is valid; null otherwise.</summary>
    public static string? Normalize(string? name)
    {
        var trimmed = name?.Trim();
        return trimmed is { Length: >= RoleLimits.NameMinLength and <= RoleLimits.NameMaxLength } ? trimmed : null;
    }
}
