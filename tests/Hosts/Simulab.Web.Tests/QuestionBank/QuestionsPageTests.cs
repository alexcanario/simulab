using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-95 `/admin/questions`: the list, its search and status filter, and the row actions
/// (AC1, AC8 screen part, AC11 screen part, and the states the screen design lists).
/// </summary>
public sealed class QuestionsPageTests : QuestionPageTestContext
{
    private IRenderedComponent<Questions> RenderPage() => Render<Questions>();

    private void SeedTwo()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(Published);
    }

    private static void WaitForRows(IRenderedComponent<Questions> page, int count) =>
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(count));

    private static IReadOnlyList<string> CellsOf(IRenderedComponent<Questions> page, int column) =>
        [.. page.FindAll("tbody tr").Select(row => row.QuerySelectorAll("td")[column].TextContent.Trim())];

    private static string Action(Guid id, string key) => $"#question-actions-{id}-{key}";

    [Fact]
    public void Load_ShowsTheExcerptTheTopicWithItsSubjectTheDifficultyTheLanguageAndTheStatus()
    {
        SeedTwo();

        var page = RenderPage();

        WaitForRows(page, 2);
        CellsOf(page, 0).Should().Equal("Qual frase usa a crase corretamente?", "Onde entra a vírgula?");

        // Topic, then its subject on a second line.
        var topicCell = page.FindAll("tbody tr")[0].QuerySelectorAll("td")[1];
        topicCell.TextContent.Should().Contain("Crase").And.Contain("Português");
        topicCell.QuerySelector(".app-cell-secondary")!.TextContent.Should().Contain("Português");

        // A difficulty is a number; an empty one is a dash that is hidden from a screen reader and spoken instead.
        CellsOf(page, 3)[0].Should().Contain("3");
        var empty = page.FindAll("tbody tr")[1].QuerySelectorAll("td")[3];
        empty.QuerySelector("[aria-hidden='true']")!.TextContent.Should().Be("—");
        empty.QuerySelector(".app-visually-hidden")!.TextContent.Should().Be("No difficulty set");

        // The language is in its own name and marked with its own tag.
        CellsOf(page, 4)[0].Should().Be("Português (Brasil)");
        page.FindAll("tbody tr")[1].QuerySelectorAll("td")[4].QuerySelector("[lang]")!.GetAttribute("lang").Should().Be("pt-PT");

        // The status is a chip with text, in the tone the status means.
        CellsOf(page, 5).Should().Equal("Draft", "Published");
        page.FindAll(".app-status-chip-neutral").Should().ContainSingle();
        page.FindAll(".app-status-chip-success").Should().ContainSingle();
    }

    [Fact]
    public void Load_AsksTheServerForTheFirstPageWithNoFilter()
    {
        SeedTwo();

        var page = RenderPage();
        WaitForRows(page, 2);

        var list = Api.Received.Single(call => call.Method == HttpMethod.Get && call.Path == "/api/v1/question-bank/questions");
        list.Query.Should().Contain("page=0").And.Contain("pageSize=25").And.NotContain("status=").And.NotContain("search=");
    }

    // The statement is not sortable: the order is newest first and is the Api's (BR15).
    [Fact]
    public void Columns_NoneIsSortable()
    {
        SeedTwo();

        var page = RenderPage();
        WaitForRows(page, 2);

        page.FindAll(".sortable-column-header").Should().BeEmpty();
    }

    [Fact]
    public void Search_SendsTheWordToTheServerAsSearch()
    {
        SeedTwo();
        var page = RenderPage();
        WaitForRows(page, 2);

        page.Find(".app-table-search input").Input("crase");
        WaitForAfterDebounce(page, () => Api.Received.Should().Contain(call =>
            call.Path == "/api/v1/question-bank/questions" && call.Query!.Contains("search=crase", StringComparison.Ordinal)));
        WaitForRows(page, 1);
    }

    [Fact]
    public void Filter_ByStatus_ReloadsTheListFromTheFirstPageWithThatStatus()
    {
        SeedTwo();
        var page = RenderPage();
        WaitForRows(page, 2);

        // F-100: the status filter is a multi-pick since the list gained its other filters.
        page.Find("#questions-filter-status").Click();
        page.Find("#questions-filter-status-option-Published").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Path == "/api/v1/question-bank/questions"
            && call.Query!.Contains("status=Published", StringComparison.Ordinal)
            && call.Query.Contains("page=0", StringComparison.Ordinal)));
        WaitForRows(page, 1);
        CellsOf(page, 5).Should().Equal("Published");
    }

    // F-95 BR15 read with F-100 and F-101: the status filter offers every status (Draft, Published, Annulled, and In review from F-105), in that order.
    [Fact]
    public void Filter_Status_OffersEveryStatus()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("#questions-filter-status").Should().ContainSingle());

        page.Find("#questions-filter-status").Click();

        page.FindAll("[id^='questions-filter-status-option-']")
            .Select(option => option.QuerySelector(".app-mp-option-name")!.TextContent.Trim())
            .Should().Equal("Draft", "Published", "Annulled", "In review");
        page.Find("label[for='questions-filter-status']").TextContent.Should().Be("Status");
    }

    [Fact]
    public void Load_NoQuestionInTheBank_ShowsTheEmptyStateWithItsAddAction()
    {
        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("No question in the bank yet."));
        page.Markup.Should().Contain("Add");
    }

    [Fact]
    public void Load_NothingMatchesTheStatusFilter_SaysSoInsteadOfTheBankIsEmpty()
    {
        Api.Questions.Add(Draft);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find("#questions-filter-status").Click();
        page.Find("#questions-filter-status-option-Published").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("No question matches the chosen filters."));
    }

    [Fact]
    public void Search_NothingMatches_NamesTheTerm()
    {
        SeedTwo();
        var page = RenderPage();
        WaitForRows(page, 2);

        page.Find(".app-table-search input").Input("zzzz");
        WaitForAfterDebounce(page, () => page.Markup.Should().Contain("No question matches").And.Contain("zzzz"));
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.ListFailure = (HttpStatusCode.InternalServerError, "common.unexpected_error");

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
        page.Markup.Should().Contain("Try again");
    }

    // AC8 (screen part): a published question's delete action is disabled and its reason is the tooltip's text.
    [Fact]
    public void Row_Published_DeleteIsDisabledWithTheReason_AndADraftCanBeDeleted()
    {
        SeedTwo();
        var page = RenderPage();
        WaitForRows(page, 2);

        page.Find(Action(Published.Id, "delete")).HasAttribute("disabled").Should().BeTrue();
        page.Find(Action(Draft.Id, "delete")).HasAttribute("disabled").Should().BeFalse();

        var actions = page.FindComponents<AppRowActions>().ToList();
        actions.Should().HaveCount(2);
        actions.Select(component => component.Instance.DeleteDisabledReason).Should().Equal(
            null, "This question is published. Unpublish it before deleting.");
    }

    [Fact]
    public void Row_LongStatement_IsTruncatedByTheKitWithTheWholeTextAsTooltip_AndTheActionNameStaysShort()
    {
        var statement = string.Join(' ', Enumerable.Repeat("enunciado", 60));
        Api.Questions.Add(Draft with { StatementHtml = $"<p>{statement}</p>" });

        var page = RenderPage();
        WaitForRows(page, 1);

        // The excerpt sits in the kit's truncating element: a width cap and an ellipsis, never a fixed width.
        var cell = page.Find("tbody tr td .app-truncate");
        cell.GetAttribute("style").Should().Be("max-width: 26rem");
        cell.TextContent.Should().Be(statement);
        cell.GetAttribute("lang").Should().Be("pt-BR");

        // The accessible name of the row's buttons carries at most 60 characters of the statement.
        var label = page.Find(Action(Draft.Id, "edit")).GetAttribute("aria-label")!;
        label.Should().StartWith("Edit: question \"enunciado enunciado").And.Contain("…");
        label.Length.Should().BeLessThan(100);
    }

    [Fact]
    public void Delete_Confirmed_SendsTheDeleteToThatQuestionAndShowsTheSnackbar()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Draft);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(Draft.Id, "delete")).Click();

        // The statement is not quoted: it can be thousands of characters.
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("this question"));
        providers.Dialogs.Markup.Should().NotContain("Qual frase usa a crase");
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Delete && call.Path == $"/api/v1/question-bank/questions/{Draft.Id}"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question deleted."));
        page.WaitForAssertion(() => page.Markup.Should().Contain("No question in the bank yet."), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Delete_Cancelled_SendsNothing()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Draft);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(Draft.Id, "delete")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-cancel").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-cancel").Click();

        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Delete);
    }

    // BR11 as another curator may have published it meanwhile: the code's text is above the table.
    [Fact]
    public void Delete_RefusedBecauseItIsPublished_ShowsTheCodesTextAboveTheTable()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Draft);
        Api.DeleteFailure = (HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(Draft.Id, "delete")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-ok").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => page.Find(".app-alert").TextContent.Should()
            .Contain("Only a draft can be deleted. Unpublish the question first."));
    }

    [Fact]
    public void Publish_FromTheRow_SendsItWithNoConfirmationAndShowsTheSnackbar()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Draft);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(Draft.Id, "publish")).Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/question-bank/questions/{Draft.Id}/publish"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question published."));
        providers.Dialogs.FindAll(".app-confirm-ok").Should().BeEmpty("publishing is reversible and asks nothing (BR12)");
        page.WaitForAssertion(() => CellsOf(page, 5).Should().Equal("Published"));
    }

    [Fact]
    public void Unpublish_FromTheRow_SendsItAndTheRowIsADraftAgain()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(Published.Id, "unpublish")).Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/question-bank/questions/{Published.Id}/unpublish"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question unpublished. It is a draft again."));
        page.WaitForAssertion(() => CellsOf(page, 5).Should().Equal("Draft"));
    }

    // A publish refused because the question is incomplete shows the code's text; the curator completes it on its page.
    [Fact]
    public void Publish_RefusedAsIncomplete_ShowsTheCodesTextInTheTopAlert()
    {
        RenderProviders();
        Api.Questions.Add(Draft);
        Api.PublicationFailure = (HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionKeyInvalid);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(Draft.Id, "publish")).Click();

        page.WaitForAssertion(() => page.Find(".app-alert").TextContent.Should().Contain("Mark exactly one option as correct."));
    }

    [Fact]
    public void Add_GoesToTheNewQuestionPage()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll(".app-primary-action").Should().ContainSingle());

        page.Find(".app-primary-action").Click();

        page.WaitForAssertion(() =>
            Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/admin/questions/new"));
    }

    [Fact]
    public void Edit_GoesToThatQuestionsPage()
    {
        Api.Questions.Add(Draft);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.Find(Action(Draft.Id, "edit")).Click();

        page.WaitForAssertion(() =>
            Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/admin/questions/{Draft.Id}"));
    }

    [Fact]
    public void Header_HasTheTitleAndTheContentBreadcrumbs()
    {
        var page = RenderPage();

        page.Find("h1").TextContent.Should().Be("Questions");
        page.Find(".app-breadcrumbs").TextContent.Should().Contain("Content").And.Contain("Questions");
    }
}
