namespace Simulab.Identity.Contracts;

/// <summary>The seed role names (F-6). First real consumer of the mechanism; F-9 replaces this with full role management.</summary>
public sealed record RoleNamesResponse(IReadOnlyList<string> Names);
