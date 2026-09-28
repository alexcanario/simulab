using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-35 Screen 1: the editions section on the exam page (AC2, AC3, AC12). It is rendered inside
/// <see cref="ExamForm"/> wherever the test is about the page, and on its own where it is about the section.
/// </summary>
public sealed class ExamEditionsSectionTests : CatalogPageTestContext
{
    private static readonly Guid Edition2026Id = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e1");
    private static readonly Guid Edition2024Id = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e2");

    private static ExamEditionResponse Edition(
        Guid id,
        int year,
        string? position,
        ExamEditionStatus status,
        DateOnly? appliedOn = null) =>
        new(id, AgentePf.Id, Cebraspe.Id, Cebraspe.Name, Cebraspe.Acronym, year, position, null, null, appliedOn, status);

    private IRenderedComponent<ExamForm> RenderExamPage(Guid? id = null) =>
        id is null
            ? Render<ExamForm>()
            : Render<ExamForm>(parameters => parameters.Add(form => form.Id, id.Value));

    private IRenderedComponent<ExamEditionsSection> RenderSection() =>
        Render<ExamEditionsSection>(parameters => parameters
            .Add(section => section.ExamId, AgentePf.Id)
            .Add(section => section.ExamName, AgentePf.Name));

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static string RowText(IRenderedComponent<ExamEditionsSection> section, int index) =>
        Flat(section.FindAll(".app-item-row-content")[index].TextContent);

    private static string DeleteButtonOf(string label) => $"button[aria-label='Delete: {label}']";

    // AC2: the exam page lists its editions, newest first, each with year, position, board, state and date.
    [Fact]
    public void ExamPage_WithEditions_ListsThemInTheApisOrderWithEveryPart()
    {
        var applied = new DateOnly(2026, 6, 14);
        Api.Editions.Add(Edition(Edition2026Id, 2026, "Guarda Municipal de 3ª Classe", ExamEditionStatus.Published, applied));
        Api.Editions.Add(Edition(Edition2024Id, 2024, null, ExamEditionStatus.Draft));

        var page = RenderExamPage(AgentePf.Id);

        page.WaitForAssertion(() => page.FindAll(".app-item-row-content").Should().HaveCount(2));
        var rows = page.FindAll(".app-item-row-content").Select(row => Flat(row.TextContent)).ToList();
        rows[0].Should().StartWith("2026 · Guarda Municipal de 3ª Classe · CEBRASPE")
            .And.Contain("Published")
            .And.Contain($"Applied on {applied.ToString("d", CultureInfo.CurrentCulture)}");
        rows[1].Should().StartWith("2024 · CEBRASPE", "an edition with no position leaves that part out")
            .And.Contain("Draft")
            .And.NotContain("Applied on");
        page.FindAll(".app-status-chip-success").Should().ContainSingle("only the published row is green");
        page.FindAll(".app-status-chip-neutral").Should().ContainSingle();
    }

    // The Api orders (BR14); the page shows what it is given.
    [Fact]
    public void Section_DoesNotSortWhatTheApiSent()
    {
        Api.Editions.Add(Edition(Edition2024Id, 2024, null, ExamEditionStatus.Draft));
        Api.Editions.Add(Edition(Edition2026Id, 2026, null, ExamEditionStatus.Draft));

        var section = RenderSection();

        section.WaitForAssertion(() => section.FindAll(".app-item-row-content").Should().HaveCount(2));
        RowText(section, 0).Should().StartWith("2024");
        RowText(section, 1).Should().StartWith("2026");
    }

    [Fact]
    public void ExamPage_TheSectionIsACardOfItsOwnOutsideTheExamForm()
    {
        Api.Editions.Add(Edition(Edition2026Id, 2026, null, ExamEditionStatus.Draft));

        var page = RenderExamPage(AgentePf.Id);

        page.WaitForAssertion(() => page.FindAll("#exam-section-editions-title").Should().ContainSingle());
        page.Find("#exam-section-editions-title").TextContent.Should().Be("Editions");
        page.FindAll(".app-form-layout #exam-section-editions-title").Should().BeEmpty("it sits below the form layout, not inside it");
        page.FindAll(".app-form-save").Should().ContainSingle("the exam's Save is the page's only primary button");
    }

