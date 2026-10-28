using System.Net;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Simulab.ExamEngine.Contracts;
using Simulab.Web.Components.Layout;

namespace Simulab.Web.Tests.Attempts;

/// <summary>
/// F-169 `/attempts/{id}`, the simulator: the first question, the sections and the answer sheet, previous and next, marking
/// and clearing an answer, a save that fails, a question of another type, and the submit with its warning (UC2 to UC5).
/// </summary>
public sealed class TakeAttemptPageTests : AttemptTestContext
{
    private static Dictionary<int, AnswerPayload> Answers(params int[] papers) =>
        papers.ToDictionary(paper => paper, paper => (AnswerPayload)new SingleChoicePayload(0));

    private static IElement NavButton(IRenderedComponent<ExamSessionLayout> layout, int index) =>
        layout.FindAll(".app-session-nav button")[index];

    private static string Status(IRenderedComponent<ExamSessionLayout> layout) =>
        layout.Find(".app-save-status").TextContent.Trim();

    // A save is over when the request was sent, the group is no longer busy and the status stopped saying "Saving…".
    private void WaitForWrites(IRenderedComponent<ExamSessionLayout> layout, int writes) =>
        layout.WaitForAssertion(() =>
        {
            Api.Writes.Should().HaveCount(writes);
            layout.FindAll(".app-answer-options[aria-busy=true]").Should().BeEmpty();
            Status(layout).Should().NotBe("Saving…");
        });

    // AC5, AC3, AC12: a new attempt opens on its first question, with its section, its base text above the statement and
    // its options in the content language; nothing is marked.
    [Fact]
    public void Open_ANewAttempt_ShowsTheFirstQuestionWithItsSectionTheBaseTextAndTheOptions()
    {
        Api.Content = AttemptSamples.InProgress();

        var layout = RenderSimulator();

        Heading(layout).Should().Be("Question 1");
        layout.Find(".app-session-question-position").TextContent.Should().Be("1 of 6");
        layout.Find(".app-session-section-label").TextContent.Should().Be("Língua Portuguesa");
        layout.Find(".app-session-section-label").GetAttribute("lang").Should().Be("pt-BR");
        layout.Find(".app-session-section-range").TextContent.Should().Be("Questions 1 to 3");
        layout.Find(".app-session-caption").TextContent.Should().Be("Base text");

        var markup = layout.Markup;
        var baseText = markup.IndexOf("The base text of question 1.", StringComparison.Ordinal);
        var statement = markup.IndexOf("Statement 1", StringComparison.Ordinal);
        baseText.Should().BeGreaterThan(-1);
        statement.Should().BeGreaterThan(baseText, "the base text is above the statement");
        layout.Find(".app-session-statement").GetAttribute("lang").Should().Be("pt-BR");

        var rows = layout.FindAll("[role=radio]");
        rows.Should().HaveCount(4);
        rows.Select(row => row.GetAttribute("aria-checked")).Should().OnlyContain(value => value == "false");
        rows.Select(row => row.QuerySelector(".app-answer-letter")!.TextContent).Should().Equal("A", "B", "C", "D");
        layout.Find("[role=radiogroup]").GetAttribute("aria-labelledby").Should().Be("attempt-question-title");
        layout.Find("h1").TextContent.Should().Contain(AttemptSamples.EditionLabel);
        layout.Find(".app-answer-sheet-progress").TextContent.Should().Be("0 of 6 answered");
    }

    // AC12: a picture in the statement or the base text keeps the address the content read gave it, which points at the
    // attempt, and its stored alt text; the screen adds nothing of its own to it.
    [Fact]
    public void ThePicturesOfAQuestion_KeepTheirAttemptAddressAndTheirAltText()
    {
        var imageId = Guid.Parse("0198f0a3-5000-7000-8000-0000000000b1");
        var address = $"/attempts/{AttemptSamples.AttemptId}/images/{imageId}";
        var content = AttemptSamples.InProgress();
        Api.Content = content with
        {
            Questions = [content.Questions[0] with { StatementHtml = $"<p>See <img src=\"{address}\" alt=\"A bar chart\"></p>" }, .. content.Questions.Skip(1)]
        };

        var layout = RenderSimulator();

        var picture = layout.Find(".app-session-statement img");
        picture.GetAttribute("src").Should().Be(address);
        picture.GetAttribute("alt").Should().Be("A bar chart");
    }

