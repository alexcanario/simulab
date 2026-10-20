using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-102 `/admin/base-texts`: the list of base texts, its search and language filter, the row actions and the
/// states the screen design lists (AC8, AC10 screen part). The rules are the Api's and have their own tests.
/// </summary>
public sealed class BaseTextsPageTests : QuestionPageTestContext
{
    private IRenderedComponent<BaseTexts> RenderPage() => Render<BaseTexts>();

    private void SeedThree() => Api.SeedBaseTexts(PacificText, ClinicalCase, UnusedTariffs);

    private static void WaitForRows(IRenderedComponent<BaseTexts> page, int count) =>
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(count));

    private static IReadOnlyList<string> CellsOf(IRenderedComponent<BaseTexts> page, int column) =>
        [.. page.FindAll("tbody tr").Select(row => row.QuerySelectorAll("td")[column].TextContent.Trim())];

    private static string Action(Guid id, string key) => $"#base-text-actions-{id}-{key}";

    private NavigationManager Navigation() => Services.GetRequiredService<NavigationManager>();

    private static readonly string[] AllLanguages = ["All languages"];

    private static Task FilterByLanguageAsync(IRenderedComponent<BaseTexts> page, string language) =>
        page.InvokeAsync(() => page.FindComponents<AppSelectField<string>>()
            .Single(component => component.Instance.Id == "base-texts-filter-language")
            .Instance.ValueChanged.InvokeAsync(language));

    // AC8: title, language and the number of questions, sorted by title.
    [Fact]
    public void Load_ShowsTitleLanguageAndQuestionCount_SortedByTitle()
    {
        SeedThree();

        var page = RenderPage();

        WaitForRows(page, 3);
        CellsOf(page, 0).Should().Equal(
            "Caso clínico - UBS do bairro",
            "Tabela de tarifas de energia 2025",
            "Text 1 - The Great Pacific Garbage Patch");
        CellsOf(page, 1).Should().Equal(
            QuestionText.LanguageName("pt-BR"), QuestionText.LanguageName("pt-BR"), QuestionText.LanguageName("en"));
        CellsOf(page, 2).Should().Equal("3", "0", "5");
        page.FindAll("tbody tr")[0].QuerySelectorAll("td")[1].QuerySelector("[lang]")!.GetAttribute("lang").Should().Be("pt-BR");
        page.FindAll("tbody tr")[2].QuerySelector(".app-truncate")!.GetAttribute("lang").Should().Be("en");
        page.FindAll(".sortable-column-header").Should().BeEmpty("the order is by title and no column sorts");
    }

    // The title is plain text: there is no read-only detail, the form is reached by Edit.
    [Fact]
    public void Title_IsNotALink()
    {
        SeedThree();
        var page = RenderPage();
        WaitForRows(page, 3);

        page.FindAll("tbody tr td a").Should().BeEmpty();
    }

    // AC8: the search narrows by title and goes to the server as search.
    [Fact]
    public void Search_SendsTheWordToTheServerAsSearch_AndNarrowsTheRows()
    {
        SeedThree();
        var page = RenderPage();
        WaitForRows(page, 3);

        page.Find(".app-table-search input").Input("tarifas");
        WaitForAfterDebounce(page, () => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Get && call.Path == "/api/v1/question-bank/base-texts"
            && call.Query!.Contains("search=tarifas", StringComparison.Ordinal)));
        WaitForRows(page, 1);
        CellsOf(page, 0).Should().Equal("Tabela de tarifas de energia 2025");
    }

    // AC8: the language filter goes to the server as language and starts again from the first page.
    [Fact]
    public async Task Filter_ByLanguage_SendsTheTagAndNarrowsTheRows()
    {
        SeedThree();
        var page = RenderPage();
        WaitForRows(page, 3);

        await FilterByLanguageAsync(page, "en");

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Get && call.Query!.Contains("language=en", StringComparison.Ordinal)));
        WaitForRows(page, 1);
        CellsOf(page, 0).Should().Equal("Text 1 - The Great Pacific Garbage Patch");
    }

    [Fact]
    public void Filter_OffersAllLanguagesFirstThenTheThreeLanguagesByTheirOwnName()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindComponents<AppSelectField<string>>().Should().ContainSingle());

        var options = page.FindComponent<AppSelectField<string>>().Instance.Options;

        var own = Simulab.Web.Localization.SupportedCultures.All
            .Select(Simulab.Web.Localization.SupportedCultures.NativeName);
        options.Select(option => option.Text).Should().Equal(AllLanguages.Concat(own));
        options[0].Value.Should().BeEmpty("an empty value is the unfiltered list");
    }

    [Fact]
    public async Task Filter_ByLanguageWithNoText_ShowsTheFilterEmptyState()
    {
        Api.SeedBaseTexts(ClinicalCase);
        var page = RenderPage();
        WaitForRows(page, 1);

        await FilterByLanguageAsync(page, "en");

        page.WaitForAssertion(() => page.Markup.Should().Contain("No base text in this language."));
    }

    [Fact]
    public void Empty_ShowsTheMessageWithAdd()
    {
        Api.SeedBaseTexts();

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("No base text yet."));
        page.FindAll(".app-state-empty .app-empty-action").Should().ContainSingle("the empty state offers Add");
    }

    [Fact]
    public void Empty_ForASearch_NamesTheTerm()
    {
        SeedThree();
        var page = RenderPage();
        WaitForRows(page, 3);

        page.Find(".app-table-search input").Input("zzzz");
        WaitForAfterDebounce(page, () => page.Markup.Should().Contain("No base text matches").And.Contain("zzzz"));
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.BaseTextListFailure = (HttpStatusCode.InternalServerError, "common.unexpected_error");

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
        page.Markup.Should().Contain("Try again");
    }

    // BR6: a text that a question links has Delete disabled, with the count in its reason; a free one can go.
    [Fact]
    public void Row_InUse_DeleteIsDisabledWithTheCount_AndAFreeTextCanBeDeleted()
    {
        SeedThree();
        var page = RenderPage();
        WaitForRows(page, 3);

        page.Find(Action(ClinicalCase.Id, "delete")).HasAttribute("disabled").Should().BeTrue();
        page.Find(Action(UnusedTariffs.Id, "delete")).HasAttribute("disabled").Should().BeFalse();
        page.FindComponents<AppRowActions>().Select(component => component.Instance.DeleteDisabledReason).Should().Equal(
            "Questions use this text (3). Remove their links before deleting it.",
            null,
            "Questions use this text (5). Remove their links before deleting it.");
    }

    [Fact]
    public void Row_LongTitle_IsTruncatedByTheKitWithTheWholeTextAsTooltip_AndTheActionNameStaysShort()
    {
        var title = string.Join(' ', Enumerable.Repeat("trecho", 40));
        Api.SeedBaseTexts(UnusedTariffs with { Title = title });

        var page = RenderPage();
        WaitForRows(page, 1);

        var cell = page.Find("tbody tr td .app-truncate");
        cell.GetAttribute("style").Should().Be("max-width: 32rem");
        cell.TextContent.Should().Be(title);
        var label = page.Find(Action(UnusedTariffs.Id, "edit")).GetAttribute("aria-label")!;
        label.Should().StartWith("Edit: base text \"trecho trecho").And.Contain("…");
        label.Length.Should().BeLessThan(100);
    }

    [Fact]
    public void Delete_Confirmed_SendsTheDeleteToThatTextAndShowsTheSnackbar()
    {
        var providers = RenderProviders();
        Api.SeedBaseTexts(UnusedTariffs);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(UnusedTariffs.Id, "delete")).Click();

        // The title is quoted in the message: it is at most 200 characters, and the confirm button says the object.
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup
            .Should().Contain("Tabela de tarifas de energia 2025").And.Contain("No question uses it."));
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Delete && call.Path == $"/api/v1/question-bank/base-texts/{UnusedTariffs.Id}"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Base text deleted."));
        page.WaitForAssertion(() => page.Markup.Should().Contain("No base text yet."));
    }

    [Fact]
    public void Delete_Cancelled_SendsNothing()
    {
        var providers = RenderProviders();
        Api.SeedBaseTexts(UnusedTariffs);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(UnusedTariffs.Id, "delete")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-cancel").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-cancel").Click();

        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Delete);
    }

    // The race: a question linked the text meanwhile, so the Api answers 409 and the alert says why.
    [Fact]
    public void Delete_RefusedBecauseAQuestionLinkedItMeanwhile_ShowsTheCodesTextAboveTheTable()
    {
        var providers = RenderProviders();
        Api.SeedBaseTexts(UnusedTariffs);
        Api.BaseTextDeleteFailure = (HttpStatusCode.Conflict, QuestionBankErrorCodes.BaseTextInUse);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(UnusedTariffs.Id, "delete")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-ok").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => page.Find(".app-alert").TextContent.Should()
            .Contain("Questions still use this base text. Remove their links before deleting it."));
    }

    [Fact]
    public void Add_GoesToTheNewBaseTextPage()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll(".app-primary-action").Should().ContainSingle());

        page.Find(".app-primary-action").Click();

        page.WaitForAssertion(() => Navigation().Uri.Should().EndWith("/admin/base-texts/new"));
    }

    [Fact]
    public void Edit_GoesToThatBaseTextsPage()
    {
        Api.SeedBaseTexts(UnusedTariffs);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(UnusedTariffs.Id, "edit")).Click();

        page.WaitForAssertion(() => Navigation().Uri.Should().EndWith($"/admin/base-texts/{UnusedTariffs.Id}"));
    }

    [Fact]
    public void Header_HasTheTitleAndTheContentBreadcrumbs()
    {
        var page = RenderPage();

        page.Find("h1").TextContent.Should().Be("Base texts");
        page.Find(".app-breadcrumbs").TextContent.Should().Contain("Content").And.Contain("Base texts");
    }

    // AC10 (screen part): the page is gated by the permission of the questions, by policy, as the question pages are.
    [Fact]
    public void ThePages_AreGatedByTheQuestionsManagePermission()
    {
        foreach (var type in new[] { typeof(BaseTexts), typeof(BaseTextForm) })
        {
            var authorize = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .Single();
            authorize.Policy.Should().EndWith(QuestionBankPermissions.Manage, $"{type.Name} needs questions.manage");
        }
    }

    // AC10 (screen part): the menu entry sits after Questions and is offered by permission only.
    [Fact]
    public void Menu_HasTheBaseTextsEntryAfterQuestions_ForTheQuestionsManagePermission()
    {
        var entries = NavigationItems.All.ToList();
        var questions = entries.FindIndex(item => item.ResourceKey == "Nav.Questions");
        var baseTexts = entries.FindIndex(item => item.ResourceKey == "Nav.BaseTexts");

        // F-105 AC9 (Screen 1): the review queue entry sits right after Questions, so the base texts follow it.
        baseTexts.Should().Be(questions + 2);
        entries[questions + 1].ResourceKey.Should().Be("Nav.ReviewQueue");
        entries[baseTexts].Route.Should().Be("/admin/base-texts");
        entries[baseTexts].RequiredPermission.Should().Be(QuestionBankPermissions.Manage);
        entries[baseTexts].Icon.Should().Be(AppIcons.BaseText);
    }
}
