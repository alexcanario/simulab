using System.Globalization;
using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-95 `/admin/questions/new` and `/admin/questions/{id}`: the question form with its options, its checklist and
/// its publication card (AC1, AC2, AC4, AC5, AC6, AC7, AC12 entry, AC14, AC17 as far as bUnit reaches).
/// </summary>
public sealed class QuestionFormTests : QuestionPageTestContext
{
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

    private static Task SetAsync<T>(IRenderedComponent<QuestionForm> page, string id, T? value) =>
        page.InvokeAsync(() => page.FindComponents<AppSelectField<T>>()
            .Single(component => component.Instance.Id == id).Instance.ValueChanged.InvokeAsync(value));

    private static Task MarkCorrectAsync(IRenderedComponent<QuestionForm> page, int? position) =>
        page.InvokeAsync(() => page.FindComponent<MudRadioGroup<int?>>().Instance.ValueChanged.InvokeAsync(position));

    private static void AddOptions(IRenderedComponent<QuestionForm> page, int count)
    {
        for (var index = 0; index < count; index++)
        {
            page.Find("#question-option-add").Click();
        }
    }

    private async Task FillValidDraftAsync(IRenderedComponent<QuestionForm> page, int options = 2, int? correct = 0)
    {
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", "<p>Qual frase está certa?</p>");

        AddOptions(page, options);
        for (var index = 0; index < options; index++)
        {
            WaitForEditors(page, $"question-option-{index}");
            await TypeAsync(page, $"question-option-{index}", $"<p>Opção {QuestionText.Letter(index)}</p>");
        }

        if (correct is not null)
        {
            await MarkCorrectAsync(page, correct);
        }
    }

    private string CurrentUri => Services.GetRequiredService<NavigationManager>().Uri;

    private static string ErrorOf(IRenderedComponent<QuestionForm> page, string fieldId) =>
        page.Find($"#{fieldId}-description .app-field-error-text").TextContent;

    private static IReadOnlyList<string> ChecklistStates(IRenderedComponent<QuestionForm> page) =>
    [
        .. page.FindAll(".app-form-aside-checklist li")
            .Select(item => item.ClassList.Contains("app-checklist-done") ? "done" : "pending")
    ];

    // -- the screen ---------------------------------------------------------------------------------------------

    [Fact]
    public void Add_RendersTheFiveSectionsInOrderWithTheirFieldsAndAskToSaveFirst()
    {
        var page = RenderAdd();

        page.Find("h1").TextContent.Should().Be("Add question");
        page.FindAll("section.app-section-card h2").Select(heading => heading.TextContent.Trim()).Should().Equal(
            "Classification", "Statement", "Base text", "Options", "Explanation", "Publication");

        foreach (var id in new[] { "question-subject", "question-topic", "question-content-language", "question-difficulty" })
        {
            page.FindAll($"#{id}").Should().NotBeEmpty($"the form has the field {id}");
        }

        foreach (var id in new[] { "question-statement", "question-base-text", "question-explanation" })
        {
            page.FindAll($"#{id}-label").Should().ContainSingle($"the rich text field {id} has its label");
        }

        // A question that is not saved has nothing to preview or publish: the card only says so.
        page.Markup.Should().Contain("Save the question first to preview or publish it.");
        page.FindAll("#question-preview, #question-publish, #question-unpublish").Should().BeEmpty();
        page.Find(".app-form-save").Should().NotBeNull("Save is the page's one primary button");
    }

    [Fact]
    public void Add_RequiredFieldsAreMarked_AndTheStatementEditorIsNamedByItsLabel()
    {
        var page = RenderAdd();
        WaitForEditors(page, "question-statement", "question-base-text", "question-explanation");

        RichText.Editors["question-statement"].Required.Should().BeTrue();
        RichText.Editors["question-statement"].LabelId.Should().Be("question-statement-label");
        RichText.Editors["question-statement"].DescriptionId.Should().Be("question-statement-description");
        RichText.Editors["question-base-text"].Required.Should().BeFalse();
        RichText.Editors["question-explanation"].Required.Should().BeFalse();
        page.Find("#question-statement-description").TextContent.Should().Contain("Up to 10,000 characters.");
        page.Find("#question-base-text-description").TextContent.Should().Contain("Up to 20,000 characters.");
    }

