using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-107 `/admin/questions/new` and `/admin/questions/{id}`: the fifth type card, the "Insert blank" button of the
/// statement for this type only, the key section with one row per marker in statement order, what the form sends, the
/// checklist, the refusals, the key-change reason and the frozen state (AC14, AC18 as far as bUnit reaches).
/// </summary>
public sealed class FillInTheBlanksQuestionFormTests : QuestionPageTestContext
{
    private const string CapitalStatement = "<p>A capital do Brasil é [[1]] e foi fundada em [[2]].</p>";

    private static readonly QuestionResponse FillInDraft = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000170"),
        QuestionType.FillInTheBlanks,
        QuestionStatus.Draft,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        CapitalStatement,
        null,
        null,
        3,
        [],
        Blanks:
        [
            new(1, ["Brasília", "Brasilia"]),
            new(2, ["1960"])
        ]);

    private IRenderedComponent<QuestionForm> RenderAdd() => Render<QuestionForm>();

    private IRenderedComponent<QuestionForm> RenderEdit(Guid id) =>
        Render<QuestionForm>(parameters => parameters.Add(form => form.Id, id));

    private void WaitForEditors(IRenderedComponent<QuestionForm> page, params string[] ids) =>
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain(ids));

    private Task TypeAsync(IRenderedComponent<QuestionForm> page, string id, string html) =>
        page.InvokeAsync(() => RichText.TypeAsync(id, html));

    private static Task PickAsync(IRenderedComponent<QuestionForm> page, string id, AppLookupOption? option) =>
        page.InvokeAsync(() => page.FindComponents<AppLookupField>()
            .Single(component => component.Instance.Id == id).Instance.ValueChanged.InvokeAsync(option));

    private static string AnswerId(int blank, int answer) => $"question-blank-{blank}-answer-{answer}";

    private static AppTextField<string> AnswerField(IRenderedComponent<QuestionForm> page, int blank, int answer) =>
        page.FindComponents<AppTextField<string>>().Single(component => component.Instance.Id == AnswerId(blank, answer)).Instance;

    private static Task AnswerAsync(IRenderedComponent<QuestionForm> page, int blank, int answer, string text) =>
        page.InvokeAsync(() => AnswerField(page, blank, answer).ValueChanged.InvokeAsync(text));

    private static IReadOnlyList<string> RowTitles(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll(".app-blank-row .app-blank-title").Select(title => title.TextContent.Trim())];

    private static IReadOnlyList<string> ChecklistTexts(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll(".app-form-aside-checklist li").Select(item => item.TextContent)];

    private static bool HasBlankButton(IRenderedComponent<QuestionForm> page, string editorId) =>
        page.FindAll($"[role='toolbar'][aria-controls='{editorId}'] button[data-format='blank']").Count == 1;

    private async Task FillInAsync(IRenderedComponent<QuestionForm> page, string statement)
    {
        page.Find("#question-type-FillInTheBlanks").Click();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", statement);
    }

    // AC14, AC19: the fifth card, single choice still preselected (F-108 adds the sixth; its tests count them).
    [Fact]
    public void Add_OffersTheFillInCardWithSingleChoicePreselected()
    {
        var page = RenderAdd();

        page.Find("#question-type-SingleChoice").GetAttribute("aria-checked").Should().Be("true");
        page.Find("#question-type-FillInTheBlanks").GetAttribute("aria-checked").Should().Be("false");
        page.Find("#question-type-FillInTheBlanks").TextContent.Should().Contain("Fill in the blanks")
            .And.Contain("Blanks in the statement; the student types each answer.");
        page.Find("#question-type").QuerySelectorAll("[role='radio']").Should().HaveCount(6);
    }

    // AC14, BR12: "Insert blank" sits on the statement toolbar of this type only, named in words.
    [Fact]
    public async Task Add_ChoosingFillIn_ShowsInsertBlankOnTheStatementOnly_AndAnotherTypeHidesIt()
    {
        var page = RenderAdd();
        WaitForEditors(page, "question-statement");
        HasBlankButton(page, "question-statement").Should().BeFalse();

        await FillInAsync(page, "<p>Sem lacuna.</p>");

        page.WaitForAssertion(() => HasBlankButton(page, "question-statement").Should().BeTrue());
        page.Find("[aria-controls='question-statement'] button[data-format='blank']").GetAttribute("aria-label").Should().Be("Insert blank");
        HasBlankButton(page, "question-explanation").Should().BeFalse();

        page.Find("#question-type-SingleChoice").Click();

        page.WaitForAssertion(() => HasBlankButton(page, "question-statement").Should().BeFalse());
    }

    // AC14, BR3: the key section replaces the options and draws one row per marker, in statement order.
    [Fact]
    public async Task Add_FillIn_TheKeySectionHasOneRowPerMarkerInStatementOrder()
    {
        var page = RenderAdd();
        await FillInAsync(page, "<p>Sem lacuna.</p>");

        page.WaitForAssertion(() => page.FindAll("#question-section-blanks-title").Should().ContainSingle());
        page.FindAll("#question-section-options-title").Should().BeEmpty();
        page.Find("#question-section-blanks").TextContent.Should().Contain("No blank yet. Use Insert blank in the statement.")
            .And.Contain("Insert a blank in the statement, then write the answers it accepts. Case and extra spaces are ignored; accents count.");

        await TypeAsync(page, "question-statement", "<p>[[3]] chegou antes de [[1]].</p>");

        page.WaitForAssertion(() => RowTitles(page).Should().Equal("Blank 3", "Blank 1"));
        page.Find("#question-section-blanks").TextContent.Should().Contain("0 of 5 answers");
    }

    // AC14, AC18: each control names its answer and its blank.
    [Fact]
    public async Task Add_FillIn_EachAnswerFieldNamesItsAnswerAndItsBlank()
    {
        var page = RenderAdd();
        await FillInAsync(page, "<p>[[2]] e [[5]]</p>");
        page.WaitForAssertion(() => RowTitles(page).Should().HaveCount(2));

        page.Find("#question-blank-2-add").Click();
        page.Find("#question-blank-2-add").Click();
        page.Find("#question-blank-5-add").Click();

        page.WaitForAssertion(() => AnswerField(page, 2, 1).Label.Should().Be("Accepted answer 2 of blank 2"));
        AnswerField(page, 2, 0).Label.Should().Be("Accepted answer 1 of blank 2");
        AnswerField(page, 5, 0).Label.Should().Be("Accepted answer 1 of blank 5");
        page.Find("#question-blank-2-add").TextContent.Should().Contain("Add answer");
        page.Find("#question-section-blanks").TextContent.Should().Contain("2 of 5 answers").And.Contain("1 of 5 answers");
    }

    [Fact]
    public async Task Add_FillIn_AFifthAnswerDisablesAddAnswer()
    {
        var page = RenderAdd();
        await FillInAsync(page, "<p>[[1]]</p>");
        page.WaitForAssertion(() => RowTitles(page).Should().ContainSingle());

        for (var index = 0; index < QuestionLimits.BlankAnswersMax; index++)
        {
            page.Find("#question-blank-1-add").Click();
        }

        page.WaitForAssertion(() => page.Find("#question-blank-1-add").HasAttribute("disabled").Should().BeTrue());
        page.Find("#question-section-blanks").TextContent.Should().Contain("5 of 5 answers");
    }

    // AC14, BR1: the form sends the type and the blanks of the markers the statement holds, nothing else.
    [Fact]
    public async Task Add_FillIn_SavesTheTypeAndTheBlanksOfTheMarkersInTheStatement()
    {
        var page = RenderAdd();
        await FillInAsync(page, "<p>[[1]] e [[2]]</p>");
        page.WaitForAssertion(() => RowTitles(page).Should().HaveCount(2));
        page.Find("#question-blank-1-add").Click();
        page.Find("#question-blank-2-add").Click();
        await AnswerAsync(page, 1, 0, "Brasília");
        await AnswerAsync(page, 2, 0, "1960");

        // Removing a marker removes its row; its answer is not sent.
        await TypeAsync(page, "question-statement", "<p>Capital: [[1]]</p>");
        page.WaitForAssertion(() => RowTitles(page).Should().Equal("Blank 1"));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post
            && call.Path == "/api/v1/question-bank/questions"));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.Type.Should().Be(QuestionType.FillInTheBlanks);
        sent.Options.Should().BeNullOrEmpty();
        sent.TrueFalseKey.Should().BeNull();
        sent.MatchItems.Should().BeNull();
        sent.Blanks!.Should().ContainSingle();
        sent.Blanks![0].Number.Should().Be(1);
        sent.Blanks![0].AcceptedAnswers.Should().Equal("Brasília");
    }

    [Fact]
    public async Task Add_FillIn_RemovingAnAnswer_DropsIt_AndAMarkerTypedBackStartsEmpty()
    {
        var page = RenderAdd();
        await FillInAsync(page, "<p>[[1]] e [[2]]</p>");
        page.WaitForAssertion(() => RowTitles(page).Should().HaveCount(2));
        page.Find("#question-blank-1-add").Click();
        page.Find("#question-blank-1-add").Click();
        await AnswerAsync(page, 1, 0, "Brasília");
        await AnswerAsync(page, 1, 1, "Brasilia");
        page.Find("#question-blank-2-add").Click();
        await AnswerAsync(page, 2, 0, "1960");

        page.Find("#question-blank-1-answer-0-actions-delete").Click();
        await TypeAsync(page, "question-statement", "<p>[[1]]</p>");
        page.WaitForAssertion(() => RowTitles(page).Should().Equal("Blank 1"));
        await TypeAsync(page, "question-statement", "<p>[[1]] e [[2]]</p>");

        page.WaitForAssertion(() => RowTitles(page).Should().Equal("Blank 1", "Blank 2"));
        AnswerField(page, 1, 0).Value.Should().Be("Brasilia");
        page.FindComponents<AppTextField<string>>().Should().NotContain(field => field.Instance.Id == AnswerId(2, 0));
    }

    // AC14: picking another type and back keeps the typed answers.
    [Fact]
    public async Task Add_GoingToAnotherTypeAndBack_KeepsTheTypedAnswers()
    {
        var page = RenderAdd();
        await FillInAsync(page, "<p>[[1]]</p>");
        page.WaitForAssertion(() => RowTitles(page).Should().ContainSingle());
        page.Find("#question-blank-1-add").Click();
        await AnswerAsync(page, 1, 0, "Brasília");

        page.Find("#question-type-Matching").Click();
        page.WaitForAssertion(() => page.FindAll("#question-section-blanks-title").Should().BeEmpty());
        page.Find("#question-type-FillInTheBlanks").Click();

        page.WaitForAssertion(() => AnswerField(page, 1, 0).Value.Should().Be("Brasília"));
    }

    // AC14: on an existing question the type is read only and the stored blanks fill the rows.
    [Fact]
    public void Edit_FillIn_ShowsTheTypeAsAReadOnlyValue_AndTheStoredAnswers()
    {
        Api.Questions.Add(FillInDraft);

        var page = RenderEdit(FillInDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        page.Find("#question-type-value").TextContent.Should().Contain("Fill in the blanks");
        page.WaitForAssertion(() => RowTitles(page).Should().Equal("Blank 1", "Blank 2"));
        AnswerField(page, 1, 0).Value.Should().Be("Brasília");
        AnswerField(page, 1, 1).Value.Should().Be("Brasilia");
        AnswerField(page, 2, 0).Value.Should().Be("1960");
        page.Find(".app-form-aside").TextContent.Should().Contain("Fill in the blanks");
    }

    // The publish checklist for this type: its own two items.
    [Fact]
    public async Task Edit_FillIn_ChecklistIsDoneOnlyWhenEveryBlankHasAnAnswer()
    {
        Api.Questions.Add(FillInDraft);
        var page = RenderEdit(FillInDraft.Id);
        page.WaitForAssertion(() => RowTitles(page).Should().HaveCount(2));

        ChecklistTexts(page).Should().Contain(text => text.Contains("1 to 10 blanks in the statement"))
            .And.Contain(text => text.Contains("Every blank has an accepted answer"));
        page.Find("#question-publish").HasAttribute("disabled").Should().BeFalse();

        await TypeAsync(page, "question-statement", "<p>[[1]], [[2]] e [[3]].</p>");

        page.WaitForAssertion(() => page.FindAll(".app-form-aside-checklist li")
            .Single(item => item.TextContent.Contains("Every blank has an accepted answer"))
            .ClassList.Should().Contain("app-checklist-pending"));
    }

    // AC14, AC18: the section refusals appear under the section, announced.
    [Theory]
    [InlineData(QuestionBankErrorCodes.QuestionBlanksCount)]
    [InlineData(QuestionBankErrorCodes.QuestionBlankRepeated)]
    [InlineData(QuestionBankErrorCodes.QuestionBlankMarkerMissing)]
    public async Task Edit_FillIn_WhenTheApiRefusesTheBlanks_TheMessageIsUnderTheSection(string code)
    {
        Api.Questions.Add(FillInDraft);
        Api.SaveFailure = (System.Net.HttpStatusCode.BadRequest, code, null);
        var page = RenderEdit(FillInDraft.Id);
        page.WaitForAssertion(() => RowTitles(page).Should().HaveCount(2));
        await TypeAsync(page, "question-statement", "<p>Outro [[1]] e [[2]].</p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            var error = page.Find("#question-blanks-description .app-field-error-text");
            error.TextContent.Should().NotBeNullOrWhiteSpace();
            error.GetAttribute("aria-live").Should().Be("polite");
        });
    }

    // AC14: a row refusal names the blank by its number and marks that row.
    [Theory]
    [InlineData(QuestionBankErrorCodes.QuestionBlankAnswerTooLong)]
    [InlineData(QuestionBankErrorCodes.QuestionBlankAnswerRepeated)]
    [InlineData(QuestionBankErrorCodes.QuestionBlankAnswersCount)]
    [InlineData(QuestionBankErrorCodes.QuestionBlankAnswersRequired)]
    public async Task Edit_FillIn_WhenTheApiRefusesARow_TheBlankOfThatNumberIsMarked(string code)
    {
        Api.Questions.Add(FillInDraft);
        Api.SaveFailure = (System.Net.HttpStatusCode.BadRequest, code, 2);
        var page = RenderEdit(FillInDraft.Id);
        page.WaitForAssertion(() => RowTitles(page).Should().HaveCount(2));
        await TypeAsync(page, "question-statement", "<p>Outro [[1]] e [[2]].</p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find(".app-blank-row[data-blank='2'] .app-blank-error").TextContent.Should().NotBeNullOrWhiteSpace());
        page.FindAll(".app-blank-row[data-blank='1'] .app-blank-error").Should().BeEmpty();
    }

    // AC14, UC7: a new accepted answer on a published question asks for the reason.
    [Fact]
    public async Task Edit_PublishedFillIn_AddingAnAnswer_AsksForTheReason()
    {
        Api.Questions.Add(FillInDraft with { Status = QuestionStatus.Published });
        var page = RenderEdit(FillInDraft.Id);
        page.WaitForAssertion(() => RowTitles(page).Should().HaveCount(2));
        page.FindAll("#question-change-reason").Should().BeEmpty();

        page.Find("#question-blank-2-add").Click();
        await AnswerAsync(page, 2, 1, "MCMLX");

        page.WaitForElement("#question-change-reason");
    }

    [Fact]
    public async Task Edit_PublishedFillIn_ChangingOnlyTheCaseOfAnAnswer_DoesNotAskForTheReason()
    {
        Api.Questions.Add(FillInDraft with { Status = QuestionStatus.Published });
        var page = RenderEdit(FillInDraft.Id);
        page.WaitForAssertion(() => RowTitles(page).Should().HaveCount(2));

        await AnswerAsync(page, 1, 0, "BRASÍLIA");

        page.WaitForAssertion(() => page.Find(".app-form-save").HasAttribute("disabled").Should().BeFalse());
        page.FindAll("#question-change-reason").Should().BeEmpty();
    }

    // F-101 BR5, F-105 BR3: a frozen question shows everything disabled.
    [Fact]
    public void Edit_AnnulledFillIn_ShowsTheAnswersDisabled_AndNoAddButtons()
    {
        Api.Questions.Add(FillInDraft with { Status = QuestionStatus.Annulled });

        var page = RenderEdit(FillInDraft.Id);

        page.WaitForAssertion(() => RowTitles(page).Should().HaveCount(2));
        AnswerField(page, 1, 0).Disabled.Should().BeTrue();
        page.FindAll("#question-blank-1-add").Should().BeEmpty();
        page.FindAll("[aria-controls='question-statement'] button[data-format='blank'][disabled]").Should().ContainSingle();
    }
}
