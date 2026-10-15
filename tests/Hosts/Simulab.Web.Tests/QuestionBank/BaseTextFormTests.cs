using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-102 `/admin/base-texts/new` and `/admin/base-texts/{id}`: the base text form, its linked questions and its
/// delete card (AC9 screen part, and the create, edit, lock, versions notice and refusals of the screen design).
/// The rules themselves are the Api's and have their own tests.
/// </summary>
public sealed class BaseTextFormTests : QuestionPageTestContext
{
    private IRenderedComponent<BaseTextForm> RenderAdd() => Render<BaseTextForm>();

    private IRenderedComponent<BaseTextForm> RenderEdit(Guid id) =>
        Render<BaseTextForm>(parameters => parameters.Add(form => form.Id, id));

    private NavigationManager Navigation() => Services.GetRequiredService<NavigationManager>();

    private void WaitForEditor(IRenderedComponent<BaseTextForm> page) =>
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain("base-text-text"));

    private static Task SetTitleAsync(IRenderedComponent<BaseTextForm> page, string value) =>
        page.InvokeAsync(() => page.FindComponents<AppTextField<string>>()
            .Single(component => component.Instance.Id == "base-text-title").Instance.ValueChanged.InvokeAsync(value));

    private static Task SetLanguageAsync(IRenderedComponent<BaseTextForm> page, string value) =>
        page.InvokeAsync(() => page.FindComponents<AppSelectField<string>>()
            .Single(component => component.Instance.Id == "base-text-content-language").Instance.ValueChanged.InvokeAsync(value));

    private static AppSelectField<string> LanguageField(IRenderedComponent<BaseTextForm> page) =>
        page.FindComponents<AppSelectField<string>>().Single(component => component.Instance.Id == "base-text-content-language").Instance;

    private static string ErrorOf(IRenderedComponent<BaseTextForm> page, string fieldId) =>
        page.Find($"#{fieldId}-description .app-field-error-text").TextContent;

    // -- new --------------------------------------------------------------------------------------------------------

    [Fact]
    public void Add_RendersTheTextSectionWithItsThreeFields_AndNoLinkedQuestionsNorDeleteCard()
    {
        var page = RenderAdd();
        WaitForEditor(page);

        page.Find("h1").TextContent.Should().Be("Add base text");
        page.Find(".app-breadcrumbs").TextContent.Should().Contain("Content").And.Contain("Base texts").And.Contain("Add base text");
        page.FindAll("section.app-section-card h2").Select(heading => heading.TextContent.Trim())
            .Should().Equal("Base text");
        page.FindAll("#base-text-title").Should().ContainSingle();
        page.FindAll("#base-text-content-language").Should().ContainSingle();
        RichText.Editors["base-text-text"].Required.Should().BeTrue();
        page.FindAll("#base-text-delete, #base-text-section-questions").Should().BeEmpty();
        page.FindAll(".app-form-save").Should().ContainSingle("Save is the page's one primary button");
        page.FindAll("#base-text-versions-notice").Should().BeEmpty();
    }

    [Fact]
    public void Add_StartsWithTheUiLanguage()
    {
        var page = RenderAdd();

        LanguageField(page).Value.Should().Be(QuestionText.DefaultContentLanguage());
    }

    // The question form's "Create a base text" link passes the question's language.
    [Fact]
    public void Add_WithALanguageInTheAddress_PreselectsIt()
    {
        Navigation().NavigateTo("/admin/base-texts/new?language=pt-PT");

        var page = RenderAdd();

        page.WaitForAssertion(() => LanguageField(page).Value.Should().Be("pt-PT"));
        WaitForEditor(page);
        RichText.Editors["base-text-text"].Lang.Should().Be("pt-PT", "the editor is marked with the chosen language");
    }

    [Fact]
    public void Add_WithAnUnknownLanguageInTheAddress_FallsBackToTheDefault()
    {
        Navigation().NavigateTo("/admin/base-texts/new?language=xx");

        var page = RenderAdd();

        LanguageField(page).Value.Should().Be(QuestionText.DefaultContentLanguage());
    }

    [Fact]
    public async Task Save_New_SendsTitleLanguageAndText_AndOpensTheSavedBaseText()
    {
        var providers = RenderProviders();
        Api.SeedBaseTexts();
        var page = RenderAdd();
        WaitForEditor(page);
        await SetTitleAsync(page, "  Texto I - A barragem  ");
        await SetLanguageAsync(page, "pt-BR");
        await page.InvokeAsync(() => RichText.TypeAsync("base-text-text", "<p>Era uma vez uma barragem.</p><script>x()</script>"));

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        var sent = Api.LastSavedBaseText(HttpMethod.Post);
        sent.Title.Should().Be("Texto I - A barragem", "the title is sent trimmed");
        sent.ContentLanguage.Should().Be("pt-BR");
        sent.TextHtml.Should().Contain("Era uma vez");
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Base text saved."));
        var created = Api.BaseTexts.Single();
        page.WaitForAssertion(() => Navigation().Uri.Should().EndWith($"/admin/base-texts/{created.Id}"));
        page.Find("h1").TextContent.Should().Be("Edit base text");
    }

    // BR8: what is stored replaces what the editor held.
    [Fact]
    public async Task Save_TheEditorShowsTheSanitizedTextTheApiStored()
    {
        RenderProviders();
        Api.SeedBaseTexts();
        var page = RenderAdd();
        WaitForEditor(page);
        await SetTitleAsync(page, "Texto");
        await page.InvokeAsync(() => RichText.TypeAsync("base-text-text", "<p>Ok</p><script>alert(1)</script>"));

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => RichText.Pushed.Should().Contain(("base-text-text", "<p>Ok</p>")));
        RichText.Editors["base-text-text"].Html.Should().Be("<p>Ok</p>");
    }

    // The client part of AC1: nothing is sent, and the summary names the fields.
    [Fact]
    public void Save_BlankTitleAndText_ShowsTheMessagesAndTheSummaryAndSendsNothing()
    {
        var page = RenderAdd();
        WaitForEditor(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            ErrorOf(page, "base-text-title").Should().Be("Write the title.");
            ErrorOf(page, "base-text-text").Should().Be("Write the text.");
            page.Find("#base-text-error-summary").TextContent.Should().Contain("Title").And.Contain("Text");
        });
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task Save_TextOfOnlyEmptyTags_CountsAsEmpty()
    {
        var page = RenderAdd();
        WaitForEditor(page);
        await SetTitleAsync(page, "Texto");
        await page.InvokeAsync(() => RichText.TypeAsync("base-text-text", "<p><br></p>"));

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => ErrorOf(page, "base-text-text").Should().Be("Write the text."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Theory]
    [InlineData(QuestionBankErrorCodes.BaseTextTitleTooLong, "base-text-title", "The title has more than 200 characters.")]
    [InlineData(QuestionBankErrorCodes.BaseTextTextTooLong, "base-text-text", "The text has more than 20,000 characters.")]
    [InlineData(QuestionBankErrorCodes.BaseTextContentLanguageInvalid, "base-text-content-language", "Choose one of the listed languages.")]
    [InlineData(QuestionBankErrorCodes.BaseTextLanguageInUse, "base-text-content-language", "Questions use this base text, so its language cannot change.")]
    public async Task Save_RefusedByCode_ShowsTheTextOnTheFieldThatCausedItAndInTheSummary(string code, string field, string text)
    {
        RenderProviders();
        Api.BaseTextSaveFailure = (HttpStatusCode.BadRequest, code);
        var page = RenderAdd();
        WaitForEditor(page);
        await SetTitleAsync(page, "Texto");
        await page.InvokeAsync(() => RichText.TypeAsync("base-text-text", "<p>Corpo</p>"));

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find($"#{field}-description").TextContent.Should().Contain(text));
        page.Find("#base-text-error-summary").Should().NotBeNull();
    }

    [Fact]
    public async Task Save_UnexpectedFailure_ShowsTheGenericAlertAndNoFieldError()
    {
        RenderProviders();
        Api.BaseTextSaveFailure = (HttpStatusCode.InternalServerError, "common.unexpected_error");
        var page = RenderAdd();
        WaitForEditor(page);
        await SetTitleAsync(page, "Texto");
        await page.InvokeAsync(() => RichText.TypeAsync("base-text-text", "<p>Corpo</p>"));

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find(".app-alert").TextContent.Should().Contain("Something went wrong"));
        page.FindAll(".app-field-error-text").Should().BeEmpty();
    }

    // -- edit -------------------------------------------------------------------------------------------------------

    [Fact]
    public void Edit_OpensFilledWithTheStoredBaseText()
    {
        Api.SeedBaseTexts(UnusedTariffs);

        var page = RenderEdit(UnusedTariffs.Id);

        WaitForEditor(page);
        page.Find("h1").TextContent.Should().Be("Edit base text");
        page.FindComponents<AppTextField<string>>().Single(c => c.Instance.Id == "base-text-title").Instance.Value
            .Should().Be(UnusedTariffs.Title);
        LanguageField(page).Value.Should().Be("pt-BR");
        RichText.Editors["base-text-text"].Html.Should().Be(UnusedTariffs.TextHtml);
        RichText.Editors["base-text-text"].Lang.Should().Be("pt-BR");
    }

    [Fact]
    public void Edit_UnknownBaseText_ShowsTheNotFoundAlertAndTheWayBack()
    {
        var page = RenderEdit(Guid.NewGuid());

        page.WaitForAssertion(() => page.Markup.Should().Contain("This base text no longer exists."));
        page.Find("a.app-link").GetAttribute("href").Should().Be("/admin/base-texts");
        page.Markup.Should().Contain("Back to the base texts");
        page.FindAll(".app-form-save").Should().BeEmpty();
    }

    [Fact]
    public void Edit_LoadFails_ShowsTheErrorStateAndRetryLoadsTheText()
    {
        Api.SeedBaseTexts(UnusedTariffs);
        Api.BaseTextFindFailure = (HttpStatusCode.InternalServerError, "common.unexpected_error");
        var page = RenderEdit(UnusedTariffs.Id);
        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));

        Api.BaseTextFindFailure = null;
        page.Find(".app-retry").Click();

        WaitForEditor(page);
        RichText.Editors["base-text-text"].Html.Should().Be(UnusedTariffs.TextHtml);
    }

    // BR3: a text that questions use keeps its language; the field is disabled with the reason and the count.
    [Fact]
    public void Edit_InUse_LocksTheLanguage_ShowsTheVersionsNotice_AndDisablesDeleteWithItsReason()
    {
        Api.SeedBaseTexts(ClinicalCase);

        var page = RenderEdit(ClinicalCase.Id);

        page.WaitForAssertion(() => page.FindAll("#base-text-delete").Should().ContainSingle());
        LanguageField(page).Disabled.Should().BeTrue();
        page.Find("#base-text-content-language-description").TextContent.Should()
            .Contain("Locked while questions use this text (3).");
        page.Find("#base-text-versions-notice").TextContent.Should()
            .Contain("Saving a change to the text records a new version of each published question that uses it");
        page.Find("#base-text-delete").HasAttribute("disabled").Should().BeTrue();
        page.Find("#base-text-delete").GetAttribute("aria-describedby").Should().Be("base-text-delete-reason");
        page.Find("#base-text-delete-reason").TextContent.Should()
            .Contain("Questions use this text (3). Remove their links before deleting it.");
    }

    // The text with no question: the language is free, there is no notice and Delete is enabled.
    [Fact]
    public void Edit_NotInUse_LeavesTheLanguageFree_NoNotice_AndDeleteEnabled()
    {
        Api.SeedBaseTexts(UnusedTariffs);

        var page = RenderEdit(UnusedTariffs.Id);

        page.WaitForAssertion(() => page.FindAll("#base-text-delete").Should().ContainSingle());
        LanguageField(page).Disabled.Should().BeFalse();
        page.Find("#base-text-content-language-description").TextContent.Should()
            .Contain("The language the text is written in.");
        page.FindAll("#base-text-versions-notice").Should().BeEmpty();
        page.Find("#base-text-delete").HasAttribute("disabled").Should().BeFalse();
        page.FindAll("#base-text-delete-reason").Should().BeEmpty();
    }

    [Fact]
    public async Task Edit_Save_SendsAPutAndShowsTheStoredText()
    {
        var providers = RenderProviders();
        Api.SeedBaseTexts(UnusedTariffs);
        var page = RenderEdit(UnusedTariffs.Id);
        WaitForEditor(page);

        await page.InvokeAsync(() => RichText.TypeAsync("base-text-text", "<p>Tabela revista</p>"));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Put && call.Path == $"/api/v1/question-bank/base-texts/{UnusedTariffs.Id}"));
        Api.LastSavedBaseText(HttpMethod.Put).TextHtml.Should().Be("<p>Tabela revista</p>");
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Base text saved."));
        Api.BaseTexts.Single().TextHtml.Should().Be("<p>Tabela revista</p>");
    }

    // -- the linked questions (AC9) ---------------------------------------------------------------------------------

    // AC9: the questions that use the text, with the excerpt, the source with the paper number, and the status.
    [Fact]
    public void LinkedQuestions_ListExcerptSourceAndStatus_AndEachOpensTheQuestion()
    {
        Api.Editions.Add(GuardEdition);
        var linked = new QuestionBaseTextResponse(ClinicalCase.Id, ClinicalCase.Title, ClinicalCase.TextHtml);
        var first = FromGuard with { BaseText = linked, ContentLanguage = "pt-BR" };
        var second = Draft with { BaseText = linked, ContentLanguage = "pt-BR", StatementHtml = "<p>Segunda questão do caso.</p>" };
        Api.SeedBaseTexts(ClinicalCase);
        Api.Questions.AddRange([first, second]);

        var page = RenderEdit(ClinicalCase.Id);

        page.WaitForAssertion(() => page.FindAll("section tbody tr").Should().HaveCount(2));
        var rows = page.FindAll("section tbody tr");
        var fromGuard = rows.Single(row => row.TextContent.Contains("Fiscal de posturas", StringComparison.Ordinal));
        fromGuard.TextContent.Should().Contain("CONSULPLAN").And.Contain("Question 12");
        var authored = rows.Single(row => row.TextContent.Contains("Segunda questão", StringComparison.Ordinal));
        authored.TextContent.Should().Contain("Authored");
        page.Find("#base-text-section-questions-title").TextContent.Should().Contain("Questions that use this text (3)");

        page.Find($"#base-text-question-actions-{first.Id}-edit").Click();

        page.WaitForAssertion(() => Navigation().Uri.Should().EndWith($"/admin/questions/{first.Id}"));
    }

    [Fact]
    public void LinkedQuestions_None_ShowsTheGuidanceWithNoButton()
    {
        Api.SeedBaseTexts(UnusedTariffs);

        var page = RenderEdit(UnusedTariffs.Id);

        page.WaitForAssertion(() => page.Markup.Should().Contain("No question uses this text yet."));
        page.FindAll(".app-empty-action").Should().BeEmpty("the empty list has guidance and no button (open point 4)");
    }

    [Fact]
    public async Task LinkedQuestions_LoadFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.SeedBaseTexts(ClinicalCase);
        var page = RenderEdit(ClinicalCase.Id);
        page.WaitForAssertion(() => page.FindAll("#base-text-delete").Should().ContainSingle());
        Api.BaseTextFindFailure = (HttpStatusCode.InternalServerError, "common.unexpected_error");

        var table = page.FindComponent<AppDataTable<QuestionListItemResponse>>();
        await page.InvokeAsync(() => table.Instance.ReloadAsync());

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
        page.Markup.Should().Contain("Try again");
    }

    // -- delete (BR6) -----------------------------------------------------------------------------------------------

    [Fact]
    public void Delete_Confirmed_SendsTheDelete_ShowsTheSnackbar_AndGoesToTheListWithoutTheDiscardQuestion()
    {
        var providers = RenderProviders();
        Api.SeedBaseTexts(UnusedTariffs);
        var page = RenderEdit(UnusedTariffs.Id);
        page.WaitForAssertion(() => page.FindAll("#base-text-delete").Should().ContainSingle());

        page.Find("#base-text-delete").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("No question uses it."));
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Delete && call.Path == $"/api/v1/question-bank/base-texts/{UnusedTariffs.Id}"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Base text deleted."));
        page.WaitForAssertion(() => Navigation().Uri.Should().EndWith("/admin/base-texts"));
    }

    [Fact]
    public void Delete_Cancelled_SendsNothing()
    {
        var providers = RenderProviders();
        Api.SeedBaseTexts(UnusedTariffs);
        var page = RenderEdit(UnusedTariffs.Id);
        page.WaitForAssertion(() => page.FindAll("#base-text-delete").Should().ContainSingle());

        page.Find("#base-text-delete").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-cancel").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-cancel").Click();

        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Delete);
    }

    // The race: a question linked the text meanwhile. The alert says why and the page shows the lock again.
    [Fact]
    public void Delete_RefusedBecauseAQuestionLinkedItMeanwhile_ShowsTheAlertAndReloadsTheLock()
    {
        var providers = RenderProviders();
        Api.SeedBaseTexts(UnusedTariffs);
        var page = RenderEdit(UnusedTariffs.Id);
        page.WaitForAssertion(() => page.FindAll("#base-text-delete").Should().ContainSingle());
        Api.BaseTextDeleteFailure = (HttpStatusCode.Conflict, QuestionBankErrorCodes.BaseTextInUse);
        Api.BaseTexts[0] = UnusedTariffs with { QuestionCount = 2 };

        page.Find("#base-text-delete").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-ok").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() =>
        {
            page.Find(".app-alert").TextContent.Should()
                .Contain("Questions still use this base text. Remove their links before deleting it.");
            page.Find("#base-text-delete").HasAttribute("disabled").Should().BeTrue();
            LanguageField(page).Disabled.Should().BeTrue();
        });
    }

    // -- unsaved changes --------------------------------------------------------------------------------------------

    [Fact]
    public async Task UnsavedChanges_AreFlaggedWhileTheFormDiffersFromTheStoredText_AndClearedAfterSaving()
    {
        RenderProviders();
        Api.SeedBaseTexts(UnusedTariffs);
        var page = RenderEdit(UnusedTariffs.Id);
        WaitForEditor(page);
        var actions = () => page.FindComponent<AppFormActions>().Instance;
        actions().HasChanges.Should().BeFalse();

        await SetTitleAsync(page, "Outro título");

        page.WaitForAssertion(() => actions().HasChanges.Should().BeTrue());
        page.Find(".app-form-save").Click();
        page.WaitForAssertion(() => actions().HasChanges.Should().BeFalse());
    }
}
