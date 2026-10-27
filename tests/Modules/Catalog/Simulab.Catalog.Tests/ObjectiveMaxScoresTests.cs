using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Tests;

/// <summary>F-168 BR4 (AC5): the objective total, written once in the contracts and read by the Api and the Web.</summary>
public sealed class ObjectiveMaxScoresTests
{
    // The Manaus 2026 notice, Quadro I: nine subjects, 60 questions, 100 points.
    private static readonly (int? QuestionCount, decimal? PointsPerQuestion)[] Manaus =
    [
        (10, 1.50m), (5, 1.50m), (5, 1.50m), (5, 1.50m), (5, 2.00m), (5, 1.50m), (5, 1.50m), (5, 1.50m), (15, 2.00m)
    ];

    [Fact]
    public void Compute_EveryRowComplete_IsTheSumOfCountTimesPoints()
    {
        var result = ObjectiveMaxScores.Compute(Manaus);

        result.Total.Should().Be(100.00m);
        result.IncompleteRows.Should().BeEmpty();
    }

    [Fact]
    public void Compute_ARowWithoutPointsOrWithoutCount_IsUnknownAndNamesThoseRows()
    {
        var rows = new (int? QuestionCount, decimal? PointsPerQuestion)[] { (10, 1.50m), (5, null), (null, 2.00m), (5, 2.00m) };

        var result = ObjectiveMaxScores.Compute(rows);

        result.Total.Should().BeNull();
        result.IncompleteRows.Should().Equal(1, 2);
    }

    // Decision 2026-10-27 (architect 6): no live subject is unknown, never 0 — a 0 total would fail every attempt.
    [Fact]
    public void Compute_NoRowAtAll_IsUnknownWithNoIncompleteRow()
    {
        var result = ObjectiveMaxScores.Compute([]);

        result.Total.Should().BeNull();
        result.IncompleteRows.Should().BeEmpty();
    }

    // T3: decimal, so three questions of 0.10 make exactly 0.30 and not 0.30000000000000004.
    [Fact]
    public void Compute_DecimalPoints_AddUpExactly()
    {
        var result = ObjectiveMaxScores.Compute([(3, 0.10m), (7, 0.10m)]);

        result.Total.Should().Be(1.00m);
    }
}
