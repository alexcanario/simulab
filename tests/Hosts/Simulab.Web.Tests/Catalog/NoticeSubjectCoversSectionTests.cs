using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-75 Screen 1: what each notice subject of an edition covers, as chips under its label, the "Not mapped" marker and
/// the footer's count of rows not mapped yet (UC1, BR2, BR8, BR13; AC1, AC6, AC10). The Api checks every rule; these
/// tests say what the section shows.
/// </summary>
public sealed class NoticeSubjectCoversSectionTests : CatalogPageTestContext
{
    private static readonly Guid EditionId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000e1");
    private static readonly Guid MathId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c1");
    private static readonly Guid LogicId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c4");
    private static readonly Guid FractionsId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c2");
    private static readonly Guid PropositionsId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c5");

    private static NoticeSubjectMappingResponse Whole(Guid id, string name) => new(id, name, null, null);

    private static NoticeSubjectMappingResponse Topic(Guid subjectId, string subject, Guid topicId, string topic) =>
        new(subjectId, subject, topicId, topic);

    private NoticeSubjectResponse Add(string label, params NoticeSubjectMappingResponse[] mappings)
    {
        var row = new NoticeSubjectResponse(Guid.CreateVersion7(), EditionId, null, label, 10, mappings);
        Api.NoticeSubjects.Add(row);
        return row;
    }

    private IRenderedComponent<NoticeSubjectsSection> RenderSection() =>
        Render<NoticeSubjectsSection>(parameters => parameters
            .Add(section => section.ExamId, AgentePf.Id)
            .Add(section => section.EditionId, EditionId));

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static void WaitForRows(IRenderedComponent<NoticeSubjectsSection> section, int count) =>
        section.WaitForAssertion(() => section.FindAll(".app-item-row").Should().HaveCount(count));

    private static IElement RowOf(IRenderedComponent<NoticeSubjectsSection> section, string label) =>
        section.FindAll(".app-item-row").Single(row => Flat(row.QuerySelector(".app-truncate")!.TextContent) == label);

    // What a chip reads on screen: "Matemática | whole subject" for a whole subject, "Matemática › Frações" for a topic.
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

    private static IReadOnlyList<string> ChipsOf(IElement row) =>
        [.. row.QuerySelectorAll("ul.app-chip-list > li").Select(ChipLabel)];

    // AC1 (screen side): both entries are shown, a whole subject with its marker and a topic with its subject; the order
    // is the screen's (subject name, whole before topics, topic name), whatever order the Api used (BR13).
    [Fact]
    public void Row_Mapped_ShowsWholeAndTopicChipsInTheScreensOrder()
    {
        Add(
            "Raciocínio Lógico-Matemático",
            Topic(LogicId, "Raciocínio Lógico", PropositionsId, "Proposições"),
            Topic(MathId, "Matemática", FractionsId, "Frações"),
            Whole(MathId, "Matemática"));

        var section = RenderSection();

        WaitForRows(section, 1);
        ChipsOf(RowOf(section, "Raciocínio Lógico-Matemático")).Should().Equal(
            "Matemática | whole subject",
            "Matemática › Frações",
            "Raciocínio Lógico › Proposições");
        section.FindAll(".app-status-chip").Should().BeEmpty("a mapped row has no 'Not mapped' marker");
    }

    // The chips are a list named after the field and the row, so a screen reader says "Covers: <label>, list, 2 items".
    [Fact]
    public void Row_Mapped_TheChipsAreAListLabelledByTheFieldAndTheRow()
    {
        Add("Raciocínio Lógico-Matemático", Whole(MathId, "Matemática"), Topic(LogicId, "Raciocínio Lógico", PropositionsId, "Proposições"));

        var section = RenderSection();

        WaitForRows(section, 1);
        section.Find("ul.app-chip-list").GetAttribute("aria-label").Should().Be("Covers: Raciocínio Lógico-Matemático");
    }

    [Fact]
    public void Row_Mapped_ATopicChipIsSpokenAsTheTopicOfItsSubjectAndItsIconIsDecorative()
    {
        Add("Lógica", Topic(LogicId, "Raciocínio Lógico", PropositionsId, "Proposições"));

        var section = RenderSection();

        WaitForRows(section, 1);
        var chip = section.Find("ul.app-chip-list > li");
        chip.QuerySelector(".app-visually-hidden")!.TextContent.Should().Be("Proposições, topic of Raciocínio Lógico");
        chip.QuerySelector(".app-chip-icon")!.GetAttribute("aria-hidden").Should().Be("true");
    }

    // BR13: names are shown as typed, never translated; nothing about a chip is a link or a tab stop.
    [Fact]
    public void Row_Mapped_TheChipsLinkNowhereAndTakeNoFocus()
    {
        Add("Lógica", Whole(MathId, "Matemática"), Topic(LogicId, "Raciocínio Lógico", PropositionsId, "Proposições"));

        var section = RenderSection();

        WaitForRows(section, 1);
        section.FindAll("ul.app-chip-list a, ul.app-chip-list button, ul.app-chip-list [tabindex]").Should().BeEmpty();
    }

