namespace Simulab.Identity.Contracts;

/// <summary>Creates or replaces a role (F-9, UC2-UC3): the whole permission set, never a patch (BR4).</summary>
public sealed record SaveRoleRequest(string? Name, IReadOnlyList<string>? Permissions);
