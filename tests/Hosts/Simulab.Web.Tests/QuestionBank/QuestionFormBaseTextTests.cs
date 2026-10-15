using System.Net;
using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-102 on the question form: the "Base text" section is a picker of stored base texts in the question's
/// language, with the open link, the read-only text, the create link, the language change and the refusals
/// (AC13, and the screen part of AC2 and AC11).
/// </summary>
public sealed class QuestionFormBaseTextTests : QuestionPageTestContext
{
    private static readonly BaseTextResponse OtherPtPtText = CommaText with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-0000000000d5"),
        Title = "Texto II - A crase",
        TextHtml = "<p>Outro texto em pt-PT.</p>",
        QuestionCount = 0
    };

    private IRenderedComponent<QuestionForm> RenderAdd() => Render<QuestionForm>();

    private IRenderedComponent<QuestionForm> RenderEdit(Guid id) =>
        Render<QuestionForm>(parameters => parameters.Add(form => form.Id, id));

    private void WaitForEditors(IRenderedComponent<QuestionForm> page, params string[] ids) =>
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain(ids));

    private static AppLookupField BaseTextField(IRenderedComponent<QuestionForm> page) =>
        page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "question-base-text").Instance;

    private static Task PickAsync(IRenderedComponent<QuestionForm> page, string id, AppLookupOption? option) =>
        page.InvokeAsync(() => page.FindComponents<AppLookupField>()
            .Single(component => component.Instance.Id == id).Instance.ValueChanged.InvokeAsync(option));

    private static Task SetLanguageAsync(IRenderedComponent<QuestionForm> page, string language) =>
        page.InvokeAsync(() => page.FindComponents<AppSelectField<string>>()
            .Single(component => component.Instance.Id == "question-content-language").Instance.ValueChanged.InvokeAsync(language));

    private static AppFormActions Actions(IRenderedComponent<QuestionForm> page) =>
        page.FindComponent<AppFormActions>().Instance;

    private static AppLookupOption OptionOf(BaseTextResponse text) => new(text.Id, text.Title);

    private async Task FillValidDraftAsync(IRenderedComponent<QuestionForm> page)
    {
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        WaitForEditors(page, "question-statement");
        await page.InvokeAsync(() => RichText.TypeAsync("question-statement", "<p>Qual frase está certa?</p>"));
    }

    // -- the picker -------------------------------------------------------------------------------------------------

    // AC13: only the texts of the question's language are offered, the search goes to the server by language.
    [Fact]
    public async Task Picker_OffersOnlyTheTextsOfTheQuestionsLanguage_AndAsksTheServerByLanguage()
    {
        Api.SeedBaseTexts(ClinicalCase, PacificText, UnusedTariffs);
        var page = RenderAdd();

        var english = await BaseTextField(page).SearchAsync(string.Empty, CancellationToken.None);

        english.Select(option => option.Text).Should().Equal(PacificText.Title);
        Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Get && call.Path == "/api/v1/question-bank/base-texts"
            && call.Query!.Contains("language=en", StringComparison.Ordinal)
            && call.Query.Contains("pageSize=20", StringComparison.Ordinal));

        await page.InvokeAsync(() => page.FindComponents<AppSelectField<string>>()
            .Single(component => component.Instance.Id == "question-content-language").Instance.ValueChanged.InvokeAsync("pt-BR"));
        var portuguese = await BaseTextField(page).SearchAsync(string.Empty, CancellationToken.None);

        portuguese.Select(option => option.Text).Should().Equal(ClinicalCase.Title, UnusedTariffs.Title);
    }

    [Fact]
    public async Task Picker_Search_SendsTheTermAndShowsTheCountAndTheDateUnderEachTitle()
    {
        Api.SeedBaseTexts(ClinicalCase, UnusedTariffs);
        var page = RenderAdd();
        await SetLanguageAsync(page, "pt-BR");

        var found = await BaseTextField(page).SearchAsync("clínico", CancellationToken.None);

        found.Should().ContainSingle();
        found[0].Id.Should().Be(ClinicalCase.Id);
        found[0].Secondary.Should().StartWith("Questions: 3 · changed on ");
        Api.Received.Should().Contain(call => call.Query != null && call.Query.Contains("search=cl", StringComparison.Ordinal));
    }

    [Fact]
    public void Picker_IsALookupThatOpensOnFocus_WithTheHintNamingTheQuestionsLanguage()
    {
        var page = RenderAdd();

        var field = BaseTextField(page);

        field.MinChars.Should().Be(0);
        field.OpenOnFocus.Should().BeTrue();
        field.Required.Should().BeFalse();
        field.Clearable.Should().BeTrue("the clear button removes the link");
        page.Find("#question-base-text-description").TextContent.Should()
            .Contain($"Only texts in {QuestionText.LanguageName("en")}, the language of this question, are listed.");
        page.FindAll("section.app-section-card h2").Select(heading => heading.TextContent.Trim())
            .Should().ContainInOrder("Statement", "Base text", "Options");
    }

    // -- saving the choice (AC2 screen part, AC13) ------------------------------------------------------------------

    [Fact]
    public async Task Save_New_SendsTheChosenBaseTextId_AndTheAsideShowsItsTitle()
    {
        RenderProviders();
        Api.SeedBaseTexts(PacificText);
        var page = RenderAdd();
        await FillValidDraftAsync(page);
        await PickAsync(page, "question-base-text", new AppLookupOption(PacificText.Id, PacificText.Title));

        page.WaitForAssertion(() => page.Find("aside.app-form-aside").TextContent.Should().Contain(PacificText.Title));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        Api.LastSaved(HttpMethod.Post).BaseTextId.Should().Be(PacificText.Id);
        page.WaitForAssertion(() => Api.Questions.Single().BaseText!.Id.Should().Be(PacificText.Id));
        page.WaitForAssertion(() => BaseTextField(page).Value!.Id.Should().Be(PacificText.Id));
    }

    [Fact]
    public async Task Edit_RemovingTheLink_SendsNullAndTheQuestionHasNoBaseText()
    {
        RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");
        BaseTextField(page).Value!.Id.Should().Be(CommaText.Id);

        await PickAsync(page, "question-base-text", null);
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        Api.LastSaved(HttpMethod.Put).BaseTextId.Should().BeNull();
        page.WaitForAssertion(() => Api.Questions.Single().BaseText.Should().BeNull());
    }

    [Fact]
    public async Task Edit_ChangingTheLink_SendsTheNewId()
    {
        RenderProviders();
        Api.BaseTexts.Add(OtherPtPtText);
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");

        await PickAsync(page, "question-base-text", OptionOf(OtherPtPtText));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        Api.LastSaved(HttpMethod.Put).BaseTextId.Should().Be(OtherPtPtText.Id);
        page.WaitForAssertion(() => Api.Questions.Single().BaseText!.Title.Should().Be("Texto II - A crase"));
    }

    // -- unsaved changes (AC13) -------------------------------------------------------------------------------------

    [Fact]
    public async Task UnsavedChanges_ADifferentChoiceIsAChange_ComingBackToTheStoredOneIsNot()
    {
        Api.BaseTexts.Add(OtherPtPtText);
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");
        Actions(page).HasChanges.Should().BeFalse("what was loaded is what is stored");
        page.FindAll("#question-annul-reason-hint").Should().BeEmpty();

        await PickAsync(page, "question-base-text", OptionOf(OtherPtPtText));

        page.WaitForAssertion(() =>
        {
            Actions(page).HasChanges.Should().BeTrue("the chosen id differs from the stored one");
            page.Find("#question-annul-reason-hint").TextContent.Should().NotBeNullOrWhiteSpace("Annul and Publish say why they wait");
        });

        await PickAsync(page, "question-base-text", new AppLookupOption(CommaText.Id, CommaText.Title));

        page.WaitForAssertion(() => Actions(page).HasChanges.Should().BeFalse());
    }

    [Fact]
    public async Task UnsavedChanges_PickingATextOnAQuestionWithNone_IsAChange_RemovingItAgainIsNot_AndSavingClearsIt()
    {
        RenderProviders();
        Api.SeedBaseTexts(ClinicalCase);
        Api.Questions.Add(Draft);
        var page = RenderEdit(Draft.Id);
        WaitForEditors(page, "question-statement");
        Actions(page).HasChanges.Should().BeFalse();

        await PickAsync(page, "question-base-text", OptionOf(ClinicalCase));

        page.WaitForAssertion(() => Actions(page).HasChanges.Should().BeTrue());
        await PickAsync(page, "question-base-text", null);
        page.WaitForAssertion(() => Actions(page).HasChanges.Should().BeFalse());

        await PickAsync(page, "question-base-text", OptionOf(ClinicalCase));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Questions.Single().BaseText!.Id.Should().Be(ClinicalCase.Id));
        page.WaitForAssertion(() => Actions(page).HasChanges.Should().BeFalse());
    }

    // -- the chosen text: open, read, create ------------------------------------------------------------------------

    [Fact]
    public void Chosen_ShowsTheOpenLinkInANewTab_WithAnAccessibleNameThatSaysSo()
    {
        Api.Questions.Add(Published);

        var page = RenderEdit(Published.Id);

        page.WaitForAssertion(() => page.FindAll("#question-base-text-open").Should().ContainSingle());
        var open = page.Find("#question-base-text-open");
        open.GetAttribute("href").Should().Be($"/admin/base-texts/{CommaText.Id}");
        open.GetAttribute("target").Should().Be("_blank");
        open.GetAttribute("rel").Should().Contain("noopener");
        open.GetAttribute("aria-label").Should().Be("Open the base text (opens in a new tab)");
        open.TextContent.Trim().Should().Be("Open the base text");
    }

    [Fact]
    public void NoChoice_ShowsNeitherTheOpenLinkNorTheDisclosure()
    {
        Api.Questions.Add(Draft);

        var page = RenderEdit(Draft.Id);

        page.WaitForAssertion(() => page.FindAll("#question-base-text-create-link").Should().ContainSingle());
        page.FindAll("#question-base-text-open, #question-base-text-preview-button").Should().BeEmpty();
    }

    [Fact]
    public void Create_IsALinkToTheNewFormInANewTab_PresetToTheQuestionsLanguage()
    {
        Api.Questions.Add(Published);

        var page = RenderEdit(Published.Id);

        page.WaitForAssertion(() => page.FindAll("#question-base-text-create-link").Should().ContainSingle());
        var create = page.Find("#question-base-text-create-link");
        create.GetAttribute("href").Should().Be("/admin/base-texts/new?language=pt-PT");
        create.GetAttribute("target").Should().Be("_blank");
        create.GetAttribute("rel").Should().Contain("noopener");
        create.GetAttribute("aria-label").Should().Be("Create a base text (opens in a new tab)");
        page.Find("#question-base-text-create").TextContent.Should()
            .Contain("Not in the list?")
            .And.Contain("It opens in a new tab, so this question stays as it is.");
    }

    [Fact]
    public async Task Create_FollowsTheLanguageOfTheForm()
    {
        var page = RenderAdd();
        page.Find("#question-base-text-create-link").GetAttribute("href").Should().Be("/admin/base-texts/new?language=en");

        await SetLanguageAsync(page, "pt-BR");

        page.WaitForAssertion(() => page.Find("#question-base-text-create-link").GetAttribute("href")
            .Should().Be("/admin/base-texts/new?language=pt-BR"));
    }

    // The stored link carries its text: showing it asks the Api for nothing.
    [Fact]
    public void ShowTheText_OfTheStoredLink_UsesTheTextOfTheQuestionResponse_AndNoExtraCall()
    {
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        page.WaitForAssertion(() => page.FindAll("#question-base-text-preview-button").Should().ContainSingle());
        page.Find("#question-base-text-preview-button").GetAttribute("aria-expanded").Should().Be("false");

        page.Find("#question-base-text-preview-button").Click();

        page.WaitForAssertion(() => page.Find("#question-base-text-preview-html").InnerHtml.Should().Contain("Texto-base com"));
        page.Find("#question-base-text-preview-html").GetAttribute("lang").Should().Be("pt-PT");
        page.Find("#question-base-text-preview").TextContent.Should().Contain("Edited on its own page, for every question that uses it.");
        page.Find("#question-base-text-preview-button").GetAttribute("aria-expanded").Should().Be("true");
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Get && call.Path.EndsWith($"/base-texts/{CommaText.Id}", StringComparison.Ordinal));
    }

    // A text picked now is read once, the first time it is opened.
    [Fact]
    public async Task ShowTheText_OfANewPick_ReadsTheBaseTextOnce()
    {
        Api.SeedBaseTexts(PacificText);
        var page = RenderAdd();
        await PickAsync(page, "question-base-text", OptionOf(PacificText));
        page.WaitForAssertion(() => page.FindAll("#question-base-text-preview-button").Should().ContainSingle());
        Api.Received.Should().NotContain(call => call.Path.EndsWith($"/base-texts/{PacificText.Id}", StringComparison.Ordinal));

        page.Find("#question-base-text-preview-button").Click();

        page.WaitForAssertion(() => page.Find("#question-base-text-preview-html").InnerHtml.Should().Contain("gyre of plastic"));
        Api.Received.Count(call => call.Method == HttpMethod.Get && call.Path.EndsWith($"/base-texts/{PacificText.Id}", StringComparison.Ordinal))
            .Should().Be(1);
    }

    // -- the language change (BR3) ----------------------------------------------------------------------------------

    [Fact]
    public async Task LanguageChange_ClearsATextOfAnotherLanguage_AndTellsWhichOneAndWhy()
    {
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");

        await SetLanguageAsync(page, "en");

        page.WaitForAssertion(() =>
        {
            BaseTextField(page).Value.Should().BeNull("a question and its base text share one language");
            var note = page.Find("#question-base-text-cleared");
            note.TextContent.Should().Contain($"The base text \"{CommaText.Title}\" was removed")
                .And.Contain($"it is in {QuestionText.LanguageName("pt-PT")}")
                .And.Contain($"this question is now in {QuestionText.LanguageName("en")}.");
            note.QuerySelector(".app-alert")!.GetAttribute("role").Should().Be("status", "it is announced as a polite status");
            Actions(page).HasChanges.Should().BeTrue();
        });
        page.FindAll("#question-base-text-open").Should().BeEmpty();
    }

    [Fact]
    public async Task LanguageChange_KeepsATextWhenTheLanguageIsTheSame()
    {
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");

        await SetLanguageAsync(page, "pt-PT");

        BaseTextField(page).Value!.Id.Should().Be(CommaText.Id);
        page.FindAll("#question-base-text-cleared").Should().BeEmpty();
    }

    [Fact]
    public async Task ClearedNote_GoesAwayAtTheNextPick()
    {
        Api.SeedBaseTexts(PacificText);
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");
        await SetLanguageAsync(page, "en");
        page.WaitForAssertion(() => page.FindAll("#question-base-text-cleared").Should().ContainSingle());

        await PickAsync(page, "question-base-text", OptionOf(PacificText));

        page.WaitForAssertion(() => page.FindAll("#question-base-text-cleared").Should().BeEmpty());
    }

    // -- refusals on save -------------------------------------------------------------------------------------------

    // BR7: the text was deleted since it was picked; the choice goes and the error is on the field and the summary.
    [Fact]
    public async Task Save_BaseTextGoneMeanwhile_ClearsTheChoiceAndSaysWhy()
    {
        RenderProviders();
        Api.SeedBaseTexts(PacificText);
        var page = RenderAdd();
        await FillValidDraftAsync(page);
        await PickAsync(page, "question-base-text", OptionOf(PacificText));
        Api.SaveFailure = (HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionBaseTextNotFound, null);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            page.Find("#question-base-text-description .app-field-error-text").TextContent
                .Should().Be("This base text no longer exists. Pick another or leave the field empty.");
            BaseTextField(page).Value.Should().BeNull();
            page.Find("#question-error-summary").TextContent.Should().Contain("Base text");
        });
    }

    // BR3: the choice stays, so the curator sees which text was refused.
    [Fact]
    public async Task Save_LanguageMismatch_KeepsTheChoiceAndSaysWhy()
    {
        RenderProviders();
        Api.SeedBaseTexts(PacificText);
        var page = RenderAdd();
        await FillValidDraftAsync(page);
        await PickAsync(page, "question-base-text", OptionOf(PacificText));
        Api.SaveFailure = (HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionBaseTextLanguageMismatch, null);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            page.Find("#question-base-text-description .app-field-error-text").TextContent
                .Should().Be("The base text and the question must be in the same language.");
            BaseTextField(page).Value!.Id.Should().Be(PacificText.Id);
            page.Find("#question-error-summary").TextContent.Should().Contain("Base text");
        });
    }

    // -- an annulled question (F-101 BR5) ---------------------------------------------------------------------------

    [Fact]
    public void Annulled_DisablesThePicker_KeepsOpenAndShowTheText_AndHidesCreate()
    {
        var annulled = Published with
        {
            Status = QuestionStatus.Annulled,
            CurrentVersion = 2,
            Annulment = new QuestionAnnulmentResponse("A banca anulou o item.", new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero))
        };
        Api.Questions.Add(annulled);

        var page = RenderEdit(annulled.Id);

        page.WaitForAssertion(() => page.FindAll("#question-base-text-open").Should().ContainSingle());
        BaseTextField(page).Disabled.Should().BeTrue();
        page.FindAll("#question-base-text-preview-button").Should().ContainSingle("reading is allowed");
        page.FindAll("#question-base-text-create, #question-base-text-create-link").Should().BeEmpty();
    }

    // -- the preview and the version view (AC11) --------------------------------------------------------------------

    // AC11: the preview of the saved question shows the linked text before the statement.
    [Fact]
    public void Preview_ShowsTheLinkedBaseTextBeforeTheStatement()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        page.WaitForAssertion(() => page.FindAll("#question-preview").Should().ContainSingle());

        page.Find("#question-preview").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll("#question-preview-base-text").Should().ContainSingle());
        providers.Dialogs.Find("#question-preview-base-text").TextContent.Should().Contain("Texto-base com nota.");
        providers.Dialogs.Markup.IndexOf("question-preview-base-text", StringComparison.Ordinal)
            .Should().BeLessThan(providers.Dialogs.Markup.IndexOf("question-preview-statement", StringComparison.Ordinal));
    }

    [Fact]
    public void Preview_OfAQuestionWithNoBaseText_ShowsNone()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Draft);
        var page = RenderEdit(Draft.Id);
        page.WaitForAssertion(() => page.FindAll("#question-preview").Should().ContainSingle());

        page.Find("#question-preview").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll("#question-preview-statement").Should().ContainSingle());
        providers.Dialogs.FindAll("#question-preview-base-text").Should().BeEmpty();
    }

    // BR5: a version of the history shows the base text HTML it recorded, which carries no id.
    [Fact]
    public void VersionView_ShowsTheBaseTextHtmlTheVersionRecorded()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published with { CurrentVersion = 1 });
        Api.Versions[Published.Id] =
        [
            new QuestionVersionResponse(
                Guid.Parse("0198f5a0-0000-7000-8000-0000000007a1"),
                Published.Id,
                1,
                QuestionType.SingleChoice,
                "pt-PT",
                "<p>Enunciado da versão.</p>",
                "<p>Texto como era na versão 1.</p>",
                null,
                Published.Options,
                null,
                false,
                null,
                new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero),
                false)
        ];
        var page = RenderEdit(Published.Id);
        page.WaitForAssertion(() => page.FindAll("[id^='question-version-actions-1-']").Should().NotBeEmpty());

        page.Find("#question-version-actions-1-view").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll("#question-preview-base-text").Should().ContainSingle());
        providers.Dialogs.Find("#question-preview-base-text").TextContent.Should().Contain("Texto como era na versão 1.");
        providers.Dialogs.Markup.Should().NotContain("Texto-base com nota", "the version, not the linked text, is shown");
    }
}
