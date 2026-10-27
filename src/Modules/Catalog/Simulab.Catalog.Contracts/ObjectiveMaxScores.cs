namespace Simulab.Catalog.Contracts;

/// <summary>
/// F-168 BR4, written once: the objective total of an edition is the sum of question count × points per question
/// over its live notice subjects. It is known only when every row states both and there is at least one row; it is
/// computed, never stored. The Api, the directory and the Web all call this one method (design trade-off 9).
/// </summary>
public static class ObjectiveMaxScores
{
    /// <summary>
    /// The total of <paramref name="rows"/>, or null with the indexes of the rows that lack a question count or the
    /// points per question. No row at all is unknown too, with no index: a total of 0 would fail every attempt.
    /// </summary>
    public static ObjectiveMaxScoreResult Compute(IReadOnlyList<(int? QuestionCount, decimal? PointsPerQuestion)> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var incomplete = new List<int>();
        var total = 0m;
        for (var index = 0; index < rows.Count; index++)
        {
            if (rows[index] is (int count, decimal points))
            {
                total += count * points;
            }
            else
            {
                incomplete.Add(index);
            }
        }

        return new ObjectiveMaxScoreResult(rows.Count > 0 && incomplete.Count == 0 ? total : null, incomplete);
    }
}
