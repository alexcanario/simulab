using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Attempts;

/// <summary>
/// F-169 AC19: every text of the three screens exists in pt-BR, pt-PT and en. The keys are read out of the screens' own
/// sources, so a key a screen asks for and no resource file holds fails here, whichever screen it is on.
/// </summary>
public sealed partial class AttemptResourcesTests
{
    private static readonly string[] Cultures = ["en", "pt-BR", "pt-PT"];

    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    private static readonly string[] ScreenSources =
    [
        "Components/Pages/Attempts/TakeAttempt.razor",
        "Components/Pages/Attempts/AttemptSubmitted.razor",
        "Components/Pages/Catalog/EditionTakeButton.razor",
        "Components/Pages/Catalog/CatalogExam.razor",
        "Components/Layout/ExamSessionLayout.razor",
        "Components/Ui/AppAnswerSheet.razor",
        "Components/Ui/AppSaveStatus.razor"
    ];

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

    public static TheoryData<string> Sources() => new(ScreenSources);

    public static TheoryData<string> CultureNames() => new(Cultures);

    [Theory]
    [MemberData(nameof(Sources))]
    public void EveryKeyAScreenAsksFor_ExistsInEachLanguage(string source)
    {
        var keys = KeyPattern().Matches(File.ReadAllText(Path.Combine(WebRoot(), source))).Select(match => match.Groups[1].Value).Distinct().ToList();

        keys.Should().NotBeEmpty($"{source} shows texts");
        foreach (var culture in Cultures)
        {
            var missing = keys.Where(key => string.IsNullOrWhiteSpace(Manager.GetString(key, new CultureInfo(culture)))).ToList();
            missing.Should().BeEmpty($"{source} asks for keys that {culture} lacks");
        }
    }

    // The satellite files hold their own text: a key is never left as the English one in pt-BR or pt-PT.
    [Theory]
    [MemberData(nameof(CultureNames))]
    public void TheAttemptKeys_AreTranslated_NotCopiedFromEnglish(string culture)
    {
        if (culture == "en")
        {
            return;
        }

        var keys = new[]
        {
            "Catalog.Editions.Take", "Attempt.Leave", "Attempt.Question.SingleHint", "Attempt.Save.Saved", "Attempt.Nav.Next",
            "Attempt.AnswerSheet.Title", "Attempt.Submit.Action", "Attempt.Submit.Blanks", "Attempt.Submitted.Title"
        };

        foreach (var key in keys)
        {
            Manager.GetString(key, new CultureInfo(culture)).Should().NotBe(Manager.GetString(key, new CultureInfo("en")), $"'{key}' in {culture}");
        }
    }

    // pt-BR and pt-PT are different texts where the vocabulary differs (salvar and guardar, enviar and submeter).
    [Fact]
    public void PortugueseOfBrazilAndOfPortugal_KeepTheirOwnVocabulary()
    {
        static string Text(string key, string culture) => Manager.GetString(key, new CultureInfo(culture))!;

        Text("Attempt.Save.Saved", "pt-BR").Should().Be("Resposta salva");
        Text("Attempt.Save.Saved", "pt-PT").Should().Be("Resposta guardada");
        Text("Attempt.Submit.Action", "pt-BR").Should().Be("Enviar prova");
        Text("Attempt.Submit.Action", "pt-PT").Should().Be("Submeter prova");
        Text("Attempt.AnswerSheet.Title", "pt-BR").Should().Be("Cartão-resposta");
        Text("Attempt.AnswerSheet.Title", "pt-PT").Should().Be("Folha de respostas");
    }

    // Values go in as placeholders, and a locale keeps every placeholder of the English text.
    [Theory]
    [InlineData("Attempt.PageTitle", 2)]
    [InlineData("Attempt.Section.Range", 2)]
    [InlineData("Attempt.Submit.Blanks", 2)]
    [InlineData("Attempt.Submitted.Message", 3)]
    [InlineData("Attempt.AnswerSheet.SectionRange", 2)]
    [InlineData("Catalog.Editions.Take.Name", 1)]
    public void TheTextsWithValues_KeepEveryPlaceholderInEachLanguage(string key, int placeholders)
    {
        foreach (var culture in Cultures)
        {
            var text = Manager.GetString(key, new CultureInfo(culture))!;
            for (var index = 0; index < placeholders; index++)
            {
                text.Should().Contain("{" + index.ToString(CultureInfo.InvariantCulture) + "}", $"'{key}' in {culture}");
            }
        }
    }

    [GeneratedRegex(@"\bL\[""([A-Za-z0-9_.]+)""")]
    private static partial Regex KeyPattern();
}
