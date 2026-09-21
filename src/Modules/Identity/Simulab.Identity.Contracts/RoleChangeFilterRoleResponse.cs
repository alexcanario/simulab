namespace Simulab.Identity.Contracts;

/// <summary>A role the history can be filtered by (F-14, BR8), deleted ones included and marked.</summary>
public sealed record RoleChangeFilterRoleResponse(Guid Id, string Name, bool IsSystem, bool IsDeleted);
