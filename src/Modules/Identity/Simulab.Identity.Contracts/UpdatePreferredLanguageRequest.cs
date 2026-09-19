namespace Simulab.Identity.Contracts;

/// <summary>F-8 UC3: the header language switch saves the language alone, so it never touches the name (BR6).</summary>
public sealed record UpdatePreferredLanguageRequest(string? PreferredLanguage);
