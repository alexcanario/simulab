using System.Globalization;
using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-35 Screen 2: <c>/admin/exams/{examId}/editions/new</c> and <c>/{id}</c> (AC4, AC5, AC11, AC17). The Api
/// runs every rule (BR3 to BR9); these tests say what the page sends, what it shows of a refusal, and what a
/// reader reaches with the keyboard.
/// </summary>
public sealed class ExamEditionFormTests : CatalogPageTestContext
{
    private static readonly Guid SavedEditionId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000f1");

    private static readonly ExamEditionResponse FullEdition = new(
        SavedEditionId,
        AgentePf.Id,
        Cebraspe.Id,
        Cebraspe.Name,
        Cebraspe.Acronym,
        2026,
        "Guarda Municipal de 3ª Classe",
        "Edital nº 01/2026",
        "https://www.exemplo.gov.br/edital-01-2026.pdf",
        new DateOnly(2026, 6, 14),
        ExamEditionStatus.Draft);

    private IRenderedComponent<ExamEditionForm> RenderAdd() =>
        Render<ExamEditionForm>(parameters => parameters.Add(form => form.ExamId, AgentePf.Id));

    private IRenderedComponent<ExamEditionForm> RenderEdit(Guid id) =>
        Render<ExamEditionForm>(parameters => parameters
            .Add(form => form.ExamId, AgentePf.Id)
            .Add(form => form.Id, id));

    /// <summary>Waits for the exam (and edition) to load: the form is there once the board field is.</summary>
    private static void WaitForForm(IRenderedComponent<ExamEditionForm> page) =>
        page.WaitForAssertion(() => page.FindAll("#edition-organizer").Should().ContainSingle());

    private static SaveExamEditionRequest SentBody(FakeCatalogApi api, HttpMethod method) =>
        FakeCatalogApi.Read<SaveExamEditionRequest>(api.Received.Last(call => call.Method == method).Body);

