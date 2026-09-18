using System.Globalization;

namespace Simulab.Web.Localization;

/// <summary>UI languages. Adding a language means adding resource files and one entry here.</summary>
public static class SupportedCultures
{
    public const string Default = "en";

    public static readonly IReadOnlyList<CultureInfo> All =
    [
        new("en"),
        new("pt-BR"),
        new("pt-PT")
    ];

    public static bool IsSupported(string? name) =>
        All.Any(culture => string.Equals(culture.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Only a path inside the app is accepted as a return address.</summary>
    public static string SafeLocalPath(string? redirectUri) =>
        string.IsNullOrEmpty(redirectUri)
        || redirectUri[0] != '/'
        || (redirectUri.Length > 1 && redirectUri[1] is '/' or '\\')
            ? "/"
            : redirectUri;
}