    // AC1 (second half), BR5: a resumed attempt opens on its first blank question, and the sheet shows the stored answers.
    [Fact]
    public void Open_AResumedAttempt_ShowsTheFirstBlankQuestion_AndTheSheetShowsTheStoredAnswers()
    {
        Api.Content = AttemptSamples.InProgress(new Dictionary<int, AnswerPayload>
        {
            [1] = new SingleChoicePayload(1),
            [2] = new TrueFalsePayload(true)
        });

        var layout = RenderSimulator();

        Heading(layout).Should().Be("Question 3");
        layout.Find(".app-session-question-position").TextContent.Should().Be("3 of 6");
        layout.Find(".app-answer-sheet-progress").TextContent.Should().Be("2 of 6 answered");
        Cell(layout, 1).ClassList.Should().Contain("app-answer-cell-answered");
        Cell(layout, 2).ClassList.Should().Contain("app-answer-cell-answered");
        Cell(layout, 3).ClassList.Should().NotContain("app-answer-cell-answered");
        Cell(layout, 3).GetAttribute("aria-current").Should().Be("step");
    }

    // BR5: with every question answered, the attempt opens on the first.
    [Fact]
    public void Open_AnAttemptWithNoBlankQuestion_ShowsTheFirstQuestion()
    {
        Api.Content = AttemptSamples.InProgress(Answers(1, 2, 3, 4, 6, 7));

        var layout = RenderSimulator();

        Heading(layout).Should().Be("Question 1");
        Option(layout, 0).GetAttribute("aria-checked").Should().Be("true");
    }

    // AC3, AC4: every number of the attempt grouped by section in paper order, the numbering jumping where a question was
    // left out, the questions with no notice subject last, the current one marked, a legend, and a click shows the question.
    [Fact]
    public void AnswerSheet_ListsEveryNumberGroupedBySection_WithTheNumberingJumping_AndTheOthersLast()
    {
        Api.Content = AttemptSamples.InProgress();

        var layout = RenderSimulator();

        layout.FindAll(".app-answer-sheet-section-label").Select(label => label.TextContent)
            .Should().Equal("Língua Portuguesa", "Legislação", "Other");
        layout.FindAll(".app-answer-sheet-range").Select(range => range.TextContent)
            .Should().Equal("1–3", "4–6", "7–7");
        layout.FindAll(".app-answer-cell").Select(cell => cell.TextContent.Trim())
            .Should().Equal("1", "2", "3", "4", "6", "7");
        layout.FindAll(".app-answer-cell").Select(cell => cell.GetAttribute("aria-label"))
            .Should().OnlyContain(name => name!.EndsWith(": Blank", StringComparison.Ordinal));
        Cell(layout, 1).GetAttribute("aria-current").Should().Be("step");
        layout.FindAll(".app-answer-cell[aria-current=step]").Should().ContainSingle();
        layout.Find(".app-answer-legend").TextContent.Should().Contain("Blank").And.Contain("Answered").And.Contain("Current");

        var sheet = layout.Find("nav.app-answer-sheet");
        layout.Find($"#{sheet.GetAttribute("aria-labelledby")}").TextContent.Should().Be("Answer sheet");

        Cell(layout, 6).Click();

        layout.WaitForAssertion(() => Heading(layout).Should().Be("Question 6"));
        layout.Find(".app-session-question-position").TextContent.Should().Be("5 of 6");
        layout.Find(".app-session-section-range").TextContent.Should().Be("Questions 4 to 6");
        Cell(layout, 6).GetAttribute("aria-current").Should().Be("step");
    }