    // AC3: an exam that is not saved says so and offers no Add.
    [Fact]
    public void ExamPage_NewExam_SaysToSaveTheExamFirstAndOffersNoAdd()
    {
        var page = RenderExamPage();

        page.Find("section[aria-labelledby='exam-section-editions-title']").TextContent.Should()
            .Contain("Save the exam first. Its editions are added here.");
        page.FindAll("#exam-edition-add").Should().BeEmpty();
        Api.Received.Should().NotContain(call => call.Path.EndsWith("/editions", StringComparison.Ordinal));
    }

    // AC3, second half: the first Save turns the page into the edit form, and the section loads and offers Add.
    [Fact]
    public async Task ExamPage_NewExamSaved_TheSectionLoadsAndTheAddLinkAppears()
    {
        RenderProviders();
        var page = RenderExamPage();
        page.FindComponent<ExamEditionsSection>().Instance.ExamId.Should().BeNull();

        var lookup = page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "exam-authority");
        await page.InvokeAsync(() => lookup.Instance.ValueChanged.InvokeAsync(
            new AppLookupOption(PoliciaFederal.Id, $"{PoliciaFederal.Name} ({PoliciaFederal.Acronym})")));
        page.Find("#exam-name").Change("Agente de Policia Federal");
        await page.InvokeAsync(() => page.FindComponents<AppSelectField<AssessmentType?>>().Single().Instance.ValueChanged
            .InvokeAsync(AssessmentType.PublicServiceExam));
        await page.InvokeAsync(() => page.FindComponents<AppRadioCards<ExamScope?>>().Single().Instance.ValueChanged
            .InvokeAsync(ExamScope.National));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#exam-edition-add").GetAttribute("href").Should().Contain("/editions/new"));
        page.Markup.Should().Contain("This exam has no edition yet.");
    }

    [Fact]
    public void Section_NoEditions_ShowsTheEmptyMessageAndTheAddLinkToTheEditionPage()
    {
        var section = RenderSection();

        section.WaitForAssertion(() => section.Markup.Should().Contain("This exam has no edition yet."));
        var add = section.Find("#exam-edition-add");
        add.TagName.Should().Be("A", "adding navigates, so it is a link");
        add.GetAttribute("href").Should().EndWith($"/admin/exams/{AgentePf.Id}/editions/new");
        add.TextContent.Should().Contain("Add edition");
    }

    [Fact]
    public void Section_LoadFails_ShowsTheErrorStateWithTryAgainAndNoAddLink()
    {
        Api.ListEditionsFailure = (HttpStatusCode.InternalServerError, "server_error");
        Api.Editions.Add(Edition(Edition2026Id, 2026, null, ExamEditionStatus.Draft));

        var section = RenderSection();

        section.WaitForAssertion(() => section.Find(".app-state-error").TextContent.Should().Contain("We could not load this list."));
        section.FindAll("#exam-edition-add").Should().BeEmpty();

        Api.ListEditionsFailure = null;
        section.Find(".app-retry").Click();

        section.WaitForAssertion(() => section.FindAll(".app-item-row-content").Should().HaveCount(1));
        section.FindAll(".app-state-error").Should().BeEmpty();
    }

    [Fact]
    public void Section_Edit_GoesToTheEditionPageOfThatEdition()
    {
        Api.Editions.Add(Edition(Edition2026Id, 2026, null, ExamEditionStatus.Draft));
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll(".app-item-row-content").Should().HaveCount(1));

        section.Find("button[aria-label='Edit: 2026, CEBRASPE']").Click();

        Services.GetRequiredService<NavigationManager>().Uri.Should()
            .EndWith($"/admin/exams/{AgentePf.Id}/editions/{Edition2026Id}");
    }

    // AC12: a published edition's delete is disabled, with the reason as the tooltip.
    [Fact]
    public void Section_PublishedRow_HasItsDeleteDisabledWithTheReason()
    {
        Api.Editions.Add(Edition(Edition2026Id, 2026, null, ExamEditionStatus.Published));
        Api.Editions.Add(Edition(Edition2024Id, 2024, null, ExamEditionStatus.Draft));

        var section = RenderSection();

        section.WaitForAssertion(() => section.FindAll(".app-item-row-content").Should().HaveCount(2));
        section.Find(DeleteButtonOf("2026, CEBRASPE")).HasAttribute("disabled").Should().BeTrue();
        section.Find(DeleteButtonOf("2024, CEBRASPE")).HasAttribute("disabled").Should().BeFalse();
        section.FindComponents<AppRowActions>()
            .Single(actions => actions.Instance.ItemName == "2026, CEBRASPE")
            .Instance.DeleteDisabledReason.Should().Be("Set it back to Draft before deleting it.");
        section.FindComponents<AppRowActions>()
            .Single(actions => actions.Instance.ItemName == "2024, CEBRASPE")
            .Instance.DeleteDisabledReason.Should().BeNull();
    }

    // AC12: a draft is deleted after the confirmation names it and the exam; it leaves the section.
    [Fact]
    public void Section_DeleteDraftConfirmed_SendsTheDeleteAndTheRowLeaves()
    {
        Api.Editions.Add(Edition(Edition2026Id, 2026, null, ExamEditionStatus.Published));
        Api.Editions.Add(Edition(Edition2024Id, 2024, "Analista", ExamEditionStatus.Draft));
        var providers = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll(".app-item-row-content").Should().HaveCount(2));

        section.Find(DeleteButtonOf("2024, Analista, CEBRASPE")).Click();

        providers.Dialogs.WaitForAssertion(() =>
            Flat(providers.Dialogs.Markup).Should().Contain("The edition 2024 · Analista · CEBRASPE leaves the exam Agente de Policia Federal.")
                .And.Contain("Delete edition 2024 · Analista · CEBRASPE"));
        providers.Dialogs.Find(".app-confirm-ok").Click();

        section.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Delete
            && call.Path == $"/api/v1/catalog/exams/{AgentePf.Id}/editions/{Edition2024Id}"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Edition deleted."));
        section.WaitForAssertion(() => section.FindAll(".app-item-row-content").Should().ContainSingle()
            .Which.TextContent.Should().Contain("2026"));
    }

    [Fact]
    public void Section_DeleteCancelled_SendsNothing()
    {
        Api.Editions.Add(Edition(Edition2024Id, 2024, null, ExamEditionStatus.Draft));
        var providers = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll(".app-item-row-content").Should().HaveCount(1));

        section.Find(DeleteButtonOf("2024, CEBRASPE")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("leaves the exam"));
        providers.Dialogs.Find(".app-confirm-cancel").Click();

        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Delete);
        section.FindAll(".app-item-row-content").Should().HaveCount(1);
    }

    // AC12, the Api's guard: another Admin published it after the list loaded - the alert says so, the row stays.
    [Fact]
    public void Section_DeleteRefusedBecausePublished_ShowsTheAlertAndTheRowStays()
    {
        Api.Editions.Add(Edition(Edition2024Id, 2024, null, ExamEditionStatus.Draft));
        var providers = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll(".app-item-row-content").Should().HaveCount(1));
        Api.Editions[0] = Api.Editions[0] with { Status = ExamEditionStatus.Published };

        section.Find(DeleteButtonOf("2024, CEBRASPE")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-ok").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-ok").Click();

        section.WaitForAssertion(() => section.Find("[role='alert']").TextContent.Should()
            .Contain("This edition is published. Set it back to Draft, save, then delete it."));
        section.WaitForAssertion(() => RowText(section, 0).Should().Contain("Published"));
        section.FindAll(".app-item-row-content").Should().ContainSingle();
    }

    [Fact]
    public void Section_DeleteOfAnEditionAlreadyGone_ShowsTheAlertAndReloadsTheList()
    {
        Api.Editions.Add(Edition(Edition2024Id, 2024, null, ExamEditionStatus.Draft));
        var providers = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll(".app-item-row-content").Should().HaveCount(1));
        Api.Editions.Clear();

        section.Find(DeleteButtonOf("2024, CEBRASPE")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-ok").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-ok").Click();

        section.WaitForAssertion(() => section.Find("[role='alert']").TextContent.Should().Contain("This edition no longer exists."));
        section.WaitForAssertion(() => section.Markup.Should().Contain("This exam has no edition yet."));
    }

    [Fact]
    public void Section_DeleteFailsOnTheServer_ShowsTheGenericAlert()
    {
        Api.Editions.Add(Edition(Edition2024Id, 2024, null, ExamEditionStatus.Draft));
        var providers = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll(".app-item-row-content").Should().HaveCount(1));
        Api.WriteFailure = (HttpStatusCode.InternalServerError, "server_error");

        section.Find(DeleteButtonOf("2024, CEBRASPE")).Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll(".app-confirm-ok").Should().ContainSingle());
        providers.Dialogs.Find(".app-confirm-ok").Click();

        section.WaitForAssertion(() => section.Find("[role='alert']").TextContent.Should().Contain("Something went wrong. Please try again."));
    }
}
