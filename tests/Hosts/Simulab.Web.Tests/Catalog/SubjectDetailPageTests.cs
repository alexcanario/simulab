using System.Net;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-79 `/admin/subjects/{id}`: the subject's page and its topics section (AC9, AC11, AC12, BR10). The move
/// itself is the Api's rule and has its own tests; here the screen is checked to ask for it and show the result.
/// </summary>
public sealed class SubjectDetailPageTests : SubjectsTestContext
{
    private IRenderedComponent<SubjectDetail> RenderPage(Guid? id = null) =>
        Render<SubjectDetail>(parameters => parameters.Add(page => page.Id, id ?? Portuguese.Id));

    private static IReadOnlyList<string> RowsOf(IRenderedComponent<SubjectDetail> page) =>
        [.. page.FindAll(".app-item-row-content").Select(row => row.TextContent.Trim())];

    // AC9: the page shows the name, the area and the topics in the order the Api answered them.
    [Fact]
    public void Load_ShowsTheSubjectItsAreaAndItsTopics()
    {
        var page = RenderPage();

        page.WaitForAssertion(() => RowsOf(page).Should().Equal("Crase", "Pontuação"));
        page.Find(".app-page-header").TextContent.Should().Contain("Português");
        page.Find("#subject-area-text").TextContent.Should().Contain("Languages");
    }

    [Fact]
    public void Load_SubjectWithoutArea_SaysSo()
    {
        var page = RenderPage(Loose.Id);

        page.WaitForAssertion(() => page.Find("#subject-area-text").TextContent.Should().Contain("No area"));
    }

    [Fact]
    public void Load_BreadcrumbsLeadBackToTheList()
    {
        var page = RenderPage();

        // Content is a section label and the subject is where the reader is: only the list is a live link.
        page.WaitForAssertion(() => page.FindAll(".mud-breadcrumbs a")
            .Where(link => link.GetAttribute("href") != "#").Should().ContainSingle()
            .Which.GetAttribute("href").Should().Be("/admin/subjects"));
        page.Find(".mud-breadcrumbs").TextContent.Should().Contain("Português");
    }

