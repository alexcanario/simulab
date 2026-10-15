using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-102 AC12 on the transcription screen: the base text chosen for one question stays chosen for the next, while
/// type, statement, difficulty and key are blank; a language change clears it with a message (BR13).
/// </summary>
public sealed class QuestionTranscriptionBaseTextTests : QuestionPageTestContext
{
    private void Seed()
    {
        Api.Editions.Add(GuardEdition);
        Api.NoticeSubjects[GuardEdition.Id] = [Basic, Specific];
        Api.SeedBaseTexts(PacificText, ClinicalCase);
    }

    private IRenderedComponent<QuestionForm> RenderTranscribe()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/admin/questions/transcribe?examEditionId={GuardEdition.Id}");

        return Render<QuestionForm>();
    }

    private static AppLookupField BaseTextField(IRenderedComponent<QuestionForm> page) =>
        page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "question-base-text").Instance;

    private static Task PickAsync(IRenderedComponent<QuestionForm> page, string id, AppLookupOption? option) =>
        page.InvokeAsync(() => page.FindComponents<AppLookupField>()
            .Single(component => component.Instance.Id == id).Instance.ValueChanged.InvokeAsync(option));

    private static Task SetLanguageAsync(IRenderedComponent<QuestionForm> page, string language) =>
        page.InvokeAsync(() => page.FindComponents<AppSelectField<string>>()
            .Single(component => component.Instance.Id == "question-content-language").Instance.ValueChanged.InvokeAsync(language));

    private async Task FillAsync(IRenderedComponent<QuestionForm> page)
    {
        await PickAsync(page, "question-subject", new AppLookupOption(Portuguese.Id, Portuguese.Name));
        await PickAsync(page, "question-topic", new AppLookupOption(Crase.Id, Crase.Name));
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain("question-statement"));
        await page.InvokeAsync(() => RichText.TypeAsync("question-statement", "<p>Qual frase está certa?</p>"));
        page.Find("#question-option-add").Click();
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain("question-option-0"));
        await page.InvokeAsync(() => RichText.TypeAsync("question-option-0", "<p>Opção A</p>"));
    }

    // AC12: the chosen text is saved with the question and is still chosen for the next one.
    [Fact]
    public async Task Save_KeepsTheChosenBaseText_WhileTheOtherFieldsAreBlank()
    {
        Seed();
        var page = RenderTranscribe();
        page.WaitForAssertion(() => page.FindAll("#question-transcribe-counter").Should().ContainSingle());
        await FillAsync(page);
        await PickAsync(page, "question-base-text", new AppLookupOption(PacificText.Id, PacificText.Title));

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            Api.LastSaved(HttpMethod.Post).BaseTextId.Should().Be(PacificText.Id);
            page.Find("#question-transcribe-counter").TextContent.Should().Contain("Question 2 of this session");
            BaseTextField(page).Value!.Id.Should().Be(PacificText.Id, "the base text stays chosen for the next question");
            page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "question-topic")
                .Instance.Value.Should().BeNull();
            page.FindAll("#question-option-0").Should().BeEmpty();
        });
        page.Find("#question-base-text-description").TextContent.Should()
            .Contain("Stays chosen for the next question. Change or remove it when the block of questions ends.");
    }

    [Fact]
    public async Task Save_TheNextQuestionIsSentWithTheSameBaseText()
    {
        Seed();
        var page = RenderTranscribe();
        page.WaitForAssertion(() => page.FindAll("#question-transcribe-counter").Should().ContainSingle());
        await FillAsync(page);
        await PickAsync(page, "question-base-text", new AppLookupOption(PacificText.Id, PacificText.Title));
        page.Find(".app-form-save").Click();
        page.WaitForAssertion(() => page.Find("#question-transcribe-counter").TextContent.Should().Contain("Question 2"));

        await FillAsync(page);
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            Api.Questions.Should().HaveCount(2);
            Api.Questions.Should().OnlyContain(question => question.BaseText!.Id == PacificText.Id);
        });
    }

    // AC12: the kept choice is not an unsaved change, so Finish does not ask to discard a blank question.
    [Fact]
    public async Task KeptBaseText_IsNotAnUnsavedChange()
    {
        Seed();
        var page = RenderTranscribe();
        page.WaitForAssertion(() => page.FindAll("#question-transcribe-counter").Should().ContainSingle());

        await PickAsync(page, "question-base-text", new AppLookupOption(PacificText.Id, PacificText.Title));

        page.WaitForAssertion(() => BaseTextField(page).Value.Should().NotBeNull());
        page.FindComponent<AppFormActions>().Instance.HasChanges.Should().BeFalse();
    }

    // AC12: a language the text does not have clears the choice with a message.
    [Fact]
    public async Task LanguageChange_ClearsTheChoiceWithTheMessage()
    {
        Seed();
        var page = RenderTranscribe();
        page.WaitForAssertion(() => page.FindAll("#question-transcribe-counter").Should().ContainSingle());
        await PickAsync(page, "question-base-text", new AppLookupOption(PacificText.Id, PacificText.Title));

        await SetLanguageAsync(page, "pt-BR");

        page.WaitForAssertion(() =>
        {
            BaseTextField(page).Value.Should().BeNull();
            page.Find("#question-base-text-cleared").TextContent.Should()
                .Contain($"The base text \"{PacificText.Title}\" was removed")
                .And.Contain($"it is in {QuestionText.LanguageName("en")}")
                .And.Contain($"this question is now in {QuestionText.LanguageName("pt-BR")}.");
        });
    }

    // The picker of the transcription screen offers the texts of the form's language only.
    [Fact]
    public async Task Picker_OffersOnlyTheTextsOfTheLanguageOfTheTranscription()
    {
        Seed();
        var page = RenderTranscribe();
        page.WaitForAssertion(() => page.FindAll("#question-transcribe-counter").Should().ContainSingle());

        var offered = await BaseTextField(page).SearchAsync(string.Empty, CancellationToken.None);

        offered.Select(option => option.Text).Should().Equal(PacificText.Title);
    }
}
