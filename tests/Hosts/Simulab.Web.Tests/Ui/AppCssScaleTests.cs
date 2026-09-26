using System.Text.RegularExpressions;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-43 BR5: the visual language is three scales on <c>:root</c>, and every <c>.app-*</c> rule reads them. A
/// length literal that a scale step already covers is a drift waiting to happen — the kit had nine different
/// spacings for the same idea before this. A value the scales do not cover is allowed and says so in a comment
/// on the rule.
/// </summary>
public sealed partial class AppCssScaleTests
{
    private static readonly string[] SpaceProperties =
    [
        "padding", "padding-top", "padding-bottom", "padding-left", "padding-right",
        "margin", "margin-top", "margin-bottom", "margin-left", "margin-right",
        "gap", "row-gap", "column-gap"
    ];

    /// <summary>Every step of the three scales, in both units a 16px root makes equal.</summary>
    private static readonly Dictionary<string, string[]> Covered = new()
    {
        ["font-size"] = ["0.75rem", "0.8125rem", "0.875rem", "0.9375rem", "1rem", "1.375rem"],
        ["space"] = ["0.25rem", "4px", "0.5rem", "8px", "0.75rem", "12px", "1rem", "16px", "1.5rem", "24px", "2rem", "32px", "2.5rem", "40px"],
        ["border-radius"] = ["6px", "10px", "16px"]
    };

    private static string Stylesheet() => File.ReadAllText(
        Path.Combine(RepositoryRoot(), "src", "Hosts", "Simulab.Web", "wwwroot", "app.css"));

    internal static IReadOnlyList<string> FindLiterals(string css)
    {
        var withoutComments = CommentPattern().Replace(css, string.Empty);
        var found = new List<string>();

        foreach (Match rule in RulePattern().Matches(withoutComments))
        {
            var selector = string.Join(' ', rule.Groups[1].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (!selector.Contains(".app-", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match declaration in DeclarationPattern().Matches(rule.Groups[2].Value))
            {
                var property = declaration.Groups[1].Value;
                var value = declaration.Groups[2].Value.Trim();
                var steps = property == "font-size" || property == "border-radius"
                    ? Covered.GetValueOrDefault(property)
                    : SpaceProperties.Contains(property) ? Covered["space"] : null;

                if (steps is null || value.Contains("var(", StringComparison.Ordinal))
                {
                    continue;
                }

                found.AddRange(value.Split(' ')
                    .Where(part => steps.Contains(part))
                    .Select(part => $"{selector} sets {property}: {part}"));
            }
        }

        return found;
    }

    [Fact]
    public void AppRules_UseTheScalesInsteadOfALengthTheyCover()
    {
        FindLiterals(Stylesheet()).Should()
            .BeEmpty("every .app-* rule reads --app-font-*, --app-space-* or --app-radius-* (F-43 BR5)");
    }

    [Fact]
    public void FindLiterals_ARuleWithALiteralTheScaleCovers_NamesTheRuleAndTheValue()
    {
        const string css = """
            .app-thing {
                padding: 16px;
                gap: var(--app-space-2);
                font-size: 0.875rem;
                /* a value no step covers stays, with its reason */
                letter-spacing: 0.04em;
                width: 13px;
            }
            .not-a-kit-rule {
                padding: 16px;
            }
            """;

        FindLiterals(css).Should().Equal(
            ".app-thing sets padding: 16px",
            ".app-thing sets font-size: 0.875rem");
    }

    /// <summary>The three scales must exist before a rule can read them.</summary>
    [Fact]
    public void Root_DeclaresEveryScaleStep()
    {
        var css = Stylesheet();

        foreach (var token in new[] { "--app-font-xs", "--app-font-xl", "--app-space-1", "--app-space-7", "--app-radius-sm", "--app-radius-pill" })
        {
            css.Should().Contain($"{token}:", $"{token} is part of the visual language");
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

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentPattern();

    [GeneratedRegex(@"([^{}]+)\{([^{}]*)\}")]
    private static partial Regex RulePattern();

    [GeneratedRegex(@"(?m)^\s*([a-z-]+)\s*:\s*([^;]+);")]
    private static partial Regex DeclarationPattern();
}
