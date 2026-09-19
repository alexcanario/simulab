namespace Simulab.Identity.Contracts;

/// <summary>
/// The languages the app ships in (ADR-0001 #25), shared by the Api and the Web. A preferred language
/// must be one of them (F-8 BR3).
/// </summary>
public static class SupportedLanguages
{
    public const string Default = "en";

    public static readonly IReadOnlyList<string> All = ["en", "pt-BR", "pt-PT"];

    /// <summary>The canonical form of <paramref name="value"/> (<c>PT-br</c> becomes <c>pt-BR</c>), or null when it is not supported.</summary>
    public static string? Canonical(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : All.FirstOrDefault(language => string.Equals(language, value.Trim(), StringComparison.OrdinalIgnoreCase));
}
