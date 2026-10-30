using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-168 UC2 in the notice subjects section and its dialog: the points per question of each row, the objective total
/// on the total line, the field that sends them and the refusal of a value out of range (AC4, AC5).
/// </summary>
public sealed class NoticeSubjectPointsTests : CatalogPageTestContext
{
    private static readonly Guid EditionId = Guid.Parse("0198f0a3-0000-7000-8000-0000000002e1");

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static string Typed(decimal value) => value.ToString("0.00", CultureInfo.CurrentCulture);

    private NoticeSubjectResponse Add(string label, int? questions, decimal? points)
    {
        var row = new NoticeSubjectResponse(Guid.CreateVersion7(), EditionId, null, label, questions, null, points);
        Api.NoticeSubjects.Add(row);
        return row;
    }

    private IRenderedComponent<NoticeSubjectsSection> RenderSection() =>
        Render<NoticeSubjectsSection>(parameters => parameters
            .Add(section => section.ExamId, AgentePf.Id)
            .Add(section => section.EditionId, (Guid?)EditionId));

    private static void WaitForRows(IRenderedComponent<NoticeSubjectsSection> section, int count) =>
        section.WaitForAssertion(() => section.FindAll(".app-item-row").Should().HaveCount(count));

    private static void WaitForDialog(IRenderedComponent<MudBlazor.MudDialogProvider> dialogs) =>
        dialogs.WaitForAssertion(() => dialogs.FindAll("#notice-subject-points").Should().ContainSingle());

    private static SaveNoticeSubjectRequest SentBody(FakeCatalogApi api, HttpMethod method) =>
        FakeCatalogApi.Read<SaveNoticeSubjectRequest>(api.Received.Last(call => call.Method == method).Body);

    [Fact]
    public void Section_EachRow_ShowsItsPointsPerQuestionOrNothing()
    {
        Add("Língua Portuguesa", 10, 1.50m);
        Add("Informática", 5, null);

        var section = RenderSection();

        WaitForRows(section, 2);
        var points = section.FindAll(".app-item-row").Select(row => row.QuerySelector(".app-notice-subject-points")?.TextContent).ToList();
        Flat(points[0]!).Should().Be($"{1.50m.ToString("N2", CultureInfo.CurrentCulture)} pt");
        points[1].Should().BeNull("a row without points shows none");
    }

    // AC5: every row complete — the total line adds the objective total.
    [Fact]
    public void Section_EveryRowComplete_TheTotalLineShowsTheObjectiveTotal()
    {
        Add("Língua Portuguesa", 10, 1.50m);
        Add("Direito Constitucional", 5, 2.00m);
        Add("Legislação específica", 15, 2.00m);

        var section = RenderSection();

        WaitForRows(section, 3);
        Flat(section.Find(".app-notice-subjects-total").TextContent).Should()
            .Contain($"{55m.ToString("N2", CultureInfo.CurrentCulture)} points");
    }

    // AC5: a row without points — the total line says how many rows lack them instead of a total.
    [Fact]
    public void Section_ARowWithoutPoints_TheTotalLineSaysTheTotalIsUnknown()
    {
        Add("Língua Portuguesa", 10, 1.50m);
        Add("Informática", 5, null);

        var section = RenderSection();

        WaitForRows(section, 2);
        var total = Flat(section.Find(".app-notice-subjects-total").TextContent);
        total.Should().NotContain("points in total").And.Contain("1 subject has no points per question");
    }

    // UC2: the dialog sends the points as typed in the reader's culture.
    [Fact]
    public void Dialog_Add_SendsThePointsPerQuestion()
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());

        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-label").Change("Língua Portuguesa");
        dialogs.Find("#notice-subject-question-count").Change("10");
        dialogs.Find("#notice-subject-points").Change(Typed(1.5m));
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => SentBody(Api, HttpMethod.Post).PointsPerQuestion.Should().Be(1.50m));
    }

    [Fact]
    public void Dialog_Edit_ShowsTheStoredPointsAndClearingThemSendsNothing()
    {
        var row = Add("Língua Portuguesa", 10, 1.50m);
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        WaitForRows(section, 1);

        section.Find($"#notice-subject-actions-{row.Id}-edit").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-points").GetAttribute("value").Should().Be(Typed(1.5m));
        dialogs.Find("#notice-subject-points").Change("");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => SentBody(Api, HttpMethod.Put).PointsPerQuestion.Should().BeNull());
    }

    // F-181 AC10: both fields are typed controls, and a text that is not a number never reaches the Api.
    [Fact]
    public void Dialog_TheTwoFields_AreTypedNumberControls()
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());

        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);

        dialogs.FindAll(".app-integer-field #notice-subject-question-count").Should().ContainSingle();
        dialogs.FindAll(".app-score-field #notice-subject-points").Should().ContainSingle();
        dialogs.Find("#notice-subject-points").GetAttribute("inputmode").Should().Be("decimal");
    }

    [Theory]
    [InlineData("notice-subject-question-count", "abc")]
    [InlineData("notice-subject-points", "1,5")]
    public void Dialog_ATextThatIsNotANumber_IsRefusedAndNothingIsSent(string field, string typed)
    {
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());
        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-label").Change("Língua Portuguesa");

        dialogs.Find($"#{field}").Change(typed);
        dialogs.Find("button.app-form-save").Click();

        dialogs.Find($"#{field}").GetAttribute("aria-invalid").Should().Be("true");
        dialogs.Find($"#{field}").GetAttribute("value").Should().Be(typed);
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    // AC4: the refusal of the Api lands on the points field.
    [Fact]
    public void Dialog_PointsRefused_ShowsTheMessageOnThePointsField()
    {
        Api.WriteFailure = (HttpStatusCode.BadRequest, CatalogErrorCodes.NoticeSubjectPointsPerQuestionInvalid);
        var (dialogs, _) = RenderProviders();
        var section = RenderSection();
        section.WaitForAssertion(() => section.FindAll("#notice-subject-add").Should().ContainSingle());

        section.Find("#notice-subject-add").Click();
        WaitForDialog(dialogs);
        dialogs.Find("#notice-subject-label").Change("Língua Portuguesa");
        dialogs.Find("#notice-subject-points").Change(Typed(0m));
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find("#notice-subject-points").GetAttribute("aria-invalid").Should().Be("true"));
        dialogs.Find("#notice-subject-points-description").TextContent.Should().Contain("0.01");
    }
}
