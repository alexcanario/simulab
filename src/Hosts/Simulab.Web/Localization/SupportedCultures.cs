using System.Globalization;
using Simulab.Identity.Contracts;

namespace Simulab.Web.Localization;

/// <summary>UI languages, from the one list the Api also uses (<see cref="SupportedLanguages"/>, F-8).</summary>
public static class SupportedCultures
{
    public const string Default = SupportedLanguages.Default;

    public static readonly IReadOnlyList<CultureInfo> All = [.. SupportedLanguages.All.Select(name => new CultureInfo(name))];

    /// <summary>A language's own name, capitalized in that language ("Português (Brasil)"): how a language picker shows it.</summary>
    public static string NativeName(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var name = culture.NativeName;
        return name.Length == 0 ? name : char.ToUpper(name[0], culture) + name[1..];
    }

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
