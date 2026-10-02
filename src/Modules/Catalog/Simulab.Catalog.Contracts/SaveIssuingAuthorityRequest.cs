namespace Simulab.Catalog.Contracts;

/// <summary>
/// What the add and the edit dialog send (F-34 BR18, v2). The issuing authority has no kind: nothing
/// filters or groups by one yet, and it arrives when a screen needs it.
/// </summary>
/// <param name="Name">The body's full name, 2 to 150 characters.</param>
/// <param name="Description">Free text, optional.</param>
/// <param name="Website">An absolute http or https address, optional.</param>
public sealed record SaveIssuingAuthorityRequest(
    string? Name,
    string? Description = null,
    string? Website = null);
