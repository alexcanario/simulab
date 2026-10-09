using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using MudBlazor;
using MudBlazor.Utilities;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-63: compares a <see cref="MudTheme"/> with the design tokens (<c>docs/design/identity.tokens.json</c>) and the
/// tokens with the front matter of <c>DESIGN.md</c>. Every method returns the mismatches, each naming the token, the
/// mode and both values, so the test that calls it can also be fed an altered theme and be seen to fail.
/// </summary>
internal static class IdentityTokensComparison
{
    /// <summary>Every palette colour the theme sets, with the hex of its light and its dark value as the theme resolves them.</summary>
    /// <remarks>A colour is "set" when it differs from a fresh <see cref="PaletteLight"/> / <see cref="PaletteDark"/> in at least one theme.</remarks>
    public static IReadOnlyDictionary<string, (string Light, string Dark)> ThemeColours(MudTheme theme)
    {
        var colours = new SortedDictionary<string, (string, string)>(StringComparer.Ordinal);
        foreach (var property in typeof(Palette).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(property => property.PropertyType == typeof(MudColor) || property.PropertyType == typeof(string)))
        {
            var light = Hex(property.GetValue(theme.PaletteLight)!.ToString()!);
            var dark = Hex(property.GetValue(theme.PaletteDark)!.ToString()!);
            var defaultLight = Hex(property.GetValue(new PaletteLight())!.ToString()!);
            var defaultDark = Hex(property.GetValue(new PaletteDark())!.ToString()!);
            if (light != defaultLight || dark != defaultDark)
            {
                colours[CamelCase(property.Name)] = (light, dark);
            }
        }

        return colours;
    }

    /// <summary>The colour tokens: light hex and dark hex by name; a missing part is an empty string.</summary>
    public static IReadOnlyDictionary<string, (string Light, string Dark)> TokenColours(JsonNode tokens)
    {
        var colours = new SortedDictionary<string, (string, string)>(StringComparer.Ordinal);
        foreach (var (name, token) in tokens["color"]!.AsObject().Where(entry => !entry.Key.StartsWith('$')))
        {
            colours[name] = (
                token?["$value"]?["hex"]?.GetValue<string>() ?? string.Empty,
                token?["$extensions"]?["mode"]?["dark"]?["hex"]?.GetValue<string>() ?? string.Empty);
        }

        return colours;
    }

    public static IReadOnlyList<string> CompareColours(MudTheme theme, JsonNode tokens)
    {
        var fromTheme = ThemeColours(theme);
        var fromTokens = TokenColours(tokens);
        var mismatches = new List<string>();

        foreach (var (name, (light, dark)) in fromTheme)
        {
            if (!fromTokens.TryGetValue(name, out var token))
            {
                mismatches.Add($"the theme sets '{name}' (light {light}, dark {dark}) but the tokens have no token '{name}'");
                continue;
            }

            if (token.Light != light)
            {
                mismatches.Add($"token '{name}' in the light theme: theme {light}, tokens {token.Light}");
            }

            if (token.Dark != dark)
            {
                mismatches.Add($"token '{name}' in the dark theme: theme {dark}, tokens {token.Dark}");
            }
        }

        mismatches.AddRange(fromTokens.Keys.Except(fromTheme.Keys)
            .Select(name => $"token '{name}' names no palette colour the theme sets"));
        return mismatches;
    }

    /// <summary>The font stack, the type scale the theme sets and the default radius, keyed like the tokens (<c>typography.h1.fontSize</c>).</summary>
    public static IReadOnlyDictionary<string, string> ThemeTypography(MudTheme theme)
    {
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["typography.fontFamily"] = string.Join(", ", theme.Typography.Default.FontFamily ?? []),
            ["shape.borderRadius"] = theme.LayoutProperties.DefaultBorderRadius,
        };

        var styles = new (string Name, BaseTypography Current, BaseTypography Default)[]
        {
            ("h1", theme.Typography.H1, new H1Typography()),
            ("h2", theme.Typography.H2, new H2Typography()),
            ("body1", theme.Typography.Body1, new Body1Typography()),
            ("body2", theme.Typography.Body2, new Body2Typography()),
            ("caption", theme.Typography.Caption, new CaptionTypography()),
        };

        foreach (var (name, current, standard) in styles)
        {
            AddIfSet(values, $"typography.{name}.fontSize", current.FontSize, standard.FontSize);
            AddIfSet(values, $"typography.{name}.fontWeight", current.FontWeight, standard.FontWeight);
            AddIfSet(values, $"typography.{name}.lineHeight", current.LineHeight, standard.LineHeight);
        }

