using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-105 `/admin/questions` (AC13, BR1, BR3): a question in review shows its own chip in the waiting tone, the
/// status filter offers it and sends it, and its row offers neither Publish nor Unpublish and cannot be deleted.
/// </summary>
public sealed class QuestionReviewListTests : QuestionPageTestContext
{
    private static readonly QuestionResponse InReview = Published with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000040"),
        Status = QuestionStatus.InReview,
        CurrentVersion = null,
        SubmittedAt = new DateTimeOffset(2026, 10, 16, 14, 2, 0, TimeSpan.Zero)
    };

    private IRenderedComponent<Questions> RenderPage() => Render<Questions>();

    private static void WaitForRows(IRenderedComponent<Questions> page, int count) =>
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(count));

    private static string Action(Guid id, string key) => $"#question-actions-{id}-{key}";

    // AC13: the chip says "In review" in the waiting tone, beside the neutral and the success ones.
    [Fact]
    public void Row_InReview_ShowsTheInReviewChipInTheWarningTone()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(Published);
        Api.Questions.Add(InReview);

        var page = RenderPage();

        WaitForRows(page, 3);
        page.FindAll(".app-status-chip-warning").Should().ContainSingle();
        page.Find(".app-status-chip-warning").TextContent.Should().Contain("In review");
    }

    // AC13, BR3: no Publish and no Unpublish on the row; Delete is disabled and says why; Edit stays.
    [Fact]
    public void Row_InReview_HasNeitherPublishNorUnpublish_AndDeleteIsDisabledWithTheReason()
    {
        Api.Questions.Add(InReview);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.FindAll(Action(InReview.Id, "publish")).Should().BeEmpty();
        page.FindAll(Action(InReview.Id, "unpublish")).Should().BeEmpty();
        page.FindAll(Action(InReview.Id, "edit")).Should().ContainSingle("opening it to withdraw it or see its history stays");
        var delete = page.Find(Action(InReview.Id, "delete"));
        delete.HasAttribute("disabled").Should().BeTrue();
        page.FindComponents<AppRowActions>().Single().Instance.DeleteDisabledReason
            .Should().Be("This question is in review. Withdraw it before deleting.");
    }

    // AC13, BR1: the filter offers In review and sends it as the status.
    [Fact]
    public void Filter_ByInReview_SendsTheStatus_AndOnlyTheRowInReviewStays()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(Published);
        Api.Questions.Add(InReview);
        var page = RenderPage();
        WaitForRows(page, 3);

        page.Find("#questions-filter-status").Click();
        page.Find("#questions-filter-status-option-InReview").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Get && call.Query != null && call.Query.Contains("status=InReview", StringComparison.Ordinal)));
        WaitForRows(page, 1);
        page.Find("tbody tr").TextContent.Should().Contain("In review");
    }
}