    private static void PickOrganizer(IRenderedComponent<ExamEditionForm> page, OrganizerResponse organizer)
    {
        var lookup = page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "edition-organizer");
        page.InvokeAsync(() => lookup.Instance.ValueChanged.InvokeAsync(
            new AppLookupOption(organizer.Id, $"{organizer.Name} ({organizer.Acronym})"))).GetAwaiter().GetResult();
    }

    private static void SetAppliedOn(IRenderedComponent<ExamEditionForm> page, DateOnly? day)
    {
        var field = page.FindComponent<AppDateField>();
        page.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(day)).GetAwaiter().GetResult();
    }

    private static string Typed(DateOnly day) =>
        day.ToDateTime(TimeOnly.MinValue).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentCulture);

    private static void FillTheRequired(IRenderedComponent<ExamEditionForm> page)
    {
        PickOrganizer(page, Cebraspe);
        page.Find("#edition-notice-year").Change("2026");
    }

    private static IReadOnlyList<string?> SummaryLinks(IRenderedComponent<ExamEditionForm> page) =>
        [.. page.FindAll("#edition-error-summary .app-error-summary-list a").Select(link => link.GetAttribute("href"))];

    // Render with real parameters (profile): the three section cards, every field in its card, Draft chosen.
    [Fact]
    public void Render_Add_ShowsTheThreeSectionsTheirFieldsAndADraftStatus()
    {
        var page = RenderAdd();
        WaitForForm(page);

        page.Find("h1").TextContent.Should().Be("Add edition");
        page.FindComponents<AppSectionCard>().Select(section => section.Instance.Title).Should().Equal(
            "Paper", "Notice and application", "Publication");

        var paper = page.Find("section[aria-labelledby='edition-section-paper-title']");
        paper.QuerySelectorAll("#edition-organizer").Should().ContainSingle();
        paper.QuerySelectorAll("#edition-notice-year").Should().ContainSingle();
        paper.QuerySelectorAll("#edition-position").Should().ContainSingle();

        var notice = page.Find("section[aria-labelledby='edition-section-notice-title']");
        notice.QuerySelectorAll("#edition-notice-reference").Should().ContainSingle();
        notice.QuerySelectorAll("#edition-notice-url").Should().ContainSingle();
        notice.QuerySelectorAll("#edition-applied-on").Should().ContainSingle();

        var publication = page.Find("section[aria-labelledby='edition-section-publication-title']");
        publication.QuerySelectorAll("#edition-status[role='radiogroup']").Should().ContainSingle();
        page.Find("#edition-status-Draft").GetAttribute("aria-checked").Should().Be("true");
        page.Find("#edition-status-Published").GetAttribute("aria-checked").Should().Be("false");
        page.Markup.Should().Contain("Kept in the administration. Students do not see it.")
            .And.Contain("Offered to students in the catalog.");
    }

    [Fact]
    public void Render_Add_TheFieldsCarryTheirLimitsRequiredMarksAndTheYearHint()
    {
        var page = RenderAdd();
        WaitForForm(page);

        page.Find("#edition-notice-year").GetAttribute("type").Should().Be("number");
        page.Find("#edition-notice-year").GetAttribute("maxlength").Should().Be(CatalogLimits.ExamEditionNoticeYearDigits.ToString(CultureInfo.InvariantCulture));
        page.Find("#edition-position").GetAttribute("maxlength").Should().Be(CatalogLimits.ExamEditionPositionMaxLength.ToString(CultureInfo.InvariantCulture));
        page.Find("#edition-notice-reference").GetAttribute("maxlength").Should().Be(CatalogLimits.ExamEditionNoticeReferenceMaxLength.ToString(CultureInfo.InvariantCulture));
        page.Find("#edition-notice-url").GetAttribute("maxlength").Should().Be(CatalogLimits.ExamEditionNoticeUrlMaxLength.ToString(CultureInfo.InvariantCulture));
        page.Find("#edition-notice-url").GetAttribute("type").Should().Be("url");
        page.Find("#edition-organizer").GetAttribute("aria-required").Should().Be("true");
        page.Find("#edition-notice-year").GetAttribute("aria-required").Should().Be("true");
        page.Find("#edition-position").GetAttribute("aria-required").Should().BeNull();
        page.Find("#edition-notice-year-description").TextContent.Should().Contain("From 1990 to 2027.");
    }

    [Fact]
    public void Render_Add_TheBreadcrumbNamesTheExamAndTheAsideShowsItsLanguage()
    {
        var page = RenderAdd();
        WaitForForm(page);

        var crumbs = page.Find(".app-breadcrumbs").TextContent;
        crumbs.Should().Contain("Content").And.Contain("Exams").And.Contain("Agente de Policia Federal").And.Contain("Add edition");
        page.Find("a[href='/admin/exams']").TextContent.Should().Contain("Exams");
        page.Find($"a[href='/admin/exams/{AgentePf.Id}']").TextContent.Should().Contain("Agente de Policia Federal");

        var aside = page.Find(".app-form-aside");
        aside.QuerySelectorAll("button").Should().BeEmpty("the aside is read-only");
        aside.TextContent.Should().Contain("Agente de Policia Federal").And.Contain("Português (Brasil)").And.Contain("Not filled");
        aside.QuerySelectorAll(".app-checklist-pending").Should().HaveCount(2);
    }

    [Fact]
    public void Aside_FollowsWhatIsTypedAndTheChecklistTicksOff()
    {
        var page = RenderAdd();
        WaitForForm(page);

        FillTheRequired(page);

        page.WaitForAssertion(() => page.Find(".app-form-aside").TextContent.Should()
            .Contain("Centro Brasileiro de Pesquisa em Avaliacao (CEBRASPE)").And.Contain("2026"));
        page.Find(".app-form-aside").QuerySelectorAll(".app-checklist-done").Should().HaveCount(2);
    }

    // The board is searched on the organizer list, by the term the reader types.
    [Fact]
    public void Organizer_Typing_AsksTheOrganizerListWithTheTerm()
    {
        RenderProviders();
        var page = RenderAdd();
        WaitForForm(page);

        page.Find("#edition-organizer").Input("ceb");

        // BR4: the year rule reads the context's frozen clock (28 Sep 2026), and so does the picker's debounce.
        AdvanceDebounce();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Path == "/api/v1/catalog/organizers"
            && call.Query!.Contains("search=ceb", StringComparison.Ordinal)
            && call.Query.Contains($"pageSize={AppLookupField.MaxCandidates}", StringComparison.Ordinal)));
    }

    // AC4: a board and a year only - a draft is created, the page becomes its edit form.
    [Fact]
    public void Save_BoardAndYearOnly_CreatesADraftAndBecomesTheEditForm()
    {
        var providers = RenderProviders();
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/catalog/exams/{AgentePf.Id}/editions"));
        var sent = SentBody(Api, HttpMethod.Post);
        sent.OrganizerId.Should().Be(Cebraspe.Id);
        sent.NoticeYear.Should().Be(2026);
        sent.Status.Should().Be("Draft");
        sent.Position.Should().BeNull("blank is sent as null");
        sent.NoticeReference.Should().BeNull();
        sent.NoticeUrl.Should().BeNull();
        sent.AppliedOn.Should().BeNull();

        page.WaitForAssertion(() => page.Find("h1").TextContent.Should().Be("Edit edition"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Edition saved."));
        var created = Api.Editions.Should().ContainSingle().Which;
        created.Status.Should().Be(ExamEditionStatus.Draft);
        page.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should()
            .EndWith($"/admin/exams/{AgentePf.Id}/editions/{created.Id}", "the URL is replaced by the edition's own"));
        page.Find(".app-breadcrumbs").TextContent.Should().Contain("2026");
    }

    // AC5: every field is sent as typed - trimmed, the date as the same calendar day, the status as a name.
    [Fact]
    public void Save_EveryField_SendsThemTrimmedWithTheDayAndTheStatus()
    {
        RenderProviders();
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);
        page.Find("#edition-position").Change("  Guarda Municipal de 3ª Classe  ");
        page.Find("#edition-notice-reference").Change("Edital nº 01/2026");
        page.Find("#edition-notice-url").Change("https://www.exemplo.gov.br/edital-01-2026.pdf");
        SetAppliedOn(page, new DateOnly(2026, 6, 14));
        page.Find("#edition-status-Published").Click();

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        var sent = SentBody(Api, HttpMethod.Post);
        sent.Position.Should().Be("Guarda Municipal de 3ª Classe");
        sent.NoticeReference.Should().Be("Edital nº 01/2026");
        sent.NoticeUrl.Should().Be("https://www.exemplo.gov.br/edital-01-2026.pdf");
        sent.AppliedOn.Should().Be(new DateOnly(2026, 6, 14));
        sent.Status.Should().Be("Published");
    }

    // AC5 and AC17: the date typed in the reader's format, by keyboard, is the day that is sent.
    [Fact]
    public void Save_TheDateTypedByKeyboard_IsTheDaySent()
    {
        RenderProviders();
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);

        page.Find("#edition-applied-on").Change(Typed(new DateOnly(2026, 6, 14)));
        page.WaitForAssertion(() => page.FindComponent<AppDateField>().Instance.Value.Should().Be(new DateOnly(2026, 6, 14)));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        SentBody(Api, HttpMethod.Post).AppliedOn.Should().Be(new DateOnly(2026, 6, 14));
    }

    // AC5: saved and reopened, every field comes back as saved and the date is the same calendar day.
    [Fact]
    public void Edit_OpensEveryFieldAsSaved()
    {
        Api.Editions.Add(FullEdition);

        var page = RenderEdit(SavedEditionId);
        WaitForForm(page);

        page.Find("h1").TextContent.Should().Be("Edit edition");
        page.Find("#edition-organizer").GetAttribute("value").Should().Be("Centro Brasileiro de Pesquisa em Avaliacao (CEBRASPE)");
        page.Find("#edition-notice-year").GetAttribute("value").Should().Be("2026");
        page.Find("#edition-position").GetAttribute("value").Should().Be("Guarda Municipal de 3ª Classe");
        page.Find("#edition-notice-reference").GetAttribute("value").Should().Be("Edital nº 01/2026");
        page.Find("#edition-notice-url").GetAttribute("value").Should().Be("https://www.exemplo.gov.br/edital-01-2026.pdf");
        page.FindComponent<AppDateField>().Instance.Value.Should().Be(new DateOnly(2026, 6, 14));
        page.Find("#edition-applied-on").GetAttribute("value").Should().Be(Typed(new DateOnly(2026, 6, 14)));
        page.Find("#edition-status-Draft").GetAttribute("aria-checked").Should().Be("true");
        page.Find(".app-breadcrumbs").TextContent.Should().Contain("2026 · Guarda Municipal de 3ª Classe");
    }

    [Fact]
    public void Edit_Save_SendsAPutOnThatEditionWithEveryFieldAndTheStatus()
    {
        Api.Editions.Add(FullEdition);
        RenderProviders();
        var page = RenderEdit(SavedEditionId);
        WaitForForm(page);

        page.Find("#edition-position").Change("Analista");
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Put
            && call.Path == $"/api/v1/catalog/exams/{AgentePf.Id}/editions/{SavedEditionId}"));
        var sent = SentBody(Api, HttpMethod.Put);
        sent.Position.Should().Be("Analista");
        sent.Status.Should().Be("Draft", "a PUT always names the status: a blank one is refused (BR9)");
        sent.NoticeYear.Should().Be(2026);
        sent.AppliedOn.Should().Be(new DateOnly(2026, 6, 14));
        page.Find("h1").TextContent.Should().Be("Edit edition");
    }

    // AC11: publishing and setting back to draft go through the same form, and the row's chip follows.
    [Fact]
    public void Edit_PublishThenSetBackToDraft_TheSectionChipFollows()
    {
        Api.Editions.Add(FullEdition);
        RenderProviders();

        ChangeStatusAndSave(ExamEditionStatus.Published);
        SentBody(Api, HttpMethod.Put).Status.Should().Be("Published");
        var afterPublishing = Render<ExamEditionsSection>(parameters => parameters
            .Add(section => section.ExamId, AgentePf.Id).Add(section => section.ExamName, AgentePf.Name));
        afterPublishing.WaitForAssertion(() => afterPublishing.Find(".app-status-chip").TextContent.Should().Contain("Published"));
        afterPublishing.FindAll(".app-status-chip-success").Should().ContainSingle();

        ChangeStatusAndSave(ExamEditionStatus.Draft);
        SentBody(Api, HttpMethod.Put).Status.Should().Be("Draft");
        var afterUnpublishing = Render<ExamEditionsSection>(parameters => parameters
            .Add(section => section.ExamId, AgentePf.Id).Add(section => section.ExamName, AgentePf.Name));
        afterUnpublishing.WaitForAssertion(() => afterUnpublishing.Find(".app-status-chip").TextContent.Should().Contain("Draft"));
        afterUnpublishing.FindAll(".app-status-chip-neutral").Should().ContainSingle();
    }

    private void ChangeStatusAndSave(ExamEditionStatus status)
    {
        var puts = Api.Received.Count(call => call.Method == HttpMethod.Put);
        var page = RenderEdit(SavedEditionId);
        WaitForForm(page);

        page.Find($"#edition-status-{status}").Click();
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Count(call => call.Method == HttpMethod.Put).Should().Be(puts + 1));
        page.WaitForAssertion(() => Api.Editions.Single().Status.Should().Be(status));
    }

    // Comfort checks: nothing is sent, one panel lists what is missing and gets the focus.
    [Fact]
    public void Save_Empty_ListsTheBoardAndTheYearAtTheTopFocusesThePanelAndCallsNothing()
    {
        var page = RenderAdd();
        WaitForForm(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => SummaryLinks(page).Should().Equal("#edition-organizer", "#edition-notice-year"));
        page.Find("#edition-error-summary").GetAttribute("role").Should().Be("alert");
        page.Markup.Should().Contain("Choose the board.").And.Contain("Type a year from 1990 to next year.");
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
        page.WaitForAssertion(() => JSInterop.Invocations.Should().Contain(invocation =>
            invocation.Identifier == "simulabShell.focusElement" && invocation.Arguments.Contains("edition-error-summary")));
    }

    [Theory]
    [InlineData("1989", true)]
    [InlineData("1990", false)]
    [InlineData("2027", false)]
    [InlineData("2028", true)]
    public void NoticeYear_LeavingTheField_AcceptsFrom1990ToNextYearOnly(string typed, bool refused)
    {
        var page = RenderAdd();
        WaitForForm(page);

        page.Find("#edition-notice-year").Change(typed);

        page.WaitForAssertion(() =>
        {
            page.Find("#edition-notice-year-description").TextContent.Contains("Type a year from 1990 to next year.", StringComparison.Ordinal)
                .Should().Be(refused);
        });
    }

    [Theory]
    [InlineData("ftp://exemplo.gov.br/edital", true)]
    [InlineData("www.exemplo.gov.br/edital", true)]
    [InlineData("http://exemplo.gov.br/edital", false)]
    [InlineData("https://exemplo.gov.br/edital", false)]
    public void NoticeUrl_LeavingTheField_MustBeAnAbsoluteHttpOrHttpsAddress(string typed, bool refused)
    {
        var page = RenderAdd();
        WaitForForm(page);

        page.Find("#edition-notice-url").Change(typed);

        page.WaitForAssertion(() =>
        {
            var description = page.Find("#edition-notice-url-description").TextContent;
            description.Contains("Type a full address of at most 300 characters", StringComparison.Ordinal).Should().Be(refused);
        });
    }

    // BR8: the calendar greys the days before the notice year, and a date before it is refused with the rule's text.
    [Fact]
    public void AppliedOn_BeforeTheNoticeYear_IsRefusedAndCheckedAgainWhenTheYearChanges()
    {
        var page = RenderAdd();
        WaitForForm(page);
        page.Find("#edition-notice-year").Change("2026");
        page.WaitForAssertion(() => page.FindComponent<AppDateField>().Instance.Min.Should().Be(new DateOnly(2026, 1, 1)));

        SetAppliedOn(page, new DateOnly(2025, 12, 31));

        page.WaitForAssertion(() => page.Find("#edition-applied-on-description").TextContent.Should()
            .Contain("The application date cannot be before 1 January of the notice year."));

        page.Find("#edition-notice-year").Change("2025");

        page.WaitForAssertion(() => page.Find("#edition-applied-on-description").TextContent.Should()
            .NotContain("cannot be before 1 January"));
    }

    [Fact]
    public void AppliedOn_WithNoYearYet_HasNoMinimum()
    {
        var page = RenderAdd();
        WaitForForm(page);

        page.FindComponent<AppDateField>().Instance.Min.Should().BeNull();
    }

    // An Api field code goes to its field, and the panel lists it.
    [Fact]
    public void Save_ApiRefusesTheUrl_ShowsItOnTheFieldAndInThePanel()
    {
        Api.WriteFailure = (HttpStatusCode.BadRequest, CatalogErrorCodes.ExamEditionNoticeUrlInvalid);
        RenderProviders();
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#edition-notice-url-description").TextContent.Should()
            .Contain("Type a full address of at most 300 characters"));
        page.Find("#edition-notice-url").GetAttribute("aria-invalid").Should().Be("true");
        SummaryLinks(page).Should().Equal("#edition-notice-url");
    }

    [Fact]
    public void Save_ApiRefusesThePositionAsTooLong_ShowsItOnThePositionField()
    {
        Api.WriteFailure = (HttpStatusCode.BadRequest, CatalogErrorCodes.ExamEditionPositionTooLong);
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#edition-position-description").TextContent.Should()
            .Contain("The position is too long: at most 200 characters."));
    }

    // exam_edition.duplicate concerns three fields at once: the alert at the top, no field, no panel.
    [Fact]
    public void Save_Duplicate_ShowsTheAlertOnlyAndKeepsWhatWasTyped()
    {
        Api.WriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.ExamEditionDuplicate);
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);
        page.Find("#edition-position").Change("Analista");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("[role='alert']").TextContent.Should()
            .Contain("This exam already has an edition with this year, position and board"));
        page.FindAll("#edition-error-summary").Should().BeEmpty();
        page.Find("#edition-position").GetAttribute("value").Should().Be("Analista");
        page.Find("#edition-notice-year").GetAttribute("value").Should().Be("2026");
    }

    // The board left the catalog since it was picked: cleared, asked again, and the alert says why.
    [Fact]
    public void Save_OrganizerNotFound_ClearsTheBoardAndAsksForAnother()
    {
        Api.WriteFailure = (HttpStatusCode.NotFound, CatalogErrorCodes.OrganizerNotFound);
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("This organizer no longer exists. Refresh the list."));
        page.Find("#edition-organizer-description").TextContent.Should().Contain("Choose the board.");
        page.Find("#edition-organizer").GetAttribute("value").Should().BeNullOrEmpty();
    }

    [Fact]
    public void Save_ExamGoneMeanwhile_ShowsTheAlertWithTheLinkBackToTheExams()
    {
        Api.WriteFailure = (HttpStatusCode.NotFound, CatalogErrorCodes.ExamNotFound);
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("This exam no longer exists."));
        page.Find("a.app-link[href='/admin/exams']").TextContent.Should().Contain("Back to the exams");
    }

    [Fact]
    public void Save_EditionGoneMeanwhile_ShowsTheAlertWithTheLinkBackToTheExam()
    {
        Api.Editions.Add(FullEdition);
        var page = RenderEdit(SavedEditionId);
        WaitForForm(page);
        Api.WriteFailure = (HttpStatusCode.NotFound, CatalogErrorCodes.ExamEditionNotFound);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("This edition no longer exists."));
        page.Find($"a.app-link[href='/admin/exams/{AgentePf.Id}']").TextContent.Should().Contain("Back to the exam");
    }

    [Fact]
    public void Save_ServerError_ShowsTheGenericAlertAndKeepsTheForm()
    {
        Api.WriteFailure = (HttpStatusCode.InternalServerError, "server_error");
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("[role='alert']").TextContent.Should().Contain("Something went wrong. Please try again."));
        page.Find("#edition-notice-year").GetAttribute("value").Should().Be("2026");
    }

    [Fact]
    public void Render_ExamNotFound_ShowsTheMessageAndALinkBackAndNoFields()
    {
        Api.FindExamFailure = (HttpStatusCode.NotFound, CatalogErrorCodes.ExamNotFound);

        var page = RenderAdd();

        page.WaitForAssertion(() => page.Markup.Should().Contain("This exam no longer exists."));
        page.Markup.Should().Contain("Back to the exams");
        page.FindAll("#edition-organizer").Should().BeEmpty();
    }

    [Fact]
    public void Render_EditionNotFound_ShowsTheMessageAndALinkBackToTheExam()
    {
        var page = RenderEdit(Guid.CreateVersion7());

        page.WaitForAssertion(() => page.Markup.Should().Contain("This edition no longer exists."));
        page.Find($"a.app-link[href='/admin/exams/{AgentePf.Id}']").TextContent.Should().Contain("Back to the exam");
        page.FindAll("#edition-organizer").Should().BeEmpty();
    }

    // An id under another exam's route is not this exam's edition.
    [Fact]
    public void Render_EditionOfAnotherExam_IsNotFound()
    {
        Api.Editions.Add(FullEdition with { ExamId = Fuvest.Id });

        var page = RenderEdit(SavedEditionId);

        page.WaitForAssertion(() => page.Markup.Should().Contain("This edition no longer exists."));
    }

    [Fact]
    public void Cancel_WithoutChanges_GoesBackToTheExamPage()
    {
        var page = RenderAdd();
        WaitForForm(page);

        page.Find(".app-form-cancel").Click();

        page.WaitForAssertion(() =>
            Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/admin/exams/{AgentePf.Id}"));
    }

    [Fact]
    public void Render_TheOnlyPrimaryButtonIsSaveAndThereIsNoDelete()
    {
        Api.Editions.Add(FullEdition);
        var page = RenderEdit(SavedEditionId);
        WaitForForm(page);

        page.FindAll(".app-form-save").Should().ContainSingle();
        page.FindAll(".app-primary-action").Should().BeEmpty("the header has no primary action here");
        page.Markup.Should().NotContain("Delete");
    }
}
