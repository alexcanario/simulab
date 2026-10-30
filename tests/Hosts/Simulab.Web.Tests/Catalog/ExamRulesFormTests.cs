using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-168 on the edition form (<c>/admin/exams/{examId}/editions/{id}</c>): the "Exam rules" card after "Paper", what
/// it sends, what it shows of a refusal, the objective total of the notice subjects, the rules still missing and the
/// cut-off warning (AC1, AC2, AC3, AC5, AC6, AC7, AC8). The Api runs every rule; these tests say what the page does.
/// </summary>
public sealed class ExamRulesFormTests : CatalogPageTestContext
{
    private static readonly Guid EditionId = Guid.Parse("0198f0a3-0000-7000-8000-0000000001f1");

    private static readonly ExamEditionResponse Edition = new(
        EditionId,
        AgentePf.Id,
        Cebraspe.Id,
        Cebraspe.Name,
        Cebraspe.Acronym,
        2026,
        "Guarda Municipal",
        "Edital nº 01/2026",
        null,
        null,
        ExamEditionStatus.Draft);

    private static readonly ExamEditionResponse WithRules = Edition with
    {
        DurationMinutes = 300,
        ObjectiveCutOffScore = 60m,
        EssayMaxScore = 20m,
        EssayCutOffScore = 10m
    };

    private static string Typed(decimal value) => value.ToString("0.00", CultureInfo.CurrentCulture);

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private IRenderedComponent<ExamEditionForm> RenderAdd() =>
        Render<ExamEditionForm>(parameters => parameters.Add(form => form.ExamId, AgentePf.Id));

    private IRenderedComponent<ExamEditionForm> RenderEdit(ExamEditionResponse edition)
    {
        Api.Editions.Add(edition);
        var page = Render<ExamEditionForm>(parameters => parameters
            .Add(form => form.ExamId, AgentePf.Id)
            .Add(form => form.Id, edition.Id));
        page.WaitForAssertion(() => page.Find("#edition-notice-year").GetAttribute("value").Should().Be("2026"));

        return page;
    }

    private void AddSubject(string label, int? questions, decimal? points) =>
        Api.NoticeSubjects.Add(new NoticeSubjectResponse(Guid.CreateVersion7(), EditionId, null, label, questions, null, points));

    private static void WaitForForm(IRenderedComponent<ExamEditionForm> page) =>
        page.WaitForAssertion(() => page.FindAll("#edition-organizer").Should().ContainSingle());

    private static void FillTheRequired(IRenderedComponent<ExamEditionForm> page)
    {
        var lookup = page.FindComponents<AppLookupField>().Single(component => component.Instance.Id == "edition-organizer");
        page.InvokeAsync(() => lookup.Instance.ValueChanged.InvokeAsync(
            new AppLookupOption(Cebraspe.Id, $"{Cebraspe.Name} ({Cebraspe.Acronym})"))).GetAwaiter().GetResult();
        page.Find("#edition-notice-year").Change("2026");
    }

    private static SaveExamEditionRequest SentBody(FakeCatalogApi api, HttpMethod method) =>
        FakeCatalogApi.Read<SaveExamEditionRequest>(api.Received.Last(call => call.Method == method).Body);

    private static IReadOnlyList<string?> SummaryLinks(IRenderedComponent<ExamEditionForm> page) =>
        [.. page.FindAll("#edition-error-summary .app-error-summary-list a").Select(link => link.GetAttribute("href"))];

    // AC1 and UC1: the card comes right after "Paper", with its five fields.
    [Fact]
    public void Render_Add_ShowsTheExamRulesCardAfterPaperWithItsFields()
    {
        var page = RenderAdd();
        WaitForForm(page);

        page.FindComponents<AppSectionCard>().Select(section => section.Instance.Title).Should().Equal(
            "Paper", "Exam rules", "Notice and application", "Publication", "Notice subjects");
        var rules = page.Find("section[aria-labelledby='edition-section-rules-title']");
        rules.QuerySelectorAll("#edition-duration-hours").Should().ContainSingle();
        rules.QuerySelectorAll("#edition-duration-minutes").Should().ContainSingle();
        rules.QuerySelectorAll("#edition-objective-cut-off").Should().ContainSingle();
        rules.QuerySelectorAll("#edition-essay-max-score").Should().ContainSingle();
        rules.QuerySelectorAll("#edition-essay-cut-off").Should().ContainSingle();
    }