    // AC5: next and previous show the neighbour in paper order across the sections; previous is unavailable on the first
    // question and next on the last.
    [Fact]
    public void NextAndPrevious_ShowTheNeighbourInPaperOrder_AcrossSections_AndAreDisabledAtTheEnds()
    {
        Api.Content = AttemptSamples.InProgress();
        var layout = RenderSimulator();

        NavButton(layout, 0).HasAttribute("disabled").Should().BeTrue("previous is unavailable on the first question");
        NavButton(layout, 1).HasAttribute("disabled").Should().BeFalse();

        foreach (var expected in new[] { "Question 2", "Question 3", "Question 4" })
        {
            NavButton(layout, 1).Click();
            layout.WaitForAssertion(() => Heading(layout).Should().Be(expected));
        }

        layout.Find(".app-session-section-label").TextContent.Should().Be("Legislação", "next crosses into the second section");
        NavButton(layout, 0).HasAttribute("disabled").Should().BeFalse();

        NavButton(layout, 1).Click();
        layout.WaitForAssertion(() => Heading(layout).Should().Be("Question 6"));
        NavButton(layout, 1).Click();
        layout.WaitForAssertion(() => Heading(layout).Should().Be("Question 7"));
        layout.Find(".app-session-section-label").TextContent.Should().Be("Other");
        NavButton(layout, 1).HasAttribute("disabled").Should().BeTrue("next is unavailable on the last question");

        NavButton(layout, 0).Click();
        layout.WaitForAssertion(() => Heading(layout).Should().Be("Question 6"));
    }

    // BR5 and the accessibility notes: after a move the focus is on the question's heading.
    [Fact]
    public void Moving_ToAnotherQuestion_MovesTheFocusToItsHeading()
    {
        Api.Content = AttemptSamples.InProgress();
        var layout = RenderSimulator();

        NavButton(layout, 1).Click();

        layout.WaitForAssertion(() => JSInterop.Invocations["simulabShell.focusElement"]
            .Should().ContainSingle().Which.Arguments.Should().Equal("attempt-question-title"));
        layout.Find("#attempt-question-title").GetAttribute("tabindex").Should().Be("-1");
    }

    // AC6: marking an option saves the answer and the sheet shows it answered; marking another replaces the first;
    // clicking the marked one again clears the answer and the sheet shows it blank.
    [Fact]
    public void MarkingAnOption_SavesIt_AnotherReplacesIt_AndClickingTheMarkedOneClearsIt()
    {
        Api.Content = AttemptSamples.InProgress();
        var layout = RenderSimulator();

        Option(layout, 1).Click();
        WaitForWrites(layout, 1);

        var saved = Api.Writes.Single();
        saved.Method.Should().Be(HttpMethod.Put);
        saved.Path.Should().EndWith($"/answers/{AttemptSamples.Q1}");
        saved.Body.Should().Contain("\"type\":\"SingleChoice\"").And.Contain("\"optionPosition\":1");
        Status(layout).Should().Be("Answer saved");
        Option(layout, 1).GetAttribute("aria-checked").Should().Be("true");
        Option(layout, 1).ClassList.Should().Contain("app-answer-option-selected");
        Cell(layout, 1).ClassList.Should().Contain("app-answer-cell-answered");
        layout.Find(".app-answer-sheet-progress").TextContent.Should().Be("1 of 6 answered");

        Option(layout, 2).Click();
        WaitForWrites(layout, 2);

        Api.Writes.Last().Body.Should().Contain("\"optionPosition\":2");
        Option(layout, 1).GetAttribute("aria-checked").Should().Be("false");
        Option(layout, 2).GetAttribute("aria-checked").Should().Be("true");

        Option(layout, 2).Click();
        WaitForWrites(layout, 3);

        Api.Writes.Last().Method.Should().Be(HttpMethod.Delete);
        Api.Writes.Last().Path.Should().EndWith($"/answers/{AttemptSamples.Q1}");
        Status(layout).Should().Be("Answer cleared");
        Option(layout, 2).GetAttribute("aria-checked").Should().Be("false");
        Cell(layout, 1).ClassList.Should().NotContain("app-answer-cell-answered");
        layout.Find(".app-answer-sheet-progress").TextContent.Should().Be("0 of 6 answered");
    }

    // BR6: while a save is in flight the row already shows the click, the group is busy, the status says so, and the sheet
    // changes only after the save succeeded.
    [Fact]
    public async Task WhileASaveIsInFlight_TheRowShowsTheClick_TheGroupIsBusy_AndTheSheetWaits()
    {
        Api.Content = AttemptSamples.InProgress();
        var hold = new TaskCompletionSource();
        Api.HoldSave = hold;
        var layout = RenderSimulator();

        Option(layout, 3).Click();

        layout.WaitForAssertion(() =>
        {
            layout.Find(".app-answer-options").GetAttribute("aria-busy").Should().Be("true");
            Option(layout, 3).GetAttribute("aria-checked").Should().Be("true");
            Status(layout).Should().Be("Saving…");
        });
        layout.Find(".app-save-status [role=progressbar]").ParentElement!.GetAttribute("aria-hidden").Should().Be("true");
        Cell(layout, 1).ClassList.Should().NotContain("app-answer-cell-answered", "the sheet changes only after the save succeeded");
        Option(layout, 0).Click();
        Api.Writes.Should().HaveCountLessThanOrEqualTo(1, "a busy group ignores further clicks");

        await layout.InvokeAsync(() => hold.SetResult());

        layout.WaitForAssertion(() =>
        {
            Status(layout).Should().Be("Answer saved");
            Cell(layout, 1).ClassList.Should().Contain("app-answer-cell-answered");
        });
        Api.Writes.Should().ContainSingle();
    }