        return values;
    }

    /// <summary>Every value of the tokens that is not a colour, keyed like <see cref="ThemeTypography"/>, plus the identity extensions.</summary>
    public static IReadOnlyDictionary<string, string> TokenValues(JsonNode tokens)
    {
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var typography = tokens["typography"]!.AsObject();
        values["typography.fontFamily"] = string.Join(", ", typography["fontFamily"]!["$value"]!.AsArray().Select(font => font!.GetValue<string>()));

        foreach (var (style, node) in typography.Where(entry => !entry.Key.StartsWith('$') && entry.Key != "fontFamily"))
        {
            foreach (var (property, token) in node!.AsObject())
            {
                values[$"typography.{style}.{property}"] = Scalar(token!["$value"]!);
            }
        }

        values["shape.borderRadius"] = Scalar(tokens["shape"]!["borderRadius"]!["$value"]!);
        return values;
    }

    public static IReadOnlyList<string> CompareTypography(MudTheme theme, JsonNode tokens)
    {
        var fromTheme = ThemeTypography(theme);
        var fromTokens = TokenValues(tokens);
        var mismatches = new List<string>();

        foreach (var (key, value) in fromTheme)
        {
            if (!fromTokens.TryGetValue(key, out var token))
            {
                mismatches.Add($"the theme sets '{key}' ({value}) but the tokens have no such token");
            }
            else if (token != value)
            {
                mismatches.Add($"token '{key}': theme {value}, tokens {token}");
            }
        }

        mismatches.AddRange(fromTokens.Keys.Except(fromTheme.Keys).Select(key => $"token '{key}' names nothing the theme sets"));
        return mismatches;
    }

    /// <summary>What DESIGN.md's front matter must hold: one value per token, named like the token, plus the identity extensions.</summary>
    public static IReadOnlyDictionary<string, string> ExpectedFrontMatter(JsonNode tokens)
    {
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, (light, dark)) in TokenColours(tokens))
        {
            values[$"color.{name}.light"] = light;
            values[$"color.{name}.dark"] = dark;
        }

        foreach (var (key, value) in TokenValues(tokens))
        {
            values[key] = value;
        }

        var agile = tokens["$extensions"]!["agile"]!;
        foreach (var key in new[] { "iconFamily", "source", "sourceRef", "date" })
        {
            values[key] = agile[key]!.GetValue<string>();
        }

        return values;
    }

    public static IReadOnlyList<string> CompareFrontMatter(IReadOnlyDictionary<string, string> expected, IReadOnlyDictionary<string, string> actual)
    {
        var mismatches = new List<string>();
        foreach (var (key, value) in expected)
        {
            if (!actual.TryGetValue(key, out var found))
            {
                mismatches.Add($"DESIGN.md front matter has no '{key}' (token {value})");
            }
            else if (found != value)
            {
                mismatches.Add($"DESIGN.md front matter '{key}': tokens {value}, DESIGN.md {found}");
            }
        }

        mismatches.AddRange(actual.Keys.Except(expected.Keys).Where(key => key != "name")
            .Select(key => $"DESIGN.md front matter '{key}' names no token"));
        return mismatches;
    }

    /// <summary>Reads the YAML front matter of a markdown file: nested <c>key:</c> groups of two-space indentation and <c>key: value</c> lines, keyed by the dotted path.</summary>
    public static IReadOnlyDictionary<string, string> ParseFrontMatter(string markdown)
    {
        var lines = markdown.ReplaceLineEndings("\n").Split('\n');
        lines[0].Should().Be("---", "DESIGN.md starts with its YAML front matter");
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var path = new List<string>();

        foreach (var line in lines.Skip(1).TakeWhile(line => line != "---"))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var depth = (line.Length - line.TrimStart().Length) / 2;
            var separator = line.IndexOf(':', StringComparison.Ordinal);
            var key = line[(depth * 2)..separator];
            var value = line[(separator + 1)..].Trim();

            path.RemoveRange(depth, path.Count - depth);
            if (value.Length == 0)
            {
                path.Add(key);
                continue;
            }

            values[string.Join('.', path.Append(key))] = value.Trim('"');
        }

        return values;
    }

    /// <summary>The 6-digit hex of an opaque colour, the 8-digit one of a translucent colour, as the theme resolves it.</summary>
    public static string Hex(string colour)
    {
        var (red, green, blue, alpha) = ColourContrast.Parse(colour);
        var alphaByte = (int)Math.Round(alpha * 255);
        var hex = string.Create(CultureInfo.InvariantCulture, $"#{(int)red:X2}{(int)green:X2}{(int)blue:X2}");
        return alphaByte == 255 ? hex : hex + alphaByte.ToString("X2", CultureInfo.InvariantCulture);
    }

    private static void AddIfSet(SortedDictionary<string, string> values, string key, string? current, string? standard)
    {
        if (!string.IsNullOrEmpty(current) && current != standard)
        {
            values[key] = current;
        }
    }

    /// <summary>A number as written ("700", "1.2") or a dimension as value plus unit ("1.75rem").</summary>
    private static string Scalar(JsonNode value) =>
        value is JsonObject dimension
            ? dimension["value"]!.ToJsonString() + dimension["unit"]!.GetValue<string>()
            : value.ToJsonString();

    private static string CamelCase(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}