    // AC1: 5 h 00 min is sent as 300 minutes, and the scores as typed in the reader's culture.
    [Fact]
    public void Save_Add_SendsTheDurationInMinutesAndTheThreeScores()
    {
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);

        page.Find("#edition-duration-hours").Change("5");
        page.Find("#edition-duration-minutes").Change("00");
        page.Find("#edition-objective-cut-off").Change(Typed(60m));
        page.Find("#edition-essay-max-score").Change(Typed(20m));
        page.Find("#edition-essay-cut-off").Change(Typed(10.5m));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            var sent = SentBody(Api, HttpMethod.Post);
            sent.DurationMinutes.Should().Be(300);
            sent.ObjectiveCutOffScore.Should().Be(60m);
            sent.EssayMaxScore.Should().Be(20m);
            sent.EssayCutOffScore.Should().Be(10.5m);
        });
    }

    // AC1: reopening the form shows the stored rules; the aside summarizes them.
    [Fact]
    public void Edit_AnEditionWithRules_ShowsTheStoredValuesAndTheAsideSummary()
    {
        var page = RenderEdit(WithRules);

        page.Find("#edition-duration-hours").GetAttribute("value").Should().Be("5");
        page.Find("#edition-duration-minutes").GetAttribute("value").Should().Be("0");
        page.Find("#edition-objective-cut-off").GetAttribute("value").Should().Be(Typed(60m));
        page.Find("#edition-essay-max-score").GetAttribute("value").Should().Be(Typed(20m));
        page.Find("#edition-essay-cut-off").GetAttribute("value").Should().Be(Typed(10m));
        Flat(page.Find(".app-form-aside").TextContent).Should().Contain("5 h").And.Contain("Cut-off 60");
    }

    // AC2 and BR1: clearing every rule field sends no rule at all.
    [Fact]
    public void Save_Edit_EveryRuleCleared_SendsNoRule()
    {
        var page = RenderEdit(WithRules);

        page.Find("#edition-duration-hours").Change("");
        page.Find("#edition-duration-minutes").Change("");
        page.Find("#edition-objective-cut-off").Change("");
        page.Find("#edition-essay-max-score").Change("");
        page.Find("#edition-essay-cut-off").Change("");
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            var sent = SentBody(Api, HttpMethod.Put);
            sent.DurationMinutes.Should().BeNull();
            sent.ObjectiveCutOffScore.Should().BeNull();
            sent.EssayMaxScore.Should().BeNull();
            sent.EssayCutOffScore.Should().BeNull();
        });
    }

    // AC3: a refused duration is said on the duration field and linked from the error summary.
    [Fact]
    public void Save_DurationRefused_ShowsTheMessageOnTheDurationField()
    {
        Api.WriteFailure = (HttpStatusCode.BadRequest, CatalogErrorCodes.ExamEditionDurationInvalid);
        RenderProviders();
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);
        page.Find("#edition-duration-minutes").Change("10");

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#edition-duration-hours-description").TextContent.Should()
            .Contain("between 15 minutes and 8 hours"));
        page.Find("#edition-duration-hours").GetAttribute("aria-invalid").Should().Be("true");
        SummaryLinks(page).Should().Equal("#edition-duration-hours");
    }

    // AC7: each essay refusal lands on the field it is about.
    [Theory]
    [InlineData(CatalogErrorCodes.ExamEditionEssayMaxScoreInvalid, "edition-essay-max-score")]
    [InlineData(CatalogErrorCodes.ExamEditionEssayCutOffInvalid, "edition-essay-cut-off")]
    [InlineData(CatalogErrorCodes.ExamEditionEssayCutOffWithoutMax, "edition-essay-cut-off")]
    [InlineData(CatalogErrorCodes.ExamEditionEssayCutOffAboveMax, "edition-essay-cut-off")]
    [InlineData(CatalogErrorCodes.ExamEditionObjectiveCutOffInvalid, "edition-objective-cut-off")]
    public void Save_ScoreRefused_ShowsTheMessageOnItsField(string code, string field)
    {
        Api.WriteFailure = (HttpStatusCode.BadRequest, code);
        RenderProviders();
        var page = RenderAdd();
        WaitForForm(page);
        FillTheRequired(page);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find($"#{field}").GetAttribute("aria-invalid").Should().Be("true"));
        page.Find($"#{field}-description").TextContent.Should().NotBeNullOrWhiteSpace();
        SummaryLinks(page).Should().Equal($"#{field}");
    }

    // AC5: every live subject complete — the form shows the objective total of the subjects.
    [Fact]
    public void Edit_EverySubjectComplete_ShowsTheObjectiveTotal()
    {
        AddSubject("Língua Portuguesa", 10, 1.50m);
        AddSubject("Legislação específica", 15, 2.00m);

        var page = RenderEdit(WithRules);

        page.WaitForAssertion(() => Flat(page.Find("#edition-objective-total").TextContent).Should()
            .Contain(45m.ToString("N2", CultureInfo.CurrentCulture)));
        page.FindAll("#edition-rules-missing").Should().BeEmpty();
    }

    // AC5 and UC3: a subject without points or count — the total is unknown and the form names the subject.
    [Fact]
    public void Edit_ASubjectWithoutPoints_SaysTheTotalIsUnknownAndNamesTheSubject()
    {
        AddSubject("Língua Portuguesa", 10, 1.50m);
        AddSubject("Informática", 5, null);
        AddSubject("Direito Penal", null, 2.00m);

        var page = RenderEdit(WithRules);

        page.WaitForAssertion(() =>
        {
            var missing = Flat(page.Find("#edition-rules-missing").TextContent);
            missing.Should().Contain("Informática").And.Contain("Direito Penal").And.NotContain("Língua Portuguesa");
        });
        Flat(page.Find("#edition-objective-total").TextContent).Should().NotContain(15m.ToString("N2", CultureInfo.CurrentCulture));
    }

    // F-181 AC11: the score fields and the year are typed controls with the keyboard of a number.
    [Fact]
    public void Edit_TheScoreFieldsAndTheYear_AreTypedNumberControls()
    {
        var page = RenderEdit(WithRules);

        page.FindAll(".app-score-field #edition-objective-cut-off").Should().ContainSingle();
        page.FindAll(".app-score-field #edition-essay-max-score").Should().ContainSingle();
        page.FindAll(".app-score-field #edition-essay-cut-off").Should().ContainSingle();
        page.FindAll(".app-year-field #edition-notice-year").Should().ContainSingle();
        page.Find("#edition-objective-cut-off").GetAttribute("inputmode").Should().Be("decimal");
    }

    // F-181 AC11: a text that is not a number blocks the save (a stale number is never sent).
    [Theory]
    [InlineData("edition-objective-cut-off", "6o")]
    [InlineData("edition-essay-max-score", "abc")]
    [InlineData("edition-essay-cut-off", "5,5")]
    [InlineData("edition-notice-year", "20x6")]
    public void Edit_ATextThatIsNotANumber_BlocksTheSave(string field, string typed)
    {
        var page = RenderEdit(WithRules);

        page.Find($"#{field}").Change(typed);
        page.Find(".app-form-save").Click();

        page.Find($"#{field}").GetAttribute("aria-invalid").Should().Be("true");
        page.Find($"#{field}").GetAttribute("value").Should().Be(typed);
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Put);
    }

    // AC6 and BR5: a cut-off above the known total is a warning; the save is still sent.
    [Fact]
    public void Edit_CutOffAboveTheKnownTotal_ShowsTheWarningAndStillSaves()
    {
        AddSubject("Língua Portuguesa", 10, 1.00m);
        var page = RenderEdit(WithRules);

        page.WaitForAssertion(() => page.FindAll("#edition-cut-off-above-total").Should().ContainSingle());
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => SentBody(Api, HttpMethod.Put).ObjectiveCutOffScore.Should().Be(60m));
    }

    [Fact]
    public void Edit_CutOffWithinTheTotal_ShowsNoWarning()
    {
        AddSubject("Língua Portuguesa", 10, 10.00m);
        var page = RenderEdit(WithRules);

        page.WaitForAssertion(() => page.FindAll("#edition-objective-total").Should().ContainSingle());
        page.FindAll("#edition-cut-off-above-total").Should().BeEmpty();
    }

    // AC8 and UC3: a published edition with no rule says which rules are missing, and saves as published.
    [Fact]
    public void Edit_PublishedWithNoRule_SaysWhichRulesAreMissingAndSavesAsPublished()
    {
        var page = RenderEdit(Edition with { Status = ExamEditionStatus.Published });

        page.WaitForAssertion(() => Flat(page.Find("#edition-rules-missing").TextContent).Should()
            .Contain("Duration").And.Contain("Objective cut-off").And.Contain("Essay maximum"));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() =>
        {
            var sent = SentBody(Api, HttpMethod.Put);
            sent.Status.Should().Be(nameof(ExamEditionStatus.Published));
            sent.DurationMinutes.Should().BeNull();
        });
    }
}