    // AC6 (true or false): two rows with the app's own words; marking one saves a boolean.
    [Fact]
    public void TrueFalse_ShowsTwoRows_AndMarkingFalseSavesFalse()
    {
        Api.Content = AttemptSamples.InProgress();
        var layout = RenderSimulator();
        Cell(layout, 2).Click();
        layout.WaitForAssertion(() => Heading(layout).Should().Be("Question 2"));

        layout.FindAll("[role=radio]").Select(row => row.TextContent.Trim()).Should().Equal("True", "False");
        layout.Find(".app-session-hint").TextContent.Should().Be("Judge the statement. Click your choice again to clear it.");

        Option(layout, 1).Click();
        WaitForWrites(layout, 1);

        Api.Writes.Single().Body.Should().Contain("\"type\":\"TrueFalse\"").And.Contain("\"value\":false");
        Option(layout, 1).GetAttribute("aria-checked").Should().Be("true");
    }

    // AC7: a multiple-answer question saves the whole set on each tick, and unticking the last one clears the answer.
    [Fact]
    public void MultipleAnswer_TickingTwoSavesBoth_AndUntickingBothClearsTheAnswer()
    {
        Api.Content = AttemptSamples.InProgress();
        var layout = RenderSimulator();
        Cell(layout, 3).Click();
        layout.WaitForAssertion(() => Heading(layout).Should().Be("Question 3"));
        layout.FindAll("[role=checkbox]").Should().HaveCount(4);
        layout.Find("[role=group]").GetAttribute("aria-labelledby").Should().Be("attempt-question-title");

        Option(layout, 0).Click();
        WaitForWrites(layout, 1);
        Option(layout, 2).Click();
        WaitForWrites(layout, 2);

        Api.Writes.Last().Body.Should().Contain("\"type\":\"MultipleAnswer\"").And.Contain("\"optionPositions\":[0,2]");
        Option(layout, 0).GetAttribute("aria-checked").Should().Be("true");
        Option(layout, 2).GetAttribute("aria-checked").Should().Be("true");
        Cell(layout, 3).ClassList.Should().Contain("app-answer-cell-answered");

        Option(layout, 0).Click();
        WaitForWrites(layout, 3);
        Api.Writes.Last().Body.Should().Contain("\"optionPositions\":[2]");
        Cell(layout, 3).ClassList.Should().Contain("app-answer-cell-answered");

        Option(layout, 2).Click();
        WaitForWrites(layout, 4);
        Api.Writes.Last().Method.Should().Be(HttpMethod.Delete);
        Cell(layout, 3).ClassList.Should().NotContain("app-answer-cell-answered");
    }

    // The arrow keys of a radio group move focus without marking anything; a checkbox group does not take them.
    [Fact]
    public void ArrowKeys_MoveTheFocusBetweenRows_WithoutMarkingOrSaving()
    {
        Api.Content = AttemptSamples.InProgress();
        var layout = RenderSimulator();

        Option(layout, 0).KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        layout.WaitForAssertion(() => JSInterop.Invocations["simulabShell.focusOption"]
            .Should().ContainSingle().Which.Arguments.Should().Equal("attempt-options-option-1"));
        Api.Writes.Should().BeEmpty();
        layout.FindAll("[role=radio][aria-checked=true]").Should().BeEmpty();
        Option(layout, 0).GetAttribute("tabindex").Should().Be("0", "the first row is the tab stop of a blank group");
        Option(layout, 1).GetAttribute("tabindex").Should().Be("-1");
    }

