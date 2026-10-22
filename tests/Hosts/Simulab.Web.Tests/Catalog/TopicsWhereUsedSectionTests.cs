using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-90 on `/admin/subjects/{id}`, the topics section: a topic in use has a "Where used" row action (BR7) whose dialog
/// shows the notice subjects that map it and how many questions point at it (BR3). The count links to the question
/// list filtered by the topic only for who holds <c>questions.manage</c> (BR6, BR10); a count of 0 is not shown.
/// </summary>
public sealed class TopicsWhereUsedSectionTests : SubjectsTestContext
{
    private static readonly WhereUsedMappingResponse GuardMapping = new(
        Guid.Parse("0198f0a3-0000-7000-8000-0000000000e1"),
        "Guarda Municipal SP",
        Guid.Parse("0198f0a3-0000-7000-8000-0000000000e2"),
        2025,
        null,
        Guid.CreateVersion7(),
        "Língua Portuguesa");

    private IRenderedComponent<SubjectDetail> RenderPage()
    {
        Topics.Add(MappedTopic);
        var page = Render<SubjectDetail>(parameters => parameters.Add(detail => detail.Id, Portuguese.Id));
        page.WaitForAssertion(() => page.FindAll(".app-item-row").Should().HaveCount(3));

        return page;
    }

    private int TopicReads => Api.Received.Count(call => call.Method == HttpMethod.Get && call.Path.EndsWith("/topics", StringComparison.Ordinal));

    private static string QuestionListHref => $"/admin/questions?topic={MappedTopic.Id}";

    private (IRenderedComponent<MudBlazor.MudDialogProvider> Dialogs, IRenderedComponent<SubjectDetail> Page) OpenWhereUsed(WhereUsedResponse answer)
    {
        WhereUsed[MappedTopic.Id] = answer;
        var providers = RenderProviders();
        var page = RenderPage();
        page.Find("button[aria-label='Where used: Ortografia']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Where \"Ortografia\" is used"));

        return (providers.Dialogs, page);
    }

    // AC7, BR7: only the topic in use has the action.
    [Fact]
    public void Load_OnlyTheTopicInUse_HasTheWhereUsedAction()
    {
        var page = RenderPage();

        page.FindAll("button[aria-label='Where used: Ortografia']").Should().ContainSingle();
        page.FindAll("button[aria-label='Where used: Crase']").Should().BeEmpty();
    }

    // AC5, UC3, BR6: with questions.manage the count is a link to the question list filtered by the topic.
    [Fact]
    public void WhereUsed_WithQuestionsManage_TheCountLinksToTheFilteredQuestionList()
    {
        Authorization.SetPolicies(PermissionPolicy.Prefix + QuestionBankPermissions.Manage);

        var (dialogs, _) = OpenWhereUsed(new WhereUsedResponse([GuardMapping], 12));

        dialogs.WaitForAssertion(() => dialogs.Find($"a[href='{QuestionListHref}']").TextContent.Should().Contain("Used by 12 questions"));
        dialogs.FindAll(".app-where-used-mapping").Should().ContainSingle()
            .Which.TextContent.Should().Contain("Guarda Municipal SP · 2025").And.Contain("Língua Portuguesa");
    }

    // AC5, UC4, BR6, BR10: with catalog.manage only the count shows as plain text, with no link.
    [Fact]
    public void WhereUsed_WithCatalogManageOnly_TheCountIsPlainText()
    {
        var (dialogs, _) = OpenWhereUsed(new WhereUsedResponse([GuardMapping], 12));

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Used by 12 questions"));
        dialogs.FindAll($"a[href='{QuestionListHref}']").Should().BeEmpty();
        dialogs.FindAll("a[href^='/admin/questions']").Should().BeEmpty();
    }

    // AC6, BR3: a topic used only by questions shows no mapping row and the count; one question reads in the singular.
    [Fact]
    public void WhereUsed_TopicUsedOnlyByQuestions_ShowsNoMappingAndTheCount()
    {
        var (dialogs, _) = OpenWhereUsed(new WhereUsedResponse([], 1));

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Used by 1 question"));
        dialogs.Markup.Should().NotContain("Used by 1 questions");
        dialogs.FindAll(".app-where-used-mapping").Should().BeEmpty();
        dialogs.Markup.Should().NotContain("Notice subjects");
    }

    // BR6: a count of zero is not shown.
    [Fact]
    public void WhereUsed_TopicOnlyMapped_ShowsNoQuestionsPart()
    {
        var (dialogs, _) = OpenWhereUsed(new WhereUsedResponse([GuardMapping], 0));

        dialogs.WaitForAssertion(() => dialogs.FindAll(".app-where-used-mapping").Should().ContainSingle());
        dialogs.Markup.Should().NotContain("Used by");
    }

    // AC8, BR8: the topic was released since the section loaded - the empty state shows, and on close the section is
    // read again and redrawn without the action (the handler is a plain delegate: project rule F-95, F-86).
    [Fact]
    public void WhereUsed_TopicReleasedSinceTheSectionLoaded_ShowsTheEmptyStateAndReloadsTheSectionOnClose()
    {
        WhereUsed[MappedTopic.Id] = new WhereUsedResponse([], 0);
        var providers = RenderProviders();
        var page = RenderPage();
        Topics[Topics.IndexOf(MappedTopic)] = MappedTopic with { InUse = false };
        var reads = TopicReads;

        page.Find("button[aria-label='Where used: Ortografia']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Nothing uses this item any more."));
        providers.Dialogs.Find("#where-used-close").Click();

        page.WaitForAssertion(() => TopicReads.Should().BeGreaterThan(reads));
        page.WaitForAssertion(() => page.FindAll("button[aria-label='Where used: Ortografia']").Should().BeEmpty());
    }
}
