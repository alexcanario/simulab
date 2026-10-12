using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-96 on screen, as far as bUnit reaches: the "Origin" section of the question form (AC14), the source line in
/// the aside, the list and the preview (AC15), and the "Questions" section of the edition page (AC16).
/// </summary>
public sealed class QuestionOriginTests : QuestionPageTestContext
{
    private IRenderedComponent<QuestionForm> RenderAdd() => Render<QuestionForm>();

    private IRenderedComponent<QuestionForm> RenderEdit(Guid id) =>
        Render<QuestionForm>(parameters => parameters.Add(form => form.Id, id));

    private static Task PickEditionAsync(IRenderedComponent<QuestionForm> page, AppLookupOption? option) =>
        page.InvokeAsync(() => page.FindComponents<AppLookupField>()
            .Single(component => component.Instance.Id == "question-edition").Instance.ValueChanged.InvokeAsync(option));

    private static Task PickNoticeSubjectAsync(IRenderedComponent<QuestionForm> page, Guid? id) =>
        page.InvokeAsync(() => page.FindComponents<AppSelectField<Guid?>>()
            .Single(component => component.Instance.Id == "question-notice-subject").Instance.ValueChanged.InvokeAsync(id));

    private static Task TypeNumberAsync(IRenderedComponent<QuestionForm> page, int? value) =>
        page.InvokeAsync(() => page.FindComponents<AppTextField<int?>>()
            .Single(component => component.Instance.Id == "question-paper-number").Instance.ValueChanged.InvokeAsync(value));

    private static AppLookupOption OptionOf(ExamEditionLookupResponse edition) =>
        new(edition.Id, $"{edition.OrganizerAcronym} · {edition.ExamName} · {edition.NoticeYear}", edition.Position);

    private static AppSelectField<Guid?> NoticeSubjectSelect(IRenderedComponent<QuestionForm> page) =>
        page.FindComponents<AppSelectField<Guid?>>().Single(component => component.Instance.Id == "question-notice-subject").Instance;

    private static AppTextField<int?> NumberField(IRenderedComponent<QuestionForm> page) =>
        page.FindComponents<AppTextField<int?>>().Single(component => component.Instance.Id == "question-paper-number").Instance;

    private void SeedGuard()
    {
        Api.Editions.Add(GuardEdition);
        Api.Editions.Add(OtherEdition);
        Api.NoticeSubjects[GuardEdition.Id] = [Basic, Specific];
        Api.NoticeSubjects[OtherEdition.Id] = [OtherSubject];
    }

    // -- AC14: the form -----------------------------------------------------------------------------------------

    [Fact]
    public void Add_TheOriginSectionHasTheThreeFields_AndTheNumberAndSubjectWaitForAnEdition()
    {
        var page = RenderAdd();

        page.Find("#question-section-origin-title").TextContent.Should().Be("Origin");
        page.FindAll("#question-edition").Should().NotBeEmpty();
        NumberField(page).Disabled.Should().BeTrue();
        NoticeSubjectSelect(page).Disabled.Should().BeTrue();
        page.Find("#question-paper-number-description").TextContent.Should().Contain("Choose an edition first.");
    }

    [Fact]
    public async Task PickingAnEdition_ListsItsNoticeSubjects_AndChangingItClearsTheSubjectAndTheNumber()
    {
        SeedGuard();
        var page = RenderAdd();

        await PickEditionAsync(page, OptionOf(GuardEdition));
        page.WaitForAssertion(() => NoticeSubjectSelect(page).Options.Select(option => option.Text)
            .Should().Equal("None", "Basic knowledge · Portuguese", "Specific knowledge · Constitutional law"));
        await TypeNumberAsync(page, 12);
        await PickNoticeSubjectAsync(page, Basic.Id);
        NumberField(page).Value.Should().Be(12);
        NoticeSubjectSelect(page).Value.Should().Be(Basic.Id);

        await PickEditionAsync(page, OptionOf(OtherEdition));

        page.WaitForAssertion(() => NoticeSubjectSelect(page).Options.Select(option => option.Text)
            .Should().Equal("None", "Basic knowledge · History"));
        NumberField(page).Value.Should().BeNull();
        NoticeSubjectSelect(page).Value.Should().BeNull();
    }

    [Fact]
    public async Task ClearingTheEdition_ClearsTheNumberAndTheSubject_AndDisablesThemAgain()
    {
        SeedGuard();
        var page = RenderAdd();
        await PickEditionAsync(page, OptionOf(GuardEdition));
        await TypeNumberAsync(page, 7);

        await PickEditionAsync(page, null);

        page.WaitForAssertion(() =>
        {
            NumberField(page).Value.Should().BeNull();
            NumberField(page).Disabled.Should().BeTrue();
            NoticeSubjectSelect(page).Disabled.Should().BeTrue();
        });
    }

