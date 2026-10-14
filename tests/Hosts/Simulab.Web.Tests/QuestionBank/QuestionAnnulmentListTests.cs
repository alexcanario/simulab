using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-101 `/admin/questions` (AC16, UC3): an annulled question shows its own chip, the status filter offers it
/// and finds it, and its row offers neither Publish nor Unpublish and cannot be deleted.
/// </summary>
public sealed class QuestionAnnulmentListTests : QuestionPageTestContext
{
    private static readonly QuestionResponse Annulled = Published with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000030"),
        Status = QuestionStatus.Annulled,
        CurrentVersion = 2,
        Annulment = new QuestionAnnulmentResponse("A banca anulou o item.", new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero))
    };

    private IRenderedComponent<Questions> RenderPage() => Render<Questions>();

    private static void WaitForRows(IRenderedComponent<Questions> page, int count) =>
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(count));

    private static IReadOnlyList<string> CellsOf(IRenderedComponent<Questions> page, int column) =>
        [.. page.FindAll("tbody tr").Select(row => row.QuerySelectorAll("td")[column].TextContent.Trim())];

    private static string Action(Guid id, string key) => $"#question-actions-{id}-{key}";

    // AC16: the chip says "Annulled" in the error tone, beside the neutral and the success ones.
    [Fact]
    public void Row_Annulled_ShowsTheAnnulledChipInTheErrorTone()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(Published);
        Api.Questions.Add(Annulled);

        var page = RenderPage();

        WaitForRows(page, 3);
        CellsOf(page, 5).Should().Equal("Draft", "Published", "Annulled");
        page.FindAll(".app-status-chip-error").Should().ContainSingle();
        page.Find(".app-status-chip-error").TextContent.Should().Contain("Annulled");
    }

    // AC16: no Publish and no Unpublish on an annulled row; Delete is disabled and says why.
    [Fact]
    public void Row_Annulled_HasNeitherPublishNorUnpublish_AndDeleteIsDisabledWithTheReason()
    {
        Api.Questions.Add(Annulled);
        var page = RenderPage();
        WaitForRows(page, 1);

        page.FindAll(Action(Annulled.Id, "publish")).Should().BeEmpty();
        page.FindAll(Action(Annulled.Id, "unpublish")).Should().BeEmpty();
        page.Find(Action(Annulled.Id, "delete")).HasAttribute("disabled").Should().BeTrue();
        page.FindComponents<AppRowActions>().Single().Instance.DeleteDisabledReason
            .Should().Be("This question is annulled and cannot be deleted.");
        page.FindAll(Action(Annulled.Id, "edit")).Should().ContainSingle("opening it to read, reinstate or see its history stays");
    }

    // AC16, UC3, BR11: the filter offers Annulled and sends it; only the annulled row stays.
    [Fact]
    public void Filter_ByAnnulled_SendsTheStatus_AndOnlyTheAnnulledRowStays()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(Published);
        Api.Questions.Add(Annulled);
        var page = RenderPage();
        WaitForRows(page, 3);

        page.Find("#questions-filter-status").Click();
        page.FindAll("[id^='questions-filter-status-option-']")
            .Select(option => option.QuerySelector(".app-mp-option-name")!.TextContent.Trim())
            .Should().Equal("Draft", "Published", "Annulled");
        page.Find("#questions-filter-status-option-Annulled").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Path == "/api/v1/question-bank/questions"
            && call.Query!.Contains("status=Annulled", StringComparison.Ordinal)));
        WaitForRows(page, 1);
        CellsOf(page, 5).Should().Equal("Annulled");
    }
}
