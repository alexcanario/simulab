using Simulab.ExamEngine.Application.Attempts;
using Simulab.ExamEngine.Contracts;

namespace Simulab.ExamEngine.Tests;

/// <summary>F-169 BR3 and BR4 as a rule: the sections of a paper, built from its questions and the notice subject labels.</summary>
public sealed class AttemptSectionsTests
{
    private static readonly Guid Portuguese = Guid.Parse("0198f0a3-0000-7000-8000-0000000000b1");
    private static readonly Guid Legislation = Guid.Parse("0198f0a3-0000-7000-8000-0000000000b2");

    private static readonly Dictionary<Guid, string> Labels = new()
    {
        [Portuguese] = "Língua portuguesa",
        [Legislation] = "Legislação"
    };

    // AC3: ordered by the lowest paper number, each section in paper order, the questions with no notice subject last.
    [Fact]
    public void Build_OrdersSectionsByTheirLowestPaperNumber_AndPutsTheQuestionsWithNoSubjectLast()
    {
        var questions = new[]
        {
            Question(5, null), Question(1, Legislation), Question(4, Portuguese), Question(2, Portuguese), Question(3, Legislation)
        };

        var sections = AttemptSections.Build(questions, Labels);

        sections.Select(section => section.NoticeSubjectId).Should().Equal(Legislation, Portuguese, null);
        sections[0].QuestionIds.Should().Equal(IdOf(questions, 1), IdOf(questions, 3));
        sections[1].QuestionIds.Should().Equal(IdOf(questions, 2), IdOf(questions, 4));
        sections[2].QuestionIds.Should().Equal(IdOf(questions, 5));
        sections.Select(section => section.Label).Should().Equal("Legislação", "Língua portuguesa", null);
    }

    // AC4, BR4: a section's range is its lowest and highest paper number; an annulled number inside it is simply absent.
    [Fact]
    public void Build_GivesEachSectionItsLowestAndHighestPaperNumber_WhenTheNumberingJumps()
    {
        var questions = new[] { Question(45, Legislation), Question(46, Legislation), Question(48, Legislation), Question(50, Portuguese) };

        var sections = AttemptSections.Build(questions, Labels);

        sections.Select(section => (section.FirstPaperNumber, section.LastPaperNumber)).Should().Equal((45, 48), (50, 50));
        sections[0].QuestionIds.Should().HaveCount(3);
    }

    // AC3: a paper whose questions have no notice subject at all is one section with no id; no question, no section.
    [Fact]
    public void Build_WithNoNoticeSubject_IsOneLastSection_AndWithNoQuestion_IsEmpty()
    {
        var questions = new[] { Question(2, null), Question(1, null) };

        var sections = AttemptSections.Build(questions, Labels);

        sections.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new AttemptSectionResponse(1, 2, [IdOf(questions, 1), IdOf(questions, 2)]));
        AttemptSections.Build([], Labels).Should().BeEmpty();
    }

    private static AttemptQuestionContentResponse Question(int paperNumber, Guid? noticeSubjectId) =>
        new(Guid.CreateVersion7(), paperNumber, 1, AttemptQuestionType.SingleChoice, "pt-BR", $"<p>{paperNumber}</p>", [], NoticeSubjectId: noticeSubjectId);

    private static Guid IdOf(IEnumerable<AttemptQuestionContentResponse> questions, int paperNumber) =>
        questions.Single(question => question.PaperNumber == paperNumber).QuestionId;
}
