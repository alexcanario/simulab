using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-97 `/admin/questions/new` and `/admin/questions/{id}`: the type field, the answer key section that replaces
/// the options for a true/false question, what the form sends and the way back to single choice
/// (AC1, AC10, AC14 as far as bUnit reaches).
/// </summary>
public sealed class TrueFalseQuestionFormTests : QuestionPageTestContext
{
    private static readonly QuestionResponse TrueFalseDraft = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000030"),
        QuestionType.TrueFalse,
        QuestionStatus.Draft,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        "<p>A crase é obrigatória antes de verbos.</p>",
        null,
        "<p>Nunca antes de verbo.</p>",
        2,
        [],
        TrueFalseKey: false);

    private static readonly QuestionResponse TrueFalseNoKey = TrueFalseDraft with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000031"),
        TrueFalseKey = null
    };

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

    private async Task FillTrueFalseAsync(IRenderedComponent<QuestionForm> page)
    {
        page.Find("#question-type-TrueFalse").Click();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", "<p>A capital da França é Paris.</p>");
    }

    private static IReadOnlyList<string> Headings(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll("section.app-section-card h2").Select(heading => heading.TextContent.Trim())];

    private static IReadOnlyList<string> ChecklistTexts(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll(".app-form-aside-checklist li").Select(item => item.TextContent)];

    // AC10: the new question starts as single choice, with the type as its first field.
    [Fact]
    public void Add_OffersTheTwoTypesWithSingleChoicePreselected_AndShowsTheOptionsSection()
    {
        var page = RenderAdd();

        var group = page.Find("#question-type");
        group.GetAttribute("role").Should().Be("radiogroup");
        page.Find("#question-type-label").TextContent.Should().Contain("Question type");
        page.Find("#question-type-SingleChoice").GetAttribute("aria-checked").Should().Be("true");
        page.Find("#question-type-SingleChoice").TextContent.Should().Contain("Single choice");
        page.Find("#question-type-TrueFalse").GetAttribute("aria-checked").Should().Be("false");
        page.Find("#question-type-TrueFalse").TextContent.Should().Contain("True/false");
        Headings(page).Should().Contain("Options").And.NotContain("Answer key");
        page.FindAll("#question-section-true-false-key").Should().BeEmpty();
    }

    // AC10, BR9: choosing true/false replaces the options section with the answer key section.
    [Fact]
    public void Add_ChoosingTrueFalse_ReplacesTheOptionsWithTheAnswerKeySection_WithNoKeyPreselected()
    {
        var page = RenderAdd();

        page.Find("#question-type-TrueFalse").Click();

        page.WaitForAssertion(() => Headings(page).Should().Equal(
            "Classification", "Origin", "Statement", "Base text", "Answer key", "Explanation", "Publication"));
        page.FindAll("#question-section-options").Should().BeEmpty();
        page.Find("#question-true-false-key").GetAttribute("role").Should().Be("radiogroup");
        page.Find("#question-true-false-key-True").TextContent.Should().Contain("True");
        page.Find("#question-true-false-key-False").TextContent.Should().Contain("False");
        page.FindAll("#question-true-false-key [aria-checked='true']").Should().BeEmpty("a draft may have no key");
        page.Find("#question-section-true-false-key").TextContent.Should()
            .Contain("scoring rules of the edition");
    }

    // AC14: the key control is a named radio group reachable by keyboard.
    [Fact]
    public void Add_TrueFalseKeyControl_IsANamedRadioGroupWithOneTabStop()
    {
        var page = RenderAdd();
        page.Find("#question-type-TrueFalse").Click();

        var group = page.WaitForElement("#question-true-false-key");

        group.GetAttribute("aria-labelledby").Should().Be("question-true-false-key-label");
        page.Find("#question-true-false-key-label").TextContent.Should().Contain("Correct answer");
        page.FindAll("#question-true-false-key [role='radio'][tabindex='0']").Should().ContainSingle();
    }

    // AC10, BR2: the answers typed for the other type stay in the form until it is saved.
    [Fact]
    public async Task Add_GoingToTrueFalseAndBack_BringsTheTypedOptionsBack()
    {
        var page = RenderAdd();
        page.Find("#question-option-add").Click();
        WaitForEditors(page, "question-option-0");
        await TypeAsync(page, "question-option-0", "<p>Opção A</p>");

        page.Find("#question-type-TrueFalse").Click();
        page.WaitForAssertion(() => page.FindAll("#question-section-options").Should().BeEmpty());
        page.Find("#question-type-SingleChoice").Click();

        page.WaitForAssertion(() => page.FindAll("#question-section-options").Should().ContainSingle());
        WaitForEditors(page, "question-option-0");
        RichText.Editors["question-option-0"].Html.Should().Be("<p>Opção A</p>");
    }

    // AC1, AC10: saving a true/false question sends its type and key, never options.
    [Theory]
    [InlineData("True", true)]
    [InlineData("False", false)]
    public async Task Add_TrueFalseWithAKey_SavesAsADraftOfThatTypeWithItsKeyAndNoOptions(string card, bool key)
    {
        var page = RenderAdd();
        await FillTrueFalseAsync(page);
        page.Find($"#question-true-false-key-{card}").Click();

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post
            && call.Path == "/api/v1/question-bank/questions"));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.Type.Should().Be(QuestionType.TrueFalse);
        sent.TrueFalseKey.Should().Be(key);
        (sent.Options ?? []).Should().BeEmpty();
        page.WaitForAssertion(() => Api.Questions.Should().ContainSingle(question =>
            question.Type == QuestionType.TrueFalse && question.TrueFalseKey == key));
    }

    // AC1, BR4: a draft with no key is saved with a null key.
    [Fact]
    public async Task Add_TrueFalseWithNoKey_SavesADraftWithANullKey()
    {
        var page = RenderAdd();
        await FillTrueFalseAsync(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post
            && call.Path == "/api/v1/question-bank/questions"));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.Type.Should().Be(QuestionType.TrueFalse);
        sent.TrueFalseKey.Should().BeNull();
    }

    // BR2: only the chosen type's answers are sent.
    [Fact]
    public async Task Add_TrueFalseAfterTypingOptions_DoesNotSendTheOptions()
    {
        var page = RenderAdd();
        page.Find("#question-option-add").Click();
        WaitForEditors(page, "question-option-0");
        await TypeAsync(page, "question-option-0", "<p>Opção A</p>");
        await FillTrueFalseAsync(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post
            && call.Path == "/api/v1/question-bank/questions"));
        (Api.LastSaved(HttpMethod.Post).Options ?? []).Should().BeEmpty();
    }

    [Fact]
    public async Task Add_SingleChoice_SendsItsTypeAndNoKey()
    {
        var page = RenderAdd();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", "<p>Qual frase está certa?</p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post
            && call.Path == "/api/v1/question-bank/questions"));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.Type.Should().Be(QuestionType.SingleChoice);
        sent.TrueFalseKey.Should().BeNull();
    }

    // AC10: on an existing question the type is a labelled value, never a control.
    [Fact]
    public void Edit_TrueFalse_ShowsTheTypeAsAReadOnlyValueWithItsHint_AndTheStoredKey()
    {
        Api.Questions.Add(TrueFalseDraft);

        var page = RenderEdit(TrueFalseDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        page.Find("#question-type-value").TextContent.Should().Contain("True/false");
        page.FindAll("#question-type[role='radiogroup']").Should().BeEmpty();
        page.Markup.Should().Contain("The type cannot be changed after the question is created.");
        page.Find("#question-true-false-key-False").GetAttribute("aria-checked").Should().Be("true");
        page.Find("#question-true-false-key-True").GetAttribute("aria-checked").Should().Be("false");
        page.FindAll("#question-section-options").Should().BeEmpty();
    }

    [Fact]
    public void Edit_SingleChoice_ShowsItsTypeAsAReadOnlyValue_AndKeepsTheOptions()
    {
        Api.Questions.Add(Draft);

        var page = RenderEdit(Draft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        page.Find("#question-type-value").TextContent.Should().Contain("Single choice");
        page.FindAll("#question-section-options").Should().ContainSingle();
        page.FindAll("#question-section-true-false-key").Should().BeEmpty();
    }

    // The publish checklist: the three option items are one "Answer key set" for this type.
    [Fact]
    public async Task Edit_TrueFalse_ChecklistHasOneAnswerKeyItem_DoneOnlyWithAKey()
    {
        Api.Questions.Add(TrueFalseNoKey);
        var page = RenderEdit(TrueFalseNoKey.Id);
        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());

        ChecklistTexts(page).Should().HaveCount(4);
        ChecklistTexts(page)[3].Should().Contain("Answer key set");
        page.FindAll(".app-form-aside-checklist li")[3].ClassList.Should().Contain("app-checklist-pending")
            .And.NotContain("app-checklist-done");

        await page.InvokeAsync(() => page.Find("#question-true-false-key-True").Click());

        page.WaitForAssertion(() => page.FindAll(".app-form-aside-checklist li")[3].ClassList.Should().Contain("app-checklist-done"));
    }

    // The aside names the type in place of the option count.
    [Fact]
    public void Edit_TrueFalse_AsideShowsTheTypeInsteadOfTheOptionCount()
    {
        Api.Questions.Add(TrueFalseDraft);

        var page = RenderEdit(TrueFalseDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        var aside = page.Find(".app-form-aside").TextContent;
        aside.Should().Contain("Type").And.Contain("True/false");
        aside.Should().NotContain("options");
    }

    // AC3, AC4, AC14: the Api's refusal is shown on the key card and in the error summary.
    [Fact]
    public async Task Edit_PublishedTrueFalse_WhenTheApiRefusesTheMissingKey_TheErrorIsOnTheKeyCardAndInTheSummary()
    {
        Api.Questions.Add(TrueFalseDraft with { Status = QuestionStatus.Published });
        Api.SaveFailure = (System.Net.HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionTrueFalseKeyRequired, null);
        var page = RenderEdit(TrueFalseDraft.Id);
        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        await TypeAsync(page, "question-statement", "<p>Outro enunciado.</p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#question-true-false-key-description .app-field-error-text")
            .TextContent.Should().Contain("Set the answer key"));
        page.Find("#question-error-summary").TextContent.Should().Contain("Set the answer key");
        page.Find("#question-true-false-key").GetAttribute("aria-invalid").Should().Be("true");
    }

    [Fact]
    public async Task Edit_PublishedTrueFalse_ChangingTheKey_SendsAPutWithTheNewKeyAndStaysPublished()
    {
        Api.Questions.Add(TrueFalseDraft with { Status = QuestionStatus.Published, TrueFalseKey = true });
        var page = RenderEdit(TrueFalseDraft.Id);
        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());

        await page.InvokeAsync(() => page.Find("#question-true-false-key-False").Click());
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        var sent = Api.LastSaved(HttpMethod.Put);
        sent.TrueFalseKey.Should().BeFalse();
        sent.Type.Should().Be(QuestionType.TrueFalse);
        page.WaitForAssertion(() => Api.Questions.Single().Status.Should().Be(QuestionStatus.Published));
    }

    // The page does not need the options to publish a true/false question: the preview button works on its key.
    [Fact]
    public void Edit_TrueFalse_OffersPreviewAndPublish()
    {
        Api.Questions.Add(TrueFalseDraft);

        var page = RenderEdit(TrueFalseDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-preview").Should().ContainSingle());
        page.Find("#question-publish").HasAttribute("disabled").Should().BeFalse(
            "a true/false draft with its statement, topic, language and key is complete");
    }
}
