using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using MudBlazor;
using Simulab.Web.Theme;

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

    /// <summary>F-125: the top-level rules, and each <c>@media</c> block's own rules by its condition.</summary>
    private static readonly (string TopLevel, IReadOnlyList<(string Condition, string Body)> Media) Split = SplitMedia(Css);

    /// <summary>The value of <paramref name="property"/> inside the top-level rule whose selector list contains <paramref name="selector"/>.</summary>
    public static string Declaration(string selector, string property) =>
        DeclarationIn(Split.TopLevel, selector, property, "app.css");

    /// <summary>F-125: the same, inside the <c>@media</c> block whose condition contains <paramref name="condition"/>.</summary>
    public static string MediaDeclaration(string condition, string selector, string property)
    {
        var blocks = Split.Media.Where(block => block.Condition.Contains(condition, StringComparison.Ordinal)).Select(block => block.Body).ToArray();
        blocks.Should().NotBeEmpty($"app.css must have an @media block for '{condition}'");
        var bodies = blocks.SelectMany(block => Rules(block).Where(rule => Names(rule, selector))).ToArray();
        bodies.Should().ContainSingle($"the @media ({condition}) blocks must have exactly one rule for '{selector}'");
        return Value(bodies[0].Body, selector, property);
    }

    /// <summary>
    /// Turns a declaration into a colour the contrast helper understands. A <c>var(--mud-palette-x)</c> is read
    /// from <paramref name="palette"/> by its own name, so a MudBlazor default counts exactly as a token the
    /// project set; MudBlazor's <c>-text</c> names (<c>--mud-palette-primary-text</c>) are the palette's contrast texts.
    /// <c>inherit</c> resolves to <paramref name="inherited"/>, the colour the parent gives it, and
    /// <c>color-mix(in srgb, X N%, transparent)</c> to X at that opacity (F-125).
    /// </summary>
    public static string Resolve(string declaration, Palette palette, string? inherited = null)
    {
        var value = declaration.Replace("!important", string.Empty, StringComparison.Ordinal).Trim();
        if (value.Equals("inherit", StringComparison.OrdinalIgnoreCase))
        {
            inherited.Should().NotBeNull("an inherited colour needs the colour of its parent to be measured");
            return inherited!;
        }

        var mix = MixPattern().Match(value);
        if (mix.Success)
        {
            var (red, green, blue, alpha) = ColourContrast.Parse(Resolve(mix.Groups[1].Value, palette, inherited));
            var share = double.Parse(mix.Groups[2].Value, CultureInfo.InvariantCulture) / 100;
            return string.Create(CultureInfo.InvariantCulture, $"rgba({red},{green},{blue},{alpha * share})");
        }

        var app = AppTokenPattern().Match(value);
        if (app.Success)
        {
            return AppToken(app.Groups[1].Value, palette is PaletteDark);
        }

        var variable = TokenPattern().Match(value);
        if (!variable.Success)
        {
            return value;
        }

        var name = string.Concat(variable.Groups[1].Value.Split('-')
            .Select(part => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(part)));
        var property = typeof(Palette).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? (name.EndsWith("Text", StringComparison.Ordinal)
                ? typeof(Palette).GetProperty(name[..^"Text".Length] + "ContrastText", BindingFlags.Public | BindingFlags.Instance)
                : null);
        property.Should().NotBeNull($"'{variable.Groups[1].Value}' must be a palette token");
        return property!.GetValue(palette)!.ToString()!;
    }

    /// <summary>F-148: the variables <c>SimulabThemeProvider</c> writes next to the palette (<c>--app-link</c>, <c>--app-brand-name</c>, the keycap edges), for the theme in use.</summary>
    public static string AppToken(string name, bool dark)
    {
        var edges = dark ? SimulabTheme.DarkKeycapEdges : SimulabTheme.LightKeycapEdges;
        return name switch
        {
            "link" => dark ? SimulabTheme.DarkLink : SimulabTheme.LightLink,
            "brand-name" => dark ? SimulabTheme.DarkBrandName : SimulabTheme.LightBrandName,
            "primary-selected" => dark ? SimulabTheme.DarkPrimarySelected : SimulabTheme.LightPrimarySelected,
            "keycap-primary-edge" => edges.Primary,
            "keycap-secondary-edge" => edges.Secondary,
            "keycap-tertiary-edge" => edges.Tertiary,
            "keycap-info-edge" => edges.Info,
            "keycap-error-edge" => edges.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "not a variable of SimulabThemeProvider"),
        };
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

    /// <summary>A rule is found by one of its selectors, or by its whole selector list written as in the sheet (F-125).</summary>
    private static bool Names((string[] Selectors, string Body) rule, string selector) =>
        rule.Selectors.Contains(selector, StringComparer.Ordinal)
        || string.Equals(string.Join(", ", rule.Selectors), selector, StringComparison.Ordinal);

    private static string DeclarationIn(string css, string selector, string property, string where)
    {
        var bodies = Rules(css)
            .Where(candidate => Names(candidate, selector))
            .Select(candidate => candidate.Body)
            .ToArray();
        bodies.Should().ContainSingle($"{where} must still have exactly one rule for '{selector}'");
        return Value(bodies[0], selector, property);
    }

    private static string Value(string body, string selector, string property)
    {
        var match = Regex.Match(body, $@"(?m)^\s*{Regex.Escape(property)}\s*:\s*([^;]+);");
        match.Success.Should().BeTrue($"the rule for '{selector}' must still declare '{property}'");
        return match.Groups[1].Value.Trim();
    }

    private static IEnumerable<(string[] Selectors, string Body)> Rules(string css)
    {
        foreach (Match match in RulePattern().Matches(css))
        {
            var selectors = SplitSelectors(match.Groups[1].Value)
                .Select(selector => string.Join(' ', selector.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
                .Where(selector => selector.Length > 0)
                .ToArray();
            yield return (selectors, match.Groups[2].Value);
        }
    }

    /// <summary>Splits a selector list at its top-level commas: the commas of <c>:is(a, button)</c> stay inside their selector.</summary>
    private static IEnumerable<string> SplitSelectors(string list)
    {
        var depth = 0;
        var start = 0;
        for (var index = 0; index < list.Length; index++)
        {
            depth += list[index] switch { '(' => 1, ')' => -1, _ => 0 };
            if (list[index] == ',' && depth == 0)
            {
                yield return list[start..index];
                start = index + 1;
            }
        }

        yield return list[start..];
    }

    /// <summary>Cuts every <c>@media</c> block out of the sheet, matching its braces, and keeps it apart with its condition.</summary>
    private static (string, IReadOnlyList<(string, string)>) SplitMedia(string css)
    {
        var media = new List<(string, string)>();
        var topLevel = new System.Text.StringBuilder();
        var position = 0;
        while (true)
        {
            var start = css.IndexOf("@media", position, StringComparison.Ordinal);
            if (start < 0)
            {
                topLevel.Append(css, position, css.Length - position);
                break;
            }

            topLevel.Append(css, position, start - position);
            var open = css.IndexOf('{', start);
            var depth = 1;
            var index = open + 1;
            while (depth > 0)
            {
                depth += css[index] switch { '{' => 1, '}' => -1, _ => 0 };
                index++;
            }

            media.Add((css[(start + "@media".Length)..open].Trim(), css[(open + 1)..(index - 1)]));
            position = index;
        }

        return (topLevel.ToString(), media);
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

    [GeneratedRegex(@"^var\(\s*--app-(link|brand-name|primary-selected|keycap-[a-z]+-edge)\s*\)$")]
    private static partial Regex AppTokenPattern();

    [GeneratedRegex(@"^color-mix\(\s*in srgb\s*,\s*(var\([^)]+\))\s+([0-9.]+)%\s*,\s*transparent\s*\)$")]
    private static partial Regex MixPattern();
}
