using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-106 `/admin/questions/new` and `/admin/questions/{id}`: the fourth type card, the key section of two lists with
/// one select per left row, what the form sends, the checklist, the refusals, the key-change reason and the frozen
/// state (AC13, AC17 as far as bUnit reaches).
/// </summary>
public sealed class MatchingQuestionFormTests : QuestionPageTestContext
{
    private static readonly QuestionResponse MatchingDraft = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000070"),
        QuestionType.Matching,
        QuestionStatus.Draft,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        "<p>Associe cada país à sua capital.</p>",
        null,
        null,
        3,
        [
            new(0, "<p>Lisboa</p>", false),
            new(1, "<p>Paris</p>", false),
            new(2, "<p>Roma</p>", false)
        ],
        MatchItems:
        [
            new(0, "<p>França</p>", 1),
            new(1, "<p>Portugal</p>", 0)
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

    private static AppSelectField<int?> MatchSelect(IRenderedComponent<QuestionForm> page, int item) =>
        page.FindComponents<AppSelectField<int?>>()
            .Single(component => component.Instance.Id == $"question-match-item-match-{item}").Instance;

    private static Task MatchAsync(IRenderedComponent<QuestionForm> page, int item, int? letter) =>
        page.InvokeAsync(() => MatchSelect(page, item).ValueChanged.InvokeAsync(letter));

    private static IReadOnlyList<string> Letters(IRenderedComponent<QuestionForm> page, int item) =>
        [.. MatchSelect(page, item).Options.Where(option => option.Value is not null).Select(option => option.Text)];

    private static IReadOnlyList<string> Notes(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll(".app-match-note").Select(note => note.TextContent.Trim())];

    private static IReadOnlyList<string> ChecklistTexts(IRenderedComponent<QuestionForm> page) =>
        [.. page.FindAll(".app-form-aside-checklist li").Select(item => item.TextContent)];

    private async Task FillMatchingAsync(IRenderedComponent<QuestionForm> page, int lefts, int rights)
    {
        page.Find("#question-type-Matching").Click();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", "<p>Associe as colunas.</p>");
        for (var index = 0; index < rights; index++)
        {
            page.Find("#question-option-add").Click();
            WaitForEditors(page, $"question-option-{index}");
            await TypeAsync(page, $"question-option-{index}", $"<p>Direita {index}</p>");
        }

        for (var index = 0; index < lefts; index++)
        {
            page.Find("#question-match-item-add").Click();
            WaitForEditors(page, $"question-match-item-{index}");
            await TypeAsync(page, $"question-match-item-{index}", $"<p>Esquerda {index}</p>");
        }
    }

    // AC13, AC18: the type field offers four cards, single choice still preselected.
    [Fact]
    public void Add_OffersTheFourTypesWithSingleChoicePreselected()
    {
        var page = RenderAdd();

        page.Find("#question-type-SingleChoice").GetAttribute("aria-checked").Should().Be("true");
        page.Find("#question-type-Matching").GetAttribute("aria-checked").Should().Be("false");
        page.Find("#question-type-Matching").TextContent.Should().Contain("Matching")
            .And.Contain("Two columns: match each numbered item with a lettered one.");
        page.Find("#question-type").QuerySelectorAll("[role='radio']").Should().HaveCount(4);
    }

    // AC13: two lists, numbered and lettered, and a select of the free letters on each left row.
    [Fact]
    public async Task Add_ChoosingMatching_DrawsTheTwoLists_AndATakenLetterIsNotOffered()
    {
        var page = RenderAdd();
        await FillMatchingAsync(page, lefts: 2, rights: 3);

        page.FindAll("#question-section-options").Should().BeEmpty();
        page.FindAll("#question-section-matching").Should().ContainSingle();
        Letters(page, 0).Should().Equal("A", "B", "C");

        await MatchAsync(page, 0, 2);

        page.WaitForAssertion(() => Letters(page, 1).Should().Equal("A", "B"));
        Letters(page, 0).Should().Equal(["A", "B", "C"], "its own letter stays offered");
        Notes(page).Should().Equal("Extra", "Extra", "Used by item 1");
        page.Markup.Should().Contain("2 of 8 items").And.Contain("3 of 8 matches");
    }

    // AC17: every control names its number or letter.
    [Fact]
    public async Task Add_Matching_EachControlNamesItsNumberOrLetter()
    {
        var page = RenderAdd();
        await FillMatchingAsync(page, lefts: 2, rights: 2);

        page.Markup.Should().Contain("Text of item 1").And.Contain("Text of item 2")
            .And.Contain("Letter matched by item 1").And.Contain("Letter matched by item 2")
            .And.Contain("Text of match A").And.Contain("Text of match B");
    }

    // AC13: the form sends the type, the right items unmarked and each left item with its match.
    [Fact]
    public async Task Add_Matching_SavesADraftWithItsTypeTheItemsAndTheMatches()
    {
        var page = RenderAdd();
        await FillMatchingAsync(page, lefts: 2, rights: 3);
        await MatchAsync(page, 0, 2);
        await MatchAsync(page, 1, 0);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post
            && call.Path == "/api/v1/question-bank/questions"));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.Type.Should().Be(QuestionType.Matching);
        sent.TrueFalseKey.Should().BeNull();
        sent.Options!.Should().HaveCount(3).And.OnlyContain(option => !option.IsCorrect);
        sent.MatchItems!.Select(item => item.MatchPosition).Should().Equal(2, 0);
        sent.MatchItems!.Select(item => item.TextHtml).Should().Equal("<p>Esquerda 0</p>", "<p>Esquerda 1</p>");
    }

    // AC13: removing a used right row clears the match; moving one keeps each match with its text.
    [Fact]
    public async Task Add_Matching_RemovingAUsedRightRowClearsTheMatch_AndMovingOneKeepsItWithItsText()
    {
        var page = RenderAdd();
        await FillMatchingAsync(page, lefts: 2, rights: 3);
        await MatchAsync(page, 0, 2);
        await MatchAsync(page, 1, 0);

        page.Find("#question-option-actions-2-move-up").Click();

        page.WaitForAssertion(() => MatchSelect(page, 0).Value.Should().Be(1, "Direita 2 moved from C to B"));
        MatchSelect(page, 1).Value.Should().Be(0);

        page.Find("#question-option-actions-1-delete").Click();

        page.WaitForAssertion(() => MatchSelect(page, 0).Value.Should().BeNull());
        MatchSelect(page, 1).Value.Should().Be(0);
    }

    // AC13: picking another type and back keeps both typed lists.
    [Fact]
    public async Task Add_GoingToAnotherTypeAndBack_KeepsBothTypedLists()
    {
        var page = RenderAdd();
        await FillMatchingAsync(page, lefts: 1, rights: 2);
        await MatchAsync(page, 0, 1);

        page.Find("#question-type-TrueFalse").Click();
        page.Find("#question-type-Matching").Click();

        WaitForEditors(page, "question-match-item-0", "question-option-1");
        RichText.Editors["question-match-item-0"].Html.Should().Be("<p>Esquerda 0</p>");
        MatchSelect(page, 0).Value.Should().Be(1);
    }

    // AC13: on an existing question the type is read only and the stored matches are selected.
    [Fact]
    public void Edit_Matching_ShowsTheTypeAsAReadOnlyValue_AndTheStoredMatches()
    {
        Api.Questions.Add(MatchingDraft);

        var page = RenderEdit(MatchingDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        page.Find("#question-type-value").TextContent.Should().Contain("Matching");
        MatchSelect(page, 0).Value.Should().Be(1);
        MatchSelect(page, 1).Value.Should().Be(0);
        Notes(page).Should().Equal("Used by item 2", "Used by item 1", "Extra");
        page.Find(".app-form-aside").TextContent.Should().Contain("Matching");
    }

    // The publish checklist for this type: its own three items.
    [Fact]
    public async Task Edit_Matching_ChecklistIsDoneOnlyWhenEveryItemHasTextAndAMatch()
    {
        Api.Questions.Add(MatchingDraft);
        var page = RenderEdit(MatchingDraft.Id);
        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());

        ChecklistTexts(page).Should().Contain(text => text.Contains("2 to 8 items on the left"))
            .And.Contain(text => text.Contains("At least as many matches on the right"))
            .And.Contain(text => text.Contains("Every item has text and a match"));
        page.Find("#question-publish").HasAttribute("disabled").Should().BeFalse();

        await MatchAsync(page, 1, null);

        page.WaitForAssertion(() => page.FindAll(".app-form-aside-checklist li")
            .Single(item => item.TextContent.Contains("Every item has text and a match"))
            .ClassList.Should().Contain("app-checklist-pending"));
    }

    // AC13: the section refusals appear under the lists.
    [Theory]
    [InlineData(QuestionBankErrorCodes.QuestionMatchingItemsCount)]
    [InlineData(QuestionBankErrorCodes.QuestionMatchingKeyInvalid)]
    [InlineData(QuestionBankErrorCodes.QuestionMatchingKeyRepeated)]
    public async Task Edit_Matching_WhenTheApiRefusesTheKey_TheMessageIsUnderTheLists(string code)
    {
        Api.Questions.Add(MatchingDraft);
        Api.SaveFailure = (System.Net.HttpStatusCode.BadRequest, code, null);
        var page = RenderEdit(MatchingDraft.Id);
        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        await TypeAsync(page, "question-statement", "<p>Outro enunciado.</p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#question-matching-description .app-field-error-text")
            .TextContent.Should().NotBeNullOrWhiteSpace());
    }

    // AC13, UC7: changing what a number matches on a published question asks for the reason.
    [Fact]
    public async Task Edit_PublishedMatching_ChangingAMatch_AsksForTheReason()
    {
        Api.Questions.Add(MatchingDraft with { Status = QuestionStatus.Published });
        var page = RenderEdit(MatchingDraft.Id);
        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        page.FindAll("#question-change-reason").Should().BeEmpty();

        await MatchAsync(page, 0, 2);

        page.WaitForElement("#question-change-reason");
    }

    // F-101 BR5, F-105 BR3: a frozen question shows everything disabled.
    [Fact]
    public void Edit_AnnulledMatching_ShowsTheSelectsDisabled_AndNoAddButtons()
    {
        Api.Questions.Add(MatchingDraft with { Status = QuestionStatus.Annulled });

        var page = RenderEdit(MatchingDraft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-type-value").Should().ContainSingle());
        MatchSelect(page, 0).Disabled.Should().BeTrue();
        page.FindAll("#question-match-item-add").Should().BeEmpty();
        page.FindAll("#question-option-add").Should().BeEmpty();
    }
}