    // AC8 (first half), BR7: a save the Api refuses puts the row back as it was and says the answer was not saved.
    [Fact]
    public void ASaveThatFails_PutsTheRowBack_AndSaysTheAnswerWasNotSaved()
    {
        Api.Content = AttemptSamples.InProgress();
        Api.SaveFailure = (HttpStatusCode.InternalServerError, "common.server_error");
        var layout = RenderSimulator();

        Option(layout, 1).Click();

        layout.WaitForAssertion(() =>
        {
            layout.Find(".app-alert").TextContent.Should().Contain("Your answer was not saved.");
            layout.FindAll(".app-answer-options[aria-busy=true]").Should().BeEmpty();
        });
        layout.Find(".app-alert").GetAttribute("role").Should().Be("alert");
        Option(layout, 1).GetAttribute("aria-checked").Should().Be("false");
        Cell(layout, 1).ClassList.Should().NotContain("app-answer-cell-answered");
        Status(layout).Should().BeEmpty();
    }

    // BR7: a code of F-166 that the student can act on is told in its own words.
    [Fact]
    public void ASaveThatIsRefusedWithACodeOfTheAttempt_ShowsThatCodesText()
    {
        Api.Content = AttemptSamples.InProgress();
        Api.SaveFailure = (HttpStatusCode.UnprocessableEntity, ExamAttemptErrorCodes.AnswerInvalid);
        var layout = RenderSimulator();

        Option(layout, 0).Click();

        layout.WaitForAssertion(() => layout.Find(".app-alert").TextContent.Should().Contain("The answer is not valid. Check it and try again."));
        Option(layout, 0).GetAttribute("aria-checked").Should().Be("false");
    }

    // AC8 (second half): a save refused because the attempt was already submitted opens the submitted page.
    [Fact]
    public void ASaveRefusedBecauseTheAttemptIsSubmitted_OpensTheSubmittedPage()
    {
        Api.Content = AttemptSamples.InProgress();
        Api.SaveFailure = (HttpStatusCode.Conflict, ExamAttemptErrorCodes.AlreadySubmitted);
        var layout = RenderSimulator();

        Option(layout, 1).Click();

        layout.WaitForAssertion(() => Navigation.Uri.Should().EndWith($"/attempts/{AttemptSamples.AttemptId}/submitted"));
    }

    // AC9, BR8: a question of another type shows its statement and base text and a notice, no answer area, and stays blank.
    [Fact]
    public void AQuestionOfAnotherType_ShowsItsStatementAndBaseTextAndTheNotice_AndStaysBlank()
    {
        Api.Content = AttemptSamples.InProgress();
        var layout = RenderSimulator();

        Cell(layout, 4).Click();

        layout.WaitForAssertion(() => Heading(layout).Should().Be("Question 4"));
        layout.Find(".app-session-statement").TextContent.Should().Contain("Statement 4, a matching question");
        layout.Markup.Should().Contain("The base text of question 4.");
        layout.FindAll(".app-answer-options").Should().BeEmpty();
        layout.Find(".app-alert").TextContent.Should().Contain("This kind of answer is not in the simulator yet.");
        Cell(layout, 4).ClassList.Should().NotContain("app-answer-cell-answered");
        layout.Find(".app-answer-sheet-progress").TextContent.Should().Be("0 of 6 answered");
    }

    // AC10: the dialog counts the blank questions and lists their numbers, says that a submitted attempt cannot be changed,
    // and cancelling changes nothing.
    [Fact]
    public void Submit_AsksWithTheBlankQuestionsListed_AndCancellingChangesNothing()
    {
        Api.Content = AttemptSamples.InProgress(Answers(1, 2, 3));
        var layout = RenderSimulator();

        layout.Find(".app-session-submit button").Click();

        layout.WaitForAssertion(() => layout.Find(".mud-dialog").TextContent
            .Should().Contain("Submit the exam?")
            .And.Contain("3 questions are blank: 4, 6, 7.")
            .And.Contain("After submitting you cannot change your answers."));
        layout.Find(".app-confirm-ok").TextContent.Trim().Should().Be("Submit exam");
        layout.Find(".app-confirm-ok").ClassList.Should().Contain("mud-button-filled-primary", "submitting is not a destructive action");

        layout.Find(".app-confirm-cancel").Click();

        layout.WaitForAssertion(() => layout.FindAll(".mud-dialog").Should().BeEmpty());
        Api.Writes.Should().BeEmpty();
        Heading(layout).Should().Be("Question 4");
    }

