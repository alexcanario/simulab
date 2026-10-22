using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-90 on `/admin/subjects`: a subject in use has a "Where used" row action (BR7) that opens a read-only dialog with
/// the notice subjects that map it whole, in the Api's order, each a link to its edition page (BR5); one released
/// since the list loaded shows the empty state and the list is read again on close (BR8).
/// </summary>
public sealed class SubjectsWhereUsedPageTests : SubjectsTestContext
{
    private static readonly Guid GuardExamId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e1");
    private static readonly Guid Guard2025Id = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e2");
    private static readonly Guid Guard2023Id = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e3");

    private static readonly WhereUsedResponse TwoEditions = new(
        [
            new WhereUsedMappingResponse(GuardExamId, "Guarda Municipal SP", Guard2025Id, 2025, "Guarda", Guid.CreateVersion7(), "Raciocínio Lógico-Matemático"),
            new WhereUsedMappingResponse(GuardExamId, "Guarda Municipal SP", Guard2023Id, 2023, null, Guid.CreateVersion7(), "Raciocínio Lógico")
        ],
        0);

    private IRenderedComponent<Subjects> RenderPage() => Render<Subjects>();

    private int ListReads => Api.Received.Count(call => call.Method == HttpMethod.Get && call.Path == "/api/v1/catalog/subjects");

    // AC7, AC2, BR7: only a row in use has the action; a row not in use (never mapped, or its mapping deleted) has none.
    [Fact]
    public void Load_OnlyTheRowInUse_HasTheWhereUsedAction()
    {
        Subjects = [Constitutional, Mapped];
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.FindAll("button[aria-label='Where used: Raciocínio Lógico']").Should().ContainSingle();
        page.FindAll("button[aria-label='Where used: Direito Constitucional']").Should().BeEmpty();
    }

    // AC1, UC1, BR5: the dialog lists both notice subjects in the Api's order, each linking to its edition page.
    [Fact]
    public void WhereUsed_SubjectMappedByTwoEditions_ListsBothInOrderWithLinksToTheEditionPages()
    {
        Subjects = [Mapped];
        WhereUsed[Mapped.Id] = TwoEditions;
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));

        page.Find("button[aria-label='Where used: Raciocínio Lógico']").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-where-used-mapping").Should().HaveCount(2));
        providers.Dialogs.Markup.Should().Contain("Where \"Raciocínio Lógico\" is used");
        providers.Dialogs.Markup.Should().Contain("Notice subjects");
        providers.Dialogs.Markup.Should().NotContain("Used by");
        var rows = providers.Dialogs.FindAll(".app-where-used-mapping");
        rows[0].TextContent.Should().Contain("Guarda Municipal SP · 2025 · Guarda").And.Contain("Raciocínio Lógico-Matemático");
        rows[1].TextContent.Should().Contain("Guarda Municipal SP · 2023").And.Contain("Raciocínio Lógico");
        rows[0].QuerySelector("a")!.GetAttribute("href").Should().Be($"/admin/exams/{GuardExamId}/editions/{Guard2025Id}");
        rows[1].QuerySelector("a")!.GetAttribute("href").Should().Be($"/admin/exams/{GuardExamId}/editions/{Guard2023Id}");
    }

    // AC8, BR8: another curator released the subject after the list loaded - the empty state shows, and closing the
    // dialog reads the list again, which no longer offers the action.
    [Fact]
    public void WhereUsed_SubjectReleasedSinceTheListLoaded_ShowsTheEmptyStateAndReloadsTheListOnClose()
    {
        Subjects = [Mapped];
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));
        Subjects = [Mapped with { InUse = false }];
        WhereUsed[Mapped.Id] = new WhereUsedResponse([], 0);
        var reads = ListReads;

        page.Find("button[aria-label='Where used: Raciocínio Lógico']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Nothing uses this item any more."));
        providers.Dialogs.Find("#where-used-close").Click();

        page.WaitForAssertion(() => ListReads.Should().BeGreaterThan(reads));
        page.WaitForAssertion(() => page.FindAll("button[aria-label='Where used: Raciocínio Lógico']").Should().BeEmpty());
    }
}
