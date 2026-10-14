using System.Net;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-75 Screen 3, `/admin/subjects`: a subject that a live notice subject maps whole has its delete disabled with the
/// reason before the click (BR10, AC11), and a 409 at the click is shown and reloads the list.
/// </summary>
public sealed class SubjectsInUsePageTests : SubjectsTestContext
{
    private const string InUseReason = "Notice subjects map this whole subject. Remove it from their mappings before deleting it.";

    private IRenderedComponent<Subjects> RenderPage() => Render<Subjects>();

    private int ListReads => Api.Received.Count(call => call.Method == HttpMethod.Get && call.Path == "/api/v1/catalog/subjects");

    // AC11, BR10: in use, no topics - the delete is disabled and the tooltip says why.
    [Fact]
    public void Load_SubjectMappedWhole_HasItsDeleteDisabledWithTheInUseReason()
    {
        Subjects = [Constitutional, Mapped];
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.Find("button[aria-label='Delete: Raciocínio Lógico']").HasAttribute("disabled").Should().BeTrue();
        page.Find("button[aria-label='Delete: Direito Constitucional']").HasAttribute("disabled").Should().BeFalse();
        page.FindComponents<AppRowActions>().Single(actions => actions.Instance.ItemName == "Raciocínio Lógico")
            .Instance.DeleteDisabledReason.Should().Be(InUseReason);
        page.FindComponents<AppRowActions>().Single(actions => actions.Instance.ItemName == "Direito Constitucional")
            .Instance.DeleteDisabledReason.Should().BeNull();
    }

    // BR9 order: a subject with topics that is also mapped says "has topics" first (subject.has_topics before subject.in_use).
    [Fact]
    public void Load_SubjectWithTopicsAndMapped_SaysItHasTopicsFirst()
    {
        Subjects = [MappedWithTopics];
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));

        page.FindComponents<AppRowActions>().Single().Instance.DeleteDisabledReason
            .Should().Be("This subject has topics. Delete them first, on the subject's page.");
    }

    // AC11: someone mapped the subject after the list loaded - the 409 is shown through the error text and the list is read again.
    [Fact]
    public void Delete_RefusedBecauseItIsNowMapped_ShowsTheMessageAndReloadsTheList()
    {
        TaxonomyWriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.SubjectInUse);
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        var reads = ListReads;

        page.Find("button[aria-label='Delete: Direito Constitucional']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Direito Constitucional"));
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Notice subjects map this subject, so it cannot be deleted. The list was reloaded."));
        page.WaitForAssertion(() => ListReads.Should().BeGreaterThan(reads));
        page.FindAll("tbody tr").Should().HaveCount(3);
    }
}
