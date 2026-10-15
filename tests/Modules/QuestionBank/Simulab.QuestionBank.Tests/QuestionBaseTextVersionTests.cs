using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-102 BR4, BR5 on the question alone: the base text HTML a caller hands to
/// <see cref="Question.NextVersion"/> is part of what a version snapshots, and a different one makes the next
/// version with no answer key change and no reason.
/// </summary>
public sealed class QuestionBaseTextVersionTests
{
    private const string Passage = "<p>O rio corria devagar.</p>";
    private static readonly Guid Topic = Guid.CreateVersion7();
    private static readonly Guid BaseTextId = Guid.CreateVersion7();

    private static SanitizedHtml Text(string html) => new(html, html);

    private static Question PublishedLinked(Guid? baseTextId = null)
    {
        var question = Question.Create(new QuestionContent(
            Topic,
            "pt-BR",
            Text("<p>Segundo o texto, o rio corria como?</p>"),
            baseTextId ?? BaseTextId,
            SanitizedHtml.Empty,
            null,
            [new OptionDraft(Text("Devagar"), true), new OptionDraft(Text("Depressa"), false)])).Value!;
        question.Publish().IsSuccess.Should().BeTrue();

        return question;
    }

    // AC3, BR5: the version keeps what the student was shown, the passage included.
    [Fact]
    public void NextVersion_FirstPublication_SnapshotsTheLinkedBaseTextHtml()
    {
        var question = PublishedLinked();

        var result = question.NextVersion(null, null, Passage);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Number.Should().Be(1);
        result.Value.BaseTextHtml.Should().Be(Passage);
    }

    // AC3, BR4: the passage was edited; the question's own content is the same, so only the passage differs.
    [Fact]
    public void NextVersion_TheLinkedTextChanged_MakesTheNextVersion_NotAKeyChange_WithNoReason()
    {
        var question = PublishedLinked();
        var first = question.NextVersion(null, null, Passage).Value!;

        var result = question.NextVersion(first, null, "<p>O rio corria depressa.</p>");

        result.IsSuccess.Should().BeTrue();
        var version = result.Value!;
        version.Number.Should().Be(2);
        version.BaseTextHtml.Should().Be("<p>O rio corria depressa.</p>");
        version.AnswerKeyChanged.Should().BeFalse();
        version.Reason.Should().BeNull();
        version.StatementHtml.Should().Be(first.StatementHtml);
        first.BaseTextHtml.Should().Be(Passage, "a version already recorded is never touched (BR5)");
    }

    [Fact]
    public void NextVersion_TheLinkedTextIsTheSame_MakesNothing()
    {
        var question = PublishedLinked();
        var first = question.NextVersion(null, null, Passage).Value!;

        var result = question.NextVersion(first, null, Passage);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // BR4: unlinking is a change of what is shown, as linking is.
    [Fact]
    public void NextVersion_TheLinkWasRemoved_MakesTheNextVersionWithNoBaseText()
    {
        var question = PublishedLinked();
        var first = question.NextVersion(null, null, Passage).Value!;
        question.Update(new QuestionContent(
            Topic,
            "pt-BR",
            Text("<p>Segundo o texto, o rio corria como?</p>"),
            null,
            SanitizedHtml.Empty,
            null,
            [new OptionDraft(Text("Devagar"), true), new OptionDraft(Text("Depressa"), false)]))
            .IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null, null);

        result.Value!.Number.Should().Be(2);
        result.Value.BaseTextHtml.Should().BeNull();
        question.BaseTextId.Should().BeNull();
    }
}
