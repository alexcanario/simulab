using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-76 on `/catalog/exams/{id}`: each published edition's row opens its syllabus - the notice subjects under their
/// group, with the number of questions and what each covers (UC1 to UC3, BR1 to BR8; AC1 to AC6). The Api checks which
/// editions and notice subjects reach the student; these tests say what the page shows.
/// </summary>
public sealed class CatalogExamSyllabusTests : StudentCatalogTestContext
{
    private static readonly Guid MathId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c1");
    private static readonly Guid FractionsId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c2");
    private static readonly Guid PortugueseId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c3");

    private static NoticeSubjectResponse Row(
        Guid edition,
        string? group,
        string label,
        int? questions,
        params NoticeSubjectMappingResponse[] mappings) =>
        new(Guid.CreateVersion7(), edition, group, label, questions, mappings);

    private static NoticeSubjectMappingResponse Whole(Guid id, string name) => new(id, name, null, null);

    private static NoticeSubjectMappingResponse Topic(Guid subjectId, string subject, Guid topicId, string topic) =>
        new(subjectId, subject, topicId, topic);

    private static PublishedExamEditionResponse WithSyllabus(
        PublishedExamEditionResponse edition,
        params NoticeSubjectResponse[] rows) => edition with { NoticeSubjects = rows };

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    // The edition used by most tests: two groups, three rows, one mixed mapping, one number left out.
    private static PublishedExamEditionResponse TwoGroups() => WithSyllabus(
        Edition2025,
        Row(Edition2025.Id, "General knowledge", "Portuguese", 10, Whole(PortugueseId, "Portuguese")),
        Row(Edition2025.Id, "General knowledge", "Mathematics and logic", 5, Whole(MathId, "Mathematics"), Topic(MathId, "Mathematics", FractionsId, "Fractions")),
        Row(Edition2025.Id, "Specific knowledge", "Municipal legislation", null));

    private IRenderedComponent<CatalogExam> RenderWith(params PublishedExamEditionResponse[] editions)
    {
        Api.Details[GuardaMunicipal.Id] = new PublishedExamDetailResponse(
            GuardaMunicipal.Id,
            GuardaMunicipal.Name,
            GuardaMunicipal.IssuingAuthorityName,
            GuardaMunicipal.AssessmentType,
            GuardaMunicipal.Scope,
            GuardaMunicipal.ScopeDetail,
            GuardaMunicipal.ContentLanguage,
            editions.Length,
            editions.Max(edition => edition.NoticeYear),
            editions);

        var page = Render<CatalogExam>(parameters => parameters.Add(item => item.Id, GuardaMunicipal.Id));
        page.WaitForAssertion(() => page.FindAll("li.app-item-row").Should().HaveCount(editions.Length));
        return page;
    }

    private static IReadOnlyList<IElement> Buttons(IRenderedComponent<CatalogExam> page) =>
        [.. page.FindAll("li.app-item-row button[aria-expanded]")];

    private static string ChipLabel(IElement item)
    {
        var text = item.QuerySelector(".app-chip-text")!;
        if (text.QuerySelector(".app-chip-secondary") is { } secondary)
        {
            var name = string.Concat(text.ChildNodes.Where(node => node.NodeType == NodeType.Text).Select(node => node.TextContent)).Trim();
            return $"{name} | {Flat(secondary.TextContent)}";
        }

        return Flat(text.QuerySelector("span[aria-hidden='true']")!.TextContent);
    }

    // AC6, BR2: closed when the page opens; the button says how many notice subjects the edition has.
    [Fact]
    public void Render_TheSyllabusIsClosedAndTheButtonSaysHowManySubjects()
    {
        var page = RenderWith(TwoGroups());

        var button = Buttons(page).Should().ContainSingle().Subject;
        Flat(button.TextContent).Should().Contain("Show syllabus (3 subjects)");
        button.GetAttribute("aria-expanded").Should().Be("false");
        page.FindAll(".app-syllabus-row").Should().BeEmpty();
    }

    // AC1, BR4, BR5: display order, one heading per group, the number of each row.
    [Fact]
    public void Open_ShowsTheRowsInOrderUnderOneHeadingPerGroupWithTheirNumbers()
    {
        var page = RenderWith(TwoGroups());

        Buttons(page).Single().Click();

        page.FindAll(".app-syllabus-group-title").Select(title => Flat(title.TextContent))
            .Should().Equal("General knowledge", "Specific knowledge");
        page.FindAll(".app-syllabus-row .app-syllabus-label").Select(label => Flat(label.TextContent))
            .Should().Equal("Portuguese", "Mathematics and logic", "Municipal legislation");
        var counts = page.FindAll(".app-syllabus-row .app-syllabus-count").Select(count => Flat(count.TextContent)).ToList();
        counts[0].Should().Be("10 questions");
        counts[1].Should().Be("5 questions");
        counts[2].Should().Contain("number of questions not stated");
        Buttons(page).Single().GetAttribute("aria-expanded").Should().Be("true");
    }

    // AC2, BR6: a whole subject and a topic in the F-75 order and text.
    [Fact]
    public void Open_ShowsTheCoverEntriesInTheFormOfTheBackOffice()
    {
        var page = RenderWith(TwoGroups());

        Buttons(page).Single().Click();

        var row = page.FindAll(".app-syllabus-row").Single(item => Flat(item.QuerySelector(".app-syllabus-label")!.TextContent) == "Mathematics and logic");
        row.QuerySelectorAll("ul.app-chip-list > li").Select(ChipLabel).Should().Equal(
            "Mathematics | whole subject",
            "Mathematics › Fractions");
        row.QuerySelector("ul.app-chip-list")!.GetAttribute("aria-label").Should().Be("Covers: Mathematics and logic");
    }

