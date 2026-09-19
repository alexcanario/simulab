namespace Simulab.Identity.Contracts;

/// <summary>A role a user holds, as a chip in the user list (F-9, UC5).</summary>
public sealed record UserRoleResponse(Guid Id, string Name, bool IsSystem);
