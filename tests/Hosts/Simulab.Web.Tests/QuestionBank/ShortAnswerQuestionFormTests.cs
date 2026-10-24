using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-108 `/admin/questions/new` and `/admin/questions/{id}`: the sixth type card, no "Insert blank" button for this type,
/// the key section of accepted answers with its one empty field, what the form sends (one blank numbered 1), the
/// checklist, the refusals with the short-answer texts, the key-change reason and the frozen state (AC14, AC18 as far as
/// bUnit reaches, AC19).
/// </summary>
public sealed class ShortAnswerQuestionFormTests : QuestionPageTestContext
{
    private const string CapitalStatement = "<p>Qual é a capital do Brasil?</p>";

    private static readonly QuestionResponse ShortDraft = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000190"),
        QuestionType.ShortAnswer,
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
        Blanks: [new(1, ["Brasília", "Brasilia"])]);

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

    private static string AnswerId(int answer) => $"question-short-answer-{answer}";

    private static AppTextField<string> AnswerField(IRenderedComponent<QuestionForm> page, int answer) =>
        page.FindComponents<AppTextField<string>>().Single(component => component.Instance.Id == AnswerId(answer)).Instance;

    private static Task AnswerAsync(IRenderedComponent<QuestionForm> page, int answer, string text) =>
        page.InvokeAsync(() => AnswerField(page, answer).ValueChanged.InvokeAsync(text));

    private static int AnswerFields(IRenderedComponent<QuestionForm> page) =>
        page.FindComponents<AppTextField<string>>().Count(component => component.Instance.Id?.StartsWith("question-short-answer-", StringComparison.Ordinal) == true
            && !component.Instance.Id.Contains("-actions", StringComparison.Ordinal));

    private static IReadOnlyList<string> ChecklistTexts(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll(".app-form-aside-checklist li").Select(item => item.TextContent)];

    private static bool HasBlankButton(IRenderedComponent<QuestionForm> page, string editorId) =>
        page.FindAll($"[role='toolbar'][aria-controls='{editorId}'] button[data-format='blank']").Count == 1;

    private async Task ShortAsync(IRenderedComponent<QuestionForm> page, string statement)
    {
        page.Find("#question-type-ShortAnswer").Click();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", statement);
    }

    // AC14, AC19: six cards, single choice still preselected.
    [Fact]
    public void Add_OffersTheSixthTypeCard_WithSingleChoiceStillPreselected()
    {
        var page = RenderAdd();

        page.Find("#question-type-SingleChoice").GetAttribute("aria-checked").Should().Be("true");
        page.Find("#question-type-ShortAnswer").GetAttribute("aria-checked").Should().Be("false");
        page.Find("#question-type-ShortAnswer").TextContent.Should().Contain("Short answer")
            .And.Contain("A short typed answer, checked against accepted answers.");
        page.Find("#question-type").QuerySelectorAll("[role='radio']").Should().HaveCount(6);
    }

    // AC14, BR11: the key section replaces the options, starts with one empty field and the statement has no "Insert blank".
    [Fact]
    public async Task Add_ChoosingShortAnswer_ShowsAcceptedAnswersWithOneEmptyField_AndNoInsertBlank()
    {
        var page = RenderAdd();
        await ShortAsync(page, CapitalStatement);

        page.WaitForAssertion(() => page.FindAll("#question-section-short-answer-title").Should().ContainSingle());
        page.FindAll("#question-section-options-title").Should().BeEmpty();
        page.FindAll("#question-section-blanks-title").Should().BeEmpty();
        page.Find("#question-section-short-answer").TextContent.Should().Contain("Accepted answers")
            .And.Contain("Write the answers the student may type. Case and extra spaces are ignored; accents count.")
            .And.Contain("1 of 5 answers");
        AnswerFields(page).Should().Be(1);
        AnswerField(page, 0).Value.Should().BeNullOrEmpty();
        HasBlankButton(page, "question-statement").Should().BeFalse();
    }

    // AC14: "Add answer" adds up to 5 fields and a removal drops one; each control names its answer (AC18).
    [Fact]
    public async Task Add_ShortAnswer_AddAnswerGoesUpToFive_AndARemovalDropsOne()
    {
        var page = RenderAdd();
        await ShortAsync(page, CapitalStatement);
        page.WaitForAssertion(() => AnswerFields(page).Should().Be(1));

        for (var index = 0; index < QuestionLimits.BlankAnswersMax - 1; index++)
        {
            page.Find("#question-short-answer-add").Click();
        }

        page.WaitForAssertion(() => AnswerFields(page).Should().Be(QuestionLimits.BlankAnswersMax));
        page.Find("#question-short-answer-add").HasAttribute("disabled").Should().BeTrue();
        page.Find("#question-section-short-answer").TextContent.Should().Contain("5 of 5 answers");
        AnswerField(page, 4).Label.Should().Be("Accepted answer 5");
        page.Find("#question-short-answer-add").TextContent.Should().Contain("Add answer");

        page.Find("#question-short-answer-4-actions-delete").Click();

        page.WaitForAssertion(() => AnswerFields(page).Should().Be(4));
        page.Find("#question-short-answer-add").HasAttribute("disabled").Should().BeFalse();
    }

    // AC14, BR3: the form sends the type and one blank numbered 1 with the trimmed non-empty answers, nothing else.
    [Fact]
    public async Task Add_ShortAnswer_SavesTheTypeAndOneBlankNumbered1()
    {
        var page = RenderAdd();
        await ShortAsync(page, CapitalStatement);
        page.WaitForAssertion(() => AnswerFields(page).Should().Be(1));
        page.Find("#question-short-answer-add").Click();
        await AnswerAsync(page, 0, "Brasília");
        await AnswerAsync(page, 1, "Brasilia");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post
            && call.Path == "/api/v1/question-bank/questions"));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.Type.Should().Be(QuestionType.ShortAnswer);
        sent.Options.Should().BeNullOrEmpty();
        sent.TrueFalseKey.Should().BeNull();
        sent.MatchItems.Should().BeNull();
        sent.Blanks!.Should().ContainSingle();
        sent.Blanks![0].Number.Should().Be(1);
        sent.Blanks![0].AcceptedAnswers.Should().Equal("Brasília", "Brasilia");
    }

    // AC14: picking another type and back keeps the typed answers; the fill-in answers are a different list.
    [Fact]
    public async Task Add_GoingToAnotherTypeAndBack_KeepsTheTypedAnswers()
    {
        var page = RenderAdd();
        await ShortAsync(page, "<p>Capital? [[1]]</p>");
        page.WaitForAssertion(() => AnswerFields(page).Should().Be(1));
        await AnswerAsync(page, 0, "Brasília");

        page.Find("#question-type-FillInTheBlanks").Click();
        page.WaitForAssertion(() => page.FindAll("#question-section-short-answer-title").Should().BeEmpty());
        page.WaitForAssertion(() => page.FindAll("#question-section-blanks-title").Should().ContainSingle());
        page.FindAll(".app-blank-row[data-blank='1'] input").Select(input => input.GetAttribute("value"))
            .Should().NotContain("Brasília", "a fill-in blank 1 does not borrow the short answer");
        page.Find("#question-type-ShortAnswer").Click();

        page.WaitForAssertion(() => AnswerField(page, 0).Value.Should().Be("Brasília"));
    }

    // AC14: on an existing question the type is read only and the stored answers fill the fields.
    [Fact]
    public void Edit_ShortAnswer_ShowsTheTypeAsAReadOnlyValue_AndTheStoredAnswers()
    {
        Api.Questions.Add(ShortDraft);

        var page = RenderEdit(ShortDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        page.Find("#question-type-value").TextContent.Should().Contain("Short answer");
        page.WaitForAssertion(() => AnswerFields(page).Should().Be(2));
        AnswerField(page, 0).Value.Should().Be("Brasília");
        AnswerField(page, 1).Value.Should().Be("Brasilia");
        page.Find(".app-form-aside").TextContent.Should().Contain("Short answer");
    }

    // The publish checklist for this type: one item.
    [Fact]
    public async Task Edit_ShortAnswer_ChecklistIsDoneOnlyWithAnAcceptedAnswer()
    {
        Api.Questions.Add(ShortDraft);
        var page = RenderEdit(ShortDraft.Id);
        page.WaitForAssertion(() => AnswerFields(page).Should().Be(2));

        ChecklistTexts(page).Should().ContainSingle(text => text.Contains("At least one accepted answer"));
        page.Find("#question-publish").HasAttribute("disabled").Should().BeFalse();

        page.Find("#question-short-answer-1-actions-delete").Click();
        page.Find("#question-short-answer-0-actions-delete").Click();

        page.WaitForAssertion(() => page.FindAll(".app-form-aside-checklist li")
            .Single(item => item.TextContent.Contains("At least one accepted answer"))
            .ClassList.Should().Contain("app-checklist-pending"));
    }

    // AC14, AC18: a refusal about the answers shows the short-answer text, announced, under the field.
    [Theory]
    [InlineData(QuestionBankErrorCodes.QuestionBlankAnswersRequired, "Write at least one accepted answer.")]
    [InlineData(QuestionBankErrorCodes.QuestionBlankAnswersCount, "A short answer accepts at most 5 answers.")]
    [InlineData(QuestionBankErrorCodes.QuestionBlankAnswerRepeated, "Two answers are the same once case and extra spaces are ignored.")]
    [InlineData(QuestionBankErrorCodes.QuestionBlankAnswerTooLong, "An answer has more than 100 characters.")]
    public async Task Edit_ShortAnswer_WhenTheApiRefusesTheAnswers_TheShortAnswerTextIsUnderTheSection(string code, string text)
    {
        Api.Questions.Add(ShortDraft);
        Api.SaveFailure = (System.Net.HttpStatusCode.BadRequest, code, 1);
        var page = RenderEdit(ShortDraft.Id);
        page.WaitForAssertion(() => AnswerFields(page).Should().Be(2));
        await TypeAsync(page, "question-statement", "<p>Outro enunciado?</p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            var error = page.Find("#question-section-short-answer .app-short-answer-error");
            error.TextContent.Trim().Should().Be(text);
            error.GetAttribute("aria-live").Should().Be("polite");
        });
    }

    // AC14, UC7: a new accepted answer on a published question asks for the reason; only case or order does not.
    [Fact]
    public async Task Edit_PublishedShortAnswer_AddingAnAnswer_AsksForTheReason()
    {
        Api.Questions.Add(ShortDraft with { Status = QuestionStatus.Published });
        var page = RenderEdit(ShortDraft.Id);
        page.WaitForAssertion(() => AnswerFields(page).Should().Be(2));
        page.FindAll("#question-change-reason").Should().BeEmpty();

        page.Find("#question-short-answer-add").Click();
        await AnswerAsync(page, 2, "Capital Federal");

        page.WaitForElement("#question-change-reason");
    }

    [Fact]
    public async Task Edit_PublishedShortAnswer_ChangingOnlyTheCaseOfAnAnswer_DoesNotAskForTheReason()
    {
        Api.Questions.Add(ShortDraft with { Status = QuestionStatus.Published });
        var page = RenderEdit(ShortDraft.Id);
        page.WaitForAssertion(() => AnswerFields(page).Should().Be(2));

        await AnswerAsync(page, 0, "BRASÍLIA");

        page.WaitForAssertion(() => page.Find(".app-form-save").HasAttribute("disabled").Should().BeFalse());
        page.FindAll("#question-change-reason").Should().BeEmpty();
    }

    // F-101 BR5, F-105 BR3: a frozen question shows everything disabled.
    [Fact]
    public void Edit_AnnulledShortAnswer_ShowsTheAnswersDisabled_AndNoAddButton()
    {
        Api.Questions.Add(ShortDraft with { Status = QuestionStatus.Annulled });

        var page = RenderEdit(ShortDraft.Id);

        page.WaitForAssertion(() => AnswerFields(page).Should().Be(2));
        AnswerField(page, 0).Disabled.Should().BeTrue();
        page.FindAll("#question-short-answer-add").Should().BeEmpty();
    }
}