    [Fact]
    public void Submit_WithOneBlankQuestion_SaysSoInTheSingular()
    {
        Api.Content = AttemptSamples.InProgress(Answers(1, 2, 3, 4, 6));
        var layout = RenderSimulator();

        layout.Find(".app-session-submit button").Click();

        layout.WaitForAssertion(() => layout.Find(".mud-dialog").TextContent.Should().Contain("1 question is blank: 7."));
    }

    [Fact]
    public void Submit_WithNoBlankQuestion_SaysNoneIsBlank()
    {
        Api.Content = AttemptSamples.InProgress(Answers(1, 2, 3, 4, 6, 7));
        var layout = RenderSimulator();

        layout.Find(".app-session-submit button").Click();

        layout.WaitForAssertion(() => layout.Find(".mud-dialog").TextContent
            .Should().Contain("No question is blank.").And.Contain("After submitting you cannot change your answers."));
    }

    // AC10: confirming submits the attempt and opens the submitted page.
    [Fact]
    public void Submit_Confirmed_SubmitsTheAttempt_AndOpensTheSubmittedPage()
    {
        Api.Content = AttemptSamples.InProgress(Answers(1, 2, 3, 4, 6, 7));
        var layout = RenderSimulator();
        layout.Find(".app-session-submit button").Click();
        layout.WaitForAssertion(() => layout.FindAll(".app-confirm-ok").Should().ContainSingle());

        layout.Find(".app-confirm-ok").Click();

        layout.WaitForAssertion(() =>
        {
            Api.Writes.Should().ContainSingle().Which.Path.Should().EndWith($"/attempts/{AttemptSamples.AttemptId}/submit");
            Navigation.Uri.Should().EndWith($"/attempts/{AttemptSamples.AttemptId}/submitted");
        });
    }

    // BR9: a submit that fails keeps the answers, says so with a way to try again, and the action asks again.
    [Fact]
    public void Submit_ThatFails_ShowsTheAlertWithTryAgain_AndTryAgainAsksAgain()
    {
        Api.Content = AttemptSamples.InProgress(Answers(1, 2, 3, 4, 6, 7));
        Api.SubmitFailure = (HttpStatusCode.InternalServerError, "common.server_error");
        var layout = RenderSimulator();
        layout.Find(".app-session-submit button").Click();
        layout.WaitForAssertion(() => layout.FindAll(".app-confirm-ok").Should().ContainSingle());

        layout.Find(".app-confirm-ok").Click();

        layout.WaitForAssertion(() => layout.Find(".app-alert").TextContent
            .Should().Contain("The exam was not submitted. Your answers are kept; try again."));
        layout.Find(".app-alert").GetAttribute("role").Should().Be("alert");
        layout.Find(".app-session-submit button").HasAttribute("disabled").Should().BeFalse();

        layout.Find(".app-alert-action").TextContent.Trim().Should().Be("Try again");
        layout.Find(".app-alert-action").Click();

        layout.WaitForAssertion(() => layout.FindAll(".app-confirm-ok").Should().ContainSingle());
    }

    // AC11 (second half): the screen of a submitted attempt opens its submitted page.
    [Fact]
    public void Open_ASubmittedAttempt_OpensItsSubmittedPage()
    {
        Api.Content = AttemptSamples.Submitted(Answers(1, 2));

        var layout = RenderSimulatorWithoutWaiting();

        layout.WaitForAssertion(() => Navigation.Uri.Should().EndWith($"/attempts/{AttemptSamples.AttemptId}/submitted"));
        layout.FindAll("#attempt-question-title").Should().BeEmpty();
    }

    // AC16, BR11: an attempt that is not the user's looks exactly like one that does not exist, and nothing of it shows.
    [Fact]
    public void Open_AnAttemptThatIsNotTheUsers_ShowsTheNotFoundPage_AndNothingOfTheAttempt()
    {
        Api.ContentFailure = (HttpStatusCode.NotFound, ExamAttemptErrorCodes.NotFound);

        var layout = RenderSimulatorWithoutWaiting();

        layout.WaitForAssertion(() => layout.Find(".app-page-state-title").TextContent.Should().Be("Page not found"));
        layout.FindAll("#attempt-question-title").Should().BeEmpty();
        layout.FindAll("nav.app-answer-sheet").Should().BeEmpty();
        layout.FindAll(".app-session-leave").Should().BeEmpty("the bar shows no edition");
        layout.FindAll(".app-session-title").Should().BeEmpty();
        layout.Markup.Should().NotContain("Statement").And.NotContain("Guarda Municipal");
    }

