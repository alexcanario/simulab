using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using MudBlazor;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// Reads a colour declaration out of <c>app.css</c> and resolves it against the theme, so a contrast test
/// measures the colour the screen really paints and not one the test repeats by hand.
/// <para>
/// B-16 and B-17 both hid in this gap: <see cref="ThemeContrastTests"/> checked the pairs the palette declares,
/// while the stylesheet painted the field hint and the selected navigation item with tokens nobody had measured
/// — one of them (<c>text-disabled</c>) a MudBlazor default that SimulabTheme never sets.
/// </para>
/// </summary>
internal static partial class AppCssColours
{
    private static readonly string Css = CommentPattern().Replace(
        File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hosts", "Simulab.Web", "wwwroot", "app.css")),
        string.Empty);

    /// <summary>The value of <paramref name="property"/> inside the rule whose selector list contains <paramref name="selector"/>.</summary>
    public static string Declaration(string selector, string property)
    {
        var bodies = Rules()
            .Where(candidate => candidate.Selectors.Contains(selector, StringComparer.Ordinal))
            .Select(candidate => candidate.Body)
            .ToArray();
        bodies.Should().ContainSingle($"app.css must still have exactly one rule for '{selector}'");

        var match = Regex.Match(bodies[0], $@"(?m)^\s*{Regex.Escape(property)}\s*:\s*([^;]+);");
        match.Success.Should().BeTrue($"the rule for '{selector}' must still declare '{property}'");
        return match.Groups[1].Value.Trim();
    }

    /// <summary>
    /// Turns a declaration into a colour the contrast helper understands. A <c>var(--mud-palette-x)</c> is read
    /// from <paramref name="palette"/> by its own name, so a MudBlazor default counts exactly as a token the
    /// project set. <c>inherit</c> resolves to <paramref name="inherited"/>, the colour the parent gives it.
    /// </summary>
    public static string Resolve(string declaration, Palette palette, string? inherited = null)
    {
        var value = declaration.Trim();
        if (value.Equals("inherit", StringComparison.OrdinalIgnoreCase))
        {
            inherited.Should().NotBeNull("an inherited colour needs the colour of its parent to be measured");
            return inherited!;
        }

        var variable = TokenPattern().Match(value);
        if (!variable.Success)
        {
            return value;
        }

        var name = string.Concat(variable.Groups[1].Value.Split('-')
            .Select(part => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(part)));
        var property = typeof(Palette).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull($"'{variable.Groups[1].Value}' must be a palette token");
        return property!.GetValue(palette)!.ToString()!;
    }

    /// <summary>A translucent colour painted over <paramref name="under"/>, as the browser composites it.</summary>
    public static string Over(string colour, string under)
    {
        var (red, green, blue, _) = ColourContrast.Parse(under);
        var (overRed, overGreen, overBlue, alpha) = ColourContrast.Parse(colour);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"rgb({(overRed * alpha) + (red * (1 - alpha))},{(overGreen * alpha) + (green * (1 - alpha))},{(overBlue * alpha) + (blue * (1 - alpha))})");
    }

    private static IEnumerable<(string[] Selectors, string Body)> Rules()
    {
        foreach (Match match in RulePattern().Matches(Css))
        {
            var selectors = match.Groups[1].Value
                .Split(',')
                .Select(selector => selector.Trim())
                .Where(selector => selector.Length > 0)
                .ToArray();
            yield return (selectors, match.Groups[2].Value);
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests must run inside the repository");
        return directory!.FullName;
    }

    [GeneratedRegex(@"([^{}]+)\{([^{}]*)\}")]
    private static partial Regex RulePattern();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentPattern();

    [GeneratedRegex(@"var\(\s*--mud-palette-([a-z0-9-]+)\s*\)")]
    private static partial Regex TokenPattern();
}
