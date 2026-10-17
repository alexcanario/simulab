using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// F-124: the pt-BR app manual is the only one maintained. The pt-PT and en folders keep every page, each marked frozen with
/// the date and the app version and a link to the pt-BR page; the UI locales are not touched.
/// </summary>
public partial class FrozenManualTests
{
    private static readonly string[] FrozenLocales = ["pt-PT", "en"];

    public static TheoryData<string, string> FrozenPages
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var locale in FrozenLocales)
            {
                foreach (var slug in Slugs("pt-BR"))
                {
                    data.Add(locale, slug);
                }
            }

            return data;
        }
    }

    public static TheoryData<string> ReferencePages
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var slug in Slugs("pt-BR"))
            {
                data.Add(slug);
            }

            return data;
        }
    }

    [Fact]
    public void ManualLocales_ListsPtBrAlone_AndUiLocalesAreUntouched()
    {
        var lines = File.ReadAllLines(Path.Combine(SolutionAssemblies.RepositoryRoot(), "CLAUDE.md"));

        lines.Should().Contain("- Manual locales: pt-BR");
        lines.Should().Contain(line => line.StartsWith("- UI locales: pt-BR, pt-PT, en.", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("en")]
    public void FrozenFolder_HoldsTheSameSlugsAsPtBr(string locale)
    {
        Slugs(locale).Should().BeEquivalentTo(Slugs("pt-BR"));
    }

    [Fact]
    public void ReferenceFolder_Holds23Pages()
    {
        Slugs("pt-BR").Should().HaveCount(23);
    }

    [Theory]
    [MemberData(nameof(FrozenPages))]
    public void FrozenPage_CarriesTheFrozenKeyAndTheNotice(string locale, string slug)
    {
        var lines = File.ReadAllLines(PagePath(locale, slug));

        var frontMatterEnd = Array.IndexOf(lines, "---", 1);
        var frozen = lines.Take(frontMatterEnd).Select(line => FrozenKey().Match(line)).SingleOrDefault(match => match.Success);
        frozen.Should().NotBeNull($"{locale}/{slug}.md needs `frozen: <YYYY-MM-DD> (v<version>)` in its front matter");

        var title = frontMatterEnd + 1;
        lines[title].Should().StartWith("# ");

        var notice = lines.Skip(title + 1).First(line => line.Length > 0);
        var date = frozen!.Groups["date"].Value;
        var version = frozen.Groups["version"].Value;
        var expected = locale == "en"
            ? $"> Frozen on {date} (v{version}). This page may be out of date; see the [pt-BR version](../pt-BR/{slug}.md)."
            : $"> Congelada em {date} (v{version}). Esta página pode estar desatualizada; consulte a [versão em pt-BR](../pt-BR/{slug}.md).";
        notice.Should().Be(expected);

        File.Exists(PagePath("pt-BR", slug)).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ReferencePages))]
    public void PtBrPage_HasNoFrozenKey(string slug)
    {
        var lines = File.ReadAllLines(PagePath("pt-BR", slug));

        var frontMatterEnd = Array.IndexOf(lines, "---", 1);
        lines.Take(frontMatterEnd).Should().NotContain(line => line.StartsWith("frozen:", StringComparison.Ordinal));
    }

    private static string ManualRoot() => Path.Combine(SolutionAssemblies.RepositoryRoot(), "docs", "manual");

    private static string PagePath(string locale, string slug) => Path.Combine(ManualRoot(), locale, slug + ".md");

    private static string[] Slugs(string locale) =>
        Directory.GetFiles(Path.Combine(ManualRoot(), locale), "*.md")
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Order(StringComparer.Ordinal)
            .ToArray();

    [GeneratedRegex(@"^frozen: (?<date>\d{4}-\d{2}-\d{2}) \(v(?<version>\d+\.\d+\.\d+)\)$")]
    private static partial Regex FrozenKey();
}
