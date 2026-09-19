namespace Simulab.Identity.Contracts;

/// <summary>The whole set of roles a user holds after the call (F-9, BR6); an empty set is allowed.</summary>
public sealed record SetUserRolesRequest(IReadOnlyList<Guid>? RoleIds);
