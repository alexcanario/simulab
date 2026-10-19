using System.Globalization;
using System.Net;
using System.Resources;
using System.Text.RegularExpressions;
using Bunit;
using MudBlazor;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-86 Screen 1 and Screen 2: the "Copy from another edition" button in the empty state of the notice subjects section
/// and its picking dialog (AC11, AC12), and the texts in the three languages (AC13). The Api runs the rules (BR1, BR2,
/// BR7); these tests say what the screen shows, what it sends and what it does with a refusal.
/// </summary>
public sealed class CopyNoticeSubjectsTests : CatalogPageTestContext
{
    private static readonly Guid EditionId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e1");
    private static readonly Guid Source2024 = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e2");
    private static readonly Guid Source2022 = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e3");

    private static readonly string[] Cultures = ["en", "pt-BR", "pt-PT"];

    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string> Keys() => new(
    [
        "NoticeSubjects.Copy.Open", "NoticeSubjects.Copy.NoSource", "NoticeSubjects.Copy.Title", "NoticeSubjects.Copy.Intro",
        "NoticeSubjects.Copy.Source", "NoticeSubjects.Copy.SourceCount.One", "NoticeSubjects.Copy.SourceCount.Many",
        "NoticeSubjects.Copy.Confirm", "NoticeSubjects.Copy.Done", "NoticeSubjects.Copy.Done.One",
        CatalogErrorCodes.ExamEditionCopySourceNotFound, CatalogErrorCodes.NoticeSubjectCopySourceEmpty,
        CatalogErrorCodes.NoticeSubjectCopyTargetNotEmpty
    ]);

    private void WithTwoSources()
    {
        Api.CopySources.Add(new NoticeSubjectCopySourceResponse(Source2024, 2024, "Analista", "CEBRASPE", 12));
        Api.CopySources.Add(new NoticeSubjectCopySourceResponse(Source2022, 2022, null, "FGV", 1));
    }

    private void WithRowsToCopy(int count)
    {
        for (var index = 1; index <= count; index++)
        {
            Api.CopyRows.Add(new NoticeSubjectResponse(Guid.CreateVersion7(), Source2024, "Basic", $"Subject {index}", 10));
        }
    }

    private IRenderedComponent<NoticeSubjectsSection> RenderSection() =>
        Render<NoticeSubjectsSection>(parameters => parameters
            .Add(section => section.ExamId, AgentePf.Id)
            .Add(section => section.EditionId, EditionId));

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static void WaitForDialog(IRenderedComponent<MudDialogProvider> dialogs) =>
        dialogs.WaitForAssertion(() => dialogs.FindAll("#copy-notice-subjects-source").Should().ContainSingle());

    private IRenderedComponent<NoticeSubjectsSection> OpenDialog(out IRenderedComponent<MudDialogProvider> dialogs, out IRenderedComponent<MudSnackbarProvider> snackbars)
    {
        (dialogs, snackbars) = RenderProviders();
        var section = RenderSection();
        var provider = dialogs;
        section.WaitForAssertion(() => section.Find("#notice-subject-copy").HasAttribute("disabled").Should().BeFalse());

        section.Find("#notice-subject-copy").Click();
        WaitForDialog(provider);

        return section;
    }

