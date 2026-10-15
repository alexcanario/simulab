using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-99 on screen, as far as bUnit reaches: transcription mode of the question form (AC1 to AC10) and the
/// "Transcribe" button of the edition's Questions card. The rules are the Api's and have their own tests.
/// </summary>
public sealed class QuestionTranscriptionTests : QuestionPageTestContext
{
    private void SeedGuard(params QuestionResponse[] stored)
    {
        Api.Editions.Add(GuardEdition);
        Api.NoticeSubjects[GuardEdition.Id] = [Basic, Specific];
        foreach (var question in stored)
        {
            Api.Questions.Add(question);
        }
    }

    private IRenderedComponent<QuestionForm> RenderTranscribe(Guid editionId)
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/admin/questions/transcribe?examEditionId={editionId}");

        return Render<QuestionForm>();
    }

    private static AppTextField<int?> NumberField(IRenderedComponent<QuestionForm> page) =>
        page.FindComponents<AppTextField<int?>>().Single(component => component.Instance.Id == "question-paper-number").Instance;

    private static Task TypeNumberAsync(IRenderedComponent<QuestionForm> page, int? value) =>
        page.InvokeAsync(() => page.FindComponents<AppTextField<int?>>()
            .Single(component => component.Instance.Id == "question-paper-number").Instance.ValueChanged.InvokeAsync(value));

    private static Task PickNoticeSubjectAsync(IRenderedComponent<QuestionForm> page, Guid? id) =>
        page.InvokeAsync(() => page.FindComponents<AppSelectField<Guid?>>()
            .Single(component => component.Instance.Id == "question-notice-subject").Instance.ValueChanged.InvokeAsync(id));

    private static AppSelectField<Guid?> NoticeSubjectSelect(IRenderedComponent<QuestionForm> page) =>
        page.FindComponents<AppSelectField<Guid?>>().Single(component => component.Instance.Id == "question-notice-subject").Instance;

    private async Task FillAsync(IRenderedComponent<QuestionForm> page)
    {
        await page.InvokeAsync(() => page.FindComponents<AppLookupField>()
            .Single(component => component.Instance.Id == "question-subject").Instance.ValueChanged
            .InvokeAsync(new AppLookupOption(Portuguese.Id, Portuguese.Name)));
        await page.InvokeAsync(() => page.FindComponents<AppLookupField>()
            .Single(component => component.Instance.Id == "question-topic").Instance.ValueChanged
            .InvokeAsync(new AppLookupOption(Crase.Id, Crase.Name)));
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain("question-statement"));
        await page.InvokeAsync(() => RichText.TypeAsync("question-statement", "<p>Qual frase está certa?</p>"));
        for (var index = 0; index < 2; index++)
        {
            page.Find("#question-option-add").Click();
            var id = $"question-option-{index}";
            page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain(id));
            await page.InvokeAsync(() => RichText.TypeAsync(id, $"<p>Opção {index}</p>"));
        }
    }

    // AC1, AC7
    [Fact]
    public void Open_ShowsTheEditionLocked_AndThePaperNumberSuggested()
    {
        SeedGuard(FromGuard, FromGuardSecond);

        var page = RenderTranscribe(GuardEdition.Id);

        page.WaitForAssertion(() =>
        {
            page.Find("#question-transcribe-edition").TextContent.Should().Contain("CONSULPLAN · Guarda Municipal · 2024");
            page.Find("#question-transcribe-counter").TextContent.Should().Contain("Question 1 of this session");
            NumberField(page).Value.Should().Be(31);
            page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "question-edition")
                .Instance.Disabled.Should().BeTrue();
        });
    }

    // AC2: an edition with no questions starts at 1.
    [Fact]
    public void Open_OfAnEditionWithNoQuestions_SuggestsOne()
    {
        SeedGuard();

        var page = RenderTranscribe(GuardEdition.Id);

        page.WaitForAssertion(() => NumberField(page).Value.Should().Be(1));
    }

    // AC3, AC5: saved as the regular form saves it, then cleared except edition, notice subject and language.
    [Fact]
    public async Task Save_StoresTheQuestionAndClearsTheForm_KeepingTheEditionTheSubjectAndTheLanguage()
    {
        SeedGuard(FromGuard);
        var page = RenderTranscribe(GuardEdition.Id);
        page.WaitForAssertion(() => NumberField(page).Value.Should().Be(13));
        await FillAsync(page);
        await PickNoticeSubjectAsync(page, Specific.Id);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            var sent = Api.LastSaved(HttpMethod.Post);
            sent.ExamEditionId.Should().Be(GuardEdition.Id);
            sent.PaperNumber.Should().Be(13);
            sent.NoticeSubjectId.Should().Be(Specific.Id);
            page.Find("#question-transcribe-counter").TextContent.Should().Contain("Question 2 of this session");
            page.Markup.Should().Contain("Question saved. Keep going.");
            NumberField(page).Value.Should().Be(14);
            NoticeSubjectSelect(page).Value.Should().Be(Specific.Id);
            page.FindAll("#question-option-0").Should().BeEmpty();
            page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "question-topic")
                .Instance.Value.Should().BeNull();
        });
        Navigation().Uri.Should().Contain("/admin/questions/transcribe");
    }

    // AC4: a refused number keeps what was typed and marks the field.
    [Fact]
    public async Task Save_RefusedAsPaperNumberTaken_KeepsWhatWasTyped()
    {
        SeedGuard();
        var page = RenderTranscribe(GuardEdition.Id);
        page.WaitForAssertion(() => NumberField(page).Value.Should().Be(1));
        await FillAsync(page);
        await TypeNumberAsync(page, 5);
        Api.SaveFailure = (HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionPaperNumberTaken, null);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            page.Find("#question-paper-number-description .app-field-error-text").TextContent
                .Should().Be("Number already used in this edition.");
            NumberField(page).Value.Should().Be(5);
            page.Find("#question-transcribe-counter").TextContent.Should().Contain("Question 1 of this session");
        });
    }

    // AC6: only the two types the form implements.
    [Fact]
    public void Open_OffersOnlySingleChoiceAndTrueFalse()
    {
        SeedGuard();

        var page = RenderTranscribe(GuardEdition.Id);

        page.WaitForAssertion(() =>
        {
            page.FindAll("#question-type-SingleChoice").Should().ContainSingle();
            page.FindAll("#question-type-TrueFalse").Should().ContainSingle();
            page.Find("#question-type").QuerySelectorAll("[role='radio']").Should().HaveCount(2);
        });
    }

    // AC9: the counter lives in the browser; a new session starts at 1 over what is already stored.
    [Fact]
    public void Reopen_RestartsTheCounter_AndSuggestsFromWhatIsStored()
    {
        SeedGuard(FromGuard, FromGuardSecond);

        var page = RenderTranscribe(GuardEdition.Id);

        page.WaitForAssertion(() =>
        {
            page.Find("#question-transcribe-counter").TextContent.Should().Contain("Question 1 of this session");
            NumberField(page).Value.Should().Be(31);
        });
        Api.Questions.Should().HaveCount(2);
    }

    // AC10
    [Fact]
    public void Open_OfAnUnknownEdition_ShowsTheNotFoundStateWithFinishOnly()
    {
        var page = RenderTranscribe(Guid.CreateVersion7());

        page.WaitForAssertion(() =>
        {
            page.FindAll("#question-transcribe-edition-missing").Should().ContainSingle();
            page.Markup.Should().Contain("The edition no longer exists.");
            page.FindAll(".app-form-save").Should().BeEmpty();
        });
    }

    [Fact]
    public void Finish_GoesToTheQuestionsOfTheEdition()
    {
        SeedGuard();
        var page = RenderTranscribe(GuardEdition.Id);
        page.WaitForAssertion(() => page.FindAll("#question-transcribe-finish").Should().ContainSingle());

        page.Find("#question-transcribe-finish").Click();

        page.WaitForAssertion(() => Navigation().Uri.Should().EndWith($"/admin/questions?edition={GuardEdition.Id}"));
    }

    // AC1: the entry point on the edition's Questions card.
    [Fact]
    public void Section_Transcribe_OpensTheTranscriptionScreenWithTheEditionChosen()
    {
        SeedGuard();
        var section = Render<EditionQuestionsSection>(parameters => parameters.Add(item => item.EditionId, GuardEdition.Id));
        section.WaitForAssertion(() => section.FindAll("#edition-question-transcribe").Should().ContainSingle());

        section.Find("#edition-question-transcribe").Click();

        section.WaitForAssertion(() => Navigation().Uri
            .Should().EndWith($"/admin/questions/transcribe?examEditionId={GuardEdition.Id}"));
    }

    // AC1: an edition not saved yet has no entry point.
    [Fact]
    public void Section_OfAnUnsavedEdition_HasNoTranscribeButton()
    {
        var section = Render<EditionQuestionsSection>(parameters => parameters.Add(item => item.EditionId, (Guid?)null));

        section.FindAll("#edition-question-transcribe").Should().BeEmpty();
    }

    private NavigationManager Navigation() => Services.GetRequiredService<NavigationManager>();
}