    // The "No topics yet" state of the item.
    [Fact]
    public void Load_NoTopics_ShowsTheEmptyState()
    {
        Topics.Clear();

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("No topics yet."));
    }

    // BR13: an unknown subject says so and offers the way back.
    [Fact]
    public void Load_UnknownSubject_ShowsNotFoundWithALinkBack()
    {
        var page = RenderPage(Guid.CreateVersion7());

        page.WaitForAssertion(() => page.Markup.Should().Contain("This subject no longer exists."));
        page.FindAll("a[href='/admin/subjects']").Should().Contain(link => link.TextContent.Contains("Back to the subjects", StringComparison.Ordinal));
        page.FindAll("#subject-section-topics").Should().BeEmpty();
    }

    [Fact]
    public void Load_ServerError_ShowsTheErrorState()
    {
        Subjects = null;

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
    }

    // AC9: adding a topic posts it to the subject's own route, with no subject in the body.
    [Fact]
    public void AddTopic_SendsItToTheSubjectsRouteAndShowsTheSnackbar()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => RowsOf(page).Should().HaveCount(2));

        page.Find("#topic-add").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#topic-name"));
        providers.Dialogs.FindAll("#topic-subject").Should().BeEmpty("an add takes its subject from the page");
        providers.Dialogs.Find("#topic-name").Change("Acentuação");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/catalog/subjects/{Portuguese.Id}/topics"));
        FakeCatalogApi.Read<SaveTopicRequest>(Api.Received.Last(call => call.Method == HttpMethod.Post).Body)
            .Should().Be(new SaveTopicRequest("Acentuação"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Topic saved."));
    }

    // AC10: a name the subject already has shows on the name field.
    [Fact]
    public void AddTopic_NameTaken_ShowsTheMessageOnTheNameField()
    {
        TaxonomyWriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.TopicNameTaken);
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => RowsOf(page).Should().HaveCount(2));

        page.Find("#topic-add").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#topic-name"));
        providers.Dialogs.Find("#topic-name").Change("crase");
        providers.Dialogs.Find(".app-form-save").Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.Markup.Should().Contain("This subject already has a topic with this name"));
        providers.Dialogs.Find("#topic-name").GetAttribute("aria-invalid").Should().Be("true");
    }

    [Fact]
    public void AddTopic_NameTooShort_IsStoppedOnTheScreenAndNothingIsSent()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => RowsOf(page).Should().HaveCount(2));

        page.Find("#topic-add").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#topic-name"));
        providers.Dialogs.Find("#topic-name").Change("A");
        providers.Dialogs.Find(".app-form-save").Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.Markup.Should().Contain("Type a name with at least 2 characters."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    // AC11 (screen side): an edit offers the subject picker, starting from this subject, and sends the subject.
    [Fact]
    public void EditTopic_OffersTheSubjectPickerAndSendsTheSubjectWithTheNewName()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => RowsOf(page).Should().HaveCount(2));

        page.Find("button[aria-label='Edit: Crase']").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#topic-name").GetAttribute("value").Should().Be("Crase"));
        providers.Dialogs.Find("#topic-subject").GetAttribute("value").Should().Be("Português");
        providers.Dialogs.Find("#topic-name").Change("Crase obrigatória");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Put && call.Path == $"/api/v1/catalog/topics/{Crase.Id}"));
        FakeCatalogApi.Read<SaveTopicRequest>(Api.Received.Last(call => call.Method == HttpMethod.Put).Body)
            .Should().Be(new SaveTopicRequest("Crase obrigatória", Portuguese.Id));
    }

    // AC11: a move the server accepts takes the topic out of this section, because the list is read again.
    [Fact]
    public void EditTopic_ListIsReadAgainAfterTheSave()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => RowsOf(page).Should().HaveCount(2));
        var reads = Api.Received.Count(call => call.Method == HttpMethod.Get && call.Path.EndsWith("/topics", StringComparison.Ordinal));

        page.Find("button[aria-label='Edit: Crase']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#topic-name"));
        Topics.RemoveAll(topic => topic.Id == Crase.Id);
        providers.Dialogs.Find("#topic-name").Change("Crase obrigatória");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => RowsOf(page).Should().Equal("Pontuação"));
        Api.Received.Count(call => call.Method == HttpMethod.Get && call.Path.EndsWith("/topics", StringComparison.Ordinal))
            .Should().BeGreaterThan(reads);
    }

    // AC12: a confirmed delete sends the DELETE and the row leaves.
    [Fact]
    public void DeleteTopic_Confirmed_SendsTheDeleteAndTheRowLeaves()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => RowsOf(page).Should().HaveCount(2));

        page.Find("button[aria-label='Delete: Crase']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Crase"));
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Delete && call.Path == $"/api/v1/catalog/topics/{Crase.Id}"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Topic deleted."));
        page.WaitForAssertion(() => RowsOf(page).Should().Equal("Pontuação"));
    }

    [Fact]
    public void DeleteTopic_Cancelled_SendsNothing()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => RowsOf(page).Should().HaveCount(2));

        page.Find("button[aria-label='Delete: Crase']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Crase"));
        providers.Dialogs.Find(".app-confirm-cancel").Click();

        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Delete);
        RowsOf(page).Should().HaveCount(2);
    }

    // The page's edit action opens the same dialog as the list: one way to edit an item.
    [Fact]
    public void EditSubject_OpensTheSubjectDialogAndSendsAPut()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => RowsOf(page).Should().HaveCount(2));

        page.Find(".app-page-header button").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-name").GetAttribute("value").Should().Be("Português"));
        providers.Dialogs.Find("#subject-name").Change("Língua Portuguesa");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Put && call.Path == $"/api/v1/catalog/subjects/{Portuguese.Id}"));
        page.WaitForAssertion(() => page.Find(".app-page-header").TextContent.Should().Contain("Língua Portuguesa"));
    }
}
