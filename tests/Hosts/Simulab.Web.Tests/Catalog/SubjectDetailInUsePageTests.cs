using System.Net;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-75 Screen 3, `/admin/subjects/{id}`: a topic that a live notice subject maps has its delete disabled with the
/// reason before the click (BR10, AC11), and a 409 at the click is shown and reloads the section.
/// </summary>
public sealed class SubjectDetailInUsePageTests : SubjectsTestContext
{
    private IRenderedComponent<SubjectDetail> RenderPage() =>
        Render<SubjectDetail>(parameters => parameters.Add(page => page.Id, Portuguese.Id));

    private int TopicReads => Api.Received.Count(call => call.Method == HttpMethod.Get && call.Path.EndsWith("/topics", StringComparison.Ordinal));

    // AC11, BR10: the topic is in use - its delete is disabled with the reason; the others stay enabled.
    [Fact]
    public void Load_TopicMapped_HasItsDeleteDisabledWithTheInUseReason()
    {
        Topics.Add(MappedTopic);
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll(".app-item-row").Should().HaveCount(3));

        page.Find("button[aria-label='Delete: Ortografia']").HasAttribute("disabled").Should().BeTrue();
        page.Find("button[aria-label='Delete: Crase']").HasAttribute("disabled").Should().BeFalse();
        page.FindComponents<AppRowActions>().Single(actions => actions.Instance.ItemName == "Ortografia")
            .Instance.DeleteDisabledReason
            .Should().Be("Notice subjects map this topic. Remove it from their mappings before deleting it.");
        page.FindComponents<AppRowActions>().Single(actions => actions.Instance.ItemName == "Crase")
            .Instance.DeleteDisabledReason.Should().BeNull();
    }

    // AC11: someone mapped the topic after the section loaded - the 409 is shown and the section is read again.
    [Fact]
    public void Delete_RefusedBecauseItIsNowMapped_ShowsTheMessageAndReloadsTheSection()
    {
        TaxonomyWriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.TopicInUse);
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll(".app-item-row").Should().HaveCount(2));
        var reads = TopicReads;

        page.Find("button[aria-label='Delete: Crase']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Crase"));
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Notice subjects map this topic, so it cannot be deleted. The list was reloaded."));
        page.WaitForAssertion(() => TopicReads.Should().BeGreaterThan(reads));
        page.FindAll(".app-item-row").Should().HaveCount(2);
    }
}
