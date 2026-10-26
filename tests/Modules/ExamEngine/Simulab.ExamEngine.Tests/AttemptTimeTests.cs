using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Domain.Entities;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-166 AC11 and BR11: the times of an attempt keep microseconds, as the database does, so the response of the first submit
/// shows the same time that every later read and the second submit show.
/// </summary>
public sealed class AttemptTimeTests
{
    private static readonly DateTimeOffset WithSevenDigits = new DateTimeOffset(2026, 10, 26, 20, 0, 21, TimeSpan.Zero).AddTicks(8269131);

    private static Attempt Started() =>
        Attempt.Start(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            WithSevenDigits,
            [new AttemptQuestion(Guid.CreateVersion7(), 1, 1, AttemptQuestionType.TrueFalse)]);

    [Fact]
    public void Submit_KeepsMicrosecondsOnly()
    {
        var attempt = Started();

        attempt.Submit(WithSevenDigits);

        attempt.SubmittedAt.Should().Be(WithSevenDigits - TimeSpan.FromTicks(1));
    }

    [Fact]
    public void Start_KeepsMicrosecondsOnly()
    {
        Started().StartedAt.Should().Be(WithSevenDigits - TimeSpan.FromTicks(1));
    }
}
