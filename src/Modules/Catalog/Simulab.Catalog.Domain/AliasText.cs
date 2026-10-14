namespace Simulab.Catalog.Domain;

/// <summary>An other name as the request typed it, trimmed, with the form the catalog compares it by (F-77).</summary>
/// <param name="Name">The text as typed, trimmed.</param>
/// <param name="NormalizedName">The text without case or accents.</param>
public sealed record AliasText(string Name, string NormalizedName);