    // AC3, BR6: a plain note, not the back office's warning chip.
    [Fact]
    public void Open_ANoticeSubjectNotMappedYet_ShowsTheNoteAndNoWarningChip()
    {
        var page = RenderWith(TwoGroups());

        Buttons(page).Single().Click();

        var row = page.FindAll(".app-syllabus-row").Single(item => Flat(item.QuerySelector(".app-syllabus-label")!.TextContent) == "Municipal legislation");
        Flat(row.TextContent).Should().Contain("Topics not mapped yet");
        row.QuerySelectorAll(".app-status-chip, ul.app-chip-list").Should().BeEmpty();
    }

    // AC4, BR3: no button, and the row says so; an edition the Api sent with an empty list reads the same.
    [Fact]
    public void Render_AnEditionWithoutNoticeSubjects_SaysItIsNotRegisteredAndHasNoButton()
    {
        var page = RenderWith(TwoGroups(), Edition2023 with { NoticeSubjects = [] });

        var rows = page.FindAll("li.app-item-row");
        Buttons(page).Should().ContainSingle();
        var empty = rows.Single(row => row.TextContent.Contains("2023"));
        empty.QuerySelector("button").Should().BeNull();
        Flat(empty.TextContent).Should().Contain("The syllabus of this edition is not registered yet.");
    }

    // AC5, BR7: the sum of the stated numbers, and how many are left out of it.
    [Fact]
    public void Open_TheTotalIsTheSumOfTheStatedNumbersAndSaysHowManyAreLeftOut()
    {
        var page = RenderWith(TwoGroups());

        Buttons(page).Single().Click();

        Flat(page.Find(".app-syllabus-total").TextContent).Should()
            .Contain("Stated in the notice: 15 questions in total")
            .And.Contain("1 subject has no number and is left out of the sum.");
    }

    [Fact]
    public void Open_NoSubjectStatesItsNumber_SaysSo()
    {
        var page = RenderWith(WithSyllabus(Edition2025, Row(Edition2025.Id, null, "Portuguese", null)));

        Buttons(page).Single().Click();

        Flat(page.Find(".app-syllabus-total").TextContent).Should().Contain("No subject states its number of questions.");
    }

    // BR4: no heading is drawn over a list in which no row has a group.
    [Fact]
    public void Open_NoRowHasAGroup_NoHeadingIsDrawn()
    {
        var page = RenderWith(WithSyllabus(
            Edition2025,
            Row(Edition2025.Id, null, "Portuguese", 10),
            Row(Edition2025.Id, " ", "Mathematics", 5)));

        Buttons(page).Single().Click();

        page.FindAll(".app-syllabus-row").Should().HaveCount(2);
        page.FindAll(".app-syllabus-group-title").Should().BeEmpty();
    }

    // AC6, BR2: one block opens and closes on its own; several can be open; the button's name follows its state.
    [Fact]
    public void Toggle_OpensOneEditionAtATime_LeavesTheOtherAsItWas_AndTheNameFollowsTheState()
    {
        var page = RenderWith(
            TwoGroups(),
            WithSyllabus(Edition2023, Row(Edition2023.Id, null, "Informatics", 8)));

        var buttons = Buttons(page);
        buttons.Should().HaveCount(2);
        buttons[0].Click();

        var after = Buttons(page);
        after[0].GetAttribute("aria-expanded").Should().Be("true");
        Flat(after[0].TextContent).Should().Contain("Hide syllabus");
        after[1].GetAttribute("aria-expanded").Should().Be("false");
        page.FindAll(".app-syllabus-row").Should().HaveCount(3);

        after[1].Click();
        page.FindAll(".app-syllabus-row").Should().HaveCount(4);

        Buttons(page)[0].Click();
        Buttons(page)[0].GetAttribute("aria-expanded").Should().Be("false");
        Flat(Buttons(page)[0].TextContent).Should().Contain("Show syllabus (3 subjects)");
        page.FindAll(".app-syllabus-row").Should().HaveCount(1);
    }

    // The button controls the block it opens, so a screen reader can jump to it.
    [Fact]
    public void Open_TheButtonControlsTheBlockItOpens()
    {
        var page = RenderWith(TwoGroups());

        var button = Buttons(page).Single();
        button.Click();

        var controls = Buttons(page).Single().GetAttribute("aria-controls");
        controls.Should().NotBeNullOrWhiteSpace();
        page.Find($"#{controls}").QuerySelectorAll(".app-syllabus-row").Should().HaveCount(3);
    }

    // BR8: labels and groups are content, marked with the exam's language, and shown as stored.
    [Fact]
    public void Open_LabelsAndGroupsCarryTheExamsContentLanguage()
    {
        var page = RenderWith(TwoGroups());

        Buttons(page).Single().Click();

        page.FindAll(".app-syllabus-label").Should().OnlyContain(label => label.GetAttribute("lang") == "pt-BR");
        page.FindAll(".app-syllabus-group-title").Should().OnlyContain(title => title.GetAttribute("lang") == "pt-BR");
    }
}
