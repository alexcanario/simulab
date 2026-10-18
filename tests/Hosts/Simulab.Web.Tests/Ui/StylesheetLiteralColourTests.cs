using System.Text.RegularExpressions;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-125 AC3 (BR2, F-83): no stylesheet of the app carries a colour of its own; each one reads a theme variable. The one
/// exception is the white quiet zone a QR code needs to scan in both themes (<c>.app-security-qr</c>).
/// <c>transparent</c>, <c>inherit</c> and <c>currentColor</c> name no colour and stay allowed.
/// </summary>
public sealed partial class StylesheetLiteralColourTests
{
    private static readonly string[] NamedColours =
    [
        "aliceblue", "antiquewhite", "aqua", "aquamarine", "azure", "beige", "bisque", "black", "blanchedalmond", "blue",
        "blueviolet", "brown", "burlywood", "cadetblue", "chartreuse", "chocolate", "coral", "cornflowerblue", "cornsilk",
        "crimson", "cyan", "darkblue", "darkcyan", "darkgoldenrod", "darkgray", "darkgreen", "darkgrey", "darkkhaki",
        "darkmagenta", "darkolivegreen", "darkorange", "darkorchid", "darkred", "darksalmon", "darkseagreen", "darkslateblue",
        "darkslategray", "darkslategrey", "darkturquoise", "darkviolet", "deeppink", "deepskyblue", "dimgray", "dimgrey",
        "dodgerblue", "firebrick", "floralwhite", "forestgreen", "fuchsia", "gainsboro", "ghostwhite", "gold", "goldenrod",
        "gray", "green", "greenyellow", "grey", "honeydew", "hotpink", "indianred", "indigo", "ivory", "khaki", "lavender",
        "lavenderblush", "lawngreen", "lemonchiffon", "lightblue", "lightcoral", "lightcyan", "lightgoldenrodyellow",
        "lightgray", "lightgreen", "lightgrey", "lightpink", "lightsalmon", "lightseagreen", "lightskyblue",
        "lightslategray", "lightslategrey", "lightsteelblue", "lightyellow", "lime", "limegreen", "linen", "magenta",
        "maroon", "mediumaquamarine", "mediumblue", "mediumorchid", "mediumpurple", "mediumseagreen", "mediumslateblue",
        "mediumspringgreen", "mediumturquoise", "mediumvioletred", "midnightblue", "mintcream", "mistyrose", "moccasin",
        "navajowhite", "navy", "oldlace", "olive", "olivedrab", "orange", "orangered", "orchid", "palegoldenrod",
        "palegreen", "paleturquoise", "palevioletred", "papayawhip", "peachpuff", "peru", "pink", "plum", "powderblue",
        "purple", "rebeccapurple", "red", "rosybrown", "royalblue", "saddlebrown", "salmon", "sandybrown", "seagreen",
        "seashell", "sienna", "silver", "skyblue", "slateblue", "slategray", "slategrey", "snow", "springgreen",
        "steelblue", "tan", "teal", "thistle", "tomato", "turquoise", "violet", "wheat", "white", "whitesmoke", "yellow",
        "yellowgreen",
    ];

    private static readonly string QrQuietZone = ".app-security-qr sets background: #ffffff";

    [Fact]
    public void Stylesheets_HoldNoLiteralColourButTheQrQuietZone()
    {
        var found = Stylesheets().SelectMany(file => FindLiterals(File.ReadAllText(file)).Select(literal => $"{Path.GetFileName(file)}: {literal}")).ToList();

        found.Should().Equal($"app.css: {QrQuietZone}");
    }

    /// <summary>The rules F-83 named: validation outlines and message, the error boundary and the checkbox border.</summary>
    [Theory]
    [InlineData(".valid.modified:not([type=checkbox])", "outline", "var(--mud-palette-success)")]
    [InlineData(".invalid", "outline", "var(--mud-palette-error)")]
    [InlineData(".validation-message", "color", "var(--mud-palette-error)")]
    [InlineData(".blazor-error-boundary", "color", "var(--mud-palette-error-text)")]
    [InlineData(".darker-border-checkbox.form-check-input", "border-color", "var(--mud-palette-lines-inputs)")]
    public void F83Rules_ReadTheTheme(string selector, string property, string variable)
    {
        AppCssColours.Declaration(selector, property).Should().Contain(variable);
    }

    [Fact]
    public void FindLiterals_NamesHexFunctionsAndNamedColoursInValuesOnly()
    {
        const string css = """
            .a { color: #fff; white-space: nowrap; }
            .b { background: rgba(0, 0, 0, 0.2); border: 1px solid var(--mud-palette-divider); }
            .c { background: lightyellow; color: transparent; fill: currentColor; }
            .d { background: url(data:image/svg+xml;base64,PHN2ZyByZWQ=) no-repeat; }
            """;

        FindLiterals(css).Should().Equal(".a sets color: #fff", ".b sets background: rgba(0, 0, 0, 0.2)", ".c sets background: lightyellow");
    }

    internal static IReadOnlyList<string> FindLiterals(string css)
    {
        var withoutComments = CommentPattern().Replace(css, string.Empty);
        var found = new List<string>();
        foreach (Match rule in RulePattern().Matches(withoutComments))
        {
            var selector = string.Join(' ', rule.Groups[1].Value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();
            // A data URL carries ';' and ':' of its own; it is a picture, not a colour declaration.
            foreach (Match declaration in DeclarationPattern().Matches(UrlPattern().Replace(rule.Groups[2].Value, "url()")))
            {
                var value = declaration.Groups[2].Value;
                if (HexPattern().IsMatch(value) || FunctionPattern().IsMatch(value)
                    || WordPattern().Matches(value).Any(word => NamedColours.Contains(word.Value.ToLowerInvariant())))
                {
                    found.Add($"{selector} sets {declaration.Groups[1].Value}: {declaration.Groups[2].Value.Trim()}");
                }
            }
        }

        return found;
    }

    private static IEnumerable<string> Stylesheets()
    {
        var web = Path.Combine(RepositoryRoot(), "src", "Hosts", "Simulab.Web");
        return new[] { Path.Combine(web, "wwwroot", "app.css") }
            .Concat(Directory.EnumerateFiles(Path.Combine(web, "Components"), "*.razor.css", SearchOption.AllDirectories).Order(StringComparer.Ordinal));
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

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentPattern();

    [GeneratedRegex(@"([^{}]+)\{([^{}]*)\}")]
    private static partial Regex RulePattern();

    [GeneratedRegex(@"([a-z-]+)\s*:\s*([^;}]+)")]
    private static partial Regex DeclarationPattern();

    [GeneratedRegex(@"url\([^)]*\)")]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b")]
    private static partial Regex HexPattern();

    [GeneratedRegex(@"\b(rgba?|hsla?|hwb|lab|lch|oklab|oklch)\(")]
    private static partial Regex FunctionPattern();

    [GeneratedRegex(@"(?<![\w-])[a-zA-Z]+(?![\w-])")]
    private static partial Regex WordPattern();
}