    // AC11: an empty list with a source offers the button, enabled, outlined and as a button (it opens a dialog).
    [Fact]
    public void Section_EmptyListWithASource_OffersTheCopyButtonEnabled()
    {
        WithTwoSources();

        var section = RenderSection();

        section.WaitForAssertion(() => section.Find("#notice-subject-copy").HasAttribute("disabled").Should().BeFalse());
        var copy = section.Find("#notice-subject-copy");
        copy.TagName.Should().Be("BUTTON");
        copy.TextContent.Should().Contain("Copy from another edition");
        copy.ClassList.Should().Contain("mud-button-outlined", "the edition's Save stays the page's only primary button");
        Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Get && call.Path == $"/api/v1/catalog/exams/{AgentePf.Id}/editions/{EditionId}/notice-subjects/copy-sources");
    }

    // AC11: with no edition to copy from the button stays, disabled, and the tooltip says why.
    [Fact]
    public void Section_EmptyListWithNoSource_ShowsTheCopyButtonDisabledWithItsReason()
    {
        var section = RenderSection();

        section.WaitForAssertion(() => section.Markup.Should().Contain("This edition has no notice subject yet."));
        section.Find("#notice-subject-copy").HasAttribute("disabled").Should().BeTrue();
        section.FindComponents<MudTooltip>().Select(tooltip => tooltip.Instance.Text)
            .Should().Contain("No other edition of this exam has notice subjects.");
    }

    // AC11: a list with rows is not copied into, so the button is not drawn and the sources are not even asked for.
    [Fact]
    public void Section_ListWithRows_DoesNotShowTheCopyButton()
    {
        WithTwoSources();
        Api.NoticeSubjects.Add(new NoticeSubjectResponse(Guid.CreateVersion7(), EditionId, null, "Portuguese", 10));

        var section = RenderSection();

        section.WaitForAssertion(() => section.FindAll(".app-item-row").Should().ContainSingle());
        section.FindAll("#notice-subject-copy").Should().BeEmpty();
        Api.Received.Should().NotContain(call => call.Path.EndsWith("/copy-sources", StringComparison.Ordinal));
    }

    // Screen 2: one card per source, newest year first, with the position, the board and the count; Copy waits for a pick.
    [Fact]
    public void Dialog_ListsOneCardPerSourceAndEnablesCopyOnlyAfterAPick()
    {
        WithTwoSources();
        OpenDialog(out var dialogs, out _);

        dialogs.Markup.Should().Contain("Copy notice subjects from another edition");
        dialogs.Markup.Should().Contain("Then adjust what changed in the notice.");
        var cards = dialogs.FindAll("[role='radio']");
        cards.Select(card => Flat(card.TextContent)).Should().Equal("2024 — Analista CEBRASPE · 12 subjects", "2022 FGV · 1 subject");
        dialogs.Find("button.app-form-save").HasAttribute("disabled").Should().BeTrue();
        dialogs.Find("button.app-form-save").TextContent.Should().Contain("Copy");

        cards[1].Click();

        dialogs.WaitForAssertion(() => dialogs.Find("button.app-form-save").HasAttribute("disabled").Should().BeFalse());
    }

    // AC12: pick, confirm: the Api gets the source, the snackbar says how many, the list shows the copies.
    [Fact]
    public void Copy_PickingASourceAndConfirming_SendsItSaysHowManyAndShowsTheCopies()
    {
        WithTwoSources();
        WithRowsToCopy(3);
        var section = OpenDialog(out var dialogs, out var snackbars);

        dialogs.FindAll("[role='radio']")[0].Click();
        dialogs.WaitForAssertion(() => dialogs.Find("button.app-form-save").HasAttribute("disabled").Should().BeFalse());
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            var copy = Api.Received.Should().ContainSingle(call => call.Method == HttpMethod.Post).Subject;
            copy.Path.Should().Be($"/api/v1/catalog/exams/{AgentePf.Id}/editions/{EditionId}/notice-subjects/copy");
            FakeCatalogApi.Read<CopyNoticeSubjectsRequest>(copy.Body).SourceEditionId.Should().Be(Source2024);
        });
        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain("3 subjects copied."));
        section.WaitForAssertion(() =>
            section.FindAll(".app-item-row .app-truncate").Select(label => Flat(label.TextContent))
                .Should().Equal("Subject 1", "Subject 2", "Subject 3"));
        section.FindAll("#notice-subject-copy").Should().BeEmpty("the list is not empty any more");
        dialogs.FindAll("#copy-notice-subjects-source").Should().BeEmpty("the dialog closed");
    }

    [Fact]
    public void Copy_OneRow_UsesTheSingularMessage()
    {
        WithTwoSources();
        WithRowsToCopy(1);
        OpenDialog(out var dialogs, out var snackbars);

        dialogs.FindAll("[role='radio']")[0].Click();
        dialogs.WaitForAssertion(() => dialogs.Find("button.app-form-save").HasAttribute("disabled").Should().BeFalse());
        dialogs.Find("button.app-form-save").Click();

        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain("1 subject copied."));
    }

    // Screen 2: while the copy runs the dialog shows progress and cannot be closed.
    [Fact]
    public void Copy_WhileItRuns_ShowsProgressAndCannotBeCancelled()
    {
        WithTwoSources();
        WithRowsToCopy(2);
        OpenDialog(out var dialogs, out _);
        Api.WriteGate = new TaskCompletionSource();

        dialogs.FindAll("[role='radio']")[0].Click();
        dialogs.WaitForAssertion(() => dialogs.Find("button.app-form-save").HasAttribute("disabled").Should().BeFalse());
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            dialogs.Find("button.app-form-save").HasAttribute("disabled").Should().BeTrue();
            dialogs.Find("button.app-form-cancel").HasAttribute("disabled").Should().BeTrue();
            dialogs.FindAll("[role='status']").Should().NotBeEmpty("the button shows progress");
        });

        Api.WriteGate.SetResult();
    }

    // Screen 2: another admin filled the list meanwhile: the alert says so, the dialog stays, the list is read again.
    [Fact]
    public void Copy_TargetNotEmpty_ShowsTheAlertKeepsTheDialogAndReloadsTheList()
    {
        WithTwoSources();
        var section = OpenDialog(out var dialogs, out _);
        Api.CopyFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.NoticeSubjectCopyTargetNotEmpty);
        Api.NoticeSubjects.Add(new NoticeSubjectResponse(Guid.CreateVersion7(), EditionId, null, "Typed by someone else", 5));
        var readsBefore = Api.Received.Count(call => call.Method == HttpMethod.Get && call.Path.EndsWith("/notice-subjects", StringComparison.Ordinal));

        dialogs.FindAll("[role='radio']")[0].Click();
        dialogs.WaitForAssertion(() => dialogs.Find("button.app-form-save").HasAttribute("disabled").Should().BeFalse());
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("This edition already has notice subjects; a copy is only made into an empty list. The list was reloaded."));
        dialogs.FindAll("#copy-notice-subjects-source").Should().ContainSingle("the dialog stays open");
        section.WaitForAssertion(() =>
        {
            Api.Received.Count(call => call.Method == HttpMethod.Get && call.Path.EndsWith("/notice-subjects", StringComparison.Ordinal))
                .Should().BeGreaterThan(readsBefore, "the list behind the dialog is read again");
            section.Markup.Should().Contain("Typed by someone else");
        });
    }

    // Screen 2: the chosen source is gone: the alert says so, the pick is dropped and the options are read again.
    [Fact]
    public void Copy_SourceNotFound_ShowsTheAlertDropsThePickAndReadsTheSourcesAgain()
    {
        WithTwoSources();
        OpenDialog(out var dialogs, out _);
        Api.CopyFailure = (HttpStatusCode.NotFound, CatalogErrorCodes.ExamEditionCopySourceNotFound);
        var readsBefore = Api.Received.Count(call => call.Path.EndsWith("/copy-sources", StringComparison.Ordinal));
        Api.CopySources.RemoveAt(0);

        dialogs.FindAll("[role='radio']")[0].Click();
        dialogs.WaitForAssertion(() => dialogs.Find("button.app-form-save").HasAttribute("disabled").Should().BeFalse());
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            dialogs.Markup.Should().Contain("The chosen edition is no longer available. Pick another one.");
            Api.Received.Count(call => call.Path.EndsWith("/copy-sources", StringComparison.Ordinal)).Should().BeGreaterThan(readsBefore);
            dialogs.FindAll("[role='radio']").Should().ContainSingle("only the edition still there is offered");
            dialogs.Find("button.app-form-save").HasAttribute("disabled").Should().BeTrue("the pick was dropped");
        });
    }

    // Screen 2: nothing was typed, so Cancel closes at once, sends nothing and the focus goes back to the button.
    [Fact]
    public void Cancel_ClosesAtOnceAndSendsNothing()
    {
        WithTwoSources();
        OpenDialog(out var dialogs, out _);

        dialogs.Find("button.app-form-cancel").Click();

        dialogs.WaitForAssertion(() => dialogs.FindAll("#copy-notice-subjects-source").Should().BeEmpty());
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    // AC13: every new text exists in en, pt-BR and pt-PT, with the same placeholders in all three.
    [Theory]
    [MemberData(nameof(Keys))]
    public void EveryNewText_ExistsInTheThreeLanguagesWithTheSamePlaceholders(string key)
    {
        var expected = Placeholders(Manager.GetString(key, new CultureInfo("en")) ?? string.Empty);

        foreach (var culture in Cultures)
        {
            var text = Manager.GetString(key, new CultureInfo(culture));
            text.Should().NotBeNullOrWhiteSpace($"'{key}' in {culture}");
            Placeholders(text!).Should().Equal(expected, $"'{key}' in {culture}");
        }
    }

    // The owner's wording: pt-PT says "aviso" where pt-BR says "edital".
    [Fact]
    public void Intro_PtPtSaysAvisoWherePtBrSaysEdital()
    {
        Manager.GetString("NoticeSubjects.Copy.Intro", new CultureInfo("pt-BR")).Should().Contain("edital");
        Manager.GetString("NoticeSubjects.Copy.Intro", new CultureInfo("pt-PT")).Should().Contain("aviso").And.NotContain("edital");
    }

    private static string[] Placeholders(string text) =>
        [.. Regex.Matches(text, @"\{\d+\}").Select(match => match.Value).Order()];
}
