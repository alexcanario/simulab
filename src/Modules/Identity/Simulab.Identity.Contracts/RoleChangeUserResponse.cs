namespace Simulab.Identity.Contracts;

/// <summary>An author or target of the role history (F-14, BR5). <paramref name="Email"/> is null for an erased account.</summary>
public sealed record RoleChangeUserResponse(Guid Id, string? Email);