    // AC6, BR8: after a topic moved, the Api sends it under its new subject, next to the subject mapped whole. Both chips
    // are shown as they are: the topic under the new subject, and no marker of an overlap.
    [Fact]
    public void Row_AnOverlapMadeByATopicMove_ShowsBothChipsAsIsWithTheTopicUnderItsNewSubject()
    {
        Add("Matemática e Lógica", Whole(MathId, "Matemática"), Topic(MathId, "Matemática", PropositionsId, "Proposições"));

        var section = RenderSection();

        WaitForRows(section, 1);
        ChipsOf(RowOf(section, "Matemática e Lógica")).Should().Equal("Matemática | whole subject", "Matemática › Proposições");
        section.FindAll(".app-chip.is-marked, .app-chip-mark").Should().BeEmpty("an overlap a move made is shown as is");
    }

    // The widest real heading lists about 20 entries: every one is shown, none behind a "more".
    [Fact]
    public void Row_WithManyEntries_ShowsEveryOneOfThem()
    {
        var entries = Enumerable.Range(1, 20).Select(number => Whole(Guid.CreateVersion7(), $"Disciplina {number:00}")).ToArray();
        Add("Legislação", entries);

        var section = RenderSection();

        WaitForRows(section, 1);
        section.FindAll("ul.app-chip-list > li").Should().HaveCount(20);
        section.Markup.Should().NotContain("more");
    }

    // AC10, UC1, BR2: three rows of which one is not mapped - that row says so and the footer counts it.
    [Fact]
    public void Section_ThreeRowsOneNotMapped_MarksThatRowAndTheFooterSaysOneIsNotMapped()
    {
        Add("Português", Whole(MathId, "Língua Portuguesa"));
        var loose = Add("Informática");
        Add("Direito", Whole(LogicId, "Direito Constitucional"));

        var section = RenderSection();

        WaitForRows(section, 3);
        var chip = RowOf(section, loose.Label).QuerySelector(".app-status-chip")!;
        Flat(chip.TextContent).Should().Be("Not mapped");
        chip.ClassList.Should().Contain("app-status-chip-warning");
        chip.QuerySelector(".app-status-chip-dot")!.GetAttribute("aria-hidden").Should().Be("true");
        RowOf(section, loose.Label).QuerySelectorAll("ul.app-chip-list").Should().BeEmpty();
        section.FindAll(".app-status-chip").Should().ContainSingle();
        Flat(section.Find(".app-notice-subjects-unmapped").TextContent).Should().Be("1 subject is not mapped yet.");
    }

    [Fact]
    public void Footer_NoRowMapped_EveryRowIsMarkedAndTheFooterCountsAllOfThem()
    {
        Add("Português");
        Add("Informática");
        Add("Direito");

        var section = RenderSection();

        WaitForRows(section, 3);
        section.FindAll(".app-status-chip-warning").Should().HaveCount(3);
        Flat(section.Find(".app-notice-subjects-unmapped").TextContent).Should().Be("3 subjects are not mapped yet.");
    }

    [Fact]
    public void Footer_EveryRowMapped_HasNoNotMappedLine()
    {
        Add("Português", Whole(MathId, "Língua Portuguesa"));
        Add("Direito", Whole(LogicId, "Direito Constitucional"));

        var section = RenderSection();

        WaitForRows(section, 2);
        section.FindAll(".app-notice-subjects-unmapped").Should().BeEmpty();
        section.FindAll(".app-status-chip").Should().BeEmpty();
    }

    // The sum lines of F-74 stay as they were; the not-mapped line comes after them, in secondary text.
    [Fact]
    public void Footer_TheNotMappedLineComesAfterTheSumAndKeepsItsText()
    {
        Add("Português");
        Add("Informática");

        var section = RenderSection();

        WaitForRows(section, 2);
        Flat(section.Find(".app-notice-subjects-total").TextContent).Should().Contain("Stated in the notice: 20 questions in total");
        section.Find(".app-notice-subjects-summary").Children.Select(child => child.ClassList.Contains("app-notice-subjects-total") ? "sum" : "unmapped")
            .Should().Equal("sum", "unmapped");
        section.Find(".app-notice-subjects-unmapped").ClassList.Should().Contain("app-muted");
    }

    // AC10: not being mapped blocks nothing - the edition's Save is still there, enabled.
    [Fact]
    public void EditionPage_ARowNotMapped_DoesNotBlockTheEditionsSave()
    {
        Api.Editions.Add(new ExamEditionResponse(
            EditionId, AgentePf.Id, Cebraspe.Id, Cebraspe.Name, Cebraspe.Acronym, 2026, "Guarda", null, null, null, ExamEditionStatus.Draft));
        Add("Informática");

        var page = Render<ExamEditionForm>(parameters => parameters
            .Add(form => form.ExamId, AgentePf.Id)
            .Add(form => form.Id, EditionId));

        page.WaitForAssertion(() => page.FindAll(".app-status-chip-warning").Should().ContainSingle());
        page.Find(".app-form-save").HasAttribute("disabled").Should().BeFalse();
    }
}
