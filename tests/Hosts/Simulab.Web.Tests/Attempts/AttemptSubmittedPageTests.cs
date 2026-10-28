using System.Net;
using Bunit;
using Simulab.ExamEngine.Contracts;

namespace Simulab.Web.Tests.Attempts;

/// <summary>
/// F-169 `/attempts/{id}/submitted`: what the student sees after submitting — the edition, when it was submitted, how many
/// questions were answered and how many left blank, and the way back to the edition (UC5, BR10, BR11).
/// </summary>
public sealed class AttemptSubmittedPageTests : AttemptTestContext
{
    // The sample is submitted on 27 October 2026 at 18:42 UTC. The expected texts are formatted in the user's culture, as
    // the page does, so a platform that spells the time with a narrow no-break space is read the same way.
    private static string DateText(int hour) =>
        new DateTime(2026, 10, 27, hour, 42, 0, DateTimeKind.Unspecified).ToString("d", System.Globalization.CultureInfo.CurrentCulture);

    private static string TimeText(int hour) =>
        new DateTime(2026, 10, 27, hour, 42, 0, DateTimeKind.Unspecified).ToString("t", System.Globalization.CultureInfo.CurrentCulture);

    private IRenderedComponent<Simulab.Web.Components.Pages.Attempts.AttemptSubmitted> RenderLoaded()
    {
        var page = RenderSubmitted();
        page.WaitForAssertion(() => page.FindAll(".app-page-state").Should().ContainSingle());
        return page;
    }

    // AC11: the edition, the date and the time of the submission, the two counts and a link to the edition.
    [Fact]
    public void Render_ShowsTheEditionTheDateAndTimeTheCountsAndTheLinkToTheEdition()
    {
        Api.Content = AttemptSamples.Submitted(new Dictionary<int, AnswerPayload>
        {
            [1] = new SingleChoicePayload(0),
            [2] = new TrueFalsePayload(false)
        });

        var page = RenderLoaded();

        page.Find("h1").TextContent.Should().Be("Exam submitted");
        page.Find(".app-page-state-message").TextContent
            .Should().Be($"Your answers to {AttemptSamples.EditionLabel} were submitted on {DateText(18)} at {TimeText(18)}.");
        page.Find("#attempt-submitted-answered").TextContent.Should().Be("2");
        page.Find("#attempt-submitted-blank").TextContent.Should().Be("4");
        page.FindAll(".app-attempt-summary-row dt").Select(term => term.TextContent).Should().Equal("Answered", "Blank");

        var back = page.Find("a.mud-button-root");
        back.TextContent.Trim().Should().Be("Back to the exam");
        back.GetAttribute("href").Should().Be($"/catalog/exams/{AttemptSamples.ExamId}");
        page.FindAll("h1").Should().ContainSingle();
    }

    // BR10: the time is shown in the user's time zone, not in UTC.
    [Fact]
    public void Render_ShowsTheTimeInTheUsersTimeZone()
    {
        JSInterop.Setup<string?>(Simulab.Web.Services.UserTimeZone.TimeZoneFunction).SetResult("America/Sao_Paulo");
        Api.Content = AttemptSamples.Submitted();

        var page = RenderSubmitted();

        page.WaitForAssertion(() => page.Find(".app-page-state-message").TextContent.Should().Contain($"at {TimeText(15)}."));
    }

    // The edition no longer exists: the message does not name it and the way back is the catalog.
    [Fact]
    public void Render_WithoutAnEdition_SaysWhenItWasSubmitted_AndGoesBackToTheCatalog()
    {
        Api.Content = AttemptSamples.Submitted(withEdition: false);

        var page = RenderLoaded();

        page.Find(".app-page-state-message").TextContent.Should().Be($"Your answers were submitted on {DateText(18)} at {TimeText(18)}.");
        page.Find("a.mud-button-root").GetAttribute("href").Should().Be("/catalog");
    }

    // An attempt still in progress opens the simulator.
    [Fact]
    public void Open_AnAttemptInProgress_OpensTheSimulator()
    {
        Api.Content = AttemptSamples.InProgress();

        var page = RenderSubmitted();

        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith($"/attempts/{AttemptSamples.AttemptId}"));
        page.FindAll(".app-page-state").Should().BeEmpty();
    }

    // AC16, BR11: another student's attempt looks exactly like an id that does not exist, and nothing of it shows.
    [Fact]
    public void Open_AnAttemptThatIsNotTheUsers_ShowsTheNotFoundPage_AndNothingOfTheAttempt()
    {
        Api.ContentFailure = (HttpStatusCode.NotFound, ExamAttemptErrorCodes.NotFound);

        var page = RenderSubmitted();

        page.WaitForAssertion(() => page.Find(".app-page-state-title").TextContent.Should().Be("Page not found"));
        page.Markup.Should().NotContain("Guarda Municipal").And.NotContain("Exam submitted");
        page.FindAll("a.mud-button-root").Select(link => link.GetAttribute("href")).Should().Equal("/");
    }

    [Fact]
    public async Task Open_WhileItLoads_ShowsTheLoadingState()
    {
        Api.Content = AttemptSamples.Submitted();
        var hold = new TaskCompletionSource();
        Api.HoldContent = hold;

        var page = RenderSubmitted();

        page.WaitForAssertion(() => page.FindAll(".app-state-loading").Should().ContainSingle());
        await page.InvokeAsync(() => hold.SetResult());
        page.WaitForAssertion(() => page.Find("h1").TextContent.Should().Be("Exam submitted"));
    }

    [Fact]
    public void Open_WhenItCannotBeLoaded_ShowsTheErrorStateAndTryAgainLoadsIt()
    {
        Api.ContentFailure = (HttpStatusCode.InternalServerError, "common.server_error");
        var page = RenderSubmitted();
        page.WaitForAssertion(() => page.Find(".app-state-error").TextContent.Should().Contain("The submitted exam could not be loaded."));

        Api.ContentFailure = null;
        Api.Content = AttemptSamples.Submitted();
        page.Find(".app-retry").Click();

        page.WaitForAssertion(() => page.Find("h1").TextContent.Should().Be("Exam submitted"));
    }

    // A long edition label wraps in the message, which is the kit's wrapping paragraph.
    [Fact]
    public void LongText_WrapsInTheMessage()
    {
        var content = AttemptSamples.Submitted();
        Api.Content = content with { Edition = content.Edition! with { ExamName = new string('x', 300) } };

        var page = RenderLoaded();

        page.Find(".app-page-state-message").TextContent.Should().Contain(new string('x', 300));
        page.Find(".app-page-state-message").HasAttribute("style").Should().BeFalse();
    }
}