    [Fact]
    public async Task EditionAlreadyChosen_WithNoNumber_IsRefusedByTheFormBeforeAnyCall()
    {
        SeedGuard();
        var page = RenderAdd();
        await PickEditionAsync(page, OptionOf(GuardEdition));

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
            page.Find("#question-paper-number-description .app-field-error-text").TextContent
                .Should().Contain("Enter the number of the question in the paper."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Fact]
    public void Edit_OfAQuestionFromAnEdition_ShowsItsOriginAndTheSourceLineInTheAside()
    {
        SeedGuard();
        Api.Questions.Add(FromGuard);

        var page = RenderEdit(FromGuard.Id);

        page.WaitForAssertion(() => NumberField(page).Value.Should().Be(12));
        page.Markup.Should().Contain("CONSULPLAN · Guarda Municipal · 2024");
        page.WaitForAssertion(() => NoticeSubjectSelect(page).Value.Should().Be(Basic.Id));
    }

    [Fact]
    public void Edit_OfAnAuthoredQuestion_SaysAuthoredInTheAside()
    {
        Api.Questions.Add(Draft);

        var page = RenderEdit(Draft.Id);

        page.WaitForAssertion(() => page.Markup.Should().Contain("Authored"));
    }

    [Fact]
    public void Add_OpenedFromAnEdition_HasTheEditionAlreadyChosen()
    {
        SeedGuard();
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/admin/questions/new?examEditionId={GuardEdition.Id}");

        var page = Render<QuestionForm>();

        page.WaitForAssertion(() =>
        {
            page.Markup.Should().Contain("CONSULPLAN · Guarda Municipal · 2024");
            NumberField(page).Disabled.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Save_WithAnOrigin_SendsTheEditionTheNumberAndTheNoticeSubject()
    {
        SeedGuard();
        var page = RenderAdd();
        await FillAsync(page);
        await PickEditionAsync(page, OptionOf(GuardEdition));
        await TypeNumberAsync(page, 12);
        await PickNoticeSubjectAsync(page, Basic.Id);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.ExamEditionId.Should().Be(GuardEdition.Id);
        sent.PaperNumber.Should().Be(12);
        sent.NoticeSubjectId.Should().Be(Basic.Id);
    }

    [Fact]
    public async Task Save_WithNoOrigin_SendsNoOriginFields()
    {
        var page = RenderAdd();
        await FillAsync(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        var sent = Api.LastSaved(HttpMethod.Post);
        sent.ExamEditionId.Should().BeNull();
        sent.PaperNumber.Should().BeNull();
        sent.NoticeSubjectId.Should().BeNull();
    }

    [Fact]
    public async Task Save_RefusedAsPaperNumberTaken_MarksTheNumberFieldAndMovesFocusToTheSummary()
    {
        SeedGuard();
        var page = RenderAdd();
        await FillAsync(page);
        await PickEditionAsync(page, OptionOf(GuardEdition));
        await TypeNumberAsync(page, 12);
        Api.SaveFailure = (HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionPaperNumberTaken, null);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
            page.Find("#question-paper-number-description .app-field-error-text").TextContent
                .Should().Be("Number already used in this edition."));
        page.Find("#question-error-summary").TextContent.Should().Contain("Number in the paper");
    }

    [Fact]
    public async Task Save_RefusedAsEditionNotFound_ClearsTheEditionAndTheFieldsThatHungFromIt()
    {
        SeedGuard();
        var page = RenderAdd();
        await FillAsync(page);
        await PickEditionAsync(page, OptionOf(GuardEdition));
        await TypeNumberAsync(page, 12);
        Api.SaveFailure = (HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionExamEditionNotFound, null);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            page.Find("#question-edition-description .app-field-error-text").TextContent
                .Should().Contain("The edition no longer exists");
            NumberField(page).Value.Should().BeNull();
            NumberField(page).Disabled.Should().BeTrue();
        });
    }

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

    // -- AC15: the source line ------------------------------------------------------------------------------------

    [Fact]
    public void List_ShowsTheSourceLineAndTheNumber_AndADashWithHiddenTextForAnAuthoredQuestion()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(FromGuard);

        var page = Render<Questions>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        var rows = page.FindAll("tbody tr");
        var authored = rows.Single(row => row.TextContent.Contains("crase corretamente", StringComparison.Ordinal));
        var fromEdition = rows.Single(row => row.TextContent.Contains("Fiscal", StringComparison.Ordinal));

        var sourceCell = fromEdition.QuerySelectorAll("td")[2];
        sourceCell.TextContent.Should().Contain("CONSULPLAN · Guarda Municipal · 2024").And.Contain("Question 12");

        var emptyCell = authored.QuerySelectorAll("td")[2];
        emptyCell.QuerySelector("[aria-hidden='true']")!.TextContent.Should().Be("—");
        emptyCell.QuerySelector(".app-visually-hidden")!.TextContent.Should().Be("Authored");
    }

    [Fact]
    public async Task Preview_ShowsTheSourceLineAndTheNumberAboveTheStatement_ForAnOrigin()
    {
        Render<MudPopoverProvider>();
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<QuestionPreviewDialog>
        {
            { dialog => dialog.Question, FromGuard },
            { dialog => dialog.UnsavedChanges, false }
        };

        await provider.InvokeAsync(async () => await dialogs.ShowAsync<QuestionPreviewDialog>("Preview", parameters));

        provider.WaitForAssertion(() =>
        {
            var source = provider.Find("#question-preview-source");
            source.TextContent.Should().Contain("CONSULPLAN · Guarda Municipal · 2024").And.Contain("Question 12");
            provider.Markup.IndexOf("question-preview-source", StringComparison.Ordinal)
                .Should().BeLessThan(provider.Markup.IndexOf("question-preview-statement", StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task Preview_OfAnAuthoredQuestion_ShowsNoSourceLine()
    {
        Render<MudPopoverProvider>();
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<QuestionPreviewDialog>
        {
            { dialog => dialog.Question, Draft },
            { dialog => dialog.UnsavedChanges, false }
        };

        await provider.InvokeAsync(async () => await dialogs.ShowAsync<QuestionPreviewDialog>("Preview", parameters));

        provider.WaitForAssertion(() => provider.FindAll("#question-preview-statement").Should().ContainSingle());
        provider.FindAll("#question-preview-source").Should().BeEmpty();
    }

    // -- AC16: the edition page's section -----------------------------------------------------------------------

    private IRenderedComponent<EditionQuestionsSection> RenderSection(Guid? editionId) =>
        Render<EditionQuestionsSection>(parameters => parameters.Add(section => section.EditionId, editionId));

    [Fact]
    public void Section_ListsTheQuestionsByNumber_WithTheirSubjectAndStatus()
    {
        SeedGuard();
        Api.Questions.Add(FromGuardSecond);
        Api.Questions.Add(FromGuard);

        var page = RenderSection(GuardEdition.Id);

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        var rows = page.FindAll("tbody tr");
        rows[0].QuerySelectorAll("td")[0].TextContent.Trim().Should().Be("12");
        rows[1].QuerySelectorAll("td")[0].TextContent.Trim().Should().Be("30");
        rows[0].TextContent.Should().Contain("Portuguese");
        rows[1].QuerySelector(".app-visually-hidden")!.TextContent.Should().Be("No notice subject");
        rows[0].QuerySelector(".app-status-chip-neutral").Should().NotBeNull();
        page.Find("#edition-section-questions-title").TextContent.Should().Be("Questions");
    }

    [Fact]
    public void Section_WithNoQuestions_SaysSo()
    {
        SeedGuard();

        var page = RenderSection(GuardEdition.Id);

        page.WaitForAssertion(() => page.Markup.Should().Contain("No questions from this edition yet."));
    }

    [Fact]
    public void Section_OfAnEditionNotSavedYet_AsksToSaveFirst_AndHasNoAddButton()
    {
        var page = RenderSection(null);

        page.Markup.Should().Contain("Save the edition first to add its questions.");
        page.FindAll("#edition-question-add").Should().BeEmpty();
    }

    [Fact]
    public void Section_AddQuestion_OpensTheFormWithTheEditionChosen()
    {
        SeedGuard();
        var page = RenderSection(GuardEdition.Id);
        page.WaitForAssertion(() => page.FindAll("#edition-question-add").Should().ContainSingle());

        page.Find("#edition-question-add").Click();

        page.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri
            .Should().EndWith($"/admin/questions/new?examEditionId={GuardEdition.Id}"));
    }

    [Fact]
    public void Section_EditAction_OpensTheQuestionOnItsOwnPage()
    {
        SeedGuard();
        Api.Questions.Add(FromGuard);
        var page = RenderSection(GuardEdition.Id);
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().ContainSingle());

        page.Find($"#edition-question-actions-{FromGuard.Id}-edit").Click();

        page.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri
            .Should().EndWith($"/admin/questions/{FromGuard.Id}"));
    }

    [Fact]
    public void Section_AsksTheServerForTheEditionsFirstPageOf25()
    {
        SeedGuard();

        var page = RenderSection(GuardEdition.Id);
        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Path == $"/api/v1/question-bank/editions/{GuardEdition.Id}/questions"));

        var call = Api.Received.First(call => call.Path.EndsWith("/questions", StringComparison.Ordinal)
            && call.Path.Contains("/editions/", StringComparison.Ordinal));
        call.Query.Should().Contain("page=0").And.Contain("pageSize=25");
    }
}