    // AC14: the form starts with the curator's UI language when the app supports it, and with the default when not.
    [Theory]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("pt-PT", "pt-PT")]
    [InlineData("en", "en")]
    [InlineData("fr", "en")]
    public void Add_PreselectsTheUiLanguage(string uiCulture, string expected)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(uiCulture);

        var page = RenderAdd();

        page.FindComponents<AppSelectField<string>>()
            .Single(component => component.Instance.Id == "question-content-language")
            .Instance.Value.Should().Be(expected);
        WaitForEditors(page, "question-statement");
        RichText.Editors["question-statement"].Lang.Should().Be(expected, "the content is marked with its language");
    }

    [Fact]
    public async Task Language_Changed_MarksEveryEditorWithTheNewLanguage()
    {
        var page = RenderAdd();
        AddOptions(page, 1);
        WaitForEditors(page, "question-statement", "question-option-0");

        await SetAsync<string>(page, "question-content-language", "pt-PT");

        page.WaitForAssertion(() =>
        {
            RichText.Editors["question-statement"].Lang.Should().Be("pt-PT");
            RichText.Editors["question-option-0"].Lang.Should().Be("pt-PT");
        });
    }

    [Fact]
    public async Task Topic_IsDisabledUntilASubjectIsChosen_AndOffersOnlyThatSubjectsTopics()
    {
        var page = RenderAdd();
        var topic = () => page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "question-topic").Instance;

        topic().Disabled.Should().BeTrue();
        page.Find("#question-topic-description").TextContent.Should().Contain("Choose a subject first.");
        topic().MinChars.Should().Be(0);
        topic().OpenOnFocus.Should().BeTrue();

        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));

        page.WaitForAssertion(() => topic().Disabled.Should().BeFalse());
        page.Find("#question-topic-description").TextContent.Should().Contain("Only topics of the chosen subject.");
        var offered = await topic().SearchAsync(string.Empty, CancellationToken.None);
        offered.Select(option => option.Text).Should().BeEquivalentTo("Crase", "Pontuação");
    }

    [Fact]
    public async Task Topic_Search_NarrowsByTheTermIgnoringCaseAndAccents()
    {
        var page = RenderAdd();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        var topic = page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "question-topic").Instance;

        var found = await topic.SearchAsync("PONTUACAO", CancellationToken.None);

        found.Select(option => option.Text).Should().Equal("Pontuação");
    }

    [Fact]
    public async Task Subject_ChangedOrCleared_ClearsTheTopic()
    {
        var page = RenderAdd();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        var topic = () => page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "question-topic").Instance;
        topic().Value!.Text.Should().Be("Crase");

        await PickAsync(page, "question-subject", new AppLookupOption(Law.Id, Law.Name));

        page.WaitForAssertion(() => topic().Value.Should().BeNull("a topic belongs to one subject"));
        await PickAsync(page, "question-topic", new AppLookupOption(FundamentalRights.Id, FundamentalRights.Name));
        await PickAsync(page, "question-subject", null);

        page.WaitForAssertion(() =>
        {
            topic().Value.Should().BeNull();
            topic().Disabled.Should().BeTrue();
        });
    }

    // -- saving a new question --------------------------------------------------------------------------------------

    // AC1: the question is sent with its topic, language, statement and options in the order the curator sees.
    [Fact]
    public async Task Save_New_SendsATopicLanguageStatementDifficultyAndTheOptionsInOrder_AndOpensTheSavedQuestion()
    {
        var providers = RenderProviders();
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 3, correct: 1);
        await SetAsync<int?>(page, "question-difficulty", 4);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.TopicId.Should().Be(Crase.Id);
        sent.ContentLanguage.Should().Be("en");
        sent.StatementHtml.Should().Be("<p>Qual frase está certa?</p>");
        sent.BaseTextHtml.Should().BeNull("an untouched base text is not sent");
        sent.ExplanationHtml.Should().BeNull();
        sent.Difficulty.Should().Be(4);
        sent.Options!.Select(option => option.TextHtml).Should().Equal("<p>Opção A</p>", "<p>Opção B</p>", "<p>Opção C</p>");
        sent.Options!.Select(option => option.IsCorrect).Should().Equal(false, true, false);

        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question saved."));
        var created = Api.Questions.Single();
        page.WaitForAssertion(() => CurrentUri.Should().EndWith($"/admin/questions/{created.Id}"));
        created.Status.Should().Be(QuestionStatus.Draft);
        page.Find("h1").TextContent.Should().Be("Edit question");
    }

    // AC2 (BR3): a draft needs a topic, a language and a statement; options and key may come later.
    [Fact]
    public async Task Save_DraftWithNoOptionsAndNoKey_IsSent()
    {
        RenderProviders();
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 0, correct: null);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        Api.LastSaved(HttpMethod.Post).Options.Should().BeEmpty();
    }

    // AC2 (client part): empty after tags are removed counts as empty, so nothing is sent and the summary names it.
    [Fact]
    public async Task Save_StatementOnlyEmptyTags_ShowsTheMessageAndTheSummaryAndSendsNothing()
    {
        var page = RenderAdd();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", "<p><br></p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => ErrorOf(page, "question-statement").Should().Be("Write the statement."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
        var summary = page.Find("#question-error-summary");
        summary.GetAttribute("role").Should().Be("alert");
        summary.QuerySelectorAll("a").Select(link => link.TextContent.Trim()).Should().Equal("Statement");
        JSInterop.Invocations.Should().Contain(call =>
            call.Identifier == "simulabShell.focusElement" && (string?)call.Arguments[0] == "question-error-summary");
        page.WaitForAssertion(() => RichText.Editors["question-statement"].Invalid.Should()
            .BeTrue("the editable is marked invalid for a screen reader"));
    }

    [Fact]
    public void Save_WithoutATopic_AsksForItAndSendsNothing()
    {
        var page = RenderAdd();
        WaitForEditors(page, "question-statement");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#question-topic-description").TextContent.Should().Contain("Choose the topic."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task Save_ErrorSummary_ListsEveryRefusedFieldInTheOrderTheFormShowsThem()
    {
        var page = RenderAdd();
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", "<p></p>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.FindAll("#question-error-summary a").Select(link => link.TextContent.Trim())
            .Should().Equal("Topic", "Statement"));
    }

    // -- the options ----------------------------------------------------------------------------------------------

    [Fact]
    public void Options_None_SaysADraftNeedsNoneAndShowsTheCountAlways()
    {
        var page = RenderAdd();

        page.Markup.Should().Contain("No option yet. A draft can be saved without options; publishing needs 2 to 5.");
        page.Find(".app-options-count").TextContent.Should().Be("0 of 5 options");
    }

    [Fact]
    public void Options_Add_AddsALetteredRowWithItsOwnEditor_AndFocusesIt()
    {
        var page = RenderAdd();

        AddOptions(page, 2);

        page.FindAll(".app-option-letter").Select(letter => letter.TextContent).Should().Equal("A", "B");
        WaitForEditors(page, "question-option-0", "question-option-1");
        RichText.Editors["question-option-1"].Compact.Should().BeTrue("an option is a phrase: inline formats only");
        var hiddenLabels = page.FindAll("label.app-visually-hidden").Select(label => label.TextContent.Trim()).ToList();
        hiddenLabels.Should().Contain("Text of option A").And.Contain("Text of option B");
        page.Find(".app-options-count").TextContent.Should().Be("2 of 5 options");

        // The row added last is the one whose editor takes the focus.
        page.WaitForAssertion(() => RichText.Focused.Should().NotBeEmpty().And.EndWith("question-option-1"));
    }

    [Fact]
    public void Options_AtFive_TheAddButtonIsDisabledAndTheCountSaysWhy()
    {
        var page = RenderAdd();

        AddOptions(page, 5);

        page.Find("#question-option-add").HasAttribute("disabled").Should().BeTrue();
        page.Find(".app-options-count").TextContent.Should().Be("5 of 5 options");
        page.FindAll(".app-option-letter").Select(letter => letter.TextContent).Should().Equal("A", "B", "C", "D", "E");
    }

    [Fact]
    public async Task Options_MarkingOneCorrect_UnmarksTheOther_AndSaveSendsTheKey()
    {
        RenderProviders();
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 3, correct: 0);

        await MarkCorrectAsync(page, 2);
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        Api.LastSaved(HttpMethod.Post).Options!.Select(option => option.IsCorrect).Should().Equal(false, false, true);
    }

    [Fact]
    public void Options_EachRowsRadioNamesItsLetter_AndTheGroupNamesTheCorrectOption()
    {
        var page = RenderAdd();

        AddOptions(page, 2);

        page.Markup.Should().Contain("Mark A as correct").And.Contain("Mark B as correct");

        // The group is named "Correct option" and described by the section-level message. Where the library puts
        // these attributes in its own DOM is checked on screen (unverified (gallery DOM)).
        var group = page.FindComponent<MudRadioGroup<int?>>().Instance;
        group.UserAttributes.Should().Contain("aria-label", "Correct option");
        group.UserAttributes.Should().Contain("aria-describedby", "question-options-description");
        page.FindAll("#question-options-description").Should().ContainSingle();
    }

    [Fact]
    public async Task Options_Move_ReordersWithTheirTextAndKey_AnnouncesIt_AndKeepsTheFocusOnThePressedButton()
    {
        RenderProviders();
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 3, correct: 0);

        page.Find("#question-option-actions-0-move-down").Click();

        // The text and the key travel with the option; the letters follow the new order.
        page.WaitForAssertion(() => RichText.Editors["question-option-1"].Html.Should().Be("<p>Opção A</p>"));
        RichText.Editors["question-option-0"].Html.Should().Be("<p>Opção B</p>");
        page.Markup.Should().Contain("Option moved to position 2 of 3.");
        page.WaitForAssertion(() => JSInterop.Invocations.Should().Contain(call =>
            call.Identifier == "simulabShell.focusElement" && (string?)call.Arguments[0] == "question-option-actions-1-move-down"));

        page.Find(".app-form-save").Click();
        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        var sent = Api.LastSaved(HttpMethod.Post).Options!;
        sent.Select(option => option.TextHtml).Should().Equal("<p>Opção B</p>", "<p>Opção A</p>", "<p>Opção C</p>");
        sent.Select(option => option.IsCorrect).Should().Equal([false, true, false], "the key moved with its option");
    }

    [Fact]
    public void Options_FirstCannotMoveUpAndLastCannotMoveDown_AndTheButtonSaysWhy()
    {
        var page = RenderAdd();

        AddOptions(page, 3);

        var rows = page.FindComponents<AppRowActions>().Select(component => component.Instance).ToList();
        rows.Should().HaveCount(3);
        rows[0].MoveUpDisabledReason.Should().Be("Already the first option.");
        rows[0].MoveDownDisabledReason.Should().BeNull();
        rows[1].MoveUpDisabledReason.Should().BeNull();
        rows[1].MoveDownDisabledReason.Should().BeNull();
        rows[2].MoveDownDisabledReason.Should().Be("Already the last option.");
        page.Find("#question-option-actions-0-move-up").HasAttribute("disabled").Should().BeTrue();
        page.Find("#question-option-actions-2-move-down").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public async Task Options_Remove_NeedsNoConfirmation_AnnouncesIt_AndFocusesTheNextRowsRemoveButton()
    {
        var page = RenderAdd();
        AddOptions(page, 3);
        WaitForEditors(page, "question-option-0", "question-option-1", "question-option-2");
        await TypeAsync(page, "question-option-1", "<p>B</p>");

        page.Find("#question-option-actions-0-delete").Click();

        page.WaitForAssertion(() => page.FindAll(".app-option-letter").Should().HaveCount(2));
        page.Markup.Should().Contain("Option removed. 2 left.");
        page.Find(".app-options-count").TextContent.Should().Be("2 of 5 options");
        page.WaitForAssertion(() => RichText.Editors["question-option-0"].Html.Should().Be("<p>B</p>"));
        JSInterop.Invocations.Should().Contain(call =>
            call.Identifier == "simulabShell.focusElement" && (string?)call.Arguments[0] == "question-option-actions-0-delete");
    }

    [Fact]
    public void Options_RemoveTheLast_FocusesTheAddButton()
    {
        var page = RenderAdd();
        AddOptions(page, 1);

        page.Find("#question-option-actions-0-delete").Click();

        page.WaitForAssertion(() => JSInterop.Invocations.Should().Contain(call =>
            call.Identifier == "simulabShell.focusElement" && (string?)call.Arguments[0] == "question-option-add"));
    }

    // -- the aside ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Checklist_FollowsTheFormLive_InTheOrderOfTheRules()
    {
        var page = RenderAdd();
        page.FindAll(".app-form-aside-checklist li")
            .Select(item => item.TextContent.Replace("Pending", string.Empty).Replace("Done", string.Empty).Trim())
            .Should().Equal(
                "Topic", "Content language", "Statement", "2 to 5 options", "Every option has text", "Exactly one correct option");
        ChecklistStates(page).Should().Equal("pending", "done", "pending", "pending", "pending", "pending");

        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", "<p>Enunciado</p>");
        page.WaitForAssertion(() => ChecklistStates(page).Should().Equal("done", "done", "done", "pending", "pending", "pending"));

        AddOptions(page, 2);
        WaitForEditors(page, "question-option-0", "question-option-1");
        await TypeAsync(page, "question-option-0", "<p>A</p>");
        page.WaitForAssertion(() => ChecklistStates(page).Should().Equal("done", "done", "done", "done", "pending", "pending"));

        await TypeAsync(page, "question-option-1", "<p>B</p>");
        await MarkCorrectAsync(page, 1);
        page.WaitForAssertion(() => ChecklistStates(page).Should().OnlyContain(state => state == "done"));
    }

    [Fact]
    public async Task Aside_SummarisesTheFormAndTheOptionCount()
    {
        var page = RenderAdd();
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        await SetAsync<int?>(page, "question-difficulty", 2);
        AddOptions(page, 2);

        page.WaitForAssertion(() =>
        {
            var aside = page.Find("aside.app-form-aside").TextContent;
            aside.Should().Contain("Português").And.Contain("Crase").And.Contain("2 of 5 options");
            aside.Should().Contain("Not filled", "the status of a question that is not saved is not filled");
        });
    }

    // -- editing a stored question --------------------------------------------------------------------------------

    [Fact]
    public void Edit_OpensFilledWithTheStoredQuestion()
    {
        Api.Questions.Add(Published);

        var page = RenderEdit(Published.Id);

        WaitForEditors(page, "question-statement", "question-option-2");
        page.Find("h1").TextContent.Should().Be("Edit question");
        RichText.Editors["question-statement"].Html.Should().Be(Published.StatementHtml);
        RichText.Editors["question-base-text"].Html.Should().Be(Published.BaseTextHtml);
        RichText.Editors["question-explanation"].Html.Should().Be(Published.ExplanationHtml);
        RichText.Editors["question-option-0"].Html.Should().Be("<p>Antes do vocativo</p>");
        page.FindAll(".app-option-letter").Select(letter => letter.TextContent).Should().Equal("A", "B", "C");
        page.FindComponents<AppLookupField>().Single(c => c.Instance.Id == "question-topic").Instance.Value!.Text.Should().Be("Pontuação");
        page.FindComponents<AppLookupField>().Single(c => c.Instance.Id == "question-subject").Instance.Value!.Text.Should().Be("Português");
        page.FindComponents<AppSelectField<string>>().Single(c => c.Instance.Id == "question-content-language").Instance.Value.Should().Be("pt-PT");
        page.FindComponent<MudRadioGroup<int?>>().Instance.Value.Should().Be(0, "the stored key is the first option");
    }

    [Fact]
    public void Edit_UnknownQuestion_ShowsTheNotFoundAlertAndTheWayBack()
    {
        var page = RenderEdit(Guid.NewGuid());

        page.WaitForAssertion(() => page.Markup.Should().Contain("This question no longer exists."));
        page.Find("a.app-link").GetAttribute("href").Should().Be("/admin/questions");
        page.Markup.Should().Contain("Back to the questions");
        page.FindAll(".app-form-save").Should().BeEmpty();
    }

    [Fact]
    public void Edit_LoadFails_ShowsTheErrorStateAndRetryLoadsTheQuestion()
    {
        Api.Questions.Add(Draft);
        Api.FindFailure = (HttpStatusCode.InternalServerError, "common.unexpected_error");
        var page = RenderEdit(Draft.Id);
        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));

        Api.FindFailure = null;
        page.Find(".app-retry").Click();

        WaitForEditors(page, "question-statement");
        RichText.Editors["question-statement"].Html.Should().Be(Draft.StatementHtml);
    }

    // AC7: a published question is edited in place and stays published.
    [Fact]
    public async Task Edit_Published_ChangingTheStatement_SendsAPutAndStaysPublished()
    {
        RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");

        await TypeAsync(page, "question-statement", "<p>Novo enunciado</p>");
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Put && call.Path == $"/api/v1/question-bank/questions/{Published.Id}"));
        Api.LastSaved(HttpMethod.Put).StatementHtml.Should().Be("<p>Novo enunciado</p>");
        Api.LastSaved(HttpMethod.Put).Options.Should().HaveCount(3);
        Api.Questions.Single().Status.Should().Be(QuestionStatus.Published);
        page.WaitForAssertion(() => page.Markup.Should().Contain("Published. Edits apply at once and must keep it complete."));
    }

    // AC7 (client part): the Api refuses an edit that leaves one option; the screen says so before asking.
    [Fact]
    public void Edit_Published_LeavingOneOption_IsRefusedBeforeTheApi()
    {
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-option-2");

        page.Find("#question-option-actions-2-delete").Click();
        page.WaitForAssertion(() => page.FindAll(".app-option-letter").Should().HaveCount(2));
        page.Find("#question-option-actions-1-delete").Click();
        page.WaitForAssertion(() => page.FindAll(".app-option-letter").Should().HaveCount(1));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#question-options-description").TextContent
            .Should().Contain("A question is published with 2 to 5 options."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Put);
    }

    [Fact]
    public async Task Edit_Published_OptionWithNoText_MarksThatOptionAndSendsNothing()
    {
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-option-1");

        await TypeAsync(page, "question-option-1", "<p><br></p>");
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => ErrorOf(page, "question-option-1").Should().Be("Every option needs text."));
        RichText.Editors["question-option-1"].Invalid.Should().BeTrue();
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Put);
    }

    [Fact]
    public async Task Edit_Published_WithNoKey_ShowsTheSectionLevelMessage()
    {
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-option-0");

        await MarkCorrectAsync(page, null);
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#question-options-description").TextContent
            .Should().Contain("Mark exactly one option as correct."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Put);
    }

    // BR8: what is stored replaces what the editors hold, so the curator sees what the Api kept.
    [Fact]
    public async Task Save_TheEditorsShowTheSanitizedTextTheApiStored()
    {
        RenderProviders();
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 2, correct: 0);
        await TypeAsync(page, "question-statement", "<p>Ok</p><script>alert(1)</script>");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => RichText.Pushed.Should().Contain(("question-statement", "<p>Ok</p>")));
        RichText.Editors["question-statement"].Html.Should().Be("<p>Ok</p>");
    }

    // -- refusals by code -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(QuestionBankErrorCodes.QuestionStatementTooLong, "question-statement", "The statement has more than 10,000 characters.")]
    [InlineData(QuestionBankErrorCodes.QuestionStatementRequired, "question-statement", "Write the statement.")]
    [InlineData(QuestionBankErrorCodes.QuestionBaseTextTooLong, "question-base-text", "The base text has more than 20,000 characters.")]
    [InlineData(QuestionBankErrorCodes.QuestionExplanationTooLong, "question-explanation", "The explanation has more than 10,000 characters.")]
    [InlineData(QuestionBankErrorCodes.QuestionDifficultyInvalid, "question-difficulty", "Difficulty is a number from 1 to 5, or empty.")]
    [InlineData(QuestionBankErrorCodes.QuestionContentLanguageInvalid, "question-content-language", "Choose one of the supported languages.")]
    [InlineData(QuestionBankErrorCodes.QuestionOptionsCount, "question-options", "A question is published with 2 to 5 options.")]
    [InlineData(QuestionBankErrorCodes.QuestionKeyInvalid, "question-options", "Mark exactly one option as correct.")]
    [InlineData(QuestionBankErrorCodes.QuestionOptionTextRequired, "question-options", "Every option needs text.")]
    public async Task Save_RefusedByCode_ShowsTheTextOnTheFieldThatCausedItAndInTheSummary(string code, string field, string text)
    {
        RenderProviders();
        Api.SaveFailure = (HttpStatusCode.BadRequest, code, null);
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 2, correct: 0);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find($"#{field}-description").TextContent.Should().Contain(text));
        page.Find("#question-error-summary").Should().NotBeNull();
        page.FindAll(".app-form-save").Should().ContainSingle();
    }

    [Fact]
    public async Task Save_OptionRefusedWithItsPosition_MarksThatRowAndNotTheOthers()
    {
        RenderProviders();
        Api.SaveFailure = (HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionOptionTooLong, 1);
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 3, correct: 0);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => ErrorOf(page, "question-option-1").Should().Be("An option has more than 2,000 characters."));
        page.FindAll("#question-option-0-description .app-field-error-text").Should().BeEmpty();
        page.FindAll("#question-option-2-description .app-field-error-text").Should().BeEmpty();
        page.FindAll("#question-options-description .app-field-error-text").Should().BeEmpty();
        RichText.Editors["question-option-1"].Invalid.Should().BeTrue();
        page.Find("#question-error-summary").TextContent.Should().Contain("Text of option B");
    }

    [Fact]
    public async Task Save_OptionRefusedWithoutAPosition_FallsBackToTheSectionMessage()
    {
        RenderProviders();
        Api.SaveFailure = (HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionOptionTooLong, null);
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 2, correct: 0);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#question-options-description").TextContent
            .Should().Contain("An option has more than 2,000 characters."));
    }

    [Fact]
    public async Task Save_TopicGoneMeanwhile_ClearsTheTopicAsksAgainAndSaysWhy()
    {
        RenderProviders();
        Api.SaveFailure = (HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionTopicNotFound, null);
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 1, correct: null);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#question-topic-description").TextContent.Should().Contain("Choose the topic."));
        page.FindComponents<AppLookupField>().Single(c => c.Instance.Id == "question-topic").Instance.Value.Should().BeNull();
        page.Find(".app-alert").TextContent.Should().Contain("The topic no longer exists. Pick another.");
    }

    [Fact]
    public async Task Save_UnexpectedFailure_ShowsTheGenericAlertAndNoFieldError()
    {
        RenderProviders();
        Api.SaveFailure = (HttpStatusCode.InternalServerError, "common.unexpected_error", null);
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 1, correct: null);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find(".app-alert").TextContent.Should().Contain("Something went wrong"));
        page.FindAll(".app-field-error-text").Should().BeEmpty();
        page.FindAll("#question-error-summary").Should().BeEmpty();
    }

    [Fact]
    public async Task Save_ShowsProgressWhileTheCallIsInFlight_AndDisablesSaveAndCancel()
    {
        RenderProviders();
        Api.WriteGate = new TaskCompletionSource();
        var page = RenderAdd();
        await FillValidDraftAsync(page, options: 1, correct: null);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find(".app-form-save").HasAttribute("disabled").Should().BeTrue());
        page.Find(".app-form-save").TextContent.Should().Contain("Saving");
        Api.WriteGate!.SetResult();
        page.WaitForAssertion(() => page.Find(".app-form-save").HasAttribute("disabled").Should().BeFalse());
    }

    [Fact]
    public void Cancel_GoesBackToTheList()
    {
        var page = RenderAdd();

        page.Find(".app-form-cancel").Click();

        CurrentUri.Should().EndWith("/admin/questions");
    }

    // -- publication --------------------------------------------------------------------------------------------

    private static string PublicationReason(IRenderedComponent<QuestionForm> page) =>
        page.Find("#question-publication-reason").TextContent.Trim();

    private static QuestionResponse CompleteDraft() =>
        Draft with
        {
            Options = [new(0, "<p>Fui à escola.</p>", true), new(1, "<p>Fui a escola.</p>", false)]
        };

    [Fact]
    public void Publication_ADraftWithAnIncompleteChecklist_DisablesPublishAndSaysWhyAsVisibleText()
    {
        Api.Questions.Add(Draft);

        var page = RenderEdit(Draft.Id);
        WaitForEditors(page, "question-statement");

        page.Find("#question-publish").HasAttribute("disabled").Should().BeTrue();
        PublicationReason(page).Should().Be("Complete the checklist before publishing.");
        page.Find("#question-publish").GetAttribute("aria-describedby").Should().Be("question-publication-reason");
        page.Markup.Should().Contain("Draft. Not available outside the back office.");
        page.Find("#question-publication-reason").Should().NotBeNull();
        page.FindAll(".app-status-chip-neutral").Should().NotBeEmpty();
    }

    [Fact]
    public async Task Publication_WithUnsavedChanges_DisablesPublishBecauseItActsOnWhatIsStored()
    {
        Api.Questions.Add(CompleteDraft());
        var page = RenderEdit(Draft.Id);
        WaitForEditors(page, "question-statement");
        page.Find("#question-publish").HasAttribute("disabled").Should().BeFalse("a complete, saved draft can be published");

        await TypeAsync(page, "question-statement", "<p>Mudou</p>");

        page.WaitForAssertion(() => PublicationReason(page).Should().Be("Save your changes first."));
        page.Find("#question-publish").HasAttribute("disabled").Should().BeTrue();
    }

    // AC5, AC6, BR12: publishing changes the status only and never saves; the chip, the sentence and the button follow.
    [Fact]
    public void Publication_Publish_CallsPublishOnTheStoredQuestionAndTheCardBecomesPublished()
    {
        var providers = RenderProviders();
        Api.Questions.Add(CompleteDraft());
        var page = RenderEdit(Draft.Id);
        WaitForEditors(page, "question-statement");

        page.Find("#question-publish").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/question-bank/questions/{Draft.Id}/publish"));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Put, "Publish never saves");
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question published."));
        page.WaitForAssertion(() => page.FindAll("#question-unpublish").Should().ContainSingle());
        page.FindAll("#question-publish").Should().BeEmpty();
        page.Markup.Should().Contain("Published. Edits apply at once and must keep it complete.");
        page.FindAll(".app-status-chip-success").Should().NotBeEmpty();
        page.Find("aside.app-form-aside").TextContent.Should().Contain("Published");
    }

    [Fact]
    public void Publication_Unpublish_IsNeverDisabled_AndTheQuestionIsADraftAgain()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");

        page.Find("#question-unpublish").HasAttribute("disabled").Should().BeFalse();
        page.Find("#question-unpublish").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/question-bank/questions/{Published.Id}/unpublish"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question unpublished. It is a draft again."));
        page.WaitForAssertion(() => page.FindAll("#question-publish").Should().ContainSingle());
        Api.Questions.Single().Options.Should().HaveCount(3, "unpublishing does not touch the content");
    }

    [Fact]
    public async Task Publication_UnpublishWithUnsavedChanges_KeepsWhatTheCuratorTyped()
    {
        RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", "<p>Em andamento</p>");

        page.Find("#question-unpublish").Click();

        page.WaitForAssertion(() => page.FindAll("#question-publish").Should().ContainSingle());
        RichText.Editors["question-statement"].Html.Should().Be("<p>Em andamento</p>");
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Put);
    }

    [Fact]
    public void Publication_RefusedByTheApi_ShowsTheCodesTextInsideTheCard()
    {
        RenderProviders();
        Api.Questions.Add(CompleteDraft());
        Api.PublicationFailure = (HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionAlreadyPublished);
        var page = RenderEdit(Draft.Id);
        WaitForEditors(page, "question-statement");

        page.Find("#question-publish").Click();

        page.WaitForAssertion(() => page.Find("section[aria-labelledby='question-section-publication-title']")
            .TextContent.Should().Contain("This question is already published."));
    }

    // -- preview ----------------------------------------------------------------------------------------------------

    // AC12: the preview shows the stored statement, base text and options lettered; the key waits for "show answer".
    [Fact]
    public void Preview_ShowsTheStoredQuestionWithTheKeyHiddenUntilShowAnswer()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");

        page.Find("#question-preview").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll("#question-preview-statement").Should().ContainSingle());
        var dialog = providers.Dialogs;
        dialog.Find("#question-preview-statement").InnerHtml.Should().Be("<p>Onde entra a vírgula?</p>");
        dialog.Find("#question-preview-base-text").InnerHtml.Should().Contain("<sup>nota</sup>");
        dialog.FindAll("li.app-question-option .app-option-letter").Select(letter => letter.TextContent).Should().Equal("A", "B", "C");
        dialog.FindAll(".is-correct").Should().BeEmpty();
        dialog.Markup.Should().NotContain("Porque separa o vocativo.").And.NotContain("Correct answer");
        dialog.FindAll("[lang='pt-PT']").Should().NotBeEmpty("the content is marked with its own language");
        dialog.FindAll(".mud-alert").Should().BeEmpty("nothing is unsaved: no note");

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("li.is-correct").Should().ContainSingle());
        dialog.Find("li.is-correct .app-option-letter").TextContent.Should().Be("A");
        dialog.Find("li.is-correct").TextContent.Should().Contain("Correct answer");
        dialog.Markup.Should().Contain("Porque separa o vocativo.");
    }

    [Fact]
    public async Task Preview_WithUnsavedChanges_StartsWithTheNoteAndShowsTheSavedVersion()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");
        await TypeAsync(page, "question-statement", "<p>Ainda não salvo</p>");

        page.Find("#question-preview").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should()
            .Contain("Showing the saved version. Unsaved changes are not included."));
        providers.Dialogs.Markup.Should().Contain("Onde entra a vírgula?").And.NotContain("Ainda não salvo");
    }

    [Fact]
    public void Preview_AStoredQuestionAlwaysHasAnEnabledPreviewButton()
    {
        Api.Questions.Add(Draft);

        var page = RenderEdit(Draft.Id);
        WaitForEditors(page, "question-statement");

        page.Find("#question-preview").HasAttribute("disabled").Should().BeFalse("preview is always enabled for a saved question");
    }
}
