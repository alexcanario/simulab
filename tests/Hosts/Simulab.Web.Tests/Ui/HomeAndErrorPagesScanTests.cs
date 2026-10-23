using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-134 AC13 and AC14 (BR8, BR10): the pages of the item use the kit and the theme only — one amber action on the whole
/// item (the error page), no literal colour in a page or in the stylesheet rules written for it, so the contrast the theme
/// tests measure is the contrast of these pages. AC15 (BR11): the new texts exist in every locale.
/// </summary>
public sealed partial class HomeAndErrorPagesScanTests
{
    private static readonly string[] Pages =
    [
        "Components/Pages/Home.razor", "Components/Pages/Error.razor", "Components/Pages/NotFound.razor",
        "Components/Layout/NotFoundContent.razor", "Components/Pages/Dev/AiDiagnostics.razor",
        "Components/Ui/AppPageState.razor"
    ];

    [Fact]
    public void OnlyTheErrorPage_UsesTheAmberAction()
    {
        var users = Pages.Where(page => File.ReadAllText(Path.Combine(WebRoot(), page)).Contains("<AppActionButton", StringComparison.Ordinal)).ToArray();

        users.Should().Equal("Components/Pages/Error.razor");
        Pages.Where(page => File.ReadAllText(Path.Combine(WebRoot(), page)).Contains("Color.Tertiary", StringComparison.Ordinal))
            .Should().BeEmpty("the amber colour reaches a page only through the action component");
    }

    [Fact]
    public void PagesAndTheirStylesheetRules_CarryNoLiteralColour()
    {
        var offenders = Pages
            .Where(page => LiteralColour().IsMatch(File.ReadAllText(Path.Combine(WebRoot(), page))))
            .ToList();

        var css = File.ReadAllText(Path.Combine(WebRoot(), "wwwroot", "app.css"));
        offenders.AddRange(RulesOfTheItem().Matches(css)
            .Where(rule => LiteralColour().IsMatch(rule.Groups["body"].Value))
            .Select(rule => "app.css " + rule.Groups["selector"].Value.Trim()));

        RulesOfTheItem().Matches(css).Should().NotBeEmpty("the item writes rules for its page state, its cards and its metrics");
        offenders.Should().BeEmpty();
    }

    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    private static readonly string[] NewKeys =
    [
        "Home.SignIn", "Home.SignUp", "Home.Welcome", "Common.GoHome",
        "Gallery.States.PageState", "Gallery.States.PageStateTitle", "Gallery.States.PageStateMessage",
        "AiDiagnostics.Question", "AiDiagnostics.Metric.Model", "AiDiagnostics.Metric.Tokens",
        "AiDiagnostics.Metric.Cost", "AiDiagnostics.Metric.Duration",
        "AiDiagnostics.Metric.TokensValue", "AiDiagnostics.Metric.DurationValue"
    ];

    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void TheNewTexts_ExistInEveryLocale(string culture)
    {
        foreach (var key in NewKeys)
        {
            Manager.GetString(key, new CultureInfo(culture)).Should().NotBeNullOrWhiteSpace($"'{key}' in {culture}");
        }
    }

    [Fact]
    public void TheNewTexts_AreTranslatedNotCopiedIntoPortuguese()
    {
        Manager.GetString("Home.Welcome", new CultureInfo("pt-BR")).Should().NotBe(Manager.GetString("Home.Welcome", new CultureInfo("en")));
        Manager.GetString("Common.GoHome", new CultureInfo("pt-PT")).Should().NotBe(Manager.GetString("Common.GoHome", new CultureInfo("en")));
    }

    private static string WebRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests must run inside the repository");
        return Path.Combine(directory!.FullName, "src", "Hosts", "Simulab.Web");
    }

    [GeneratedRegex(@"(?<![\w&])#[0-9a-fA-F]{3,8}\b|\brgba?\(|\bhsla?\(")]
    private static partial Regex LiteralColour();

    [GeneratedRegex(@"(?<selector>[^{}]*(?:app-page-state|app-home|ai-metric)[^{}]*)\{(?<body>[^}]*)\}")]
    private static partial Regex RulesOfTheItem();
}
