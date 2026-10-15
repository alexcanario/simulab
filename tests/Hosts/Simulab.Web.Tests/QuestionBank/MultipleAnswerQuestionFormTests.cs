using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-104 `/admin/questions/new` and `/admin/questions/{id}`: the third type card, checkboxes in place of the
/// radios, what the form sends, the publish checklist and the two new refusals (AC12, AC16 as far as bUnit
/// reaches).
/// </summary>
public sealed class MultipleAnswerQuestionFormTests : QuestionPageTestContext
{
    private static readonly QuestionResponse MultipleDraft = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000050"),
        QuestionType.MultipleAnswer,
        QuestionStatus.Draft,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        "<p>Quais frases usam a crase corretamente?</p>",
        null,
        null,
        3,
        [
            new(0, "<p>Fui à escola.</p>", true),
            new(1, "<p>Fui a escola.</p>", false),
            new(2, "<p>Cheguei às cinco horas.</p>", true)
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

    private static IReadOnlyList<string> Headings(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll("section.app-section-card h2").Select(heading => heading.TextContent.Trim())];

    private static IReadOnlyList<string> ChecklistTexts(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll(".app-form-aside-checklist li").Select(item => item.TextContent)];

    private static IReadOnlyList<AngleSharp.Dom.IElement> Boxes(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll("#question-section-options input[type='checkbox']")];

    private async Task FillMultipleAsync(IRenderedComponent<QuestionForm> page, int options, params int[] correct)
    {
        page.Find("#question-type-MultipleAnswer").Click();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", "<p>Quais são capitais europeias?</p>");
        for (var index = 0; index < options; index++)
        {
            page.Find("#question-option-add").Click();
            WaitForEditors(page, $"question-option-{index}");
            await TypeAsync(page, $"question-option-{index}", $"<p>Opção {index}</p>");
        }

        foreach (var position in correct)
        {
            await page.InvokeAsync(() => Boxes(page)[position].Change(true));
        }
    }

    // AC12: the type field offers three cards, single choice still preselected.
    [Fact]
    public void Add_OffersTheThreeTypesWithSingleChoicePreselected()
    {
        var page = RenderAdd();

        page.Find("#question-type-SingleChoice").GetAttribute("aria-checked").Should().Be("true");
        page.Find("#question-type-TrueFalse").GetAttribute("aria-checked").Should().Be("false");
        page.Find("#question-type-MultipleAnswer").GetAttribute("aria-checked").Should().Be("false");
        page.Find("#question-type-MultipleAnswer").TextContent.Should().Contain("Multiple answer");
    }

    // AC12, BR9: the options section stays, with checkboxes in place of the radio group.
    [Fact]
    public async Task Add_ChoosingMultipleAnswer_DrawsCheckboxesInPlaceOfTheRadios_AndSeveralCanBeChecked()
    {
        var page = RenderAdd();
        await FillMultipleAsync(page, 3, 0, 2);

        Headings(page).Should().Contain("Options").And.NotContain("Answer key");
        page.FindAll("#question-section-options [role='radiogroup'], #question-section-options input[type='radio']")
            .Should().BeEmpty();
        Boxes(page).Should().HaveCount(3);
        Boxes(page).Select(box => box.HasAttribute("checked")).Should().Equal(true, false, true);
        page.Find("#question-section-options").TextContent.Should()
            .Contain("Mark every correct one: at least two, and at least one wrong.");
    }

    // AC16: every box names its letter.
    [Fact]
    public async Task Add_MultipleAnswer_EachCheckboxNamesItsLetter()
    {
        var page = RenderAdd();
        await FillMultipleAsync(page, 3);

        page.Markup.Should().Contain("Mark A as correct").And.Contain("Mark B as correct").And.Contain("Mark C as correct");
    }

    // AC12: the form sends the type and every marked option, never a true/false key.
    [Fact]
    public async Task Add_MultipleAnswer_SavesADraftWithItsTypeAndEveryMarkedOption()
    {
        var page = RenderAdd();
        await FillMultipleAsync(page, 3, 0, 2);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post
            && call.Path == "/api/v1/question-bank/questions"));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.Type.Should().Be(QuestionType.MultipleAnswer);
        sent.TrueFalseKey.Should().BeNull();
        sent.Options!.Select(option => option.IsCorrect).Should().Equal(true, false, true);
    }

    // AC12, BR2: the options typed for single choice come back, with the marks the form allows for the type.
    [Fact]
    public async Task Add_GoingToMultipleAnswerAndBack_BringsTheTypedOptionsBack()
    {
        var page = RenderAdd();
        page.Find("#question-option-add").Click();
        WaitForEditors(page, "question-option-0");
        await TypeAsync(page, "question-option-0", "<p>Opção A</p>");

        page.Find("#question-type-MultipleAnswer").Click();
        page.Find("#question-type-SingleChoice").Click();

        page.WaitForAssertion(() => page.FindAll("#question-section-options-title").Should().ContainSingle());
        WaitForEditors(page, "question-option-0");
        RichText.Editors["question-option-0"].Html.Should().Be("<p>Opção A</p>");
    }

    // AC12: on an existing question the type is a labelled value, and the stored marks are the checked boxes.
    [Fact]
    public void Edit_MultipleAnswer_ShowsTheTypeAsAReadOnlyValue_AndTheStoredMarks()
    {
        Api.Questions.Add(MultipleDraft);

        var page = RenderEdit(MultipleDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        page.Find("#question-type-value").TextContent.Should().Contain("Multiple answer");
        page.FindAll("#question-type[role='radiogroup']").Should().BeEmpty();
        page.Markup.Should().Contain("The type cannot be changed after the question is created.");
        Boxes(page).Select(box => box.HasAttribute("checked")).Should().Equal(true, false, true);
        page.Find(".app-form-aside").TextContent.Should().Contain("Multiple answer");
    }

    // The publish checklist for this type: its own three items, done only for a valid marking.
    [Fact]
    public async Task Edit_MultipleAnswer_ChecklistIsDoneOnlyForTwoOrMoreCorrectAndOneWrong()
    {
        Api.Questions.Add(MultipleDraft);
        var page = RenderEdit(MultipleDraft.Id);
        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());

        ChecklistTexts(page).Should().Contain(text => text.Contains("3 to 5 options"))
            .And.Contain(text => text.Contains("At least two correct and one wrong"));
        page.FindAll(".app-form-aside-checklist li")
            .Single(item => item.TextContent.Contains("At least two correct and one wrong"))
            .ClassList.Should().Contain("app-checklist-done");

        await page.InvokeAsync(() => Boxes(page)[2].Change(false));

        page.WaitForAssertion(() => page.FindAll(".app-form-aside-checklist li")
            .Single(item => item.TextContent.Contains("At least two correct and one wrong"))
            .ClassList.Should().Contain("app-checklist-pending"));

        await page.InvokeAsync(() => { Boxes(page)[1].Change(true); Boxes(page)[2].Change(true); });

        page.WaitForAssertion(() => page.FindAll(".app-form-aside-checklist li")
            .Single(item => item.TextContent.Contains("At least two correct and one wrong"))
            .ClassList.Should().Contain("app-checklist-pending", "marking every option leaves none wrong"));
    }

    // AC3, AC4: the two refusals appear under the list and in the error summary.
    [Theory]
    [InlineData(QuestionBankErrorCodes.QuestionMultipleOptionsCount, "A multiple-answer question is published with 3 to 5 options.")]
    [InlineData(QuestionBankErrorCodes.QuestionMultipleKeyInvalid, "Mark at least two options as correct, and leave at least one wrong.")]
    public async Task Edit_PublishedMultipleAnswer_WhenTheApiRefuses_TheMessageIsUnderTheOptionsList(string code, string text)
    {
        Api.Questions.Add(MultipleDraft with { Status = QuestionStatus.Published });
        Api.SaveFailure = (System.Net.HttpStatusCode.BadRequest, code, null);
        var page = RenderEdit(MultipleDraft.Id);
        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        await TypeAsync(page, "question-statement", "<p>Outro enunciado.</p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#question-options-description .app-field-error-text")
            .TextContent.Should().Contain(text));
    }

    // AC8, BR7: moving a second correct option asks for the reason, as moving the first one does.
    [Fact]
    public async Task Edit_PublishedMultipleAnswer_MovingASecondCorrectOption_AsksForTheReason()
    {
        Api.Questions.Add(MultipleDraft with { Status = QuestionStatus.Published });
        var page = RenderEdit(MultipleDraft.Id);
        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        page.FindAll("#question-change-reason").Should().BeEmpty();

        await page.InvokeAsync(() => { Boxes(page)[2].Change(false); Boxes(page)[1].Change(true); });

        page.WaitForElement("#question-change-reason");
    }

    // F-101 BR5: an annulled question is frozen, so its boxes cannot be changed.
    [Fact]
    public void Edit_AnnulledMultipleAnswer_ShowsTheBoxesDisabled()
    {
        Api.Questions.Add(MultipleDraft with { Status = QuestionStatus.Annulled });

        var page = RenderEdit(MultipleDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        Boxes(page).Should().HaveCount(3).And.OnlyContain(box => box.HasAttribute("disabled"));
    }

    [Fact]
    public void Edit_MultipleAnswer_OffersPreviewAndPublish()
    {
        Api.Questions.Add(MultipleDraft);

        var page = RenderEdit(MultipleDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-preview").Should().ContainSingle());
        page.Find("#question-publish").HasAttribute("disabled").Should().BeFalse(
            "three options with two correct and one wrong is complete");
    }
}
