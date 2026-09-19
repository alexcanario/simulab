namespace Simulab.Identity.Contracts;

/// <summary>F-8 UC2: the whole editable profile. A blank name clears it (BR2).</summary>
public sealed record UpdateProfileRequest(string? FullName, string? PreferredLanguage);
