namespace Simulab.Catalog.Contracts;

/// <summary>
/// What <see cref="ObjectiveMaxScores.Compute"/> answers (F-168, BR4): the total in points, or null while it is
/// unknown, and the indexes (in the order given) of the rows that lack a question count or points.
/// </summary>
public sealed record ObjectiveMaxScoreResult(decimal? Total, IReadOnlyList<int> IncompleteRows);