    [Fact]
    public async Task Open_WhileTheAttemptLoads_ShowsTheLoadingStateAndABarWithoutTheEdition()
    {
        Api.Content = AttemptSamples.InProgress();
        var hold = new TaskCompletionSource();
        Api.HoldContent = hold;

        var layout = RenderSimulatorWithoutWaiting();

        layout.WaitForAssertion(() => layout.FindAll(".app-state-loading").Should().ContainSingle());
        layout.FindAll(".app-session-title").Should().BeEmpty();

        await layout.InvokeAsync(() => hold.SetResult());

        layout.WaitForAssertion(() => Heading(layout).Should().Be("Question 1"));
        layout.FindAll(".app-state-loading").Should().BeEmpty();
    }

    [Fact]
    public void Open_WhenTheAttemptCannotBeLoaded_ShowsTheErrorStateAndTryAgainLoadsIt()
    {
        Api.ContentFailure = (HttpStatusCode.InternalServerError, "common.server_error");
        var layout = RenderSimulatorWithoutWaiting();
        layout.WaitForAssertion(() => layout.Find(".app-state-error").TextContent.Should().Contain("The exam could not be loaded."));

        Api.ContentFailure = null;
        Api.Content = AttemptSamples.InProgress();
        layout.Find(".app-retry").Click();

        layout.WaitForAssertion(() => Heading(layout).Should().Be("Question 1"));
    }

    // AC20, BR13: the bar has the way out, to the exam page of the edition; the session has no app menu.
    [Fact]
    public void TheBar_HasTheEditionAsTheH1_AndTheWayOutToTheExamPage_AndNoAppMenu()
    {
        Api.Content = AttemptSamples.InProgress();

        var layout = RenderSimulator();

        var leave = layout.Find("a.app-session-leave");
        leave.TextContent.Trim().Should().Be("Leave and continue later");
        leave.GetAttribute("href").Should().Be($"/catalog/exams/{AttemptSamples.ExamId}");
        layout.FindAll("h1").Should().ContainSingle().Which.TextContent.Should().Contain(AttemptSamples.EditionLabel);
        layout.Find("h1 [lang]").GetAttribute("lang").Should().Be("pt-BR");
        layout.FindAll("header.mud-appbar").Should().ContainSingle();
        layout.FindAll("#app-drawer, .app-breadcrumbs, .app-page-header").Should().BeEmpty();
        layout.Find(".app-skip-link").GetAttribute("href").Should().Be("#main-content");
        layout.FindAll("main#main-content").Should().ContainSingle().Which.Id.Should().Be("main-content");
        layout.FindAll(".app-theme-switch").Should().ContainSingle();
    }

    // The edition no longer exists: the bar shows no edition and no way out, and the question still opens.
    [Fact]
    public void TheBar_WithoutAnEdition_ShowsNeitherTheTitleNorTheWayOut()
    {
        Api.Content = AttemptSamples.InProgress(withEdition: false);

        var layout = RenderSimulator();

        layout.FindAll(".app-session-title").Should().BeEmpty();
        layout.FindAll(".app-session-leave").Should().BeEmpty();
        Heading(layout).Should().Be("Question 1");
    }

    // A long edition label truncates in the bar with its full text in the tooltip; long statements and options wrap.
    [Fact]
    public void LongText_TruncatesInTheBarAndWrapsInTheQuestion()
    {
        var longLabel = new string('x', 300);
        var content = AttemptSamples.InProgress();
        Api.Content = content with { Edition = content.Edition! with { ExamName = longLabel } };

        var layout = RenderSimulator();

        layout.Find(".app-session-title .app-truncate").TextContent.Should().Contain(longLabel);
        layout.Find(".app-session-title .app-truncate").GetAttribute("style").Should().Contain("max-width");
        layout.Find(".app-answer-text").HasAttribute("style").Should().BeFalse("an option's text wraps in its row and has no fixed width");
        layout.Find(".app-session-statement").ClassList.Should().Contain("app-question-content");
    }
}
