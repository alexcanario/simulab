using System.Text.RegularExpressions;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-125 AC8 (BR8): the two families of the brand are files of the app, never a third-party host, each <c>@font-face</c>
/// names its weight, and the OFL licence of each family sits next to its files. Body text is Bricolage Grotesque 500,
/// titles 800, code and numbers JetBrains Mono. <see cref="BrandAssetsHostTests"/> fetches the files through the host.
/// </summary>
public sealed partial class BrandFontTests
{
    public static readonly (string Family, string Weight, string File)[] Faces =
    [
        ("Bricolage Grotesque", "500", "bricolage-grotesque-latin-500-normal.woff2"),
        ("Bricolage Grotesque", "800", "bricolage-grotesque-latin-800-normal.woff2"),
        ("JetBrains Mono", "500", "jetbrains-mono-latin-500-normal.woff2"),
        ("JetBrains Mono", "700", "jetbrains-mono-latin-700-normal.woff2"),
    ];

    [Fact]
    public void FontFaces_AreTheFourFilesOfTheAppEachWithItsWeight()
    {
        var faces = FontFacePattern().Matches(Css())
            .Select(match => (
                Family: Property(match.Value, "font-family").Trim('"'),
                Weight: Property(match.Value, "font-weight"),
                Source: Property(match.Value, "src")))
            .ToList();

        faces.Select(face => (face.Family, face.Weight)).Should().BeEquivalentTo(Faces.Select(face => (face.Family, face.Weight)));
        foreach (var (family, weight, file) in Faces)
        {
            var face = faces.Single(candidate => candidate.Family == family && candidate.Weight == weight);
            face.Source.Should().Be($"url(\"fonts/{file}\") format(\"woff2\")", "the font is served by the app, never by another host");
            File.Exists(Path.Combine(WwwRoot(), "fonts", file)).Should().BeTrue(file);
        }
    }

    [Theory]
    [InlineData("OFL-BricolageGrotesque.txt")]
    [InlineData("OFL-JetBrainsMono.txt")]
    public void Licences_SitNextToTheFonts(string licence)
    {
        var path = Path.Combine(WwwRoot(), "fonts", licence);

        File.Exists(path).Should().BeTrue();
        File.ReadAllText(path).Should().Contain("SIL OPEN FONT LICENSE");
    }

    [Fact]
    public void Theme_BodyIsBricolage500AndTitlesAre800()
    {
        var typography = SimulabTheme.Create().Typography;

        typography.Default.FontFamily.Should().Equal("Bricolage Grotesque", "system-ui", "sans-serif");
        typography.Default.FontWeight.Should().Be("500");
        typography.H1.FontWeight.Should().Be("800");
        typography.H2.FontWeight.Should().Be("800");
        typography.H3.FontWeight.Should().Be("800");
    }

    [Fact]
    public void Mono_IsJetBrainsMonoForCodeNumbersAndShortcuts()
    {
        AppCssColours.Declaration(":root", "--app-font-mono").Should().Be("\"JetBrains Mono\", ui-monospace, Consolas, monospace");
        foreach (var selector in new[] { "code, kbd, pre, samp", ".app-permission-technical", ".app-security-secret" })
        {
            AppCssColours.Declaration(selector, "font-family").Should().Be("var(--app-font-mono)", selector);
        }
    }

    [Fact]
    public void Stylesheet_LoadsNoFontFromAnotherHost()
    {
        Css().Should().NotContainAny("fonts.googleapis", "fonts.gstatic", "@import");
    }

    private static string Property(string block, string name) =>
        Regex.Match(block, $@"{Regex.Escape(name)}\s*:\s*([^;]+);").Groups[1].Value.Trim();

    private static string Css() => File.ReadAllText(Path.Combine(WwwRoot(), "app.css"));

    private static string WwwRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests must run inside the repository");
        return Path.Combine(directory!.FullName, "src", "Hosts", "Simulab.Web", "wwwroot");
    }

    [GeneratedRegex(@"@font-face\s*\{[^}]*\}")]
    private static partial Regex FontFacePattern();
}
